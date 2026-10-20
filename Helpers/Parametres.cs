using System;
using System.Configuration;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Lecture des paramètres de configuration, en donnant la priorité aux
    /// variables d'environnement du serveur sur Web.config.
    ///
    /// Ordre de résolution :
    ///   1. variable d'environnement <c>CAMALERT_&lt;CLE&gt;</c>
    ///      (processus, puis utilisateur, puis machine) ;
    ///   2. <c>appSettings</c> de Web.config ;
    ///   3. valeur par défaut fournie par l'appelant.
    ///
    /// Pourquoi : les identifiants du compte Gmail d'envoi étaient écrits en
    /// clair dans Contact.aspx.cs, donc publiés avec le dépôt. Sur le serveur
    /// on définit désormais :
    ///
    ///     setx CAMALERT_SMTP_PASSWORD "xxxxxxxxxxxxxxxx" /M
    ///
    /// et le dépôt ne contient plus que des espaces réservés.
    /// </summary>
    public static class Parametres
    {
        /// <summary>Préfixe des variables d'environnement reconnues.</summary>
        public const string PREFIXE_ENV = "CAMALERT_";

        /// <summary>
        /// Valeurs présentes dans l'historique Git, donc publiques et à
        /// considérer comme compromises.
        /// </summary>
        private static readonly string[] ValeursCompromises =
        {
            "sxnc ieny uguy ucnw",
            "sxncienyuguyucnw",
            "cvkcvuyfjnbjeyzj"
        };

        private static readonly string[] Placeholders =
        {
            "A_DEFINIR",
            "A_DEFINIR_VARIABLE_ENVIRONNEMENT",
            "CHANGEME",
            "TODO"
        };

        /// <summary>
        /// Lit un paramètre. Retourne <paramref name="valeurDefaut"/> si rien
        /// n'est configuré ou si la valeur trouvée est un espace réservé.
        /// </summary>
        public static string Lire(string cle, string valeurDefaut = null)
        {
            if (string.IsNullOrWhiteSpace(cle)) return valeurDefaut;

            var valeur = LireEnvironnement(PREFIXE_ENV + cle);

            if (string.IsNullOrWhiteSpace(valeur))
                valeur = ConfigurationManager.AppSettings[cle];

            if (string.IsNullOrWhiteSpace(valeur) || EstPlaceholder(valeur))
                return valeurDefaut;

            return valeur.Trim();
        }

        /// <summary>
        /// Lit un paramètre obligatoire. Lève une exception explicite s'il
        /// manque ou s'il correspond à un secret connu comme divulgué : mieux
        /// vaut un échec lisible qu'un envoi d'email avec des identifiants
        /// publics.
        /// </summary>
        public static string LireObligatoire(string cle)
        {
            var valeur = Lire(cle);

            if (string.IsNullOrWhiteSpace(valeur))
                throw new ConfigurationErrorsException(string.Format(
                    "Paramètre « {0} » non configuré. Définissez la variable " +
                    "d'environnement {1}{0} sur le serveur (voir SECURITY.md).",
                    cle, PREFIXE_ENV));

            if (EstCompromis(valeur))
                throw new ConfigurationErrorsException(string.Format(
                    "Le paramètre « {0} » utilise un secret publié dans " +
                    "l'historique du dépôt. Révoquez-le puis définissez {1}{0} " +
                    "avec une nouvelle valeur (voir SECURITY.md).",
                    cle, PREFIXE_ENV));

            return valeur;
        }

        /// <summary>Lit un entier, avec repli sur la valeur par défaut.</summary>
        public static int LireEntier(string cle, int valeurDefaut)
        {
            int resultat;
            return int.TryParse(Lire(cle), out resultat) ? resultat : valeurDefaut;
        }

        /// <summary>Lit un booléen, avec repli sur la valeur par défaut.</summary>
        public static bool LireBooleen(string cle, bool valeurDefaut)
        {
            bool resultat;
            return bool.TryParse(Lire(cle), out resultat) ? resultat : valeurDefaut;
        }

        /// <summary>
        /// Lit une liste de valeurs séparées par des points-virgules ou des
        /// virgules (destinataires du formulaire de contact, par exemple).
        /// </summary>
        public static string[] LireListe(string cle)
        {
            var brut = Lire(cle);
            if (string.IsNullOrWhiteSpace(brut)) return new string[0];

            return brut.Split(new[] { ';', ',' },
                              StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Chaîne de connexion : variable d'environnement d'abord,
        /// section connectionStrings ensuite.
        /// </summary>
        public static string ChaineConnexion(string nom)
        {
            var valeur = LireEnvironnement(PREFIXE_ENV + "CONNECTIONSTRING_" + nom);
            if (!string.IsNullOrWhiteSpace(valeur)) return valeur.Trim();

            var reglage = ConfigurationManager.ConnectionStrings[nom];
            return reglage == null ? null : reglage.ConnectionString;
        }

        /// <summary>Vrai si la valeur figure parmi les secrets divulgués connus.</summary>
        public static bool EstCompromis(string valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) return false;

            var normalisee = valeur.Trim();
            foreach (var compromise in ValeursCompromises)
                if (string.Equals(normalisee, compromise, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static bool EstPlaceholder(string valeur)
        {
            var normalisee = valeur.Trim();
            foreach (var p in Placeholders)
                if (string.Equals(normalisee, p, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string LireEnvironnement(string nom)
        {
            try
            {
                var valeur = Environment.GetEnvironmentVariable(
                    nom, EnvironmentVariableTarget.Process);
                if (!string.IsNullOrWhiteSpace(valeur)) return valeur;

                valeur = Environment.GetEnvironmentVariable(
                    nom, EnvironmentVariableTarget.User);
                if (!string.IsNullOrWhiteSpace(valeur)) return valeur;

                return Environment.GetEnvironmentVariable(
                    nom, EnvironmentVariableTarget.Machine);
            }
            catch (System.Security.SecurityException)
            {
                // Pool d'applications sans droit de lecture du registre :
                // on se rabat sur Web.config.
                return null;
            }
        }
    }
}
