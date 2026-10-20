using CamAlert.Helpers;
using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Web;

namespace CamAlert.Handlers
{
    /// <summary>
    /// Point de contrôle d'état, exposé à l'adresse <c>/sante</c>
    /// (enregistré dans Web.config, section system.webServer/handlers).
    ///
    /// Sert à trois choses : vérifier après un déploiement que le site répond
    /// et joint SQL Server, alimenter une supervision, et confirmer que la
    /// configuration sensible est en place — mot de passe SMTP défini hors
    /// Web.config, et non l'ancien secret publié avec le dépôt.
    ///
    /// La réponse ne contient ni chaîne de connexion, ni nom de serveur, ni
    /// message d'exception : un appel anonyme ne doit rien apprendre de
    /// l'infrastructure. Le détail des erreurs part dans le journal.
    /// </summary>
    public class SanteHandler : IHttpHandler
    {
        /// <summary>
        /// Pas d'état de session : ce gestionnaire doit répondre même quand le
        /// magasin de sessions est en difficulté.
        /// </summary>
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext contexte)
        {
            var chrono = System.Diagnostics.Stopwatch.StartNew();

            bool baseJoignable = Database.TesterConnexion();
            bool smtpConfigure = SmtpConfigure();

            chrono.Stop();

            // La base est indispensable ; une configuration SMTP incomplète ne
            // dégrade que le formulaire de contact.
            string etat = baseJoignable
                ? (smtpConfigure ? "ok" : "degrade")
                : "indisponible";

            var reponse = contexte.Response;
            reponse.ContentType = "application/json";
            reponse.ContentEncoding = Encoding.UTF8;
            reponse.StatusCode = baseJoignable ? 200 : 503;
            reponse.TrySkipIisCustomErrors = true;
            reponse.Cache.SetCacheability(HttpCacheability.NoCache);

            var json = new StringBuilder();
            json.Append("{");
            json.AppendFormat(CultureInfo.InvariantCulture, "\"etat\":\"{0}\",", etat);
            json.Append("\"application\":\"CAM-ALERT\",");
            json.AppendFormat(CultureInfo.InvariantCulture, "\"version\":\"{0}\",", Version());
            json.AppendFormat(CultureInfo.InvariantCulture, "\"baseDeDonnees\":\"{0}\",",
                              baseJoignable ? "joignable" : "injoignable");
            json.AppendFormat(CultureInfo.InvariantCulture, "\"smtp\":\"{0}\",",
                              smtpConfigure ? "configure" : "non configure");
            json.AppendFormat(CultureInfo.InvariantCulture, "\"dureeMs\":{0},",
                              chrono.ElapsedMilliseconds);
            json.AppendFormat(CultureInfo.InvariantCulture, "\"horodatage\":\"{0}\"",
                              DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            json.Append("}");

            reponse.Write(json.ToString());
        }

        private static bool SmtpConfigure()
        {
            try
            {
                // LireObligatoire lève une exception si le paramètre manque ou
                // s'il correspond au mot de passe d'application publié dans
                // l'historique du dépôt.
                Parametres.LireObligatoire("SMTP_Password");
                return !string.IsNullOrWhiteSpace(Parametres.Lire("SMTP_User"))
                    && Parametres.LireListe("Contact_Destinataires").Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string Version()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch
            {
                return "inconnue";
            }
        }
    }
}
