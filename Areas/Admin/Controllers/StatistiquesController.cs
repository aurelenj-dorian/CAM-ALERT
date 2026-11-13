using CamAlert.Web.Data;
using CamAlert.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CamAlert.Web.Areas.Admin.Controllers;

// Graphiques, carte et audience : pages + données JSON chargées par admin.js
[Area("Admin")]
[Authorize(Policy = Roles.PolitiqueAdmin)]
[Route("admin")]
public class StatistiquesController(
    SignalementRepository signalements,
    DossierRepository dossiers,
    AudienceRepository audience) : Controller
{
    [HttpGet("graphiques")]
    public IActionResult Graphiques() => View();

    [HttpGet("graphiques/donnees")]
    public async Task<IActionResult> DonneesGraphiques() => Json(new
    {
        parMois = await signalements.ParMoisAsync(),
        parRegion = await signalements.ParRegionAsync(),
    });

    [HttpGet("carte")]
    public IActionResult Carte() => View();

    [HttpGet("carte/donnees")]
    public async Task<IActionResult> DonneesCarte() => Json(await dossiers.PointsCarteAsync());

    [HttpGet("audience")]
    public async Task<IActionResult> Audience() => View(await audience.StatistiquesAsync(30));
}
