using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlert.Helpers;

namespace CamAlert.Tests
{
    /// <summary>
    /// Règles de robustesse des mots de passe d'administration. Aucune n'était
    /// appliquée : un compte pouvait porter « admin » comme mot de passe.
    /// </summary>
    [TestClass]
    public class PolitiqueMotDePasseTests
    {
        [TestMethod]
        public void MotDePasseSolide_EstAccepte()
        {
            Assert.IsNull(PolitiqueMotDePasse.Valider("Koumassi#Prev2026"));
            Assert.IsNull(PolitiqueMotDePasse.Valider("tG7!muraille-verte"));
        }

        [TestMethod]
        public void PhraseDePasseLongue_EstAccepteeSansSymbole()
        {
            // 20 caractères ou plus : on n'exige plus trois familles, pour ne
            // pas pénaliser les phrases de passe.
            Assert.IsNull(PolitiqueMotDePasse.Valider("le classeur du bureau trois"));
        }

        [TestMethod]
        public void TropCourt_EstRefuse()
        {
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("Ab3!xyzab"));   // 9
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("Ab3!xyzabc"));  // 10
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("Ab3!xyzabcd")); // 11
        }

        [TestMethod]
        public void VideOuNul_EstRefuse()
        {
            Assert.IsNotNull(PolitiqueMotDePasse.Valider(null));
            Assert.IsNotNull(PolitiqueMotDePasse.Valider(""));
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("              "));
        }

        [TestMethod]
        public void TropLong_EstRefuse()
        {
            Assert.IsNotNull(PolitiqueMotDePasse.Valider(new string('a', 200)));
        }

        [TestMethod]
        public void UneSeuleFamilleDeCaracteres_EstRefusee()
        {
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("abcdefghijklm"));
        }

        [TestMethod]
        public void SuiteOuRepetition_EstRefusee()
        {
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("1234567890"));
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("aaaaaaaaaaaaaa"));
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("azertyuiop"));
        }

        [TestMethod]
        public void MotDePasseDeLaListeNoire_EstRefuse()
        {
            // Mot de passe initial du compte seedé : il ne doit pas pouvoir
            // être rechoisi lors du changement imposé à la première connexion.
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("CamAlert@2026"));
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("camalert@2026"));
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("MotDePasse123"));
        }

        [TestMethod]
        public void MotDePasseContenantLIdentifiant_EstRefuse()
        {
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("Admin#Koumassi2026", "admin"));
            Assert.IsNotNull(PolitiqueMotDePasse.Valider("jmbarga#Prev2026", "jmbarga"));
        }

        [TestMethod]
        public void DonneesPersonnellesTropCourtes_SontIgnorees()
        {
            // Un identifiant de deux lettres ne doit pas interdire la moitié
            // des mots de passe de la langue française.
            Assert.IsNull(PolitiqueMotDePasse.Valider("Koumassi#Prev2026", "ko"));
        }

        [TestMethod]
        public void EstValide_EstCoherentAvecValider()
        {
            Assert.IsTrue(PolitiqueMotDePasse.EstValide("Koumassi#Prev2026"));
            Assert.IsFalse(PolitiqueMotDePasse.EstValide("1234567890"));
        }
    }
}
