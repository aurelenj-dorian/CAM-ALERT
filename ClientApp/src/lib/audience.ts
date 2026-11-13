import { useEffect } from 'react'
import { useLocation } from 'react-router'
import { post } from './api'
import { useConsentement } from './consentement'

// Envoie une page vue à chaque changement de page, uniquement si la mesure d'audience est acceptée
export function useMesureAudience() {
  const { pathname } = useLocation()
  const { choix } = useConsentement()
  const autorise = choix?.audience === true

  useEffect(() => {
    if (!autorise) return
    post('/api/audience', { chemin: pathname }).catch(() => {
      /* la mesure ne doit jamais gêner la navigation */
    })
  }, [pathname, autorise])
}
