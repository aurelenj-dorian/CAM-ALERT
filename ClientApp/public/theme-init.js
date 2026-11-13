// Applique le thème choisi avant le rendu de React.
// Source : cookie camalert_prefs (si les préférences sont acceptées), sinon l'onglet en cours.
(function () {
  var theme = null
  try {
    var m = document.cookie.match(/(?:^|;\s*)camalert_prefs=([^;]*)/)
    if (m) theme = JSON.parse(decodeURIComponent(m[1])).theme
  } catch (e) { /* cookie illisible : ignoré */ }
  if (!theme) {
    try { theme = sessionStorage.getItem('camalert_theme') } catch (e) { /* stockage indisponible */ }
  }
  if (theme === 'clair' || theme === 'sombre') document.documentElement.setAttribute('data-theme', theme)
})()
