namespace CamAlert.Web.Models;

public class DossierResume
{
    public int Id { get; set; }
    public string Titre { get; set; } = "";
    public int? Annee { get; set; }
    public string? Ville { get; set; }
    public int TypeId { get; set; }
    public string Type { get; set; } = "";
    public string Region { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string Reference => "DOS-" + Id.ToString().PadLeft(4, '0');
}

public class DossierDetail : DossierResume
{
    public string? Description { get; set; }
    public string? SignesDistinctifs { get; set; }
    public DateTime? DatePublication { get; set; }
    public string? Statut { get; set; }
}

public record PageResultat<T>(IReadOnlyList<T> Elements, int Total, int Page, int TaillePage)
{
    public int NombrePages => Math.Max(1, (int)Math.Ceiling(Total / (double)TaillePage));
}

public record Option(int Id, string Libelle);

public record Referentiels(
    IReadOnlyList<Option> Types,
    IReadOnlyList<Option> Regions,
    IReadOnlyList<int> Annees,
    IReadOnlyList<Option> Dossiers);

public class DossierHistorique
{
    public int Id { get; set; }
    public string Titre { get; set; } = "";
    public string Type { get; set; } = "";
    public string Region { get; set; } = "";
    public int? Annee { get; set; }
    public string? Statut { get; set; }
    public DateTime? DateResolution { get; set; }
    public string? CommentaireResolution { get; set; }
}

// Signalement traité, tel qu'exposé publiquement : ni nom, ni message, ni e-mail
public class SignalementPublic
{
    public int Id { get; set; }
    public string Dossier { get; set; } = "";
    public string Statut { get; set; } = "";
    public DateTime? Date { get; set; }
}

public record Historique(
    int NbResolus,
    int NbEnCours,
    int NbSignalementsTraites,
    IReadOnlyList<DossierHistorique> Resolus,
    IReadOnlyList<SignalementPublic> SignalementsTraites,
    IReadOnlyList<DossierHistorique> Tous);

public class PointCarte
{
    public int Id { get; set; }
    public string Titre { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Ville { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime? DatePublication { get; set; }
}
