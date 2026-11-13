// Lecture / écriture de cookies côté navigateur

export function lireCookie(nom: string): string | null {
  const m = document.cookie.match(new RegExp('(?:^|;\\s*)' + nom + '=([^;]*)'))
  return m ? decodeURIComponent(m[1]) : null
}

export function ecrireCookie(nom: string, valeur: string, joursValidite: number) {
  const secure = location.protocol === 'https:' ? '; Secure' : ''
  document.cookie =
    `${nom}=${encodeURIComponent(valeur)}; Max-Age=${Math.round(joursValidite * 86400)}` +
    `; Path=/; SameSite=Lax${secure}`
}

export function supprimerCookie(nom: string) {
  document.cookie = `${nom}=; Max-Age=0; Path=/; SameSite=Lax`
}

export function lireCookieJson<T>(nom: string): T | null {
  const brut = lireCookie(nom)
  if (!brut) return null
  try {
    return JSON.parse(brut) as T
  } catch {
    return null
  }
}
