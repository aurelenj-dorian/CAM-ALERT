using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlert.Helpers;

namespace CamAlert.Tests
{
    /// <summary>
    /// Contrôle du contenu des fichiers déposés. L'ancien code se contentait de
    /// regarder l'extension : un script renommé en .jpg passait, et le nom
    /// fourni par le navigateur servait tel quel de nom de fichier sur le
    /// disque.
    /// </summary>
    [TestClass]
    public class FichierSecuriseTests
    {
        private static MemoryStream Flux(params byte[] octets)
        {
            var flux = new MemoryStream();
            flux.Write(octets, 0, octets.Length);

            // Un fichier réel a du contenu après son en-tête.
            var remplissage = new byte[64];
            flux.Write(remplissage, 0, remplissage.Length);

            flux.Position = 0;
            return flux;
        }

        [TestMethod]
        public void SignatureJpeg_EstReconnue()
        {
            using (var flux = Flux(0xFF, 0xD8, 0xFF, 0xE0))
            {
                Assert.IsTrue(FichierSecurise.SignatureCorrespond(flux, ".jpg"));
                Assert.IsTrue(FichierSecurise.SignatureCorrespond(flux, ".jpeg"));
            }
        }

        [TestMethod]
        public void SignaturePng_EstReconnue()
        {
            using (var flux = Flux(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
            {
                Assert.IsTrue(FichierSecurise.SignatureCorrespond(flux, ".png"));
            }
        }

        [TestMethod]
        public void SignatureWebp_EstReconnue()
        {
            // "RIFF" + taille (4 octets) + "WEBP"
            using (var flux = Flux(0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,
                                   0x57, 0x45, 0x42, 0x50))
            {
                Assert.IsTrue(FichierSecurise.SignatureCorrespond(flux, ".webp"));
            }
        }

        [TestMethod]
        public void SignaturePdf_EstReconnue()
        {
            using (var flux = Flux(Encoding.ASCII.GetBytes("%PDF-1.7")))
            {
                Assert.IsTrue(FichierSecurise.SignatureCorrespond(flux, ".pdf"));
            }
        }

        [TestMethod]
        public void ScriptRenommeEnImage_EstRefuse()
        {
            // Le cœur du contrôle : une page ASP.NET renommée en .jpg passait
            // le test d'extension de l'ancien code.
            var contenu = Encoding.UTF8.GetBytes("<%@ Page Language=\"C#\" %><script runat=\"server\">");
            using (var flux = Flux(contenu))
            {
                Assert.IsFalse(FichierSecurise.SignatureCorrespond(flux, ".jpg"));
                Assert.IsFalse(FichierSecurise.SignatureCorrespond(flux, ".png"));
                Assert.IsFalse(FichierSecurise.SignatureCorrespond(flux, ".pdf"));
            }
        }

        [TestMethod]
        public void MauvaiseCorrespondanceEntreFormatEtExtension_EstRefusee()
        {
            // Un vrai PNG annoncé comme un JPEG : refusé aussi, l'extension
            // détermine le type servi par IIS.
            using (var flux = Flux(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
            {
                Assert.IsFalse(FichierSecurise.SignatureCorrespond(flux, ".jpg"));
            }
        }

        [TestMethod]
        public void ExtensionInconnue_EstRefusee()
        {
            using (var flux = Flux(0xFF, 0xD8, 0xFF))
            {
                Assert.IsFalse(FichierSecurise.SignatureCorrespond(flux, ".exe"));
                Assert.IsFalse(FichierSecurise.SignatureCorrespond(flux, ""));
            }
        }

        [TestMethod]
        public void FichierTropCourt_EstRefuse()
        {
            using (var flux = new MemoryStream(new byte[] { 0xFF, 0xD8 }))
            {
                Assert.IsFalse(FichierSecurise.SignatureCorrespond(flux, ".jpg"));
            }
        }

        [TestMethod]
        public void FluxNul_EstRefuse()
        {
            Assert.IsFalse(FichierSecurise.SignatureCorrespond(null, ".jpg"));
        }

        [TestMethod]
        public void SignatureCorrespond_RembobineLeFlux()
        {
            // L'appelant écrit le fichier juste après ce contrôle : le flux doit
            // être réutilisable depuis le début.
            using (var flux = Flux(0xFF, 0xD8, 0xFF, 0xE0))
            {
                FichierSecurise.SignatureCorrespond(flux, ".jpg");
                Assert.AreEqual(0, flux.Position);
            }
        }

        [TestMethod]
        public void ExtensionNormalisee_MetEnMinusculesEtIsoleLExtension()
        {
            Assert.AreEqual(".jpg", FichierSecurise.ExtensionNormalisee("PHOTO.JPG"));
            Assert.AreEqual(".png", FichierSecurise.ExtensionNormalisee("dossier/photo.PnG"));
            Assert.AreEqual(string.Empty, FichierSecurise.ExtensionNormalisee("sans-extension"));
            Assert.AreEqual(string.Empty, FichierSecurise.ExtensionNormalisee(null));
            Assert.AreEqual(string.Empty, FichierSecurise.ExtensionNormalisee("   "));
        }

        [TestMethod]
        public void ExtensionNormalisee_NeRetientQueLaDerniereExtension()
        {
            // « photo.jpg.aspx » doit être vu comme du .aspx, pas comme du .jpg.
            Assert.AreEqual(".aspx", FichierSecurise.ExtensionNormalisee("photo.jpg.aspx"));
        }
    }
}
