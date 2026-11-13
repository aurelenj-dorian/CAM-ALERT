namespace CamAlert.Web.Security;

// Validation (taille, extension, signature) et enregistrement des fichiers envoyés
public class StockageFichiers(IWebHostEnvironment env)
{
    public const long TailleMax = 5 * 1024 * 1024; // 5 Mo

    public static readonly string[] ExtensionsImage = [".jpg", ".jpeg", ".png"];
    public static readonly string[] ExtensionsSignalement = [".jpg", ".jpeg", ".png", ".pdf"];

    private static readonly Dictionary<string, byte[]> Signatures = new()
    {
        [".jpg"] = [0xFF, 0xD8, 0xFF],
        [".jpeg"] = [0xFF, 0xD8, 0xFF],
        [".png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        [".pdf"] = [0x25, 0x50, 0x44, 0x46], // %PDF
    };

    // Dossier privé des pièces jointes de signalement (jamais servi en statique)
    public string DossierPiecesJointes => Path.Combine(env.ContentRootPath, "App_Data", "uploads");

    // Retourne null si le fichier est accepté, sinon le message d'erreur
    public static string? Valider(IFormFile? fichier, string[] extensionsAutorisees)
    {
        if (fichier == null || fichier.Length == 0) return null;

        if (fichier.Length > TailleMax)
            return "Fichier trop volumineux (5 Mo maximum).";

        string extension = Path.GetExtension(fichier.FileName).ToLowerInvariant();
        if (!extensionsAutorisees.Contains(extension))
            return "Type de fichier non autorisé (" + string.Join(", ", extensionsAutorisees) + ").";

        byte[] attendu = Signatures[extension];
        byte[] entete = new byte[attendu.Length];
        using (var flux = fichier.OpenReadStream())
        {
            int lus = flux.ReadAtLeast(entete, entete.Length, throwOnEndOfStream: false);
            if (lus < attendu.Length || !entete.AsSpan().SequenceEqual(attendu))
                return "Le contenu du fichier ne correspond pas à son extension.";
        }
        return null;
    }

    // Image de dossier : enregistrée dans wwwroot/uploads/dossiers, retourne son URL publique
    public async Task<string> EnregistrerImageDossierAsync(IFormFile fichier)
    {
        string dossier = Path.Combine(env.WebRootPath, "uploads", "dossiers");
        string nom = await EnregistrerAsync(fichier, dossier);
        return "/uploads/dossiers/" + nom;
    }

    // Pièce jointe : enregistrée dans App_Data/uploads, retourne le nom à stocker en base
    public async Task<string> EnregistrerPieceJointeAsync(IFormFile fichier)
    {
        return await EnregistrerAsync(fichier, DossierPiecesJointes);
    }

    // Chemin physique d'une pièce jointe à partir de la valeur en base (ancien format "/Uploads/xxx" accepté)
    public string? CheminPieceJointe(string? valeurEnBase)
    {
        if (string.IsNullOrWhiteSpace(valeurEnBase)) return null;
        string nom = Path.GetFileName(valeurEnBase.Replace('\\', '/'));
        if (string.IsNullOrEmpty(nom)) return null;
        string chemin = Path.Combine(DossierPiecesJointes, nom);
        return File.Exists(chemin) ? chemin : null;
    }

    private static async Task<string> EnregistrerAsync(IFormFile fichier, string dossier)
    {
        Directory.CreateDirectory(dossier);
        string nom = Guid.NewGuid().ToString("N") + Path.GetExtension(fichier.FileName).ToLowerInvariant();
        await using var sortie = File.Create(Path.Combine(dossier, nom));
        await fichier.CopyToAsync(sortie);
        return nom;
    }
}
