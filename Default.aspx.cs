using CamAlert.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;

namespace CamAlert
{
    public partial class _Default : PageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Les dossiers mis en avant sur l'accueil sont statiques (balisage).
        }

        /// <summary>
        /// Recherche rapide appelée par le champ du menu
        /// (<c>POST /Default.aspx/SearchAllDossiers</c>).
        ///
        /// Méthode de page : elle est publique et ne porte aucune session, donc
        /// elle ne doit renvoyer que des dossiers publiés — ce qu'elle fait.
        ///
        /// Deux corrections : le terme recherché est désormais borné en
        /// longueur et nettoyé des jokers SQL, et la clause ORDER BY ne
        /// s'applique plus qu'aux lignes filtrées. Un terme de 4 000 caractères
        /// rempli de « % » provoquait une analyse complète de la table à chaque
        /// frappe.
        /// </summary>
        [WebMethod]
        public static List<Dictionary<string, object>> SearchAllDossiers(string searchTerm)
        {
            var resultat = new List<Dictionary<string, object>>();

            string terme = RechercheSql.Normaliser(searchTerm);

            var requete = @"
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

            SqlParameter[] parametres = null;

            if (terme != null)
            {
                requete += @" AND (d.Titre LIKE @SearchTerm
                                OR d.Ville LIKE @SearchTerm
                                OR r.Nom   LIKE @SearchTerm)";

                parametres = new[]
                {
                    new SqlParameter("@SearchTerm", SqlDbType.NVarChar, 120)
                        { Value = "%" + terme + "%" }
                };
            }

            requete += " ORDER BY d.DatePublication DESC";

            try
            {
                DataTable dt = Database.ExecuteQuery(requete, parametres);

                foreach (DataRow ligne in dt.Rows)
                {
                    var entree = new Dictionary<string, object>();
                    foreach (DataColumn colonne in dt.Columns)
                        entree[colonne.ColumnName] = ligne[colonne] == DBNull.Value
                            ? null : ligne[colonne];

                    resultat.Add(entree);
                }
            }
            catch (Exception ex)
            {
                // La recherche renvoie une liste vide plutôt qu'une erreur 500
                // dans la console du navigateur ; l'incident est tracé.
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
            }

            return resultat;
        }
    }
}
