-- ============================================================
--  CAM-ALERT — Données de référence et jeu de démonstration
--  À exécuter après 01_Schema.sql.
--
--  Le script est rejouable : il vide les tables de données avant
--  de les réinsérer, et réinitialise les compteurs IDENTITY pour
--  que les identifiants restent ceux auxquels le code se réfère
--  (StatutID = 3 pour « Traité », notamment).
-- ============================================================

USE CamAlert;
GO

DELETE FROM Journal;
DELETE FROM MessagesContact;
DELETE FROM Signalements;
DELETE FROM Geolocalisation;
DELETE FROM Dossiers;
DELETE FROM Utilisateurs;
DELETE FROM Roles;
DELETE FROM Statuts;
DELETE FROM TypesDossier;
DELETE FROM Regions;
GO

DBCC CHECKIDENT ('Regions',         RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('TypesDossier',    RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Statuts',         RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Roles',           RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Utilisateurs',    RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Dossiers',        RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Geolocalisation', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Signalements',    RESEED, 0) WITH NO_INFOMSGS;
GO

-- ============================================================
--  1. Les 10 régions du Cameroun, avec leur chef-lieu
-- ============================================================
INSERT INTO Regions (Nom, ChefLieu, Latitude, Longitude) VALUES
(N'Adamaoua',      N'Ngaoundéré',  7.327500, 13.584200),
(N'Centre',        N'Yaoundé',     3.848000, 11.502100),
(N'Est',           N'Bertoua',     4.577500, 13.684600),
(N'Extrême-Nord',  N'Maroua',     10.591000, 14.315900),
(N'Littoral',      N'Douala',      4.051000,  9.767900),
(N'Nord',          N'Garoua',      9.301700, 13.397100),
(N'Nord-Ouest',    N'Bamenda',     5.959700, 10.145300),
(N'Ouest',         N'Bafoussam',   5.477800, 10.417800),
(N'Sud',           N'Ebolowa',     2.915800, 11.154700),
(N'Sud-Ouest',     N'Buéa',        4.155000,  9.241400);
GO

-- ============================================================
--  2. Types de dossier
--  Le libellé « Disparition » est comparé littéralement dans
--  DossierDetail.aspx.cs et Carte.aspx.cs : ne pas le renommer.
-- ============================================================
INSERT INTO TypesDossier (Libelle, Description) VALUES
(N'Disparition',          N'Recherche d''une personne disparue'),
(N'Recherche judiciaire', N'Personne recherchée par la justice');
GO

-- ============================================================
--  3. Statuts de traitement d'un signalement
--  L'ordre fixe les identifiants : 1 = Nouveau, 2 = En cours,
--  3 = Traité, 4 = Non retenu. Le code s'appuie sur StatutID = 3
--  pour compter les signalements traités.
-- ============================================================
INSERT INTO Statuts (Libelle, Ordre) VALUES
(N'Nouveau',     1),
(N'En cours',    2),
(N'Traité',      3),
(N'Non retenu',  4);
GO

-- ============================================================
--  4. Rôles
--  Site.Master.cs et Carte.aspx.cs comparent le libellé à
--  « Administrateur » : ne pas le renommer.
-- ============================================================
INSERT INTO Roles (Libelle, Description) VALUES
(N'Administrateur', N'Accès complet : dossiers, signalements, comptes'),
(N'Agent',          N'Traitement des signalements, sans suppression de dossier');
GO

-- ============================================================
--  5. Compte d'administration initial
--
--  MOT DE PASSE INITIAL : CamAlert@2026
--
--  Le hash ci-dessous est réel et utilisable immédiatement.
--  Format : pbkdf2$sha256$<iterations>$<sel b64>$<cle b64>
--  (PBKDF2-HMAC-SHA256, 210 000 itérations, sel de 16 octets).
--  Voir Helpers/HacheurMotDePasse.cs.
--
--  DoitChangerMotDePasse = 1 : à sa première connexion, le compte
--  est redirigé vers Admin/MotDePasse.aspx et ne peut rien faire
--  d'autre avant d'avoir changé ce mot de passe. La politique de
--  robustesse refuse de rechoisir « CamAlert@2026 ».
--
--  Ce mot de passe est public : il figure dans un dépôt ouvert.
--  Il ne vaut que pour la première connexion d'une installation
--  neuve. Sur un site exposé à Internet, changez-le avant toute
--  mise en ligne.
-- ============================================================
INSERT INTO Utilisateurs (Login, NomComplet, Email, MotDePasse, RoleID,
                          EstActif, DoitChangerMotDePasse) VALUES
(N'admin', N'Administrateur CAM-ALERT', NULL,
 N'pbkdf2$sha256$210000$Ll6OoLYW5sYfPEA9H9UicQ==$wvG81n78Z1UK5peyALbH9jQJTLRmYWWp9U3SfzDJRhc=',
 1, 1, 1);
GO

-- ============================================================
--  6. Dossiers de démonstration
--
--  Les images correspondent aux fichiers déjà présents dans
--  Content/images/. Ce jeu de données est fictif : CAM-ALERT est
--  une maquette pédagogique, aucun nom ne désigne une personne
--  réelle.
-- ============================================================
INSERT INTO Dossiers (Titre, Description, TypeID, RegionID, Annee, Ville,
                      ImageUrl, SignesDistinctifs, EstPublie, StatutDossier,
                      DatePublication) VALUES
(N'MBIDA Pierre',
 N'Disparu après avoir quitté son domicile du quartier Mvog-Ada pour se rendre à son lieu de travail. Aucune nouvelle depuis.',
 1, 2, 2024, N'Yaoundé', N'/Content/images/mbida_pierre.jpg',
 N'Cicatrice au-dessus du sourcil gauche, 1 m 78, corpulence mince.',
 1, N'En cours', '2024-03-14'),

(N'NDONGO Alice',
 N'Signalée disparue par sa famille après un déplacement vers Douala. Son téléphone est resté injoignable depuis.',
 1, 5, 2024, N'Douala', N'/Content/images/ndongo_alice.jpg',
 N'Tatouage au poignet droit, cheveux tressés, 1 m 62.',
 1, N'En cours', '2024-06-02'),

(N'ESSOMBA Paul',
 N'Recherché dans le cadre d''une enquête pour escroquerie en bande organisée.',
 2, 2, 2023, N'Yaoundé', N'/Content/images/essomba_paul.jpg',
 N'Boiterie de la jambe gauche, porte habituellement des lunettes.',
 1, N'En cours', '2023-11-21'),

(N'HAMADOU Amadou',
 N'Disparu lors d''un déplacement entre Maroua et Garoua. Dernier contact établi à hauteur de Mora.',
 1, 4, 2025, N'Maroua', N'/Content/images/hamadou_amadou.jpg',
 N'Grande taille (1 m 90), marque de brûlure à l''avant-bras droit.',
 1, N'En cours', '2025-01-30'),

(N'NKEMBE Rose',
 N'Personne retrouvée en bonne santé chez un membre de sa famille à Bafoussam. Dossier clos.',
 1, 8, 2023, N'Bafoussam', N'/Content/images/nkembe_rose.jpg',
 N'1 m 58, lunettes de vue.',
 1, N'Résolu', '2023-05-09'),

(N'MOKOM Jules',
 N'Recherché pour abus de confiance. Un mandat a été levé après régularisation de sa situation.',
 2, 7, 2023, N'Bamenda', N'/Content/images/mokom_jules.jpg',
 N'Barbe fournie, 1 m 75.',
 1, N'Résolu', '2023-08-17'),

(N'MEBENGA Caroline',
 N'Disparue dans la zone de Kribi lors d''une sortie en mer. Recherches en cours avec les autorités maritimes.',
 1, 9, 2025, N'Kribi', N'/Content/images/mebenga_caroline.jpg',
 N'1 m 65, cicatrice au genou droit.',
 1, N'En cours', '2025-07-04'),

(N'TATA John',
 N'Recherché dans le cadre d''une enquête sur un trafic de véhicules volés.',
 2, 10, 2024, N'Buéa', N'/Content/images/tata_john.jpg',
 N'Cheveux rasés, tatouage sur l''avant-bras gauche.',
 1, N'En cours', '2024-09-12'),

(N'Dossier en préparation — non publié',
 N'Exemple de dossier encore invisible du public : EstPublie = 0. Toutes les requêtes publiques filtrent sur ce drapeau.',
 1, 1, 2026, N'Ngaoundéré', NULL,
 NULL, 0, N'En cours', '2026-02-01');
GO

-- Dates de résolution des dossiers clos, exigées par la contrainte
-- CK_Dossiers_Resolution.
UPDATE Dossiers
   SET DateResolution = '2023-09-15',
       CommentaireResolution = N'Personne retrouvée en bonne santé, dossier clos avec l''accord de la famille.'
 WHERE Titre = N'NKEMBE Rose';

UPDATE Dossiers
   SET DateResolution = '2024-01-08',
       CommentaireResolution = N'Situation régularisée, mandat levé par le parquet.'
 WHERE Titre = N'MOKOM Jules';
GO

-- ============================================================
--  7. Géolocalisation des dossiers publiés
--  La carte ne montre que les dossiers ayant une ligne ici
--  (jointure interne dans Carte.aspx.cs).
-- ============================================================
INSERT INTO Geolocalisation (DossierID, Latitude, Longitude, Precision_)
SELECT d.DossierID, v.Latitude, v.Longitude, N'Ville'
  FROM Dossiers d
  JOIN (VALUES
        (N'Yaoundé',     3.848000, 11.502100),
        (N'Douala',      4.051000,  9.767900),
        (N'Maroua',     10.591000, 14.315900),
        (N'Bafoussam',   5.477800, 10.417800),
        (N'Bamenda',     5.959700, 10.145300),
        (N'Kribi',       2.936800,  9.909500),
        (N'Buéa',        4.155000,  9.241400),
        (N'Ngaoundéré',  7.327500, 13.584200)
       ) AS v(Ville, Latitude, Longitude)
    ON v.Ville = d.Ville
 WHERE d.EstPublie = 1;
GO

-- ============================================================
--  8. Signalements de démonstration
-- ============================================================
INSERT INTO Signalements (DossierID, NomDeclarant, Email, Message,
                          StatutID, DateSoumission, CommentaireInterne)
SELECT d.DossierID, N'Anonyme', NULL,
       N'Je crois avoir aperçu cette personne au marché Mokolo mardi matin, vers 9 h.',
       1, '2025-09-18', NULL
  FROM Dossiers d WHERE d.Titre = N'MBIDA Pierre';

INSERT INTO Signalements (DossierID, NomDeclarant, Email, Message,
                          StatutID, DateSoumission, DateTraitement, CommentaireInterne)
SELECT d.DossierID, N'NGUEMA Victor', N'temoin.exemple@example.cm',
       N'Un véhicule correspondant à la description stationnait près de la gare routière de Bonabéri.',
       3, '2025-04-22', '2025-04-25',
       N'Vérification effectuée sur place, piste écartée.'
  FROM Dossiers d WHERE d.Titre = N'NDONGO Alice';

INSERT INTO Signalements (DossierID, NomDeclarant, Email, Message,
                          StatutID, DateSoumission, CommentaireInterne)
SELECT d.DossierID, NULL, NULL,
       N'Signalement anonyme : la personne aurait été vue dans le quartier Briqueterie.',
       2, '2025-10-02', N'En cours de vérification avec l''unité territoriale.'
  FROM Dossiers d WHERE d.Titre = N'ESSOMBA Paul';
GO

-- ============================================================
--  Contrôle final
-- ============================================================
SELECT N'Regions' AS [Table], COUNT(*) AS Lignes FROM Regions
UNION ALL SELECT N'TypesDossier',    COUNT(*) FROM TypesDossier
UNION ALL SELECT N'Statuts',         COUNT(*) FROM Statuts
UNION ALL SELECT N'Roles',           COUNT(*) FROM Roles
UNION ALL SELECT N'Utilisateurs',    COUNT(*) FROM Utilisateurs
UNION ALL SELECT N'Dossiers',        COUNT(*) FROM Dossiers
UNION ALL SELECT N'Geolocalisation', COUNT(*) FROM Geolocalisation
UNION ALL SELECT N'Signalements',    COUNT(*) FROM Signalements;
GO

PRINT '';
PRINT '════════════════════════════════════════════════════════';
PRINT ' CAM-ALERT : données de référence insérées.';
PRINT ' Connexion initiale : admin / CamAlert@2026';
PRINT ' Changement du mot de passe imposé à la première connexion.';
PRINT '════════════════════════════════════════════════════════';
GO
