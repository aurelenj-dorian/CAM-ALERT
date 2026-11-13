using System.Text.Json;

namespace CamAlert.Web.Security;

// Lecture du cookie de consentement posé par le front-end React
public record Consentement(int Version, bool Preferences, bool Audience)
{
    public const string NomCookie = "camalert_consent";
    public const string NomCookieVisiteur = "camalert_vid";
    public const int VersionCourante = 1;

    public static Consentement? Lire(HttpRequest requete)
    {
        if (!requete.Cookies.TryGetValue(NomCookie, out var brut) || string.IsNullOrEmpty(brut))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(Uri.UnescapeDataString(brut));
            var racine = doc.RootElement;
            int version = racine.TryGetProperty("v", out var v) && v.TryGetInt32(out int n) ? n : 0;
            if (version != VersionCourante) return null;
            bool preferences = racine.TryGetProperty("preferences", out var p) && p.ValueKind == JsonValueKind.True;
            bool audience = racine.TryGetProperty("audience", out var a) && a.ValueKind == JsonValueKind.True;
            return new Consentement(version, preferences, audience);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
