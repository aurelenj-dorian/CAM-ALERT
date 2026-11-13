using CamAlert.Web.Models;
using Dapper;

namespace CamAlert.Web.Data;

public class DossierRepository(ConnexionSql sql)
{
    private const string ColonnesResume = @"
        d.DossierID AS Id, d.Titre, d.Annee, d.Ville, d.ImageUrl,
        d.TypeID AS TypeId, t.Libelle AS Type, r.Nom AS Region";

    private const string Jointures = @"
        FROM Dossiers d
        INNER JOIN TypesDossier t ON d.TypeID   = t.TypeID
        INNER JOIN Regions      r ON d.RegionID = r.RegionID";

    // ========== PUBLIC (dossiers publiés uniquement) ==========

    public async Task<PageResultat<DossierResume>> RechercherAsync(
        string? q, int? type, int? annee, int page, int taillePage)
    {
        var filtre = " WHERE d.EstPublie = 1";
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(q))
        {
            filtre += " AND (d.Titre LIKE @Q OR d.Ville LIKE @Q OR r.Nom LIKE @Q)";
            p.Add("Q", ConnexionSql.Contient(q.Trim()));
        }
        if (type.HasValue) { filtre += " AND d.TypeID = @Type"; p.Add("Type", type); }
        if (annee.HasValue) { filtre += " AND d.Annee = @Annee"; p.Add("Annee", annee); }
        p.Add("Saut", (page - 1) * taillePage);
        p.Add("Taille", taillePage);

        string requete = $@"
            SELECT COUNT(*) {Jointures} {filtre};
            SELECT {ColonnesResume} {Jointures} {filtre}
            ORDER BY d.DatePublication DESC, d.DossierID DESC
            OFFSET @Saut ROWS FETCH NEXT @Taille ROWS ONLY;";

