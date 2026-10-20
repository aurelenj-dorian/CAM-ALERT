using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Hachage des mots de passe par PBKDF2-HMAC-SHA256, avec sel aléatoire
    /// propre à chaque compte.
    ///
    /// Format stocké dans Utilisateurs.MotDePasse :
    ///
    ///     pbkdf2$sha256$&lt;iterations&gt;$&lt;sel base64&gt;$&lt;cle base64&gt;
    ///
    /// Remplace le SHA-256 simple utilisé jusqu'ici (Admin/Login.aspx.cs).
    /// Un SHA-256 sans sel se casse par table précalculée : les mots de passe
    /// courants s'y retrouvent en quelques secondes, et deux comptes ayant
    /// choisi le même mot de passe portaient le même hash, donc se trahissaient
    /// l'un l'autre.
    ///
    /// Les hash existants (64 caractères hexadécimaux, en majuscules dans cette
    /// application) restent vérifiables : <see cref="Verifier"/> les reconnaît et
    /// <see cref="NecessiteRehachage"/> signale qu'il faut les réécrire. La
    /// bascule se fait à la connexion, sans rien demander aux utilisateurs.
    /// </summary>
    public static class HacheurMotDePasse
    {
        private const string PREFIXE = "pbkdf2";
        private const string ALGO = "sha256";

        private const int TAILLE_SEL = 16;
        private const int TAILLE_CLE = 32;

        /// <summary>
        /// Coût par défaut. À relever quand le matériel le permet : les hash
        /// déjà stockés restent valides, ils portent leur propre compteur.
        /// </summary>
        public const int ITERATIONS_PAR_DEFAUT = 210000;

        private const int LONGUEUR_HEX_SHA256 = 64;

        /// <summary>Hache un mot de passe au format courant.</summary>
        public static string Hacher(string motDePasse)
        {
            return Hacher(motDePasse, ITERATIONS_PAR_DEFAUT);
        }

        /// <summary>
        /// Hache un mot de passe avec un coût explicite (surcharge des tests).
        /// </summary>
        public static string Hacher(string motDePasse, int iterations)
        {
            if (motDePasse == null) throw new ArgumentNullException("motDePasse");
            if (iterations < 1000) throw new ArgumentOutOfRangeException("iterations");

            var sel = new byte[TAILLE_SEL];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(sel);

            var cle = Deriver(motDePasse, sel, iterations, TAILLE_CLE);

            return string.Concat(
                PREFIXE, "$", ALGO, "$",
                iterations.ToString(CultureInfo.InvariantCulture), "$",
                Convert.ToBase64String(sel), "$",
                Convert.ToBase64String(cle));
        }

        /// <summary>
        /// Vérifie un mot de passe contre le hash stocké, quel que soit son
        /// format (PBKDF2 courant ou ancien SHA-256 hexadécimal).
        /// </summary>
        public static bool Verifier(string motDePasse, string hashStocke)
        {
            if (motDePasse == null || string.IsNullOrWhiteSpace(hashStocke))
                return false;

            hashStocke = hashStocke.Trim();

            if (EstFormatLegacy(hashStocke))
                return ComparaisonConstanteHex(HacherLegacySha256(motDePasse), hashStocke);

            var parties = hashStocke.Split('$');
            if (parties.Length != 5) return false;
            if (parties[0] != PREFIXE || parties[1] != ALGO) return false;

            int iterations;
            if (!int.TryParse(parties[2], NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out iterations)
                || iterations < 1)
                return false;

            byte[] sel;
            byte[] cleAttendue;
            try
            {
                sel = Convert.FromBase64String(parties[3]);
                cleAttendue = Convert.FromBase64String(parties[4]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (sel.Length == 0 || cleAttendue.Length == 0) return false;

            var cleCalculee = Deriver(motDePasse, sel, iterations, cleAttendue.Length);
            return ComparaisonConstante(cleCalculee, cleAttendue);
        }

        /// <summary>
        /// Indique qu'un hash doit être réécrit : ancien format, ou coût devenu
        /// inférieur au coût courant. À appeler après une vérification réussie.
        /// </summary>
        public static bool NecessiteRehachage(string hashStocke)
        {
            if (string.IsNullOrWhiteSpace(hashStocke)) return true;

            hashStocke = hashStocke.Trim();
            if (EstFormatLegacy(hashStocke)) return true;

            var parties = hashStocke.Split('$');
            if (parties.Length != 5) return true;
            if (parties[0] != PREFIXE || parties[1] != ALGO) return true;

            int iterations;
            if (!int.TryParse(parties[2], NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out iterations))
                return true;

            return iterations < ITERATIONS_PAR_DEFAUT;
        }

        /// <summary>
        /// Vrai si le hash est au format historique : 64 caractères
        /// hexadécimaux, quelle que soit leur casse.
        /// </summary>
        public static bool EstFormatLegacy(string hashStocke)
        {
            if (string.IsNullOrWhiteSpace(hashStocke)) return false;

            hashStocke = hashStocke.Trim();
            if (hashStocke.Length != LONGUEUR_HEX_SHA256) return false;

            foreach (char c in hashStocke)
            {
                bool hex = (c >= '0' && c <= '9')
                        || (c >= 'a' && c <= 'f')
                        || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }

        /// <summary>
        /// Ancien hachage SHA-256 non salé, en hexadécimal majuscule — le format
        /// que produisait Admin/Login.aspx.cs. Conservé uniquement pour vérifier
        /// les comptes créés avant la migration ; ne jamais l'employer pour un
        /// nouveau mot de passe.
        /// </summary>
        public static string HacherLegacySha256(string motDePasse)
        {
            using (var sha256 = SHA256.Create())
            {
                var octets = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(motDePasse ?? string.Empty));
                return BitConverter.ToString(octets).Replace("-", string.Empty)
                                   .ToUpperInvariant();
            }
        }

        private static byte[] Deriver(string motDePasse, byte[] sel,
                                      int iterations, int tailleCle)
        {
            // Rfc2898DeriveBytes avec HashAlgorithmName : disponible depuis
            // .NET Framework 4.7.2 (le projet cible 4.8).
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                       motDePasse, sel, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(tailleCle);
            }
        }

        /// <summary>
        /// Comparaison à temps constant de deux séquences d'octets.
        /// CryptographicOperations.FixedTimeEquals n'existe pas sur
        /// .NET Framework : on l'écrit à la main.
        /// </summary>
        private static bool ComparaisonConstante(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            int diff = a.Length ^ b.Length;
            int longueur = Math.Min(a.Length, b.Length);
            for (int i = 0; i < longueur; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        /// <summary>
        /// Comparaison à temps constant de deux hash hexadécimaux, insensible à
        /// la casse : l'ancien code stockait l'hexadécimal en majuscules, mais
        /// des bases ont pu recevoir des minuscules.
        /// </summary>
        public static bool ComparaisonConstanteHex(string a, string b)
        {
            if (a == null || b == null) return false;

            var x = a.ToUpperInvariant();
            var y = b.ToUpperInvariant();

            int diff = x.Length ^ y.Length;
            int longueur = Math.Min(x.Length, y.Length);
            for (int i = 0; i < longueur; i++)
                diff |= x[i] ^ y[i];
            return diff == 0;
        }
    }
}
