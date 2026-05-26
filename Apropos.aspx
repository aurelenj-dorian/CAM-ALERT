<%@ Page Title="À propos" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="APropos.aspx.cs" Inherits="CamAlert.APropos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div style="margin-bottom:16px;">
        <a href="Default.aspx" class="btn btn-outline"
           style="padding:7px 16px;font-size:13px;">
            &#8592; Retour à l'accueil
        </a>
    </div>

    <!-- EN-TETE -->
    <div class="page-header" style="border-left:4px solid #e8600a;">
        <h2 style="color:#003f7d;">À propos de CAM-ALERT</h2>
        <p>Portail national camerounais de signalement — Informations et contexte</p>
    </div>

    <!-- CONTEXTE -->
    <div class="card" style="margin-bottom:24px;">
        <div style="display:flex;align-items:center;gap:14px;margin-bottom:16px;">
            <div style="font-size:32px;">&#127468;&#127�;</div>
            <h3 style="font-size:19px;color:#003f7d;">Contexte & Problématique</h3>
        </div>
        <p style="color:#555e6d;line-height:1.9;font-size:15px;">
            Au Cameroun, comme dans de nombreux pays, les autorités de sécurité et d'administration 
            territoriale diffusent régulièrement des avis officiels concernant des personnes disparues, 
            des personnes recherchées par la justice, ainsi que des appels à la collaboration citoyenne.
        </p>
        <p style="color:#555e6d;line-height:1.9;font-size:15px;margin-top:12px;">
            Face à la dispersion de ces informations sur différents canaux (affichages physiques, 
            bulletins officiels, médias locaux), le besoin d'une plateforme centralisée, accessible 
            et sécurisée s'est imposé comme une nécessité pour améliorer l'efficacité des recherches 
            et renforcer la collaboration citoyenne.
        </p>
    </div>

    <!-- MISSION -->
    <div class="card" style="margin-bottom:24px;">
        <div style="display:flex;align-items:center;gap:14px;margin-bottom:16px;">
            <div style="font-size:32px;">&#127919;</div>
            <h3 style="font-size:19px;color:#003f7d;">Mission du portail</h3>
        </div>
        <p style="color:#555e6d;line-height:1.9;font-size:15px;">
            CAM-ALERT est une plateforme institutionnelle centralisée permettant aux autorités 
            camerounaises de diffuser et gérer efficacement les avis officiels concernant 
            les personnes disparues ou recherchées par la justice.
        </p>

        <!-- FONCTIONNALITES -->
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));
                    gap:16px;margin-top:20px;">

            <div style="background:#f0f5ff;border:1px solid #c5d5f0;border-radius:8px;padding:18px;">
                <div style="font-size:24px;margin-bottom:10px;">&#128269;</div>
                <strong style="color:#003f7d;">Consultation publique</strong>
                <p style="color:#555e6d;font-size:13px;margin-top:6px;">
                    Accès libre aux avis de disparition et de recherche judiciaire pour tout citoyen.
                </p>
            </div>

            <div style="background:#fff5f0;border:1px solid #f0c5a0;border-radius:8px;padding:18px;">
                <div style="font-size:24px;margin-bottom:10px;">&#128202;</div>
                <strong style="color:#e8600a;">Recherche & Filtrage</strong>
                <p style="color:#555e6d;font-size:13px;margin-top:6px;">
                    Filtrage des dossiers par type, région, année et mots-clés pour des recherches ciblées.
                </p>
            </div>

            <div style="background:#f0fff4;border:1px solid #a0d5b0;border-radius:8px;padding:18px;">
                <div style="font-size:24px;margin-bottom:10px;">&#128172;</div>
                <strong style="color:#2e7d32;">Remontée d'informations</strong>
                <p style="color:#555e6d;font-size:13px;margin-top:6px;">
                    Formulaire sécurisé permettant aux citoyens de transmettre des informations utiles.
                </p>
            </div>

            <div style="background:#f5f0ff;border:1px solid #c5a0f0;border-radius:8px;padding:18px;">
                <div style="font-size:24px;margin-bottom:10px;">&#128274;</div>
                <strong style="color:#6a1b9a;">Gestion sécurisée</strong>
                <p style="color:#555e6d;font-size:13px;margin-top:6px;">
                    Espace d'administration réservé aux agents habilités pour gérer les dossiers et signalements.
                </p>
            </div>

        </div>
    </div>

    <!-- FONCTIONNEMENT -->
    <div class="card" style="margin-bottom:24px;">
        <div style="display:flex;align-items:center;gap:14px;margin-bottom:16px;">
            <div style="font-size:32px;">&#9881;</div>
            <h3 style="font-size:19px;color:#003f7d;">Comment ça fonctionne ?</h3>
        </div>

        <div style="display:flex;flex-direction:column;gap:16px;">

            <div style="display:flex;align-items:flex-start;gap:16px;">
                <div style="min-width:36px;height:36px;background:#003f7d;color:white;
                            border-radius:50%;display:flex;align-items:center;
                            justify-content:center;font-weight:700;font-size:16px;">1</div>
                <div>
                    <strong style="color:#003f7d;">Les autorités publient un dossier</strong>
                    <p style="color:#555e6d;font-size:14px;margin-top:4px;">
                        Un agent habilité crée un dossier (disparition ou recherche judiciaire) 
                        avec photo, description et signes distinctifs.
                    </p>
                </div>
            </div>

            <div style="display:flex;align-items:flex-start;gap:16px;">
                <div style="min-width:36px;height:36px;background:#e8600a;color:white;
                            border-radius:50%;display:flex;align-items:center;
                            justify-content:center;font-weight:700;font-size:16px;">2</div>
                <div>
                    <strong style="color:#e8600a;">Les citoyens consultent les avis</strong>
                    <p style="color:#555e6d;font-size:14px;margin-top:4px;">
                        Tout citoyen peut librement consulter, filtrer et rechercher 
                        les dossiers publiés sur le portail.
                    </p>
                </div>
            </div>

            <div style="display:flex;align-items:flex-start;gap:16px;">
                <div style="min-width:36px;height:36px;background:#2e7d32;color:white;
                            border-radius:50%;display:flex;align-items:center;
                            justify-content:center;font-weight:700;font-size:16px;">3</div>
                <div>
                    <strong style="color:#2e7d32;">Les citoyens soumettent des informations</strong>
                    <p style="color:#555e6d;font-size:14px;margin-top:4px;">
                        Via le formulaire de signalement, tout citoyen peut transmettre 
                        une information utile de façon confidentielle.
                    </p>
                </div>
            </div>

            <div style="display:flex;align-items:flex-start;gap:16px;">
                <div style="min-width:36px;height:36px;background:#6a1b9a;color:white;
                            border-radius:50%;display:flex;align-items:center;
                            justify-content:center;font-weight:700;font-size:16px;">4</div>
                <div>
                    <strong style="color:#6a1b9a;">Les agents traitent les signalements</strong>
                    <p style="color:#555e6d;font-size:14px;margin-top:4px;">
                        Les agents habilités analysent les informations reçues, 
                        mettent à jour les dossiers et clôturent les cas résolus.
                    </p>
                </div>
            </div>

        </div>
    </div>

    <!-- AVERTISSEMENT -->
    <div class="legal-banner" style="margin-bottom:24px;">
        <strong>⚠ Avertissement légal :</strong> Ce portail est entièrement fictif, 
        réalisé à des fins pédagogiques dans le cadre du cours <strong>TIPAM2</strong>. 
        Aucune donnée présentée n'est réelle. Toute ressemblance avec des personnes 
        réelles serait purement fortuite.
    </div>

    <!-- TECHNOLOGIE -->
    <div class="card" style="margin-bottom:32px;">
        <div style="display:flex;align-items:center;gap:14px;margin-bottom:16px;">
            <div style="font-size:32px;">&#128187;</div>
            <h3 style="font-size:19px;color:#003f7d;">Technologies utilisées</h3>
        </div>
        <div style="display:flex;flex-wrap:wrap;gap:10px;">
            <span style="background:#e8f0fe;color:#003f7d;padding:6px 16px;
                         border-radius:20px;font-size:13px;font-weight:600;
                         border:1px solid #c5d5f0;">ASP.NET Web Forms</span>
            <span style="background:#e8f0fe;color:#003f7d;padding:6px 16px;
                         border-radius:20px;font-size:13px;font-weight:600;
                         border:1px solid #c5d5f0;">C# / .NET Framework 4.8</span>
            <span style="background:#e8f0fe;color:#003f7d;padding:6px 16px;
                         border-radius:20px;font-size:13px;font-weight:600;
                         border:1px solid #c5d5f0;">SQL Server</span>
            <span style="background:#e8f0fe;color:#003f7d;padding:6px 16px;
                         border-radius:20px;font-size:13px;font-weight:600;
                         border:1px solid #c5d5f0;">ADO.NET</span>
            <span style="background:#e8f0fe;color:#003f7d;padding:6px 16px;
                         border-radius:20px;font-size:13px;font-weight:600;
                         border:1px solid #c5d5f0;">HTML5 / CSS3</span>
        </div>
    </div>

</asp:Content>