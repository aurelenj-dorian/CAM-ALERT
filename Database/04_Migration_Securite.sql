-- ============================================================
--  CAM-ALERT — Migration de sécurité
--
--  Pour une base « CamAlert » créée à la main avant l'existence des
--  scripts de ce dossier. Le script est IDEMPOTENT : il ajoute ce
--  qui manque et ne touche à aucune donnée existante.
--
--  Sur une installation neuve, utiliser 01/02/03 plutôt que ce
--  script.
--
--  À exécuter avant de déployer la version du code qui accompagne
--  cette migration : l'application attend les colonnes ajoutées ici.
-- ============================================================

USE CamAlert;
GO

SET NOCOUNT ON;
PRINT '── 1. Colonnes de comptes ───────────────────────────────';
GO

-- La colonne MotDePasse doit pouvoir contenir un hash PBKDF2
-- (environ 95 caractères) et non plus seulement 64 caractères
-- hexadécimaux. Sans cet élargissement, le hash serait tronqué à
-- l'écriture et personne ne pourrait plus se connecter.
IF EXISTS (SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID('Utilisateurs')
              AND name = 'MotDePasse'
              AND max_length < 512)   -- max_length en octets pour NVARCHAR
BEGIN
    ALTER TABLE Utilisateurs ALTER COLUMN MotDePasse NVARCHAR(256) NOT NULL;
    PRINT '   ~ Utilisateurs.MotDePasse élargi à NVARCHAR(256)';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('Utilisateurs')
                  AND name = 'DoitChangerMotDePasse')
BEGIN
    ALTER TABLE Utilisateurs
        ADD DoitChangerMotDePasse BIT NOT NULL
            CONSTRAINT DF_Utilisateurs_Changer DEFAULT 1;
    PRINT '   + Utilisateurs.DoitChangerMotDePasse';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('Utilisateurs')
                  AND name = 'DateDerniereConnexion')
BEGIN
    ALTER TABLE Utilisateurs ADD DateDerniereConnexion DATETIME2(0) NULL;
    PRINT '   + Utilisateurs.DateDerniereConnexion';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('Utilisateurs') AND name = 'NomComplet')
BEGIN
    ALTER TABLE Utilisateurs ADD NomComplet NVARCHAR(150) NULL;
    PRINT '   + Utilisateurs.NomComplet';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('Utilisateurs') AND name = 'Email')
BEGIN
    ALTER TABLE Utilisateurs ADD Email NVARCHAR(150) NULL;
    PRINT '   + Utilisateurs.Email';
END
GO

-- Les comptes existants utilisent encore un hash SHA-256 non salé.
-- Ils restent acceptés à la connexion et sont réécrits en PBKDF2
-- automatiquement : on ne force donc pas DoitChangerMotDePasse,
-- sauf pour les comptes qui n'ont jamais servi.
UPDATE Utilisateurs
   SET DoitChangerMotDePasse = 1
 WHERE DateDerniereConnexion IS NULL;
GO

PRINT '── 2. Traçabilité des signalements ──────────────────────';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('Signalements') AND name = 'AdresseIP')
BEGIN
    ALTER TABLE Signalements ADD AdresseIP NVARCHAR(45) NULL;
    PRINT '   + Signalements.AdresseIP';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('Signalements') AND name = 'DateTraitement')
BEGIN
    ALTER TABLE Signalements ADD DateTraitement DATETIME2(0) NULL;
    PRINT '   + Signalements.DateTraitement';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('Signalements') AND name = 'Telephone')
BEGIN
    ALTER TABLE Signalements ADD Telephone NVARCHAR(30) NULL;
    PRINT '   + Signalements.Telephone';
END
GO

PRINT '── 3. Journal d''audit ───────────────────────────────────';
GO

IF OBJECT_ID('Journal', 'U') IS NULL
BEGIN
    CREATE TABLE Journal (
        JournalID        INT            IDENTITY(1,1) CONSTRAINT PK_Journal PRIMARY KEY,
        UtilisateurID    INT            NULL,
        LoginUtilisateur NVARCHAR(50)   NULL,
        TypeAction       NVARCHAR(50)   NOT NULL,
        Entite           NVARCHAR(50)   NULL,
        EntiteID         INT            NULL,
        Details          NVARCHAR(1000) NULL,
        AdresseIP        NVARCHAR(45)   NULL,
        DateAction       DATETIME2(0)   NOT NULL CONSTRAINT DF_Journal_Date DEFAULT SYSDATETIME(),
        CONSTRAINT FK_Journal_Utilisateurs
            FOREIGN KEY (UtilisateurID) REFERENCES Utilisateurs(UtilisateurID)
    );
    PRINT '   + table Journal';
