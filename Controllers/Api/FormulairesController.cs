using System.ComponentModel.DataAnnotations;
using CamAlert.Web.Data;
using CamAlert.Web.Security;
using CamAlert.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CamAlert.Web.Controllers.Api;

// Formulaires publics : signalement et contact (jeton anti-CSRF exigé par le filtre global)
[ApiController]
[Route("api")]
public class FormulairesController(
    DossierRepository dossiers,
    SignalementRepository signalements,
    StockageFichiers stockage,
    EmailService email) : ControllerBase
{
    private const int LimiteRequete = 25 * 1024 * 1024;

    public static readonly string[] SujetsContact =
    [
        "Signalement infraction",
        "Information personne recherchée",
        "Disparition",
        "Presse",
        "Recrutement",
        "Commission de contrôle",
        "Autre",
    ];

    // Pose le cookie XSRF-TOKEN lu par React et renvoyé dans l'en-tête X-XSRF-TOKEN
    [HttpGet("antiforgery")]
    public IActionResult JetonAntiforgery([FromServices] IAntiforgery antiforgery)
    {
        var jetons = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append("XSRF-TOKEN", jetons.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Path = "/",
        });
        return NoContent();
    }

    // Limite de requête large (25 Mo) : un fichier de plus de 5 Mo reçoit ainsi
    // le message de validation sur le champ « fichier » plutôt qu'un rejet global
    [HttpPost("signalements")]
    [EnableRateLimiting("formulaires")]
    [RequestSizeLimit(LimiteRequete)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteRequete)]
    public async Task<IActionResult> Signaler([FromForm] SignalementRequete r)
    {
        // Le dossier doit exister et être publié
        if (r.DossierId is int id && !await dossiers.EstPublieAsync(id))
            ModelState.AddModelError(nameof(r.DossierId), "Ce dossier n'existe pas ou n'est plus publié.");

        string? erreurFichier = StockageFichiers.Valider(r.Fichier, StockageFichiers.ExtensionsSignalement);
        if (erreurFichier != null)
            ModelState.AddModelError(nameof(r.Fichier), erreurFichier);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        string? fichier = r.Fichier is { Length: > 0 }
            ? await stockage.EnregistrerPieceJointeAsync(r.Fichier)
            : null;

        await signalements.CreerAsync(
            r.DossierId!.Value,
            string.IsNullOrWhiteSpace(r.Nom) ? null : r.Nom.Trim(),
            string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim(),
            r.Message!.Trim(),
            fichier);

        return Ok(new { message = "Votre signalement a bien été transmis. Merci pour votre aide." });
    }

    [HttpPost("contact")]
    [EnableRateLimiting("formulaires")]
    public async Task<IActionResult> Contacter([FromBody] ContactRequete r)
    {
        if (r.Sujet != null && !SujetsContact.Contains(r.Sujet))
            ModelState.AddModelError(nameof(r.Sujet), "Sujet inconnu.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Retours à la ligne retirés : ils servent à injecter des en-têtes dans le sujet
        static string UneLigne(string s) => s.Replace("\r", " ").Replace("\n", " ").Trim();

        bool envoye = await email.EnvoyerContactAsync(
            UneLigne(r.Nom!), UneLigne(r.Email!), r.Sujet!, r.Message!.Trim());

        return envoye
            ? Ok(new { message = "Votre message a bien été envoyé." })
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Le message n'a pas pu être envoyé. Réessayez plus tard." });
    }
}

public class SignalementRequete
{
    [Required(ErrorMessage = "Veuillez sélectionner un dossier.")]
    public int? DossierId { get; set; }

    [StringLength(100, ErrorMessage = "100 caractères maximum.")]
    public string? Nom { get; set; }

    [EmailAddress(ErrorMessage = "Adresse e-mail invalide.")]
    [StringLength(150, ErrorMessage = "150 caractères maximum.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Le message est obligatoire.")]
    [StringLength(4000, ErrorMessage = "4000 caractères maximum.")]
    public string? Message { get; set; }

    public IFormFile? Fichier { get; set; }
}

public class ContactRequete
{
    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(100, ErrorMessage = "100 caractères maximum.")]
    public string? Nom { get; set; }

    [Required(ErrorMessage = "L'adresse e-mail est obligatoire.")]
    [EmailAddress(ErrorMessage = "Adresse e-mail invalide.")]
    [StringLength(150, ErrorMessage = "150 caractères maximum.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Choisissez un sujet.")]
    public string? Sujet { get; set; }

    [Required(ErrorMessage = "Le message est obligatoire.")]
    [StringLength(4000, ErrorMessage = "4000 caractères maximum.")]
    public string? Message { get; set; }
}
