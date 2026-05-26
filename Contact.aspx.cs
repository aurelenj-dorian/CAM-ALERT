using System;
using System.Net;
using System.Net.Mail;
using System.Web.UI;

namespace CamAlert
{
    public partial class Contact : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnEnvoyer_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string nom = txtNom.Text.Trim();
            string email = txtEmail.Text.Trim();
            string sujet = ddlSujet.SelectedValue;
            string message = txtMessage.Text.Trim();

            // Construction de l'email en HTML
            string corps = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ padding: 20px; }}
                    h2 {{ color: #003f7d; }}
                    .label {{ font-weight: bold; color: #333; }}
                    .message {{ background: #f4f6fb; padding: 15px; border-radius: 8px; margin-top: 10px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>📩 Nouveau message depuis CAM-ALERT</h2>
                    <p><span class='label'>👤 Nom :</span> {nom}</p>
                    <p><span class='label'>📧 Email :</span> {email}</p>
                    <p><span class='label'>📌 Sujet :</span> {sujet}</p>
                    <div class='message'>
                        <strong>💬 Message :</strong><br/>
                        {message.Replace("\n", "<br/>")}
                    </div>
                </div>
            </body>
            </html>";

            try
            {
                using (SmtpClient client = new SmtpClient())
                {
                    client.Host = "smtp.gmail.com";
                    client.Port = 587;
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("aureledorian@gmail.com", "sxnc ieny uguy ucnw");

                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress("aureledorian@gmail.com", "CAM-ALERT - Portail Camerounais");

                        // === AJOUT DES DESTINATAIRES ===
                        mail.To.Add("aureledorian@gmail.com");
                        mail.To.Add("Falmatamamadou690@gmail.com");
                        mail.To.Add("yannobato@gmail.com");
                        mail.To.Add("lionelguigouon@gmail.com");

                        mail.Subject = $"CAM-ALERT - {sujet} - {nom}";
                        mail.Body = corps;
                        mail.IsBodyHtml = true;

                        client.Send(mail);
                    }
                }

                pnlSuccess.Visible = true;
                pnlError.Visible = false;

                // Vider le formulaire après envoi
                txtNom.Text = "";
                txtEmail.Text = "";
                ddlSujet.SelectedIndex = 0;
                txtMessage.Text = "";
            }
            catch (Exception ex)
            {
                pnlError.Visible = true;
                pnlSuccess.Visible = false;
                System.Diagnostics.Debug.WriteLine("Erreur envoi email: " + ex.Message);
            }
        }
    }
}