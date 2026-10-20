# Sécurité — CAM-ALERT

## Signaler une vulnérabilité

Écrivez au responsable du dépôt plutôt que d'ouvrir une *issue* publique.
Décrivez le chemin d'exploitation, la version concernée et l'impact constaté.
Merci de laisser un délai raisonnable avant toute divulgation.

---

## ⚠️ Secret à faire tourner immédiatement

Ce dépôt est **public** et son historique Git contient un mot de passe
d'application Gmail en clair. **Le retirer du code ne suffit pas** : il reste
lisible dans tous les commits antérieurs, dans les forks et dans les copies de
sauvegarde.

| Secret | Emplacement d'origine | Action |
|---|---|---|
| Mot de passe d'application Gmail de `aureledorian@gmail.com` | `Contact.aspx.cs`, appel `new NetworkCredential(...)` (commit « Tous les éléments du projet ») | **Révoquer** sur https://myaccount.google.com/apppasswords, puis en générer un nouveau |

Le même compte Gmail apparaît aussi, avec un **autre** mot de passe
d'application, dans le dépôt public `SOCADEL.DUERP.Web` (`Web.config`).
Les deux doivent être révoqués.

Les quatre adresses email des membres du groupe figuraient également en clair
dans `Contact.aspx.cs`. Elles sont désormais lues depuis la configuration
(`Contact_Destinataires`), mais restent présentes dans l'historique : prévenez
les personnes concernées qu'elles peuvent recevoir du courrier indésirable.

### Procédure

1. Ouvrir https://myaccount.google.com/apppasswords et **supprimer** le mot de
   passe d'application concerné. Toute copie publiée devient alors inutilisable.
2. Générer un nouveau mot de passe d'application, dédié à cette application.
3. Le déposer **uniquement** dans une variable d'environnement du serveur :

   ```powershell
   setx CAMALERT_SMTP_PASSWORD "<nouveau mot de passe>" /M
   setx CAMALERT_SMTP_USER     "contact@exemple.cm"     /M
   ```

   Puis redémarrer le pool d'applications IIS.
4. Vérifier que `Web.config` contient bien l'espace réservé
   `A_DEFINIR_VARIABLE_ENVIRONNEMENT` et aucune valeur réelle.
5. Contrôler l'effet : `GET /sante` doit renvoyer `"smtp":"configure"`.

`Helpers/Parametres.cs` connaît les valeurs divulguées et **refuse** de les
utiliser : si l'ancienne reste configurée, l'envoi échoue avec un message
explicite au lieu de s'exécuter avec un secret public.

### Faut-il réécrire l'historique Git ?

Une réécriture (`git filter-repo`, BFG) retire la valeur des commits, mais :

- elle ne touche pas les forks, les caches de GitHub ni les clones existants ;
- elle change tous les identifiants de commit, ce qui casse les copies locales.

La révocation est donc la mesure qui compte, et elle suffit.

---

## Contrôles en place

### Mots de passe

- **PBKDF2-HMAC-SHA256**, sel aléatoire de 16 octets par compte,
  210 000 itérations (`Helpers/HacheurMotDePasse.cs`). Format stocké :
  `pbkdf2$sha256$<iterations>$<sel b64>$<cle b64>`.
- Les anciens hash **SHA-256 non salés** (64 caractères hexadécimaux, que
  produisait `Admin/Login.aspx.cs`) restent vérifiables et sont **réécrits
  automatiquement** au format courant à la première connexion réussie.
  Un SHA-256 sans sel se casse par table précalculée, et deux comptes
  partageant le même mot de passe portaient le même hash.
- Comparaison des hash **à temps constant**.
- **Politique de robustesse côté serveur** (`Helpers/PolitiqueMotDePasse.cs`) :
  12 caractères minimum, trois familles de caractères sur quatre (ou 20
  caractères pour une phrase de passe), refus des mots de passe courants, des
  suites, des répétitions, des rangées de clavier, et de tout mot de passe
  contenant l'identifiant du compte.
- Le mot de passe initial `CamAlert@2026` est sur liste noire et ne peut pas
  être rechoisi. `DoitChangerMotDePasse` impose le changement dès la première
  connexion : tant qu'il n'est pas fait, aucune page d'administration n'est
  accessible.
- **`Admin/MotDePasse.aspx`** : cet écran n'existait pas. Un agent ne pouvait
  pas changer son mot de passe, et le compte livré avec la base gardait
  indéfiniment celui de l'installation.

### Authentification et session

- Le contrôle d'accès était recopié dans chaque page ; une page qui oubliait
  le bloc `if (Session["UserLogin"] == null)` était publiquement accessible.
  Il est désormais fait une fois, dans `PageAdmin.OnInit`, donc avant tout
  traitement et avant le déclenchement du moindre événement de contrôle.
- **Anti-fixation de session** (`Helpers/JetonSession.cs`) : un jeton
  aléatoire de 256 bits est tiré à la connexion, stocké en session et déposé
  dans un cookie `HttpOnly`, `SameSite=Strict`, `Secure` en HTTPS. Chaque page
  protégée vérifie la correspondance. Un identifiant de session imposé à la
  victime devient inexploitable.
