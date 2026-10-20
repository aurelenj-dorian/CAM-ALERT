using CamAlert.Helpers;
using System;
using System.Data;
using System.Data.SqlClient;

namespace CamAlert
{
    /// <summary>
    /// Fiche détaillée d'un dossier publié.
    ///
    /// Corrections par rapport à la version précédente : tous les champs
    /// venaient de la base et étaient écrits dans des contrôles
    /// <c>Literal</c>, qui rendent leur contenu tel quel. Un titre ou une
    /// description contenant des balises était donc interprété par le
    /// navigateur du visiteur. Le contenu est désormais encodé, et l'URL de
    /// l'image est validée avant d'être posée sur le contrôle Image : une
    /// valeur commençant par « javascript: » en base aurait produit un lien
    /// exécutable.
    /// </summary>
    public partial class DossierDetail : PageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            int dossierId;
            if (!int.TryParse(Request.QueryString["id"], out dossierId) || dossierId <= 0)
            {
                Rediriger();
                return;
            }

            ChargerDossier(dossierId);
        }

        private void ChargerDossier(int id)
        {
            const string requete = @"
                SELECT d.DossierID, d.Titre, d.Description,
                       d.Annee, d.Ville, d.ImageUrl,
                       d.SignesDistinctifs, d.DatePublication,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                WHERE d.DossierID = @ID AND d.EstPublie = 1";

            DataTable dt;
            try
            {
                dt = Database.ExecuteQuery(requete,
                    new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = id } });
            }
            catch (Exception ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                Rediriger();
                return;
            }

            if (dt.Rows.Count == 0)
            {
                // Dossier inexistant ou non publié : même réponse dans les deux
                // cas, pour ne pas révéler l'existence d'un dossier confidentiel.
                Rediriger();
                return;
            }

            DataRow ligne = dt.Rows[0];

            litTitre.Text = Encoder(ligne["Titre"]);
            litRef.Text = "DOS-" + ligne["DossierID"].ToString().PadLeft(4, '0');
            litAnnee.Text = Encoder(ligne["Annee"]);
            litRegion.Text = Encoder(ligne["RegionNom"]);
            litVille.Text = Encoder(ligne["Ville"]);

            // Les retours à la ligne sont rendus après encodage : la mise en
            // forme est conservée sans laisser passer de balise.
            litDescription.Text = Encoder(ligne["Description"]).Replace("\n", "<br/>");
            litSignes.Text = Encoder(ligne["SignesDistinctifs"]).Replace("\n", "<br/>");

            litDate.Text = Convert.ToDateTime(ligne["DatePublication"])
                                  .ToString("dd/MM/yyyy");

            litBadge.Text = Chaine(ligne["TypeLibelle"]) == "Disparition"
                ? "<span class='badge badge-disparition'>Disparition</span><br/><br/>"
                : "<span class='badge badge-recherche'>Recherche judiciaire</span><br/><br/>";

            imgDossier.ImageUrl = UrlImage(Chaine(ligne["ImageUrl"]));
            imgDossier.AlternateText = "Photographie du dossier "
                                       + Chaine(ligne["Titre"]);

            Page.Title = "CAM-ALERT – " + Chaine(ligne["Titre"]);
        }

        /// <summary>
        /// Valide le chemin d'image stocké en base. On n'accepte qu'un chemin
        /// relatif vers les deux dossiers de l'application ; tout le reste
        /// retombe sur l'image par défaut.
        /// </summary>
        private static string UrlImage(string valeur)
        {
            const string parDefaut = "~/Content/images/dossier1.jpg";

            if (string.IsNullOrWhiteSpace(valeur)) return parDefaut;

            valeur = valeur.Trim();

            bool acceptee = UrlLocale.EstLocale(valeur)
                && (valeur.StartsWith("/Admin/image/", StringComparison.OrdinalIgnoreCase)
                 || valeur.StartsWith("/Content/images/", StringComparison.OrdinalIgnoreCase))
                && valeur.IndexOf("..", StringComparison.Ordinal) < 0;

            return acceptee ? valeur : parDefaut;
        }

        private void Rediriger()
        {
            Response.Redirect("~/Dossiers.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static string Chaine(object valeur)
        {
            return valeur == null || valeur == DBNull.Value
                ? string.Empty
                : valeur.ToString();
        }

        private string Encoder(object valeur)
        {
            return Server.HtmlEncode(Chaine(valeur));
        }
    }
}
