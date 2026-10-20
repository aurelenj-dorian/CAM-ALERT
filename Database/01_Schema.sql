-- ============================================================
--  CAM-ALERT — Schéma de la base de données
--  Base : CamAlert   |   SQL Server 2017 ou supérieur
--
--  À exécuter en premier, puis 02_DonneesReference.sql,
--  puis 03_Procedures.sql.
--
--  Pourquoi ce fichier existe : le dépôt ne contenait aucun script
--  de base de données. Le code interrogeait des tables (Dossiers,
--  Regions, TypesDossier, Statuts, Signalements, Geolocalisation,
--  Utilisateurs, Roles) dont la définition n'existait nulle part :
--  personne ne pouvait installer l'application sans la reconstruire
--  à la main depuis les requêtes. Ce schéma est reconstitué à partir
--  de toutes les requêtes du code (Helpers/Database.cs et les
--  code-behind), et complété des contraintes et index manquants.
-- ============================================================

IF DB_ID('CamAlert') IS NULL
    CREATE DATABASE CamAlert;
GO

USE CamAlert;
GO

-- ============================================================
--  Suppression dans l'ordre inverse des dépendances,
--  pour que le script puisse être relancé proprement.
-- ============================================================
IF OBJECT_ID('Journal',          'U') IS NOT NULL DROP TABLE Journal;
IF OBJECT_ID('MessagesContact',  'U') IS NOT NULL DROP TABLE MessagesContact;
IF OBJECT_ID('Signalements',     'U') IS NOT NULL DROP TABLE Signalements;
IF OBJECT_ID('Geolocalisation',  'U') IS NOT NULL DROP TABLE Geolocalisation;
IF OBJECT_ID('Dossiers',         'U') IS NOT NULL DROP TABLE Dossiers;
IF OBJECT_ID('Utilisateurs',     'U') IS NOT NULL DROP TABLE Utilisateurs;
IF OBJECT_ID('Roles',            'U') IS NOT NULL DROP TABLE Roles;
IF OBJECT_ID('Statuts',          'U') IS NOT NULL DROP TABLE Statuts;
IF OBJECT_ID('TypesDossier',     'U') IS NOT NULL DROP TABLE TypesDossier;
IF OBJECT_ID('Regions',          'U') IS NOT NULL DROP TABLE Regions;
GO

-- ============================================================
--  TABLES DE RÉFÉRENCE
-- ============================================================

-- Les 10 régions du Cameroun.
CREATE TABLE Regions (
    RegionID    INT           IDENTITY(1,1) CONSTRAINT PK_Regions PRIMARY KEY,
    Nom         NVARCHAR(100) NOT NULL CONSTRAINT UQ_Regions_Nom UNIQUE,
    ChefLieu    NVARCHAR(100) NULL,
    -- Centre de la région, pour centrer la carte quand un dossier
    -- n'a pas de géolocalisation propre.
    Latitude    DECIMAL(9,6)  NULL,
    Longitude   DECIMAL(9,6)  NULL
);
GO

-- Nature du dossier. Le code compare le libellé à « Disparition »
-- (DossierDetail.aspx.cs, Carte.aspx.cs) : ne pas renommer cette
-- valeur sans adapter le code.
CREATE TABLE TypesDossier (
    TypeID      INT           IDENTITY(1,1) CONSTRAINT PK_TypesDossier PRIMARY KEY,
    Libelle     NVARCHAR(50)  NOT NULL CONSTRAINT UQ_TypesDossier_Libelle UNIQUE,
    Description NVARCHAR(200) NULL
);
GO

-- Cycle de traitement d'un signalement reçu du public.
-- Historique.aspx.cs et Admin/Signalements.aspx.cs utilisent
-- StatutID = 3 pour « Traité » : conserver cet ordre d'insertion.
CREATE TABLE Statuts (
    StatutID    INT           IDENTITY(1,1) CONSTRAINT PK_Statuts PRIMARY KEY,
    Libelle     NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Statuts_Libelle UNIQUE,
    Ordre       INT           NOT NULL DEFAULT 0
);
GO

CREATE TABLE Roles (
    RoleID      INT           IDENTITY(1,1) CONSTRAINT PK_Roles PRIMARY KEY,
    Libelle     NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Roles_Libelle UNIQUE,
    Description NVARCHAR(200) NULL
);
GO

-- ============================================================
--  COMPTES D'ADMINISTRATION
-- ============================================================

