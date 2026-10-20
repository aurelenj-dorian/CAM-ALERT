using CamAlert.Helpers;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace CamAlert.Admin
{
    /// <summary>
    /// Traitement des signalements reçus du public.
    ///
    /// Corrections par rapport à la version précédente :
    ///   • le contrôle d'accès est remonté dans <see cref="PageAdmin"/> ;
    ///   • les champs remplis depuis la base étaient écrits dans des contrôles
    ///     <c>Literal</c>, qui rendent leur contenu tel quel : un signalement
    ///     contenant des balises était interprété par le navigateur de l'agent
    ///     (XSS stocké, déclenchable par n'importe quel visiteur du site
    ///     puisque le formulaire est public). Tout est désormais encodé ;
    ///   • <c>DateTraitement</c> n'était jamais renseignée ;
    ///   • aucun changement de statut n'était tracé.
    /// </summary>
    public partial class Signalements : PageAdmin
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ChargerStatuts();
                ChargerSignalements();
            }
        }

        private void ChargerStatuts()
        {
            // La liste était figée dans le balisage : elle vient maintenant de
            // la table, donc un statut ajouté en base apparaît sans toucher au
            // code.
            DataTable dt = Database.ExecuteQuery(
                "SELECT StatutID, Libelle FROM Statuts ORDER BY Ordre, StatutID");

            if (dt.Rows.Count == 0) return;

            ddlStatut.Items.Clear();
            ddlStatut.DataSource = dt;
            ddlStatut.DataTextField = "Libelle";
            ddlStatut.DataValueField = "StatutID";
            ddlStatut.DataBind();
        }

        private void ChargerSignalements()
        {
            const string requete = @"
                SELECT s.SignalementID, s.NomDeclarant, s.Email,
                       s.Message, s.DateSoumission, s.StatutID,
                       s.CommentaireInterne,
                       d.Titre    AS DossierTitre,
                       st.Libelle AS StatutLibelle
                FROM Signalements s
                INNER JOIN Dossiers d  ON s.DossierID = d.DossierID
                INNER JOIN Statuts  st ON s.StatutID  = st.StatutID
                ORDER BY s.DateSoumission DESC";

            gvSignalements.DataSource = Database.ExecuteQuery(requete);
            gvSignalements.DataBind();
        }

        protected void gvSignalements_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Voir") return;

            int id;
            if (e.CommandArgument == null
                || !int.TryParse(e.CommandArgument.ToString(), out id))
                return;

            AfficherDetail(id);
        }

        private void AfficherDetail(int id)
        {
            const string requete = @"
                SELECT s.*, d.Titre AS DossierTitre, st.Libelle AS StatutLibelle
                FROM Signalements s
                INNER JOIN Dossiers d  ON s.DossierID = d.DossierID
                INNER JOIN Statuts  st ON s.StatutID  = st.StatutID
                WHERE s.SignalementID = @ID";

            DataTable dt = Database.ExecuteQuery(requete,
                new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = id } });

            if (dt.Rows.Count == 0)
            {
                AfficherMessage("Ce signalement n'existe plus.");
                ChargerSignalements();
                return;
            }

            DataRow ligne = dt.Rows[0];
            hdnSignalementID.Value = id.ToString();

            // Encodage systématique : ces valeurs viennent d'un formulaire
            // public et sont rendues dans des Literal.
            litDossier.Text = Encoder(ligne["DossierTitre"]);
            litDeclarant.Text = Vide(ligne["NomDeclarant"]) ? "Anonyme"
                                                            : Encoder(ligne["NomDeclarant"]);
            litEmail.Text = Vide(ligne["Email"]) ? "Non renseigné"
                                                 : Encoder(ligne["Email"]);
            litDate.Text = Convert.ToDateTime(ligne["DateSoumission"])
                                  .ToString("dd/MM/yyyy HH:mm");

            // Les retours à la ligne du message sont rendus en <br/> après
            // encodage : la mise en forme est conservée sans laisser passer de
            // balise.
            litMessageDetail.Text = Encoder(ligne["Message"]).Replace("\n", "<br/>");

            txtCommentaire.Text = ligne["CommentaireInterne"].ToString();
            ddlStatut.SelectedValue = ligne["StatutID"].ToString();
            pnlDetail.Visible = true;
            pnlMessage.Visible = false;
        }

        protected void btnChangerStatut_Click(object sender, EventArgs e)
        {
            int id, statut;
            if (!int.TryParse(hdnSignalementID.Value, out id)
                || !int.TryParse(ddlStatut.SelectedValue, out statut))
            {
                AfficherMessage("Sélection invalide. Rechargez la page.");
                return;
            }

            try
            {
                // DateTraitement passe à la date du jour dès que le
                // signalement quitte l'état « Nouveau », et repasse à NULL s'il
                // y revient : sans cela, la date de traitement restait vide et
                // on ne savait pas quand un signalement avait été examiné.
                Database.ExecuteNonQuery(@"
                    UPDATE Signalements
                       SET StatutID       = @Statut,
                           DateTraitement = CASE WHEN @Statut = 1
                                                 THEN NULL
                                                 ELSE SYSDATETIME() END
                     WHERE SignalementID = @ID",
                    new[]
                    {
                        new SqlParameter("@Statut", SqlDbType.Int) { Value = statut },
                        new SqlParameter("@ID",     SqlDbType.Int) { Value = id }
                    });
            }
            catch (SqlException ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                AfficherMessage("Le changement de statut a échoué.");
                return;
            }

            Journal.Enregistrer(Journal.Actions.Modification, LoginConnecte,
                "Signalement", id,
                "Statut porté à « " + ddlStatut.SelectedItem.Text + " »",
                IpClient);

            AfficherMessage("Statut mis à jour.", succes: true);
            pnlDetail.Visible = false;
            ChargerSignalements();
        }

        protected void btnSauvegarderCommentaire_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(hdnSignalementID.Value, out id))
            {
                AfficherMessage("Sélection invalide. Rechargez la page.");
                return;
            }

            try
            {
                Database.ExecuteNonQuery(
                    "UPDATE Signalements SET CommentaireInterne = @Commentaire " +
                    "WHERE SignalementID = @ID",
                    new[]
                    {
                        new SqlParameter("@Commentaire", SqlDbType.NVarChar)
                        {
                            Value = string.IsNullOrEmpty(txtCommentaire.Text)
                                ? (object)DBNull.Value : txtCommentaire.Text
                        },
                        new SqlParameter("@ID", SqlDbType.Int) { Value = id }
                    });
            }
            catch (SqlException ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                AfficherMessage("Le commentaire n'a pas pu être enregistré.");
                return;
            }

            Journal.Enregistrer(Journal.Actions.Modification, LoginConnecte,
                "Signalement", id, "Commentaire interne modifié", IpClient);

            AfficherMessage("Commentaire enregistré.", succes: true);
            ChargerSignalements();
        }

        protected void btnFermerDetail_Click(object sender, EventArgs e)
        {
            pnlDetail.Visible = false;
        }

        // ─────────────────────────────────────────────────────

        private static bool Vide(object valeur)
        {
            return valeur == null || valeur == DBNull.Value
                || string.IsNullOrWhiteSpace(valeur.ToString());
        }

        private string Encoder(object valeur)
        {
            return valeur == null || valeur == DBNull.Value
                ? string.Empty
                : Server.HtmlEncode(valeur.ToString());
        }

        /// <summary>
        /// Affiche un message. <paramref name="succes"/> choisit le bandeau :
        /// vert pour une confirmation, rouge pour un refus.
        /// Le texte passe par un Literal, qui rend son contenu tel quel :
        /// on encode, sinon un titre contenant des balises serait interprété.
        /// </summary>
        private void AfficherMessage(string message, bool succes = false)
        {
            litMessage.Text = Server.HtmlEncode(message);
            pnlMessage.CssClass = succes ? "success-msg" : "error-msg";
            pnlMessage.Visible = true;
        }
    }
}
