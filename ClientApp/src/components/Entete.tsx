import { LogIn, Menu, Monitor, Moon, Search, Sun, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { Link, NavLink, useLocation } from 'react-router'
import { usePreferences, type Theme } from '../lib/preferences'
import { RechercheGlobale } from './RechercheGlobale'

const LIENS = [
  { vers: '/', libelle: 'Accueil', fin: true },
  { vers: '/dossiers', libelle: 'Dossiers' },
  { vers: '/signaler', libelle: 'Signaler' },
  { vers: '/contact', libelle: 'Contact' },
]

// Liens secondaires : menu burger à l'extrême droite sur ordinateur, liens simples dans le menu mobile
const LIENS_SECONDAIRES = [
  { vers: '/historique', libelle: 'Historique' },
  { vers: '/a-propos', libelle: 'À propos' },
]

const classeLien = ({ isActive }: { isActive: boolean }) => (isActive ? 'actif' : '')

function MenuSecondaire() {
  const [ouvert, setOuvert] = useState(false)
  const { pathname } = useLocation()
  const conteneur = useRef<HTMLDivElement>(null)
  const bouton = useRef<HTMLButtonElement>(null)
  const actif = LIENS_SECONDAIRES.some((l) => pathname.startsWith(l.vers))

  useEffect(() => setOuvert(false), [pathname])

  // Fermeture au clic en dehors et avec Échap (le focus revient au bouton)
  useEffect(() => {
    if (!ouvert) return
    const clic = (e: MouseEvent) => {
      if (!conteneur.current?.contains(e.target as Node)) setOuvert(false)
    }
    const touche = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.stopPropagation()
        setOuvert(false)
        bouton.current?.focus()
      }
    }
    document.addEventListener('mousedown', clic)
    document.addEventListener('keydown', touche, true)
    return () => {
      document.removeEventListener('mousedown', clic)
      document.removeEventListener('keydown', touche, true)
    }
  }, [ouvert])

  return (
    <div ref={conteneur} className={`menu-secondaire ${ouvert ? 'ouvert' : ''}`}>
      <button
        ref={bouton}
        type="button"
        className={`bouton-burger ${actif ? 'actif' : ''}`}
        aria-expanded={ouvert}
        aria-controls="menu-secondaire"
        onClick={() => setOuvert((o) => !o)}
      >
        {ouvert ? <X size={20} aria-hidden="true" /> : <Menu size={20} aria-hidden="true" />}
        <span className="visuellement-cache">Plus de pages</span>
      </button>
      <ul id="menu-secondaire" className="sous-menu">
        {LIENS_SECONDAIRES.map((l) => (
          <li key={l.vers}>
            <NavLink to={l.vers} className={classeLien}>{l.libelle}</NavLink>
          </li>
        ))}
      </ul>
    </div>
  )
}

// Loupe seule sur ordinateur : un clic ouvre la barre de recherche juste en dessous.
// Sur mobile, la barre est affichée directement dans le menu.
function RechercheDepliable({ onNavigation }: { onNavigation: () => void }) {
  const [ouvert, setOuvert] = useState(false)
  const { pathname } = useLocation()
  const conteneur = useRef<HTMLDivElement>(null)
  const bouton = useRef<HTMLButtonElement>(null)

  useEffect(() => setOuvert(false), [pathname])

  // À l'ouverture, le focus va directement dans le champ
  useEffect(() => {
    if (ouvert) conteneur.current?.querySelector<HTMLInputElement>('.panneau-recherche input')?.focus()
  }, [ouvert])

  useEffect(() => {
    if (!ouvert) return
    const clic = (e: MouseEvent) => {
      if (!conteneur.current?.contains(e.target as Node)) setOuvert(false)
    }
    const touche = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.stopPropagation()
        setOuvert(false)
        bouton.current?.focus()
      }
    }
    document.addEventListener('mousedown', clic)
    document.addEventListener('keydown', touche, true)
    return () => {
      document.removeEventListener('mousedown', clic)
      document.removeEventListener('keydown', touche, true)
    }
  }, [ouvert])

  return (
    <div ref={conteneur} className={`recherche-depliable ${ouvert ? 'ouvert' : ''}`}>
      <button
        ref={bouton}
        type="button"
        className="bouton-loupe"
        aria-expanded={ouvert}
        aria-controls="panneau-recherche"
        onClick={() => setOuvert((o) => !o)}
      >
        {ouvert ? <X size={20} aria-hidden="true" /> : <Search size={20} aria-hidden="true" />}
        <span className="visuellement-cache">{ouvert ? 'Fermer la recherche' : 'Rechercher un dossier'}</span>
      </button>
      <div id="panneau-recherche" className="panneau-recherche">
        <RechercheGlobale onNavigation={() => { setOuvert(false); onNavigation() }} />
      </div>
    </div>
  )
}

