using CamAlert.Helpers;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using System.Web.UI;

namespace CamAlert
{
    public partial class Site : System.Web.UI.MasterPage //   MasterPage 
    {
        // Propriété publique pour savoir si l'utilisateur est administrateur
        public bool IsAdmin { get; private set; }
        protected void Page_Load(object sender, EventArgs e)
        {

            // Vérifier si l'utilisateur est connecté et a le rôle "Administrateur"
            IsAdmin = Session["UserRole"] != null &&
                      Session["UserRole"].ToString().Equals("Administrateur", StringComparison.OrdinalIgnoreCase);
        }

        [WebMethod]
        public static DataTable SearchAllDossiers(string searchTerm)
        {
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

            var parameters = new SqlParameter[] { };
            if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Length >= 2)
            {
                parameters = new SqlParameter[] { new SqlParameter("@SearchTerm", "%" + searchTerm + "%") };
            }

            return Database.ExecuteQuery(query, parameters);
        }

    }
}