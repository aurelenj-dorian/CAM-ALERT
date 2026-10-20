# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

CAM-ALERT — an ASP.NET **Web Forms** site: a fictional Cameroonian portal for missing and wanted persons, modelled on INTERPOL's. It is a group student project; every case, name and report in it is invented. The public browses published cases, sees them on a map and submits tips anonymously; authorised agents manage cases and process tips.

The codebase is written in **French** (class names, methods, variables, comments, UI strings) — match that convention when adding code.

- **Framework:** .NET Framework 4.8, ASP.NET Web Forms (`.aspx` + code-behind + `.designer.cs`)
- **Solution:** `CamAlert.slnx` (new-format solution) with `CamAlert.csproj` (web, `OutputType=Library`) and `CamAlert.Tests/CamAlert.Tests.csproj` (MSTest)
- **Data access:** plain ADO.NET through `Helpers/Database.cs`, always **parameterised**. No ORM.
- **Packages:** classic `packages.config` + `packages/` folder, **not** PackageReference.

See `README.md` for setup, `SECURITY.md` for the security model and the secret that must be rotated, `CONTRIBUTING.md` for conventions, `Database/README.md` for the schema.

## Build, run, test

No `dotnet build` — this is classic .NET Framework. Use MSBuild / Visual Studio 2022.

```powershell
# Restore (two packages.config into one shared folder)
nuget restore packages.config -PackagesDirectory packages
nuget restore CamAlert.Tests\packages.config -PackagesDirectory packages

# Build — building the test project builds the web project by reference
msbuild CamAlert.Tests\CamAlert.Tests.csproj /p:Configuration=Debug

# Test
vstest.console.exe CamAlert.Tests\bin\Debug\CamAlert.Tests.dll
```

CI (`.github/workflows/ci.yml`) builds Debug + Release, runs the suite, and scans for secrets. It never has a SQL Server, so **don't add tests that need a database** — the test project covers the DB-free helpers only.

`GET /sante` returns a JSON health report (DB reachable, SMTP configured, version) with HTTP 200 or 503.

## Database

Scripts live in `Database/`, run in order: `01_Schema.sql`, `02_DonneesReference.sql`, `03_Procedures.sql`. For a database created by hand before these scripts existed, run the idempotent `04_Migration_Securite.sql` instead of 01/02.

The schema was **reconstructed from the queries in the code** — the repo originally shipped none, so the app could not be installed anywhere but its author's machine. If you change a query's shape, update `01_Schema.sql` **and** add to `04_Migration_Securite.sql`.

Seeded login: `admin` / `CamAlert@2026`. `DoitChangerMotDePasse` forces a change on first login; `PolitiqueMotDePasse` blacklists that value so it can't be re-chosen.

Values the C# code compares literally — renaming them in the database breaks the app:
- `TypesDossier.Libelle = 'Disparition'` (`DossierDetail`, `Carte`)
- `Dossiers.StatutDossier` ∈ `'En cours' | 'Résolu' | 'Classé'` (`Historique`, `Admin/Dossiers`)
- `Statuts.Libelle = 'Traité'` (`Historique`)
- `Roles.Libelle = 'Administrateur'` (`Site.Master`, `PageAdmin`, `Carte`)

Connection string `CamAlertDB` lives in `Web.config`. **Secrets never go in `Web.config`** — this repo is public. `Helpers/Parametres.cs` reads `CAMALERT_*` environment variables first (`CAMALERT_SMTP_PASSWORD`, `CAMALERT_CONNECTIONSTRING_CamAlertDB`, …) and falls back to `appSettings`. It also refuses the Gmail app password that was committed to this repo's history.

## Architecture

```
Page (.aspx) → PageBase / PageAdmin → Helpers → Database.cs → SQL Server
```

- **`PageBase`** — base of every page. Sets `ViewStateUserKey` from the session id in `OnInit` (CSRF protection for the ViewState), anchors the session so that id is stable, and exposes `IpClient`, `LoginConnecte`, `RoleConnecte`, `IdConnecte`, `EstAdministrateur`, `Deconnecter()`.
- **`PageAdmin : PageBase`** — base of every protected page. In `OnInit` it checks authentication, the anti-fixation session token, the role (`RolesAutorises`, override to restrict), and the forced initial password change (`ExempteDeChangementMotDePasse` to opt out). **Never hand-roll an access check in `Page_Load`** — it runs too late and will eventually be forgotten; that is exactly how the old code worked.
- **`Helpers/`** — DB-free where possible, which is what makes them unit-testable: `HacheurMotDePasse` (PBKDF2 + legacy SHA-256), `PolitiqueMotDePasse`, `LimiteurTentatives`, `JetonSession`, `FichierSecurise`, `UrlLocale`, `RechercheSql`, `Parametres`, `Journal`, `Database`.
- **`Handlers/SanteHandler`** — `/sante`, registered in `Web.config`.

The `.csproj` uses **explicit file lists, no globbing**. A new file must be declared: `<Compile>` for a class, `<Content>` for a page/asset, `<None>` for a SQL script. A new `.aspx` also needs its `.aspx.cs` and `.aspx.designer.cs` with `<DependentUpon>`.

### Authentication

Hand-rolled, session-based — not ASP.NET Identity.

- Login by **identifier + password** on `/Admin/Login.aspx`. Passwords are hashed with **PBKDF2-HMAC-SHA256** (16-byte random salt per account, 210 000 iterations; stored as `pbkdf2$sha256$<iters>$<salt b64>$<key b64>`). Legacy unsalted SHA-256 hashes (64 hex chars, uppercase — what the old `Login.aspx.cs` produced) still verify and are **transparently rewritten** on the next successful login. Never hash a new password with `SHA256` directly.
- Session keys are the `SESSION_*` constants on `PageBase`, not string literals.
- `JetonSession` adds a random token in session plus an `HttpOnly`/`SameSite=Strict` cookie, checked by `PageAdmin` — a session id forced onto a victim's browser is unusable without it.
- Login, tips and contact messages are rate-limited by `LimiteurTentatives` (keyed on identifier **and** client IP, so nobody can lock out someone else's account).
- The `retour` parameter on the login page goes through `UrlLocale` before any redirect.

### Non-negotiable rules when editing

- Every SQL query is parameterised, with `SqlDbType` and length declared.
- Every value from the database or a form rendered into a `Literal`, `Label` or `Eval` is `Server.HtmlEncode`d — `Literal` renders its content raw, and the tip form is public.
- Every uploaded file goes through `FichierSecurise` (regenerated filename, size cap, magic-byte check). Never `SaveAs` with a name built from `FileName`.
- Every admin action is logged with `Journal.Enregistrer`.
- No secret, key or personal email address in the repo.

## Offline behaviour

`sw.js` is a service worker registered from `Site.Master`. Pages are **network-first** (a wanted notice may be withdrawn; a stale one is worse than an offline message), static assets are cache-first with background refresh, and `hors-ligne.html` is the fallback. `/Admin/*`, `/Uploads/*`, `/sante` and non-GET requests are never cached. `manifest.json` lets the site be installed on a phone's home screen.
