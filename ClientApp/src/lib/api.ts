import { lireCookie } from './cookies'

// Erreur d'API avec, le cas échéant, les erreurs par champ (réponse 400 « ValidationProblem »)
export class ErreurApi extends Error {
  readonly statut: number
  readonly champs: Record<string, string>

  constructor(statut: number, message: string, champs: Record<string, string> = {}) {
    super(message)
    this.statut = statut
    this.champs = champs
  }
}

async function erreurDepuis(reponse: Response): Promise<ErreurApi> {
  let message = 'Une erreur est survenue. Réessayez dans un instant.'
  const champs: Record<string, string> = {}
  try {
    const corps = await reponse.json()
    if (typeof corps?.message === 'string') message = corps.message
    if (corps?.errors && typeof corps.errors === 'object') {
      for (const [cle, valeurs] of Object.entries(corps.errors)) {
        const nom = cle.charAt(0).toLowerCase() + cle.slice(1)
        if (Array.isArray(valeurs) && valeurs.length) champs[nom] = String(valeurs[0])
      }
      if (Object.keys(champs).length) message = 'Certains champs sont à corriger.'
    }
  } catch {
    /* réponse non JSON */
  }
  if (reponse.status === 429 && message.startsWith('Une erreur'))
    message = "Trop d'envois en peu de temps. Réessayez dans une minute."
  return new ErreurApi(reponse.status, message, champs)
}

export async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  let reponse: Response
  try {
    reponse = await fetch(url, { headers: { Accept: 'application/json' }, signal })
  } catch (e) {
    if ((e as Error).name === 'AbortError') throw e
    throw new ErreurApi(0, 'Le serveur est injoignable. Vérifiez votre connexion.')
  }
  if (!reponse.ok) throw await erreurDepuis(reponse)
  return (await reponse.json()) as T
}

// Jeton anti-CSRF : cookie XSRF-TOKEN posé par /api/antiforgery, renvoyé en en-tête
let jetonEnCours: Promise<void> | null = null
async function jetonCsrf(forcer = false): Promise<string> {
  if (forcer || !lireCookie('XSRF-TOKEN')) {
    jetonEnCours ??= fetch('/api/antiforgery', { credentials: 'same-origin' }).then(() => {
      jetonEnCours = null
    })
    await jetonEnCours
  }
  return lireCookie('XSRF-TOKEN') ?? ''
}

export async function post<T = unknown>(
  url: string,
  corps?: FormData | object,
  nouvelEssai = true,
): Promise<T> {
  const enTetes: Record<string, string> = {
    Accept: 'application/json',
    'X-XSRF-TOKEN': await jetonCsrf(!nouvelEssai),
  }
  let body: BodyInit | undefined
  if (corps instanceof FormData) body = corps
  else if (corps !== undefined) {
    body = JSON.stringify(corps)
    enTetes['Content-Type'] = 'application/json'
  }

  let reponse: Response
  try {
    reponse = await fetch(url, { method: 'POST', headers: enTetes, body, credentials: 'same-origin' })
  } catch {
    throw new ErreurApi(0, 'Le serveur est injoignable. Vérifiez votre connexion.')
  }
  // 400 sans corps = jeton anti-CSRF refusé (expiré) : un seul nouvel essai avec un jeton neuf
  if (reponse.status === 400 && nouvelEssai && reponse.headers.get('content-length') === '0')
    return post<T>(url, corps, false)
  if (!reponse.ok) throw await erreurDepuis(reponse)
  if (reponse.status === 204) return undefined as T
  return (await reponse.json()) as T
}
