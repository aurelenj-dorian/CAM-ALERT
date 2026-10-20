-- ============================================================
--  CAM-ALERT — Vues et procédures
--  À exécuter après 01_Schema.sql et 02_DonneesReference.sql.
--  Rejouable : chaque objet est supprimé avant d'être recréé.
-- ============================================================

USE CamAlert;
GO

IF OBJECT_ID('vw_StatistiquesPubliques', 'V')  IS NOT NULL DROP VIEW vw_StatistiquesPubliques;
IF OBJECT_ID('sp_PurgerDonneesPersonnelles', 'P') IS NOT NULL DROP PROCEDURE sp_PurgerDonneesPersonnelles;
IF OBJECT_ID('sp_JournaliserAction', 'P')      IS NOT NULL DROP PROCEDURE sp_JournaliserAction;
IF OBJECT_ID('sp_CompterEchecsConnexion', 'P') IS NOT NULL DROP PROCEDURE sp_CompterEchecsConnexion;
GO

-- ============================================================
--  vw_StatistiquesPubliques
--
--  Les compteurs de la page Historique étaient obtenus par trois
--  requêtes COUNT successives, donc trois allers-retours vers SQL
--  Server à chaque affichage. Cette vue les renvoie en une fois.
-- ============================================================
CREATE VIEW vw_StatistiquesPubliques
AS
SELECT
    NbDossiersResolus =
        (SELECT COUNT(*) FROM Dossiers WHERE StatutDossier = N'Résolu'),
    NbDossiersEnCours =
        (SELECT COUNT(*) FROM Dossiers WHERE StatutDossier = N'En cours'),
    NbDossiersPublies =
        (SELECT COUNT(*) FROM Dossiers WHERE EstPublie = 1),
    NbSignalementsTraites =
        (SELECT COUNT(*) FROM Signalements s
          JOIN Statuts st ON st.StatutID = s.StatutID
         WHERE st.Libelle = N'Traité'),
    NbSignalementsRecus =
        (SELECT COUNT(*) FROM Signalements);
GO

-- ============================================================
--  sp_JournaliserAction
--
--  Point d'entrée unique du journal d'audit. L'application passe
--  par Helpers/Journal.cs, qui appelle cette procédure : si le
--  format du journal évolue, le code applicatif n'a pas à changer.
-- ============================================================
CREATE PROCEDURE sp_JournaliserAction
    @UtilisateurID    INT            = NULL,
    @LoginUtilisateur NVARCHAR(50)   = NULL,
    @TypeAction       NVARCHAR(50),
    @Entite           NVARCHAR(50)   = NULL,
    @EntiteID         INT            = NULL,
    @Details          NVARCHAR(1000) = NULL,
    @AdresseIP        NVARCHAR(45)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Journal (UtilisateurID, LoginUtilisateur, TypeAction,
                         Entite, EntiteID, Details, AdresseIP)
    VALUES (@UtilisateurID, @LoginUtilisateur, @TypeAction,
            @Entite, @EntiteID, @Details, @AdresseIP);
END
GO

-- ============================================================
--  sp_CompterEchecsConnexion
--
--  Nombre d'échecs de connexion pour un login ou une adresse IP
--  sur une fenêtre donnée. Complète le limiteur en mémoire de
--  l'application : celui-ci repart de zéro au recyclage du pool
--  d'applications IIS, le journal non.
-- ============================================================
CREATE PROCEDURE sp_CompterEchecsConnexion
    @Login          NVARCHAR(50) = NULL,
    @AdresseIP      NVARCHAR(45) = NULL,
    @FenetreMinutes INT          = 15
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @depuis DATETIME2(0) = DATEADD(MINUTE, -@FenetreMinutes, SYSDATETIME());

    SELECT NbEchecs = COUNT(*)
      FROM Journal
     WHERE TypeAction = N'ConnexionEchouee'
       AND DateAction >= @depuis
       AND (@Login     IS NULL OR LoginUtilisateur = @Login)
       AND (@AdresseIP IS NULL OR AdresseIP        = @AdresseIP);
END
GO

