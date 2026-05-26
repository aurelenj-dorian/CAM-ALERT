using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using CamAlert.Helpers;

namespace CamAlert.Admin
{
    public partial class Signalements : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserLogin"] == null)
            {
                Response.Redirect("~/Admin/Login.aspx");
                return;
            }

            if (!IsPostBack)
                ChargerSignalements();
        }

        private void ChargerSignalements()
        {
            string query = @"
                SELECT s.SignalementID, s.NomDeclarant, s.Email,
                       s.Message, s.DateSoumission, s.StatutID,
                       s.CommentaireInterne,
                       d.Titre  AS DossierTitre,
                       st.Libelle AS StatutLibelle
                FROM Signalements s
                INNER JOIN Dossiers d  ON s.DossierID = d.DossierID
                INNER JOIN Statuts  st ON s.StatutID  = st.StatutID
                ORDER BY s.DateSoumission DESC";

            DataTable dt = Database.ExecuteQuery(query);
            gvSignalements.DataSource = dt;
            gvSignalements.DataBind();
        }

        protected void gvSignalements_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Voir")
            {
                int id = int.Parse(e.CommandArgument.ToString());

                string query = @"
                    SELECT s.*, d.Titre AS DossierTitre, st.Libelle AS StatutLibelle
                    FROM Signalements s
                    INNER JOIN Dossiers d  ON s.DossierID = d.DossierID
                    INNER JOIN Statuts  st ON s.StatutID  = st.StatutID
                    WHERE s.SignalementID = @ID";

                DataTable dt = Database.ExecuteQuery(query,
                    new SqlParameter[] { new SqlParameter("@ID", id) });

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    hdnSignalementID.Value = id.ToString();
                    litDossier.Text = row["DossierTitre"].ToString();
                    litDeclarant.Text = string.IsNullOrEmpty(row["NomDeclarant"].ToString())
                                               ? "Anonyme" : row["NomDeclarant"].ToString();
                    litEmail.Text = string.IsNullOrEmpty(row["Email"].ToString())
                                               ? "Non renseigné" : row["Email"].ToString();
                    litDate.Text = Convert.ToDateTime(row["DateSoumission"])
                                               .ToString("dd/MM/yyyy HH:mm");
                    litMessageDetail.Text = row["Message"].ToString();
                    txtCommentaire.Text = row["CommentaireInterne"].ToString();
                    ddlStatut.SelectedValue = row["StatutID"].ToString();
                    pnlDetail.Visible = true;
                    pnlMessage.Visible = false;
                }
            }
        }

        protected void btnChangerStatut_Click(object sender, EventArgs e)
        {
            int id = int.Parse(hdnSignalementID.Value);

            Database.ExecuteNonQuery(
                "UPDATE Signalements SET StatutID = @Statut WHERE SignalementID = @ID",
                new SqlParameter[] {
                    new SqlParameter("@Statut", int.Parse(ddlStatut.SelectedValue)),
                    new SqlParameter("@ID",     id)
                });

            litMessage.Text = "✓ Statut mis à jour avec succès !";
            pnlMessage.Visible = true;
            pnlDetail.Visible = false;
            ChargerSignalements();
        }

        protected void btnSauvegarderCommentaire_Click(object sender, EventArgs e)
        {
            int id = int.Parse(hdnSignalementID.Value);

            Database.ExecuteNonQuery(
                "UPDATE Signalements SET CommentaireInterne = @Commentaire WHERE SignalementID = @ID",
                new SqlParameter[] {
                    new SqlParameter("@Commentaire", txtCommentaire.Text),
                    new SqlParameter("@ID",          id)
                });

            litMessage.Text = "✓ Commentaire sauvegardé !";
            pnlMessage.Visible = true;
            ChargerSignalements();
        }

        protected void btnFermerDetail_Click(object sender, EventArgs e)
        {
            pnlDetail.Visible = false;
        }
    }
}