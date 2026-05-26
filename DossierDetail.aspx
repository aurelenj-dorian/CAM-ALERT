<%@ Page Title="Fiche dossier" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="DossierDetail.aspx.cs" Inherits="CamAlert.DossierDetail" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="detail-layout">

        <!-- PHOTO -->
        <div class="detail-photo-container">
            <asp:Image ID="imgDossier" runat="server"
                CssClass="detail-photo"
                AlternateText="Photo du dossier"
                ImageUrl="~/Content/images/default.png" />
        </div>

        <!-- INFOS -->
        <div class="detail-card">

            <div class="badge-container">
                <asp:Literal ID="litBadge" runat="server" />
            </div>

            <h1><asp:Literal ID="litTitre" runat="server" Text="Chargement..." /></h1>

            <p class="ref-dossier">
                Référence dossier : <asp:Literal ID="litRef" runat="server" />
            </p>

            <div class="detail-meta">
                <div class="meta-item">
                    <span class="meta-label">Année :</span>
                    <span class="meta-value"><asp:Literal ID="litAnnee" runat="server" /></span>
                </div>
                <div class="meta-item">
                    <span class="meta-label">Région :</span>
                    <span class="meta-value"><asp:Literal ID="litRegion" runat="server" /></span>
                </div>
                <div class="meta-item">
                    <span class="meta-label">Ville :</span>
                    <span class="meta-value"><asp:Literal ID="litVille" runat="server" /></span>
                </div>
                <div class="meta-item">
                    <span class="meta-label">Publié le :</span>
                    <span class="meta-value"><asp:Literal ID="litDate" runat="server" /></span>
                </div>
            </div>

            <div class="detail-section">
                <h3>📝 Description</h3>
                <div class="detail-content">
                    <asp:Literal ID="litDescription" runat="server" />
                </div>
            </div>

            <div class="detail-section">
                <h3>🔍 Signes distinctifs</h3>
                <div class="detail-content">
                    <asp:Literal ID="litSignes" runat="server" />
                </div>
            </div>

            <div class="detail-actions">
                <a href="Signalement.aspx" class="btn btn-orange">
                    📢 Soumettre une information sur ce dossier
                </a>
                <a href="Dossiers.aspx" class="btn btn-outline">
                    ← Retour à la liste
                </a>
            </div>

        </div>
    </div>

    <!-- STYLES RESPONSIVES INTÉGRÉS -->
    <style>
        /* ========== DOSSIER DETAIL - RESPONSIVE ========== */
        
        * {
            box-sizing: border-box;
        }
        
        .detail-layout {
            display: flex;
            gap: 30px;
            flex-wrap: wrap;
        }
        
        /* Colonne photo */
        .detail-photo-container {
            flex: 1;
            min-width: 280px;
            max-width: 400px;
        }
        
        .detail-photo {
            width: 100%;
            height: auto;
            border-radius: 12px;
            box-shadow: 0 4px 16px rgba(0,0,0,0.1);
            object-fit: cover;
            background: #f4f6fb;
        }
        
        /* Colonne infos */
        .detail-card {
            flex: 2;
            min-width: 300px;
            background: white;
            border-radius: 12px;
            padding: 25px;
            box-shadow: 0 2px 12px rgba(0,0,0,0.08);
        }
        
        /* Badge */
        .badge-container {
            margin-bottom: 15px;
        }
        
        .detail-card h1 {
            font-size: 28px;
            color: #1a2235;
            margin: 0 0 5px 0;
        }
        
        .ref-dossier {
            color: #555e6d;
            font-size: 13px;
            margin-top: 4px;
            margin-bottom: 20px;
            padding-bottom: 15px;
            border-bottom: 1px solid #eef2f6;
        }
        
        /* Métadonnées */
        .detail-meta {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            background: #f4f6fb;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 25px;
        }
        
        .meta-item {
            font-size: 14px;
        }
        
        .meta-label {
            font-weight: 600;
            color: #1a2235;
        }
        
        .meta-value {
            color: #555;
            margin-left: 5px;
        }
        
        /* Sections */
        .detail-section {
            margin-bottom: 25px;
        }
        
        .detail-section h3 {
            font-size: 18px;
            color: #003f7d;
            margin-bottom: 12px;
            font-weight: 600;
        }
        
        .detail-content {
            line-height: 1.6;
            color: #333;
            font-size: 15px;
        }
        
        /* Boutons d'action */
        .detail-actions {
            display: flex;
            gap: 15px;
            flex-wrap: wrap;
            margin-top: 25px;
            padding-top: 20px;
            border-top: 1px solid #eef2f6;
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
        
        .btn-orange {
            background: #e8600a;
            color: white;
        }
        
        .btn-orange:hover {
            background: #d45508;
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
        
        /* ========== RESPONSIVE ========== */
        
        /* Tablettes (max-width: 768px) */
        @media (max-width: 768px) {
            .detail-layout {
                flex-direction: column;
                gap: 20px;
            }
            
            .detail-photo-container {
                max-width: 100%;
                min-width: auto;
            }
            
            .detail-photo {
                max-height: 350px;
                object-fit: cover;
            }
            
            .detail-card {
                padding: 20px;
            }
            
            .detail-card h1 {
                font-size: 24px;
            }
            
            .detail-meta {
                grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                gap: 12px;
            }
            
            .detail-section h3 {
                font-size: 16px;
            }
            
            .detail-content {
                font-size: 14px;
            }
            
            .detail-actions {
                flex-direction: column;
            }
            
            .detail-actions .btn {
                width: 100%;
                justify-content: center;
            }
        }
        
        /* Petits mobiles (max-width: 480px) */
        @media (max-width: 480px) {
            .detail-card {
                padding: 15px;
            }
            
            .detail-card h1 {
                font-size: 20px;
            }
            
            .detail-meta {
                grid-template-columns: 1fr;
                gap: 10px;
                padding: 12px;
            }
            
            .meta-item {
                font-size: 13px;
            }
            
            .detail-section h3 {
                font-size: 15px;
            }
            
            .detail-content {
                font-size: 13px;
            }
            
            .btn {
                padding: 8px 16px;
                font-size: 13px;
            }
            
            .ref-dossier {
                font-size: 12px;
            }
        }
        
        /* Grands écrans (min-width: 1200px) */
        @media (min-width: 1200px) {
            .detail-card {
                padding: 30px;
            }
            
            .detail-card h1 {
                font-size: 32px;
            }
            
            .detail-section h3 {
                font-size: 18px;
            }
        }
    </style>

</asp:Content>