using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlert.Helpers;

namespace CamAlert.Tests
{
    /// <summary>
    /// Limiteur de tentatives. Sans lui, la connexion d'administration
    /// acceptait un nombre illimité d'essais, et le formulaire de contact
    /// envoyait un email à quatre adresses à chaque soumission.
    /// </summary>
    [TestClass]
    public class LimiteurTentativesTests
    {
        [TestInitialize]
        public void Initialiser()
        {
            // L'état est statique : on repart d'une ardoise propre.
            LimiteurTentatives.ReinitialiserTout();
        }

        [TestCleanup]
        public void Nettoyer()
        {
            LimiteurTentatives.ReinitialiserTout();
        }

        [TestMethod]
        public void CleInconnue_NEstPasBloquee()
        {
            var cle = LimiteurTentatives.Cle("connexion", "admin", "10.0.0.1");
            Assert.IsFalse(LimiteurTentatives.EstBloque(cle));
        }

        [TestMethod]
        public void TentativesSousLeSeuil_NeBloquentPas()
        {
            var cle = LimiteurTentatives.Cle("connexion", "admin", "10.0.0.1");

            for (int i = 0; i < LimiteurTentatives.MAX_CONNEXION - 1; i++)
                Assert.IsFalse(LimiteurTentatives.CompterTentative(cle));

            Assert.IsFalse(LimiteurTentatives.EstBloque(cle));
        }

        [TestMethod]
        public void TentativesAuSeuil_Bloquent()
        {
            var cle = LimiteurTentatives.Cle("connexion", "admin", "10.0.0.1");

            for (int i = 0; i < LimiteurTentatives.MAX_CONNEXION; i++)
                LimiteurTentatives.CompterTentative(cle);

            int attente;
            Assert.IsTrue(LimiteurTentatives.EstBloque(cle, out attente));
            Assert.IsTrue(attente > 0, "un délai d'attente doit être renvoyé");
        }

        [TestMethod]
        public void Reussite_RemetLeCompteurAZero()
        {
            var cle = LimiteurTentatives.Cle("connexion", "admin", "10.0.0.1");

            LimiteurTentatives.CompterTentative(cle);
            LimiteurTentatives.CompterTentative(cle);
            LimiteurTentatives.Reussite(cle);

            Assert.AreEqual(LimiteurTentatives.MAX_CONNEXION,
                            LimiteurTentatives.TentativesRestantes(cle));
            Assert.IsFalse(LimiteurTentatives.EstBloque(cle));
        }

        [TestMethod]
        public void BlocageIsoleParAdresseIp()
        {
            // Bloquer sur le seul identifiant permettrait de verrouiller le
            // compte d'un agent depuis n'importe où.
            var attaquant = LimiteurTentatives.Cle("connexion", "admin", "203.0.113.9");
            var legitime = LimiteurTentatives.Cle("connexion", "admin", "10.0.0.1");

            for (int i = 0; i < LimiteurTentatives.MAX_CONNEXION; i++)
                LimiteurTentatives.CompterTentative(attaquant);

            Assert.IsTrue(LimiteurTentatives.EstBloque(attaquant));
            Assert.IsFalse(LimiteurTentatives.EstBloque(legitime));
        }

        [TestMethod]
        public void PorteesIndependantes()
        {
            // Épuiser le quota de signalements ne doit pas bloquer la connexion.
            var signalement = LimiteurTentatives.CleIp("signalement", "10.0.0.1");
            var connexion = LimiteurTentatives.CleIp("connexion", "10.0.0.1");

            for (int i = 0; i < LimiteurTentatives.MAX_SIGNALEMENT; i++)
                LimiteurTentatives.CompterTentative(signalement,
                                                    LimiteurTentatives.MAX_SIGNALEMENT);

            Assert.IsTrue(LimiteurTentatives.EstBloque(signalement));
            Assert.IsFalse(LimiteurTentatives.EstBloque(connexion));
        }

        [TestMethod]
        public void Cle_InsensibleALaCasseDeLIdentifiant()
        {
            Assert.AreEqual(
                LimiteurTentatives.Cle("connexion", "Admin", "10.0.0.1"),
                LimiteurTentatives.Cle("connexion", "admin", "10.0.0.1"));
        }

        [TestMethod]
        public void TentativesRestantes_Decroit()
        {
            var cle = LimiteurTentatives.Cle("connexion", "admin", "10.0.0.1");

            Assert.AreEqual(LimiteurTentatives.MAX_CONNEXION,
                            LimiteurTentatives.TentativesRestantes(cle));

            LimiteurTentatives.CompterTentative(cle);
            Assert.AreEqual(LimiteurTentatives.MAX_CONNEXION - 1,
                            LimiteurTentatives.TentativesRestantes(cle));
        }

        [TestMethod]
        public void MessageBlocage_EstLisible()
        {
            Assert.IsTrue(LimiteurTentatives.MessageBlocage(30).Contains("minute"));
            Assert.IsTrue(LimiteurTentatives.MessageBlocage(600).Contains("10"));
        }
    }
}
