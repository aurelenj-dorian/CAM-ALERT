import { ArrowLeft, Calendar, MapPin, Megaphone } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { BadgeType, ImageDossier, Reference } from '../components/Dossier'
import { Chargement, Erreur } from '../components/Etats'
import type { DossierDetail as Detail } from '../lib/types'
import { useApi, useTitre } from '../lib/useApi'
import Introuvable from './Introuvable'

const formatDate = (iso: string | null) =>
  iso ? new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' }) : '—'

export default function DossierDetail() {
  const { id } = useParams()
  const valide = /^\d{1,9}$/.test(id ?? '')
  const { donnees: d, chargement, erreur, recharger } = useApi<Detail>(valide ? `/api/dossiers/${id}` : null)
  useTitre(d?.titre ?? 'Dossier')

  if (!valide || erreur?.statut === 404)
    return <Introuvable titre="Dossier introuvable" texte="Ce dossier n’existe pas ou n’est plus publié." />

  return (
    <div className="conteneur page">
      <Link to="/dossiers" className="lien-retour">
        <ArrowLeft size={16} aria-hidden="true" /> Retour aux dossiers
      </Link>

      {chargement && <Chargement />}
      {erreur && <Erreur erreur={erreur} onReessayer={recharger} />}

      {d && (
        <article className="fiche">
          <div className="fiche-photo">
            <ImageDossier url={d.imageUrl} titre={d.titre} typeId={d.typeId} />
          </div>

          <div className="fiche-contenu">
            <BadgeType typeId={d.typeId} libelle={d.type} />
            <h1>{d.titre}</h1>
            <p className="fiche-ref">Référence <Reference valeur={d.reference} /></p>

            <dl className="fiche-meta">
              <div><dt>Région</dt><dd>{d.region}</dd></div>
              <div><dt>Ville</dt><dd><MapPin size={16} aria-hidden="true" /> {d.ville || '—'}</dd></div>
              <div><dt>Année</dt><dd>{d.annee ?? '—'}</dd></div>
              <div><dt>Publié le</dt><dd><Calendar size={16} aria-hidden="true" /> {formatDate(d.datePublication)}</dd></div>
              {d.statut && <div><dt>Statut</dt><dd>{d.statut}</dd></div>}
            </dl>

            <section aria-labelledby="fiche-description">
              <h2 id="fiche-description">Description</h2>
              {/* Texte brut : React l'encode, les retours à la ligne sont conservés en CSS */}
              <p className="texte-libre">{d.description || 'Aucune description.'}</p>
            </section>

            <section aria-labelledby="fiche-signes">
              <h2 id="fiche-signes">Signes distinctifs</h2>
              <p className="texte-libre">{d.signesDistinctifs || 'Non renseignés.'}</p>
            </section>

            <div className="fiche-appel">
              <div>
                <p className="fiche-appel-titre">Vous avez une information ?</p>
                <p className="texte-secondaire">Elle sera transmise de façon confidentielle aux agents habilités.</p>
              </div>
              <Link to={`/signaler?dossier=${d.id}`} className="bouton bouton-primaire">
                <Megaphone size={18} aria-hidden="true" /> Transmettre une information
              </Link>
            </div>
          </div>
        </article>
      )}
    </div>
  )
}
