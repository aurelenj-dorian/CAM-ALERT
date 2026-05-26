<%@ Page Title="Accueil" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="CamAlert._Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <!-- HERO -->
    <div class="hero">
        <div class="container">
            <h1>Portail National CAM-ALERT</h1>
            <p>Plateforme camerounaise officielle de signalement des personnes disparues et des avis de recherche judiciaire.</p>
            <div class="hero-btns">
                <a href="Dossiers.aspx" class="btn btn-orange">Consulter les dossiers</a>
                <a href="Signalement.aspx" class="btn btn-outline"
                   style="border-color:white;color:white;">
                    Soumettre une information
                </a>
            </div>
        </div>
    </div>

    <div class="container">

        <!-- BANNIERE LEGALE -->
        <div class="legal-banner">
            <strong>⚠ Avertissement :</strong> Ce portail est entièrement fictif, Aucune donnée présentée n'est réelle.
        </div>

        <!-- CARDS INFO -->
        <div class="cards-grid">
            <div class="card">
                <div class="card-icon">&#128269;</div>
                <h3>Personnes disparues</h3>
                <p>Consultez les avis de disparition signalés sur l'ensemble du territoire camerounais.</p>
                <br />
                <a href="Dossiers.aspx?type=1" class="btn btn-outline">Voir les dossiers</a>
            </div>
            <div class="card">
                <div class="card-icon">&#9878;</div>
                <h3>Personnes recherchées</h3>
                <p>Accédez aux avis de recherche judiciaire émis par les autorités compétentes.</p>
                <br />
                <a href="Dossiers.aspx?type=2" class="btn btn-outline">Voir les dossiers</a>
            </div>
            <div class="card">
                <div class="card-icon">&#128172;</div>
                <h3>Soumettre une information</h3>
                <p>Vous disposez d'un renseignement ? Transmettez-le de façon sécurisée.</p>
                <br />
                <a href="Signalement.aspx" class="btn btn-primary">Signaler maintenant</a>
            </div>
        </div>

        <!-- SECTION DERNIERS DOSSIERS -->
        <div style="margin-bottom:40px;">
            <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:20px;">
                <div>
                    <h2 style="color:#003f7d;font-size:22px;margin-bottom:4px;">
                        Derniers dossiers publiés
                    </h2>
                    <p style="color:#555e6d;font-size:14px;">
                        Personnes disparues et recherchées — Avis récents
                    </p>
                </div>
                <a href="Dossiers.aspx" class="btn btn-outline">Voir tous les dossiers</a>
            </div>

            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:20px;">

                <!-- Dossier 1 - MBALLA Jean-Pierre -->
                <div class="card" style="padding:0;overflow:hidden;text-align:center;">
                    <img src="/Admin/image/téléchargement.jpg"
                         style="width:100%;height:220px;object-fit:cover;"
                         alt="MBALLA Jean-Pierre" />
                    <div style="padding:16px;">
                        <span class="badge badge-disparition">Disparition</span>
                        <h3 style="margin-top:10px;font-size:15px;color:#1a1f2e;">MBALLA Jean-Pierre</h3>
                        <p style="font-size:12px;color:#555e6d;margin-top:4px;">Yaoundé – Centre – 2024</p>
                        <a href="DossierDetail.aspx?id=1" class="btn btn-outline"
                           style="margin-top:12px;padding:6px 14px;font-size:12px;">Voir la fiche</a>
                    </div>
                </div>

                <!-- Dossier 2 - FOUDA Marie-Claire -->
                <div class="card" style="padding:0;overflow:hidden;text-align:center;">
                    <img src="/Admin/image/téléchargement (1).jpg"
                         style="width:100%;height:220px;object-fit:cover;"
                         alt="FOUDA Marie-Claire" />
                    <div style="padding:16px;">
                        <span class="badge badge-recherche">Recherche judiciaire</span>
                        <h3 style="margin-top:10px;font-size:15px;color:#1a1f2e;">FOUDA Marie-Claire</h3>
                        <p style="font-size:12px;color:#555e6d;margin-top:4px;">Douala – Littoral – 2024</p>
                        <a href="DossierDetail.aspx?id=2" class="btn btn-outline"
                           style="margin-top:12px;padding:6px 14px;font-size:12px;">Voir la fiche</a>
                    </div>
                </div>

                <!-- Dossier 3 - NGONO Samuel -->
                <div class="card" style="padding:0;overflow:hidden;text-align:center;">
                    <img src="/Admin/image/téléchargement (2).jpg"
                         style="width:100%;height:220px;object-fit:cover;"
                         alt="NGONO Samuel" />
                    <div style="padding:16px;">
                        <span class="badge badge-disparition">Disparition</span>
                        <h3 style="margin-top:10px;font-size:15px;color:#1a1f2e;">NGONO Samuel</h3>
                        <p style="font-size:12px;color:#555e6d;margin-top:4px;">Bafoussam – Ouest – 2023</p>
                        <a href="DossierDetail.aspx?id=3" class="btn btn-outline"
                           style="margin-top:12px;padding:6px 14px;font-size:12px;">Voir la fiche</a>
                    </div>
                </div>

                <!-- Dossier 4 - BELLO Hamidou -->
                <div class="card" style="padding:0;overflow:hidden;text-align:center;">
                    <img src="/Admin/image/téléchargement (3).jpg"
                         style="width:100%;height:220px;object-fit:cover;"
                         alt="BELLO Hamidou" />
                    <div style="padding:16px;">
                        <span class="badge badge-recherche">Recherche judiciaire</span>
                        <h3 style="margin-top:10px;font-size:15px;color:#1a1f2e;">BELLO Hamidou</h3>
                        <p style="font-size:12px;color:#555e6d;margin-top:4px;">Garoua – Nord – 2023</p>
                        <a href="DossierDetail.aspx?id=4" class="btn btn-outline"
                           style="margin-top:12px;padding:6px 14px;font-size:12px;">Voir la fiche</a>
                    </div>
                </div>

                <!-- Dossier 5 - ONANA Cécile -->
                <div class="card" style="padding:0;overflow:hidden;text-align:center;">
                    <img src="/Admin/image/téléchargement (4).jpg"
                         style="width:100%;height:220px;object-fit:cover;"
                         alt="ONANA Cécile" />
                    <div style="padding:16px;">
                        <span class="badge badge-disparition">Disparition</span>
                        <h3 style="margin-top:10px;font-size:15px;color:#1a1f2e;">ONANA Cécile</h3>
                        <p style="font-size:12px;color:#555e6d;margin-top:4px;">Yaoundé – Centre – 2024</p>
                        <a href="DossierDetail.aspx?id=5" class="btn btn-outline"
                           style="margin-top:12px;padding:6px 14px;font-size:12px;">Voir la fiche</a>
                    </div>
                </div>

                <!-- Dossier 6 - NTEP Christian -->
                <div class="card" style="padding:0;overflow:hidden;text-align:center;">
                    <img src="/Admin/image/téléchargement (5).jpg"
                         style="width:100%;height:220px;object-fit:cover;"
                         alt="NTEP Christian" />
                    <div style="padding:16px;">
                        <span class="badge badge-recherche">Recherche judiciaire</span>
                        <h3 style="margin-top:10px;font-size:15px;color:#1a1f2e;">NTEP Christian</h3>
                        <p style="font-size:12px;color:#555e6d;margin-top:4px;">Douala – Littoral – 2024</p>
                        <a href="DossierDetail.aspx?id=6" class="btn btn-outline"
                           style="margin-top:12px;padding:6px 14px;font-size:12px;">Voir la fiche</a>
                    </div>
                </div>

                <!-- Dossier 7 - ABENA Rose -->
                <div class="card" style="padding:0;overflow:hidden;text-align:center;">
                    <img src="/Admin/image/téléchargement (6).jpg"
                         style="width:100%;height:220px;object-fit:cover;"
                         alt="ABENA Rose" />
                    <div style="padding:16px;">
                        <span class="badge badge-disparition">Disparition</span>
                        <h3 style="margin-top:10px;font-size:15px;color:#1a1f2e;">ABENA Rose</h3>
                        <p style="font-size:12px;color:#555e6d;margin-top:4px;">Kribi – Sud – 2023</p>
                        <a href="DossierDetail.aspx?id=7" class="btn btn-outline"
                           style="margin-top:12px;padding:6px 14px;font-size:12px;">Voir la fiche</a>
                    </div>
                </div>

                <!-- Dossier 8 - MVONDO Paul -->
                <div class="card" style="padding:0;overflow:hidden;text-align:center;">
                    <img src="/Admin/image/téléchargement (7).jpg"
                         style="width:100%;height:220px;object-fit:cover;"
                         alt="MVONDO Paul" />
                    <div style="padding:16px;">
                        <span class="badge badge-recherche">Recherche judiciaire</span>
                        <h3 style="margin-top:10px;font-size:15px;color:#1a1f2e;">MVONDO Paul</h3>
                        <p style="font-size:12px;color:#555e6d;margin-top:4px;">Yaoundé – Centre – 2023</p>
                        <a href="DossierDetail.aspx?id=8" class="btn btn-outline"
                           style="margin-top:12px;padding:6px 14px;font-size:12px;">Voir la fiche</a>
                    </div>
                </div>

            </div>
        </div>

        <!-- MISSION -->
        <div class="card" style="margin-bottom:32px;">
            <h3 style="font-size:18px;color:#003f7d;margin-bottom:12px;">Notre mission</h3>
            <p style="color:#555e6d;line-height:1.8;">
                Le portail CAM-ALERT est une plateforme institutionnelle permettant aux autorités camerounaises
                de diffuser des avis officiels concernant des personnes disparues ou recherchées par la justice.
                Les citoyens peuvent consulter les dossiers et soumettre des informations utiles aux enquêteurs.
            </p>
        </div>

    </div>

</asp:Content>