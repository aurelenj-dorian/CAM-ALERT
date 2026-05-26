<%@ Page Title="Administration" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="CamAlert.Admin.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="login-wrap">
        <div class="login-card">

            <div style="width:60px;height:60px;background:#003f7d;border-radius:50%;
                        display:flex;align-items:center;justify-content:center;
                        margin:0 auto 20px;border:2px solid #e8600a;">
                <span style="color:white;font-weight:700;font-size:18px;">CA</span>
            </div>

            <h2>Espace Administration</h2>
            <p>Accès réservé aux agents habilités du portail CAM-ALERT.</p>

            <!-- Panel d'erreur avec Label dynamique -->
            <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="error-msg">
                <asp:Label ID="lblError" runat="server" />
            </asp:Panel>

            <div class="form-group" style="text-align:left">
                <label>Identifiant</label>
                <asp:TextBox ID="txtLogin" runat="server" 
                    placeholder="Votre identifiant" />
                <asp:RequiredFieldValidator runat="server"
                    ControlToValidate="txtLogin"
                    ErrorMessage="Champ obligatoire"
                    CssClass="validator-msg"
                    Display="Dynamic" />
            </div>

            <div class="form-group" style="text-align:left">
                <label>Mot de passe</label>
                <asp:TextBox ID="txtPassword" runat="server"
                    TextMode="Password"
                    placeholder="••••••••" />
                <asp:RequiredFieldValidator runat="server"
                    ControlToValidate="txtPassword"
                    ErrorMessage="Champ obligatoire"
                    CssClass="validator-msg"
                    Display="Dynamic" />
            </div>

            <asp:Button ID="btnLogin" runat="server"
                Text="Se connecter"
                CssClass="btn btn-primary"
                OnClick="btnLogin_Click"
                style="width:100%;padding:13px;margin-top:8px;font-size:15px;" />

            <p style="margin-top:20px;font-size:12px;color:#999;">
                Portail institutionnel fictif – usage pédagogique uniquement
            </p>

        </div>
    </div>

</asp:Content>