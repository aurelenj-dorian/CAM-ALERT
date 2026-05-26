<%@ Page Title="Carte interactive" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Carte.aspx.cs" Inherits="CamAlert.Carte" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-header">
        <h1>📍 Carte interactive des signalements</h1>
        <p>Visualisez les derniers signalements géolocalisés au Cameroun</p>
    </div>

    <div style="display: flex; gap: 20px; flex-wrap: wrap;">
        <!-- Carte -->
        <div style="flex: 3; min-width: 300px;">
            <div class="card" style="padding: 0; overflow: hidden;">
                <div id="map" style="height: 550px; width: 100%;"></div>
            </div>
        </div>
        
        <!-- Légende et panneau d'infos -->
        <div style="flex: 1; min-width: 250px;">
            <!-- Légende -->
            <div class="card" style="margin-bottom: 20px;">
                <h3 style="margin-bottom: 15px;">📖 Légende</h3>
                <div style="display: flex; flex-direction: column; gap: 12px;">
                    <div style="display: flex; align-items: center; gap: 12px;">
                        <div style="width: 32px; height: 32px; background: #e8600a; border-radius: 50%; display: flex; align-items: center; justify-content: center; box-shadow: 0 2px 4px rgba(0,0,0,0.2);">
                            <span style="color: white; font-size: 14px;">⚠️</span>
                        </div>
                        <div>
                            <strong style="color: #e8600a;">Personne disparue</strong>
                            <p style="margin: 0; font-size: 12px; color: #666;">Signalement de disparition</p>
                        </div>
                    </div>
                    <div style="display: flex; align-items: center; gap: 12px;">
                        <div style="width: 32px; height: 32px; background: #003f7d; border-radius: 50%; display: flex; align-items: center; justify-content: center; box-shadow: 0 2px 4px rgba(0,0,0,0.2);">
                            <span style="color: white; font-size: 14px;">🔍</span>
                        </div>
                        <div>
                            <strong style="color: #003f7d;">Personne recherchée</strong>
                            <p style="margin: 0; font-size: 12px; color: #666;">Signalement de recherche</p>
                        </div>
                    </div>
                    <hr />
                    <div style="display: flex; align-items: center; gap: 12px;">
                        <div style="width: 32px; height: 32px; background: #6c757d; border-radius: 4px; display: flex; align-items: center; justify-content: center;">
                            <span style="color: white; font-size: 14px;">⊕</span>
                        </div>
                        <div>
                            <strong>Groupes de marqueurs</strong>
                            <p style="margin: 0; font-size: 12px; color: #666;">Zoomer pour voir les détails</p>
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- Panneau d'informations -->
            <div id="info-panel" class="card" style="display: none; animation: fadeInUp 0.3s ease;">
                <h3 id="info-title" style="color: #003f7d; margin-bottom: 10px;"></h3>
                <p id="info-description" style="margin: 10px 0;"></p>
                <p id="info-date" style="font-size: 12px; color: #666;"></p>
                <a id="info-link" href="#" class="btn btn-primary" style="margin-top: 10px; width: 100%; text-align: center;">Voir le dossier complet</a>
            </div>
            
            <!-- Compteur de signalements -->
            <div class="card">
                <h3>📊 Statistiques</h3>
                <div id="stats-count">
                    <p><strong id="total-count">0</strong> signalements affichés</p>
                    <p><strong id="disparus-count">0</strong> personnes disparues</p>
                    <p><strong id="recherches-count">0</strong> personnes recherchées</p>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        // Coordonnées du Cameroun
        var cameroonCenter = [7.3697, 12.3547];

        // Données des signalements (seront remplacées par les données du code-behind)
        var signalements = [];

        // Vérifier si les données sont chargées
        function initMap() {
            if (typeof signalementsData !== 'undefined') {
                signalements = signalementsData;
                console.log("Données chargées :", signalements); // Vérifier les IDs
            }

            // Initialisation de la carte
            var map = L.map('map').setView(cameroonCenter, 6);

            // Ajout du fond de carte
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
                maxZoom: 19
            }).addTo(map);

            // Création du cluster de marqueurs
            var markers = L.markerClusterGroup({
                showCoverageOnHover: false,
                maxClusterRadius: 50,
                iconCreateFunction: function (cluster) {
                    var count = cluster.getChildCount();
                    return L.divIcon({
                        html: '<div style="background: #003f7d; color: white; border-radius: 50%; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; font-weight: bold; font-size: 14px; border: 2px solid white; box-shadow: 0 2px 6px rgba(0,0,0,0.3);">' + count + '</div>',
                        className: 'custom-cluster',
                        iconSize: L.point(36, 36)
                    });
                }
            });

            var disparusCount = 0;
            var recherchesCount = 0;

            // Parcours des signalements
            if (signalements && signalements.length > 0) {
                signalements.forEach(function (s) {
                    // Vérifier que les coordonnées sont valides
                    if (!s.Latitude || !s.Longitude) return;

                    // Utiliser DossierID (et non Id) pour l'identifiant
                    var dossierId = s.DossierID || s.Id; // fallback au cas où
                    if (!dossierId) {
                        console.warn("Aucun ID trouvé pour", s);
                        return;
                    }

                    var isDisparu = s.Type === "Disparu";
                    var markerColor = isDisparu ? "#e8600a" : "#003f7d";
                    var iconHtml = isDisparu ? "⚠️" : "🔍";

                    // Comptage
                    if (isDisparu) disparusCount++;
                    else recherchesCount++;

                    // Créer un marqueur personnalisé
                    var marker = L.marker([parseFloat(s.Latitude), parseFloat(s.Longitude)], {
                        icon: L.divIcon({
                            className: 'custom-marker',
                            html: '<div style="background-color: ' + markerColor + '; width: 30px; height: 30px; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; font-size: 14px; font-weight: bold; box-shadow: 0 2px 6px rgba(0,0,0,0.3); border: 2px solid white; transition: transform 0.2s; cursor: pointer;">' + iconHtml + '</div>',
                            iconSize: [30, 30],
                            popupAnchor: [0, -15]
                        })
                    });

                    // Contenu du popup
                    var typeLabel = isDisparu ? "badge-orange" : "badge-blue";
                    var typeText = isDisparu ? "⚠️ Disparu(e)" : "🔍 Recherché(e)";

                    var popupContent = '<div style="min-width: 200px;">' +
                        '<strong style="font-size: 16px;">' + (s.NomComplet || s.Prenom + " " + s.Nom) + '</strong><br>' +
                        '<span class="badge ' + typeLabel + '" style="margin: 5px 0;">' + typeText + '</span><br>' +
                        '<i class="fas fa-map-marker-alt"></i> <strong>Ville:</strong> ' + (s.Ville || "Non spécifiée") + '<br>' +
                        '<i class="far fa-calendar-alt"></i> <strong>Date:</strong> ' + (s.DateSignalement || "Non spécifiée") + '<br>' +
                        '<p style="margin: 8px 0;">' + (s.Description || "Aucune description disponible.") + '</p>' +
                        '<a href="DossierDetail.aspx?id=' + dossierId + '" class="btn btn-primary" style="display: block; text-align: center; margin-top: 10px; padding: 6px 12px; font-size: 12px;">📄 Voir le dossier</a>' +
                        '</div>';

                    marker.bindPopup(popupContent);

                    // Événement au clic pour le panneau d'infos
                    marker.on('click', function () {
                        var panel = document.getElementById('info-panel');
                        if (panel) {
                            document.getElementById('info-title').innerHTML = s.NomComplet || (s.Prenom + " " + s.Nom);
                            document.getElementById('info-description').innerHTML = s.Description || "Aucune description disponible.";
                            document.getElementById('info-date').innerHTML = '📅 Signalé le : ' + (s.DateSignalement || "Date inconnue");
                            document.getElementById('info-link').href = 'DossierDetail.aspx?id=' + dossierId;
                            panel.style.display = 'block';
                            panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                        }
                    });

                    markers.addLayer(marker);
                });
            }

            // Mettre à jour les statistiques
            document.getElementById('total-count').innerText = signalements.length;
            document.getElementById('disparus-count').innerText = disparusCount;
            document.getElementById('recherches-count').innerText = recherchesCount;

            // Ajouter les marqueurs à la carte
            if (markers.getLayers().length > 0) {
                map.addLayer(markers);

                // Ajuster la vue pour voir tous les marqueurs
                var bounds = markers.getBounds();
                if (bounds.isValid()) {
                    map.fitBounds(bounds, { padding: [30, 30] });
                }
            } else {
                // Afficher un message si aucun marqueur
                document.getElementById('map').innerHTML = '<div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f8f9fa;"><div style="text-align: center;"><p>📍 Aucun signalement géolocalisé pour le moment.</p><p style="font-size: 12px; color: #666;">Les signalements avec coordonnées GPS apparaîtront ici.</p></div></div>';
            }
        }

        // Attendre que la page soit chargée
        document.addEventListener('DOMContentLoaded', function () {
            setTimeout(initMap, 100);
        });

        if (typeof signalementsData !== 'undefined') {
            setTimeout(initMap, 50);
        }
    </script>
    
    <style>
        .custom-marker div:hover {
            transform: scale(1.1);
            transition: transform 0.2s;
        }
        .leaflet-popup-content {
            font-family: 'Segoe UI', sans-serif;
            font-size: 13px;
            line-height: 1.4;
            min-width: 220px;
        }
        .leaflet-popup-content .btn-primary {
            background: #003f7d;
            color: white;
            text-decoration: none;
            border-radius: 6px;
        }
        .leaflet-cluster-anim .leaflet-marker-icon, 
        .leaflet-cluster-anim .leaflet-marker-shadow {
            transition: transform 0.2s ease-out, opacity 0.2s ease-in;
        }
        #info-panel {
            transition: all 0.3s ease;
        }
    </style>
</asp:Content>