-- ============================================================
--  sp_PurgerDonneesPersonnelles
--
--  CAM-ALERT reçoit des signalements du public, parfois anonymes,
--  au sujet de personnes disparues ou recherchées. Conserver
--  indéfiniment l'adresse IP et l'identité du déclarant n'a aucune
--  utilité une fois le signalement traité, et constitue un risque
--  si la base fuite — un témoin peut être identifié.
--
--  Cette procédure applique une durée de conservation :
--    • adresses IP des signalements traités ou non retenus ;
--    • identité du déclarant des signalements non retenus ;
--    • entrées de journal et messages de contact anciens.
--
--  Les signalements eux-mêmes et les dossiers ne sont jamais
--  supprimés : ils constituent la mémoire de l'enquête.
--
--  À planifier dans l'Agent SQL Server, une fois par semaine.
--  @Simulation = 1 affiche ce qui serait purgé sans rien modifier.
-- ============================================================
CREATE PROCEDURE sp_PurgerDonneesPersonnelles
    @JoursConservationIP      INT = 180,
    @JoursConservationJournal INT = 365,
    @JoursConservationContact INT = 365,
    @Simulation               BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @limiteIP      DATETIME2(0) = DATEADD(DAY, -@JoursConservationIP,      SYSDATETIME());
    DECLARE @limiteJournal DATETIME2(0) = DATEADD(DAY, -@JoursConservationJournal, SYSDATETIME());
    DECLARE @limiteContact DATETIME2(0) = DATEADD(DAY, -@JoursConservationContact, SYSDATETIME());

    IF @Simulation = 1
    BEGIN
        SELECT Operation = N'IP de signalements clos à effacer',
               Lignes = COUNT(*)
          FROM Signalements s
          JOIN Statuts st ON st.StatutID = s.StatutID
         WHERE s.AdresseIP IS NOT NULL
           AND s.DateSoumission < @limiteIP
           AND st.Libelle IN (N'Traité', N'Non retenu')
        UNION ALL
        SELECT N'Identités de signalements non retenus à effacer', COUNT(*)
          FROM Signalements s
          JOIN Statuts st ON st.StatutID = s.StatutID
         WHERE st.Libelle = N'Non retenu'
           AND s.DateSoumission < @limiteIP
           AND (s.NomDeclarant IS NOT NULL OR s.Email IS NOT NULL)
        UNION ALL
        SELECT N'Entrées de journal à supprimer', COUNT(*)
          FROM Journal WHERE DateAction < @limiteJournal
        UNION ALL
        SELECT N'Messages de contact à supprimer', COUNT(*)
          FROM MessagesContact WHERE DateReception < @limiteContact;

        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE s
           SET s.AdresseIP = NULL
          FROM Signalements s
          JOIN Statuts st ON st.StatutID = s.StatutID
         WHERE s.AdresseIP IS NOT NULL
           AND s.DateSoumission < @limiteIP
           AND st.Libelle IN (N'Traité', N'Non retenu');

        UPDATE s
           SET s.NomDeclarant = NULL,
               s.Email        = NULL,
               s.Telephone    = NULL
          FROM Signalements s
          JOIN Statuts st ON st.StatutID = s.StatutID
         WHERE st.Libelle = N'Non retenu'
           AND s.DateSoumission < @limiteIP;

        DELETE FROM Journal         WHERE DateAction    < @limiteJournal;
        DELETE FROM MessagesContact WHERE DateReception < @limiteContact;

        -- La purge elle-même est tracée, pour qu'un audit puisse
        -- constater que la durée de conservation est appliquée.
        INSERT INTO Journal (LoginUtilisateur, TypeAction, Entite, Details)
        VALUES (N'système', N'Modification', N'Purge',
                N'Purge des données personnelles : IP > '
                + CAST(@JoursConservationIP AS NVARCHAR(10)) + N' j, journal > '
                + CAST(@JoursConservationJournal AS NVARCHAR(10)) + N' j.');

        COMMIT TRANSACTION;
        PRINT 'Purge effectuée.';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT 'CAM-ALERT : vues et procédures créées.';
GO
