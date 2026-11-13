import { Cookie, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router'
import { useConsentement, type Choix } from '../lib/consentement'

// Bandeau affiché tant qu'aucun choix valide n'a été fait.
// « Tout accepter » et « Tout refuser » ont le même poids visuel.
export function BandeauCookies() {
  const { choix, panneauOuvert, ouvrirPanneau, enregistrer } = useConsentement()
  if (choix !== null || panneauOuvert) return null

  return (
    <section className="bandeau-cookies" aria-labelledby="bandeau-cookies-titre" role="region">
      <div className="bandeau-cookies-texte">
        <h2 id="bandeau-cookies-titre">
          <Cookie size={20} aria-hidden="true" /> Vos choix sur les cookies
        </h2>
        <p>
          Nous utilisons des cookies nécessaires au fonctionnement du site et, avec votre accord, des cookies
          pour mémoriser vos préférences et mesurer l'audience de manière anonyme.{' '}
          <Link to="/cookies">En savoir plus</Link>
        </p>
      </div>
      <div className="bandeau-cookies-actions">
        <button type="button" className="bouton bouton-secondaire" onClick={() => enregistrer({ preferences: false, audience: false })}>
          Tout refuser
        </button>
        <button type="button" className="bouton bouton-secondaire" onClick={ouvrirPanneau}>
          Personnaliser
        </button>
        <button type="button" className="bouton bouton-secondaire" onClick={() => enregistrer({ preferences: true, audience: true })}>
          Tout accepter
        </button>
      </div>
    </section>
  )
}

const CATEGORIES: { cle: keyof Choix; titre: string; description: string }[] = [
  {
    cle: 'preferences',
    titre: 'Préférences',
    description: 'Mémorise votre thème (clair, sombre) et vos derniers filtres de recherche pendant 6 mois.',
  },
  {
    cle: 'audience',
    titre: "Mesure d'audience",
    description:
      'Compte les pages vues à l’aide d’un identifiant aléatoire (13 mois). Aucune adresse IP n’est conservée, aucune donnée n’est partagée.',
  },
]

// Panneau de personnalisation (boîte de dialogue modale native)
export function PanneauCookies() {
  const { choix, panneauOuvert, fermerPanneau, enregistrer } = useConsentement()
  const dialogue = useRef<HTMLDialogElement>(null)
  const [brouillon, setBrouillon] = useState<Choix>({ preferences: false, audience: false })

  useEffect(() => {
    const d = dialogue.current
    if (!d) return
    if (panneauOuvert && !d.open) {
      setBrouillon(choix ?? { preferences: false, audience: false })
      d.showModal()
    } else if (!panneauOuvert && d.open) {
      d.close()
    }
  }, [panneauOuvert, choix])

  return (
    <dialog
      ref={dialogue}
      className="panneau-cookies"
      aria-labelledby="panneau-cookies-titre"
      onClose={fermerPanneau}
    >
      <div className="panneau-cookies-entete">
        <h2 id="panneau-cookies-titre">Gérer mes cookies</h2>
        <button type="button" className="bouton-icone" onClick={fermerPanneau} aria-label="Fermer">
          <X size={20} aria-hidden="true" />
        </button>
      </div>

      <ul className="categories-cookies">
        <li>
          <div>
            <p className="categorie-titre">Nécessaires</p>
            <p className="aide">
              Indispensables : sécurité des formulaires, mémorisation de vos choix de cookies, connexion à
              l'espace administration. Ils ne peuvent pas être désactivés.
            </p>
          </div>
          <span className="badge badge-neutre">Toujours actifs</span>
        </li>
        {CATEGORIES.map((c) => (
          <li key={c.cle}>
            <div>
              <label className="categorie-titre" htmlFor={`cookie-${c.cle}`}>
                {c.titre}
              </label>
              <p className="aide" id={`cookie-${c.cle}-aide`}>{c.description}</p>
            </div>
            <input
              id={`cookie-${c.cle}`}
              type="checkbox"
              role="switch"
              className="interrupteur"
              aria-describedby={`cookie-${c.cle}-aide`}
              checked={brouillon[c.cle]}
              onChange={(e) => setBrouillon((b) => ({ ...b, [c.cle]: e.target.checked }))}
            />
          </li>
        ))}
      </ul>

      <div className="panneau-cookies-actions">
        <button type="button" className="bouton bouton-secondaire" onClick={() => enregistrer({ preferences: false, audience: false })}>
          Tout refuser
        </button>
        <button type="button" className="bouton bouton-secondaire" onClick={() => enregistrer({ preferences: true, audience: true })}>
          Tout accepter
        </button>
        <button type="button" className="bouton bouton-primaire" onClick={() => enregistrer(brouillon)}>
          Enregistrer mes choix
        </button>
      </div>
    </dialog>
  )
}
