using CamAlert.Helpers;
using System;
using System.Data;
using System.Data.SqlClient;

namespace CamAlert
{
    /// <summary>
    /// Formulaire public de signalement rattaché à un dossier.
    ///
    /// Corrections par rapport à la version précédente :
    ///   • aucune limite n'existait : un script pouvait insérer des milliers de
    ///     faux signalements et remplir le dossier /Uploads ;
    ///   • <c>int.Parse(ddlDossier.SelectedValue)</c> levait une exception non
    ///     rattrapée quand aucun dossier n'était choisi — l'option
    ///     « Sélectionnez un dossier » a une valeur vide ;
    ///   • la pièce jointe était écrite sous le nom fourni par le navigateur
    ///     après un simple test d'extension, sans limite de taille ; elle passe
    ///     par <see cref="FichierSecurise"/> ;
    ///   • un fichier au format refusé était silencieusement ignoré : le
    ///     témoin croyait avoir joint sa photo. Le refus est désormais affiché ;
    ///   • la pré-sélection par <c>?dossier=</c> levait une exception si
    ///     l'identifiant ne correspondait à aucun dossier publié.
    /// </summary>
    public partial class Signalement : PageBase
    {
        private const string PORTEE = "signalement";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            ChargerDossiers();
            PreselectionnerDossier();
        }

        private void ChargerDossiers()
        {
            DataTable dt = Database.ExecuteQuery(
                "SELECT DossierID, Titre FROM Dossiers WHERE EstPublie = 1 ORDER BY Titre");

            ddlDossier.DataSource = dt;
            ddlDossier.DataTextField = "Titre";
            ddlDossier.DataValueField = "DossierID";
            ddlDossier.DataBind();

            ddlDossier.Items.Insert(0, new System.Web.UI.WebControls.ListItem(
                "-- Sélectionnez un dossier --", ""));
        }

        private void PreselectionnerDossier()
        {
            var parametre = Request.QueryString["dossier"];
            if (string.IsNullOrEmpty(parametre)) return;

            // SelectedValue lève une exception si la valeur n'est pas dans la
            // liste : un identifiant inexistant dans l'URL suffisait à casser
            // la page.
            var element = ddlDossier.Items.FindByValue(parametre);
            if (element != null)
                ddlDossier.SelectedValue = parametre;
        }

        protected void btnEnvoyer_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            // Limitation par adresse IP : le formulaire est public et n'a
            // aucune autre barrière.
            string cle = LimiteurTentatives.CleIp(PORTEE, IpClient);
            int attente;
            if (LimiteurTentatives.EstBloque(cle, out attente))
            {
                AfficherErreur("Vous avez envoyé plusieurs signalements coup sur coup. "
                               + LimiteurTentatives.MessageBlocage(attente));
                return;
            }

            int dossierId;
            if (!int.TryParse(ddlDossier.SelectedValue, out dossierId))
            {
                AfficherErreur("Choisissez le dossier concerné par votre signalement.");
                return;
            }

            string message = (txtMessage.Text ?? string.Empty).Trim();
            if (message.Length < 10)
            {
                AfficherErreur("Décrivez votre signalement en quelques mots "
                               + "(10 caractères minimum).");
                return;
            }

            // Le dossier doit être publié : sans ce contrôle, un identifiant
            // forgé permettait de rattacher un signalement à un dossier encore
            // confidentiel, et d'en déduire l'existence.
            if (!DossierPublie(dossierId))
            {
                AfficherErreur("Ce dossier n'est pas accessible. "
                               + "Rechargez la page et réessayez.");
                return;
            }

            var piece = FichierSecurise.EnregistrerPieceJointe(fuFichier, Server);
            if (!piece.EstValide)
            {
                // Un fichier refusé était auparavant ignoré sans un mot :
                // le témoin repartait en croyant sa photo transmise.
                AfficherErreur(piece.Erreur);
                return;
            }

            try
            {
                const string requete = @"
                    INSERT INTO Signalements
                        (DossierID, NomDeclarant, Email, Message, FichierJoint,
                         StatutID, AdresseIP)
                    VALUES
                        (@DossierID, @Nom, @Email, @Message, @Fichier, 1, @Ip);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var parametres = new[]
                {
                    new SqlParameter("@DossierID", SqlDbType.Int) { Value = dossierId },
                    Texte("@Nom",     txtNom.Text, 150),
                    Texte("@Email",   txtEmail.Text, 150),
                    Texte("@Message", message, -1),
                    Texte("@Fichier", piece.Absent ? null : piece.CheminRelatif, 300),
                    Texte("@Ip",      IpClient, 45)
                };

                var nouvelId = Database.ExecuteScalar(requete, parametres);

                Journal.Enregistrer(Journal.Actions.Signalement, "public",
                    "Signalement",
                    nouvelId == null ? (int?)null : Convert.ToInt32(nouvelId),
                    "Signalement reçu pour le dossier #" + dossierId
                    + (piece.Absent ? string.Empty : " (avec pièce jointe)"),
                    IpClient);
            }
            catch (SqlException ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                AfficherErreur("Votre signalement n'a pas pu être enregistré. "
                               + "Réessayez dans quelques instants.");
                return;
            }

            // Le compteur n'est incrémenté qu'après un envoi réellement
            // enregistré : une erreur technique ne doit pas consommer le quota
            // d'un témoin de bonne foi.
            LimiteurTentatives.CompterTentative(cle, LimiteurTentatives.MAX_SIGNALEMENT);

            pnlForm.Visible = false;
            pnlSuccess.Visible = true;
        }

        private static bool DossierPublie(int dossierId)
        {
            var valeur = Database.ExecuteScalar(
                "SELECT 1 FROM Dossiers WHERE DossierID = @ID AND EstPublie = 1",
                new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = dossierId } });

            return valeur != null;
        }

        private static SqlParameter Texte(string nom, string valeur, int longueur)
        {
            var p = longueur > 0
                ? new SqlParameter(nom, SqlDbType.NVarChar, longueur)
                : new SqlParameter(nom, SqlDbType.NVarChar);

            p.Value = string.IsNullOrWhiteSpace(valeur)
                ? (object)DBNull.Value
                : valeur.Trim();
            return p;
        }

        private void AfficherErreur(string message)
        {
            pnlSuccess.Visible = false;
            pnlForm.Visible = true;
            lblErreur.Text = Server.HtmlEncode(message);
            pnlErreur.Visible = true;
        }
    }
}
