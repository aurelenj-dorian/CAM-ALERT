import { CircleCheck, Lock, Paperclip, Send } from 'lucide-react'
import { useRef, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Champ, aria } from '../components/Champ'
import { Chargement, Erreur } from '../components/Etats'
import { ErreurApi, post } from '../lib/api'
import type { Referentiels } from '../lib/types'
import { useApi, useTitre } from '../lib/useApi'

const TAILLE_MAX = 5 * 1024 * 1024
const EXTENSIONS = ['.jpg', '.jpeg', '.png', '.pdf']

type Erreurs = Partial<Record<'dossierId' | 'nom' | 'email' | 'message' | 'fichier', string>>

export default function Signaler() {
  useTitre('Transmettre une information')
  const [params] = useSearchParams()
  const referentiels = useApi<Referentiels>('/api/referentiels')
  const formulaire = useRef<HTMLFormElement>(null)

  const [erreurs, setErreurs] = useState<Erreurs>({})
  const [erreurGenerale, setErreurGenerale] = useState('')
  const [envoi, setEnvoi] = useState(false)
  const [confirmation, setConfirmation] = useState('')

  // Pré-sélection depuis une fiche (?dossier=ID), seulement si le dossier est publié
  const dossierParam = params.get('dossier') ?? ''
  const dossiers = referentiels.donnees?.dossiers ?? []
  const preselection = dossiers.some((d) => String(d.id) === dossierParam) ? dossierParam : ''

  function valider(donnees: FormData): Erreurs {
    const e: Erreurs = {}
    if (!donnees.get('dossierId')) e.dossierId = 'Veuillez sélectionner un dossier.'
    if (!String(donnees.get('message') ?? '').trim()) e.message = 'Le message est obligatoire.'
    const email = String(donnees.get('email') ?? '').trim()
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email)) e.email = 'Adresse e-mail invalide.'
    const fichier = donnees.get('fichier')
    if (fichier instanceof File && fichier.size > 0) {
      const ext = fichier.name.slice(fichier.name.lastIndexOf('.')).toLowerCase()
      if (!EXTENSIONS.includes(ext)) e.fichier = 'Type de fichier non autorisé (jpg, png ou pdf).'
      else if (fichier.size > TAILLE_MAX) e.fichier = 'Fichier trop volumineux (5 Mo maximum).'
    }
    return e
  }

  async function soumettre(ev: FormEvent<HTMLFormElement>) {
    ev.preventDefault()
    const donnees = new FormData(ev.currentTarget)
    const fichier = donnees.get('fichier')
    if (fichier instanceof File && fichier.size === 0) donnees.delete('fichier')

    const e = valider(donnees)
    setErreurs(e)
    setErreurGenerale('')
    if (Object.keys(e).length) {
      focaliserPremiereErreur(e)
      return
    }

    setEnvoi(true)
    try {
      const r = await post<{ message: string }>('/api/signalements', donnees)
      setConfirmation(r.message)
      window.scrollTo({ top: 0 })
    } catch (err) {
      const erreur = err instanceof ErreurApi ? err : new ErreurApi(0, 'Une erreur est survenue.')
      setErreurs(erreur.champs as Erreurs)
      setErreurGenerale(erreur.message)
      focaliserPremiereErreur(erreur.champs as Erreurs)
    } finally {
      setEnvoi(false)
    }
  }

  function focaliserPremiereErreur(e: Erreurs) {
    const premier = (['dossierId', 'nom', 'email', 'message', 'fichier'] as const).find((c) => e[c])
    if (premier) formulaire.current?.querySelector<HTMLElement>(`#signal-${premier}`)?.focus()
  }

  if (confirmation)
    return (
      <div className="conteneur page page-etroite">
        <div className="confirmation" role="status">
          <CircleCheck size={40} aria-hidden="true" />
          <h1>Merci, votre information a été transmise</h1>
          <p>{confirmation}</p>
          <p className="texte-secondaire">Elle sera examinée par les agents habilités dans les meilleurs délais.</p>
          <div className="actions-formulaire">
            <button type="button" className="bouton bouton-secondaire" onClick={() => setConfirmation('')}>
              Envoyer une autre information
            </button>
            <Link to="/dossiers" className="bouton bouton-primaire">Retour aux dossiers</Link>
          </div>
        </div>
      </div>
    )

  return (
    <div className="conteneur page page-etroite">
      <header className="page-entete">
        <h1>Transmettre une information</h1>
        <p className="texte-secondaire">
          Vous avez vu une personne recherchée ou disparue, ou disposez d’un renseignement utile ? Décrivez-le
          ci-dessous. Vous pouvez rester anonyme.
        </p>
        <p className="mention-confidentialite">
          <Lock size={16} aria-hidden="true" /> Votre message n’est visible que des agents habilités. Il n’est jamais publié.
        </p>
      </header>

      {referentiels.chargement && <Chargement />}
      {referentiels.erreur && <Erreur erreur={referentiels.erreur} onReessayer={referentiels.recharger} />}

      {referentiels.donnees && (
        <form ref={formulaire} className="carte formulaire" onSubmit={soumettre} noValidate>
          {erreurGenerale && <div className="alerte alerte-erreur" role="alert">{erreurGenerale}</div>}

          <Champ id="signal-dossierId" libelle="Dossier concerné" obligatoire erreur={erreurs.dossierId}>
            <select name="dossierId" defaultValue={preselection} key={preselection} required
              {...aria('signal-dossierId', erreurs.dossierId)}>
              <option value="">— Sélectionnez un dossier —</option>
              {dossiers.map((d) => <option key={d.id} value={d.id}>{d.libelle}</option>)}
            </select>
          </Champ>

          <div className="grille-2">
            <Champ id="signal-nom" libelle="Votre nom" facultatif erreur={erreurs.nom}>
              <input name="nom" type="text" maxLength={100} autoComplete="name" {...aria('signal-nom', erreurs.nom)} />
            </Champ>
            <Champ id="signal-email" libelle="Votre e-mail" facultatif erreur={erreurs.email}
              aide="Uniquement pour vous recontacter si nécessaire.">
              <input name="email" type="email" maxLength={150} autoComplete="email"
                {...aria('signal-email', erreurs.email, true)} />
            </Champ>
          </div>

          <Champ id="signal-message" libelle="Votre message" obligatoire erreur={erreurs.message}
            aide="Lieu, date, heure, description : tout détail peut aider.">
            <textarea name="message" rows={7} maxLength={4000} required {...aria('signal-message', erreurs.message, true)} />
          </Champ>

          <Champ id="signal-fichier" libelle="Pièce jointe" facultatif erreur={erreurs.fichier}
            aide="Photo ou document : jpg, png ou pdf, 5 Mo maximum.">
            <div className="champ-fichier">
              <Paperclip size={18} aria-hidden="true" />
              <input name="fichier" type="file" accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf"
                {...aria('signal-fichier', erreurs.fichier, true)} />
            </div>
          </Champ>

          <button type="submit" className="bouton bouton-primaire bouton-large" disabled={envoi}>
            <Send size={18} aria-hidden="true" /> {envoi ? 'Envoi en cours…' : 'Envoyer l’information'}
          </button>
        </form>
      )}
    </div>
  )
}
