using System.Security.Claims;
using CamAlert.Web.Areas.Admin.Models;
using CamAlert.Web.Data;
using CamAlert.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CamAlert.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class CompteController(
    UtilisateurRepository utilisateurs,
    VerrouConnexion verrou,
    ILogger<CompteController> logger) : Controller
{
    private const string ErreurGenerique = "Login ou mot de passe incorrect.";

    [HttpGet("admin/connexion")]
    [AllowAnonymous]
    public IActionResult Connexion(string? returnUrl)
    {
        if (User.IsInRole(Roles.Administrateur))
            return Redirect(UrlRetourSure(returnUrl));
        return View(new ConnexionModele { ReturnUrl = returnUrl });
    }

    [HttpPost("admin/connexion")]
    [AllowAnonymous]
    public async Task<IActionResult> Connexion(ConnexionModele modele)
    {
        if (!ModelState.IsValid)
            return View(modele);

        string login = modele.Login!.Trim();

        if (verrou.EstBloque(login))
        {
            ModelState.AddModelError("", "Trop de tentatives échouées. Réessayez dans 15 minutes.");
            return View(modele);
        }

        var utilisateur = await utilisateurs.TrouverParLoginAsync(login);

        // Seul un administrateur actif peut se connecter ; même message dans tous les autres cas
        if (utilisateur is { EstActif: true } &&
            utilisateur.Role.Equals(Roles.Administrateur, StringComparison.OrdinalIgnoreCase) &&
            MotDePasse.Verifier(modele.MotDePasse!, utilisateur.MotDePasse, out bool doitMigrer))
        {
            if (doitMigrer)
                await utilisateurs.MettreAJourMotDePasseAsync(utilisateur.Id, MotDePasse.Hacher(modele.MotDePasse!));

            verrou.Reinitialiser(login);
            var identite = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, utilisateur.Login),
                new Claim(ClaimTypes.Role, Roles.Administrateur),
            ], CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identite));
            logger.LogInformation("Connexion admin de {Login}.", utilisateur.Login);
            return Redirect(UrlRetourSure(modele.ReturnUrl));
        }

        verrou.EnregistrerEchec(login);
        ModelState.AddModelError("", ErreurGenerique);
        return View(modele);
    }

    [HttpPost("admin/deconnexion")]
    [Authorize(Policy = Roles.PolitiqueAdmin)]
    public async Task<IActionResult> Deconnexion()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/admin/connexion");
    }

    [HttpGet("admin")]
    [Authorize(Policy = Roles.PolitiqueAdmin)]
    public IActionResult Accueil() => Redirect("/admin/dossiers");

    // Uniquement une URL locale de l'espace admin : pas de redirection ouverte
    private string UrlRetourSure(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) &&
        returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            ? returnUrl
            : "/admin/dossiers";
}
