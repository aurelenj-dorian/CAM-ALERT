using CamAlert.Web.Areas.Admin.Models;
using CamAlert.Web.Data;
using CamAlert.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace CamAlert.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = Roles.PolitiqueAdmin)]
[Route("admin/signalements")]
public class SignalementsController(SignalementRepository signalements, StockageFichiers stockage) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await signalements.ListerAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var s = await signalements.ObtenirAsync(id);
        if (s is null) return NotFound();
        return View(new SignalementDetailModele
        {
            Signalement = s,
            Statuts = await signalements.StatutsAsync(),
            PieceJointeDisponible = stockage.CheminPieceJointe(s.FichierJoint) != null,
        });
    }

    [HttpPost("{id:int}/statut")]
    public async Task<IActionResult> Statut(int id, int statutId)
    {
        var statuts = await signalements.StatutsAsync();
        if (statuts.Any(s => s.Id == statutId))
        {
            await signalements.ChangerStatutAsync(id, statutId);
            TempData["Message"] = "Statut mis à jour.";
        }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:int}/commentaire")]
    public async Task<IActionResult> Commentaire(int id, string? commentaire)
    {
        await signalements.CommenterAsync(id, commentaire);
        TempData["Message"] = "Commentaire enregistré.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Pièce jointe servie uniquement aux administrateurs, en téléchargement
    [HttpGet("{id:int}/piece-jointe")]
    public async Task<IActionResult> PieceJointe(int id)
    {
        var s = await signalements.ObtenirAsync(id);
        string? chemin = stockage.CheminPieceJointe(s?.FichierJoint);
        if (chemin is null) return NotFound();

        if (!new FileExtensionContentTypeProvider().TryGetContentType(chemin, out var type))
            type = "application/octet-stream";
        return PhysicalFile(chemin, type, $"signalement-{id}{Path.GetExtension(chemin)}");
    }
}
