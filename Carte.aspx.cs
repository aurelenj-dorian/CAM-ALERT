using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using System.Web.UI;
using CamAlert.Helpers; // Assure-toi que Database est dans ce namespace

namespace CamAlert
{
    public partial class Carte : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Vérifier si l'utilisateur est administrateur
            if (Session["UserRole"] == null ||
                !Session["UserRole"].ToString().Equals("Administrateur", StringComparison.OrdinalIgnoreCase))
            {
                if (!Request.Url.AbsolutePath.EndsWith("Default.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("~/Default.aspx");
                    return;
                }
            }

            if (!IsPostBack)
            {
                var signalements = GetSignalementsFromDatabase();
                var serializer = new JavaScriptSerializer();
                string jsonSignalements = serializer.Serialize(signalements);
                ClientScript.RegisterStartupScript(this.GetType(), "signalementsData",
                    "var signalementsData = " + jsonSignalements + ";", true);
            }
        }

        private List<SignalementMap> GetSignalementsFromDatabase()
        {
            var signalements = new List<SignalementMap>();
            string query = @"
        SELECT 
            d.DossierID,
            d.Titre,
            d.Ville,
            t.Libelle AS TypeLibelle,
            g.Latitude,
            g.Longitude,
            d.DatePublication,
            d.Description
        FROM Dossiers d
        INNER JOIN TypesDossier t ON d.TypeID = t.TypeID
        INNER JOIN Geolocalisation g ON d.DossierID = g.DossierID
        WHERE d.EstPublie = 1
        ORDER BY d.DatePublication DESC";

            try
            {
                DataTable dt = Database.ExecuteQuery(query, null);
                foreach (DataRow row in dt.Rows)
                {
                    string type = row["TypeLibelle"].ToString() == "Disparition" ? "Disparu" : "Recherché";
                    string prenom = "";
                    string nom = row["Titre"].ToString();

                    signalements.Add(new SignalementMap
                    {
                        DossierID = Convert.ToInt32(row["DossierID"]),
                        Nom = nom,
                        Prenom = prenom,
                        Type = type,
                        Ville = row["Ville"]?.ToString() ?? "",
                        Latitude = Convert.ToDouble(row["Latitude"]),
                        Longitude = Convert.ToDouble(row["Longitude"]),
                        DateSignalement = Convert.ToDateTime(row["DatePublication"]).ToString("dd/MM/yyyy"),
                        Description = row["Description"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur GetSignalementsFromDatabase: " + ex.Message);
            }
            return signalements;
        }
    }

    public class SignalementMap
    {
        public int DossierID { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Type { get; set; }
        public string Ville { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string DateSignalement { get; set; }
        public string Description { get; set; }

        public string NomComplet
        {
            get { return string.IsNullOrEmpty(Prenom) ? Nom : Prenom + " " + Nom; }
        }
    }
}