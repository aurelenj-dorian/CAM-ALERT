import { ArrowRight, Megaphone, Scale, UserSearch } from 'lucide-react'
import { Link } from 'react-router'
import { CarteDossier } from '../components/Dossier'
import { Chargement, Erreur, Vide } from '../components/Etats'
import type { DossierResume, PageResultat } from '../lib/types'
import { useApi, useTitre } from '../lib/useApi'

const ACCES = [
  {
    vers: '/dossiers?type=1',
    titre: 'Personnes disparues',
    texte: 'Consultez les avis de disparition signalés sur l’ensemble du territoire camerounais.',
    Icone: UserSearch,
    classe: 'acces-disparition',
  },
  {
    vers: '/dossiers?type=2',
    titre: 'Personnes recherchées',
    texte: 'Accédez aux avis de recherche judiciaire émis par les autorités compétentes.',
    Icone: Scale,
    classe: 'acces-recherche',
  },
  {
    vers: '/signaler',
    titre: 'Transmettre une information',
    texte: 'Vous disposez d’un renseignement ? Transmettez-le de façon confidentielle.',
    Icone: Megaphone,
    classe: 'acces-signaler',
  },
]

export default function Accueil() {
  useTitre('')
  const { donnees, chargement, erreur, recharger } = useApi<PageResultat<DossierResume>>('/api/dossiers?page=1')
  const derniers = donnees?.elements.slice(0, 6) ?? []

  return (
    <>
      <section className="hero">
        <div className="conteneur hero-interne">
          <h1>Ensemble, retrouvons les personnes disparues et aidons la justice</h1>
          <p className="hero-texte">
            CAM-ALERT centralise les avis officiels de disparition et de recherche judiciaire au Cameroun, et
            permet à chaque citoyen de transmettre une information utile en toute confidentialité.
          </p>
          <div className="hero-actions">
            <Link to="/dossiers" className="bouton bouton-primaire">
              Consulter les dossiers <ArrowRight size={18} aria-hidden="true" />
            </Link>
            <Link to="/signaler" className="bouton bouton-secondaire">
              Transmettre une information
            </Link>
          </div>
        </div>
      </section>

      <section className="conteneur section" aria-labelledby="acces-titre">
        <h2 id="acces-titre" className="visuellement-cache">Accès rapides</h2>
        <ul className="grille-acces">
          {ACCES.map(({ vers, titre, texte, Icone, classe }) => (
            <li key={vers} className={`acces ${classe}`}>
              <span className="acces-icone" aria-hidden="true"><Icone size={20} strokeWidth={1.75} /></span>
              <h3><Link to={vers} className="lien-etendu">{titre}</Link></h3>
              <p>{texte}</p>
            </li>
          ))}
        </ul>
      </section>

      <section className="conteneur section" aria-labelledby="derniers-titre">
        <div className="section-entete">
          <div>
            <h2 id="derniers-titre">Derniers dossiers publiés</h2>
            <p className="texte-secondaire">Avis récents de disparition et de recherche judiciaire</p>
          </div>
          <Link to="/dossiers" className="lien-fleche">
            Voir tous les dossiers <ArrowRight size={16} aria-hidden="true" />
          </Link>
        </div>
        {chargement && <Chargement />}
        {erreur && <Erreur erreur={erreur} onReessayer={recharger} />}
        {!chargement && !erreur && derniers.length === 0 && <Vide titre="Aucun dossier publié pour le moment" />}
        {derniers.length > 0 && (
          <ul className="grille-dossiers">
            {derniers.map((d) => (
              <li key={d.id}><CarteDossier dossier={d} /></li>
            ))}
          </ul>
        )}
      </section>
    </>
  )
}
