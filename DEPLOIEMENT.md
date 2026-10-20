# Déploiement de CAM-ALERT

Procédure pour mettre en ligne la version corrigée sur un **hébergement
mutualisé** (type MonsterASP.NET / `*.bsite.net`), c'est-à-dire sans accès à la
ligne de commande du serveur.

> **L'ordre compte.** La nouvelle version du code attend des colonnes et des
> tables qui n'existent pas encore dans la base en production. Déployer le code
> avant la base rend le site inutilisable. Suivez les étapes dans l'ordre.

---

## Avant de commencer

- [ ] Les anciens mots de passe d'application Gmail ont été **révoqués** sur
      https://myaccount.google.com/apppasswords (voir [SECURITY.md](SECURITY.md)).
- [ ] Vous avez une **sauvegarde de la base** de production.
      Sur la plupart des panneaux d'hébergement : *Databases → Backup*.
      L'étape 1 modifie le schéma ; sans sauvegarde, il n'y a pas de retour arrière.
- [ ] Vous avez les accès FTP et le panneau de l'hébergeur sous la main.

---

## Étape 1 — Mettre la base à niveau

À exécuter **avant** de déployer le code, sur la base de production.

Depuis SQL Server Management Studio, Azure Data Studio, ou la console SQL du
panneau de l'hébergeur :

```sql
-- Script : Database/04_Migration_Securite.sql
```

Il est **idempotent** : il ajoute ce qui manque et ne touche à aucune donnée
existante. Vous pouvez le relancer sans risque.

Ce qu'il fait :

| Objet | Effet |
|---|---|
| `Utilisateurs.MotDePasse` | élargi à `NVARCHAR(256)` — sans quoi les nouveaux hash seraient tronqués et **plus personne ne pourrait se connecter** |
| `Utilisateurs.DoitChangerMotDePasse` | ajoutée, à 1 pour les comptes jamais utilisés |
| `Utilisateurs.DateDerniereConnexion`, `NomComplet`, `Email` | ajoutées |
| `Signalements.AdresseIP`, `DateTraitement`, `Telephone` | ajoutées |
| Tables `Journal`, `MessagesContact` | créées |
| Index de performance | créés |
| `Geolocalisation` | doublons supprimés, unicité par dossier posée |

Puis exécutez **`Database/03_Procedures.sql`**, qui crée la vue de statistiques
et les procédures (journal, purge). La page Historique en dépend.

**Les comptes existants continuent de fonctionner.** Leurs mots de passe sont
encore en SHA-256 ; ils restent acceptés et sont réécrits automatiquement en
PBKDF2 à la première connexion réussie de chaque agent. Personne n'a à changer
son mot de passe.

Vérifiez le résultat :

```sql
SELECT Login,
       Format = CASE WHEN MotDePasse LIKE 'pbkdf2$sha256$%' THEN 'PBKDF2'
                     WHEN LEN(MotDePasse) = 64              THEN 'SHA-256 (migrera à la connexion)'
                     ELSE 'inconnu' END,
       DoitChangerMotDePasse
  FROM Utilisateurs;
```

---

## Étape 2 — Préparer `secrets.config`

1. Recopiez `secrets.config.example` sous le nom **`secrets.config`**.
2. Renseignez les vraies valeurs :
   - `ConnectionString_CamAlertDB` — celle de votre hébergeur, mot de passe SQL compris ;
   - `SMTP_User`, `SMTP_Password` — le **nouveau** mot de passe d'application à
     16 lettres, créé après la révocation ;
   - `SMTP_From`, `Contact_Destinataires`.
3. Ne laissez que les clés réellement renseignées : une clé vide écrase la
   valeur par défaut de `Web.config`.

Ce fichier est exclu de Git. Il ne doit exister que sur le serveur et sur votre
poste.

---

## Étape 3 — Publier le site

Depuis Visual Studio : clic droit sur le projet → **Publier**, profil Web Deploy
ou FTP de votre hébergeur, configuration **Release**.

