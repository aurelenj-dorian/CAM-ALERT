using System.Security.Cryptography;
using System.Text;

namespace CamAlert.Web.Security;

// Hachage PBKDF2, compatible avec le format de l'ancien site :
// PBKDF2$iterations$selBase64$hashBase64 (et migration des anciens SHA-256 hexadécimaux)
public static class MotDePasse
{
    private const int Iterations = 100_000;
    private const int TailleSel = 16;
    private const int TailleHash = 32;

    public static string Hacher(string motDePasse)
    {
        byte[] sel = RandomNumberGenerator.GetBytes(TailleSel);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, Iterations, HashAlgorithmName.SHA256, TailleHash);
        return $"PBKDF2${Iterations}${Convert.ToBase64String(sel)}${Convert.ToBase64String(hash)}";
    }

    // doitMigrer = true si le hachage stocké est un ancien SHA-256 à remplacer
    public static bool Verifier(string motDePasse, string? stocke, out bool doitMigrer)
    {
        doitMigrer = false;
        if (string.IsNullOrEmpty(motDePasse) || string.IsNullOrWhiteSpace(stocke)) return false;
        stocke = stocke.Trim();

        if (stocke.StartsWith("PBKDF2$", StringComparison.Ordinal))
        {
            string[] parties = stocke.Split('$');
            if (parties.Length != 4 || !int.TryParse(parties[1], out int iterations) || iterations <= 0)
                return false;
            try
            {
                byte[] sel = Convert.FromBase64String(parties[2]);
                byte[] attendu = Convert.FromBase64String(parties[3]);
                byte[] calcule = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, iterations, HashAlgorithmName.SHA256, attendu.Length);
                return CryptographicOperations.FixedTimeEquals(calcule, attendu);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Ancien format : SHA-256 hexadécimal sans sel
        string sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(motDePasse)));
        bool valide = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(sha256), Encoding.ASCII.GetBytes(stocke.ToUpperInvariant()));
        doitMigrer = valide;
        return valide;
    }
}
