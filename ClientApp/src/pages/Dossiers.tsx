import { ChevronLeft, ChevronRight, Search } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { CarteDossier } from '../components/Dossier'
import { Chargement, Erreur, Vide } from '../components/Etats'
import { usePreferences } from '../lib/preferences'
import type { DossierResume, PageResultat, Referentiels } from '../lib/types'
import { useApi, useTitre } from '../lib/useApi'

export default function Dossiers() {
  useTitre('Dossiers')
  const [params, setParams] = useSearchParams()
  const { filtres: filtresMemorises, memoriserFiltres } = usePreferences()
  const referentiels = useApi<Referentiels>('/api/referentiels')

  const q = params.get('q') ?? ''
  const type = params.get('type') ?? ''
  const annee = params.get('annee') ?? ''
  const page = Math.max(1, Number.parseInt(params.get('page') ?? '1', 10) || 1)

  // Au premier affichage sans filtre dans l'URL : derniers filtres mémorisés (si préférences acceptées)
  const restaure = useRef(false)
  useEffect(() => {
    if (restaure.current) return
    restaure.current = true
    if (!params.toString() && (filtresMemorises.q || filtresMemorises.type || filtresMemorises.annee)) {
      const p = new URLSearchParams()
      if (filtresMemorises.q) p.set('q', filtresMemorises.q)
      if (filtresMemorises.type) p.set('type', filtresMemorises.type)
      if (filtresMemorises.annee) p.set('annee', filtresMemorises.annee)
      setParams(p, { replace: true })
    }
  }, [params, filtresMemorises, setParams])

  useEffect(() => memoriserFiltres({ q, type, annee }), [q, type, annee, memoriserFiltres])

  // Champ de recherche : URL mise à jour 400 ms après la dernière frappe
  const [saisie, setSaisie] = useState(q)
  useEffect(() => setSaisie(q), [q])
  useEffect(() => {
    if (saisie.trim() === q) return
    const t = setTimeout(() => majFiltre('q', saisie.trim()), 400)
    return () => clearTimeout(t)
  }, [saisie])

  function majFiltre(cle: string, valeur: string) {
    setParams((p) => {
      const n = new URLSearchParams(p)
      if (valeur) n.set(cle, valeur)
      else n.delete(cle)
      n.delete('page')
      return n
    })
  }

  function allerPage(numero: number) {
    setParams((p) => {
      const n = new URLSearchParams(p)
      if (numero > 1) n.set('page', String(numero))
      else n.delete('page')
      return n
    })
    document.getElementById('resultats')?.focus()
  }

  const requete = new URLSearchParams()
  if (q) requete.set('q', q)
  if (type) requete.set('type', type)
  if (annee) requete.set('annee', annee)
  requete.set('page', String(page))
  const resultats = useApi<PageResultat<DossierResume>>(`/api/dossiers?${requete}`)
  const donnees = resultats.donnees
  const filtreActif = Boolean(q || type || annee)

  return (
    <div className="conteneur page">
      <header className="page-entete">
        <h1>Dossiers</h1>
        <p className="texte-secondaire">Avis de disparition et de recherche judiciaire publiés par les autorités</p>
      </header>

      <form className="filtres" role="search" onSubmit={(e) => { e.preventDefault(); majFiltre('q', saisie.trim()) }}>
        <div className="champ filtre-recherche">
          <label htmlFor="filtre-q">Rechercher</label>
          <div className="champ-icone">
            <Search size={18} aria-hidden="true" />
            <input id="filtre-q" type="search" placeholder="Nom, ville ou région" value={saisie}
              onChange={(e) => setSaisie(e.target.value)} />
          </div>
        </div>
        <div className="champ">
          <label htmlFor="filtre-type">Type</label>
          <select id="filtre-type" value={type} onChange={(e) => majFiltre('type', e.target.value)}>
            <option value="">Tous les types</option>
            {referentiels.donnees?.types.map((t) => (
              <option key={t.id} value={t.id}>{t.libelle}</option>
            ))}
          </select>
        </div>
        <div className="champ">
          <label htmlFor="filtre-annee">Année</label>
          <select id="filtre-annee" value={annee} onChange={(e) => majFiltre('annee', e.target.value)}>
            <option value="">Toutes les années</option>
            {referentiels.donnees?.annees.map((a) => (
              <option key={a} value={a}>{a}</option>
            ))}
          </select>
        </div>
      </form>

      <section id="resultats" tabIndex={-1} aria-labelledby="resultats-titre" className="resultats">
        <h2 id="resultats-titre" className="resultats-compte" aria-live="polite">
          {donnees ? `${donnees.total} dossier${donnees.total > 1 ? 's' : ''}` : 'Résultats'}
        </h2>

        {resultats.chargement && !donnees && <Chargement />}
        {resultats.erreur && <Erreur erreur={resultats.erreur} onReessayer={resultats.recharger} />}

        {donnees && !resultats.erreur && (
          donnees.elements.length === 0 ? (
            <Vide titre="Aucun dossier ne correspond à votre recherche.">
              {filtreActif && (
                <Link to="/dossiers" className="bouton bouton-secondaire" onClick={() => setSaisie('')}>
                  Effacer les filtres
                </Link>
              )}
            </Vide>
          ) : (
            <>
              <ul className={`grille-dossiers ${resultats.chargement ? 'en-chargement' : ''}`} aria-busy={resultats.chargement}>
                {donnees.elements.map((d) => (
                  <li key={d.id}><CarteDossier dossier={d} /></li>
                ))}
              </ul>
              {donnees.nombrePages > 1 && (
                <nav className="pagination" aria-label="Pagination">
                  <button type="button" className="bouton bouton-secondaire" disabled={page <= 1}
                    onClick={() => allerPage(page - 1)}>
                    <ChevronLeft size={18} aria-hidden="true" /> Précédent
                  </button>
                  <span aria-current="page">Page {donnees.page} sur {donnees.nombrePages}</span>
                  <button type="button" className="bouton bouton-secondaire" disabled={page >= donnees.nombrePages}
                    onClick={() => allerPage(page + 1)}>
                    Suivant <ChevronRight size={18} aria-hidden="true" />
                  </button>
                </nav>
              )}
            </>
          )
        )}
      </section>
    </div>
  )
}
