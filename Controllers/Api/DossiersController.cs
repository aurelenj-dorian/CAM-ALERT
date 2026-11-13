using CamAlert.Web.Data;
using CamAlert.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CamAlert.Web.Controllers.Api;

[ApiController]
[Route("api")]
public class DossiersController(DossierRepository dossiers) : ControllerBase
{
    private const int TaillePage = 12;

    // Les paramètres non numériques ou hors limites sont ignorés (pas d'erreur 400)
    [HttpGet("dossiers")]
    public async Task<PageResultat<DossierResume>> Rechercher(
        string? q, string? type, string? annee, string? page)
    {
        int? typeId = int.TryParse(type, out int t) && t > 0 ? t : null;
        int? an = int.TryParse(annee, out int a) && a is >= 1900 and <= 2100 ? a : null;
        int numPage = int.TryParse(page, out int p) && p is >= 1 and <= 10_000 ? p : 1;
        string? recherche = string.IsNullOrWhiteSpace(q) ? null : q.Trim()[..Math.Min(q.Trim().Length, 100)];

        var resultat = await dossiers.RechercherAsync(recherche, typeId, an, numPage, TaillePage);

        // Page au-delà de la dernière : on renvoie la dernière page plutôt qu'une liste vide
        if (resultat.Elements.Count == 0 && resultat.Total > 0)
            resultat = await dossiers.RechercherAsync(recherche, typeId, an, resultat.NombrePages, TaillePage);
        return resultat;
    }

    [HttpGet("dossiers/{id:int}")]
    public async Task<ActionResult<DossierDetail>> Detail(int id)
    {
        var dossier = await dossiers.DetailPublieAsync(id);
        return dossier is null ? NotFound(new { message = "Dossier introuvable." }) : dossier;
    }

    [HttpGet("dossiers/suggestions")]
    public async Task<IReadOnlyList<DossierResume>> Suggestions(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) return [];
        return await dossiers.SuggestionsAsync(q.Trim()[..Math.Min(q.Trim().Length, 100)]);
    }

    [HttpGet("referentiels")]
    public Task<Referentiels> Referentiels() => dossiers.ReferentielsAsync();

    [HttpGet("historique")]
    public Task<Historique> Historique() => dossiers.HistoriqueAsync();
}