        await using var cn = sql.Creer();
        using var multi = await cn.QueryMultipleAsync(requete, p);
        int total = await multi.ReadSingleAsync<int>();
        var elements = (await multi.ReadAsync<DossierResume>()).ToList();
        return new PageResultat<DossierResume>(elements, total, page, taillePage);
    }

    public async Task<DossierDetail?> DetailPublieAsync(int id)
    {
        await using var cn = sql.Creer();
        return await cn.QuerySingleOrDefaultAsync<DossierDetail>($@"
            SELECT {ColonnesResume}, d.Description, d.SignesDistinctifs,
                   d.DatePublication, d.StatutDossier AS Statut
            {Jointures}
            WHERE d.DossierID = @Id AND d.EstPublie = 1", new { Id = id });
    }

    public async Task<IReadOnlyList<DossierResume>> SuggestionsAsync(string q)
    {
        await using var cn = sql.Creer();
        return (await cn.QueryAsync<DossierResume>($@"
            SELECT TOP 8 {ColonnesResume} {Jointures}
            WHERE d.EstPublie = 1 AND (d.Titre LIKE @Q OR d.Ville LIKE @Q OR r.Nom LIKE @Q)
            ORDER BY d.DatePublication DESC", new { Q = ConnexionSql.Contient(q.Trim()) })).ToList();
    }

    public async Task<Referentiels> ReferentielsAsync()
    {
        await using var cn = sql.Creer();
        using var multi = await cn.QueryMultipleAsync(@"
            SELECT TypeID AS Id, Libelle FROM TypesDossier ORDER BY TypeID;
            SELECT RegionID AS Id, Nom AS Libelle FROM Regions ORDER BY Nom;
            SELECT DISTINCT Annee FROM Dossiers WHERE EstPublie = 1 AND Annee IS NOT NULL ORDER BY Annee DESC;
            SELECT DossierID AS Id, Titre AS Libelle FROM Dossiers WHERE EstPublie = 1 ORDER BY Titre;");
        return new Referentiels(
            (await multi.ReadAsync<Option>()).ToList(),
            (await multi.ReadAsync<Option>()).ToList(),
            (await multi.ReadAsync<int>()).ToList(),
            (await multi.ReadAsync<Option>()).ToList());
    }

    public async Task<bool> EstPublieAsync(int id)
    {
        await using var cn = sql.Creer();
        return await cn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Dossiers WHERE DossierID = @Id AND EstPublie = 1", new { Id = id }) > 0;
    }

    public async Task<Historique> HistoriqueAsync()
    {
        await using var cn = sql.Creer();
        using var multi = await cn.QueryMultipleAsync($@"
            SELECT COUNT(*) FROM Dossiers WHERE StatutDossier = N'Résolu' AND EstPublie = 1;
            SELECT COUNT(*) FROM Dossiers WHERE StatutDossier = N'En cours' AND EstPublie = 1;
            SELECT COUNT(*) FROM Signalements s
                INNER JOIN Dossiers d ON s.DossierID = d.DossierID
                WHERE s.StatutID = 3 AND d.EstPublie = 1;

            SELECT d.DossierID AS Id, d.Titre, t.Libelle AS Type, r.Nom AS Region, d.Annee,
                   d.StatutDossier AS Statut, d.DateResolution, d.CommentaireResolution
            {Jointures}
            WHERE d.StatutDossier = N'Résolu' AND d.EstPublie = 1
            ORDER BY d.DateResolution DESC;

            -- Ni nom du déclarant ni message : anonymat des informateurs
            SELECT s.SignalementID AS Id, d.Titre AS Dossier, st.Libelle AS Statut, s.DateSoumission AS Date
            FROM Signalements s
            INNER JOIN Dossiers d  ON s.DossierID = d.DossierID
            INNER JOIN Statuts  st ON s.StatutID  = st.StatutID
            WHERE s.StatutID = 3 AND d.EstPublie = 1
            ORDER BY s.DateSoumission DESC;

            SELECT d.DossierID AS Id, d.Titre, t.Libelle AS Type, r.Nom AS Region, d.Annee,
                   d.StatutDossier AS Statut, d.DateResolution, d.CommentaireResolution
            {Jointures}
            WHERE d.EstPublie = 1
            ORDER BY d.DatePublication DESC;");

        return new Historique(
            await multi.ReadSingleAsync<int>(),
            await multi.ReadSingleAsync<int>(),
            await multi.ReadSingleAsync<int>(),
            (await multi.ReadAsync<DossierHistorique>()).ToList(),
            (await multi.ReadAsync<SignalementPublic>()).ToList(),
            (await multi.ReadAsync<DossierHistorique>()).ToList());
    }

    // ========== ADMIN ==========

    public async Task<IReadOnlyList<DossierAdmin>> ListerTousAsync()
    {
        await using var cn = sql.Creer();
        return (await cn.QueryAsync<DossierAdmin>($@"
            SELECT {ColonnesResume}, d.EstPublie, d.StatutDossier AS Statut, d.RegionID AS RegionId
            {Jointures}
            ORDER BY d.DatePublication DESC, d.DossierID DESC")).ToList();
    }

    public async Task<DossierEdition?> ObtenirAsync(int id)
    {
        await using var cn = sql.Creer();
        return await cn.QuerySingleOrDefaultAsync<DossierEdition>(@"
            SELECT DossierID AS Id, Titre, TypeID AS TypeId, RegionID AS RegionId, Ville, Annee,
                   ISNULL(EstPublie, 0) AS EstPublie, ISNULL(StatutDossier, N'En cours') AS Statut,
                   CommentaireResolution, Description, SignesDistinctifs, ImageUrl
            FROM Dossiers WHERE DossierID = @Id", new { Id = id });
    }

    public async Task<int> CreerAsync(DossierEdition d)
    {
        await using var cn = sql.Creer();
        return await cn.ExecuteScalarAsync<int>(@"
            INSERT INTO Dossiers
                (Titre, Description, TypeID, RegionID, Annee, ImageUrl, SignesDistinctifs, Ville,
                 EstPublie, StatutDossier, CommentaireResolution, DateResolution)
            VALUES
                (@Titre, @Description, @TypeId, @RegionId, @Annee, @ImageUrl, @SignesDistinctifs, @Ville,
                 @EstPublie, @Statut, @CommentaireResolution,
                 CASE WHEN @Statut = N'Résolu' THEN GETDATE() ELSE NULL END);
            SELECT CAST(SCOPE_IDENTITY() AS int);", d);
    }

    public async Task ModifierAsync(DossierEdition d)
    {
        // La date de résolution est conservée si le dossier était déjà résolu
        await using var cn = sql.Creer();
        await cn.ExecuteAsync(@"
            UPDATE Dossiers SET
                Titre = @Titre, Description = @Description, TypeID = @TypeId, RegionID = @RegionId,
                Annee = @Annee, SignesDistinctifs = @SignesDistinctifs, Ville = @Ville,
                EstPublie = @EstPublie, StatutDossier = @Statut,
                CommentaireResolution = @CommentaireResolution,
                DateResolution = CASE WHEN @Statut = N'Résolu'
                                      THEN ISNULL(DateResolution, GETDATE()) ELSE NULL END,
                ImageUrl = ISNULL(@ImageUrl, ImageUrl)
            WHERE DossierID = @Id", d);
    }

    public async Task SupprimerAsync(int id)
    {
        // Données liées puis dossier, en tout ou rien
        await using var cn = sql.Creer();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync("DELETE FROM Geolocalisation WHERE DossierID = @Id", new { Id = id }, tx);
        await cn.ExecuteAsync("DELETE FROM Signalements    WHERE DossierID = @Id", new { Id = id }, tx);
        await cn.ExecuteAsync("DELETE FROM Dossiers        WHERE DossierID = @Id", new { Id = id }, tx);
        await tx.CommitAsync();
    }

    public async Task<(IReadOnlyList<Option> Types, IReadOnlyList<Option> Regions)> ListesAsync()
    {
        await using var cn = sql.Creer();
        using var multi = await cn.QueryMultipleAsync(@"
            SELECT TypeID AS Id, Libelle FROM TypesDossier ORDER BY TypeID;
            SELECT RegionID AS Id, Nom AS Libelle FROM Regions ORDER BY Nom;");
        return ((await multi.ReadAsync<Option>()).ToList(), (await multi.ReadAsync<Option>()).ToList());
    }

    public async Task<IReadOnlyList<PointCarte>> PointsCarteAsync()
    {
        await using var cn = sql.Creer();
        return (await cn.QueryAsync<PointCarte>(@"
            SELECT d.DossierID AS Id, d.Titre, t.Libelle AS Type, d.Ville,
                   g.Latitude, g.Longitude, d.DatePublication
            FROM Dossiers d
            INNER JOIN TypesDossier    t ON d.TypeID    = t.TypeID
            INNER JOIN Geolocalisation g ON d.DossierID = g.DossierID
            WHERE d.EstPublie = 1
            ORDER BY d.DatePublication DESC")).ToList();
    }
}

public class DossierAdmin : DossierResume
{
    public bool? EstPublie { get; set; }
    public string? Statut { get; set; }
    public int RegionId { get; set; }
}

public class DossierEdition
{
    public int Id { get; set; }
    public string Titre { get; set; } = "";
    public int TypeId { get; set; }
    public int RegionId { get; set; }
    public string? Ville { get; set; }
    public int? Annee { get; set; }
    public bool EstPublie { get; set; } = true;
    public string Statut { get; set; } = "En cours";
    public string? CommentaireResolution { get; set; }
    public string? Description { get; set; }
    public string? SignesDistinctifs { get; set; }
    public string? ImageUrl { get; set; }
}
