namespace CamAlert.Web.Security;

// En-têtes de sécurité ajoutés à chaque réponse
public class EnTetesSecurite(RequestDelegate suivant, IWebHostEnvironment env)
{
    // En développement uniquement : outils de Visual Studio (Browser Link, rechargement à chaud)
    private const string OutilsDev = " http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*";

    private readonly string csp =
        "default-src 'self'; " +
        "script-src 'self'" + (env.IsDevelopment() ? OutilsDev : "") + "; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https://*.tile.openstreetmap.org; " +
        "font-src 'self'; " +
        "connect-src 'self'" + (env.IsDevelopment() ? OutilsDev : "") + "; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            h.ContentSecurityPolicy = csp;
            h.XContentTypeOptions = "nosniff";
            h.XFrameOptions = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            h.Remove("X-Powered-By");
            return Task.CompletedTask;
        });
        return suivant(context);
    }
}
