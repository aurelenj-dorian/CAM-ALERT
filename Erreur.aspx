<%@ Page Language="C#" AutoEventWireup="true" %>
<%--
    Page d'erreur générique, cible de <customErrors defaultRedirect> dans
    Web.config. Elle remplace l'écran jaune ASP.NET, qui affichait au visiteur
    le chemin des fichiers sources, la requête SQL en cause et le nom du
    serveur.

    Page à code intégré, sans fichier de concepteur : elle doit pouvoir
    s'afficher même quand l'assembly de l'application est en difficulté.
--%>
<script runat="server">
    protected string Reference;

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.StatusCode = 500;
        Response.TrySkipIisCustomErrors = true;

        // Référence posée par Application_Error au moment de l'incident.
        var brute = Context.Items["ReferenceIncident"] as string;
        if (string.IsNullOrEmpty(brute))
            brute = Request.QueryString["ref"];

        // La référence est un identifiant hexadécimal : on ne reflète rien
        // d'autre, pour qu'un paramètre forgé ne puisse pas injecter de contenu.
        Reference = EstReferenceValide(brute) ? brute : null;
    }

    private static bool EstReferenceValide(string valeur)
    {
        if (string.IsNullOrEmpty(valeur) || valeur.Length != 8) return false;

        foreach (char c in valeur)
        {
            bool hex = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F');
            if (!hex) return false;
        }
        return true;
    }
</script>
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="robots" content="noindex" />
    <title>Erreur – CAM-ALERT</title>
    <link rel="stylesheet" type="text/css" href="/Content/css/style.css" />
    <style>
        .bloc-erreur {
            max-width: 560px; margin: 80px auto; padding: 48px;
            background: #fff; border-radius: 12px; text-align: center;
            box-shadow: 0 8px 28px rgba(0,0,0,.12);
        }
        .bloc-erreur h1 { color: #003f7d; font-size: 1.6rem; margin: 0 0 12px; }
        .bloc-erreur p { color: #666; line-height: 1.6; }
        .reference {
            display: inline-block; font-family: monospace; font-size: 1.1rem;
            background: #f4f6fb; border: 1px solid #d7deeb;
            border-radius: 6px; padding: 8px 16px; margin-top: 8px;
        }
    </style>
</head>
<body>
    <div class="bloc-erreur">
        <div style="font-size:48px;line-height:1;">&#9888;</div>
        <h1>Une erreur est survenue</h1>
        <p>
            L'opération n'a pas pu aboutir. L'incident a été enregistré et sera
            examiné par l'équipe technique.
        </p>
        <% if (Reference != null) { %>
            <p style="margin-bottom:4px;">Référence à communiquer au support :</p>
            <div class="reference"><%= Reference %></div>
        <% } %>
        <p style="margin-top:28px;">
            <a href="/Default.aspx" class="btn btn-primary">Retour à l'accueil</a>
        </p>
    </div>
</body>
</html>