END
GO

PRINT '── 4. Messages de contact ───────────────────────────────';
GO

IF OBJECT_ID('MessagesContact', 'U') IS NULL
BEGIN
    CREATE TABLE MessagesContact (
        MessageID     INT           IDENTITY(1,1) CONSTRAINT PK_MessagesContact PRIMARY KEY,
        Nom           NVARCHAR(150) NOT NULL,
        Email         NVARCHAR(150) NOT NULL,
        Sujet         NVARCHAR(150) NOT NULL,
        Message       NVARCHAR(MAX) NOT NULL,
        AdresseIP     NVARCHAR(45)  NULL,
        EmailEnvoye   BIT           NOT NULL CONSTRAINT DF_MessagesContact_Envoye DEFAULT 0,
        DateReception DATETIME2(0)  NOT NULL CONSTRAINT DF_MessagesContact_Date DEFAULT SYSDATETIME()
    );
    PRINT '   + table MessagesContact';
END
GO

PRINT '── 5. Index ─────────────────────────────────────────────';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Dossiers_Publie_Date')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Dossiers_Publie_Date
        ON Dossiers (EstPublie, DatePublication DESC)
        INCLUDE (Titre, Annee, Ville, TypeID, RegionID);
    PRINT '   + IX_Dossiers_Publie_Date';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Dossiers_Statut')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Dossiers_Statut
        ON Dossiers (StatutDossier) INCLUDE (DateResolution);
    PRINT '   + IX_Dossiers_Statut';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Signalements_Statut_Date')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Signalements_Statut_Date
        ON Signalements (StatutID, DateSoumission DESC)
        INCLUDE (DossierID, NomDeclarant);
    PRINT '   + IX_Signalements_Statut_Date';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Journal_Date')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Journal_Date
        ON Journal (DateAction DESC)
        INCLUDE (TypeAction, LoginUtilisateur, AdresseIP);
    PRINT '   + IX_Journal_Date';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Journal_Type_Ip')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Journal_Type_Ip
        ON Journal (TypeAction, AdresseIP, DateAction DESC);
    PRINT '   + IX_Journal_Type_Ip';
END
GO

PRINT '── 6. Unicité de la géolocalisation ─────────────────────';
GO

-- La carte joint Dossiers et Geolocalisation : deux lignes pour un
-- même dossier l'affichaient deux fois. On déduplique avant de poser
-- la contrainte, en gardant la ligne la plus récente.
IF OBJECT_ID('Geolocalisation', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Geolocalisation_Dossier')
BEGIN
    ;WITH doublons AS (
        SELECT GeoID,
               rang = ROW_NUMBER() OVER (PARTITION BY DossierID ORDER BY GeoID DESC)
          FROM Geolocalisation
    )
    DELETE FROM doublons WHERE rang > 1;

    CREATE UNIQUE NONCLUSTERED INDEX UQ_Geolocalisation_Dossier
        ON Geolocalisation (DossierID);
    PRINT '   + UQ_Geolocalisation_Dossier (doublons supprimés)';
END
GO

PRINT '── 7. Vues et procédures ────────────────────────────────';
PRINT '   Exécuter 03_Procedures.sql pour (re)créer';
PRINT '   vw_StatistiquesPubliques, sp_JournaliserAction,';
PRINT '   sp_CompterEchecsConnexion et sp_PurgerDonneesPersonnelles.';
GO

PRINT '── 8. État des mots de passe ────────────────────────────';
GO

SELECT Format = CASE
            WHEN MotDePasse LIKE 'pbkdf2$sha256$%' THEN 'PBKDF2 (courant)'
            WHEN LEN(MotDePasse) = 64              THEN 'SHA-256 (migré à la prochaine connexion)'
            ELSE 'inconnu (à vérifier)' END,
       NbComptes = COUNT(*)
  FROM Utilisateurs
 GROUP BY CASE
            WHEN MotDePasse LIKE 'pbkdf2$sha256$%' THEN 'PBKDF2 (courant)'
            WHEN LEN(MotDePasse) = 64              THEN 'SHA-256 (migré à la prochaine connexion)'
            ELSE 'inconnu (à vérifier)' END;
GO

PRINT '';
PRINT '════════════════════════════════════════════════════════';
PRINT ' Migration appliquée. Exécuter ensuite 03_Procedures.sql.';
PRINT '════════════════════════════════════════════════════════';
GO
