using System;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Validation des URL de retour.
    ///
    /// La page de connexion accepte un paramètre <c>retour</c> pour ramener
    /// l'agent là où il allait. Rediriger vers cette valeur sans la contrôler
    /// ouvre une redirection arbitraire : un lien
    /// <c>…/Admin/Login.aspx?retour=https://site-pirate.example</c> envoie la
    /// victime sur une fausse page de connexion après son authentification,
    /// avec l'adresse du vrai site dans l'historique.
    ///
    /// ASP.NET MVC fournit <c>Url.IsLocalUrl</c> ; les Web Forms n'ont pas
    /// d'équivalent, d'où cette vérification.
    /// </summary>
    public static class UrlLocale
    {
        /// <summary>
        /// Vrai si l'URL désigne une ressource du site lui-même : chemin
        /// relatif commençant par « / » mais ni « // » ni « /\ », qui
        /// désigneraient un autre hôte.
        /// </summary>
        public static bool EstLocale(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            // Un schéma explicite (http:, javascript:, data:…) sort du site.
            if (url.IndexOf(':') >= 0) return false;

            if (url[0] == '~')
                return url.Length > 1 && url[1] == '/';

            if (url[0] != '/') return false;

            // « //hote » et « /\hote » sont des URL de protocole relatif :
            // le navigateur les résout vers un autre domaine.
            if (url.Length > 1 && (url[1] == '/' || url[1] == '\\')) return false;

            return true;
        }

        /// <summary>
        /// Retourne l'URL si elle est locale, sinon
        /// <paramref name="parDefaut"/>.
        /// </summary>
        public static string OuParDefaut(string url, string parDefaut)
        {
            return EstLocale(url) ? url : parDefaut;
        }
    }
}
