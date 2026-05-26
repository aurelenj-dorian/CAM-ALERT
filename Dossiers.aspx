<%@ Page Title="Dossiers" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Dossiers.aspx.cs" Inherits="CamAlert.Dossiers" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    
    <div style="margin-bottom:16px;">
        <a href="Default.aspx" class="btn btn-outline" style="padding:7px 16px;font-size:13px;">
            &#8592; Retour à l'accueil
        </a>
    </div>

    <div class="page-header">
        <h2>Dossiers actifs</h2>
        <p>Liste des personnes disparues et recherchées enregistrées sur le portail CAM-ALERT.</p>
    </div>

    <!-- BARRE DE RECHERCHE DYNAMIQUE -->
    <div class="search-container">
        <div class="search-wrapper">
            <input type="text" id="searchInput" class="search-input" 
                   placeholder="Rechercher par nom, prénom, ville..." 
                   autocomplete="off">
            <div id="suggestionsBox" class="suggestions-box"></div>
        </div>
        <div id="searchLoader" class="search-loader" style="display: none;">Chargement...</div>
    </div>

    <!-- FILTRES CLASSIQUES (sans AutoPostBack) -->
    <div class="filters-bar" id="filtersBar">
        <div class="filter-group">
            <label>Type de dossier</label>
            <asp:DropDownList ID="ddlType" runat="server" ClientIDMode="Static">
                <asp:ListItem Value="">Tous les types</asp:ListItem>
                <asp:ListItem Value="Disparition">Disparition</asp:ListItem>
                <asp:ListItem Value="Recherche judiciaire">Recherche judiciaire</asp:ListItem>
            </asp:DropDownList>
        </div>
        <div class="filter-group">
            <label>Année</label>
            <asp:DropDownList ID="ddlAnnee" runat="server" ClientIDMode="Static">
                <asp:ListItem Value="">Toutes les années</asp:ListItem>
                <asp:ListItem Value="2025">2025</asp:ListItem>
                <asp:ListItem Value="2024">2024</asp:ListItem>
                <asp:ListItem Value="2023">2023</asp:ListItem>
                <asp:ListItem Value="2022">2022</asp:ListItem>
            </asp:DropDownList>
        </div>
    </div>

    <!-- Message "Aucun résultat" -->
    <div id="noResultsMsg" class="no-results" style="display: none;">
        Aucun dossier ne correspond à votre recherche.
    </div>

    <!-- RÉSULTATS DYNAMIQUES -->
    <div id="dynamicResults" class="dynamic-results"></div>

    <!-- GRIDVIEW STATIQUE (caché) -->
    <div id="staticGridView" style="display: none;">
        <asp:GridView ID="gvDossiers" runat="server"
            AutoGenerateColumns="false"
            CssClass="dossiers-table"
            AllowPaging="true"
            PageSize="10"
            OnPageIndexChanging="gvDossiers_PageIndexChanging"
            DataKeyNames="DossierID"
            EmptyDataText="Aucun dossier trouvé.">
            <Columns>
                <asp:BoundField DataField="Titre" HeaderText="Identité / Référence" />
                <asp:TemplateField HeaderText="Type">
                    <ItemTemplate>
                        <span class='<%# (string)Eval("TypeLibelle") == "Disparition" ? "badge badge-disparition" : "badge badge-recherche" %>'>
                            <%# Eval("TypeLibelle") %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Annee" HeaderText="Année" />
                <asp:BoundField DataField="RegionNom" HeaderText="Région" />
                <asp:TemplateField HeaderText="Action">
                    <ItemTemplate>
                        <a href='DossierDetail.aspx?id=<%# Eval("DossierID") %>'
                           class="btn btn-outline" 
                           style="padding:5px 12px;font-size:12px;">
                            Voir la fiche
                        </a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

    <style>
        /* Vos styles existants */
        .search-container { margin-bottom: 25px; }
        .search-wrapper { position: relative; width: 100%; max-width: 500px; }
        .search-input { width: 100%; padding: 14px 15px 14px 20px; font-size: 16px; border: 2px solid #e0e4e8; border-radius: 50px; transition: all 0.3s ease; outline: none; background: white; }
        .search-input:focus { border-color: #003f7d; box-shadow: 0 0 0 3px rgba(0,63,125,0.1); transform: scale(1.01); }
        .suggestions-box { position: absolute; top: 100%; left: 0; right: 0; background: white; border-radius: 12px; box-shadow: 0 8px 24px rgba(0,0,0,0.15); z-index: 1000; max-height: 300px; overflow-y: auto; display: none; margin-top: 5px; }
        .suggestions-box.active { display: block; animation: fadeInUp 0.2s ease; }
        .suggestion-item { padding: 12px 18px; cursor: pointer; transition: background 0.15s; border-bottom: 1px solid #f0f2f7; }
        .suggestion-item:hover { background: #f4f6fb; }
        .suggestion-item strong { color: #003f7d; }
        .suggestion-type { font-size: 11px; padding: 2px 8px; border-radius: 20px; margin-left: 10px; }
        .suggestion-type.disparu { background: #fff0e6; color: #e8600a; }
        .suggestion-type.recherche { background: #e8f0fe; color: #003f7d; }
        .dynamic-results { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 20px; margin-top: 20px; }
        .result-card { background: white; border-radius: 12px; padding: 18px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); transition: transform 0.2s, box-shadow 0.2s; animation: fadeInUp 0.3s ease; }
        .result-card:hover { transform: translateY(-3px); box-shadow: 0 8px 20px rgba(0,0,0,0.12); }
        .result-title { font-size: 18px; font-weight: 700; color: #1a2235; margin-bottom: 8px; }
        .result-badge { display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; margin-bottom: 10px; }
        .result-badge.disparition { background: #fff0e6; color: #e8600a; }
        .result-badge.recherche { background: #e8f0fe; color: #003f7d; }
        .result-details { font-size: 13px; color: #666; margin: 8px 0; }
        .result-link { display: inline-block; margin-top: 10px; padding: 6px 14px; background: #003f7d; color: white; text-decoration: none; border-radius: 6px; font-size: 12px; transition: background 0.2s; }
        .result-link:hover { background: #00509e; }
        .search-loader { text-align: center; padding: 10px; color: #666; font-size: 13px; }
        .no-results { text-align: center; padding: 40px; background: white; border-radius: 12px; color: #666; }
        @keyframes fadeInUp { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
    </style>

    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script>
        var allDossiers = [];

        $(document).ready(function () {
            // Lire le paramètre search dans l'URL (pour la loupe)
            const urlParams = new URLSearchParams(window.location.search);
            const searchTermFromUrl = urlParams.get('search');
            if (searchTermFromUrl) {
                $('#searchInput').val(searchTermFromUrl);
            }

            // Charger tous les dossiers via SearchDossiers
            $.ajax({
                type: 'POST',
                url: 'Dossiers.aspx/SearchDossiers',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify({ searchTerm: '', typeId: '', annee: '' }),
                success: function (response) {
                    allDossiers = response.d;
                    console.log("Dossiers chargés :", allDossiers.length);
                    applyFilters();
                },
                error: function (xhr) {
                    console.error("Erreur chargement dossiers :", xhr.responseText);
                    $('#dynamicResults').html('<div class="no-results">Erreur de chargement des données.</div>');
                }
            });

            // Écouteurs d'événements
            $('#ddlType, #ddlAnnee').on('change', function () {
                console.log("Filtre changé : type=" + $('#ddlType').val() + ", année=" + $('#ddlAnnee').val());
                applyFilters();
            });

            $('#searchInput').on('input', function () {
                applyFilters();
            });

            // Fermer les suggestions
            $(document).on('click', function (e) {
                if (!$(e.target).closest('.search-wrapper').length) {
                    $('#suggestionsBox').removeClass('active').empty();
                }
            });
        });

        function applyFilters() {
            var typeId = $('#ddlType').val();
            var annee = $('#ddlAnnee').val();
            var searchTerm = $('#searchInput').val().trim().toLowerCase();

            var filtered = allDossiers.filter(function (d) {
                // Filtre type
                if (typeId && typeId !== "") {
                    // Comparaison directe des libellés (car les valeurs des options sont maintenant "Disparition" et "Recherche judiciaire")
                    if (d.TypeLibelle !== typeId) return false;
                }
                // Filtre année
                if (annee && annee !== "") {
                    if (d.Annee.toString() !== annee) return false;
                }
                // Filtre recherche
                if (searchTerm !== "") {
                    if (d.Titre.toLowerCase().indexOf(searchTerm) === -1 &&
                        d.Ville.toLowerCase().indexOf(searchTerm) === -1 &&
                        d.RegionNom.toLowerCase().indexOf(searchTerm) === -1) {
                        return false;
                    }
                }
                return true;
            });

            console.log("Résultats filtrés :", filtered.length);
            displayResults(filtered);
            updateSuggestions(filtered, searchTerm);
        }

        function displayResults(dossiers) {
            var container = $('#dynamicResults');
            container.empty();
            if (!dossiers || dossiers.length === 0) {
                $('#noResultsMsg').show();
                container.hide();
                return;
            }
            $('#noResultsMsg').hide();
            container.show();
            for (var i = 0; i < dossiers.length; i++) {
                var d = dossiers[i];
                var badgeClass = d.TypeLibelle === 'Disparition' ? 'disparition' : 'recherche';
                var badgeText = d.TypeLibelle === 'Disparition' ? '⚠️ Disparition' : '🔍 Recherche judiciaire';
                var card = $('<div class="result-card"></div>');
                card.html('<div class="result-title">' + escapeHtml(d.Titre) + '</div>' +
                    '<span class="result-badge ' + badgeClass + '">' + badgeText + '</span>' +
                    '<div class="result-details">📍 ' + escapeHtml(d.Ville || 'Non spécifiée') + ' | 📅 ' + d.Annee + ' | 🗺️ ' + escapeHtml(d.RegionNom || 'Non spécifiée') + '</div>' +
                    '<a href="DossierDetail.aspx?id=' + d.DossierID + '" class="result-link">Voir la fiche →</a>');
                container.append(card);
            }
        }

        function updateSuggestions(dossiers, searchTerm) {
            var box = $('#suggestionsBox');
            if (searchTerm.length < 2) {
                box.removeClass('active').empty();
                return;
            }
            var suggestions = dossiers.filter(function (d) {
                return d.Titre.toLowerCase().indexOf(searchTerm) !== -1;
            }).slice(0, 10);
            box.empty();
            if (suggestions.length > 0) {
                for (var i = 0; i < suggestions.length; i++) {
                    var s = suggestions[i];
                    var item = $('<div class="suggestion-item"></div>');
                    var typeClass = s.TypeLibelle === 'Disparition' ? 'disparu' : 'recherche';
                    item.html('<strong>' + escapeHtml(s.Titre) + '</strong>' +
                        '<span class="suggestion-type ' + typeClass + '">' + (s.TypeLibelle === 'Disparition' ? '⚠️ Disparu(e)' : '🔍 Recherché(e)') + '</span>' +
                        '<div style="font-size:12px; color:#888;">' + escapeHtml(s.Ville) + ' · ' + s.Annee + '</div>');
                    item.on('click', function () {
                        var titre = $(this).find('strong').text();
                        $('#searchInput').val(titre);
                        applyFilters();
                        box.removeClass('active').empty();
                    });
                    box.append(item);
                }
                box.addClass('active');
            } else {
                box.removeClass('active');
            }
        }

        function escapeHtml(str) {
            if (!str) return '';
            return str.replace(/[&<>]/g, function (m) {
                if (m === '&') return '&amp;';
                if (m === '<') return '&lt;';
                if (m === '>') return '&gt;';
                return m;
            });
        }
    </script>
</asp:Content>