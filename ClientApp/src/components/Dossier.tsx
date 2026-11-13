import { MapPin, Scale, UserSearch } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { TYPE_DISPARITION, type DossierResume } from '../lib/types'

export function BadgeType({ typeId, libelle }: { typeId: number; libelle: string }) {
  const disparition = typeId === TYPE_DISPARITION
  const Icone = disparition ? UserSearch : Scale
  return (
    <span className={`badge ${disparition ? 'badge-disparition' : 'badge-recherche'}`}>
      <Icone size={14} aria-hidden="true" />
      {libelle}
    </span>
  )
}

// Signature : la référence du dossier, dans un cartouche de tampon
export function Reference({ valeur }: { valeur: string }) {
  return <span className="reference">{valeur}</span>
}

// « MBALLA Jean-Pierre » → « MJ »
function initiales(titre: string) {
  return titre
    .split(/[\s-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((mot) => mot[0].toUpperCase())
    .join('')
}

// Photo du dossier ; sans photo (ou photo introuvable), une fiche typographique
export function ImageDossier({ url, titre, typeId }: { url: string | null; titre: string; typeId: number }) {
  const [enErreur, setEnErreur] = useState(false)

  if (!url || enErreur)
    return (
      <div className={`sans-photo ${typeId === TYPE_DISPARITION ? 'sans-photo-disparition' : 'sans-photo-recherche'}`}>
        <span className="sans-photo-initiales" aria-hidden="true">{initiales(titre)}</span>
        <p className="sans-photo-mention">Photo non disponible</p>
      </div>
    )

  return (
    <img
      src={url}
      alt={`Photo de ${titre}`}
      width={300}
      height={375}
      loading="lazy"
      decoding="async"
      onError={() => setEnErreur(true)}
    />
  )
}

export function CarteDossier({ dossier }: { dossier: DossierResume }) {
  const lieu = [dossier.ville, dossier.region].filter(Boolean).join(', ')
  return (
    <article className="carte-dossier">
      <div className="carte-dossier-image">
        <ImageDossier url={dossier.imageUrl} titre={dossier.titre} typeId={dossier.typeId} />
      </div>
      <div className="carte-dossier-corps">
        <BadgeType typeId={dossier.typeId} libelle={dossier.type} />
        <h3>
          {/* Le lien couvre toute la carte (voir CSS) */}
          <Link to={`/dossiers/${dossier.id}`} className="lien-etendu">
            {dossier.titre}
          </Link>
        </h3>
        <p className="carte-dossier-meta">
          <MapPin size={16} aria-hidden="true" />
          <span>
            {lieu || 'Lieu non précisé'}
            {dossier.annee ? ` · ${dossier.annee}` : ''}
          </span>
        </p>
        <p className="carte-dossier-ref"><Reference valeur={dossier.reference} /></p>
      </div>
    </article>
  )
}
