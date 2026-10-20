using CamAlert.Helpers;
using System;
using System.Web;

namespace CamAlert
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Dernier filet avant la page d'erreur.
        ///
        /// Les exceptions non rattrapées ne laissaient aucune trace : les
        /// quelques <c>Debug.WriteLine</c> du code ne produisent rien en
        /// configuration Release, et aucun journal applicatif n'existait. On
        /// enregistre désormais l'exception complète côté serveur, puis on
        /// renvoie le visiteur vers une page neutre portant une référence
        /// d'incident — à la place de l'écran jaune ASP.NET, qui affichait le
        /// chemin des sources, la requête SQL et le nom du serveur.
        /// </summary>
        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            if (exception == null) return;

            // Une requête annulée par le visiteur (fermeture d'onglet pendant
            // un téléchargement) n'est pas un incident : inutile d'en garder
            // des milliers de traces.
            var http = exception as HttpException;
            if (http != null && http.ErrorCode == unchecked((int)0x800704CD))
                return;

            var reference = Journal.EnregistrerErreur(exception, HttpContext.Current);

            // La page d'erreur affiche cette référence ; le visiteur la cite au
            // support, qui retrouve l'entrée correspondante dans le journal.
            try
            {
                if (HttpContext.Current != null)
                    HttpContext.Current.Items["ReferenceIncident"] = reference;
            }
            catch
            {
                // Rien à faire : la référence sera simplement absente.
            }
        }

        /// <summary>
        /// Renforce les cookies émis par l'application : <c>HttpOnly</c>
        /// systématique, et <c>Secure</c> dès que la requête arrive en HTTPS.
        /// Le cookie de session est ainsi protégé même sans réglage côté IIS.
        /// </summary>
        protected void Application_EndRequest(object sender, EventArgs e)
        {
            var contexte = HttpContext.Current;
            if (contexte == null) return;

            HttpResponse reponse;
            HttpRequest requete;
            try
            {
                reponse = contexte.Response;
                requete = contexte.Request;
            }
            catch (HttpException)
            {
                // Requête déjà terminée : rien à ajuster.
                return;
            }

            if (reponse == null || requete == null) return;

            bool https = requete.IsSecureConnection;

            for (int i = 0; i < reponse.Cookies.Count; i++)
            {
                var cookie = reponse.Cookies[i];
                if (cookie == null) continue;

                cookie.HttpOnly = true;
                if (https) cookie.Secure = true;
            }
        }
    }
}
