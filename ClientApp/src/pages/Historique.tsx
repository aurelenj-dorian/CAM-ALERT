import { useRef, useState, type KeyboardEvent } from 'react'
import { Link } from 'react-router'
import { Chargement, Erreur } from '../components/Etats'
import type { Historique as Donnees } from '../lib/types'
import { useApi, useTitre } from '../lib/useApi'

const formatDate = (iso: string | null) => (iso ? new Date(iso).toLocaleDateString('fr-FR') : '—')

const ONGLETS = [
  { cle: 'resolus', libelle: 'Dossiers résolus' },
  { cle: 'signalements', libelle: 'Signalements traités' },
  { cle: 'tous', libelle: 'Tous les dossiers' },
] as const
type Onglet = (typeof ONGLETS)[number]['cle']

function classeStatut(statut: string | null) {
  if (statut === 'Résolu') return 'badge badge-succes'
  if (statut === 'Classé') return 'badge badge-neutre'
  return 'badge badge-disparition'
}

export default function Historique() {
  useTitre('Historique')
  const { donnees, chargement, erreur, recharger } = useApi<Donnees>('/api/historique')
  const [onglet, setOnglet] = useState<Onglet>('resolus')
  const boutons = useRef<(HTMLButtonElement | null)[]>([])

  // Onglets accessibles : flèches gauche/droite, Début/Fin
  function surTouche(e: KeyboardEvent, index: number) {
    let cible = -1
    if (e.key === 'ArrowRight') cible = (index + 1) % ONGLETS.length
    else if (e.key === 'ArrowLeft') cible = (index - 1 + ONGLETS.length) % ONGLETS.length
    else if (e.key === 'Home') cible = 0
    else if (e.key === 'End') cible = ONGLETS.length - 1
    if (cible < 0) return
    e.preventDefault()
    setOnglet(ONGLETS[cible].cle)
    boutons.current[cible]?.focus()
  }

  return (
    <div className="conteneur page">
      <header className="page-entete">
        <h1>Historique</h1>
        <p className="texte-secondaire">Bilan des dossiers résolus et des informations traitées par les agents</p>
      </header>

      {chargement && <Chargement />}
      {erreur && <Erreur erreur={erreur} onReessayer={recharger} />}

      {donnees && (
        <>
          <dl className="chiffres">
            <div className="chiffre chiffre-succes"><dt>Dossiers résolus</dt><dd>{donnees.nbResolus}</dd></div>
            <div className="chiffre"><dt>Dossiers en cours</dt><dd>{donnees.nbEnCours}</dd></div>
            <div className="chiffre"><dt>Signalements traités</dt><dd>{donnees.nbSignalementsTraites}</dd></div>
          </dl>

          <div className="onglets" role="tablist" aria-label="Historique">
            {ONGLETS.map((o, i) => (
              <button
                key={o.cle}
                ref={(el) => { boutons.current[i] = el }}
                type="button"
                role="tab"
                id={`onglet-${o.cle}`}
                aria-selected={onglet === o.cle}
                aria-controls={`panneau-${o.cle}`}
                tabIndex={onglet === o.cle ? 0 : -1}
                onClick={() => setOnglet(o.cle)}
                onKeyDown={(e) => surTouche(e, i)}
              >
                {o.libelle}
              </button>
            ))}
          </div>

          <div role="tabpanel" id={`panneau-${onglet}`} aria-labelledby={`onglet-${onglet}`} tabIndex={0} className="panneau-onglet">
            {onglet === 'resolus' && (
              donnees.resolus.length === 0 ? <p className="etat-vide">Aucun dossier résolu pour l’instant.</p> : (
                <div className="tableau-defilant">
                  <table className="tableau">
                    <thead><tr><th scope="col">Identité</th><th scope="col">Type</th><th scope="col">Région</th><th scope="col">Résolu le</th><th scope="col">Commentaire</th></tr></thead>
                    <tbody>
                      {donnees.resolus.map((d) => (
                        <tr key={d.id}>
                          <td><Link to={`/dossiers/${d.id}`}>{d.titre}</Link></td>
                          <td>{d.type}</td><td>{d.region}</td>
                          <td className="sans-retour">{formatDate(d.dateResolution)}</td>
                          <td>{d.commentaireResolution || '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )
            )}

            {onglet === 'signalements' && (
              donnees.signalementsTraites.length === 0 ? <p className="etat-vide">Aucun signalement traité pour l’instant.</p> : (
                <>
                  <p className="aide">Par respect pour la confidentialité des informateurs, ni leur identité ni leur message ne sont publiés.</p>
                  <div className="tableau-defilant">
                    <table className="tableau">
                      <thead><tr><th scope="col">Dossier concerné</th><th scope="col">Statut</th><th scope="col">Date</th></tr></thead>
                      <tbody>
                        {donnees.signalementsTraites.map((s) => (
                          <tr key={s.id}>
                            <td>{s.dossier}</td>
                            <td><span className="badge badge-succes">{s.statut}</span></td>
                            <td className="sans-retour">{formatDate(s.date)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </>
              )
            )}

            {onglet === 'tous' && (
              <div className="tableau-defilant">
                <table className="tableau">
                  <thead><tr><th scope="col">Identité</th><th scope="col">Type</th><th scope="col">Région</th><th scope="col">Année</th><th scope="col">Statut</th></tr></thead>
                  <tbody>
                    {donnees.tous.map((d) => (
                      <tr key={d.id}>
                        <td><Link to={`/dossiers/${d.id}`}>{d.titre}</Link></td>
                        <td>{d.type}</td><td>{d.region}</td><td>{d.annee ?? '—'}</td>
                        <td><span className={classeStatut(d.statut)}>{d.statut ?? 'En cours'}</span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}
