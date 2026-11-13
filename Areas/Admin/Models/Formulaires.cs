using System.ComponentModel.DataAnnotations;
using CamAlert.Web.Data;
using CamAlert.Web.Models;

namespace CamAlert.Web.Areas.Admin.Models;

public class ConnexionModele
{
    [Required(ErrorMessage = "Le login est obligatoire.")]
    [Display(Name = "Login")]
    public string? Login { get; set; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string? MotDePasse { get; set; }

    public string? ReturnUrl { get; set; }
}

public class DossierFormulaire
{
    public static readonly string[] Statuts = ["En cours", "Résolu", "Classé"];

    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(100, ErrorMessage = "100 caractères maximum.")]
    [Display(Name = "Titre / Nom")]
    public string? Titre { get; set; }

    [Required(ErrorMessage = "Choisissez un type.")]
    [Display(Name = "Type")]
    public int? TypeId { get; set; }

    [Required(ErrorMessage = "Choisissez une région.")]
    [Display(Name = "Région")]
    public int? RegionId { get; set; }

    [StringLength(100, ErrorMessage = "100 caractères maximum.")]
    [Display(Name = "Ville")]
    public string? Ville { get; set; }

    [Range(1900, 2100, ErrorMessage = "Année invalide (1900 à 2100).")]
    [Display(Name = "Année")]
    public int? Annee { get; set; }

    [Display(Name = "Publié sur le site")]
    public bool EstPublie { get; set; } = true;

    [Required]
    [Display(Name = "Statut du dossier")]
    public string Statut { get; set; } = "En cours";

    [Display(Name = "Commentaire de résolution")]
    public string? CommentaireResolution { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Signes distinctifs")]
    public string? SignesDistinctifs { get; set; }

    [Display(Name = "Photo (jpg, png — 5 Mo max)")]
    public IFormFile? Image { get; set; }

    public string? ImageUrl { get; set; }

    // Listes déroulantes
    public IReadOnlyList<Option> Types { get; set; } = [];
    public IReadOnlyList<Option> Regions { get; set; } = [];

    public static DossierFormulaire Depuis(DossierEdition d) => new()
    {
        Id = d.Id, Titre = d.Titre, TypeId = d.TypeId, RegionId = d.RegionId, Ville = d.Ville,
        Annee = d.Annee, EstPublie = d.EstPublie, Statut = d.Statut,
        CommentaireResolution = d.CommentaireResolution, Description = d.Description,
        SignesDistinctifs = d.SignesDistinctifs, ImageUrl = d.ImageUrl,
    };

    public DossierEdition VersEdition(string? nouvelleImage) => new()
    {
        Id = Id, Titre = Titre!.Trim(), TypeId = TypeId!.Value, RegionId = RegionId!.Value,
        Ville = Ville?.Trim(), Annee = Annee, EstPublie = EstPublie, Statut = Statut,
        CommentaireResolution = CommentaireResolution, Description = Description,
        SignesDistinctifs = SignesDistinctifs, ImageUrl = nouvelleImage,
    };
}

public class SignalementDetailModele
{
    public required SignalementAdmin Signalement { get; init; }
    public required IReadOnlyList<Option> Statuts { get; init; }
    public bool PieceJointeDisponible { get; init; }
}
