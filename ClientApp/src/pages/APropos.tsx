import { Filter, Lock, Megaphone, Search, TriangleAlert } from 'lucide-react'
import { Link } from 'react-router'
import { useTitre } from '../lib/useApi'

const FONCTIONS = [
  { Icone: Search, titre: 'Consultation publique', texte: 'Accès libre aux avis de disparition et de recherche judiciaire pour tout citoyen.' },
  { Icone: Filter, titre: 'Recherche et filtrage', texte: 'Filtrage des dossiers par type, année et mots-clés pour des recherches ciblées.' },
  { Icone: Megaphone, titre: 'Remontée d’informations', texte: 'Formulaire sécurisé permettant aux citoyens de transmettre des informations utiles.' },
  { Icone: Lock, titre: 'Gestion sécurisée', texte: 'Espace d’administration réservé aux agents habilités pour gérer dossiers et signalements.' },
]

const ETAPES = [
  { titre: 'Les autorités publient un dossier', texte: 'Un agent habilité crée un dossier (disparition ou recherche judiciaire) avec photo, description et signes distinctifs.' },
  { titre: 'Les citoyens consultent les avis', texte: 'Tout citoyen peut librement consulter, filtrer et rechercher les dossiers publiés sur le portail.' },
  { titre: 'Les citoyens transmettent des informations', texte: 'Via le formulaire de signalement, chacun peut transmettre une information utile de façon confidentielle.' },
  { titre: 'Les agents traitent les signalements', texte: 'Les agents analysent les informations reçues, mettent à jour les dossiers et clôturent les cas résolus.' },
]

export default function APropos() {
  useTitre('À propos')
  return (
    <div className="conteneur page page-etroite prose">
      <header className="page-entete">
        <h1>À propos de CAM-ALERT</h1>
        <p className="texte-secondaire">Portail national camerounais de signalement — informations et contexte</p>
      </header>

      <section>
        <h2>Contexte</h2>
        <p>
          Au Cameroun, comme dans de nombreux pays, les autorités de sécurité et d’administration territoriale
          diffusent régulièrement des avis officiels concernant des personnes disparues, des personnes recherchées
          par la justice, ainsi que des appels à la collaboration citoyenne.
        </p>
        <p>
          Ces informations sont dispersées sur différents canaux (affichages physiques, bulletins officiels, médias
          locaux). Une plateforme centralisée, accessible et sécurisée améliore l’efficacité des recherches et
          renforce la collaboration citoyenne.
        </p>
      </section>

      <section>
        <h2>Mission du portail</h2>
        <p>
          CAM-ALERT permet aux autorités de diffuser et gérer efficacement les avis officiels concernant les
          personnes disparues ou recherchées par la justice.
        </p>
        <ul className="grille-fonctions">
          {FONCTIONS.map(({ Icone, titre, texte }) => (
            <li key={titre} className="carte">
              <Icone size={22} aria-hidden="true" className="icone-primaire" />
              <h3>{titre}</h3>
              <p>{texte}</p>
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2>Comment ça fonctionne ?</h2>
        <ol className="etapes">
          {ETAPES.map((e) => (
            <li key={e.titre}>
              <h3>{e.titre}</h3>
              <p>{e.texte}</p>
            </li>
          ))}
        </ol>
      </section>

      <div className="alerte alerte-avertissement">
        <TriangleAlert size={20} aria-hidden="true" />
        <p>
          <strong>Avertissement :</strong> ce portail est entièrement fictif et réalisé dans un cadre pédagogique.
          Aucune donnée présentée n’est réelle. <Link to="/contact">Nous contacter</Link>
        </p>
      </div>
    </div>
  )
}
