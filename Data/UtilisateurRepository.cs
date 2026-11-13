using Dapper;

namespace CamAlert.Web.Data;

public class UtilisateurRepository(ConnexionSql sql)
{
    public async Task<Utilisateur?> TrouverParLoginAsync(string login)
    {
        await using var cn = sql.Creer();
        return await cn.QuerySingleOrDefaultAsync<Utilisateur>(@"
            SELECT u.UtilisateurID AS Id, u.Login, u.MotDePasse, r.Libelle AS Role,
                   ISNULL(u.EstActif, 0) AS EstActif
            FROM Utilisateurs u
            INNER JOIN Roles r ON u.RoleID = r.RoleID
            WHERE u.Login = @Login", new { Login = login });
    }

    public async Task MettreAJourMotDePasseAsync(int id, string hash)
    {
        await using var cn = sql.Creer();
        await cn.ExecuteAsync("UPDATE Utilisateurs SET MotDePasse = @Hash WHERE UtilisateurID = @Id",
            new { Id = id, Hash = hash });
    }
}

public class Utilisateur
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string MotDePasse { get; set; } = "";
    public string Role { get; set; } = "";
    public bool EstActif { get; set; }
}
