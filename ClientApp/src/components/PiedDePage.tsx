import { TriangleAlert } from 'lucide-react'
import { Link } from 'react-router'
import { useConsentement } from '../lib/consentement'

export function PiedDePage() {
  const { ouvrirPanneau } = useConsentement()
  return (
    <footer className="pied">
      <div className="conteneur pied-interne">
        <p className="pied-avertissement">
          <TriangleAlert size={18} aria-hidden="true" />
          <span>Portail entièrement fictif, réalisé dans un cadre pédagogique. Aucune donnée présentée n'est réelle.</span>
        </p>
        <nav aria-label="Liens de pied de page">
          <ul className="pied-liens">
            <li><Link to="/contact">Contact</Link></li>
            <li><Link to="/a-propos">À propos</Link></li>
            <li><Link to="/cookies">Politique de cookies</Link></li>
            <li>
              <button type="button" className="lien-bouton" onClick={ouvrirPanneau}>
                Gérer mes cookies
              </button>
            </li>
            {/* Lien serveur (hors React) */}
            <li><a href="/admin/connexion">Login</a></li>
          </ul>
        </nav>
        <p className="pied-copyright">© {new Date().getFullYear()} CAM-ALERT</p>
      </div>
    </footer>
  )
}
