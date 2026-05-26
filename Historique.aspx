<%@ Page Title="Historique" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Historique.aspx.cs" Inherits="CamAlert.Historique" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div style="margin-bottom:16px;">
        <a href="Default.aspx" class="btn btn-outline"
           style="padding:7px 16px;font-size:13px;">
            &#8592; Retour à l'accueil
        </a>
    </div>

    <div class="page-header">
        <h2>Historique & Dossiers résolus</h2>
        <p>Personnes retrouvées, dossiers classés et signalements traités par nos agents.</p>
    </div>

    <!-- ONGLETS -->
    <div style="display:flex;gap:8px;margin-bottom:24px;">
        <asp:Button ID="btnOngletResolus" runat="server" Text="Personnes retrouvées"
            CssClass="btn btn-primary" OnClick="btnOngletResolus_Click" />
        <asp:Button ID="btnOngletSignalements" runat="server" Text="Signalements traités"
            CssClass="btn btn-outline" OnClick="btnOngletSignalements_Click" />
        <asp:Button ID="btnOngletTous" runat="server" Text="Tous les dossiers"
            CssClass="btn btn-outline" OnClick="btnOngletTous_Click" />
    </div>

    <!-- STATS -->
    <div class="cards-grid" style="margin-bottom:32px;">
        <div class="card" style="text-align:center;">
            <div style="font-size:40px;font-weight:700;color:#003f7d;">
                <asp:Literal ID="litNbResolus" runat="server" Text="0" />
            </div>
            <p style="color:#555e6d;margin-top:8px;">Dossiers résolus</p>
        </div>
        <div class="card" style="text-align:center;">
            <div style="font-size:40px;font-weight:700;color:#e8600a;">
                <asp:Literal ID="litNbEnCours" runat="server" Text="0" />
            </div>
            <p style="color:#555e6d;margin-top:8px;">Dossiers en cours</p>
        </div>
        <div class="card" style="text-align:center;">
            <div style="font-size:40px;font-weight:700;color:#2e7d32;">
                <asp:Literal ID="litNbSignalements" runat="server" Text="0" />
            </div>
            <p style="color:#555e6d;margin-top:8px;">Signalements traités</p>
        </div>
    </div>

    <!-- LISTE DOSSIERS RESOLUS -->
    <asp:Panel ID="pnlResolus" runat="server">
        <h3 style="color:#003f7d;margin-bottom:16px;font-size:18px;">
            Personnes retrouvées / Dossiers résolus
        </h3>
        <asp:GridView ID="gvResolus" runat="server"
            AutoGenerateColumns="false"
            CssClass="dossiers-table"
            EmptyDataText="Aucun dossier résolu pour l'instant.">
            <Columns>
                <asp:BoundField DataField="Titre" HeaderText="Identité" />
                <asp:BoundField DataField="TypeLibelle" HeaderText="Type" />
                <asp:BoundField DataField="RegionNom" HeaderText="Région" />
                <asp:BoundField DataField="Annee" HeaderText="Année" />
                <asp:TemplateField HeaderText="Statut">
                    <ItemTemplate>
                        <span style="color:green;font-weight:600;">
                            ✓ <%# Eval("StatutDossier") %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Résolu le">
                    <ItemTemplate>
                        <%# Eval("DateResolution") != DBNull.Value ?
                            Convert.ToDateTime(Eval("DateResolution")).ToString("dd/MM/yyyy")
                            : "–" %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="CommentaireResolution" HeaderText="Commentaire" />
            </Columns>
        </asp:GridView>
    </asp:Panel>

    <!-- LISTE SIGNALEMENTS TRAITES -->
    <asp:Panel ID="pnlSignalements" runat="server" Visible="false">
        <h3 style="color:#003f7d;margin-bottom:16px;font-size:18px;">
            Signalements traités
        </h3>
        <asp:GridView ID="gvSignalements" runat="server"
            AutoGenerateColumns="false"
            CssClass="dossiers-table"
            EmptyDataText="Aucun signalement traité pour l'instant.">
            <Columns>
                <asp:BoundField DataField="DossierTitre" HeaderText="Dossier concerné" />
                <asp:BoundField DataField="NomDeclarant" HeaderText="Déclarant" />
                <asp:TemplateField HeaderText="Message">
                    <ItemTemplate>
                        <%# Eval("Message").ToString().Length > 60 ?
                            Eval("Message").ToString().Substring(0, 60) + "..." :
                            Eval("Message") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="StatutLibelle" HeaderText="Statut" />
                <asp:TemplateField HeaderText="Date">
                    <ItemTemplate>
                        <%# Convert.ToDateTime(Eval("DateSoumission")).ToString("dd/MM/yyyy") %>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>

    <!-- LISTE TOUS DOSSIERS -->
    <asp:Panel ID="pnlTous" runat="server" Visible="false">
        <h3 style="color:#003f7d;margin-bottom:16px;font-size:18px;">
            Tous les dossiers
        </h3>
        <asp:GridView ID="gvTous" runat="server"
            AutoGenerateColumns="false"
            CssClass="dossiers-table"
            EmptyDataText="Aucun dossier.">
            <Columns>
                <asp:BoundField DataField="Titre" HeaderText="Identité" />
                <asp:BoundField DataField="TypeLibelle" HeaderText="Type" />
                <asp:BoundField DataField="RegionNom" HeaderText="Région" />
                <asp:BoundField DataField="Annee" HeaderText="Année" />
                <asp:TemplateField HeaderText="Statut">
                    <ItemTemplate>
                        <span style='color:<%# (string)Eval("StatutDossier") == "Résolu" ? "green" : (string)Eval("StatutDossier") == "En cours" ? "#e8600a" : "#003f7d" %>; font-weight:600;'>
                            <%# Eval("StatutDossier") %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Action">
                    <ItemTemplate>
                        <a href='DossierDetail.aspx?id=<%# Eval("DossierID") %>'
                           class="btn btn-outline"
                           style="padding:4px 10px;font-size:12px;">Voir</a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>

</asp:Content>