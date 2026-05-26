using CamAlert.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CamAlert
{
    public partial class Dossiers : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ChargerDossiers();
            }
        }

        private void ChargerDossiers()
        {
            string type = ddlType.SelectedValue;
            string annee = ddlAnnee.SelectedValue;

            string query = @"
                SELECT d.DossierID, d.Titre, d.Annee, d.Ville,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID  = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                WHERE d.EstPublie = 1";

            if (!string.IsNullOrEmpty(type))
                query += " AND d.TypeID = @TypeID";
            if (!string.IsNullOrEmpty(annee))
                query += " AND d.Annee = @Annee";

            query += " ORDER BY d.DatePublication DESC";

            var liste = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(type))
                liste.Add(new SqlParameter("@TypeID", int.Parse(type)));
            if (!string.IsNullOrEmpty(annee))
                liste.Add(new SqlParameter("@Annee", int.Parse(annee)));

            SqlParameter[] parameters = liste.Count > 0 ? liste.ToArray() : null;

            DataTable dt = Database.ExecuteQuery(query, parameters);
            gvDossiers.DataSource = dt;
            gvDossiers.DataBind();
        }

       /* protected void Filtres_Changed(object sender, EventArgs e)
        {
            gvDossiers.PageIndex = 0;
            ChargerDossiers();
        }*/

        protected void btnRecherche_Click(object sender, EventArgs e)
        {
            gvDossiers.PageIndex = 0;
            ChargerDossiers();
        }

        protected void gvDossiers_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvDossiers.PageIndex = e.NewPageIndex;
            ChargerDossiers();
        }

        [WebMethod]
        public static List<object> SearchDossiers(string searchTerm, string typeId, string annee)
        {
            var result = new List<object>();
            string query = @"
                SELECT TOP 30 
                    d.DossierID, d.Titre, d.Annee, d.Ville,
                    t.Libelle AS TypeLibelle, r.Nom AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID = t.TypeID
                INNER JOIN Regions r ON d.RegionID = r.RegionID
                WHERE d.EstPublie = 1";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(typeId) && typeId != "")
            {
                query += " AND d.TypeID = @TypeID";
                parameters.Add(new SqlParameter("@TypeID", int.Parse(typeId)));
            }
            if (!string.IsNullOrEmpty(annee) && annee != "")
            {
                query += " AND d.Annee = @Annee";
                parameters.Add(new SqlParameter("@Annee", int.Parse(annee)));
            }
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += @" AND (d.Titre LIKE @SearchTerm OR d.Ville LIKE @SearchTerm OR r.Nom LIKE @SearchTerm)";
                parameters.Add(new SqlParameter("@SearchTerm", "%" + searchTerm + "%"));
            }

            query += " ORDER BY d.DatePublication DESC";

            DataTable dt = Database.ExecuteQuery(query, parameters.ToArray());
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new
                {
                    DossierID = row["DossierID"],
                    Titre = row["Titre"],
                    Annee = row["Annee"],
                    Ville = row["Ville"],
                    TypeLibelle = row["TypeLibelle"],
                    RegionNom = row["RegionNom"]
                });
            }
            return result;
        }

        [WebMethod]
        public static List<object> GetSuggestions(string searchTerm, string typeId, string annee)
        {
            var result = new List<object>();
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 2)
                return result;

            string query = @"
                SELECT TOP 10 d.DossierID, d.Titre, d.Annee, d.Ville, t.Libelle AS TypeLibelle
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID = t.TypeID
                WHERE d.EstPublie = 1 AND d.Titre LIKE @SearchTerm";

            var parameters = new List<SqlParameter> { new SqlParameter("@SearchTerm", "%" + searchTerm + "%") };

            if (!string.IsNullOrEmpty(typeId) && typeId != "")
            {
                query += " AND d.TypeID = @TypeID";
                parameters.Add(new SqlParameter("@TypeID", int.Parse(typeId)));
            }
            if (!string.IsNullOrEmpty(annee) && annee != "")
            {
                query += " AND d.Annee = @Annee";
                parameters.Add(new SqlParameter("@Annee", int.Parse(annee)));
            }

            query += " ORDER BY d.DatePublication DESC";

            DataTable dt = Database.ExecuteQuery(query, parameters.ToArray());
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new
                {
                    DossierID = row["DossierID"],
                    Titre = row["Titre"],
                    Annee = row["Annee"],
                    Ville = row["Ville"],
                    TypeLibelle = row["TypeLibelle"]
                });
            }
            return result;
        }
    }
}