import { useConsentement } from '../lib/consentement'
import { useTitre } from '../lib/useApi'

const COOKIES = [
  { nom: 'camalert_consent', categorie: 'Nécessaire', finalite: 'Mémorise vos choix sur les cookies.', duree: '6 mois' },
  { nom: 'XSRF-TOKEN, camalert_csrf', categorie: 'Nécessaire', finalite: 'Protège les formulaires contre les envois frauduleux (CSRF).', duree: 'Session' },
  { nom: 'camalert_admin', categorie: 'Nécessaire', finalite: 'Maintient la connexion des agents à l’espace administration (uniquement sous /admin).', duree: 'Session, 8 h d’inactivité max.' },
  { nom: 'camalert_flash', categorie: 'Nécessaire', finalite: 'Affiche les messages de confirmation de l’espace administration.', duree: 'Session' },
  { nom: 'camalert_prefs', categorie: 'Préférences', finalite: 'Mémorise le thème d’affichage et vos derniers filtres de recherche.', duree: '6 mois' },
  { nom: 'camalert_vid', categorie: 'Mesure d’audience', finalite: 'Identifiant aléatoire pour compter les pages vues et les visiteurs uniques. Aucune adresse IP n’est conservée.', duree: '13 mois' },
]

export default function PolitiqueCookies() {
  useTitre('Politique de cookies')
  const { choix, ouvrirPanneau } = useConsentement()

  return (
    <div className="conteneur page page-etroite prose">
      <header className="page-entete">
        <h1>Politique de cookies</h1>
        <p className="texte-secondaire">Quels cookies nous utilisons, pourquoi, et comment changer d’avis</p>
      </header>

      <section>
        <h2>Vos choix actuels</h2>
        {choix ? (
          <ul>
            <li>Préférences : <strong>{choix.preferences ? 'acceptées' : 'refusées'}</strong></li>
            <li>Mesure d’audience : <strong>{choix.audience ? 'acceptée' : 'refusée'}</strong></li>
          </ul>
        ) : (
          <p>Vous n’avez pas encore fait de choix.</p>
        )}
        <button type="button" className="bouton bouton-primaire" onClick={ouvrirPanneau}>
          Modifier mes choix
        </button>
      </section>

      <section>
        <h2>Liste des cookies</h2>
        <p>
          Les cookies nécessaires sont déposés sans consentement car le site ne peut pas fonctionner sans eux. Les
          autres ne le sont qu’avec votre accord, que vous pouvez retirer à tout moment : les cookies concernés sont
          alors supprimés.
        </p>
        <div className="tableau-defilant">
          <table className="tableau">
            <thead>
              <tr><th scope="col">Cookie</th><th scope="col">Catégorie</th><th scope="col">Finalité</th><th scope="col">Durée</th></tr>
            </thead>
            <tbody>
              {COOKIES.map((c) => (
                <tr key={c.nom}>
                  <td className="mono">{c.nom}</td>
                  <td>{c.categorie}</td>
                  <td>{c.finalite}</td>
                  <td>{c.duree}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section>
        <h2>Aucun traceur tiers</h2>
        <p>
          Le site n’utilise aucun service tiers de publicité ou de statistiques : polices, scripts et mesure
          d’audience sont hébergés par CAM-ALERT. Seules les tuiles de carte OpenStreetMap sont chargées depuis un
          serveur externe, dans l’espace administration.
        </p>
      </section>
    </div>
  )
}
