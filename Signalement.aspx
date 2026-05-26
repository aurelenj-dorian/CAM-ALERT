<%@ Page Title="Signalement" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Signalement.aspx.cs" Inherits="CamAlert.Signalement" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div style="margin-bottom:16px;">
    <a href="javascript:history.back()" class="btn btn-outline"
       style="padding:7px 16px;font-size:13px;">
        &#8592; Retour
    </a>
</div>

    <div class="form-card">

        <h2>Soumettre une information</h2>
        <p class="form-subtitle">
            Toute information utile est traitée de façon strictement confidentielle 
            par nos agents habilités. Votre identité peut rester anonyme.
        </p>

        <!-- Message succès -->
        <asp:Panel ID="pnlSuccess" runat="server" Visible="false">
            <div class="success-msg">
                ✓ Votre signalement a bien été enregistré. 
                Merci pour votre collaboration citoyenne.
            </div>
        </asp:Panel>

        <!-- Formulaire -->
        <asp:Panel ID="pnlForm" runat="server">

            <div class="form-row">
                <div class="form-group">
                    <label>Votre nom 
                        <em style="color:#999;font-weight:400">(facultatif)</em>
                    </label>
                    <asp:TextBox ID="txtNom" runat="server" 
                        placeholder="Ex : Jean Dupont" />
                </div>
                <div class="form-group">
                    <label>Votre email 
                        <em style="color:#999;font-weight:400">(facultatif)</em>
                    </label>
                    <asp:TextBox ID="txtEmail" runat="server" 
                        placeholder="exemple@mail.com" />
                    <asp:RegularExpressionValidator runat="server"
                        ControlToValidate="txtEmail"
                        ValidationExpression="^[\w\.-]+@[\w\.-]+\.\w{2,}$"
                        ErrorMessage="Adresse email invalide"
                        CssClass="validator-msg"
                        Display="Dynamic" />
                </div>
            </div>

            <div class="form-group">
                <label>Dossier concerné <span class="required">*</span></label>
                <asp:DropDownList ID="ddlDossier" runat="server">
                    <asp:ListItem Value="">-- Sélectionnez un dossier --</asp:ListItem>
                </asp:DropDownList>
                <asp:RequiredFieldValidator runat="server"
                    ControlToValidate="ddlDossier"
                    InitialValue=""
                    ErrorMessage="Veuillez sélectionner un dossier"
                    CssClass="validator-msg"
                    Display="Dynamic" />
            </div>

            <div class="form-group">
                <label>Votre message <span class="required">*</span></label>
                <asp:TextBox ID="txtMessage" runat="server"
                    TextMode="MultiLine"
                    placeholder="Décrivez l'information que vous souhaitez transmettre aux autorités..." />
                <asp:RequiredFieldValidator runat="server"
                    ControlToValidate="txtMessage"
                    ErrorMessage="Le message est obligatoire"
                    CssClass="validator-msg"
                    Display="Dynamic" />
            </div>

            <div class="form-group">
                <label>Fichier joint 
                    <em style="color:#999;font-weight:400">(image ou PDF, max 5 Mo)</em>
                </label>
                <asp:FileUpload ID="fuFichier" runat="server"
                    style="width:100%;padding:10px;border:1px solid #d0d7e3;
                           border-radius:6px;background:#f4f6f9;
                           font-size:14px;font-family:inherit;" />
            </div>

            <asp:Button ID="btnEnvoyer" runat="server"
                Text="Envoyer mon signalement"
                CssClass="btn btn-primary"
                OnClick="btnEnvoyer_Click"
                style="width:100%;padding:14px;font-size:15px;" />

        </asp:Panel>
    </div>

</asp:Content>