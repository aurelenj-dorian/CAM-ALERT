using CamAlert.Web.Areas.Admin.Models;
using CamAlert.Web.Data;
using CamAlert.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CamAlert.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = Roles.PolitiqueAdmin)]
[Route("admin/dossiers")]
public class DossiersController(DossierRepository dossiers, StockageFichiers stockage) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await dossiers.ListerTousAsync());

    [HttpGet("nouveau")]
    public async Task<IActionResult> Nouveau() => View("Formulaire", await AvecListes(new DossierFormulaire()));

    [HttpPost("nouveau")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Nouveau(DossierFormulaire modele)
    {
        if (!await Valider(modele))
            return View("Formulaire", await AvecListes(modele));

        string? image = modele.Image is { Length: > 0 }
            ? await stockage.EnregistrerImageDossierAsync(modele.Image)
            : null;
        await dossiers.CreerAsync(modele.VersEdition(image));
        TempData["Message"] = "Dossier ajouté.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/modifier")]
    public async Task<IActionResult> Modifier(int id)
    {
        var dossier = await dossiers.ObtenirAsync(id);
        if (dossier is null) return NotFound();
        return View("Formulaire", await AvecListes(DossierFormulaire.Depuis(dossier)));
    }

    [HttpPost("{id:int}/modifier")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Modifier(int id, DossierFormulaire modele)
    {
        var existant = await dossiers.ObtenirAsync(id);
        if (existant is null) return NotFound();
        modele.Id = id;
        modele.ImageUrl = existant.ImageUrl;

        if (!await Valider(modele))
            return View("Formulaire", await AvecListes(modele));

        string? image = modele.Image is { Length: > 0 }
            ? await stockage.EnregistrerImageDossierAsync(modele.Image)
            : null;
        await dossiers.ModifierAsync(modele.VersEdition(image));
        TempData["Message"] = "Dossier modifié.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/supprimer")]
    public async Task<IActionResult> Supprimer(int id)
    {
        await dossiers.SupprimerAsync(id);
        TempData["Message"] = "Dossier supprimé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> Valider(DossierFormulaire modele)
    {
        if (!DossierFormulaire.Statuts.Contains(modele.Statut))
            ModelState.AddModelError(nameof(modele.Statut), "Statut inconnu.");

        var (types, regions) = await dossiers.ListesAsync();
        if (modele.TypeId is int t && types.All(x => x.Id != t))
            ModelState.AddModelError(nameof(modele.TypeId), "Type inconnu.");
        if (modele.RegionId is int r && regions.All(x => x.Id != r))
            ModelState.AddModelError(nameof(modele.RegionId), "Région inconnue.");

        string? erreur = StockageFichiers.Valider(modele.Image, StockageFichiers.ExtensionsImage);
        if (erreur != null)
            ModelState.AddModelError(nameof(modele.Image), erreur);

        return ModelState.IsValid;
    }

    private async Task<DossierFormulaire> AvecListes(DossierFormulaire modele)
    {
        var (types, regions) = await dossiers.ListesAsync();
        modele.Types = types;
        modele.Regions = regions;
        return modele;
    }
}
