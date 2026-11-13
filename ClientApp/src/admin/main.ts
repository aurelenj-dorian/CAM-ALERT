// Espace administration (vues Razor) : styles, confirmations, graphiques et carte.
// Aucun script en ligne dans les vues : la CSP n'autorise que 'self'.
// admin.css inclut lui-même polices, tokens et base : un seul fichier CSS chargé par la vue Razor
import './admin.css'

// Confirmation avant les actions destructrices : <form data-confirmer="message">
document.addEventListener('submit', (e) => {
  const form = e.target as HTMLFormElement
  const message = form.dataset.confirmer
  if (message && !window.confirm(message)) e.preventDefault()
})

// Œil : afficher / masquer le mot de passe (<button data-afficher-mdp="idDuChamp">)
document.addEventListener('click', (e) => {
  const bouton = (e.target as Element).closest<HTMLButtonElement>('[data-afficher-mdp]')
  if (!bouton) return
  const champ = document.getElementById(bouton.dataset.afficherMdp!) as HTMLInputElement | null
  if (!champ) return
  const visible = champ.type === 'password'
  champ.type = visible ? 'text' : 'password'
  bouton.setAttribute('aria-pressed', String(visible))
  bouton.setAttribute('aria-label', visible ? 'Masquer le mot de passe' : 'Afficher le mot de passe')
  champ.focus()
})

// Le mot de passe est toujours remasqué à l'envoi du formulaire
document.addEventListener('submit', (e) => {
  for (const bouton of (e.target as HTMLFormElement).querySelectorAll<HTMLButtonElement>('[data-afficher-mdp][aria-pressed="true"]')) {
    const champ = document.getElementById(bouton.dataset.afficherMdp!) as HTMLInputElement | null
    if (champ) champ.type = 'password'
  }
})

const couleur = (variable: string) =>
  getComputedStyle(document.documentElement).getPropertyValue(variable).trim()

class SessionExpiree extends Error {}

async function chargerJson<T>(url: string): Promise<T> {
  const r = await fetch(url, { headers: { Accept: 'application/json' }, credentials: 'same-origin' })
  // Session expirée : le serveur renvoie la page de connexion (HTML) au lieu du JSON
  if (r.redirected || !(r.headers.get('content-type') ?? '').includes('json')) throw new SessionExpiree()
  if (!r.ok) throw new Error(`HTTP ${r.status}`)
  return r.json() as Promise<T>
}

// Les erreurs ne sont jamais affichées dans la page : uniquement dans la console (F12)
async function chargerOuSignaler<T>(url: string, contexte: string): Promise<T | null> {
  try {
    return await chargerJson<T>(url)
  } catch (e) {
    console.error(e instanceof SessionExpiree
      ? `${contexte} : session expirée, rechargez la page pour vous reconnecter.`
      : `${contexte} : impossible de charger les données.`, e)
    return null
  }
}

interface ValeurLibellee { libelle: string; valeur: number }

// ---------- Graphiques ----------
async function initGraphiques(conteneur: HTMLElement) {
  const { Chart, registerables } = await import('chart.js')
  Chart.register(...registerables)
  Chart.defaults.font.family = couleur('--police')
  Chart.defaults.color = couleur('--texte-2')
  Chart.defaults.borderColor = couleur('--bordure')

  const donnees = await chargerOuSignaler<{ parMois: ValeurLibellee[]; parRegion: ValeurLibellee[] }>(
    conteneur.dataset.graphiques!, 'Graphiques')
  if (!donnees) return

  // Une erreur d'affichage est journalisée dans la console, sans faire croire à un échec de chargement
  try {
    const mois = document.querySelector<HTMLCanvasElement>('#graphique-mois')
    const region = document.querySelector<HTMLCanvasElement>('#graphique-region')
    if (mois)
      new Chart(mois, {
        type: 'line',
        data: {
          labels: donnees.parMois.map((x) => x.libelle),
          datasets: [{
            label: 'Signalements', data: donnees.parMois.map((x) => x.valeur),
            borderColor: couleur('--primaire'), backgroundColor: couleur('--primaire-doux'),
            fill: true, tension: 0.3, pointRadius: 4,
          }],
        },
        options: { maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } },
      })
    if (region)
      new Chart(region, {
        type: 'bar',
        data: {
          labels: donnees.parRegion.map((x) => x.libelle),
          datasets: [{ label: 'Signalements', data: donnees.parRegion.map((x) => x.valeur), backgroundColor: couleur('--primaire'), borderRadius: 6 }],
        },
        options: { indexAxis: 'y', maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { x: { beginAtZero: true, ticks: { precision: 0 } } } },
      })
  } catch (e) {
    console.error('Graphiques : affichage', e)
  }
}

