using CamAlert.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Script.Serialization;

namespace CamAlert
{
    /// <summary>
    /// Carte interactive des dossiers géolocalisés.
    ///
    /// La page est réservée aux administrateurs — c'est déjà l'intention du
    /// menu, qui n'affiche le lien qu'à eux. Le contrôle était écrit ici à la
    /// main, avec une condition inutile sur l'URL courante :
    ///
    ///     if (Session["UserRole"] == null || !…Equals("Administrateur"))
    ///         if (!Request.Url.AbsolutePath.EndsWith("Default.aspx"))
    ///             Response.Redirect("~/Default.aspx");
    ///
    /// Le test imbriqué ne pouvait jamais être faux, puisque ce code ne
    /// s'exécute que sur Carte.aspx. Le contrôle passe désormais par
    /// <see cref="PageAdmin"/>, comme toutes les autres pages protégées, avec
    /// la vérification du jeton anti-fixation de session en prime.
    ///
    /// Les erreurs de chargement étaient envoyées à
    /// <c>System.Diagnostics.Debug.WriteLine</c>, qui ne produit rien en
    /// configuration Release : une carte vide ne laissait aucune trace.
    /// </summary>
    public partial class Carte : PageAdmin
    {
        protected override string[] RolesAutorises
        {
            get { return new[] { "Administrateur" }; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            var dossiers = ChargerDossiersGeolocalises();

            // JavaScriptSerializer échappe « < » et « > » : un titre ou une
            // description contenant des balises ne peut pas s'échapper du
            // littéral JavaScript.
            var json = new JavaScriptSerializer().Serialize(dossiers);

            ClientScript.RegisterStartupScript(GetType(), "signalementsData",
                "var signalementsData = " + json + ";", true);
        }

        private List<SignalementMap> ChargerDossiersGeolocalises()
        {
            var resultat = new List<SignalementMap>();

            const string requete = @"
                SELECT d.DossierID, d.Titre, d.Ville, d.Description,
                       d.DatePublication,
                       t.Libelle AS TypeLibelle,
                       g.Latitude, g.Longitude
                FROM Dossiers d
                INNER JOIN TypesDossier   t ON d.TypeID    = t.TypeID
                INNER JOIN Geolocalisation g ON d.DossierID = g.DossierID
                WHERE d.EstPublie = 1
                ORDER BY d.DatePublication DESC";

            try
            {
                DataTable dt = Database.ExecuteQuery(requete);

                foreach (DataRow ligne in dt.Rows)
                {
                    resultat.Add(new SignalementMap
                    {
                        DossierID = Convert.ToInt32(ligne["DossierID"]),
                        Nom = Chaine(ligne["Titre"]),
                        Prenom = string.Empty,
                        Type = Chaine(ligne["TypeLibelle"]) == "Disparition"
                                   ? "Disparu" : "Recherché",
                        Ville = Chaine(ligne["Ville"]),
                        Latitude = Convert.ToDouble(ligne["Latitude"]),
                        Longitude = Convert.ToDouble(ligne["Longitude"]),
                        DateSignalement = Convert.ToDateTime(ligne["DatePublication"])
                                                 .ToString("dd/MM/yyyy"),
                        Description = Chaine(ligne["Description"])
                    });
                }
            }
            catch (Exception ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                // La page s'affiche avec une carte vide plutôt qu'une erreur :
                // l'incident est tracé côté serveur.
            }

            return resultat;
        }

        private static string Chaine(object valeur)
        {
            return valeur == null || valeur == DBNull.Value
                ? string.Empty
                : valeur.ToString();
        }
    }

    /// <summary>
    /// Forme des données envoyées au script de la carte.
    /// Les noms de propriétés sont ceux attendus par le JavaScript de
    /// Carte.aspx : les renommer casse l'affichage des marqueurs.
    /// </summary>
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
    }
}
