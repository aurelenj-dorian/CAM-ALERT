using System;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;

namespace CamAlert.Helpers
{
    /// <summary>
    /// Contrôle et enregistrement des fichiers déposés par les utilisateurs.
    ///
    /// Les deux formulaires d'upload — photo d'un dossier côté administration,
    /// pièce jointe d'un signalement côté public — se contentaient de regarder
    /// l'extension puis d'écrire le fichier sous le nom fourni par le client :
    ///
    ///     string nomFichier = DateTime.Now.Ticks + "_" + fuFichier.FileName;
    ///     fuFichier.SaveAs(dossierUpload + nomFichier);
    ///
    /// Trois problèmes. Le nom venait du navigateur, donc pouvait contenir
    /// « ..\ » et sortir du dossier prévu. L'extension ne dit rien du contenu :
    /// un script renommé en .jpg passait. Et aucune taille maximale n'était
    /// vérifiée, un fichier de plusieurs gigaoctets était accepté.
    ///
    /// Ici, le nom est entièrement régénéré (horodatage + GUID), la taille est
    /// bornée, et le contenu est contrôlé sur ses premiers octets — la signature
    /// du format doit correspondre à l'extension annoncée.
    /// </summary>
    public static class FichierSecurise
    {
        /// <summary>Taille maximale d'une image : 5 Mo.</summary>
        public const int TAILLE_MAX_IMAGE = 5 * 1024 * 1024;

        /// <summary>Taille maximale d'une pièce jointe de signalement : 8 Mo.</summary>
        public const int TAILLE_MAX_PIECE_JOINTE = 8 * 1024 * 1024;

        private static readonly string[] ExtensionsImage = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] ExtensionsPieceJointe = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

        /// <summary>
        /// Résultat d'un contrôle : soit un nom de fichier accepté, soit un
        /// message d'erreur destiné à l'utilisateur.
        /// </summary>
        public class Resultat
        {
            /// <summary>Chemin relatif à stocker en base, ou null en cas de refus.</summary>
            public string CheminRelatif { get; set; }

            /// <summary>Message à afficher, ou null si tout s'est bien passé.</summary>
            public string Erreur { get; set; }

            /// <summary>Vrai si aucun fichier n'avait été joint (champ facultatif).</summary>
            public bool Absent { get; set; }

            public bool EstValide { get { return Erreur == null; } }

            public static Resultat Refus(string message)
            {
                return new Resultat { Erreur = message };
            }

            public static Resultat AucunFichier()
            {
                return new Resultat { Absent = true };
            }
        }

        /// <summary>
        /// Contrôle et enregistre une image de dossier dans
        /// <c>~/Admin/image/</c>.
        /// </summary>
        public static Resultat EnregistrerImage(FileUpload champ, HttpServerUtility serveur)
        {
            return Enregistrer(champ, serveur, "~/Admin/image/", "/Admin/image/",
                               "dossier", ExtensionsImage, TAILLE_MAX_IMAGE);
        }

        /// <summary>
        /// Contrôle et enregistre une pièce jointe de signalement dans
        /// <c>~/Uploads/</c>.
        /// </summary>
        public static Resultat EnregistrerPieceJointe(FileUpload champ, HttpServerUtility serveur)
        {
            return Enregistrer(champ, serveur, "~/Uploads/", "/Uploads/",
                               "piece", ExtensionsPieceJointe, TAILLE_MAX_PIECE_JOINTE);
        }