// ---------- Audience (données dans les attributs data-*) ----------
async function initAudience(canvas: HTMLCanvasElement) {
  const { Chart, registerables } = await import('chart.js')
  Chart.register(...registerables)
  Chart.defaults.font.family = couleur('--police')
  Chart.defaults.color = couleur('--texte-2')
  new Chart(canvas, {
    type: 'bar',
    data: {
      labels: JSON.parse(canvas.dataset.libelles ?? '[]'),
      datasets: [{ label: 'Pages vues', data: JSON.parse(canvas.dataset.valeurs ?? '[]'), backgroundColor: couleur('--primaire'), borderRadius: 4 }],
    },
    options: { maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } },
  })
}

// ---------- Carte ----------
interface PointCarte { id: number; titre: string; type: string; ville: string | null; latitude: number; longitude: number }

async function initCarte(element: HTMLElement) {
  const L = (await import('leaflet')).default
  await import('leaflet/dist/leaflet.css')
  ;(window as unknown as { L: typeof L }).L = L // requis par leaflet.markercluster
  await import('leaflet.markercluster')
  await import('leaflet.markercluster/dist/MarkerCluster.css')
  await import('leaflet.markercluster/dist/MarkerCluster.Default.css')

  const carte = L.map(element).setView([5.7, 12.4], 6) // Cameroun
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 18,
    attribution: '&copy; contributeurs OpenStreetMap',
  }).addTo(carte)

  const points = await chargerOuSignaler<PointCarte[]>(element.dataset.carte!, 'Carte')
  if (!points) return

  const groupe = L.markerClusterGroup()
  for (const p of points) {
    // Un point invalide est ignoré sans bloquer les autres
    try {
      const disparition = p.type === 'Disparition'
      const marqueur = L.marker([p.latitude, p.longitude], {
        title: p.titre,
        icon: L.divIcon({
          className: `marqueur ${disparition ? 'marqueur-disparition' : 'marqueur-recherche'}`,
          iconSize: [22, 22],
        }),
      })
      // Contenu construit en nœuds DOM (textContent) : aucune injection HTML possible
      const popup = document.createElement('div')
      const titre = document.createElement('strong')
      titre.textContent = p.titre
      const detail = document.createElement('p')
      detail.textContent = `${p.type}${p.ville ? ' · ' + p.ville : ''}`
      const lien = document.createElement('a')
      lien.href = `/dossiers/${p.id}`
      lien.textContent = 'Voir la fiche publique'
      popup.append(titre, detail, lien)
      marqueur.bindPopup(popup)
      groupe.addLayer(marqueur)
    } catch (e) {
      console.error('Carte : marqueur ignoré', p, e)
    }
  }
  try {
    carte.addLayer(groupe)
  } catch (e) {
    console.error('Carte : affichage des marqueurs', e)
  }
}

const graphiques = document.querySelector<HTMLElement>('[data-graphiques]')
if (graphiques) initGraphiques(graphiques)

const audience = document.querySelector<HTMLCanvasElement>('#graphique-audience')
if (audience) initAudience(audience)

const carte = document.querySelector<HTMLElement>('[data-carte]')
if (carte) initCarte(carte)
