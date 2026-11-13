using Microsoft.Data.SqlClient;

namespace CamAlert.Web.Data;

public class ConnexionSql(IConfiguration configuration)
{
    private readonly string chaine = configuration.GetConnectionString("CamAlertDB")
        ?? throw new InvalidOperationException("Chaîne de connexion 'CamAlertDB' manquante.");

    public SqlConnection Creer() => new(chaine);

    // Échappe les caractères spéciaux de LIKE (%, _, [) dans une saisie utilisateur
    public static string Contient(string texte) =>
        "%" + texte.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
}
