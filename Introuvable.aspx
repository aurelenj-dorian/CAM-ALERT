<%@ Page Language="C#" AutoEventWireup="true" %>
<%--
    Page 404, cible de <customErrors> dans Web.config.
    Page à code intégré, sans fichier de concepteur.
--%>
<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.StatusCode = 404;
        Response.TrySkipIisCustomErrors = true;
    }
</script>
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="robots" content="noindex" />
    <title>Page introuvable – CAM-ALERT</title>
    <link rel="stylesheet" type="text/css" href="/Content/css/style.css" />
    <style>
        .bloc-erreur {
            max-width: 560px; margin: 80px auto; padding: 48px;
            background: #fff; border-radius: 12px; text-align: center;
            box-shadow: 0 8px 28px rgba(0,0,0,.12);
        }
        .bloc-erreur h1 { color: #003f7d; font-size: 1.4rem; margin: 0 0 12px; }
        .code { font-size: 3rem; font-weight: 700; color: #e8600a; line-height: 1; }
        .bloc-erreur p { color: #666; line-height: 1.6; }
    </style>
</head>
<body>
    <div class="bloc-erreur">
        <div class="code">404</div>
        <h1>Page introuvable</h1>
        <p>L'adresse demandée n'existe pas ou n'est plus accessible.</p>
        <p style="margin-top:28px;">
            <a href="/Default.aspx" class="btn btn-primary">Retour à l'accueil</a>
            &nbsp;
            <a href="/Dossiers.aspx" class="btn btn-outline">Consulter les dossiers</a>
        </p>
    </div>
</body>
</html>
