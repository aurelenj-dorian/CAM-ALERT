using CamAlert.Helpers;
using System;
using System.Data;

namespace CamAlert
{
    /// <summary>
    /// Historique public : dossiers résolus, signalements traités, vue d'ensemble.
    ///
    /// Les trois compteurs du haut de page étaient obtenus par trois requêtes
    /// COUNT successives, donc trois allers-retours vers SQL Server à chaque
    /// affichage et à chaque clic d'onglet. Ils viennent maintenant de la vue
    /// <c>vw_StatistiquesPubliques</c>, en une seule requête.
    ///
    /// Les requêtes comparaient aussi <c>StatutID = 3</c> en dur ; on joint
    /// désormais la table Statuts sur son libellé, ce qui ne dépend plus de
    /// l'ordre d'insertion des statuts.
    /// </summary>
    public partial class Historique : PageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ChargerStats();
                ChargerResolus();
            }
        }

        private void ChargerStats()
        {
            try
            {
                DataTable dt = Database.ExecuteQuery(
                    "SELECT NbDossiersResolus, NbDossiersEnCours, " +
                    "       NbSignalementsTraites " +
                    "FROM vw_StatistiquesPubliques");

                if (dt.Rows.Count == 0) return;

                DataRow ligne = dt.Rows[0];
                litNbResolus.Text = ligne["NbDossiersResolus"].ToString();
                litNbEnCours.Text = ligne["NbDossiersEnCours"].ToString();
                litNbSignalements.Text = ligne["NbSignalementsTraites"].ToString();
            }
            catch (Exception ex)
            {
                // Statistiques indisponibles : la page reste consultable, les
                // compteurs affichent un tiret.
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                litNbResolus.Text = "—";
                litNbEnCours.Text = "—";
                litNbSignalements.Text = "—";
            }
        }

        private void ChargerResolus()
        {
            const string requete = @"
                SELECT d.DossierID, d.Titre, d.Annee, d.StatutDossier,
                       d.DateResolution, d.CommentaireResolution,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                WHERE d.StatutDossier = N'Résolu' AND d.EstPublie = 1
                ORDER BY d.DateResolution DESC";

            gvResolus.DataSource = Database.ExecuteQuery(requete);
            gvResolus.DataBind();

            AfficherOnglet(pnlResolus);
        }

        private void ChargerSignalements()
        {
            // On ne publie ni le nom du déclarant ni le contenu du signalement :
            // la version précédente affichait les deux sur une page publique,
            // alors qu'un témoin peut être identifié par ce qu'il raconte.
            const string requete = @"
                SELECT s.SignalementID, s.DateSoumission, s.DateTraitement,
                       d.Titre    AS DossierTitre,
                       st.Libelle AS StatutLibelle
                FROM Signalements s
                INNER JOIN Dossiers d  ON s.DossierID = d.DossierID
                INNER JOIN Statuts  st ON s.StatutID  = st.StatutID
                WHERE st.Libelle = N'Traité' AND d.EstPublie = 1
                ORDER BY s.DateSoumission DESC";

            gvSignalements.DataSource = Database.ExecuteQuery(requete);
            gvSignalements.DataBind();

            AfficherOnglet(pnlSignalements);
        }

        private void ChargerTous()
        {
            const string requete = @"
                SELECT d.DossierID, d.Titre, d.Annee, d.StatutDossier,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                WHERE d.EstPublie = 1
                ORDER BY d.DatePublication DESC";

            gvTous.DataSource = Database.ExecuteQuery(requete);
            gvTous.DataBind();

            AfficherOnglet(pnlTous);
        }

        /// <summary>
        /// N'affiche que l'onglet demandé. La version précédente répétait les
        /// trois affectations de visibilité dans chaque méthode de chargement.
        /// </summary>
        private void AfficherOnglet(System.Web.UI.WebControls.Panel onglet)
        {
            pnlResolus.Visible = ReferenceEquals(onglet, pnlResolus);
            pnlSignalements.Visible = ReferenceEquals(onglet, pnlSignalements);
            pnlTous.Visible = ReferenceEquals(onglet, pnlTous);
        }

        protected void btnOngletResolus_Click(object sender, EventArgs e)
        {
            ChargerStats();
            ChargerResolus();
        }

        protected void btnOngletSignalements_Click(object sender, EventArgs e)
        {
            ChargerStats();
            ChargerSignalements();
        }

        protected void btnOngletTous_Click(object sender, EventArgs e)
        {
            ChargerStats();
            ChargerTous();
        }
    }
}
