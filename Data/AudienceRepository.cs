using Dapper;
using Microsoft.Data.SqlClient;

namespace CamAlert.Web.Data;

// Mesure d'audience (table PagesVues, voir Database/AjoutAudience.sql).
// Si la table n'existe pas, la mesure est simplement inactive.
public class AudienceRepository(ConnexionSql sql, ILogger<AudienceRepository> logger)
{
    private const int ObjetInexistant = 208;

    public async Task EnregistrerAsync(Guid visiteur, string chemin)
    {
        try
        {
            await using var cn = sql.Creer();
            await cn.ExecuteAsync(
                "INSERT INTO PagesVues (VisiteurID, Chemin) VALUES (@Visiteur, @Chemin)",
                new { Visiteur = visiteur, Chemin = chemin });
        }
        catch (SqlException ex) when (ex.Number == ObjetInexistant)
        {
            logger.LogWarning("Table PagesVues absente : mesure d'audience inactive.");
        }
    }

    public async Task<StatistiquesAudience> StatistiquesAsync(int jours)
    {
        try
        {
            await using var cn = sql.Creer();
            using var multi = await cn.QueryMultipleAsync(@"
                DECLARE @Depuis datetime2 = DATEADD(day, -@Jours, SYSUTCDATETIME());
                SELECT COUNT(*) FROM PagesVues WHERE DateVue >= @Depuis;
                SELECT COUNT(DISTINCT VisiteurID) FROM PagesVues WHERE DateVue >= @Depuis;
                SELECT CONVERT(char(10), DateVue, 120) AS Libelle, COUNT(*) AS Valeur
                    FROM PagesVues WHERE DateVue >= @Depuis
                    GROUP BY CONVERT(char(10), DateVue, 120) ORDER BY Libelle;
                SELECT TOP 10 Chemin AS Libelle, COUNT(*) AS Valeur
                    FROM PagesVues WHERE DateVue >= @Depuis
                    GROUP BY Chemin ORDER BY Valeur DESC;", new { Jours = jours });
            return new StatistiquesAudience(
                true,
                await multi.ReadSingleAsync<int>(),
                await multi.ReadSingleAsync<int>(),
                (await multi.ReadAsync<ValeurLibellee>()).ToList(),
                (await multi.ReadAsync<ValeurLibellee>()).ToList());
        }
        catch (SqlException ex) when (ex.Number == ObjetInexistant)
        {
            return new StatistiquesAudience(false, 0, 0, [], []);
        }
    }
}

public record StatistiquesAudience(
    bool Active,
    int PagesVues,
    int Visiteurs,
    IReadOnlyList<ValeurLibellee> ParJour,
    IReadOnlyList<ValeurLibellee> PagesPopulaires);