- **Limitation des tentatives** (`Helpers/LimiteurTentatives.cs`) : la
  connexion acceptait un nombre illimité d'essais. Deux compteurs : par couple
  (identifiant, IP) et par IP seule, pour contrer aussi le balayage de
  plusieurs identifiants. Les échecs sont journalisés, et
  `sp_CompterEchecsConnexion` permet de les interroger même après un recyclage
  du pool d'applications.
- **Pas de divulgation** : identifiant inconnu, mot de passe faux et compte
  désactivé produisent le même message et le même traitement.
- Le message d'erreur affichait auparavant `ex.Message`, donc la chaîne de
  connexion ou la requête SQL en cas de panne de la base.

### Requêtes et affichage

- **Clé ViewState liée à la session** (`PageBase`) : ASP.NET signe le
  ViewState, mais sans `ViewStateUserKey` cette signature n'est rattachée à
  aucun utilisateur, et un ViewState d'administration pouvait être rejoué dans
  le navigateur d'un agent connecté.
- **Encodage systématique** des valeurs issues de la base et des formulaires.
  Les fiches de dossier et l'écran de traitement des signalements écrivaient
  le contenu brut dans des contrôles `Literal`, qui le rendent tel quel : un
  signalement contenant des balises était interprété par le navigateur de
  l'agent — déclenchable par n'importe quel visiteur, puisque le formulaire de
  signalement est public.
- **Requêtes paramétrées** partout, avec types et longueurs déclarés.
- **Jokers de LIKE neutralisés** (`Helpers/RechercheSql.cs`) : une recherche
  sur « % » renvoyait toute la table, et un terme très long provoquait une
  analyse complète à chaque frappe.
- **Redirection de retour validée** (`Helpers/UrlLocale.cs`) : le paramètre
  `retour` de la page de connexion ne peut désigner qu'une page du site.
- **En-têtes de sécurité** dans `Web.config` : `X-Content-Type-Options`,
  `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`,
  `Content-Security-Policy` (liste blanche d'origines), HSTS en Release.
- Cookies `HttpOnly` partout, `Secure` dès que la requête est en HTTPS,
  `SameSite=Lax`, `requireSSL` en Release. Session par cookie uniquement.

### Fichiers déposés

`Helpers/FichierSecurise.cs` remplace le traitement précédent, qui écrivait le
fichier sous le nom fourni par le navigateur après un simple test
d'extension :

- nom de fichier **entièrement régénéré** (horodatage + GUID) : aucune chaîne
  venant du client n'atteint le système de fichiers, donc plus de `..\` ;
- taille bornée (5 Mo pour une photo de dossier, 8 Mo pour une pièce jointe) ;
- **signature du format vérifiée** sur les premiers octets : un script renommé
  en `.jpg` est refusé, alors que le contrôle d'extension seul le laissait
  passer ;
- un fichier refusé est **signalé** au visiteur ; il était auparavant ignoré
  en silence, et le témoin repartait en croyant sa photo transmise ;
- `Web.config` restreint `/Uploads` et `/Admin/image` aux extensions d'images
  et de PDF : un fichier exécutable qui y serait déposé renvoie 404.

### Vie privée

- La page publique **Historique** affichait le nom du déclarant et le contenu
  des signalements traités. Un témoin pouvait être identifié par son nom comme
  par ce qu'il racontait. Seuls le dossier concerné, le statut et les dates
  sont désormais publiés.
- `sp_PurgerDonneesPersonnelles` applique une durée de conservation aux
  adresses IP et à l'identité des déclarants des signalements clos. À
  planifier une fois par semaine dans l'Agent SQL Server.
- `robots.txt` exclut `/Admin/`, `/Uploads/` et les formulaires de
  l'indexation.

### Erreurs et journalisation

- Les exceptions non rattrapées ne laissaient **aucune trace** : les
  `Debug.WriteLine` du code ne produisent rien en configuration Release, et
  aucun journal applicatif n'existait. `Application_Error` enregistre
  désormais l'exception complète et renvoie vers une page neutre portant une
  référence d'incident, à la place de l'écran jaune ASP.NET qui affichait le
  chemin des sources, la requête SQL et le nom du serveur.
- Les actions d'administration sont tracées dans la table `Journal` :
  qui a supprimé quel dossier, quand, depuis quelle adresse.

---

## Points connus restant à traiter

| Sujet | État | Piste |
|---|---|---|
| `'unsafe-inline'` dans la CSP | Nécessaire en l'état | Extraire les styles et gestionnaires d'événements en ligne des pages vers des fichiers `.css`/`.js`, puis retirer `'unsafe-inline'` |
| `machineKey` non fixée | À faire avant un déploiement en ferme | Générer une clé sur le serveur et la déclarer dans `Web.config` ; sinon un recyclage du pool invalide le ViewState des formulaires ouverts |
| Limiteur de tentatives en mémoire | Suffisant pour un serveur IIS unique | Pour plusieurs serveurs, s'appuyer sur la table `Journal` (`sp_CompterEchecsConnexion`) ou un cache distribué |
| Fichiers orphelins | Non traité | La suppression d'un dossier laisse sa photo sur le disque ; prévoir un nettoyage périodique |
| Captcha sur les formulaires publics | Absent | La limitation par IP freine les abus, mais pas un envoi distribué |
| Chiffrement au repos | Non traité | Activer TDE sur la base si des signalements nominatifs y sont conservés |
