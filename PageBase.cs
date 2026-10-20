using System;
using System.Web;
using System.Web.UI;
using CamAlert.Helpers;

namespace CamAlert
{
    /// <summary>
    /// Classe de base de toutes les pages du site.
    ///
    /// Elle apporte la protection anti-CSRF du ViewState. ASP.NET signe le
    /// ViewState, mais sans <see cref="Page.ViewStateUserKey"/> cette signature
    /// n'est liée à aucun utilisateur : un attaquant pouvait récupérer le
    /// ViewState d'un formulaire d'administration et le faire rejouer par le
    /// navigateur d'un administrateur connecté. En rattachant la clé à la
    /// session, un ViewState produit dans une session ne vaut plus rien dans
    /// une autre.
    ///
    /// Elle expose aussi les raccourcis dont les pages ont besoin
    /// (<see cref="IpClient"/>, <see cref="LoginConnecte"/>) pour éviter que
    /// chaque code-behind ne réinvente l'accès à la session.
    /// </summary>
    public abstract class PageBase : Page
    {
        /// <summary>Clé de session portant le login de l'agent connecté.</summary>
        public const string SESSION_LOGIN = "UserLogin";

        /// <summary>Clé de session portant le rôle de l'agent connecté.</summary>
        public const string SESSION_ROLE = "UserRole";

        /// <summary>Clé de session portant l'identifiant du compte connecté.</summary>
        public const string SESSION_UTILISATEUR_ID = "UserId";

        /// <summary>
        /// Clé de session signalant que le mot de passe initial doit être changé
        /// avant tout autre accès à l'administration.
        /// </summary>
        public const string SESSION_DOIT_CHANGER = "UserDoitChangerMotDePasse";

        /// <summary>
        /// Valeur stockée en session dès le premier affichage. Sans elle, une
        /// session vide reçoit un nouvel identifiant à chaque requête, et
        /// ViewStateUserKey ne correspondrait plus au moment du renvoi du
        /// formulaire — toutes les soumissions échoueraient.
        /// </summary>
        private const string SESSION_ANCRAGE = "AncrageSession";

        protected override void OnInit(EventArgs e)
        {
            AncrerSession();

            // Doit être affecté dans Init : plus tard, le ViewState est déjà
            // en cours de désérialisation et la clé serait ignorée.
            ViewStateUserKey = Session.SessionID;

            base.OnInit(e);
        }

        /// <summary>Adresse IP du client, pour le journal et la limitation d'abus.</summary>
        protected string IpClient
        {
            get
            {
                try
                {
                    return Request.UserHostAddress;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>Login de l'agent connecté, ou null.</summary>
        protected string LoginConnecte
        {
            get { return Session[SESSION_LOGIN] as string; }
        }

        /// <summary>Rôle de l'agent connecté, ou null.</summary>
        protected string RoleConnecte
        {
            get { return Session[SESSION_ROLE] as string; }
        }

        /// <summary>Identifiant du compte connecté, ou null.</summary>
        protected int? IdConnecte
        {
            get { return Session[SESSION_UTILISATEUR_ID] as int?; }
        }

        /// <summary>Vrai si un agent est connecté.</summary>
        protected bool EstConnecte
        {
            get { return !string.IsNullOrEmpty(LoginConnecte); }
        }

        /// <summary>
        /// Vrai si l'agent connecté a le rôle « Administrateur ».
        /// Le libellé vient de la table Roles : ne pas le renommer sans adapter
        /// la base.
        /// </summary>
        protected bool EstAdministrateur
        {
            get
            {
                return string.Equals(RoleConnecte, "Administrateur",
                                     StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Ferme la session proprement : contenu vidé, session abandonnée,
        /// cookie de session supprimé côté navigateur.
        /// </summary>
        protected void Deconnecter()
        {
            var login = LoginConnecte;

            JetonSession.Terminer(Session, Response, Request.IsSecureConnection);

            Session.Clear();
            Session.Abandon();

            // Sans cette suppression, le navigateur continue de présenter
            // l'ancien identifiant de session.
            var suppression = new HttpCookie("ASP.NET_SessionId", string.Empty)
            {
                Expires = new DateTime(1999, 1, 1),
                HttpOnly = true,
                Path = "/"
            };
            Response.Cookies.Add(suppression);

            if (!string.IsNullOrEmpty(login))
                Journal.Enregistrer(Journal.Actions.Deconnexion, login,
                                    adresseIp: IpClient);
        }

        private void AncrerSession()
        {
            try
            {
                if (Session != null && Session[SESSION_ANCRAGE] == null)
                    Session[SESSION_ANCRAGE] = DateTime.UtcNow.Ticks;
            }
            catch (HttpException)
            {
                // L'état de session n'est pas disponible sur cette requête
                // (gestionnaire sans session) : rien à ancrer.
            }
        }
    }
}
