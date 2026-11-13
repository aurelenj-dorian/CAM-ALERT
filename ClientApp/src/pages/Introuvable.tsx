import { SearchX } from 'lucide-react'
import { Link } from 'react-router'
import { useTitre } from '../lib/useApi'

export default function Introuvable({
  titre = 'Page introuvable',
  texte = 'La page demandée n’existe pas ou a été déplacée.',
}: {
  titre?: string
  texte?: string
}) {
  useTitre(titre)
  return (
    <div className="conteneur page page-etroite introuvable">
      <SearchX size={48} aria-hidden="true" />
      <h1>{titre}</h1>
      <p className="texte-secondaire">{texte}</p>
      <div className="actions-formulaire">
        <Link to="/" className="bouton bouton-secondaire">Accueil</Link>
        <Link to="/dossiers" className="bouton bouton-primaire">Voir les dossiers</Link>
      </div>
    </div>
  )
}
