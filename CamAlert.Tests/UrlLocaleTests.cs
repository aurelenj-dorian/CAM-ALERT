using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlert.Helpers;

namespace CamAlert.Tests
{
    /// <summary>
    /// Validation des URL de retour. Sans elle, la page de connexion
    /// redirigeait vers n'importe quelle valeur passée en paramètre, ce qui
    /// permettait d'envoyer un agent authentifié vers une fausse page.
    /// </summary>
    [TestClass]
    public class UrlLocaleTests
    {
        [TestMethod]
        public void CheminRelatif_EstAccepte()
        {
            Assert.IsTrue(UrlLocale.EstLocale("/Admin/Dossiers.aspx"));
            Assert.IsTrue(UrlLocale.EstLocale("/Admin/Dossiers.aspx?id=3"));
            Assert.IsTrue(UrlLocale.EstLocale("~/Admin/Dossiers.aspx"));
        }

        [TestMethod]
        public void UrlAbsolue_EstRefusee()
        {
            Assert.IsFalse(UrlLocale.EstLocale("https://site-pirate.example/connexion"));
            Assert.IsFalse(UrlLocale.EstLocale("http://site-pirate.example"));
        }

        [TestMethod]
        public void ProtocoleRelatif_EstRefuse()
        {
            // « //hote » est résolu par le navigateur vers un autre domaine.
            Assert.IsFalse(UrlLocale.EstLocale("//site-pirate.example/connexion"));
            Assert.IsFalse(UrlLocale.EstLocale("/\\site-pirate.example"));
        }

        [TestMethod]
        public void SchemaDangereux_EstRefuse()
        {
            Assert.IsFalse(UrlLocale.EstLocale("javascript:alert(1)"));
            Assert.IsFalse(UrlLocale.EstLocale("data:text/html,<script>"));
        }

        [TestMethod]
        public void ValeurVide_EstRefusee()
        {
            Assert.IsFalse(UrlLocale.EstLocale(null));
            Assert.IsFalse(UrlLocale.EstLocale(""));
            Assert.IsFalse(UrlLocale.EstLocale("   "));
        }

        [TestMethod]
        public void CheminSansBarreOblique_EstRefuse()
        {
            // Une valeur relative sans « / » initial peut sortir du site une
            // fois résolue par le navigateur.
            Assert.IsFalse(UrlLocale.EstLocale("site-pirate.example"));
            Assert.IsFalse(UrlLocale.EstLocale("Admin/Dossiers.aspx"));
            Assert.IsFalse(UrlLocale.EstLocale("~"));
        }

        [TestMethod]
        public void OuParDefaut_RetombeSurLaValeurDeSecours()
        {
            Assert.AreEqual("/Admin/Dossiers.aspx",
                UrlLocale.OuParDefaut("https://ailleurs.example", "/Admin/Dossiers.aspx"));

            Assert.AreEqual("/Admin/Signalements.aspx",
                UrlLocale.OuParDefaut("/Admin/Signalements.aspx", "/Admin/Dossiers.aspx"));
        }
    }
}
