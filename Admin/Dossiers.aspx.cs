using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using CamAlert.Helpers;

namespace CamAlert.Admin
{
    public partial class Dossiers : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Vérifier connexion admin
            if (Session["UserLogin"] == null)
            {
                Response.Redirect("~/Admin/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                ChargerDossiers();
                ChargerTypes();
                ChargerRegions();
            }
        }

        private void ChargerDossiers()
        {
            string query = @"
                SELECT d.DossierID, d.Titre, d.Annee, d.EstPublie,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                ORDER BY d.DatePublication DESC";

            DataTable dt = Database.ExecuteQuery(query);
            gvDossiers.DataSource = dt;
            gvDossiers.DataBind();
        }

        private void ChargerTypes()
        {
            DataTable dt = Database.ExecuteQuery("SELECT TypeID, Libelle FROM TypesDossier");
            ddlType.DataSource = dt;
            ddlType.DataTextField = "Libelle";
            ddlType.DataValueField = "TypeID";
            ddlType.DataBind();
        }

        private void ChargerRegions()
        {
            DataTable dt = Database.ExecuteQuery("SELECT RegionID, Nom FROM Regions ORDER BY Nom");
            ddlRegion.DataSource = dt;
            ddlRegion.DataTextField = "Nom";
            ddlRegion.DataValueField = "RegionID";
            ddlRegion.DataBind();
        }

        protected void btnNouveauDossier_Click(object sender, EventArgs e)
        {
            hdnDossierID.Value = "0";
            litTitreForm.Text = "Nouveau dossier";
            txtTitre.Text = "";
            txtVille.Text = "";
            txtAnnee.Text = "";
            txtDescription.Text = "";
            txtSignes.Text = "";
            pnlFormulaire.Visible = true;
            pnlMessage.Visible = false;
        }

        protected void btnAnnuler_Click(object sender, EventArgs e)
        {
            pnlFormulaire.Visible = false;
        }

        protected void btnSauvegarder_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            int dossierID = int.Parse(hdnDossierID.Value);

            // Gestion image
            string imageUrl = null;
            if (fuImage.HasFile)
            {
                string ext = Path.GetExtension(fuImage.FileName).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    string dossierImg = Server.MapPath("~/Admin/image/");
                    if (!Directory.Exists(dossierImg))
                        Directory.CreateDirectory(dossierImg);

                    string nomFichier = DateTime.Now.Ticks + "_" + fuImage.FileName;
                    fuImage.SaveAs(dossierImg + nomFichier);
                    imageUrl = "/Admin/image/" + nomFichier;
                }
            }

            if (dossierID == 0)
            {
                // INSERT
                string query = @"
                    INSERT INTO Dossiers 
                        (Titre, Description, TypeID, RegionID, Annee, 
                         ImageUrl, SignesDistinctifs, Ville, EstPublie)
                    VALUES 
                        (@Titre, @Description, @TypeID, @RegionID, @Annee,
                         @ImageUrl, @Signes, @Ville, @Publie)";

                SqlParameter[] p = {
                    new SqlParameter("@Titre",       txtTitre.Text),
                    new SqlParameter("@Description", txtDescription.Text),
                    new SqlParameter("@TypeID",      int.Parse(ddlType.SelectedValue)),
                    new SqlParameter("@RegionID",    int.Parse(ddlRegion.SelectedValue)),
                    new SqlParameter("@Annee",       string.IsNullOrEmpty(txtAnnee.Text)
                                                     ? (object)DBNull.Value
                                                     : int.Parse(txtAnnee.Text)),
                    new SqlParameter("@ImageUrl",    imageUrl == null
                                                     ? (object)DBNull.Value : imageUrl),
                    new SqlParameter("@Signes",      txtSignes.Text),
                    new SqlParameter("@Ville",       txtVille.Text),
                    new SqlParameter("@Publie",      int.Parse(ddlPublie.SelectedValue))
                };

                Database.ExecuteNonQuery(query, p);
                litMessage.Text = "✓ Dossier ajouté avec succès !";
            }
            else
            {
                // UPDATE
                string query = @"
                    UPDATE Dossiers SET
                        Titre             = @Titre,
                        Description       = @Description,
                        TypeID            = @TypeID,
                        RegionID          = @RegionID,
                        Annee             = @Annee,
                        SignesDistinctifs  = @Signes,
                        Ville             = @Ville,
                        EstPublie         = @Publie"
                    + (imageUrl != null ? ", ImageUrl = @ImageUrl" : "") +
                    " WHERE DossierID = @ID";

                var liste = new System.Collections.Generic.List<SqlParameter> {
                    new SqlParameter("@Titre",       txtTitre.Text),
                    new SqlParameter("@Description", txtDescription.Text),
                    new SqlParameter("@TypeID",      int.Parse(ddlType.SelectedValue)),
                    new SqlParameter("@RegionID",    int.Parse(ddlRegion.SelectedValue)),
                    new SqlParameter("@Annee",       string.IsNullOrEmpty(txtAnnee.Text)
                                                     ? (object)DBNull.Value
                                                     : int.Parse(txtAnnee.Text)),
                    new SqlParameter("@Signes",      txtSignes.Text),
                    new SqlParameter("@Ville",       txtVille.Text),
                    new SqlParameter("@Publie",      int.Parse(ddlPublie.SelectedValue)),
                    new SqlParameter("@ID",          dossierID)
                };

                if (imageUrl != null)
                    liste.Add(new SqlParameter("@ImageUrl", imageUrl));

                Database.ExecuteNonQuery(query, liste.ToArray());
                litMessage.Text = "✓ Dossier modifié avec succès !";
            }

            pnlFormulaire.Visible = false;
            pnlMessage.Visible = true;
            ChargerDossiers();
        }

        protected void gvDossiers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id = int.Parse(e.CommandArgument.ToString());

            if (e.CommandName == "Supprimer")
            {
                // D'abord supprimer les signalements liés
                Database.ExecuteNonQuery(
                    "DELETE FROM Signalements WHERE DossierID = @ID",
                    new SqlParameter[] { new SqlParameter("@ID", id) });

                // Ensuite supprimer le dossier
                Database.ExecuteNonQuery(
                    "DELETE FROM Dossiers WHERE DossierID = @ID",
                    new SqlParameter[] { new SqlParameter("@ID", id) });

                litMessage.Text = "✓ Dossier supprimé.";
                pnlMessage.Visible = true;
                pnlFormulaire.Visible = false;
                ChargerDossiers();
            }
            else if (e.CommandName == "Modifier")
            {
                string query = @"
                    SELECT * FROM Dossiers WHERE DossierID = @ID";

                DataTable dt = Database.ExecuteQuery(query,
                    new SqlParameter[] { new SqlParameter("@ID", id) });

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    hdnDossierID.Value = id.ToString();
                    litTitreForm.Text = "Modifier le dossier";
                    txtTitre.Text = row["Titre"].ToString();
                    txtDescription.Text = row["Description"].ToString();
                    txtVille.Text = row["Ville"].ToString();
                    txtAnnee.Text = row["Annee"].ToString();
                    txtSignes.Text = row["SignesDistinctifs"].ToString();
                    ddlType.SelectedValue = row["TypeID"].ToString();
                    ddlRegion.SelectedValue = row["RegionID"].ToString();
                    ddlPublie.SelectedValue = Convert.ToBoolean(row["EstPublie"]) ? "1" : "0";
                    pnlFormulaire.Visible = true;
                    pnlMessage.Visible = false;
                }
            }
        }

        protected void btnDeconnexion_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Response.Redirect("~/Admin/Login.aspx");
        }
    }
}