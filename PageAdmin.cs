using System;
using System.Web;
using CamAlert.Helpers;

namespace CamAlert
{
    /// <summary>
    /// Classe de base des pages d'administration.
    ///
    /// Le contrôle d'accès était recopié dans chaque code-behind :
    ///
    ///     if (Session["UserLogin"] == null)
    ///     {
    ///         Response.Redirect("~/Admin/Login.aspx");
    ///         return;
    ///     }
    ///
    /// Une page qui oubliait ce bloc était publiquement accessible, et rien
    /// n'imposait de vérifier le rôle. Le contrôle est désormais fait une seule
    /// fois, dans <see cref="OnInit"/>, donc avant tout traitement de la page et
    /// avant le déclenchement du moindre événement de contrôle : il suffit
    /// d'hériter de cette classe pour qu'une page soit protégée.
    ///
    /// Elle impose aussi le changement du mot de passe initial : tant que le
    /// compte porte <c>DoitChangerMotDePasse</c>, toute page d'administration
    /// renvoie vers l'écran de changement.
    /// </summary>
    public abstract class PageAdmin : PageBase
    {
        /// <summary>
        /// Rôles autorisés sur cette page. <c>null</c> ou tableau vide = tout
        /// agent connecté. Une page redéfinit cette propriété pour se restreindre,
        /// par exemple aux administrateurs.
        /// </summary>
        protected virtual string[] RolesAutorises
        {
            get { return null; }
        }

        /// <summary>
        /// Pages qui doivent rester accessibles pendant le changement de mot de
        /// passe imposé, sans quoi la redirection bouclerait sur elle-même.
        /// </summary>
        protected virtual bool ExempteDeChangementMotDePasse
        {
            get { return false; }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (!EstConnecte)
            {
                RedirigerVersConnexion();
                return;
            }

            // Verrou anti-fixation : le navigateur doit présenter le jeton
            // déposé à la connexion. Une session détournée échoue ici.
            if (!JetonSession.EstValide(Session, Request))
            {
                Journal.Enregistrer(Journal.Actions.Erreur, LoginConnecte,
                    details: "Jeton de session absent ou incorrect sur "
                             + CheminDemande(),
                    adresseIp: IpClient);

                Session.Clear();
                RedirigerVersConnexion();
                return;
            }

            if (!RoleAutorise())
            {
                Journal.Enregistrer(Journal.Actions.Erreur, LoginConnecte,
                    details: "Accès refusé à " + CheminDemande()
                             + " (rôle « " + (RoleConnecte ?? "aucun") + " »)",
                    adresseIp: IpClient);

                // Même page que pour un visiteur non connecté : on ne révèle
                // pas qu'une page existe à cette adresse.
                RedirigerVersConnexion();
                return;
            }

            if (DoitChangerMotDePasse && !ExempteDeChangementMotDePasse)
            {
                Response.Redirect("~/Admin/MotDePasse.aspx?initial=1", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        /// <summary>
        /// Vrai si le compte connecté doit changer son mot de passe initial.
        /// </summary>
        protected bool DoitChangerMotDePasse
        {
            get
            {
                var valeur = Session[SESSION_DOIT_CHANGER] as bool?;
                return valeur.HasValue && valeur.Value;
            }
        }

        private bool RoleAutorise()
        {
            var roles = RolesAutorises;
            if (roles == null || roles.Length == 0) return true;

            var role = RoleConnecte;
            if (string.IsNullOrEmpty(role)) return false;

            foreach (var autorise in roles)
                if (string.Equals(autorise, role, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        private void RedirigerVersConnexion()
        {
            // Response.Redirect(url, false) + CompleteRequest : on interrompt le
            // traitement sans lever ThreadAbortException, qui remplirait le
            // journal d'erreurs sans raison.
            var retour = Server.UrlEncode(CheminDemande());
            Response.Redirect("~/Admin/Login.aspx?retour=" + retour, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private string CheminDemande()
        {
            try
            {
                return Request.Unvalidated.RawUrl ?? Request.AppRelativeCurrentExecutionFilePath;
            }
            catch
            {
                return "~/";
            }
        }
    }
}
