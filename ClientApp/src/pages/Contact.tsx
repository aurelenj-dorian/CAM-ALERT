import { CircleCheck, Mail, Send } from 'lucide-react'
import { useRef, useState, type FormEvent } from 'react'
import { Champ, aria } from '../components/Champ'
import { ErreurApi, post } from '../lib/api'
import { useTitre } from '../lib/useApi'

// Valeurs identiques à la liste fermée du serveur (FormulairesController.SujetsContact)
const SUJETS = [
  { valeur: 'Signalement infraction', libelle: 'Signalement d’une infraction' },
  { valeur: 'Information personne recherchée', libelle: 'Information sur une personne recherchée' },
  { valeur: 'Disparition', libelle: 'Personne disparue' },
  { valeur: 'Presse', libelle: 'Presse / médias' },
  { valeur: 'Recrutement', libelle: 'Recrutement / stage' },
  { valeur: 'Commission de contrôle', libelle: 'Commission de contrôle des fichiers' },
  { valeur: 'Autre', libelle: 'Autre demande' },
]

type Erreurs = Partial<Record<'nom' | 'email' | 'sujet' | 'message', string>>

export default function Contact() {
  useTitre('Contact')
  const formulaire = useRef<HTMLFormElement>(null)
  const [erreurs, setErreurs] = useState<Erreurs>({})
  const [erreurGenerale, setErreurGenerale] = useState('')
  const [envoi, setEnvoi] = useState(false)
  const [envoye, setEnvoye] = useState(false)

  async function soumettre(ev: FormEvent<HTMLFormElement>) {
    ev.preventDefault()
    const f = new FormData(ev.currentTarget)
    const corps = {
      nom: String(f.get('nom') ?? '').trim(),
      email: String(f.get('email') ?? '').trim(),
      sujet: String(f.get('sujet') ?? ''),
      message: String(f.get('message') ?? '').trim(),
    }
    const e: Erreurs = {}
    if (!corps.nom) e.nom = 'Le nom est obligatoire.'
    if (!corps.email) e.email = 'L’adresse e-mail est obligatoire.'
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(corps.email)) e.email = 'Adresse e-mail invalide.'
    if (!corps.sujet) e.sujet = 'Choisissez un sujet.'
    if (!corps.message) e.message = 'Le message est obligatoire.'
    setErreurs(e)
    setErreurGenerale('')
    if (Object.keys(e).length) return focaliser(e)

    setEnvoi(true)
    try {
      await post('/api/contact', corps)
      setEnvoye(true)
    } catch (err) {
      const erreur = err instanceof ErreurApi ? err : new ErreurApi(0, 'Une erreur est survenue.')
      setErreurs(erreur.champs as Erreurs)
      setErreurGenerale(erreur.message)
      focaliser(erreur.champs as Erreurs)
    } finally {
      setEnvoi(false)
    }
  }

  function focaliser(e: Erreurs) {
    const premier = (['nom', 'email', 'sujet', 'message'] as const).find((c) => e[c])
    if (premier) formulaire.current?.querySelector<HTMLElement>(`#contact-${premier}`)?.focus()
  }

  return (
    <div className="conteneur page page-etroite">
      <header className="page-entete">
        <h1>Nous contacter</h1>
        <p className="texte-secondaire">
          Une question sur le portail, une demande presse ou une remarque ? Écrivez-nous.
          Pour transmettre une information sur un dossier, utilisez plutôt le formulaire « Signaler ».
        </p>
      </header>

      {envoye ? (
        <div className="confirmation" role="status">
          <CircleCheck size={40} aria-hidden="true" />
          <h2>Message envoyé</h2>
          <p>Merci, nous vous répondrons dans les meilleurs délais.</p>
          <button type="button" className="bouton bouton-secondaire" onClick={() => setEnvoye(false)}>
            Envoyer un autre message
          </button>
        </div>
      ) : (
        <form ref={formulaire} className="carte formulaire" onSubmit={soumettre} noValidate>
          {erreurGenerale && <div className="alerte alerte-erreur" role="alert">{erreurGenerale}</div>}
          <div className="grille-2">
            <Champ id="contact-nom" libelle="Nom" obligatoire erreur={erreurs.nom}>
              <input name="nom" type="text" maxLength={100} autoComplete="name" required {...aria('contact-nom', erreurs.nom)} />
            </Champ>
            <Champ id="contact-email" libelle="E-mail" obligatoire erreur={erreurs.email}>
              <input name="email" type="email" maxLength={150} autoComplete="email" required {...aria('contact-email', erreurs.email)} />
            </Champ>
          </div>
          <Champ id="contact-sujet" libelle="Sujet" obligatoire erreur={erreurs.sujet}>
            <select name="sujet" defaultValue="" required {...aria('contact-sujet', erreurs.sujet)}>
              <option value="">— Choisissez un sujet —</option>
              {SUJETS.map((s) => <option key={s.valeur} value={s.valeur}>{s.libelle}</option>)}
            </select>
          </Champ>
          <Champ id="contact-message" libelle="Message" obligatoire erreur={erreurs.message}>
            <textarea name="message" rows={7} maxLength={4000} required {...aria('contact-message', erreurs.message)} />
          </Champ>
          <button type="submit" className="bouton bouton-primaire bouton-large" disabled={envoi}>
            <Send size={18} aria-hidden="true" /> {envoi ? 'Envoi en cours…' : 'Envoyer le message'}
          </button>
          <p className="aide aide-centree">
            <Mail size={14} aria-hidden="true" /> Vos coordonnées servent uniquement à vous répondre.
          </p>
        </form>
      )}
    </div>
  )
}
