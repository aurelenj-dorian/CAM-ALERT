using CamAlert.Helpers;
using System;
using System.Data;
using System.Data.SqlClient;

namespace CamAlert.Admin
{
    /// <summary>
    /// Changement de son propre mot de passe.
    ///
    /// Cette page n'existait pas : un agent ne pouvait pas changer son mot de
    /// passe, et le compte livré avec la base gardait indéfiniment le mot de
    /// passe d'installation — lequel figure dans un dépôt public.
    ///
    /// Elle est exemptée de la redirection « changement imposé » de
    /// <see cref="PageAdmin"/>, sans quoi la redirection boucherait sur
    /// elle-même.
    /// </summary>
    public partial class MotDePasse : PageAdmin
    {
        protected override bool ExempteDeChangementMotDePasse
        {
            get { return true; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            bool initial = Request.QueryString["initial"] == "1" || DoitChangerMotDePasse;

            if (initial)
            {
                litTitre.Text = "Changement de mot de passe requis";
                litConsigne.Text =
                    "<p>Ce compte utilise encore le mot de passe défini à " +
                    "l'installation. Choisissez-en un nouveau pour accéder à " +
                    "l'administration.</p>";
                lnkRetour.Visible = false;
            }
            else
            {
                litConsigne.Text =
                    "<p>Choisissez un mot de passe différent de l'actuel.</p>";
            }
        }

        protected void btnEnregistrer_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string ancien = txtAncien.Text ?? string.Empty;
            string nouveau = txtNouveau.Text ?? string.Empty;
            string login = LoginConnecte;
            int? id = IdConnecte;

            if (string.IsNullOrEmpty(login) || !id.HasValue)
            {
                AfficherErreur("Session expirée. Reconnectez-vous.");
                return;
            }

            if (string.Equals(ancien, nouveau, StringComparison.Ordinal))
            {
                AfficherErreur("Le nouveau mot de passe doit être différent de l'actuel.");
                return;
            }

            // Robustesse vérifiée côté serveur : le navigateur ne contrôle
            // que la longueur, et il est contournable.
            string refus = PolitiqueMotDePasse.Valider(nouveau, login);
            if (refus != null)
            {
                AfficherErreur(refus);
                return;
            }

            DataRow compte;
            try
            {
                compte = ChargerCompte(id.Value);
            }
            catch (Exception ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                AfficherErreur("Service momentanément indisponible. Réessayez.");
                return;
            }

            if (compte == null)
            {
                AfficherErreur("Compte introuvable. Reconnectez-vous.");
                return;
            }

            if (!HacheurMotDePasse.Verifier(ancien, compte["MotDePasse"].ToString()))
            {
                Journal.Enregistrer(Journal.Actions.ConnexionEchouee, login,
                    "Utilisateur", id.Value,
                    "Mot de passe actuel incorrect lors d'un changement",
                    IpClient);

                AfficherErreur("Mot de passe actuel incorrect.");
                return;
            }

            try
            {
                Database.ExecuteNonQuery(@"
                    UPDATE Utilisateurs
                       SET MotDePasse            = @Hash,
                           DoitChangerMotDePasse = 0
                     WHERE UtilisateurID = @ID",
                    new[]
                    {
                        new SqlParameter("@Hash", SqlDbType.NVarChar, 256)
                            { Value = HacheurMotDePasse.Hacher(nouveau) },
                        new SqlParameter("@ID", SqlDbType.Int) { Value = id.Value }
                    });
            }
            catch (Exception ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                AfficherErreur("Le mot de passe n'a pas pu être enregistré. Réessayez.");
                return;
            }

            Session[SESSION_DOIT_CHANGER] = false;

            // Le mot de passe a changé : on repart sur un nouveau jeton, pour
            // qu'une session ouverte ailleurs avec l'ancien ne reste pas valide.
            JetonSession.Demarrer(Session, Response, Request.IsSecureConnection);

            Journal.Enregistrer(Journal.Actions.Modification, login,
                "Utilisateur", id.Value, "Changement de mot de passe", IpClient);

            pnlFormulaire.Visible = false;
            pnlError.Visible = false;
            lnkRetour.Visible = true;
            lblSucces.Text = "Mot de passe modifié. "
                           + "Il sera demandé à votre prochaine connexion.";
            pnlSucces.Visible = true;
            litConsigne.Text = string.Empty;
        }

        private static DataRow ChargerCompte(int utilisateurId)
        {
            DataTable dt = Database.ExecuteQuery(
                "SELECT UtilisateurID, Login, MotDePasse FROM Utilisateurs " +
                "WHERE UtilisateurID = @ID AND EstActif = 1",
                new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = utilisateurId } });

            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        private void AfficherErreur(string message)
        {
            lblError.Text = Server.HtmlEncode(message);
            pnlError.Visible = true;
            pnlSucces.Visible = false;
        }
    }
}
