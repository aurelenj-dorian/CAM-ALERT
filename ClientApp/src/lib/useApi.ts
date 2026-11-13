import { useCallback, useEffect, useState } from 'react'
import { ErreurApi, getJson } from './api'

interface EtatApi<T> {
  donnees: T | null
  chargement: boolean
  erreur: ErreurApi | null
  recharger: () => void
}

// Chargement GET avec états chargement / erreur / réessai ; url = null pour ne rien charger
export function useApi<T>(url: string | null): EtatApi<T> {
  const [donnees, setDonnees] = useState<T | null>(null)
  const [chargement, setChargement] = useState(url !== null)
  const [erreur, setErreur] = useState<ErreurApi | null>(null)
  const [essai, setEssai] = useState(0)

  useEffect(() => {
    if (url === null) {
      setChargement(false)
      return
    }
    const controleur = new AbortController()
    setChargement(true)
    setErreur(null)
    getJson<T>(url, controleur.signal)
      .then((d) => {
        setDonnees(d)
        setChargement(false)
      })
      .catch((e: Error) => {
        if (e.name === 'AbortError') return
        setErreur(e instanceof ErreurApi ? e : new ErreurApi(0, e.message))
        setChargement(false)
      })
    return () => controleur.abort()
  }, [url, essai])

  const recharger = useCallback(() => setEssai((n) => n + 1), [])
  return { donnees, chargement, erreur, recharger }
}

// Titre de l'onglet
export function useTitre(titre: string) {
  useEffect(() => {
    document.title = titre ? `${titre} · CAM-ALERT` : 'CAM-ALERT'
  }, [titre])
}