CREATE TABLE Utilisateurs (
    UtilisateurID       INT            IDENTITY(1,1) CONSTRAINT PK_Utilisateurs PRIMARY KEY,
    Login               NVARCHAR(50)   NOT NULL CONSTRAINT UQ_Utilisateurs_Login UNIQUE,
    NomComplet          NVARCHAR(150)  NULL,
    Email               NVARCHAR(150)  NULL,

    -- Format PBKDF2 : pbkdf2$sha256$<iterations>$<sel b64>$<cle b64>
    -- Voir Helpers/HacheurMotDePasse.cs. Les anciens hash SHA-256 non
    -- salés (64 caractères hexadécimaux) restent acceptés à la connexion
    -- et sont réécrits automatiquement à ce format.
    MotDePasse          NVARCHAR(256)  NOT NULL,

    RoleID              INT            NOT NULL,
    EstActif            BIT            NOT NULL CONSTRAINT DF_Utilisateurs_Actif DEFAULT 1,

    -- Impose le changement du mot de passe initial à la première connexion.
    DoitChangerMotDePasse BIT          NOT NULL CONSTRAINT DF_Utilisateurs_Changer DEFAULT 1,

    DateCreation        DATETIME2(0)   NOT NULL CONSTRAINT DF_Utilisateurs_Creation DEFAULT SYSDATETIME(),
    DateDerniereConnexion DATETIME2(0) NULL,

    CONSTRAINT FK_Utilisateurs_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);
GO

-- ============================================================
--  DOSSIERS
-- ============================================================

CREATE TABLE Dossiers (
    DossierID            INT            IDENTITY(1,1) CONSTRAINT PK_Dossiers PRIMARY KEY,
    Titre                NVARCHAR(200)  NOT NULL,
    Description          NVARCHAR(MAX)  NULL,
    TypeID               INT            NOT NULL,
    RegionID             INT            NOT NULL,

    -- Année de l'événement. Les filtres publics s'appuient dessus.
    Annee                INT            NULL,
    Ville                NVARCHAR(100)  NULL,

    -- Chemin relatif de la photo, servi depuis /Admin/image/.
    ImageUrl             NVARCHAR(300)  NULL,
    SignesDistinctifs    NVARCHAR(1000) NULL,

    -- Un dossier non publié reste invisible du public : toutes les
    -- requêtes publiques filtrent sur EstPublie = 1.
    EstPublie            BIT            NOT NULL CONSTRAINT DF_Dossiers_Publie DEFAULT 0,

    -- Avancement de l'enquête, lu par Historique.aspx.cs qui compare
    -- aux chaînes « Résolu » et « En cours ».
    StatutDossier        NVARCHAR(30)   NOT NULL CONSTRAINT DF_Dossiers_Statut DEFAULT N'En cours',
    DateResolution       DATETIME2(0)   NULL,
    CommentaireResolution NVARCHAR(1000) NULL,

    DatePublication      DATETIME2(0)   NOT NULL CONSTRAINT DF_Dossiers_DatePub DEFAULT SYSDATETIME(),
    DateModification     DATETIME2(0)   NULL,

    CONSTRAINT FK_Dossiers_Types   FOREIGN KEY (TypeID)   REFERENCES TypesDossier(TypeID),
    CONSTRAINT FK_Dossiers_Regions FOREIGN KEY (RegionID) REFERENCES Regions(RegionID),

    CONSTRAINT CK_Dossiers_Statut CHECK (StatutDossier IN (N'En cours', N'Résolu', N'Classé')),
    -- Garde-fou sur la saisie : l'année est un entier libre dans le
    -- formulaire d'administration.
    CONSTRAINT CK_Dossiers_Annee CHECK (Annee IS NULL OR Annee BETWEEN 1960 AND 2100),
    -- Un dossier résolu doit porter sa date de résolution.
    CONSTRAINT CK_Dossiers_Resolution
        CHECK (StatutDossier <> N'Résolu' OR DateResolution IS NOT NULL)
);
GO

-- Coordonnées affichées sur la carte interactive (Carte.aspx).
-- Une ligne au plus par dossier : la jointure du code suppose
-- l'unicité, sans quoi un dossier apparaîtrait en double.
CREATE TABLE Geolocalisation (
    GeoID       INT          IDENTITY(1,1) CONSTRAINT PK_Geolocalisation PRIMARY KEY,
    DossierID   INT          NOT NULL CONSTRAINT UQ_Geolocalisation_Dossier UNIQUE,
    Latitude    DECIMAL(9,6) NOT NULL,
    Longitude   DECIMAL(9,6) NOT NULL,
    Precision_  NVARCHAR(50) NULL,   -- Ville | Quartier | Exacte

    CONSTRAINT FK_Geolocalisation_Dossiers
        FOREIGN KEY (DossierID) REFERENCES Dossiers(DossierID) ON DELETE CASCADE,

    -- Cadre géographique du Cameroun, marge incluse : évite qu'une
    -- inversion latitude/longitude place un marqueur dans l'océan.
    CONSTRAINT CK_Geolocalisation_Latitude  CHECK (Latitude  BETWEEN 1.0 AND 13.5),
    CONSTRAINT CK_Geolocalisation_Longitude CHECK (Longitude BETWEEN 8.0 AND 16.5)
);
GO

-- ============================================================
--  SIGNALEMENTS DU PUBLIC
-- ============================================================

