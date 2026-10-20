using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Journal d'audit de l'application.
    ///
    /// Rien n'était tracé : impossible de savoir qui avait supprimé un dossier,
    /// quand un signalement avait été reclassé, ni de repérer une série de
    /// tentatives de connexion. Les erreurs partaient dans
    /// <c>System.Diagnostics.Debug.WriteLine</c>, qui ne produit rien en
    /// configuration Release.
    ///
    /// Les écritures vont dans la table <c>Journal</c> via la procédure
    /// <c>sp_JournaliserAction</c>. Si la base est injoignable — c'est
    /// justement le moment où l'on a besoin d'une trace — l'entrée est écrite
    /// dans <c>App_Data/Logs/</c>, dossier qu'IIS ne sert jamais au public.
    ///
    /// Journaliser ne doit jamais faire échouer une requête : toutes les
    /// erreurs d'écriture sont absorbées.
    /// </summary>
    public static class Journal
    {
        private const string DOSSIER_LOGS = "~/App_Data/Logs";
        private static readonly object VerrouFichier = new object();

        /// <summary>Types d'action reconnus, à utiliser plutôt que des littéraux.</summary>
        public static class Actions
        {
            public const string Connexion = "Connexion";
            public const string ConnexionEchouee = "ConnexionEchouee";
            public const string Deconnexion = "Deconnexion";
            public const string Creation = "Creation";
            public const string Modification = "Modification";
            public const string Suppression = "Suppression";
            public const string Publication = "Publication";
            public const string Signalement = "Signalement";
            public const string Contact = "Contact";
            public const string Erreur = "Erreur";
        }

        /// <summary>
        /// Enregistre une action. <paramref name="details"/> ne doit contenir
        /// aucun mot de passe ni donnée sensible : le journal est consulté par
        /// les administrateurs et conservé dans les sauvegardes.
        /// </summary>
        public static void Enregistrer(string typeAction,
                                       string login = null,
                                       string entite = null,
                                       int? entiteId = null,
                                       string details = null,
                                       string adresseIp = null)
        {
            if (string.IsNullOrWhiteSpace(typeAction)) return;

            // L'IP et le login sont déduits du contexte quand l'appelant ne les
            // fournit pas, pour que chaque appel reste court.
            if (adresseIp == null) adresseIp = IpCourante();
            if (login == null) login = LoginCourant();

            details = Tronquer(Sain(details), 1000);

            try
            {
                SqlParameter[] parametres =
                {
                    Parametre("@LoginUtilisateur", SqlDbType.NVarChar, Tronquer(login, 50)),
                    Parametre("@TypeAction",       SqlDbType.NVarChar, Tronquer(typeAction, 50)),
                    Parametre("@Entite",           SqlDbType.NVarChar, Tronquer(entite, 50)),
                    Parametre("@EntiteID",         SqlDbType.Int,      entiteId),
                    Parametre("@Details",          SqlDbType.NVarChar, details),
                    Parametre("@AdresseIP",        SqlDbType.NVarChar, Tronquer(adresseIp, 45))
                };

                Database.ExecuteNonQuery("sp_JournaliserAction", parametres,
                                         CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                // Base injoignable, procédure absente : on bascule sur le
                // fichier plutôt que de perdre la trace.
                EcrireFichier(string.Format(CultureInfo.InvariantCulture,
                    "{0:yyyy-MM-dd HH:mm:ss}  {1}  login={2}  entite={3}#{4}  ip={5}  {6}" +
                    "   [journal SQL indisponible : {7}]",
                    DateTime.Now, typeAction, login ?? "-", entite ?? "-",
                    entiteId.HasValue ? entiteId.Value.ToString(CultureInfo.InvariantCulture) : "-",
                    adresseIp ?? "-", details ?? "-", ex.Message));
            }
        }

        /// <summary>
        /// Enregistre une exception : trace complète dans le fichier, résumé
        /// dans la table. Retourne une référence courte à afficher à
        /// l'utilisateur et à citer au support.
        /// </summary>
        public static string EnregistrerErreur(Exception exception, HttpContext contexte)
        {
            var reference = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            if (exception == null) return reference;

            try
            {
                var texte = new StringBuilder();
                texte.AppendLine("────────────────────────────────────────────");
                texte.AppendFormat(CultureInfo.InvariantCulture,
                    "{0:yyyy-MM-dd HH:mm:ss}  REF={1}", DateTime.Now, reference);
                texte.AppendLine();

                if (contexte != null && contexte.Request != null)
                {
                    texte.AppendLine("URL        : " + Sain(contexte.Request.Unvalidated.RawUrl));
                    texte.AppendLine("Méthode    : " + Sain(contexte.Request.HttpMethod));
                    texte.AppendLine("IP         : " + Sain(contexte.Request.UserHostAddress));
                    texte.AppendLine("Navigateur : " + Sain(contexte.Request.UserAgent));
                }

                texte.AppendLine(Decrire(exception, 0));
                texte.AppendLine();

                EcrireFichier(texte.ToString());

                Enregistrer(Actions.Erreur, details:
                    string.Format(CultureInfo.InvariantCulture, "REF={0} — {1} : {2}",
                                  reference, exception.GetType().Name, exception.Message));
            }
            catch
            {
                // Journaliser ne doit jamais aggraver un incident.
            }

            return reference;
        }

        // ─────────────────────────────────────────────────────
        // Outils internes
        // ─────────────────────────────────────────────────────

        private static SqlParameter Parametre(string nom, SqlDbType type, object valeur)
        {
            return new SqlParameter(nom, type)
            {
                Value = valeur ?? DBNull.Value
            };
        }

        private static string LoginCourant()
        {
            try
            {
                var session = HttpContext.Current == null ? null : HttpContext.Current.Session;

                // Littéral plutôt que CamAlert.PageBase.SESSION_LOGIN : les
                // aides ne dépendent pas des classes de page, ce qui les rend
                // testables sans contexte web. La clé est documentée dans
                // PageBase.
                return session == null ? null : session["UserLogin"] as string;
            }
            catch
            {
                return null;
            }
        }

        private static string IpCourante()
        {
            try
            {
                var requete = HttpContext.Current == null ? null : HttpContext.Current.Request;
                return requete == null ? null : requete.UserHostAddress;
            }
            catch
            {
                return null;
            }
        }

        private static string Decrire(Exception exception, int niveau)
        {
            var prefixe = niveau == 0 ? "Exception  : " : "Cause      : ";
            var texte = new StringBuilder();
            texte.AppendLine(prefixe + exception.GetType().FullName + " — " + exception.Message);

            if (!string.IsNullOrEmpty(exception.StackTrace))
                texte.AppendLine(exception.StackTrace);

            // Trois niveaux suffisent ; au-delà le journal devient illisible.
            if (exception.InnerException != null && niveau < 3)
                texte.Append(Decrire(exception.InnerException, niveau + 1));

            return texte.ToString();
        }

        private static void EcrireFichier(string contenu)
        {
            try
            {
                var dossier = CheminDossier();
                if (dossier == null) return;

                lock (VerrouFichier)
                {
                    if (!Directory.Exists(dossier)) Directory.CreateDirectory(dossier);

                    var fichier = Path.Combine(dossier, string.Format(
                        CultureInfo.InvariantCulture,
                        "journal-{0:yyyy-MM-dd}.log", DateTime.Now));

                    File.AppendAllText(fichier, contenu + Environment.NewLine,
                                       Encoding.UTF8);
                }
            }
            catch
            {
                // Dernier recours : il n'y a plus rien à faire.
            }
        }

        private static string CheminDossier()
        {
            try
            {
                if (HttpContext.Current != null)
                    return HttpContext.Current.Server.MapPath(DOSSIER_LOGS);

                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    "App_Data", "Logs");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Neutralise les retours à la ligne : sans cela, une valeur forgée
        /// (en-tête, champ de formulaire) permettrait d'injecter de fausses
        /// entrées dans le journal.
        /// </summary>
        private static string Sain(string valeur)
        {
            if (string.IsNullOrEmpty(valeur)) return "-";
            return valeur.Replace("\r", " ").Replace("\n", " ");
        }

        private static string Tronquer(string valeur, int longueurMax)
        {
            if (string.IsNullOrEmpty(valeur)) return valeur;
            return valeur.Length <= longueurMax
                ? valeur
                : valeur.Substring(0, longueurMax);
        }
    }
}
