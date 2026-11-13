import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { post } from './api'
import { ecrireCookie, lireCookieJson, supprimerCookie } from './cookies'

// Cookie de consentement (catégories Préférences et Mesure d'audience ; les nécessaires sont toujours actifs)
export const COOKIE_CONSENTEMENT = 'camalert_consent'
const VERSION = 1
const DUREE_JOURS = 182 // 6 mois

export interface Choix {
  preferences: boolean
  audience: boolean
}

interface CookieConsentement extends Choix {
  v: number
  date: string
}

interface ContexteConsentement {
  choix: Choix | null            // null : pas encore de choix valide → bandeau affiché
  panneauOuvert: boolean
  ouvrirPanneau: () => void
  fermerPanneau: () => void
  enregistrer: (choix: Choix) => void
}

const Contexte = createContext<ContexteConsentement | null>(null)

function lireChoix(): Choix | null {
  const c = lireCookieJson<CookieConsentement>(COOKIE_CONSENTEMENT)
  // Cookie illisible ou d'une ancienne version : le bandeau réapparaît
  if (!c || c.v !== VERSION || typeof c.preferences !== 'boolean' || typeof c.audience !== 'boolean')
    return null
  return { preferences: c.preferences, audience: c.audience }
}

export function FournisseurConsentement({ children }: { children: ReactNode }) {
  const [choix, setChoix] = useState<Choix | null>(lireChoix)
  const [panneauOuvert, setPanneauOuvert] = useState(false)

  const enregistrer = useCallback((nouveau: Choix) => {
    const valeur: CookieConsentement = { v: VERSION, date: new Date().toISOString(), ...nouveau }
    ecrireCookie(COOKIE_CONSENTEMENT, JSON.stringify(valeur), DUREE_JOURS)

    // Retrait d'une catégorie : suppression immédiate des cookies concernés
    if (!nouveau.preferences) supprimerCookie('camalert_prefs')
    if (!nouveau.audience) post('/api/audience/retrait').catch(() => {})

    setChoix(nouveau)
    setPanneauOuvert(false)
  }, [])

  const valeur = useMemo<ContexteConsentement>(
    () => ({
      choix,
      panneauOuvert,
      ouvrirPanneau: () => setPanneauOuvert(true),
      fermerPanneau: () => setPanneauOuvert(false),
      enregistrer,
    }),
    [choix, panneauOuvert, enregistrer],
  )

  return <Contexte.Provider value={valeur}>{children}</Contexte.Provider>
}

export function useConsentement() {
  const c = useContext(Contexte)
  if (!c) throw new Error('useConsentement doit être utilisé dans FournisseurConsentement')
  return c
}
