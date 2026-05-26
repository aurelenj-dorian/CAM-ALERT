using System;
using System.Data;
using System.Web.UI;
using CamAlert.Helpers;
namespace CamAlert
{
    public partial class DossierDetail : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string idParam = Request.QueryString["id"];

                if (string.IsNullOrEmpty(idParam))
                {
                    Response.Redirect("Dossiers.aspx");
                    return;
                }

                int dossierID;
                if (!int.TryParse(idParam, out dossierID))
                {
                    Response.Redirect("Dossiers.aspx");
                    return;
                }

                ChargerDossier(dossierID);
            }
        }

        private void ChargerDossier(int id)
        {
            string query = @"
                SELECT d.DossierID, d.Titre, d.Description,
                       d.Annee, d.Ville, d.ImageUrl,
                       d.SignesDistinctifs, d.DatePublication,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                WHERE d.DossierID = @ID AND d.EstPublie = 1";

            System.Data.SqlClient.SqlParameter[] parameters = {
                new System.Data.SqlClient.SqlParameter("@ID", id)
            };

            DataTable dt = Database.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0)
            {
                Response.Redirect("Dossiers.aspx");
                return;
            }

            DataRow row = dt.Rows[0];

            // Remplir les champs
            litTitre.Text = row["Titre"].ToString();
            litRef.Text = "DOS-" + row["DossierID"].ToString().PadLeft(4, '0');
            litAnnee.Text = row["Annee"].ToString();
            litRegion.Text = row["RegionNom"].ToString();
            litVille.Text = row["Ville"].ToString();
            litDescription.Text = row["Description"].ToString();
            litSignes.Text = row["SignesDistinctifs"].ToString();
            litDate.Text = Convert.ToDateTime(row["DatePublication"])
                                    .ToString("dd/MM/yyyy");

            // Badge type
            string type = row["TypeLibelle"].ToString();
            if (type == "Disparition")
                litBadge.Text = "<span class='badge badge-disparition'>Disparition</span><br/><br/>";
            else
                litBadge.Text = "<span class='badge badge-recherche'>Recherche judiciaire</span><br/><br/>";

            // Image
            // Image
            string imageUrl = row["ImageUrl"].ToString();
            if (!string.IsNullOrEmpty(imageUrl))
                imgDossier.ImageUrl = imageUrl;
            else
                imgDossier.ImageUrl = "/Admin/image/téléchargement.jpg";

            // Titre de la page
            Page.Title = "CAM-ALERT – " + row["Titre"].ToString();
        }
    }
}