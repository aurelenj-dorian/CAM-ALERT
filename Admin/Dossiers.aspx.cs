using CamAlert.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace CamAlert.Admin
{
    /// <summary>
    /// Gestion des dossiers : création, modification, suppression.
    ///
    /// Corrections par rapport à la version précédente :
    ///   • le contrôle d'accès, recopié dans Page_Load, est remonté dans
    ///     <see cref="PageAdmin"/> : il s'applique avant tout traitement et
    ///     avant le déclenchement des événements de contrôle ;
    ///   • la suppression était ouverte à tout agent connecté ; elle est
    ///     réservée au rôle « Administrateur » ;
    ///   • l'image était écrite sous le nom fourni par le navigateur, après un
    ///     simple test d'extension ; elle passe par
    ///     <see cref="FichierSecurise"/>, qui régénère le nom, borne la taille
    ///     et contrôle la signature du format ;
    ///   • <c>int.Parse(txtAnnee.Text)</c> levait une exception non rattrapée
    ///     sur une saisie non numérique, affichant la page d'erreur ASP.NET ;
    ///   • aucune action n'était tracée : impossible de savoir qui avait
    ///     supprimé un dossier.
    /// </summary>
    public partial class Dossiers : PageAdmin
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // L'authentification et le rôle sont déjà vérifiés par PageAdmin.
            if (!IsPostBack)
            {
                ChargerDossiers();
                ChargerTypes();
                ChargerRegions();
            }
        }

        private void ChargerDossiers()
        {
            const string requete = @"
                SELECT d.DossierID, d.Titre, d.Annee, d.EstPublie,
                       t.Libelle AS TypeLibelle,
                       r.Nom     AS RegionNom
                FROM Dossiers d
                INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
                INNER JOIN Regions      r ON d.RegionID = r.RegionID
                ORDER BY d.DatePublication DESC";

            gvDossiers.DataSource = Database.ExecuteQuery(requete);
            gvDossiers.DataBind();
        }

        private void ChargerTypes()
        {
            ddlType.DataSource = Database.ExecuteQuery(
                "SELECT TypeID, Libelle FROM TypesDossier ORDER BY Libelle");
            ddlType.DataTextField = "Libelle";
            ddlType.DataValueField = "TypeID";
            ddlType.DataBind();
        }

        private void ChargerRegions()
        {
            ddlRegion.DataSource = Database.ExecuteQuery(
                "SELECT RegionID, Nom FROM Regions ORDER BY Nom");
            ddlRegion.DataTextField = "Nom";
            ddlRegion.DataValueField = "RegionID";
            ddlRegion.DataBind();
        }

        protected void btnNouveauDossier_Click(object sender, EventArgs e)
        {
            hdnDossierID.Value = "0";
            litTitreForm.Text = "Nouveau dossier";
            txtTitre.Text = "";
            txtVille.Text = "";
            txtAnnee.Text = "";
            txtDescription.Text = "";
            txtSignes.Text = "";
            txtCommentaireResolution.Text = "";
            ddlStatutDossier.SelectedValue = "En cours";
            pnlFormulaire.Visible = true;
            pnlMessage.Visible = false;
        }

        protected void btnAnnuler_Click(object sender, EventArgs e)
        {
            pnlFormulaire.Visible = false;
        }

        protected void btnSauvegarder_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            int dossierID;
            if (!int.TryParse(hdnDossierID.Value, out dossierID) || dossierID < 0)
            {
                AfficherMessage("Formulaire invalide. Rechargez la page.");
                return;
            }

            // L'année est saisie librement : une valeur non numérique ou
            // absurde ne doit pas produire une exception.
            int? annee = null;
            if (!string.IsNullOrWhiteSpace(txtAnnee.Text))
            {
                int valeur;
                if (!int.TryParse(txtAnnee.Text.Trim(), out valeur)
                    || valeur < 1960 || valeur > DateTime.Now.Year + 1)
                {
                    AfficherMessage("L'année doit être un nombre compris entre 1960 et "
                                    + (DateTime.Now.Year + 1) + ".");
                    return;
                }
                annee = valeur;
            }

            int typeID, regionID, publie;
            if (!int.TryParse(ddlType.SelectedValue, out typeID)
                || !int.TryParse(ddlRegion.SelectedValue, out regionID)
                || !int.TryParse(ddlPublie.SelectedValue, out publie))
            {
                AfficherMessage("Sélection invalide. Rechargez la page.");
                return;
            }

            // Le formulaire proposait déjà un statut de dossier et un
            // commentaire de résolution, mais aucun des deux n'était
            // enregistré : la page Historique ne pouvait donc jamais afficher
            // de dossier résolu, puisque rien ne mettait StatutDossier à jour.
            string statut = ddlStatutDossier.SelectedValue;
            if (statut != "En cours" && statut != "Résolu" && statut != "Classé")
            {
                AfficherMessage("Statut de dossier invalide. Rechargez la page.");
                return;
            }

            // Contrôle complet de l'image avant toute écriture en base.
            var image = FichierSecurise.EnregistrerImage(fuImage, Server);
            if (!image.EstValide)
            {
                AfficherMessage(image.Erreur);
                return;
            }
            string imageUrl = image.Absent ? null : image.CheminRelatif;

            try
            {
                if (dossierID == 0)
                    CreerDossier(typeID, regionID, annee, publie, imageUrl, statut);
                else
                    ModifierDossier(dossierID, typeID, regionID, annee, publie,
                                    imageUrl, statut);
            }
            catch (SqlException ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                AfficherMessage("L'enregistrement a échoué. Vérifiez les champs "
                                + "et réessayez.");
                return;
            }

            pnlFormulaire.Visible = false;
            ChargerDossiers();
        }

        private void CreerDossier(int typeID, int regionID, int? annee,
                                  int publie, string imageUrl, string statut)
        {
            const string requete = @"
                INSERT INTO Dossiers
                    (Titre, Description, TypeID, RegionID, Annee,
                     ImageUrl, SignesDistinctifs, Ville, EstPublie,
                     StatutDossier, DateResolution, CommentaireResolution)
                VALUES
                    (@Titre, @Description, @TypeID, @RegionID, @Annee,
                     @ImageUrl, @Signes, @Ville, @Publie,
                     @Statut, @DateResolution, @CommentaireResolution);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parametres = ParametresCommuns(typeID, regionID, annee, publie);
            parametres.Add(Texte("@ImageUrl", imageUrl, 300));
            parametres.AddRange(ParametresResolution(statut));

            var nouvelId = Database.ExecuteScalar(requete, parametres.ToArray());

            Journal.Enregistrer(Journal.Actions.Creation, LoginConnecte, "Dossier",
                nouvelId == null ? (int?)null : Convert.ToInt32(nouvelId),
                "Création du dossier « " + txtTitre.Text.Trim() + " »"
                + (publie == 1 ? " (publié)" : " (non publié)"),
                IpClient);

            AfficherMessage("Dossier ajouté avec succès.", succes: true);
        }

        private void ModifierDossier(int dossierID, int typeID, int regionID,
                                     int? annee, int publie, string imageUrl,
                                     string statut)
        {
            // L'image n'est remplacée que si un nouveau fichier a été fourni :
            // modifier un dossier sans toucher à la photo doit la conserver.
            string requete = @"
                UPDATE Dossiers SET
                    Titre             = @Titre,
                    Description       = @Description,
                    TypeID            = @TypeID,
                    RegionID          = @RegionID,
                    Annee             = @Annee,
                    SignesDistinctifs = @Signes,
                    Ville             = @Ville,
                    EstPublie         = @Publie,
                    StatutDossier     = @Statut,
                    DateResolution    = @DateResolution,
                    CommentaireResolution = @CommentaireResolution,
                    DateModification  = SYSDATETIME()"
                + (imageUrl != null ? ", ImageUrl = @ImageUrl" : string.Empty)
                + " WHERE DossierID = @ID";

            var parametres = ParametresCommuns(typeID, regionID, annee, publie);
            parametres.Add(new SqlParameter("@ID", SqlDbType.Int) { Value = dossierID });
            parametres.AddRange(ParametresResolution(statut, dossierID));
            if (imageUrl != null)
                parametres.Add(Texte("@ImageUrl", imageUrl, 300));

            Database.ExecuteNonQuery(requete, parametres.ToArray());

            Journal.Enregistrer(Journal.Actions.Modification, LoginConnecte,
                "Dossier", dossierID,
                "Modification du dossier « " + txtTitre.Text.Trim() + " »"
                + (imageUrl != null ? " (photo remplacée)" : string.Empty),
                IpClient);

            AfficherMessage("Dossier modifié avec succès.", succes: true);
        }

        private List<SqlParameter> ParametresCommuns(int typeID, int regionID,
                                                     int? annee, int publie)
        {
            return new List<SqlParameter>
            {
                Texte("@Titre",       txtTitre.Text.Trim(),       200),
                Texte("@Description", txtDescription.Text,        -1),
                new SqlParameter("@TypeID",   SqlDbType.Int) { Value = typeID },
                new SqlParameter("@RegionID", SqlDbType.Int) { Value = regionID },
                new SqlParameter("@Annee",    SqlDbType.Int)
                    { Value = annee.HasValue ? (object)annee.Value : DBNull.Value },
                Texte("@Signes",      txtSignes.Text,             1000),
                Texte("@Ville",       txtVille.Text.Trim(),       100),
                new SqlParameter("@Publie",   SqlDbType.Bit) { Value = publie == 1 }
            };
        }

        /// <summary>
        /// Paramètres liés à la résolution d'un dossier.
        ///
        /// La contrainte CK_Dossiers_Resolution exige une date de résolution dès
        /// que le statut est « Résolu ». On la pose à la date du jour si elle
        /// manque, et on l'efface quand le dossier repasse en cours — sans quoi
        /// un dossier rouvert garderait une date de clôture.
        /// </summary>
        private List<SqlParameter> ParametresResolution(string statut,
                                                        int? dossierExistant = null)
        {
            object dateResolution = DBNull.Value;

            if (statut == "Résolu")
            {
                // Sur une modification, on conserve la date déjà enregistrée :
                // rouvrir puis reclore un dossier ne doit pas réécrire
                // l'historique.
                object dateExistante = null;
                if (dossierExistant.HasValue)
                {
                    dateExistante = Database.ExecuteScalar(
                        "SELECT DateResolution FROM Dossiers WHERE DossierID = @ID",
                        new[]
                        {
                            new SqlParameter("@ID", SqlDbType.Int)
                                { Value = dossierExistant.Value }
                        });
                }

                dateResolution = dateExistante ?? (object)DateTime.Now;
            }

            return new List<SqlParameter>
            {
                Texte("@Statut", statut, 30),
                new SqlParameter("@DateResolution", SqlDbType.DateTime2)
                    { Value = dateResolution },
                Texte("@CommentaireResolution",
                      statut == "En cours" ? null : txtCommentaireResolution.Text, 1000)
            };
        }

        /// <summary>
        /// Paramètre texte typé et dimensionné. Déclarer le type et la longueur
        /// évite les conversions implicites côté SQL Server, qui empêchent
        /// l'utilisation des index.
        /// </summary>
        private static SqlParameter Texte(string nom, string valeur, int longueur)
        {
            var p = longueur > 0
                ? new SqlParameter(nom, SqlDbType.NVarChar, longueur)
                : new SqlParameter(nom, SqlDbType.NVarChar);

            p.Value = string.IsNullOrEmpty(valeur) ? (object)DBNull.Value : valeur;
            return p;
        }

        protected void gvDossiers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (e.CommandArgument == null
                || !int.TryParse(e.CommandArgument.ToString(), out id))
                return;

            if (e.CommandName == "Supprimer")
                Supprimer(id);
            else if (e.CommandName == "Modifier")
                PreparerModification(id);
        }

        private void Supprimer(int id)
        {
            // La suppression efface aussi les signalements reçus du public :
            // elle est réservée aux administrateurs.
            if (!EstAdministrateur)
            {
                Journal.Enregistrer(Journal.Actions.Erreur, LoginConnecte,
                    "Dossier", id,
                    "Tentative de suppression sans le rôle Administrateur",
                    IpClient);

                AfficherMessage("Seul un administrateur peut supprimer un dossier.");
                return;
            }

            try
            {
                // On relève le titre avant l'effacement : après, il n'existe
                // plus et le journal serait inexploitable.
                var titre = Database.ExecuteScalar(
                    "SELECT Titre FROM Dossiers WHERE DossierID = @ID",
                    new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = id } });

                int signalements = Database.ExecuteNonQuery(
                    "DELETE FROM Signalements WHERE DossierID = @ID",
                    new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = id } });

                Database.ExecuteNonQuery(
                    "DELETE FROM Dossiers WHERE DossierID = @ID",
                    new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = id } });

                Journal.Enregistrer(Journal.Actions.Suppression, LoginConnecte,
                    "Dossier", id,
                    "Suppression du dossier « " + (titre == null ? "?" : titre.ToString())
                    + " » et de " + signalements + " signalement(s) associé(s)",
                    IpClient);

                AfficherMessage("Dossier supprimé.", succes: true);
            }
            catch (SqlException ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                AfficherMessage("La suppression a échoué.");
                return;
            }

            pnlFormulaire.Visible = false;
            ChargerDossiers();
        }

        private void PreparerModification(int id)
        {
            DataTable dt = Database.ExecuteQuery(
                "SELECT * FROM Dossiers WHERE DossierID = @ID",
                new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = id } });

            if (dt.Rows.Count == 0)
            {
                AfficherMessage("Ce dossier n'existe plus.");
                ChargerDossiers();
                return;
            }

            DataRow ligne = dt.Rows[0];
            hdnDossierID.Value = id.ToString();
            litTitreForm.Text = "Modifier le dossier";
            txtTitre.Text = ligne["Titre"].ToString();
            txtDescription.Text = ligne["Description"].ToString();
            txtVille.Text = ligne["Ville"].ToString();
            txtAnnee.Text = ligne["Annee"].ToString();
            txtSignes.Text = ligne["SignesDistinctifs"].ToString();
            ddlType.SelectedValue = ligne["TypeID"].ToString();
            ddlRegion.SelectedValue = ligne["RegionID"].ToString();
            ddlPublie.SelectedValue = Convert.ToBoolean(ligne["EstPublie"]) ? "1" : "0";
            ddlStatutDossier.SelectedValue = ligne["StatutDossier"].ToString();
            txtCommentaireResolution.Text = ligne["CommentaireResolution"].ToString();
            pnlFormulaire.Visible = true;
            pnlMessage.Visible = false;
        }

        protected void btnDeconnexion_Click(object sender, EventArgs e)
        {
            Deconnecter();
            Response.Redirect("~/Admin/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// Les messages passent par un Literal : on encode, sinon un titre
        /// contenant des balises serait interprété par le navigateur.
        /// </summary>
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
