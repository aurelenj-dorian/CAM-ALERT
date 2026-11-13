using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;

namespace CamAlert.Web.Services;

public class SmtpOptions
{
    public string Hote { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Compte { get; set; } = "";
    public string MotDePasse { get; set; } = "";
    public string NomExpediteur { get; set; } = "CAM-ALERT";
    public string[] Destinataires { get; set; } = [];

    public bool EstConfigure =>
        !string.IsNullOrWhiteSpace(Hote) && !string.IsNullOrWhiteSpace(Compte) &&
        !string.IsNullOrWhiteSpace(MotDePasse) && Destinataires.Length > 0;
}

public class EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> logger)
{
    private readonly SmtpOptions smtp = options.Value;

    // Retourne false si l'envoi a échoué (configuration absente ou erreur SMTP)
    public async Task<bool> EnvoyerContactAsync(string nom, string email, string sujet, string message)
    {
        if (!smtp.EstConfigure)
        {
            logger.LogError("Configuration SMTP incomplète (section Smtp) : message de contact non envoyé.");
            return false;
        }

        var html = HtmlEncoder.Default;
        string corps = $@"<html><body style=""font-family:Arial,sans-serif"">
            <h2 style=""color:#0F4C45"">Nouveau message depuis CAM-ALERT</h2>
            <p><b>Nom :</b> {html.Encode(nom)}</p>
            <p><b>Email :</b> {html.Encode(email)}</p>
            <p><b>Sujet :</b> {html.Encode(sujet)}</p>
            <div style=""background:#F6F7F9;padding:15px;border-radius:8px"">
                {html.Encode(message).Replace("&#xD;&#xA;", "<br/>").Replace("&#xA;", "<br/>")}
            </div></body></html>";

        try
        {
            using var client = new SmtpClient(smtp.Hote, smtp.Port)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtp.Compte, smtp.MotDePasse),
            };
            using var mail = new MailMessage
            {
                From = new MailAddress(smtp.Compte, smtp.NomExpediteur),
                Subject = $"CAM-ALERT - {sujet} - {nom}",
                Body = corps,
                IsBodyHtml = true,
            };
            foreach (var destinataire in smtp.Destinataires)
                mail.To.Add(destinataire);
            mail.ReplyToList.Add(new MailAddress(email, nom));

            await client.SendMailAsync(mail);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException)
        {
            logger.LogError(ex, "Échec de l'envoi du message de contact.");
            return false;
        }
    }
}
