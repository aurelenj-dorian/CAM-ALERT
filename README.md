# CAM-ALERT — ASP.NET Core + React

Refonte du portail CAM-ALERT, site fictif inspiré d'INTERPOL réalisé en groupe dans un cadre pédagogique.

## Versions

| Version | Tag | Technologie |
|---|---|---|
| 2 (actuelle, branche `main`) | `v2.0.0` | ASP.NET Core 9 MVC + API, front-end React (Vite + TypeScript) |
| 1 | `v1.0.0` (branche `v1`) | ASP.NET Web Forms, .NET Framework 4.8 |

Pour récupérer la première version : `git checkout v1.0.0`.

## Architecture

- **Site public** : application React (Vite + TypeScript) dans `ClientApp/`, servie par ASP.NET depuis `wwwroot/app/`.
- **API JSON** : `Controllers/Api/` (`/api/...`).
- **Espace administration** : vues Razor MVC dans `Areas/Admin/` (`/admin/...`), réservé au rôle `Administrateur`.
- **Données** : même base SQL Server `CamAlert` que l'ancien site (Dapper, requêtes paramétrées dans `Data/`).

## Prérequis

- .NET SDK 9 (ou plus récent) — Visual Studio 2022 17.12+ ou Visual Studio 2026
- Node.js 20 ou plus récent
- SQL Server avec la base `CamAlert` existante de la version 1 (scripts de création non publiés : ils contiennent les comptes d'administration)

## Installation

```powershell
# 1. Table de mesure d'audience (idempotent)
sqlcmd -S AURELE -E -i Database\AjoutAudience.sql

# 2. Dépendances du front-end
cd ClientApp
npm install
cd ..
```

La chaîne de connexion se trouve dans `appsettings.json` (`ConnectionStrings:CamAlertDB`).

### Secrets SMTP (formulaire de contact)

Aucun mot de passe n'est stocké dans le code ni dans `appsettings.json`. En développement, utilisez les *user secrets* :

```powershell
dotnet user-secrets set "Smtp:Compte" "compte@gmail.com"
dotnet user-secrets set "Smtp:MotDePasse" "mot-de-passe-application"
dotnet user-secrets set "Smtp:Destinataires:0" "destinataire1@exemple.com"
dotnet user-secrets set "Smtp:Destinataires:1" "destinataire2@exemple.com"
```

En production, utilisez des variables d'environnement (`Smtp__Compte`, `Smtp__MotDePasse`, `Smtp__Destinataires__0`…).
Sans configuration SMTP, le formulaire de contact répond « Le message n'a pas pu être envoyé » (503) ; le reste du site fonctionne.

## Lancer en développement

**Option A — un seul serveur (le plus simple)** : construire le front-end une fois, puis lancer ASP.NET.

```powershell
cd ClientApp; npm run build; cd ..
dotnet run --launch-profile http
```

Ouvrir http://localhost:5109 (site public) et http://localhost:5109/admin (administration).

**Option B — rechargement à chaud du front-end** : deux terminaux.

```powershell
# Terminal 1 : API + administration
dotnet run --launch-profile http

# Terminal 2 : site React avec rechargement à chaud
cd ClientApp
npm run dev
```

Ouvrir http://localhost:5173 : Vite redirige `/api`, `/admin`, `/uploads` et `/Admin/image` vers ASP.NET.
L'espace administration utilise les fichiers construits (`admin.css`, `admin.js`) : lancez `npm run build` après une modification de `src/admin/`.

## Publier

```powershell
dotnet publish -c Release -o publication
```

La publication exécute automatiquement `npm ci` puis `npm run build` (cible `ConstruireClientApp` du `.csproj`).
Les pièces jointes des signalements (`App_Data/uploads/`) ne sont jamais publiées : copiez-les à part si besoin.

## Cookies

| Cookie | Catégorie | Rôle | Durée |
|---|---|---|---|
| `camalert_consent` | Nécessaire | Choix de l'utilisateur sur les cookies (écrit par React) | 6 mois |
| `XSRF-TOKEN`, `camalert_csrf` | Nécessaire | Protection anti-CSRF des formulaires | Session |
| `camalert_admin` | Nécessaire | Connexion admin (HttpOnly, SameSite=Strict, chemin `/admin`) | Session, 8 h glissantes |
| `camalert_flash` | Nécessaire | Messages de confirmation de l'admin (chemin `/admin`) | Session |
| `camalert_prefs` | Préférences | Thème et derniers filtres, uniquement si accepté | 6 mois |
| `camalert_vid` | Mesure d'audience | Identifiant aléatoire (HttpOnly), uniquement si accepté | 13 mois |

Le serveur relit `camalert_consent` avant d'enregistrer une page vue : sans accord, rien n'est stocké et `camalert_vid` est supprimé. Aucune adresse IP n'est conservée (table `PagesVues`).

## Sécurité

- Connexion admin : PBKDF2 (compatible avec l'ancien site, migration automatique des anciens hachages SHA-256), blocage 5 échecs / 15 min, message d'erreur unique.
- Anti-CSRF sur tous les POST (formulaires Razor et API publique).
- Limitation à 5 envois / minute / IP sur les formulaires publics.
- En-têtes : CSP stricte (`script-src 'self'`, aucun script en ligne), `X-Frame-Options: DENY`, `nosniff`, `Referrer-Policy`, HSTS hors développement.
- Fichiers envoyés : taille (5 Mo), extension et signature vérifiées, nom généré ; pièces jointes stockées hors de `wwwroot` et téléchargeables uniquement par un administrateur.

## Arborescence

```
Areas/Admin/      Contrôleurs et vues Razor de l'administration
ClientApp/        Application React (Vite) + module admin (src/admin)
Controllers/Api/  API JSON publique
Data/             Accès SQL Server (Dapper)
Database/         Script SQL (table PagesVues)
Security/         Mots de passe, verrouillage, fichiers, en-têtes, consentement
Services/         Envoi d'e-mails
wwwroot/          Fichiers statiques (app/ = build Vite, Admin/image/ = anciennes photos)
```
