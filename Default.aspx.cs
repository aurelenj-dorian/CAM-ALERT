using CamAlert.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using System.Web.UI;

namespace CamAlert
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Rien pour l'instant
            // Les dossiers sur l'accueil sont statiques (HTML)
        }

        [WebMethod]
        public static List<Dictionary<string, object>> SearchAllDossiers(string searchTerm)
        {
            var result = new List<Dictionary<string, object>>();
            string query = @"
        SELECT TOP 10 
            d.DossierID, 
            d.Titre, 
            d.Annee, 
            d.Ville,
            t.Libelle AS TypeLibelle,
            r.Nom AS RegionNom
        FROM Dossiers d
        INNER JOIN TypesDossier t ON d.TypeID = t.TypeID
        INNER JOIN Regions r ON d.RegionID = r.RegionID
        WHERE d.EstPublie = 1";

            if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Length >= 2)
            {
                query += @" AND (d.Titre LIKE @SearchTerm 
                      OR d.Ville LIKE @SearchTerm 
                      OR r.Nom LIKE @SearchTerm)";
            }
            query += " ORDER BY d.DatePublication DESC";

            SqlParameter[] parameters = null;
            if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Length >= 2)
            {
                parameters = new SqlParameter[] { new SqlParameter("@SearchTerm", "%" + searchTerm + "%") };
            }

            DataTable dt = Database.ExecuteQuery(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                result.Add(dict);
            }
            return result;
        }
    }
}
