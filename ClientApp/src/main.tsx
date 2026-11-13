import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
// Polices auto-hébergées (aucun appel à un service tiers)
import '@fontsource-variable/public-sans/wght.css'
import '@fontsource/ibm-plex-mono/400.css'
import './styles/tokens.css'
import './styles/base.css'
import './styles/site.css'
import App from './App'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