        private static Resultat Enregistrer(FileUpload champ, HttpServerUtility serveur,
                                            string dossierVirtuel, string prefixeUrl,
                                            string prefixeNom, string[] extensionsAutorisees,
                                            int tailleMax)
        {
            if (champ == null || !champ.HasFile || champ.PostedFile == null
                || champ.PostedFile.ContentLength == 0)
                return Resultat.AucunFichier();

            var fichier = champ.PostedFile;

            if (fichier.ContentLength > tailleMax)
                return Resultat.Refus(string.Format(CultureInfo.CurrentCulture,
                    "Le fichier dépasse la taille maximale autorisée ({0} Mo).",
                    tailleMax / (1024 * 1024)));

            // On ne garde que l'extension, et uniquement pour choisir le format
            // attendu : le reste du nom fourni par le client est jeté.
            var extension = ExtensionNormalisee(fichier.FileName);
            if (!EstAutorisee(extension, extensionsAutorisees))
                return Resultat.Refus("Format non autorisé. Formats acceptés : "
                                      + string.Join(", ", extensionsAutorisees) + ".");

            if (!SignatureCorrespond(fichier.InputStream, extension))
                return Resultat.Refus(
                    "Le contenu du fichier ne correspond pas à son extension. " +
                    "Envoyez une image ou un PDF valide.");

            try
            {
                var dossierPhysique = serveur.MapPath(dossierVirtuel);
                if (!Directory.Exists(dossierPhysique))
                    Directory.CreateDirectory(dossierPhysique);

                // Nom entièrement régénéré : aucune chaîne venant du client
                // n'atteint le système de fichiers.
                var nom = string.Format(CultureInfo.InvariantCulture,
                    "{0}_{1:yyyyMMdd_HHmmss}_{2}{3}",
                    prefixeNom, DateTime.Now,
                    Guid.NewGuid().ToString("N").Substring(0, 8),
                    extension);

                var cheminComplet = Path.Combine(dossierPhysique, nom);

                // Le flux a été lu par le contrôle de signature : on le rembobine.
                fichier.InputStream.Position = 0;
                using (var sortie = new FileStream(cheminComplet, FileMode.CreateNew,
                                                   FileAccess.Write))
                {
                    fichier.InputStream.CopyTo(sortie);
                }

                return new Resultat { CheminRelatif = prefixeUrl + nom };
            }
            catch (Exception ex)
            {
                Journal.Enregistrer(Journal.Actions.Erreur,
                    details: "Échec d'enregistrement d'un fichier : " + ex.Message);

                return Resultat.Refus(
                    "Le fichier n'a pas pu être enregistré. Réessayez, " +
                    "ou contactez l'administrateur si le problème persiste.");
            }
        }

        /// <summary>Extension en minuscules, sans le reste du nom.</summary>
        public static string ExtensionNormalisee(string nomFichier)
        {
            if (string.IsNullOrWhiteSpace(nomFichier)) return string.Empty;

            try
            {
                // GetExtension sur un nom contenant des caractères interdits
                // lève une exception : on le traite comme un refus.
                var extension = Path.GetExtension(nomFichier);
                return extension == null ? string.Empty : extension.ToLowerInvariant();
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }

        private static bool EstAutorisee(string extension, string[] autorisees)
        {
            if (string.IsNullOrEmpty(extension)) return false;

            foreach (var a in autorisees)
                if (string.Equals(a, extension, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        /// Vérifie que les premiers octets du fichier correspondent au format
        /// annoncé par l'extension. Un fichier .aspx renommé en .jpg est ainsi
        /// refusé, alors que le contrôle d'extension seul le laissait passer.
        /// </summary>
        public static bool SignatureCorrespond(Stream flux, string extension)
        {
            if (flux == null || !flux.CanRead || !flux.CanSeek) return false;

            var entete = new byte[12];
            flux.Position = 0;
            int lus = flux.Read(entete, 0, entete.Length);
            flux.Position = 0;

            if (lus < 4) return false;

            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    // FF D8 FF
                    return entete[0] == 0xFF && entete[1] == 0xD8 && entete[2] == 0xFF;

                case ".png":
                    // 89 50 4E 47 0D 0A 1A 0A
                    return lus >= 8
                        && entete[0] == 0x89 && entete[1] == 0x50
                        && entete[2] == 0x4E && entete[3] == 0x47
                        && entete[4] == 0x0D && entete[5] == 0x0A
                        && entete[6] == 0x1A && entete[7] == 0x0A;

                case ".webp":
                    // "RIFF" .... "WEBP"
                    return lus >= 12
                        && entete[0] == (byte)'R' && entete[1] == (byte)'I'
                        && entete[2] == (byte)'F' && entete[3] == (byte)'F'
                        && entete[8] == (byte)'W' && entete[9] == (byte)'E'
                        && entete[10] == (byte)'B' && entete[11] == (byte)'P';

                case ".pdf":
                    // "%PDF"
                    return entete[0] == (byte)'%' && entete[1] == (byte)'P'
                        && entete[2] == (byte)'D' && entete[3] == (byte)'F';

                default:
                    return false;
            }
        }
    }
}
