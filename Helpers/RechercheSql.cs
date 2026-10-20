using System;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Préparation des termes de recherche utilisés dans une clause
    /// <c>LIKE</c>.
    ///
    /// Les requêtes sont paramétrées, il n'y a donc pas d'injection SQL. Mais
    /// les jokers de <c>LIKE</c> (<c>%</c>, <c>_</c>, <c>[</c>) restent
    /// interprétés à l'intérieur d'un paramètre : une recherche sur « % »
    /// renvoie tout, et un terme de plusieurs milliers de caractères force une
    /// analyse complète de la table — à chaque frappe, puisque la recherche du
    /// menu se déclenche à la saisie.
    /// </summary>
    public static class RechercheSql
    {
        /// <summary>Longueur minimale d'un terme pris en compte.</summary>
        public const int LONGUEUR_MINIMALE = 2;

        /// <summary>
        /// Longueur maximale retenue. Au-delà, le terme est tronqué : aucun
        /// titre de dossier ne dépasse 200 caractères.
        /// </summary>
        public const int LONGUEUR_MAXIMALE = 100;

        /// <summary>
        /// Nettoie un terme de recherche. Retourne <c>null</c> si le terme est
        /// absent ou trop court pour être utile — l'appelant omet alors la
        /// clause de filtrage.
        /// </summary>
        public static string Normaliser(string terme)
        {
            if (string.IsNullOrWhiteSpace(terme)) return null;

            terme = terme.Trim();

            if (terme.Length > LONGUEUR_MAXIMALE)
                terme = terme.Substring(0, LONGUEUR_MAXIMALE);

            terme = EchapperJokers(terme);

            return terme.Length < LONGUEUR_MINIMALE ? null : terme;
        }

        /// <summary>
        /// Neutralise les jokers de LIKE. Les requêtes n'utilisant pas de
        /// clause ESCAPE, on retire simplement ces caractères plutôt que de les
        /// échapper : ils n'ont aucun sens dans le nom d'une personne ou d'une
        /// ville.
        /// </summary>
        public static string EchapperJokers(string terme)
        {
            if (string.IsNullOrEmpty(terme)) return terme;

            var sortie = new System.Text.StringBuilder(terme.Length);
            foreach (char c in terme)
            {
                if (c == '%' || c == '_' || c == '[' || c == ']' || c == '^')
                    continue;
                sortie.Append(c);
            }
            return sortie.ToString().Trim();
        }
    }
}
