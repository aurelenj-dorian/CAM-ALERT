using Microsoft.Extensions.Caching.Memory;

namespace CamAlert.Web.Security;

// Blocage d'un login après 5 échecs, pendant 15 minutes après le dernier échec
public class VerrouConnexion(IMemoryCache cache)
{
    private const int MaxEchecs = 5;
    private static readonly TimeSpan DureeBlocage = TimeSpan.FromMinutes(15);
    private readonly Lock verrou = new();

    private sealed class Compteur { public int Nombre; }

    private static string Cle(string login) => "echecs:" + login.ToLowerInvariant();

    public bool EstBloque(string login) =>
        cache.TryGetValue(Cle(login), out Compteur? c) && c!.Nombre >= MaxEchecs;

    public void EnregistrerEchec(string login)
    {
        lock (verrou)
        {
            var c = cache.Get<Compteur>(Cle(login)) ?? new Compteur();
            c.Nombre++;
            cache.Set(Cle(login), c, DureeBlocage);
        }
    }

    public void Reinitialiser(string login) => cache.Remove(Cle(login));
}
