using CamAlert.Web.Data;
using CamAlert.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CamAlert.Web.Controllers.Api;

// Mesure d'audience : rien n'est enregistré sans consentement explicite
[ApiController]
[Route("api/audience")]
public class AudienceController(AudienceRepository audience) : ControllerBase
{
    public record PageVue(string? Chemin);

    [HttpPost]
    public async Task<IActionResult> Enregistrer([FromBody] PageVue vue)
    {
        var consentement = Consentement.Lire(Request);
        if (consentement is not { Audience: true })
        {
            SupprimerCookieVisiteur();
            return NoContent();
        }

        string? chemin = vue.Chemin;
        if (string.IsNullOrEmpty(chemin) || !chemin.StartsWith('/') || chemin.Length > 200)
            return NoContent();

        // Identifiant aléatoire du visiteur, posé par le serveur (HttpOnly)
        if (!Request.Cookies.TryGetValue(Consentement.NomCookieVisiteur, out var brut) ||
            !Guid.TryParse(brut, out Guid visiteur))
        {
            visiteur = Guid.NewGuid();
            Response.Cookies.Append(Consentement.NomCookieVisiteur, visiteur.ToString("N"), new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(395), // 13 mois
                Path = "/",
            });
        }

        await audience.EnregistrerAsync(visiteur, chemin);
        return NoContent();
    }

    // Appelé quand le visiteur retire son consentement
    [HttpPost("retrait")]
    public IActionResult Retirer()
    {
        SupprimerCookieVisiteur();
        return NoContent();
    }

    private void SupprimerCookieVisiteur()
    {
        if (Request.Cookies.ContainsKey(Consentement.NomCookieVisiteur))
            Response.Cookies.Delete(Consentement.NomCookieVisiteur, new CookieOptions { Path = "/" });
    }
}
