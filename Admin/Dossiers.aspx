<%@ Page Title="Admin – Dossiers" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Dossiers.aspx.cs" Inherits="CamAlert.Admin.Dossiers" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    
    <a href="/Default.aspx" class="btn btn-outline" style="margin-right:10px;">
        &#8592; Voir le site
    </a>

    <div class="page-header">
        <h2>Gestion des dossiers</h2>
        <p>Ajouter, modifier ou supprimer des dossiers du portail CAM-ALERT.</p>
    </div>

    <!-- BOUTONS ACTIONS -->
    <div class="action-bar">
        <asp:Button ID="btnNouveauDossier" runat="server" Text="+ Nouveau dossier"
            CssClass="btn btn-primary" OnClick="btnNouveauDossier_Click" />
        <a href="Signalements.aspx" class="btn btn-outline">📋 Signalements</a>
        <asp:Button ID="btnDeconnexion" runat="server" Text="Se déconnecter"
            CssClass="btn btn-outline" OnClick="btnDeconnexion_Click" />
    </div>

    <!-- MESSAGE CONFIRMATION -->
    <asp:Panel ID="pnlMessage" runat="server" Visible="false">
        <div class="success-msg">
            <asp:Literal ID="litMessage" runat="server" />
        </div>
    </asp:Panel>

    <!-- FORMULAIRE AJOUT/MODIFICATION -->
    <asp:Panel ID="pnlFormulaire" runat="server" Visible="false">
        <div class="form-card">
            <h2><asp:Literal ID="litTitreForm" runat="server" Text="Nouveau dossier" /></h2>
            <asp:HiddenField ID="hdnDossierID" runat="server" Value="0" />

            <div class="form-row">
                <div class="form-group">
                    <label>Titre / Nom <span class="required">*</span></label>
                    <asp:TextBox ID="txtTitre" runat="server" placeholder="Nom complet" />
                    <asp:RequiredFieldValidator runat="server"
                        ControlToValidate="txtTitre"
                        ErrorMessage="Champ obligatoire"
                        CssClass="validator-msg" Display="Dynamic" />
                </div>
                <div class="form-group">
                    <label>Type <span class="required">*</span></label>
                    <asp:DropDownList ID="ddlType" runat="server" />
                </div>
            </div>

            <div class="form-row">
                <div class="form-group">
                    <label>Région <span class="required">*</span></label>
                    <asp:DropDownList ID="ddlRegion" runat="server" />
                </div>
                <div class="form-group">
                    <label>Ville</label>
                    <asp:TextBox ID="txtVille" runat="server" placeholder="Ville" />
                </div>
            </div>

            <div class="form-row">
                <div class="form-group">
                    <label>Année</label>
                    <asp:TextBox ID="txtAnnee" runat="server" placeholder="2024" />
                </div>
                <div class="form-group">
                    <label>Publié</label>
                    <asp:DropDownList ID="ddlPublie" runat="server">
                        <asp:ListItem Value="1">Oui</asp:ListItem>
                        <asp:ListItem Value="0">Non</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>

            <div class="form-row">
                <div class="form-group">
                    <label>Statut du dossier</label>
                    <asp:DropDownList ID="ddlStatutDossier" runat="server">
                        <asp:ListItem Value="En cours">En cours</asp:ListItem>
                        <asp:ListItem Value="Résolu">Résolu — Personne retrouvée</asp:ListItem>
                        <asp:ListItem Value="Classé">Classé sans suite</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <label>Commentaire résolution</label>
                    <asp:TextBox ID="txtCommentaireResolution" runat="server"
                        placeholder="Ex: Personne retrouvée saine et sauve le..." />
                </div>
            </div>

            <div class="form-group">
                <label>Description</label>
                <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine"
                    Rows="4" placeholder="Description du dossier..." />
            </div>

            <div class="form-group">
                <label>Signes distinctifs</label>
                <asp:TextBox ID="txtSignes" runat="server" TextMode="MultiLine"
                    Rows="3" placeholder="Signes distinctifs..." />
            </div>

            <div class="form-group">
                <label>Image (fichier)</label>
                <asp:FileUpload ID="fuImage" runat="server" />
            </div>

            <div style="display: flex; gap: 10px; flex-wrap: wrap; margin-top: 20px;">
                <asp:Button ID="btnSauvegarder" runat="server" Text="Sauvegarder"
                    CssClass="btn btn-primary" OnClick="btnSauvegarder_Click" />
                <asp:Button ID="btnAnnuler" runat="server" Text="Annuler"
                    CssClass="btn btn-outline" OnClick="btnAnnuler_Click"
                    CausesValidation="false" />
            </div>
        </div>
    </asp:Panel>

    <!-- LISTE DES DOSSIERS -->
    <div class="table-responsive">
        <asp:GridView ID="gvDossiers" runat="server"
            AutoGenerateColumns="false"
            CssClass="dossiers-table"
            DataKeyNames="DossierID"
            OnRowCommand="gvDossiers_RowCommand"
            EmptyDataText="Aucun dossier."
            GridLines="None">
            <Columns>
                <asp:BoundField DataField="Titre" HeaderText="Titre" />
                <asp:BoundField DataField="TypeLibelle" HeaderText="Type" />
                <asp:BoundField DataField="RegionNom" HeaderText="Région" />
                <asp:BoundField DataField="Annee" HeaderText="Année" />
                <asp:TemplateField HeaderText="Publié">
                    <ItemTemplate>
                        <span style='color:<%# (bool)Eval("EstPublie") ? "green" : "red" %>'>
                            <%# (bool)Eval("EstPublie") ? "Oui" : "Non" %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Actions">
                    <ItemTemplate>
                        <div style="display: flex; gap: 5px; flex-wrap: wrap;">
                            <asp:LinkButton CommandName="Modifier" CommandArgument='<%# Eval("DossierID") %>'
                                runat="server" CssClass="btn btn-outline-small">
                                Modifier
                            </asp:LinkButton>
                            <asp:LinkButton CommandName="Supprimer" CommandArgument='<%# Eval("DossierID") %>'
                                runat="server" CssClass="btn btn-outline-small btn-danger"
                                OnClientClick="return confirm('Supprimer ce dossier ?');">
                                Supprimer
                            </asp:LinkButton>
                        </div>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

    <!-- STYLES RESPONSIVES INTÉGRÉS -->
    <style>
        /* ========== PAGE ADMIN - RESPONSIVE ========== */
        
        /* Reset et box-sizing */
        * {
            box-sizing: border-box;
        }
        
        /* Formulaire en colonnes flexibles */
        .form-row {
            display: flex;
            gap: 20px;
            margin-bottom: 15px;
            flex-wrap: wrap;
        }
        
        .form-group {
            flex: 1;
            min-width: 200px;
            margin-bottom: 15px;
        }
        
        .form-group label {
            display: block;
            margin-bottom: 5px;
            font-weight: 600;
            color: #1a2235;
        }
        
        .form-group .required {
            color: #e8600a;
        }
        
        .form-group input[type="text"],
        .form-group textarea,
        .form-group select,
        .form-group input[type="file"] {
            width: 100%;
            padding: 10px 12px;
            border: 1px solid #d0d7e3;
            border-radius: 6px;
            font-size: 14px;
            transition: all 0.2s;
            font-family: inherit;
        }
        
        .form-group input:focus,
        .form-group textarea:focus,
        .form-group select:focus {
            border-color: #003f7d;
            outline: none;
            box-shadow: 0 0 0 2px rgba(0,63,125,0.1);
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
        
        .btn-danger {
            border-color: #dc3545;
            color: #dc3545;
        }
        
        .btn-danger:hover {
            background: #dc3545;
            color: white;
            border-color: #dc3545;
        }
        
        /* Barre d'actions responsive */
        .action-bar {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            margin-bottom: 20px;
            align-items: center;
        }
        
        .action-bar .btn {
            margin: 0;
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
        }
        
        /* Validator messages */
        .validator-msg {
            color: #e8600a;
            font-size: 12px;
            display: block;
            margin-top: 5px;
        }
        
        /* ========== RESPONSIVE ========== */
        
        /* Tablettes (max-width: 768px) */
        @media (max-width: 768px) {
            .form-row {
                flex-direction: column;
                gap: 0;
            }
            
            .form-group {
                min-width: 100%;
            }
            
            .form-card {
                padding: 18px;
            }
            
            .action-bar {
                flex-direction: column;
                align-items: stretch;
            }
            
            .action-bar .btn {
                text-align: center;
                justify-content: center;
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
            
            .form-card h2 {
                font-size: 18px;
            }
        }
        
        /* Grands écrans */
        @media (min-width: 1200px) {
            .form-card {
                padding: 30px;
            }
            
            .form-group input,
            .form-group textarea,
            .form-group select {
                padding: 12px 14px;
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
            
            .action-bar {
                gap: 8px;
            }
            
            .btn-outline-small {
                padding: 4px 10px;
                font-size: 11px;
            }
        }
    </style>

</asp:Content>