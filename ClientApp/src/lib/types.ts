// Types des réponses de l'API (/api)

export interface DossierResume {
  id: number
  titre: string
  annee: number | null
  ville: string | null
  typeId: number
  type: string
  region: string
  imageUrl: string | null
  reference: string
}

export interface DossierDetail extends DossierResume {
  description: string | null
  signesDistinctifs: string | null
  datePublication: string | null
  statut: string | null
}

export interface PageResultat<T> {
  elements: T[]
  total: number
  page: number
  taillePage: number
  nombrePages: number
}

export interface Option {
  id: number
  libelle: string
}

export interface Referentiels {
  types: Option[]
  regions: Option[]
  annees: number[]
  dossiers: Option[]
}

export interface DossierHistorique {
  id: number
  titre: string
  type: string
  region: string
  annee: number | null
  statut: string | null
  dateResolution: string | null
  commentaireResolution: string | null
}

export interface SignalementPublic {
  id: number
  dossier: string
  statut: string
  date: string | null
}

export interface Historique {
  nbResolus: number
  nbEnCours: number
  nbSignalementsTraites: number
  resolus: DossierHistorique[]
  signalementsTraites: SignalementPublic[]
  tous: DossierHistorique[]
}

// Type 1 = Disparition, 2 = Recherche judiciaire (table TypesDossier)
export const TYPE_DISPARITION = 1
