using System;
using System.Security.Cryptography;
using System.Web;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Verrou anti-fixation de session.
    ///
    /// L'identifiant de session ASP.NET est attribué avant la connexion et ne
    /// change pas quand l'agent s'authentifie. Un attaquant qui parvient à
    /// imposer un identifiant connu au navigateur de la victime (lien piégé,
    /// sous-domaine compromis) réutilise ensuite la même session, désormais
    /// authentifiée. ASP.NET ne sait pas régénérer proprement cet identifiant
    /// en cours de requête sans perdre le contenu de la session : on ajoute donc
    /// un second facteur.
    ///
    /// À la connexion, on tire un jeton aléatoire, placé à la fois en session et
    /// dans un cookie dédié (HttpOnly, SameSite=Strict, Secure en HTTPS). Chaque
    /// page d'administration vérifie que les deux correspondent. L'attaquant
    /// connaît l'identifiant de session qu'il a imposé, mais pas le jeton,
    /// déposé seulement dans le navigateur de la victime.
    /// </summary>
    public static class JetonSession
    {
        /// <summary>Clé de session contenant le jeton.</summary>
        public const string CLE_SESSION = "JetonSession";

        /// <summary>Nom du cookie portant le même jeton.</summary>
        public const string NOM_COOKIE = "CAMALERT_ST";

        /// <summary>
        /// Tire un nouveau jeton, l'enregistre en session et le dépose en
        /// cookie. À appeler juste après une authentification réussie.
        /// </summary>
        public static void Demarrer(System.Web.SessionState.HttpSessionState session,
                                    HttpResponse reponse, bool https)
        {
            if (session == null) return;

            var jeton = Generer();
            session[CLE_SESSION] = jeton;

            if (reponse == null) return;

            var cookie = new HttpCookie(NOM_COOKIE, jeton)
            {
                HttpOnly = true,
                Path = "/",
                Secure = https
                // Pas d'Expires : cookie de session, rien n'est écrit sur le
                // disque du client.
            };
            cookie.SameSite = SameSiteMode.Strict;
            reponse.Cookies.Set(cookie);
        }

        /// <summary>
        /// Vérifie que le cookie présenté correspond au jeton de la session.
        /// Retourne true quand la session ne porte pas encore de jeton : les
        /// sessions ouvertes avant le déploiement de ce contrôle ne sont pas
        /// coupées brutalement.
        /// </summary>
        public static bool EstValide(System.Web.SessionState.HttpSessionState session,
                                     HttpRequest requete)
        {
            if (session == null) return false;

            var attendu = session[CLE_SESSION] as string;
            if (string.IsNullOrEmpty(attendu)) return true;

            if (requete == null || requete.Cookies == null) return false;

            var cookie = requete.Cookies[NOM_COOKIE];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value)) return false;

            return HacheurMotDePasse.ComparaisonConstanteHex(attendu, cookie.Value);
        }

        /// <summary>
        /// Efface le jeton et demande au navigateur de supprimer le cookie.
        /// </summary>
        public static void Terminer(System.Web.SessionState.HttpSessionState session,
                                    HttpResponse reponse, bool https)
        {
            if (session != null) session.Remove(CLE_SESSION);
            if (reponse == null) return;

            var suppression = new HttpCookie(NOM_COOKIE, string.Empty)
            {
                HttpOnly = true,
                Path = "/",
                Expires = new DateTime(1999, 1, 1),
                Secure = https
            };
            suppression.SameSite = SameSiteMode.Strict;
            reponse.Cookies.Set(suppression);
        }

        /// <summary>Jeton de 256 bits encodé en hexadécimal.</summary>
        public static string Generer()
        {
            var octets = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(octets);

            return BitConverter.ToString(octets).Replace("-", string.Empty);
        }
    }
}
