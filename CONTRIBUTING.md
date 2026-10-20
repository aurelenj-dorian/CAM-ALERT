# Contribuer à CAM-ALERT

## Avant de commencer

1. Lire [README.md](README.md) pour la mise en route et la structure,
   puis [SECURITY.md](SECURITY.md) pour les contraintes de sécurité.
2. Restaurer les paquets et créer la base (voir le README).

## Conventions

- **Le code est en français** : noms de classes, de méthodes, de variables,
  commentaires et libellés d'interface. C'est la convention du projet.
- Une page publique hérite de `PageBase`. Une page protégée hérite de
  `PageAdmin` et redéfinit `RolesAutorises` si elle doit restreindre l'accès.
  **N'écrivez pas de contrôle d'accès à la main dans un `Page_Load`** : il
  s'exécute trop tard et sera oublié tôt ou tard.
- La logique sans interface va dans `Helpers/`, sans dépendance à
  `HttpContext` quand c'est possible — c'est ce qui la rend testable.
- Les commentaires expliquent *pourquoi*, pas *quoi*.
- 4 espaces d'indentation, fichiers en UTF-8 avec BOM (voir `.editorconfig`).

## Règles de sécurité non négociables

- **Aucun secret dans le dépôt.** Pas de mot de passe, de clé, de jeton ni
  d'adresse email personnelle dans `Web.config` ou dans le code. Passez par
  `Helpers/Parametres.cs` et une variable d'environnement.
- Toute requête SQL est **paramétrée**, avec type et longueur déclarés.
  Jamais de concaténation d'une valeur venant de l'utilisateur.
- Toute valeur issue de la base ou d'un formulaire et rendue dans un
  `Literal`, un `Label` ou un `Eval` est **encodée** (`Server.HtmlEncode`).
  Les contrôles `Literal` rendent leur contenu tel quel.
- Un mot de passe est haché avec `HacheurMotDePasse`, jamais avec `SHA256`
  directement.
- Un fichier déposé passe par `FichierSecurise` : jamais de `SaveAs` avec un
  nom construit à partir de `FileName`.
- Un formulaire public est limité par `LimiteurTentatives`.
- Une redirection vers une URL venant du client passe par `UrlLocale`.
- Une action d'administration est journalisée (`Journal.Enregistrer`).

## Ajouter un fichier

Le projet utilise un `.csproj` classique avec des listes de fichiers
explicites, **sans globbing**. Un fichier ajouté doit être déclaré :

- une classe → `<Compile Include="…" />`
- une page, une image, un script → `<Content Include="…" />`
- un script SQL, un fichier de configuration → `<None Include="…" />`

Une page `.aspx` nécessite aussi son `.aspx.cs` et son `.aspx.designer.cs`,
déclarés avec `<DependentUpon>`.

## Modifier le modèle de données

1. Mettre à jour `Database/01_Schema.sql` (création à zéro).
2. Ajouter les lignes correspondantes dans `Database/02_DonneesReference.sql`
   si des données de référence sont concernées.
3. Compléter `Database/04_Migration_Securite.sql`, ou créer un nouveau script
   de migration **idempotent**, pour les bases déjà en service.

## Tests

Ajoutez un test pour toute logique sans base : validation, format, calcul,
décision de sécurité.

```powershell
msbuild CamAlert.Tests\CamAlert.Tests.csproj /p:Configuration=Debug
vstest.console.exe CamAlert.Tests\bin\Debug\CamAlert.Tests.dll
```

N'ajoutez pas de test qui exige un serveur SQL : la chaîne d'intégration
continue compile et exécute la suite sans base.

## Avant de pousser

- [ ] La solution compile en `Debug` **et** en `Release`.
- [ ] La suite de tests passe.
- [ ] Aucun secret ni adresse personnelle ajouté (`git diff` relu ligne à ligne).
- [ ] Les nouveaux fichiers sont déclarés dans `CamAlert.csproj`.
- [ ] Les scripts SQL suivent la modification du modèle, le cas échéant.
- [ ] Le message de commit décrit l'intention, en français, à l'impératif.
