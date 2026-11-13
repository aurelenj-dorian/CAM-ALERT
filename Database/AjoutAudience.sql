-- =============================================
-- CAM-ALERT : table de mesure d'audience
-- Idempotent : peut être exécuté plusieurs fois.
-- Aucune adresse IP n'est stockée.
-- =============================================

USE CamAlert;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PagesVues')
BEGIN
    CREATE TABLE PagesVues (
        PageVueID  BIGINT IDENTITY(1,1) PRIMARY KEY,
        VisiteurID UNIQUEIDENTIFIER NOT NULL,
        Chemin     NVARCHAR(200)    NOT NULL,
        DateVue    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_PagesVues_DateVue ON PagesVues (DateVue) INCLUDE (VisiteurID, Chemin);
END
GO
