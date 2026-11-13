import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { ecrireCookie, lireCookieJson } from './cookies'
import { useConsentement } from './consentement'

// Cookie de préférences : thème et derniers filtres de la page Dossiers.
// Sans consentement « Préférences », le thème ne vaut que pour l'onglet en cours (sessionStorage).
const COOKIE_PREFS = 'camalert_prefs'
const CLE_ONGLET = 'camalert_theme'
const DUREE_JOURS = 182

export type Theme = 'clair' | 'sombre' | 'systeme'

export interface Filtres {
  q?: string
  type?: string
  annee?: string
}

interface Prefs {
  theme: Theme
  filtres: Filtres
}

interface ContextePreferences extends Prefs {
  changerTheme: (theme: Theme) => void
  memoriserFiltres: (filtres: Filtres) => void
}

const Contexte = createContext<ContextePreferences | null>(null)

function themeOnglet(): Theme | null {
  try {
    const t = sessionStorage.getItem(CLE_ONGLET)
    return t === 'clair' || t === 'sombre' || t === 'systeme' ? t : null
  } catch {
    return null
  }
}

function appliquerTheme(theme: Theme) {
  if (theme === 'systeme') document.documentElement.removeAttribute('data-theme')
  else document.documentElement.setAttribute('data-theme', theme)
}

export function FournisseurPreferences({ children }: { children: ReactNode }) {
  const { choix } = useConsentement()
  const autorise = choix?.preferences === true

  const [prefs, setPrefs] = useState<Prefs>(() => {
    const cookie = lireCookieJson<Partial<Prefs>>(COOKIE_PREFS)
    return {
      theme: cookie?.theme ?? themeOnglet() ?? 'systeme',
      filtres: cookie?.filtres ?? {},
    }
  })

  // Persistance dans le cookie uniquement si les préférences sont acceptées
  useEffect(() => {
    if (autorise) ecrireCookie(COOKIE_PREFS, JSON.stringify(prefs), DUREE_JOURS)
  }, [prefs, autorise])

  useEffect(() => appliquerTheme(prefs.theme), [prefs.theme])

  const changerTheme = useCallback((theme: Theme) => {
    try {
      sessionStorage.setItem(CLE_ONGLET, theme)
    } catch {
      /* stockage indisponible */
    }
    setPrefs((p) => ({ ...p, theme }))
  }, [])

  const memoriserFiltres = useCallback((filtres: Filtres) => {
    setPrefs((p) =>
      JSON.stringify(p.filtres) === JSON.stringify(filtres) ? p : { ...p, filtres },
    )
  }, [])

  const valeur = useMemo(
    () => ({ ...prefs, filtres: autorise ? prefs.filtres : {}, changerTheme, memoriserFiltres }),
    [prefs, autorise, changerTheme, memoriserFiltres],
  )
  return <Contexte.Provider value={valeur}>{children}</Contexte.Provider>
}

export function usePreferences() {
  const c = useContext(Contexte)
  if (!c) throw new Error('usePreferences doit être utilisé dans FournisseurPreferences')
  return c
}
