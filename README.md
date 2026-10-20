# CAM-ALERT

Portail camerounais fictif de recherche des **personnes disparues et
recherchées**, inspiré du site d'INTERPOL. Projet pédagogique réalisé en
groupe : les dossiers, les noms et les signalements sont inventés et ne
désignent aucune personne réelle.

Le public consulte les dossiers publiés, les localise sur une carte et
transmet une information de façon anonyme. Les agents habilités gèrent les
dossiers et traitent les signalements reçus.

---

## Sommaire

- [Pile technique](#pile-technique)
- [Démarrage rapide](#démarrage-rapide)
- [Configuration et secrets](#configuration-et-secrets)
- [Base de données](#base-de-données)
- [Structure du projet](#structure-du-projet)
- [Authentification](#authentification)
- [Fonctionnement hors ligne](#fonctionnement-hors-ligne)
- [Tests](#tests)
- [Exploitation](#exploitation)
- [Déploiement](#déploiement)
- [Sécurité](#sécurité)

---

## Pile technique

| Élément | Version |
|---|---|
| .NET Framework | 4.8 |
| ASP.NET Web Forms | — |
| SQL Server | 2017 ou supérieur |
| Leaflet + MarkerCluster | 1.9 / 1.5 (CDN) |
| Tests | MSTest |

Accès aux données en ADO.NET direct (`Helpers/Database.cs`), avec des
requêtes **paramétrées**. Pas d'ORM.

---

## Démarrage rapide

Prérequis : Visual Studio 2022, SQL Server (instance locale ou accessible),
IIS Express.

```powershell
# 1. Restaurer les paquets
nuget restore packages.config -PackagesDirectory packages
nuget restore CamAlert.Tests\packages.config -PackagesDirectory packages

# 2. Créer et peupler la base
sqlcmd -S . -i Database\01_Schema.sql
sqlcmd -S . -i Database\02_DonneesReference.sql
sqlcmd -S . -i Database\03_Procedures.sql

# 3. Compiler (construit aussi le projet web par référence)
msbuild CamAlert.Tests\CamAlert.Tests.csproj /p:Configuration=Debug

# 4. Lancer : ouvrir CamAlert.slnx dans Visual Studio 2022 et démarrer
```

Il n'y a **pas** de `dotnet build` : le projet cible le .NET Framework
classique et se compile avec MSBuild.

**Première connexion.** `admin` / `CamAlert@2026`, sur `/Admin/Login.aspx`.
Le changement de mot de passe est imposé immédiatement, et la politique de
robustesse refuse de rechoisir `CamAlert@2026`.

Vérifier que le site tourne :

```
GET /sante   →   {"etat":"ok","baseDeDonnees":"joignable","smtp":"configure",...}
```

---

## Configuration et secrets

**Aucun secret ne doit être écrit dans `Web.config`** : ce fichier est
versionné dans un dépôt public. Les valeurs sensibles sont lues par
`Helpers/Parametres.cs`, qui consulte d'abord les variables d'environnement
du serveur, puis `appSettings`.

Deux moyens, selon ce à quoi vous avez accès sur le serveur.

### Serveur dont vous maîtrisez l'environnement

```powershell
# En administrateur (/M = portée machine)
setx CAMALERT_SMTP_USER             "contact@exemple.cm"                 /M
setx CAMALERT_SMTP_PASSWORD         "<mot de passe d'application>"       /M
setx CAMALERT_SMTP_FROM             "contact@exemple.cm"                 /M
setx CAMALERT_CONTACT_DESTINATAIRES "agent1@exemple.cm;agent2@exemple.cm" /M
setx CAMALERT_CONNECTIONSTRING_CamAlertDB "Server=.;Database=CamAlert;Integrated Security=True;" /M
# Puis redémarrer le pool d'applications IIS pour qu'il relise l'environnement.
```

C'est la méthode à privilégier : aucun secret ne touche le disque du site.

### Hébergement mutualisé (pas de ligne de commande)

Recopiez `secrets.config.example` en **`secrets.config`**, renseignez les vraies
valeurs, et déposez-le par FTP à la racine du site, à côté de `Web.config`.

`Web.config` déclare `<appSettings file="secrets.config">` : les clés du fichier
externe écrasent celles du fichier versionné. L'attribut `file=` est tolérant —
si `secrets.config` est absent, ASP.NET l'ignore sans erreur, et le poste de
développement fonctionne sans configuration supplémentaire.

`secrets.config` est exclu de Git, et l'intégration continue échoue s'il y est
ajouté par mégarde.

La chaîne de connexion peut y figurer sous la clé `ConnectionString_CamAlertDB` :
en production elle contient le mot de passe du compte SQL, elle n'a donc pas sa
place dans `Web.config`.

La procédure complète de mise en ligne est dans **[DEPLOIEMENT.md](DEPLOIEMENT.md)**.

| Clé | Rôle | Défaut |
|---|---|---|
| `SMTP_Host` / `SMTP_Port` / `SMTP_SSL` | Serveur d'envoi | `smtp.gmail.com` / 587 / true |
| `SMTP_User` / `SMTP_Password` | Identifiants d'envoi | — (obligatoire) |
| `SMTP_From` / `SMTP_NomExp` | Expéditeur affiché | `SMTP_User` / `CAM-ALERT` |
| `Contact_Destinataires` | Destinataires du formulaire, séparés par `;` | — (obligatoire) |

`Parametres.LireObligatoire` **refuse** le mot de passe d'application qui a
été publié avec ce dépôt : si cette valeur est encore configurée, l'envoi
échoue avec un message explicite au lieu d'utiliser un secret public.
Voir [SECURITY.md](SECURITY.md).

---

## Base de données

Les scripts se trouvent dans [`Database/`](Database/README.md) :
`01_Schema.sql`, `02_DonneesReference.sql`, `03_Procedures.sql`, plus
`04_Migration_Securite.sql` pour mettre à niveau une base existante.

Le schéma a été **reconstitué à partir du code** : le dépôt n'en contenait
aucun, et l'application était donc impossible à installer ailleurs que sur le
poste d'origine. Voir le README du dossier pour le détail de ce qui a été
ajouté (contraintes, index, journal d'audit, purge des données personnelles).

---

## Structure du projet

| Chemin | Rôle |
|---|---|
| `*.aspx` (racine) | Pages publiques : accueil, liste et fiche de dossier, signalement, contact, historique, à propos, carte |
| `Admin/` | Espace d'administration : connexion, dossiers, signalements, changement de mot de passe |
| `PageBase.cs` | Base de toutes les pages : clé ViewState liée à la session (anti-CSRF), accès à la session et à l'IP client |
| `PageAdmin.cs` | Base des pages protégées : authentification, rôle, jeton anti-fixation, changement de mot de passe imposé |
| `Helpers/` | Briques transverses, toutes sans interface : accès aux données, hachage, politique de mots de passe, limitation de tentatives, contrôle des fichiers déposés, journal, configuration |
| `Handlers/` | `SanteHandler` — point de contrôle d'état `/sante` |
| `Database/` | Scripts SQL (schéma, données, procédures, migration) |
| `Content/` | Feuilles de style et images |
| `CamAlert.Tests/` | Tests MSTest des briques sans base |

Les classes `Helpers` sont volontairement sans dépendance à `HttpContext`
quand c'est possible : elles sont ainsi testables sans serveur.

---

## Authentification

Authentification **maison**, par session — ce n'est pas ASP.NET Identity.

- Connexion par **identifiant + mot de passe** sur `/Admin/Login.aspx`.
  Les mots de passe sont hachés en **PBKDF2-HMAC-SHA256**, sel aléatoire par
  compte, 210 000 itérations (`Helpers/HacheurMotDePasse.cs`). Les anciens
  hash SHA-256 non salés restent acceptés et sont réécrits automatiquement à
  la première connexion réussie.
- Une page protégée hérite de `PageAdmin`. Elle restreint les rôles en
  redéfinissant `RolesAutorises` :

```csharp
public partial class Carte : PageAdmin
{
    protected override string[] RolesAutorises
    {
        get { return new[] { "Administrateur" }; }
    }
}
```

- Deux rôles seedés : **`Administrateur`** (tout, y compris la suppression de
  dossiers) et **`Agent`** (traitement des signalements). Les libellés sont
  comparés littéralement dans le code : ne pas les renommer sans adapter la
  base.
- Une requête non authentifiée est redirigée vers `Admin/Login.aspx`, avec
  l'adresse demandée en paramètre `retour` — validée pour qu'elle désigne bien
  une page du site.

---

## Fonctionnement hors ligne

Le portail est consulté depuis le Cameroun, souvent sur réseau mobile et avec
une connexion intermittente. Un *service worker* (`sw.js`) met en cache la
feuille de style et les pages déjà visitées, et affiche `hors-ligne.html`
plutôt que l'erreur du navigateur quand la connexion est coupée.

Les pages sont servies **réseau d'abord** : un avis de recherche peut être
clos ou retiré, et afficher une version périmée serait pire qu'un message
d'indisponibilité. Les ressources statiques, elles, sont servies depuis le
cache puis rafraîchies en arrière-plan.

Ne passent jamais par le cache : `/Admin/*`, `/Uploads/*`, `/sante`, et toute
requête autre que `GET`.

Le site déclare aussi un manifeste (`manifest.json`) : il peut être installé
sur l'écran d'accueil d'un téléphone et s'ouvrir en plein écran.

---

## Tests

```powershell
msbuild CamAlert.Tests\CamAlert.Tests.csproj /p:Configuration=Debug
vstest.console.exe CamAlert.Tests\bin\Debug\CamAlert.Tests.dll
```

Les tests couvrent les briques sans base : hachage et migration des mots de
passe, politique de robustesse, limitation de tentatives, validation des URL
de retour, nettoyage des termes de recherche, contrôle du contenu des
fichiers déposés. N'ajoutez pas de test qui exige un serveur SQL : la chaîne
d'intégration continue compile et exécute la suite sans base.

---

## Exploitation

- **État de santé** : `GET /sante` renvoie un JSON (`ok`, `degrade`,
  `indisponible`) et le code HTTP 200 ou 503. À brancher sur la supervision.
- **Journal d'audit** : connexions, échecs, créations, modifications,
  suppressions et signalements sont écrits dans la table `Journal` via
  `sp_JournaliserAction` (`Helpers/Journal.cs`). Si la base est injoignable,
  l'entrée part dans `App_Data/Logs/`.
- **Journal des erreurs** : les exceptions non rattrapées sont enregistrées
  avec une référence courte, affichée au visiteur sur la page d'erreur.
- **Durée de conservation** : planifier
  `EXEC sp_PurgerDonneesPersonnelles;` une fois par semaine dans l'Agent SQL
  Server. La procédure efface les adresses IP et l'identité des déclarants des
  signalements clos. `@Simulation = 1` montre ce qui serait purgé.
- **Publication** : la configuration `Release` applique `Web.Release.config`
  — `debug="false"`, `customErrors mode="On"`, cookies `requireSSL`, en-tête
  HSTS. N'activez HSTS qu'une fois le certificat en place.

---

## Déploiement

La mise en ligne sur un hébergement mutualisé est décrite pas à pas dans
**[DEPLOIEMENT.md](DEPLOIEMENT.md)** : migration de la base **avant** le code,
dépôt de `secrets.config`, publication en Release, vérifications, et triage des
erreurs `500.19` dues aux sections de configuration verrouillées par
l'hébergeur.

L'ordre compte : la nouvelle version attend des colonnes qui n'existent pas
encore dans une base antérieure.

## Sécurité

Voir **[SECURITY.md](SECURITY.md)** pour la procédure de signalement, les
secrets à faire tourner et le détail des contrôles.

En résumé : mots de passe PBKDF2 salés, politique de robustesse côté serveur,
limitation des tentatives sur tous les formulaires publics, jeton
anti-fixation de session, clé ViewState liée à la session, encodage
systématique des données affichées, contrôle du contenu des fichiers déposés,
en-têtes de sécurité et CSP, secrets hors du dépôt.