La configuration Release applique `Web.Release.config` : `debug="false"`, pages
d'erreur neutres, cookies `requireSSL`, en-tête HSTS.

> **HSTS** indique au navigateur de refuser toute connexion en clair à ce
> domaine **pendant un an**. Ne publiez en Release que si le site est bien servi
> en HTTPS avec un certificat valide. Si ce n'est pas encore le cas, retirez la
> ligne `Strict-Transport-Security` de `Web.Release.config` avant de publier.

Puis déposez **`secrets.config` par FTP** à la racine du site, à côté de
`Web.config`. Il n'est pas publié par Visual Studio : c'est voulu.

---

## Étape 4 — Vérifier

Dans cet ordre :

1. **`GET /sante`** → doit renvoyer :

   ```json
   {"etat":"ok","baseDeDonnees":"joignable","smtp":"configure", ...}
   ```

   - `"baseDeDonnees":"injoignable"` → `ConnectionString_CamAlertDB` est faux,
     ou `secrets.config` n'est pas au bon endroit.
   - `"smtp":"non configure"` → `SMTP_Password` absent, ou c'est encore
     l'ancienne valeur, que l'application refuse volontairement.

2. **Page d'accueil** → la liste des dossiers s'affiche.

3. **`/Admin/Login.aspx`** → connectez-vous avec un compte existant. Si le
   compte n'avait jamais servi, le changement de mot de passe est imposé.

4. **Formulaire de contact** → envoyez un message de test, vérifiez qu'il arrive.
   Même si l'email échoue, le message est enregistré dans `MessagesContact` :
   rien n'est perdu.

5. **Journal** → `SELECT TOP 20 * FROM Journal ORDER BY DateAction DESC;`
   doit montrer votre connexion.

---

## Si le site renvoie une erreur après publication

### `500.19 — Configuration invalide`

L'hébergeur verrouille une section de `Web.config` que le nouveau fichier
utilise. Le message nomme la section fautive. Retirez-la de `Web.config`,
republiez, et notez laquelle : ce sont des renforcements, leur absence dégrade
la sécurité mais n'empêche pas le site de fonctionner.

Par ordre de probabilité :

| Section | Rôle | Conséquence si retirée |
|---|---|---|
| `<handlers>` (point `/sante`) | contrôle d'état | plus de `/sante` ; vérifiez à la main |
| `<security><requestFiltering>` | limite de taille, extensions servies dans `/Uploads` | un fichier exécutable déposé redeviendrait servi |
| `<httpProtocol><customHeaders>` | en-têtes de sécurité, CSP | protections navigateur perdues |
| `<directoryBrowse>` | listage des répertoires | à remplacer par le réglage équivalent du panneau |

### `500 — Erreur de serveur`

Consultez **`App_Data/Logs/journal-AAAA-MM-JJ.log`** par FTP : l'exception
complète s'y trouve, avec la référence d'incident affichée à l'écran.

Cause la plus fréquente à ce stade : l'étape 1 n'a pas été exécutée, et le code
interroge une colonne qui n'existe pas encore.

### Revenir en arrière

Republiez la version précédente depuis la branche `main`. La base, elle, reste
compatible : la migration n'a fait qu'**ajouter** des colonnes et des tables,
l'ancien code les ignore. Seul l'élargissement de `Utilisateurs.MotDePasse` est
structurel, et il est sans effet sur l'ancien code.

---

## Après la mise en ligne

- [ ] Planifiez `EXEC sp_PurgerDonneesPersonnelles;` une fois par semaine
      (panneau de l'hébergeur → tâches planifiées, ou à la main).
      `EXEC sp_PurgerDonneesPersonnelles @Simulation = 1;` montre ce qui serait
      purgé sans rien modifier.
- [ ] Changez le mot de passe du compte `admin` s'il utilise encore celui
      d'installation.
- [ ] Si votre hébergeur recycle souvent le pool d'applications, fixez une
      `machineKey` dans `Web.config` (voir le commentaire dans le fichier) :
      sans elle, un recyclage invalide le ViewState des formulaires ouverts.
