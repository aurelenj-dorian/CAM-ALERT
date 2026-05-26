<%@ Page Title="Admin – Signalements" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Signalements.aspx.cs" Inherits="CamAlert.Admin.Signalements" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <!-- BOUTONS ACTIONS -->
    <div class="action-bar">
        <a href="Dossiers.aspx" class="btn btn-outline">
            &#8592; Retour gestion dossiers
        </a>
        <a href="/Default.aspx" class="btn btn-outline">
            Voir le site
        </a>
    </div>

    <div class="page-header">
        <h2>Gestion des signalements</h2>
        <p>Consultez et traitez les informations soumises par les citoyens.</p>
    </div>

    <!-- MESSAGE -->
    <asp:Panel ID="pnlMessage" runat="server" Visible="false">
        <div class="success-msg">
            <asp:Literal ID="litMessage" runat="server" />
        </div>
    </asp:Panel>

    <!-- DETAIL SIGNALEMENT -->
    <asp:Panel ID="pnlDetail" runat="server" Visible="false">
        <div class="form-card">
            <h2>📋 Détail du signalement</h2>
            
            <div class="detail-meta">
                <div class="meta-item">
                    <strong>Dossier :</strong> <asp:Literal ID="litDossier" runat="server" />
                </div>
                <div class="meta-item">
                    <strong>Déclarant :</strong> <asp:Literal ID="litDeclarant" runat="server" />
                </div>
                <div class="meta-item">
                    <strong>Email :</strong> <asp:Literal ID="litEmail" runat="server" />
                </div>
                <div class="meta-item">
                    <strong>Date :</strong> <asp:Literal ID="litDate" runat="server" />
                </div>
            </div>

            <div class="detail-section">
                <h3>💬 Message</h3>
                <div class="message-box">
                    <asp:Literal ID="litMessageDetail" runat="server" />
                </div>
            </div>

            <div class="detail-section">
                <h3>🔄 Changer le statut</h3>
                <div class="status-group">
                    <asp:HiddenField ID="hdnSignalementID" runat="server" />
                    <asp:DropDownList ID="ddlStatut" runat="server" CssClass="status-select">
                        <asp:ListItem Value="1">Non traité</asp:ListItem>
                        <asp:ListItem Value="2">En cours de traitement</asp:ListItem>
                        <asp:ListItem Value="3">Traité</asp:ListItem>
                    </asp:DropDownList>
                    <asp:Button ID="btnChangerStatut" runat="server"
                        Text="Mettre à jour le statut"
                        CssClass="btn btn-primary"
                        OnClick="btnChangerStatut_Click" />
                </div>
            </div>

            <div class="detail-section">
                <h3>📝 Commentaire interne</h3>
                <asp:TextBox ID="txtCommentaire" runat="server"
                    TextMode="MultiLine"
                    Rows="4"
                    CssClass="commentaire-textarea"
                    placeholder="Commentaire visible uniquement par les agents..." />
                <asp:Button ID="btnSauvegarderCommentaire" runat="server"
                    Text="Sauvegarder le commentaire"
                    CssClass="btn btn-outline"
                    OnClick="btnSauvegarderCommentaire_Click"
                    style="margin-top:10px;" />
            </div>

            <div class="form-actions">
                <asp:Button ID="btnFermerDetail" runat="server"
                    Text="Fermer le détail"
                    CssClass="btn btn-outline"
                    OnClick="btnFermerDetail_Click"
                    CausesValidation="false" />
            </div>
        </div>
    </asp:Panel>

    <!-- LISTE SIGNALEMENTS -->
    <div class="table-responsive">
        <asp:GridView ID="gvSignalements" runat="server"
            AutoGenerateColumns="false"
            CssClass="dossiers-table"
            DataKeyNames="SignalementID"
            OnRowCommand="gvSignalements_RowCommand"
            EmptyDataText="Aucun signalement."
            GridLines="None">
            <Columns>
                <asp:BoundField DataField="DossierTitre" HeaderText="Dossier" />
                <asp:BoundField DataField="NomDeclarant" HeaderText="Déclarant" />
                <asp:TemplateField HeaderText="Message">
                    <ItemTemplate>
                        <%# Eval("Message").ToString().Length > 50 ?
                            Eval("Message").ToString().Substring(0, 50) + "..." :
                            Eval("Message") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Statut">
                    <ItemTemplate>
                        <span class='status-badge status-<%# ((string)Eval("StatutLibelle")).Replace(" ", "") %>'>
                            <%# Eval("StatutLibelle") %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Date">
                    <ItemTemplate>
                        <%# Convert.ToDateTime(Eval("DateSoumission")).ToString("dd/MM/yyyy") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Action">
                    <ItemTemplate>
                        <asp:LinkButton CommandName="Voir"
                            CommandArgument='<%# Eval("SignalementID") %>'
                            runat="server" CssClass="btn btn-outline-small">
                            Voir détail
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

    <!-- STYLES RESPONSIVES INTÉGRÉS -->
    <style>
        /* ========== PAGE ADMIN SIGNALEMENTS - RESPONSIVE ========== */
        
        * {
            box-sizing: border-box;
        }
        
        /* Barre d'actions responsive */
        .action-bar {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            margin-bottom: 20px;
            align-items: center;
        }
        
        /* Boutons */
        .btn {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 10px 20px;
            border-radius: 6px;
            font-size: 14px;
            font-weight: 500;
            cursor: pointer;
            transition: all 0.2s;
            text-decoration: none;
            border: none;
        }
        
        .btn-primary {
            background: #003f7d;
            color: white;
        }
        
        .btn-primary:hover {
            background: #00509e;
        }
        
        .btn-outline {
            background: transparent;
            border: 1px solid #003f7d;
            color: #003f7d;
        }
        
        .btn-outline:hover {
            background: #003f7d;
            color: white;
        }
        
        .btn-outline-small {
            background: transparent;
            border: 1px solid #003f7d;
            color: #003f7d;
            padding: 5px 12px;
            font-size: 12px;
            border-radius: 4px;
            text-decoration: none;
            display: inline-block;
            cursor: pointer;
        }
        
        .btn-outline-small:hover {
            background: #003f7d;
            color: white;
        }
        
        /* Message de succès */
        .success-msg {
            background: #d4edda;
            color: #155724;
            padding: 12px 18px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        
        /* Formulaire card */
        .form-card {
            background: white;
            border-radius: 12px;
            padding: 25px;
            box-shadow: 0 2px 12px rgba(0,0,0,0.08);
            margin-bottom: 28px;
        }
        
        .form-card h2 {
            margin-bottom: 20px;
            font-size: 20px;
            color: #003f7d;
            margin-top: 0;
        }
        
        /* Détails métadonnées */
        .detail-meta {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            background: #f4f6fb;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        
        .meta-item {
            font-size: 14px;
        }
        
        .meta-item strong {
            color: #1a2235;
        }
        
        .meta-item span {
            color: #555;
        }
        
        /* Section détail */
        .detail-section {
            margin-bottom: 25px;
        }
        
        .detail-section h3 {
            font-size: 16px;
            color: #003f7d;
            margin-bottom: 10px;
        }
        
        .message-box {
            background: #f8fafd;
            padding: 15px;
            border-radius: 8px;
            border-left: 3px solid #003f7d;
            line-height: 1.6;
        }
        
        /* Groupe statut */
        .status-group {
            display: flex;
            gap: 12px;
            align-items: center;
            flex-wrap: wrap;
        }
        
        .status-select {
            padding: 9px 12px;
            border: 1px solid #d0d7e3;
            border-radius: 6px;
            background: #f4f6f9;
            font-size: 14px;
            min-width: 200px;
        }
        
        .commentaire-textarea {
            width: 100%;
            padding: 11px 14px;
            border: 1px solid #d0d7e3;
            border-radius: 6px;
            background: #f4f6f9;
            font-size: 14px;
            min-height: 100px;
            font-family: inherit;
        }
        
        .form-actions {
            margin-top: 20px;
            display: flex;
            gap: 10px;
            flex-wrap: wrap;
        }
        
        /* Badges statut */
        .status-badge {
            display: inline-block;
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
        }
        
        .status-Traité {
            background: #d4edda;
            color: #155724;
        }
        
        .status-Encoursdetraitement {
            background: #fff3e0;
            color: #e8600a;
        }
        
        .status-Nontraité {
            background: #f8d7da;
            color: #c62828;
        }
        
        /* Table responsive */
        .dossiers-table {
            width: 100%;
            border-collapse: collapse;
            background: white;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 2px 8px rgba(0,0,0,0.08);
        }
        
        .dossiers-table th,
        .dossiers-table td {
            padding: 12px 15px;
            text-align: left;
            border-bottom: 1px solid #eef2f6;
        }
        
        .dossiers-table th {
            background: #f4f6fb;
            font-weight: 600;
            color: #1a2235;
        }
        
        .dossiers-table tr:hover {
            background: #f8fafd;
        }
        
        .table-responsive {
            overflow-x: auto;
        }
        
        /* ========== RESPONSIVE ========== */
        
        /* Tablettes (max-width: 768px) */
        @media (max-width: 768px) {
            .action-bar {
                flex-direction: column;
                align-items: stretch;
            }
            
            .action-bar .btn {
                text-align: center;
                justify-content: center;
                width: 100%;
            }
            
            .form-card {
                padding: 18px;
            }
            
            .form-card h2 {
                font-size: 18px;
            }
            
            .detail-meta {
                grid-template-columns: 1fr;
                gap: 10px;
            }
            
            .status-group {
                flex-direction: column;
                align-items: stretch;
            }
            
            .status-select {
                width: 100%;
            }
            
            .dossiers-table th,
            .dossiers-table td {
                padding: 10px 12px;
                font-size: 13px;
            }
            
            .btn {
                padding: 8px 16px;
                font-size: 13px;
            }
        }
        
        /* Petits mobiles (max-width: 480px) */
        @media (max-width: 480px) {
            .form-card {
                padding: 15px;
            }
            
            .dossiers-table th,
            .dossiers-table td {
                padding: 8px 10px;
                font-size: 12px;
            }
            
            .btn {
                padding: 8px 14px;
                font-size: 12px;
                width: 100%;
                justify-content: center;
            }
            
            .btn-outline-small {
                padding: 4px 10px;
                font-size: 11px;
            }
            
            .detail-section h3 {
                font-size: 14px;
            }
            
            .message-box {
                padding: 12px;
                font-size: 13px;
            }
        }
    </style>

</asp:Content>