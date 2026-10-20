using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Accès aux données : ouverture de connexion et exécution de requêtes
    /// paramétrées.
    ///
    /// Trois corrections par rapport à la version précédente :
    ///   • la chaîne de connexion passe par <see cref="Parametres"/>, donc par
    ///     une variable d'environnement sur le serveur plutôt que par la valeur
    ///     « Data Source=AURELE » codée dans Web.config, qui désigne un poste
    ///     de développement précis ;
    ///   • <see cref="SqlDataAdapter"/> est libéré (il ne l'était pas) ;
    ///   • un délai d'attente est fixé, et les procédures stockées sont
    ///     appelables, ce qui permet au journal d'audit de passer par
    ///     sp_JournaliserAction.
    ///
    /// Toutes les requêtes de l'application passent par ces méthodes avec des
    /// paramètres : aucune valeur fournie par l'utilisateur n'est concaténée
    /// dans du SQL.
    /// </summary>
    public class Database
    {
        /// <summary>Nom de la chaîne de connexion dans Web.config.</summary>
        public const string NOM_CONNEXION = "CamAlertDB";

        /// <summary>Délai d'exécution maximal d'une commande, en secondes.</summary>
        private const int DELAI_COMMANDE = 30;

        private static string ChaineConnexion
        {
            get
            {
                var chaine = Parametres.ChaineConnexion(NOM_CONNEXION);
                if (string.IsNullOrWhiteSpace(chaine))
                    throw new ConfigurationErrorsException(
                        "Chaîne de connexion « " + NOM_CONNEXION + " » absente. " +
                        "Définissez CAMALERT_CONNECTIONSTRING_" + NOM_CONNEXION +
                        " ou la section connectionStrings de Web.config.");
                return chaine;
            }
        }

        /// <summary>Ouvre une connexion. L'appelant doit la libérer.</summary>
        public static SqlConnection GetConnection()
        {
            var con = new SqlConnection(ChaineConnexion);
            con.Open();
            return con;
        }

        /// <summary>Exécute une requête et renvoie son résultat.</summary>
        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            return ExecuteQuery(query, parameters, CommandType.Text);
        }

        /// <summary>
        /// Exécute une requête ou une procédure stockée et renvoie son résultat.
        /// </summary>
        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters,
                                             CommandType commandType)
        {
            var dt = new DataTable();
            using (var con = GetConnection())
            using (var cmd = Commande(query, parameters, commandType, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>Exécute une commande et renvoie le nombre de lignes affectées.</summary>
        public static int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            return ExecuteNonQuery(query, parameters, CommandType.Text);
        }

        /// <summary>
        /// Exécute une commande ou une procédure stockée et renvoie le nombre
        /// de lignes affectées.
        /// </summary>
        public static int ExecuteNonQuery(string query, SqlParameter[] parameters,
                                          CommandType commandType)
        {
            using (var con = GetConnection())
            using (var cmd = Commande(query, parameters, commandType, con))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Exécute une requête et renvoie la première valeur.</summary>
        public static object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            return ExecuteScalar(query, parameters, CommandType.Text);
        }

        /// <summary>
        /// Exécute une requête ou une procédure stockée et renvoie la première
        /// valeur, ou null si le résultat est vide ou DBNull.
        /// </summary>
        public static object ExecuteScalar(string query, SqlParameter[] parameters,
                                           CommandType commandType)
        {
            using (var con = GetConnection())
            using (var cmd = Commande(query, parameters, commandType, con))
            {
                var valeur = cmd.ExecuteScalar();
                return valeur == DBNull.Value ? null : valeur;
            }
        }

        /// <summary>
        /// Vérifie que la base répond. Utilisé par le point de contrôle d'état
        /// (<c>/sante</c>) : on ne veut ni exception, ni message technique
        /// renvoyé au client.
        /// </summary>
        public static bool TesterConnexion()
        {
            try
            {
                using (var con = new SqlConnection(ChaineConnexion))
                {
                    con.Open();
                    using (var cmd = new SqlCommand("SELECT 1", con))
                    {
                        cmd.CommandTimeout = 5;
                        cmd.ExecuteScalar();
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static SqlCommand Commande(string query, SqlParameter[] parameters,
                                           CommandType commandType, SqlConnection con)
        {
            var cmd = new SqlCommand(query, con)
            {
                CommandType = commandType,
                CommandTimeout = DELAI_COMMANDE
            };

            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            return cmd;
        }
    }
}