CREATE TABLE Signalements (
    SignalementID     INT            IDENTITY(1,1) CONSTRAINT PK_Signalements PRIMARY KEY,
    DossierID         INT            NOT NULL,

    -- Un signalement peut être anonyme : nom et email sont facultatifs.
    NomDeclarant      NVARCHAR(150)  NULL,
    Email             NVARCHAR(150)  NULL,
    Telephone         NVARCHAR(30)   NULL,

    Message           NVARCHAR(MAX)  NOT NULL,
    FichierJoint      NVARCHAR(300)  NULL,

    StatutID          INT            NOT NULL CONSTRAINT DF_Signalements_Statut DEFAULT 1,
    CommentaireInterne NVARCHAR(MAX) NULL,

    -- Conservée pour la limitation d'abus et l'enquête. À purger selon
    -- la durée de conservation retenue (voir 03_Procedures.sql).
    AdresseIP         NVARCHAR(45)   NULL,

    DateSoumission    DATETIME2(0)   NOT NULL CONSTRAINT DF_Signalements_Date DEFAULT SYSDATETIME(),
    DateTraitement    DATETIME2(0)   NULL,

    CONSTRAINT FK_Signalements_Dossiers
        FOREIGN KEY (DossierID) REFERENCES Dossiers(DossierID),
    CONSTRAINT FK_Signalements_Statuts
        FOREIGN KEY (StatutID)  REFERENCES Statuts(StatutID)
);
GO

-- Messages du formulaire de contact.
-- Ils n'étaient qu'envoyés par email : si le serveur SMTP était
-- indisponible, le message était perdu sans que personne ne le sache.
CREATE TABLE MessagesContact (
    MessageID      INT            IDENTITY(1,1) CONSTRAINT PK_MessagesContact PRIMARY KEY,
    Nom            NVARCHAR(150)  NOT NULL,
    Email          NVARCHAR(150)  NOT NULL,
    Sujet          NVARCHAR(150)  NOT NULL,
    Message        NVARCHAR(MAX)  NOT NULL,
    AdresseIP      NVARCHAR(45)   NULL,
    EmailEnvoye    BIT            NOT NULL CONSTRAINT DF_MessagesContact_Envoye DEFAULT 0,
    DateReception  DATETIME2(0)   NOT NULL CONSTRAINT DF_MessagesContact_Date DEFAULT SYSDATETIME()
);
GO

-- ============================================================
--  JOURNAL D'AUDIT
--  Aucune trace n'était conservée : impossible de savoir qui avait
--  supprimé un dossier, ni de détecter une série de tentatives de
--  connexion. Les données d'identité sont dénormalisées pour survivre
--  à la suppression d'un compte.
-- ============================================================

CREATE TABLE Journal (
    JournalID       INT            IDENTITY(1,1) CONSTRAINT PK_Journal PRIMARY KEY,
    UtilisateurID   INT            NULL,
    LoginUtilisateur NVARCHAR(50)  NULL,

    -- Connexion | ConnexionEchouee | Deconnexion | Creation |
    -- Modification | Suppression | Publication | Signalement | Erreur
    TypeAction      NVARCHAR(50)   NOT NULL,
    Entite          NVARCHAR(50)   NULL,   -- Dossier | Signalement | Utilisateur
    EntiteID        INT            NULL,
    Details         NVARCHAR(1000) NULL,
    AdresseIP       NVARCHAR(45)   NULL,
    DateAction      DATETIME2(0)   NOT NULL CONSTRAINT DF_Journal_Date DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Journal_Utilisateurs
        FOREIGN KEY (UtilisateurID) REFERENCES Utilisateurs(UtilisateurID)
);
GO

-- ============================================================
--  INDEX
--  Toutes les listes publiques trient par DatePublication
--  décroissante en filtrant sur EstPublie : sans index, chaque
--  affichage provoque une analyse complète de la table.
-- ============================================================

CREATE NONCLUSTERED INDEX IX_Dossiers_Publie_Date
    ON Dossiers (EstPublie, DatePublication DESC)
    INCLUDE (Titre, Annee, Ville, TypeID, RegionID);
GO

CREATE NONCLUSTERED INDEX IX_Dossiers_Type_Annee
    ON Dossiers (TypeID, Annee)
    WHERE EstPublie = 1;
GO

CREATE NONCLUSTERED INDEX IX_Dossiers_Statut
    ON Dossiers (StatutDossier)
    INCLUDE (DateResolution);
GO

CREATE NONCLUSTERED INDEX IX_Signalements_Statut_Date
    ON Signalements (StatutID, DateSoumission DESC)
    INCLUDE (DossierID, NomDeclarant);
GO

CREATE NONCLUSTERED INDEX IX_Signalements_Dossier
    ON Signalements (DossierID);
GO

CREATE NONCLUSTERED INDEX IX_Journal_Date
    ON Journal (DateAction DESC)
    INCLUDE (TypeAction, LoginUtilisateur, AdresseIP);
GO

CREATE NONCLUSTERED INDEX IX_Journal_Type_Ip
    ON Journal (TypeAction, AdresseIP, DateAction DESC);
GO

PRINT 'CAM-ALERT : schéma créé.';
PRINT 'Exécuter ensuite 02_DonneesReference.sql puis 03_Procedures.sql.';
GO
