using System;
using System.Data;
using System.Web.UI;
using CamAlert.Helpers;

namespace CamAlert
{
    public partial class Historique : Page
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
            // Nb dossiers résolus
            object nbResolus = Database.ExecuteScalar(
                "SELECT COUNT(*) FROM Dossiers WHERE StatutDossier = 'Résolu'");
            litNbResolus.Text = nbResolus.ToString();

            // Nb dossiers en cours
            object nbEnCours = Database.ExecuteScalar(
                "SELECT COUNT(*) FROM Dossiers WHERE StatutDossier = 'En cours'");
            litNbEnCours.Text = nbEnCours.ToString();

            // Nb signalements traités
            object nbSignalements = Database.ExecuteScalar(
                "SELECT COUNT(*) FROM Signalements WHERE StatutID = 3");
            litNbSignalements.Text = nbSignalements.ToString();
        }

        private void ChargerResolus()
        {
            string query = @"
                SELECT d.DossierID, d.Titre, d.Annee, d.StatutDossier,
                       d.DateResolution, d.CommentaireResolution,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                WHERE d.StatutDossier = 'Résolu'
                ORDER BY d.DateResolution DESC";

            DataTable dt = Database.ExecuteQuery(query);
            gvResolus.DataSource = dt;
            gvResolus.DataBind();

            pnlResolus.Visible = true;
            pnlSignalements.Visible = false;
            pnlTous.Visible = false;
        }

        private void ChargerSignalements()
        {
            string query = @"
                SELECT s.SignalementID, s.NomDeclarant, s.Message,
                       s.DateSoumission,
                       d.Titre  AS DossierTitre,
                       st.Libelle AS StatutLibelle
                FROM Signalements s
                INNER JOIN Dossiers d ON s.DossierID = d.DossierID
                INNER JOIN Statuts  st ON s.StatutID = st.StatutID
                WHERE s.StatutID = 3
                ORDER BY s.DateSoumission DESC";

            DataTable dt = Database.ExecuteQuery(query);
            gvSignalements.DataSource = dt;
            gvSignalements.DataBind();

            pnlResolus.Visible = false;
            pnlSignalements.Visible = true;
            pnlTous.Visible = false;
        }

        private void ChargerTous()
        {
            string query = @"
                SELECT d.DossierID, d.Titre, d.Annee, d.StatutDossier,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                ORDER BY d.DatePublication DESC";

            DataTable dt = Database.ExecuteQuery(query);
            gvTous.DataSource = dt;
            gvTous.DataBind();

            pnlResolus.Visible = false;
            pnlSignalements.Visible = false;
            pnlTous.Visible = true;
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