using System;
using System.Collections.Generic;
using System.Globalization;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Règles de robustesse appliquées à tout nouveau mot de passe
    /// d'administration.
    ///
    /// Le formulaire de connexion n'imposait rien du tout : un compte pouvait
    /// porter « admin » comme mot de passe. Ces règles suivent les
    /// recommandations actuelles (NIST SP 800-63B, guide ANSSI) : on privilégie
    /// la longueur et le refus des mots de passe devinables plutôt que des
    /// obligations de composition qui poussent aux variantes du type
    /// « Motdepasse1! ».
    /// </summary>
    public static class PolitiqueMotDePasse
    {
        public const int LONGUEUR_MINIMALE = 12;
        public const int LONGUEUR_MAXIMALE = 128;

        private static readonly HashSet<string> ListeNoire =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "motdepasse", "motdepasse1", "motdepasse123", "password",
                "password1", "password123", "passw0rd", "azertyuiop",
                "qwertyuiop", "123456789", "1234567890", "0123456789",
                "administrateur", "administrator", "admin", "admin123",
                "camalert", "camalert123", "camalert2026", "camalert@2026",
                "camalert@2025", "camalert@2024", "cameroun", "cameroun2026",
                "interpol", "changeme", "bienvenue", "bienvenue1",
                "yaounde", "douala", "police", "gendarmerie"
            };

        /// <summary>
        /// Valide un mot de passe. Retourne <c>null</c> si tout est correct,
        /// sinon le message d'erreur à afficher.
        /// </summary>
        /// <param name="motDePasse">Le mot de passe proposé.</param>
        /// <param name="donneesPersonnelles">
        /// Éléments liés au compte (login, nom) : un mot de passe qui les
        /// contient est trivial à deviner.
        /// </param>
        public static string Valider(string motDePasse, params string[] donneesPersonnelles)
        {
            if (string.IsNullOrWhiteSpace(motDePasse))
                return "Le mot de passe est obligatoire.";

            if (motDePasse.Length < LONGUEUR_MINIMALE)
                return string.Format(CultureInfo.CurrentCulture,
                    "Le mot de passe doit comporter au moins {0} caractères.",
                    LONGUEUR_MINIMALE);

            if (motDePasse.Length > LONGUEUR_MAXIMALE)
                return string.Format(CultureInfo.CurrentCulture,
                    "Le mot de passe ne doit pas dépasser {0} caractères.",
                    LONGUEUR_MAXIMALE);

            if (motDePasse.Trim().Length != motDePasse.Length)
                return "Le mot de passe ne doit pas commencer ou finir par un espace.";

            // Au moins trois familles de caractères sur quatre. On laisse passer
            // les phrases de passe longues avec moins de familles : elles sont
            // plus solides et plus faciles à retenir.
            int familles = 0;
            if (Contient(motDePasse, char.IsLower)) familles++;
            if (Contient(motDePasse, char.IsUpper)) familles++;
            if (Contient(motDePasse, char.IsDigit)) familles++;
            if (Contient(motDePasse, c => !char.IsLetterOrDigit(c))) familles++;

            if (familles < 3 && motDePasse.Length < 20)
                return "Le mot de passe doit mêler au moins trois types de " +
                       "caractères (minuscules, majuscules, chiffres, symboles), " +
                       "ou compter au moins 20 caractères.";

            if (ListeNoire.Contains(motDePasse.Trim()))
                return "Ce mot de passe est trop courant. Choisissez-en un autre.";

            if (EstUneSuiteOuRepetition(motDePasse))
                return "Évitez les suites et les répétitions " +
                       "(« 123456789 », « aaaaaaaaaaaa », « azertyuiop »).";

            if (donneesPersonnelles != null)
            {
                foreach (var donnee in donneesPersonnelles)
                {
                    if (string.IsNullOrWhiteSpace(donnee) || donnee.Trim().Length < 3)
                        continue;

                    if (motDePasse.IndexOf(donnee.Trim(),
                                           StringComparison.OrdinalIgnoreCase) >= 0)
                        return "Le mot de passe ne doit pas contenir votre " +
                               "identifiant ni votre nom.";
                }
            }

            return null;
        }

        /// <summary>Vrai si le mot de passe satisfait toutes les règles.</summary>
        public static bool EstValide(string motDePasse, params string[] donneesPersonnelles)
            => Valider(motDePasse, donneesPersonnelles) == null;

        private static bool Contient(string texte, Func<char, bool> predicat)
        {
            foreach (char c in texte)
                if (predicat(c)) return true;
            return false;
        }

        /// <summary>
        /// Détecte un mot de passe entièrement composé d'un même caractère,
        /// d'une suite croissante ou décroissante, ou d'une portion de rangée
        /// de clavier.
        /// </summary>
        private static bool EstUneSuiteOuRepetition(string motDePasse)
        {
            var s = motDePasse.ToLowerInvariant();

            bool identiques = true;
            for (int i = 1; i < s.Length; i++)
                if (s[i] != s[0]) { identiques = false; break; }
            if (identiques) return true;

            bool croissante = true, decroissante = true;
            for (int i = 1; i < s.Length; i++)
            {
                if (s[i] != s[i - 1] + 1) croissante = false;
                if (s[i] != s[i - 1] - 1) decroissante = false;
            }
            if (croissante || decroissante) return true;

            string[] rangees =
            {
                "azertyuiop", "qwertyuiop", "qsdfghjklm",
                "asdfghjkl", "wxcvbn", "zxcvbnm", "1234567890"
            };
            foreach (var rangee in rangees)
                if (rangee.IndexOf(s, StringComparison.Ordinal) >= 0) return true;

            return false;
        }
    }
}
