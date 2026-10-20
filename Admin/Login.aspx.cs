using CamAlert.Helpers;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace CamAlert.Admin
{
    /// <summary>
    /// Connexion à l'espace d'administration.
    ///
    /// Corrections par rapport à la version précédente :
    ///   • les mots de passe étaient comparés en SHA-256 sans sel ; la
    ///     vérification passe par <see cref="HacheurMotDePasse"/>, qui accepte
    ///     encore l'ancien format et le réécrit en PBKDF2 à la volée ;
    ///   • aucune limite n'existait sur le nombre d'essais : un script pouvait
    ///     tester des mots de passe indéfiniment ;
    ///   • le message d'erreur affichait <c>ex.Message</c>, donc la chaîne de
    ///     connexion ou la requête SQL en cas de panne de la base ;
    ///   • la session n'était pas réinitialisée à la connexion, ce qui laissait
    ///     la porte ouverte à la fixation de session ;
    ///   • rien n'était journalisé, ni les réussites ni les échecs.
    /// </summary>
    public partial class Login : PageBase
    {
        private const string PORTEE = "connexion";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (EstConnecte && JetonSession.EstValide(Session, Request))
            {
                Response.Redirect(DestinationApresConnexion(), false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string login = (txtLogin.Text ?? string.Empty).Trim();
            string motDePasse = txtPassword.Text ?? string.Empty;

            if (login.Length == 0 || motDePasse.Length == 0)
            {
                AfficherErreur("Veuillez saisir un identifiant et un mot de passe.");
                return;
            }

            // Deux compteurs : l'un par couple (identifiant, IP) pour contrer
            // le test de mots de passe sur un compte précis, l'autre par IP
            // seule pour contrer le balayage de plusieurs identifiants.
            string cleCompte = LimiteurTentatives.Cle(PORTEE, login, IpClient);
            string cleIp = LimiteurTentatives.CleIp(PORTEE, IpClient);

            int attente;
            if (LimiteurTentatives.EstBloque(cleCompte, out attente)
                || LimiteurTentatives.EstBloque(cleIp, out attente))
            {
                AfficherErreur(LimiteurTentatives.MessageBlocage(attente));
                return;
            }

            DataRow compte;
            try
            {
                compte = ChargerCompte(login);
            }
            catch (Exception ex)
            {
                // L'exception part au journal, pas à l'écran : son message
                // contient le serveur, la base et la requête.
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                AfficherErreur("Service momentanément indisponible. "
                               + "Réessayez dans quelques instants.");
                return;
            }

            bool motDePasseCorrect = compte != null
                && HacheurMotDePasse.Verifier(motDePasse,
                                              compte["MotDePasse"].ToString());

            // Même message et même traitement pour un identifiant inconnu, un
            // mot de passe faux et un compte désactivé : rien ne doit permettre
            // de deviner quels comptes existent.
            if (!motDePasseCorrect || !Convert.ToBoolean(compte["EstActif"]))
            {
                EnregistrerEchec(cleCompte, cleIp, login, compte);
                AfficherErreur("Identifiant ou mot de passe incorrect.");
                return;
            }

            LimiteurTentatives.Reussite(cleCompte);
            LimiteurTentatives.Reussite(cleIp);

            OuvrirSession(compte, motDePasse);

            Response.Redirect(DestinationApresConnexion(), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ─────────────────────────────────────────────────────

        private static DataRow ChargerCompte(string login)
        {
            const string requete = @"
                SELECT u.UtilisateurID, u.Login, u.MotDePasse, u.EstActif,
                       u.DoitChangerMotDePasse, u.NomComplet,
                       r.Libelle AS Role
                FROM Utilisateurs u
                INNER JOIN Roles r ON u.RoleID = r.RoleID
                WHERE u.Login = @Login";

            var parametres = new[]
            {
                new SqlParameter("@Login", SqlDbType.NVarChar, 50) { Value = login }
            };

            DataTable dt = Database.ExecuteQuery(requete, parametres);
            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        private void OuvrirSession(DataRow compte, string motDePasse)
        {
            int utilisateurId = Convert.ToInt32(compte["UtilisateurID"]);
            string login = compte["Login"].ToString();
            string role = compte["Role"].ToString();
            bool doitChanger = Convert.ToBoolean(compte["DoitChangerMotDePasse"]);
            string hashStocke = compte["MotDePasse"].ToString();

            // Migration transparente : ancien hash SHA-256, ou coût PBKDF2
            // devenu inférieur au coût courant. Le mot de passe de l'agent ne
            // change pas, seul son stockage évolue.
            if (HacheurMotDePasse.NecessiteRehachage(hashStocke))
                ReecrireHash(utilisateurId, motDePasse);

            // Session repartie de zéro, puis jeton anti-fixation : un
            // identifiant de session imposé avant la connexion devient
            // inutilisable.
            Session.Clear();
            Session[SESSION_UTILISATEUR_ID] = utilisateurId;
            Session[SESSION_LOGIN] = login;
            Session[SESSION_ROLE] = role;
            Session[SESSION_DOIT_CHANGER] = doitChanger;

            JetonSession.Demarrer(Session, Response, Request.IsSecureConnection);

            try
            {
                Database.ExecuteNonQuery(
                    "UPDATE Utilisateurs SET DateDerniereConnexion = SYSDATETIME() " +
                    "WHERE UtilisateurID = @ID",
                    new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = utilisateurId } });
            }
            catch (Exception ex)
            {
                // Une date de connexion non enregistrée ne doit pas empêcher
                // de travailler.
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
            }

            Journal.Enregistrer(Journal.Actions.Connexion, login,
                                "Utilisateur", utilisateurId,
                                "Connexion réussie (rôle « " + role + " »)",
                                IpClient);
        }

        private static void ReecrireHash(int utilisateurId, string motDePasse)
        {
            try
            {
                Database.ExecuteNonQuery(
                    "UPDATE Utilisateurs SET MotDePasse = @Hash WHERE UtilisateurID = @ID",
                    new[]
                    {
                        new SqlParameter("@Hash", SqlDbType.NVarChar, 256)
                            { Value = HacheurMotDePasse.Hacher(motDePasse) },
                        new SqlParameter("@ID", SqlDbType.Int) { Value = utilisateurId }
                    });
            }
            catch (Exception ex)
            {
                // Échec de migration : le hash sera réécrit à la prochaine
                // connexion. On ne bloque pas l'agent pour autant.
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
            }
        }

        private void EnregistrerEchec(string cleCompte, string cleIp,
                                      string login, DataRow compte)
        {
            LimiteurTentatives.CompterTentative(cleCompte, LimiteurTentatives.MAX_CONNEXION);
            LimiteurTentatives.CompterTentative(cleIp, LimiteurTentatives.MAX_CONNEXION * 3);

            string motif = compte == null
                ? "identifiant inconnu"
                : (Convert.ToBoolean(compte["EstActif"])
                       ? "mot de passe incorrect"
                       : "compte désactivé");

            Journal.Enregistrer(Journal.Actions.ConnexionEchouee, login,
                                details: "Échec de connexion : " + motif,
                                adresseIp: IpClient);
        }

        /// <summary>
        /// Page à afficher après la connexion. Le paramètre <c>retour</c> est
        /// validé : rediriger vers une valeur arbitraire permettrait d'envoyer
        /// l'agent sur une fausse page après son authentification.
        /// </summary>
        private string DestinationApresConnexion()
        {
            const string defaut = "~/Admin/Dossiers.aspx";

            var doitChanger = Session[SESSION_DOIT_CHANGER] as bool?;
            if (doitChanger.HasValue && doitChanger.Value)
                return "~/Admin/MotDePasse.aspx?initial=1";

            var retour = Request.QueryString["retour"];
            return UrlLocale.OuParDefaut(retour, defaut);
        }

        private void AfficherErreur(string message)
        {
            lblError.Text = Server.HtmlEncode(message);
            pnlError.Visible = true;
        }
    }
}
