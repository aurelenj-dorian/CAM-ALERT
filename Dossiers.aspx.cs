using CamAlert.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using System.Web.UI.WebControls;

namespace CamAlert
{
    /// <summary>
    /// Liste publique des dossiers, avec filtres par type et par année.
    ///
    /// Corrections par rapport à la version précédente :
    ///   • <c>int.Parse(type)</c> et <c>int.Parse(annee)</c> levaient une
    ///     exception non rattrapée si la valeur postée n'était pas numérique ;
    ///   • les termes de recherche n'étaient ni bornés ni nettoyés de leurs
    ///     jokers <c>LIKE</c> : une recherche sur « % » renvoyait tout, et un
    ///     terme très long provoquait une analyse complète de la table à chaque
    ///     frappe (la recherche se déclenche à la saisie) ;
    ///   • <c>SearchDossiers</c> construisait un tableau de paramètres vide
    ///     plutôt que null, ce que le pilote accepte mais qui masquait le cas
    ///     « aucun filtre ».
    /// </summary>
    public partial class Dossiers : PageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                ChargerDossiers();
        }

        private void ChargerDossiers()
        {
            var requete = new System.Text.StringBuilder(@"
                SELECT d.DossierID, d.Titre, d.Annee, d.Ville,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                WHERE d.EstPublie = 1");

            var parametres = new List<SqlParameter>();

            int typeId;
            if (int.TryParse(ddlType.SelectedValue, out typeId))
            {
                requete.Append(" AND d.TypeID = @TypeID");
                parametres.Add(new SqlParameter("@TypeID", SqlDbType.Int) { Value = typeId });
            }

            int annee;
            if (int.TryParse(ddlAnnee.SelectedValue, out annee))
            {
                requete.Append(" AND d.Annee = @Annee");
                parametres.Add(new SqlParameter("@Annee", SqlDbType.Int) { Value = annee });
            }

            requete.Append(" ORDER BY d.DatePublication DESC");

            try
            {
                gvDossiers.DataSource = Database.ExecuteQuery(
                    requete.ToString(),
                    parametres.Count > 0 ? parametres.ToArray() : null);
                gvDossiers.DataBind();
            }
            catch (Exception ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                gvDossiers.DataSource = null;
                gvDossiers.DataBind();
            }
        }

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

        // ─────────────────────────────────────────────────────
        // Méthodes de page appelées par le JavaScript de la liste.
        // Publiques et sans session : elles ne renvoient que des dossiers
        // publiés.
        // ─────────────────────────────────────────────────────

        [WebMethod]
        public static List<object> SearchDossiers(string searchTerm, string typeId, string annee)
        {
            return Rechercher(searchTerm, typeId, annee, 30, false);
        }

        [WebMethod]
        public static List<object> GetSuggestions(string searchTerm, string typeId, string annee)
        {
            // Les suggestions ne portent que sur le titre, et exigent un terme :
            // sans lui, la liste complète remonterait à chaque ouverture du champ.
            if (RechercheSql.Normaliser(searchTerm) == null)
                return new List<object>();

            return Rechercher(searchTerm, typeId, annee, 10, true);
        }

        private static List<object> Rechercher(string searchTerm, string typeId,
                                               string annee, int limite, bool titreSeul)
        {
            var resultat = new List<object>();

            var requete = new System.Text.StringBuilder();
            requete.Append("SELECT TOP ").Append(limite).Append(@"
                    d.DossierID, d.Titre, d.Annee, d.Ville,
                    t.Libelle AS TypeLibelle, r.Nom AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                WHERE d.EstPublie = 1");

            var parametres = new List<SqlParameter>();

            int type;
            if (int.TryParse(typeId, out type))
            {
                requete.Append(" AND d.TypeID = @TypeID");
                parametres.Add(new SqlParameter("@TypeID", SqlDbType.Int) { Value = type });
            }

            int an;
            if (int.TryParse(annee, out an))
            {
                requete.Append(" AND d.Annee = @Annee");
                parametres.Add(new SqlParameter("@Annee", SqlDbType.Int) { Value = an });
            }

            string terme = RechercheSql.Normaliser(searchTerm);
            if (terme != null)
            {
                requete.Append(titreSeul
                    ? " AND d.Titre LIKE @SearchTerm"
                    : @" AND (d.Titre LIKE @SearchTerm
                           OR d.Ville LIKE @SearchTerm
                           OR r.Nom   LIKE @SearchTerm)");

                parametres.Add(new SqlParameter("@SearchTerm", SqlDbType.NVarChar, 120)
                    { Value = "%" + terme + "%" });
            }

            requete.Append(" ORDER BY d.DatePublication DESC");

            try
            {
                DataTable dt = Database.ExecuteQuery(
                    requete.ToString(),
                    parametres.Count > 0 ? parametres.ToArray() : null);

                foreach (DataRow ligne in dt.Rows)
                {
                    resultat.Add(new
                    {
                        DossierID = ligne["DossierID"],
                        Titre = ligne["Titre"],
                        Annee = ligne["Annee"] == DBNull.Value ? null : ligne["Annee"],
                        Ville = ligne["Ville"] == DBNull.Value ? null : ligne["Ville"],
                        TypeLibelle = ligne["TypeLibelle"],
                        RegionNom = ligne["RegionNom"]
                    });
                }
            }
            catch (Exception ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
            }

            return resultat;
        }
    }
}
