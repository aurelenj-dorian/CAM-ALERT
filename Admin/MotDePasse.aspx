<%@ Page Title="Changer mon mot de passe" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="MotDePasse.aspx.cs" Inherits="CamAlert.Admin.MotDePasse" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="login-wrap">
        <div class="login-card">

            <div style="width:60px;height:60px;background:#003f7d;border-radius:50%;
                        display:flex;align-items:center;justify-content:center;
                        margin:0 auto 20px;border:2px solid #e8600a;">
                <span style="color:white;font-weight:700;font-size:22px;">&#128274;</span>
            </div>

            <h2><asp:Literal ID="litTitre" runat="server" Text="Changer mon mot de passe" /></h2>
            <asp:Literal ID="litConsigne" runat="server" />

            <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="error-msg">
                <asp:Label ID="lblError" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlSucces" runat="server" Visible="false" CssClass="success-msg">
                <asp:Label ID="lblSucces" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlFormulaire" runat="server">

                <div class="form-group" style="text-align:left">
                    <label>Mot de passe actuel</label>
                    <asp:TextBox ID="txtAncien" runat="server" TextMode="Password"
                        placeholder="&bull;&bull;&bull;&bull;&bull;&bull;&bull;&bull;" />
                    <asp:RequiredFieldValidator runat="server"
                        ControlToValidate="txtAncien"
                        ErrorMessage="Champ obligatoire"
                        CssClass="validator-msg"
                        Display="Dynamic" />
                </div>

                <div class="form-group" style="text-align:left">
                    <label>Nouveau mot de passe</label>
                    <asp:TextBox ID="txtNouveau" runat="server" TextMode="Password"
                        placeholder="12 caractères minimum" />
                    <asp:RequiredFieldValidator runat="server"
                        ControlToValidate="txtNouveau"
                        ErrorMessage="Champ obligatoire"
                        CssClass="validator-msg"
                        Display="Dynamic" />
                    <p style="margin:6px 0 0;font-size:12px;color:#777;">
                        Au moins 12 caractères, mêlant trois types parmi
                        minuscules, majuscules, chiffres et symboles.
                        Évitez votre identifiant et les mots courants.
                    </p>
                </div>

                <div class="form-group" style="text-align:left">
                    <label>Confirmer le nouveau mot de passe</label>
                    <asp:TextBox ID="txtConfirmation" runat="server" TextMode="Password"
                        placeholder="Répétez le mot de passe" />
                    <asp:CompareValidator runat="server"
                        ControlToValidate="txtConfirmation"
                        ControlToCompare="txtNouveau"
                        ErrorMessage="Les mots de passe ne correspondent pas"
                        CssClass="validator-msg"
                        Display="Dynamic" />
                </div>

                <asp:Button ID="btnEnregistrer" runat="server"
                    Text="Enregistrer le nouveau mot de passe"
                    CssClass="btn btn-primary"
                    OnClick="btnEnregistrer_Click"
                    style="width:100%;padding:13px;margin-top:8px;font-size:15px;" />

            </asp:Panel>

            <p style="margin-top:20px;font-size:12px;color:#999;">
                <asp:HyperLink ID="lnkRetour" runat="server"
                    NavigateUrl="~/Admin/Dossiers.aspx" Text="Retour à l'administration" />
            </p>

        </div>
    </div>

</asp:Content>
