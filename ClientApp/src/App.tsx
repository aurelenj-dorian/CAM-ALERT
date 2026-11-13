import { useEffect, useRef } from 'react'
import { BrowserRouter, Route, Routes, useLocation } from 'react-router'
import { BandeauCookies, PanneauCookies } from './components/Cookies'
import { Entete } from './components/Entete'
import { PiedDePage } from './components/PiedDePage'
import { useMesureAudience } from './lib/audience'
import { FournisseurConsentement } from './lib/consentement'
import { FournisseurPreferences } from './lib/preferences'
import Accueil from './pages/Accueil'
import APropos from './pages/APropos'
import Contact from './pages/Contact'
import DossierDetail from './pages/DossierDetail'
import Dossiers from './pages/Dossiers'
import Historique from './pages/Historique'
import Introuvable from './pages/Introuvable'
import PolitiqueCookies from './pages/PolitiqueCookies'
import Signaler from './pages/Signaler'

function MiseEnPage() {
  const { pathname } = useLocation()
  const contenu = useRef<HTMLElement>(null)
  const premierRendu = useRef(true)
  useMesureAudience()

  // Changement de page : retour en haut et focus sur le contenu (lecteurs d'écran)
  useEffect(() => {
    if (premierRendu.current) {
      premierRendu.current = false
      return
    }
    window.scrollTo({ top: 0 })
    contenu.current?.focus({ preventScroll: true })
  }, [pathname])

  return (
    <>
      <a className="lien-evitement" href="#contenu">Aller au contenu</a>
      <Entete />
      <main id="contenu" ref={contenu} tabIndex={-1}>
        <Routes>
          <Route path="/" element={<Accueil />} />
          <Route path="/dossiers" element={<Dossiers />} />
          <Route path="/dossiers/:id" element={<DossierDetail />} />
          <Route path="/signaler" element={<Signaler />} />
          <Route path="/historique" element={<Historique />} />
          <Route path="/contact" element={<Contact />} />
          <Route path="/a-propos" element={<APropos />} />
          <Route path="/cookies" element={<PolitiqueCookies />} />
          <Route path="*" element={<Introuvable />} />
        </Routes>
      </main>
      <PiedDePage />
      <BandeauCookies />
      <PanneauCookies />
    </>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <FournisseurConsentement>
        <FournisseurPreferences>
          <MiseEnPage />
        </FournisseurPreferences>
      </FournisseurConsentement>
    </BrowserRouter>
  )
}
