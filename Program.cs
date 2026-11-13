using System.Threading.RateLimiting;
using CamAlert.Web.Data;
using CamAlert.Web.Security;
using CamAlert.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Pas d'en-tête « Server: Kestrel » dans les réponses
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// ========== SERVICES ==========

builder.Services.AddControllersWithViews(options =>
{
    // Tous les formulaires admin et POST publics exigent le jeton anti-CSRF
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Messages de conversion en français (ex. « abc » saisi dans un champ numérique)
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((valeur, champ) => $"La valeur « {valeur} » n'est pas valide pour {champ}.");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(valeur => $"La valeur « {valeur} » n'est pas valide.");
    messages.SetValueIsInvalidAccessor(valeur => $"La valeur « {valeur} » n'est pas valide.");
    messages.SetValueMustBeANumberAccessor(champ => $"Le champ {champ} doit être un nombre.");
    messages.SetValueMustNotBeNullAccessor(valeur => "Ce champ est obligatoire.");
    messages.SetMissingKeyOrValueAccessor(() => "Ce champ est obligatoire.");
})
// Les actions valident elles-mêmes : toutes les erreurs (annotations + règles métier) sont renvoyées ensemble
.ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "camalert_csrf";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Connexion admin par cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "camalert_admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.Path = "/admin";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/admin/connexion";
        options.LogoutPath = "/admin/deconnexion";
        options.AccessDeniedPath = "/admin/connexion";
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Roles.PolitiqueAdmin, p => p.RequireRole(Roles.Administrateur));

// 5 envois de formulaire par minute et par IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("formulaires", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
    options.OnRejected = async (context, token) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Trop d'envois en peu de temps. Réessayez dans une minute." }, token);
    };
});

// Messages de confirmation de l'admin (cookie limité à /admin)
builder.Services.Configure<CookieTempDataProviderOptions>(options =>
{
    options.Cookie.Name = "camalert_flash";
    options.Cookie.Path = "/admin";
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddMemoryCache();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

builder.Services.AddSingleton<ConnexionSql>();
builder.Services.AddScoped<DossierRepository>();
builder.Services.AddScoped<SignalementRepository>();
builder.Services.AddScoped<UtilisateurRepository>();
builder.Services.AddScoped<AudienceRepository>();
builder.Services.AddSingleton<VerrouConnexion>();
builder.Services.AddSingleton<StockageFichiers>();
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// ========== PIPELINE ==========

if (!app.Environment.IsDevelopment())
{
    // Page d'erreur générique, sans détail technique
    app.UseExceptionHandler(erreur => erreur.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        if (feature?.Path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) == true)
            await context.Response.WriteAsJsonAsync(new { message = "Une erreur technique est survenue." });
        else
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(
                "<!doctype html><meta charset=utf-8><title>Erreur</title>" +
                "<p>Une erreur technique est survenue. <a href=\"/\">Retour à l'accueil</a></p>");
        }
    }));
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<EnTetesSecurite>();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Les fichiers de build Vite ont un nom haché : cache long
        if (ctx.Context.Request.Path.StartsWithSegments("/app/assets"))
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Toute autre route publique : application React (les routes /api et /admin inconnues restent en 404)
app.MapFallback(async context =>
{
    var chemin = context.Request.Path;
    var index = app.Environment.WebRootFileProvider.GetFileInfo("app/index.html");
    if (chemin.StartsWithSegments("/api") || chemin.StartsWithSegments("/admin") || !index.Exists)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    await context.Response.SendFileAsync(index);
});

app.Run();