const THEMES: { valeur: Theme; libelle: string; Icone: typeof Sun }[] = [
  { valeur: 'clair', libelle: 'Thème clair', Icone: Sun },
  { valeur: 'sombre', libelle: 'Thème sombre', Icone: Moon },
  { valeur: 'systeme', libelle: 'Thème du système', Icone: Monitor },
]

export function SelecteurTheme() {
  const { theme, changerTheme } = usePreferences()
  return (
    <div className="selecteur-theme" role="radiogroup" aria-label="Thème d'affichage">
      {THEMES.map(({ valeur, libelle, Icone }) => (
        <button
          key={valeur}
          type="button"
          role="radio"
          aria-checked={theme === valeur}
          aria-label={libelle}
          title={libelle}
          onClick={() => changerTheme(valeur)}
        >
          <Icone size={18} aria-hidden="true" />
        </button>
      ))}
    </div>
  )
}

export function Entete() {
  const [menuOuvert, setMenuOuvert] = useState(false)
  const { pathname } = useLocation()
  const boutonMenu = useRef<HTMLButtonElement>(null)

  // Menu mobile fermé à chaque changement de page
  useEffect(() => setMenuOuvert(false), [pathname])

  // Échap ferme le menu et rend le focus au bouton
  useEffect(() => {
    if (!menuOuvert) return
    const surTouche = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setMenuOuvert(false)
        boutonMenu.current?.focus()
      }
    }
    document.addEventListener('keydown', surTouche)
    return () => document.removeEventListener('keydown', surTouche)
  }, [menuOuvert])

  return (
    <header className="entete">
      <div className="entete-interne conteneur">
        <Link to="/" className="marque" aria-label="CAM-ALERT, accueil">
          <span className="marque-signal" aria-hidden="true" />
          CAM-ALERT
        </Link>

        <button
          ref={boutonMenu}
          type="button"
          className="bouton-menu"
          aria-expanded={menuOuvert}
          aria-controls="navigation-principale"
          onClick={() => setMenuOuvert((o) => !o)}
        >
          {menuOuvert ? <X size={22} aria-hidden="true" /> : <Menu size={22} aria-hidden="true" />}
          <span className="visuellement-cache">{menuOuvert ? 'Fermer le menu' : 'Ouvrir le menu'}</span>
        </button>

        <div id="navigation-principale" className={`entete-panneau ${menuOuvert ? 'ouvert' : ''}`}>
          <nav aria-label="Navigation principale">
            <ul className="nav-liens">
              {LIENS.map((l) => (
                <li key={l.vers}>
                  <NavLink to={l.vers} end={l.fin} className={classeLien}>
                    {l.libelle}
                  </NavLink>
                </li>
              ))}
              {/* Menu mobile uniquement : sur ordinateur, ces liens sont dans le burger de droite */}
              {LIENS_SECONDAIRES.map((l) => (
                <li key={l.vers} className="lien-mobile">
                  <NavLink to={l.vers} className={classeLien}>{l.libelle}</NavLink>
                </li>
              ))}
            </ul>
          </nav>
          <div className="entete-outils">
            <RechercheDepliable onNavigation={() => setMenuOuvert(false)} />
            <SelecteurTheme />
            {/* Lien serveur (hors React) vers l'espace administration */}
            <a href="/admin/connexion" className="bouton bouton-secondaire bouton-login">
              <LogIn size={18} aria-hidden="true" /> Login
            </a>
            <MenuSecondaire />
          </div>
        </div>
      </div>
    </header>
  )
}
