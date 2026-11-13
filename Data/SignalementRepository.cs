using CamAlert.Web.Models;
using Dapper;

namespace CamAlert.Web.Data;

public class SignalementRepository(ConnexionSql sql)
{
    public async Task CreerAsync(int dossierId, string? nom, string? email, string message, string? fichierJoint)
    {
        await using var cn = sql.Creer();
        await cn.ExecuteAsync(@"
            INSERT INTO Signalements (DossierID, NomDeclarant, Email, Message, FichierJoint, StatutID)
            VALUES (@DossierId, @Nom, @Email, @Message, @FichierJoint, 1)",
            new { DossierId = dossierId, Nom = nom, Email = email, Message = message, FichierJoint = fichierJoint });
    }

    // ========== ADMIN ==========

    public async Task<IReadOnlyList<SignalementAdmin>> ListerAsync()
    {
        await using var cn = sql.Creer();
        return (await cn.QueryAsync<SignalementAdmin>(Selection + " ORDER BY s.DateSoumission DESC")).ToList();
    }

    public async Task<SignalementAdmin?> ObtenirAsync(int id)
    {
        await using var cn = sql.Creer();
        return await cn.QuerySingleOrDefaultAsync<SignalementAdmin>(
            Selection + " WHERE s.SignalementID = @Id", new { Id = id });
    }

    public async Task<IReadOnlyList<Option>> StatutsAsync()
    {
        await using var cn = sql.Creer();
        return (await cn.QueryAsync<Option>("SELECT StatutID AS Id, Libelle FROM Statuts ORDER BY StatutID")).ToList();
    }

    public async Task ChangerStatutAsync(int id, int statutId)
    {
        await using var cn = sql.Creer();
        await cn.ExecuteAsync("UPDATE Signalements SET StatutID = @StatutId WHERE SignalementID = @Id",
            new { Id = id, StatutId = statutId });
    }

    public async Task CommenterAsync(int id, string? commentaire)
    {
        await using var cn = sql.Creer();
        await cn.ExecuteAsync("UPDATE Signalements SET CommentaireInterne = @Commentaire WHERE SignalementID = @Id",
            new { Id = id, Commentaire = commentaire });
    }

    // Signalements des 12 derniers mois actifs, en ordre chronologique
    public async Task<IReadOnlyList<ValeurLibellee>> ParMoisAsync()
    {
        await using var cn = sql.Creer();
        return (await cn.QueryAsync<ValeurLibellee>(@"
            SELECT Libelle, Valeur FROM (
                SELECT TOP 12 CONVERT(char(7), DateSoumission, 120) AS Libelle, COUNT(*) AS Valeur
                FROM Signalements
                WHERE DateSoumission IS NOT NULL
                GROUP BY CONVERT(char(7), DateSoumission, 120)
                ORDER BY Libelle DESC
            ) m ORDER BY Libelle")).ToList();
    }

    public async Task<IReadOnlyList<ValeurLibellee>> ParRegionAsync()
    {
        await using var cn = sql.Creer();
        return (await cn.QueryAsync<ValeurLibellee>(@"
            SELECT r.Nom AS Libelle, COUNT(s.SignalementID) AS Valeur
            FROM Signalements s
            INNER JOIN Dossiers d ON s.DossierID = d.DossierID
            INNER JOIN Regions  r ON d.RegionID  = r.RegionID
            GROUP BY r.Nom
            ORDER BY Valeur DESC")).ToList();
    }

    private const string Selection = @"
        SELECT s.SignalementID AS Id, s.DossierID AS DossierId, d.Titre AS Dossier,
               s.NomDeclarant, s.Email, s.Message, s.FichierJoint, s.StatutID AS StatutId,
               st.Libelle AS Statut, s.CommentaireInterne, s.DateSoumission
        FROM Signalements s
        INNER JOIN Dossiers d  ON s.DossierID = d.DossierID
        INNER JOIN Statuts  st ON s.StatutID  = st.StatutID";
}

public class SignalementAdmin
{
    public int Id { get; set; }
    public int DossierId { get; set; }
    public string Dossier { get; set; } = "";
    public string? NomDeclarant { get; set; }
    public string? Email { get; set; }
    public string Message { get; set; } = "";
    public string? FichierJoint { get; set; }
    public int StatutId { get; set; }
    public string Statut { get; set; } = "";
    public string? CommentaireInterne { get; set; }
    public DateTime? DateSoumission { get; set; }
}

public class ValeurLibellee
{
    public string Libelle { get; set; } = "";
    public int Valeur { get; set; }
}
