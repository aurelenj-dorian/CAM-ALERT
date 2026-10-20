/*
 * CAM-ALERT — service worker
 *
 * Pourquoi : le portail est consulté depuis le Cameroun, souvent sur réseau
 * mobile et avec une connexion intermittente. Sans cache, une coupure au
 * mauvais moment affiche la page d'erreur du navigateur, et le visiteur perd
 * la fiche qu'il était en train de lire.
 *
 * Trois stratégies, selon le type de requête :
 *
 *   • navigation (pages HTML)  → réseau d'abord, cache en secours, puis page
 *     hors ligne. On privilégie le réseau parce qu'un dossier peut être mis à
 *     jour ou retiré : servir une version périmée d'un avis de recherche
 *     serait pire qu'afficher un message d'indisponibilité ;
 *
 *   • ressources statiques (CSS, images, polices) → cache d'abord, avec
 *     rafraîchissement en arrière-plan. Elles changent rarement et
 *     représentent l'essentiel du poids de la page ;
 *
 *   • tout le reste → réseau seul.
 *
 * Ce qui n'est JAMAIS mis en cache :
 *   • les requêtes autres que GET (envoi d'un signalement, connexion) ;
 *   • /Admin/* : les écrans d'administration contiennent des données
 *     nominatives et des commentaires internes, qui n'ont rien à faire dans
 *     le cache du navigateur ;
 *   • /sante : c'est un point de contrôle, une réponse en cache le rendrait
 *     inutile ;
 *   • les réponses opaques venant d'autres origines (tuiles de carte, CDN).
 */

'use strict';

// Changer ce numéro force la mise à jour du cache chez tous les visiteurs.
var VERSION = 'cam-alert-v1';
var CACHE_COQUILLE = VERSION + '-coquille';
var CACHE_PAGES = VERSION + '-pages';
var CACHE_STATIQUE = VERSION + '-statique';

var PAGE_HORS_LIGNE = '/hors-ligne.html';

// Ressources indispensables pour afficher quelque chose sans réseau.
var COQUILLE = [
    PAGE_HORS_LIGNE,
    '/Content/css/style.css',
    '/Content/css/design.css',
    '/Content/css/dark-mode.css'
];

// Nombre maximal de pages conservées : on évite de remplir le stockage du
// téléphone avec l'historique de navigation complet.
var MAX_PAGES = 30;

self.addEventListener('install', function (evenement) {
    evenement.waitUntil(
        caches.open(CACHE_COQUILLE)
            .then(function (cache) {
                // addAll échoue en bloc si une seule ressource manque :
                // on ajoute une par une pour qu'un fichier absent ne prive
                // pas le visiteur de tout le cache.
                return Promise.all(COQUILLE.map(function (url) {
                    return cache.add(new Request(url, { cache: 'reload' }))
                        .catch(function () { /* ressource absente : on continue */ });
                }));
            })
            .then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener('activate', function (evenement) {
    evenement.waitUntil(
        caches.keys()
            .then(function (noms) {
                return Promise.all(noms.map(function (nom) {
                    // Suppression des caches des versions précédentes.
                    if (nom.indexOf(VERSION) !== 0) return caches.delete(nom);
                    return null;
                }));
            })
            .then(function () { return self.clients.claim(); })
    );
});

self.addEventListener('fetch', function (evenement) {
    var requete = evenement.request;

    if (requete.method !== 'GET') return;

    var url;
    try {
        url = new URL(requete.url);
    } catch (e) {
        return;
    }

    // Autres origines (tuiles OpenStreetMap, Leaflet sur CDN) : on laisse le
    // navigateur gérer, leurs réponses sont opaques et non inspectables.
    if (url.origin !== self.location.origin) return;

    if (estExclu(url.pathname)) return;

    if (requete.mode === 'navigate'
        || (requete.headers.get('accept') || '').indexOf('text/html') !== -1) {
        evenement.respondWith(reseauPuisCache(requete));
        return;
    }

    if (estStatique(url.pathname)) {
        evenement.respondWith(cachePuisReseau(requete));
    }
});

/** Chemins qui ne doivent jamais passer par le cache. */
function estExclu(chemin) {
    var c = chemin.toLowerCase();
    return c.indexOf('/admin/') === 0
        || c === '/sante'
        || c.indexOf('/uploads/') === 0
        || c.indexOf('/erreur.aspx') === 0
        || c.indexOf('.axd') !== -1;
}

function estStatique(chemin) {
    return /\.(css|js|png|jpe?g|webp|gif|svg|ico|woff2?|ttf)$/i.test(chemin);
}

/**
 * Pages : on tente le réseau, on garde une copie, et en cas d'échec on sert la
 * dernière version connue, puis la page hors ligne.
 */
function reseauPuisCache(requete) {
    return fetch(requete)
        .then(function (reponse) {
            if (reponse && reponse.ok && reponse.type === 'basic') {
                var copie = reponse.clone();
                caches.open(CACHE_PAGES).then(function (cache) {
                    cache.put(requete, copie);
                    limiterTaille(CACHE_PAGES, MAX_PAGES);
                });
            }
            return reponse;
        })
        .catch(function () {
            return caches.match(requete).then(function (enCache) {
                if (enCache) return enCache;
                return caches.match(PAGE_HORS_LIGNE).then(function (horsLigne) {
                    return horsLigne || new Response(
                        'Hors ligne. Reconnectez-vous au réseau pour consulter cette page.',
                        {
                            status: 503,
                            headers: { 'Content-Type': 'text/plain; charset=utf-8' }
                        });
                });
            });
        });
}

/**
 * Ressources statiques : réponse immédiate depuis le cache, puis
 * rafraîchissement silencieux en arrière-plan.
 */
function cachePuisReseau(requete) {
    return caches.match(requete).then(function (enCache) {
        var reseau = fetch(requete)
            .then(function (reponse) {
                if (reponse && reponse.ok && reponse.type === 'basic') {
                    var copie = reponse.clone();
                    caches.open(CACHE_STATIQUE).then(function (cache) {
                        cache.put(requete, copie);
                    });
                }
                return reponse;
            })
            .catch(function () { return enCache; });

        return enCache || reseau;
    });
}

/** Garde les N entrées les plus récentes d'un cache. */
function limiterTaille(nomCache, maximum) {
    caches.open(nomCache).then(function (cache) {
        cache.keys().then(function (cles) {
            if (cles.length <= maximum) return;
            // cache.keys() renvoie les clés dans l'ordre d'insertion :
            // les premières sont les plus anciennes.
            for (var i = 0; i < cles.length - maximum; i++) {
                cache.delete(cles[i]);
            }
        });
    });
}
