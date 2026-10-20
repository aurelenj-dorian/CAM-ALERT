using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Limiteur de tentatives en mémoire, partagé par tout le processus IIS.
    ///
    /// Protège les formulaires exposés au public :
    ///   • la connexion d'administration, qui acceptait un nombre illimité
    ///     d'essais sur un mot de passe ;
    ///   • le formulaire de signalement, par lequel on pouvait inonder la base
    ///     de fausses déclarations et remplir le dossier /Uploads ;
    ///   • le formulaire de contact, qui envoyait un email à quatre adresses à
    ///     chaque soumission — un relais de spam prêt à l'emploi.
    ///
    /// Principe : fenêtre glissante par clé. Au-delà de <c>maxTentatives</c>
    /// dans la fenêtre, la clé est bloquée pour la durée de blocage.
    ///
    /// Limite connue : l'état vit dans le processus. Un recyclage du pool
    /// d'applications remet les compteurs à zéro. C'est assumé — cela évite une
    /// écriture en base à chaque tentative, et le journal d'audit
    /// (<see cref="Journal"/>) conserve de son côté la trace des échecs, que la
    /// procédure sp_CompterEchecsConnexion permet d'interroger.
    /// </summary>
    public static class LimiteurTentatives
    {
        private class Compteur
        {
            public int Tentatives;
            public DateTime Debut;
            public DateTime? BloqueJusqua;
        }

        private static readonly ConcurrentDictionary<string, Compteur> Compteurs =
            new ConcurrentDictionary<string, Compteur>(StringComparer.OrdinalIgnoreCase);

        private static DateTime _dernierNettoyage = DateTime.UtcNow;

        // Connexion : 5 échecs en 15 minutes → 15 minutes de blocage
        public const int MAX_CONNEXION = 5;

        // Signalement : 3 envois en 15 minutes depuis la même adresse
        public const int MAX_SIGNALEMENT = 3;

        // Contact : 3 messages en 15 minutes depuis la même adresse
        public const int MAX_CONTACT = 3;

        public const int FENETRE_MINUTES = 15;
        public const int BLOCAGE_MINUTES = 15;

        /// <summary>
        /// Construit une clé de suivi à partir d'une portée, d'un identifiant
        /// et de l'adresse IP. On combine les deux derniers : bloquer sur le
        /// seul identifiant permettrait de verrouiller le compte d'autrui.
        /// </summary>
        public static string Cle(string portee, string identifiant, string adresseIp)
        {
            return string.Concat(
                portee ?? "?", "|",
                (identifiant ?? "?").Trim().ToLowerInvariant(), "|",
                adresseIp ?? "?");
        }

        /// <summary>Clé fondée sur la seule adresse IP (formulaires publics).</summary>
        public static string CleIp(string portee, string adresseIp)
        {
            return string.Concat(portee ?? "?", "|ip|", adresseIp ?? "?");
        }

        /// <summary>Vrai si la clé est actuellement bloquée.</summary>
        public static bool EstBloque(string cle)
        {
            int restant;
            return EstBloque(cle, out restant);
        }

        /// <summary>
        /// Vrai si la clé est bloquée ; <paramref name="secondesRestantes"/>
        /// donne le temps d'attente à afficher.
        /// </summary>
        public static bool EstBloque(string cle, out int secondesRestantes)
        {
            secondesRestantes = 0;
            if (string.IsNullOrEmpty(cle)) return false;

            NettoyerSiNecessaire();

            Compteur c;
            if (!Compteurs.TryGetValue(cle, out c)) return false;

            lock (c)
            {
                if (c.BloqueJusqua.HasValue)
                {
                    var reste = c.BloqueJusqua.Value - DateTime.UtcNow;
                    if (reste > TimeSpan.Zero)
                    {
                        secondesRestantes = (int)Math.Ceiling(reste.TotalSeconds);
                        return true;
                    }

                    c.BloqueJusqua = null;
                    c.Tentatives = 0;
                    c.Debut = DateTime.UtcNow;
                }
                return false;
            }
        }

        /// <summary>
        /// Compte une tentative. Retourne true si la clé vient d'être bloquée.
        /// </summary>
        public static bool CompterTentative(string cle, int maxTentatives = MAX_CONNEXION)
        {
            if (string.IsNullOrEmpty(cle)) return false;

            NettoyerSiNecessaire();

            var c = Compteurs.GetOrAdd(cle, _ => new Compteur
            {
                Tentatives = 0,
                Debut = DateTime.UtcNow
            });

            lock (c)
            {
                var maintenant = DateTime.UtcNow;

                if (maintenant - c.Debut > TimeSpan.FromMinutes(FENETRE_MINUTES))
                {
                    c.Tentatives = 0;
                    c.Debut = maintenant;
                }

                c.Tentatives++;

                if (c.Tentatives >= maxTentatives)
                {
                    c.BloqueJusqua = maintenant.AddMinutes(BLOCAGE_MINUTES);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Remet le compteur à zéro — à appeler après une connexion réussie.
        /// </summary>
        public static void Reussite(string cle)
        {
            if (string.IsNullOrEmpty(cle)) return;
            Compteur ignore;
            Compteurs.TryRemove(cle, out ignore);
        }

        /// <summary>Nombre de tentatives restantes avant blocage.</summary>
        public static int TentativesRestantes(string cle, int maxTentatives = MAX_CONNEXION)
        {
            Compteur c;
            if (string.IsNullOrEmpty(cle) || !Compteurs.TryGetValue(cle, out c))
                return maxTentatives;

            lock (c)
            {
                var reste = maxTentatives - c.Tentatives;
                return reste < 0 ? 0 : reste;
            }
        }

        /// <summary>Message prêt à afficher, sans révéler si le compte existe.</summary>
        public static string MessageBlocage(int secondesRestantes)
        {
            var minutes = (int)Math.Ceiling(secondesRestantes / 60.0);
            if (minutes <= 1)
                return "Trop de tentatives. Réessayez dans une minute.";

            return string.Format(CultureInfo.CurrentCulture,
                "Trop de tentatives. Réessayez dans {0} minutes.", minutes);
        }

        /// <summary>
        /// Purge les entrées inactives, au plus une fois toutes les 10 minutes,
        /// pour que le dictionnaire ne croisse pas indéfiniment.
        /// </summary>
        private static void NettoyerSiNecessaire()
        {
            var maintenant = DateTime.UtcNow;
            if (maintenant - _dernierNettoyage < TimeSpan.FromMinutes(10)) return;
            _dernierNettoyage = maintenant;

            var limite = TimeSpan.FromMinutes(FENETRE_MINUTES + BLOCAGE_MINUTES);
            foreach (var paire in Compteurs)
            {
                var c = paire.Value;
                bool perime;
                lock (c)
                {
                    perime = (!c.BloqueJusqua.HasValue || c.BloqueJusqua.Value < maintenant)
                             && (maintenant - c.Debut) > limite;
                }
                if (perime)
                {
                    Compteur ignore;
                    Compteurs.TryRemove(paire.Key, out ignore);
                }
            }
        }

        /// <summary>Vide l'état — réservé aux tests unitaires.</summary>
        public static void ReinitialiserTout()
        {
            Compteurs.Clear();
            _dernierNettoyage = DateTime.UtcNow;
        }
    }
}
