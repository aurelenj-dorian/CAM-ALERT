using System;
using System.Data.SqlClient;
using System.IO;
using System.Web.UI;
using CamAlert.Helpers;
namespace CamAlert
{
    public partial class Signalement : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ChargerDossiers();

                // Pré-sélection si on vient d'une fiche
                string dossierParam = Request.QueryString["dossier"];
                if (!string.IsNullOrEmpty(dossierParam))
                    ddlDossier.SelectedValue = dossierParam;
            }
        }

        private void ChargerDossiers()
        {
            string query = "SELECT DossierID, Titre FROM Dossiers WHERE EstPublie = 1 ORDER BY Titre";
            System.Data.DataTable dt = Database.ExecuteQuery(query);

            ddlDossier.DataSource = dt;
            ddlDossier.DataTextField = "Titre";
            ddlDossier.DataValueField = "DossierID";
            ddlDossier.DataBind();

            ddlDossier.Items.Insert(0, new System.Web.UI.WebControls.ListItem(
                "-- Sélectionnez un dossier --", ""));
        }

        protected void btnEnvoyer_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            // Gestion fichier joint
            string fichierJoint = null;
            if (fuFichier.HasFile)
            {
                string extension = Path.GetExtension(fuFichier.FileName).ToLower();
                if (extension == ".jpg" || extension == ".jpeg" ||
                    extension == ".png" || extension == ".pdf")
                {
                    string dossierUpload = Server.MapPath("~/Uploads/");
                    if (!Directory.Exists(dossierUpload))
                        Directory.CreateDirectory(dossierUpload);

                    string nomFichier = DateTime.Now.Ticks + "_" + fuFichier.FileName;
                    fuFichier.SaveAs(dossierUpload + nomFichier);
                    fichierJoint = "/Uploads/" + nomFichier;
                }
            }

            string query = @"
                INSERT INTO Signalements 
                    (DossierID, NomDeclarant, Email, Message, FichierJoint, StatutID)
                VALUES 
                    (@DossierID, @Nom, @Email, @Message, @Fichier, 1)";

            SqlParameter[] parameters = {
                new SqlParameter("@DossierID", int.Parse(ddlDossier.SelectedValue)),
                new SqlParameter("@Nom",       string.IsNullOrEmpty(txtNom.Text)
                                               ? (object)DBNull.Value : txtNom.Text),
                new SqlParameter("@Email",     string.IsNullOrEmpty(txtEmail.Text)
                                               ? (object)DBNull.Value : txtEmail.Text),
                new SqlParameter("@Message",   txtMessage.Text),
                new SqlParameter("@Fichier",   fichierJoint == null
                                               ? (object)DBNull.Value : fichierJoint)
            };

            Database.ExecuteNonQuery(query, parameters);

            // Afficher message succès
            pnlForm.Visible = false;
            pnlSuccess.Visible = true;
        }
    }
}