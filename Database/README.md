# Base de données CAM-ALERT

La base `CamAlert` est créée et peuplée par les scripts de ce dossier.
Elle n'est pas gérée par un outil de migration : les scripts sont la
référence, et toute modification du schéma doit y être reportée.

## Installation

Exécuter dans cet ordre, sur une instance SQL Server 2017 ou supérieure :

```powershell
sqlcmd -S . -i Database\01_Schema.sql
sqlcmd -S . -i Database\02_DonneesReference.sql
sqlcmd -S . -i Database\03_Procedures.sql
```

Sur une base déjà en service, créée à la main avant l'existence de ces
scripts, exécuter à la place :

```powershell
sqlcmd -S . -i Database\04_Migration_Securite.sql
```

Ce script de migration est **idempotent** : il ajoute ce qui manque
(colonnes, index, journal, procédures) sans toucher aux données.

| Script | Contenu |
|---|---|
| `01_Schema.sql` | Tables, contraintes, index. Recrée la base à zéro. |
| `02_DonneesReference.sql` | 10 régions, types, statuts, rôles, compte initial, jeu de dossiers de démonstration. |
| `03_Procedures.sql` | Vue de statistiques, journalisation, purge des données personnelles. |
| `04_Migration_Securite.sql` | Mise à niveau d'une base existante. |

## Connexion initiale

| Login | Mot de passe | Rôle |
|---|---|---|
| `admin` | `CamAlert@2026` | Administrateur |

Le changement est **imposé à la première connexion** : l'application
redirige vers `Admin/MotDePasse.aspx` et n'autorise aucune autre page
tant que le mot de passe initial n'a pas été remplacé. La politique de
robustesse refuse de rechoisir `CamAlert@2026`.

Ce mot de passe figure dans un dépôt public : il ne vaut que pour une
installation neuve, et doit être changé avant toute mise en ligne.

## Chaîne de connexion

Elle est lue par `Helpers/Parametres.cs`, qui donne la priorité à la
variable d'environnement du serveur sur `Web.config` :

```powershell
setx CAMALERT_CONNECTIONSTRING_CamAlertDB "Server=.;Database=CamAlert;Integrated Security=True;" /M
```

À défaut, la valeur de `Web.config` (`Data Source=AURELE`) est utilisée —
elle désigne un poste de développement précis et doit être adaptée.

## Pourquoi ces scripts n'existaient pas

Le dépôt contenait le code d'accès aux données mais aucune définition de
schéma. Les requêtes faisaient référence à `Dossiers`, `Regions`,
`TypesDossier`, `Statuts`, `Signalements`, `Geolocalisation`,
`Utilisateurs` et `Roles` sans que personne puisse savoir comment ces
tables étaient faites : impossible d'installer l'application ailleurs que
sur le poste d'origine. Le schéma a été reconstitué à partir de
l'ensemble des requêtes du code, puis complété de ce qui manquait.

## Ce que le schéma ajoute par rapport aux requêtes d'origine

- **Clés étrangères et contraintes de validité.** `Geolocalisation`
  vérifie que les coordonnées tombent dans le cadre du Cameroun : une
  inversion latitude/longitude plaçait un marqueur dans l'Atlantique sans
  que rien ne le signale. `Dossiers` contraint `StatutDossier` aux trois
  valeurs que le code reconnaît, et exige une date de résolution pour un
  dossier résolu.
- **Unicité de la géolocalisation par dossier.** La requête de la carte
  fait une jointure interne : deux lignes pour un même dossier
  l'affichaient deux fois.
- **Index.** Toutes les listes publiques trient par `DatePublication`
  décroissante en filtrant sur `EstPublie`. Sans index, chaque affichage
  provoquait une analyse complète de la table.
- **Table `Journal`.** Aucune trace n'était conservée : impossible de
  savoir qui avait supprimé un dossier, ni de repérer une série de
  tentatives de connexion.
- **Table `MessagesContact`.** Les messages du formulaire de contact
  étaient uniquement envoyés par email. Si le serveur SMTP était
  indisponible, le message était perdu sans que personne ne le sache.
- **Colonnes de comptes.** `DoitChangerMotDePasse`, `DateDerniereConnexion`,
  `NomComplet` et `Email`, pour imposer le changement du mot de passe
  initial et suivre l'usage des comptes.
- **Purge des données personnelles.** `sp_PurgerDonneesPersonnelles`
  applique une durée de conservation aux adresses IP et à l'identité des
  déclarants. Un témoin qui signale une disparition ne doit pas rester
  identifiable indéfiniment dans la base.

## Maintenance

```sql
-- Voir ce qui serait purgé, sans rien modifier
EXEC sp_PurgerDonneesPersonnelles @Simulation = 1;

-- Appliquer la purge (à planifier une fois par semaine dans l'Agent SQL)
EXEC sp_PurgerDonneesPersonnelles;

-- Échecs de connexion des 15 dernières minutes pour une IP
EXEC sp_CompterEchecsConnexion @AdresseIP = '192.0.2.10';

-- Suivre la bascule des mots de passe vers PBKDF2
SELECT Login,
       Format = CASE WHEN MotDePasse LIKE 'pbkdf2$sha256$%' THEN 'PBKDF2'
                     WHEN LEN(MotDePasse) = 64              THEN 'SHA-256 (à migrer)'
                     ELSE 'inconnu' END
  FROM Utilisateurs;
```
