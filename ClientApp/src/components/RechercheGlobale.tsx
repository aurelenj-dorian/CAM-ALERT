import { Search } from 'lucide-react'
import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import { useNavigate } from 'react-router'
import { getJson } from '../lib/api'
import type { DossierResume } from '../lib/types'

// Champ de recherche avec suggestions (motif « combobox » accessible)
export function RechercheGlobale({ onNavigation }: { onNavigation?: () => void }) {
  const navigate = useNavigate()
  const id = useId()
  const [terme, setTerme] = useState('')
  const [suggestions, setSuggestions] = useState<DossierResume[]>([])
  const [ouvert, setOuvert] = useState(false)
  const [actif, setActif] = useState(-1)
  const conteneur = useRef<HTMLFormElement>(null)

  // Suggestions après 250 ms sans frappe, à partir de 2 caractères
  useEffect(() => {
    const t = terme.trim()
    if (t.length < 2) {
      setSuggestions([])
      return
    }
    const controleur = new AbortController()
    const minuteur = setTimeout(() => {
      getJson<DossierResume[]>(`/api/dossiers/suggestions?q=${encodeURIComponent(t)}`, controleur.signal)
        .then((s) => {
          setSuggestions(s)
          setActif(-1)
          setOuvert(true)
        })
        .catch(() => setSuggestions([]))
    }, 250)
    return () => {
      clearTimeout(minuteur)
      controleur.abort()
    }
  }, [terme])

  // Fermeture au clic en dehors
  useEffect(() => {
    const fermer = (e: MouseEvent) => {
      if (!conteneur.current?.contains(e.target as Node)) setOuvert(false)
    }
    document.addEventListener('mousedown', fermer)
    return () => document.removeEventListener('mousedown', fermer)
  }, [])

  const aller = (chemin: string) => {
    setOuvert(false)
    setTerme('')
    onNavigation?.()
    navigate(chemin)
  }

  const surTouche = (e: KeyboardEvent<HTMLInputElement>) => {
    if (!ouvert || suggestions.length === 0) return
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActif((i) => (i + 1) % suggestions.length)
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActif((i) => (i <= 0 ? suggestions.length - 1 : i - 1))
    } else if (e.key === 'Enter' && actif >= 0) {
      e.preventDefault()
      aller(`/dossiers/${suggestions[actif].id}`)
    } else if (e.key === 'Escape') {
      setOuvert(false)
    }
  }

  const listeVisible = ouvert && terme.trim().length >= 2

  return (
    <form
      ref={conteneur}
      className="recherche-globale"
      role="search"
      onSubmit={(e) => {
        e.preventDefault()
        const t = terme.trim()
        if (t) aller(`/dossiers?q=${encodeURIComponent(t)}`)
      }}
    >
      <label htmlFor={`${id}-champ`} className="visuellement-cache">
        Rechercher un dossier
      </label>
      <Search className="recherche-icone" size={18} aria-hidden="true" />
      <input
        id={`${id}-champ`}
        type="search"
        placeholder="Nom, ville, région…"
        autoComplete="off"
        role="combobox"
        aria-expanded={listeVisible}
        aria-controls={`${id}-liste`}
        aria-autocomplete="list"
        aria-activedescendant={actif >= 0 ? `${id}-option-${actif}` : undefined}
        value={terme}
        onChange={(e) => setTerme(e.target.value)}
        onFocus={() => suggestions.length && setOuvert(true)}
        onKeyDown={surTouche}
      />
      <ul id={`${id}-liste`} role="listbox" className="suggestions" hidden={!listeVisible} aria-label="Suggestions">
        {suggestions.length === 0 ? (
          <li className="suggestion-vide" role="presentation">Aucun dossier trouvé</li>
        ) : (
          suggestions.map((s, i) => (
            <li
              key={s.id}
              id={`${id}-option-${i}`}
              role="option"
              aria-selected={i === actif}
              className="suggestion"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => aller(`/dossiers/${s.id}`)}
            >
              <span className="suggestion-titre">{s.titre}</span>
              <span className="suggestion-detail">
                {s.type} · {[s.ville, s.region].filter(Boolean).join(', ')}
              </span>
            </li>
          ))
        )}
      </ul>
    </form>
  )
}
