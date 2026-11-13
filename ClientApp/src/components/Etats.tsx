import { LoaderCircle, RefreshCw, SearchX, TriangleAlert } from 'lucide-react'
import type { ReactNode } from 'react'
import type { ErreurApi } from '../lib/api'

export function Chargement({ texte = 'Chargement…' }: { texte?: string }) {
  return (
    <div className="etat" role="status" aria-live="polite">
      <LoaderCircle className="tourne" size={28} aria-hidden="true" />
      <p>{texte}</p>
    </div>
  )
}

export function Erreur({ erreur, onReessayer }: { erreur: ErreurApi | Error; onReessayer?: () => void }) {
  return (
    <div className="etat etat-erreur" role="alert">
      <TriangleAlert size={28} aria-hidden="true" />
      <p>{erreur.message}</p>
      {onReessayer && (
        <button type="button" className="bouton bouton-secondaire" onClick={onReessayer}>
          <RefreshCw size={18} aria-hidden="true" /> Réessayer
        </button>
      )}
    </div>
  )
}

export function Vide({ titre, children }: { titre: string; children?: ReactNode }) {
  return (
    <div className="etat">
      <SearchX size={28} aria-hidden="true" />
      <p className="etat-titre">{titre}</p>
      {children}
    </div>
  )
}
