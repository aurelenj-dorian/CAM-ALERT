using CamAlert.Helpers;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace CamAlert
{
    /// <summary>
    /// Formulaire de contact.
    ///
    /// Corrections par rapport à la version précédente :
    ///   • l'adresse et le mot de passe d'application du compte Gmail
    ///     expéditeur étaient écrits en clair dans ce fichier, donc publiés
    ///     avec le dépôt. Tout passe désormais par <see cref="Parametres"/>,
    ///     c'est-à-dire par des variables d'environnement du serveur. Le
    ///     secret publié doit être révoqué : voir SECURITY.md ;
    ///   • les quatre adresses des destinataires étaient également figées dans
    ///     le code ; elles viennent de la configuration ;
    ///   • le corps de l'email était composé par interpolation de chaînes sans
    ///     encodage : un message contenant des balises était injecté tel quel
    ///     dans l'email HTML reçu par les agents ;
    ///   • aucune limite n'existait : chaque soumission envoyait un email à
    ///     quatre adresses, ce qui faisait du formulaire un relais de spam ;
    ///   • le message n'était nulle part conservé : si le serveur SMTP était
    ///     indisponible, il était perdu sans que personne ne le sache. Il est
    ///     maintenant enregistré en base d'abord, puis envoyé.
    /// </summary>
    public partial class Contact : PageBase
    {
        private const string PORTEE = "contact";

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnEnvoyer_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string cle = LimiteurTentatives.CleIp(PORTEE, IpClient);
            int attente;
            if (LimiteurTentatives.EstBloque(cle, out attente))
            {
                AfficherErreur("Vous avez envoyé plusieurs messages coup sur coup. "
                               + LimiteurTentatives.MessageBlocage(attente));
                return;
            }

            string nom = (txtNom.Text ?? string.Empty).Trim();
            string email = (txtEmail.Text ?? string.Empty).Trim();
            string sujet = ddlSujet.SelectedValue ?? string.Empty;
            string message = (txtMessage.Text ?? string.Empty).Trim();

            if (nom.Length == 0 || email.Length == 0 || message.Length < 10)
            {
                AfficherErreur("Renseignez votre nom, votre email et un message "
                               + "d'au moins 10 caractères.");
                return;
            }

            // Le message est enregistré avant toute tentative d'envoi : même si
            // le serveur SMTP est injoignable, il ne sera pas perdu.
            int? messageId = Enregistrer(nom, email, sujet, message);

            bool envoye = Envoyer(nom, email, sujet, message);

            if (envoye && messageId.HasValue)
                MarquerEnvoye(messageId.Value);

            LimiteurTentatives.CompterTentative(cle, LimiteurTentatives.MAX_CONTACT);

            Journal.Enregistrer(Journal.Actions.Contact, "public",
                "MessageContact", messageId,
                "Message de contact reçu (" + sujet + ")"
                + (envoye ? " et transmis par email" : " ; envoi email en échec"),
                IpClient);

            if (messageId.HasValue || envoye)
            {
                pnlSuccess.Visible = true;
                pnlError.Visible = false;

                txtNom.Text = "";
                txtEmail.Text = "";
                ddlSujet.SelectedIndex = 0;
                txtMessage.Text = "";
            }
            else
            {
                AfficherErreur("Votre message n'a pas pu être transmis. "
                               + "Réessayez dans quelques instants.");
            }
        }

        // ─────────────────────────────────────────────────────

        private int? Enregistrer(string nom, string email, string sujet, string message)
        {
            try
            {
                const string requete = @"
                    INSERT INTO MessagesContact (Nom, Email, Sujet, Message, AdresseIP)
                    VALUES (@Nom, @Email, @Sujet, @Message, @Ip);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var parametres = new[]
                {
                    Texte("@Nom",     nom,      150),
                    Texte("@Email",   email,    150),
                    Texte("@Sujet",   sujet,    150),
                    Texte("@Message", message,  -1),
                    Texte("@Ip",      IpClient, 45)
                };

                var id = Database.ExecuteScalar(requete, parametres);
                return id == null ? (int?)null : Convert.ToInt32(id);
            }
            catch (Exception ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                return null;
            }
        }

        private static void MarquerEnvoye(int messageId)
        {
            try
            {
                Database.ExecuteNonQuery(
                    "UPDATE MessagesContact SET EmailEnvoye = 1 WHERE MessageID = @ID",
                    new[] { new SqlParameter("@ID", SqlDbType.Int) { Value = messageId } });
            }
            catch (Exception ex)
            {
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
            }
        }

        private bool Envoyer(string nom, string email, string sujet, string message)
        {
            var destinataires = Parametres.LireListe("Contact_Destinataires");
            if (destinataires.Length == 0)
            {
                Journal.Enregistrer(Journal.Actions.Erreur, "système",
                    details: "Aucun destinataire configuré "
                             + "(Contact_Destinataires) : email non envoyé",
                    adresseIp: IpClient);
                return false;
            }

            try
            {
                string hote = Parametres.Lire("SMTP_Host", "smtp.gmail.com");
                int port = Parametres.LireEntier("SMTP_Port", 587);
                bool ssl = Parametres.LireBooleen("SMTP_SSL", true);
                string utilisateur = Parametres.LireObligatoire("SMTP_User");
                string motDePasse = Parametres.LireObligatoire("SMTP_Password");
                string expediteur = Parametres.Lire("SMTP_From", utilisateur);
                string nomExpediteur = Parametres.Lire("SMTP_NomExp",
                                                       "CAM-ALERT — Portail camerounais");

                using (var client = new SmtpClient(hote, port))
                {
                    client.EnableSsl = ssl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(utilisateur, motDePasse);
                    client.Timeout = 20000;

                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(expediteur, nomExpediteur);
                        foreach (var destinataire in destinataires)
                            mail.To.Add(destinataire.Trim());

                        // L'expéditeur réel reste le compte du service ; on place
                        // l'adresse du visiteur en Reply-To pour pouvoir lui
                        // répondre sans usurper son identité, ce qui ferait
                        // rejeter l'email par les filtres SPF et DKIM.
                        mail.ReplyToList.Add(new MailAddress(email, nom));

                        // Sujet sur une seule ligne : un retour chariot dans un
                        // en-tête permet d'injecter d'autres en-têtes, donc
                        // d'ajouter des destinataires cachés.
                        mail.Subject = SansRetourLigne("CAM-ALERT — " + sujet + " — " + nom);
                        mail.Body = ComposerCorps(nom, email, sujet, message);
                        mail.IsBodyHtml = true;

                        client.Send(mail);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                // Le message est déjà en base : l'échec d'envoi est journalisé,
                // pas affiché au visiteur, dont ce n'est pas le problème.
                Journal.EnregistrerErreur(ex, System.Web.HttpContext.Current);
                return false;
            }
        }

        /// <summary>
        /// Compose le corps HTML de l'email. Chaque valeur fournie par le
        /// visiteur est encodée : sans cela, un message contenant des balises
        /// était injecté tel quel dans l'email lu par les agents.
        /// </summary>
        private string ComposerCorps(string nom, string email, string sujet, string message)
        {
            string nomE = Server.HtmlEncode(nom);
            string emailE = Server.HtmlEncode(email);
            string sujetE = Server.HtmlEncode(sujet);
            string messageE = Server.HtmlEncode(message).Replace("\n", "<br/>");

            var corps = new StringBuilder();
            corps.Append(@"<html><head><meta charset=""utf-8"" /><style>
                body { font-family: Arial, sans-serif; }
                .container { padding: 20px; }
                h2 { color: #003f7d; }
                .label { font-weight: bold; color: #333; }
                .message { background: #f4f6fb; padding: 15px;
                           border-radius: 8px; margin-top: 10px; }
                .pied { color: #999; font-size: 12px; margin-top: 18px; }
            </style></head><body><div class=""container"">");

            corps.Append("<h2>Nouveau message depuis CAM-ALERT</h2>");
            corps.Append("<p><span class=\"label\">Nom :</span> ").Append(nomE).Append("</p>");
            corps.Append("<p><span class=\"label\">Email :</span> ").Append(emailE).Append("</p>");
            corps.Append("<p><span class=\"label\">Sujet :</span> ").Append(sujetE).Append("</p>");
            corps.Append("<div class=\"message\"><strong>Message :</strong><br/>")
                 .Append(messageE).Append("</div>");
            corps.Append("<p class=\"pied\">Reçu le ")
                 .Append(DateTime.Now.ToString("dd/MM/yyyy à HH:mm"))
                 .Append(". Répondre à cet email contacte directement le visiteur.</p>");
            corps.Append("</div></body></html>");

            return corps.ToString();
        }

        private static string SansRetourLigne(string valeur)
        {
            if (string.IsNullOrEmpty(valeur)) return string.Empty;
            return valeur.Replace("\r", " ").Replace("\n", " ");
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
            pnlError.Visible = true;
            pnlSuccess.Visible = false;

            // Le panneau d'erreur du balisage porte un texte générique : on y
            // ajoute la raison précise, encodée.
            pnlError.Controls.Add(new System.Web.UI.WebControls.Literal
            {
                Text = "<p style=\"margin:6px 0 0;\">"
                       + Server.HtmlEncode(message) + "</p>"
            });
        }
    }
}
