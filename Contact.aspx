<%@ Page Title="Contactez-nous" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="CamAlert.Contact" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-header">
        <h1>📞 Contacter CAM-ALERT</h1>
        <p>Vous avez une information ou une question ? Utilisez ce formulaire. Nous vous répondrons dans les meilleurs délais.</p>
    </div>

    <div class="card">
        <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert alert-success">
            ✅ Votre message a bien été envoyé. Nous vous répondrons sous 48h.
        </asp:Panel>
        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-error">
            ❌ Une erreur est survenue. Veuillez réessayer plus tard.
        </asp:Panel>

        <div class="form-group">
            <label for="txtNom">Nom complet *</label>
            <asp:TextBox ID="txtNom" runat="server" CssClass="form-control" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNom" ErrorMessage="Champ requis" CssClass="validator-msg" Display="Dynamic" />
        </div>

        <div class="form-group">
            <label for="txtEmail">Email *</label>
            <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" TextMode="Email" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" ErrorMessage="Champ requis" CssClass="validator-msg" Display="Dynamic" />
            <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail" ValidationExpression="^[\w\.-]+@[\w\.-]+\.\w+$" ErrorMessage="Email invalide" CssClass="validator-msg" Display="Dynamic" />
        </div>

        <div class="form-group">
            <label for="ddlSujet">Sujet *</label>
            <asp:DropDownList ID="ddlSujet" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Choisissez un sujet --</asp:ListItem>
                <asp:ListItem Value="Signalement infraction">⚠️ Signalement d'une infraction</asp:ListItem>
                <asp:ListItem Value="Information personne recherchée">🔍 Information sur une personne recherchée</asp:ListItem>
                <asp:ListItem Value="Disparition">⚠️ Personne disparue</asp:ListItem>
                <asp:ListItem Value="Presse">📰 Presse / Médias</asp:ListItem>
                <asp:ListItem Value="Recrutement">💼 Recrutement / Stage</asp:ListItem>
                <asp:ListItem Value="Commission de contrôle">⚖️ Commission de contrôle des fichiers</asp:ListItem>
                <asp:ListItem Value="Autre">📝 Autre demande</asp:ListItem>
            </asp:DropDownList>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlSujet" InitialValue="" ErrorMessage="Choisissez un sujet" CssClass="validator-msg" Display="Dynamic" />
        </div>

        <div class="form-group">
            <label for="txtMessage">Message *</label>
            <asp:TextBox ID="txtMessage" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="8" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMessage" ErrorMessage="Champ requis" CssClass="validator-msg" Display="Dynamic" />
        </div>

        <div class="form-group">
            <asp:Button ID="btnEnvoyer" runat="server" Text="Envoyer le message" CssClass="btn btn-primary" OnClick="btnEnvoyer_Click" />
        </div>
    </div>

    <div class="legal-banner" style="margin-top: 20px;">
        <strong>⚠️ Avertissement :</strong> Ce formulaire est fictif et réalisé à des fins pédagogiques. Les données sont envoyées par email à l'administrateur du portail.
    </div>

    <style>
        .alert-success {
            background: #d4edda;
            color: #155724;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        .alert-error {
            background: #f8d7da;
            color: #721c24;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        .form-group {
            margin-bottom: 20px;
        }
        .form-control {
            width: 100%;
            padding: 10px;
            border: 1px solid #ccc;
            border-radius: 6px;
        }
        .validator-msg {
            color: #e8600a;
            font-size: 12px;
            display: block;
            margin-top: 4px;
        }
    </style>
</asp:Content>