using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlert.Helpers;

namespace CamAlert.Tests
{
    /// <summary>
    /// Hachage PBKDF2 et compatibilité avec les anciens hash SHA-256, qui
    /// doivent rester vérifiables tant que tous les comptes n'ont pas été
    /// migrés.
    /// </summary>
    [TestClass]
    public class HacheurMotDePasseTests
    {
        // Coût réduit : les tests ne mesurent pas la résistance au calcul.
        private const int ITERATIONS_TEST = 1000;

        [TestMethod]
        public void Hacher_ProduitLeFormatAttendu()
        {
            var hash = HacheurMotDePasse.Hacher("Koumassi#Prev2026", ITERATIONS_TEST);

            var parties = hash.Split('$');
            Assert.AreEqual(5, parties.Length, "cinq segments séparés par $");
            Assert.AreEqual("pbkdf2", parties[0]);
            Assert.AreEqual("sha256", parties[1]);
            Assert.AreEqual("1000", parties[2]);
        }

        [TestMethod]
        public void Hacher_TientDansLaColonneDeLaBase()
        {
            // Utilisateurs.MotDePasse est un NVARCHAR(256).
            var hash = HacheurMotDePasse.Hacher("Koumassi#Prev2026");
            Assert.IsTrue(hash.Length <= 256,
                "hash de " + hash.Length + " caractères, colonne de 256");
        }

        [TestMethod]
        public void Hacher_DeuxFois_DonneDeuxResultatsDifferents()
        {
            // Sel aléatoire : deux comptes partageant le même mot de passe ne
            // doivent pas porter le même hash.
            var a = HacheurMotDePasse.Hacher("MemeMotDePasse", ITERATIONS_TEST);
            var b = HacheurMotDePasse.Hacher("MemeMotDePasse", ITERATIONS_TEST);
            Assert.AreNotEqual(a, b);
            Assert.IsTrue(HacheurMotDePasse.Verifier("MemeMotDePasse", a));
            Assert.IsTrue(HacheurMotDePasse.Verifier("MemeMotDePasse", b));
        }

        [TestMethod]
        public void Verifier_BonMotDePasse_RetourneTrue()
        {
            var hash = HacheurMotDePasse.Hacher("Koumassi#Prev2026", ITERATIONS_TEST);
            Assert.IsTrue(HacheurMotDePasse.Verifier("Koumassi#Prev2026", hash));
        }

        [TestMethod]
        public void Verifier_MauvaisMotDePasse_RetourneFalse()
        {
            var hash = HacheurMotDePasse.Hacher("Koumassi#Prev2026", ITERATIONS_TEST);
            Assert.IsFalse(HacheurMotDePasse.Verifier("Koumassi#Prev2025", hash));
        }

        [TestMethod]
        public void Verifier_SensibleALaCasse()
        {
            var hash = HacheurMotDePasse.Hacher("Phrase-De-Passe", ITERATIONS_TEST);
            Assert.IsFalse(HacheurMotDePasse.Verifier("phrase-de-passe", hash));
        }

        [TestMethod]
        public void Verifier_HashCorrompuOuVide_RetourneFalseSansException()
        {
            Assert.IsFalse(HacheurMotDePasse.Verifier("x", "pbkdf2$sha256$abc$def"));
            Assert.IsFalse(HacheurMotDePasse.Verifier("x", "pbkdf2$sha256$1000$!!!$!!!"));
            Assert.IsFalse(HacheurMotDePasse.Verifier("x", "pbkdf2$sha512$1000$AAAA$AAAA"));
            Assert.IsFalse(HacheurMotDePasse.Verifier("x", "n'importe quoi"));
            Assert.IsFalse(HacheurMotDePasse.Verifier("x", null));
            Assert.IsFalse(HacheurMotDePasse.Verifier("x", ""));
            Assert.IsFalse(HacheurMotDePasse.Verifier("x", "    "));
        }

        // ── Compatibilité avec l'ancien format ──────────────

        [TestMethod]
        public void Verifier_AncienHashSha256Majuscule_RestAccepte()
        {
            // Admin/Login.aspx.cs produisait de l'hexadécimal en majuscules.
            var ancien = HacheurMotDePasse.HacherLegacySha256("CamAlert@2026");
            Assert.AreEqual(64, ancien.Length);
            Assert.AreEqual(ancien, ancien.ToUpperInvariant(), "majuscules attendues");

            Assert.IsTrue(HacheurMotDePasse.Verifier("CamAlert@2026", ancien));
            Assert.IsFalse(HacheurMotDePasse.Verifier("AutreChose", ancien));
        }

        [TestMethod]
        public void Verifier_AncienHashSha256Minuscule_RestAccepte()
        {
            // Une base alimentée à la main a pu recevoir des minuscules.
            var ancien = HacheurMotDePasse.HacherLegacySha256("CamAlert@2026")
                                          .ToLowerInvariant();
            Assert.IsTrue(HacheurMotDePasse.Verifier("CamAlert@2026", ancien));
        }

        [TestMethod]
        public void NecessiteRehachage_AncienFormat_RetourneTrue()
        {
            var ancien = HacheurMotDePasse.HacherLegacySha256("CamAlert@2026");
            Assert.IsTrue(HacheurMotDePasse.NecessiteRehachage(ancien));
        }

        [TestMethod]
        public void NecessiteRehachage_CoutInsuffisant_RetourneTrue()
        {
            var faible = HacheurMotDePasse.Hacher("Koumassi#Prev2026", ITERATIONS_TEST);
            Assert.IsTrue(HacheurMotDePasse.NecessiteRehachage(faible));
        }

        [TestMethod]
        public void NecessiteRehachage_FormatCourant_RetourneFalse()
        {
            var courant = HacheurMotDePasse.Hacher("Koumassi#Prev2026");
            Assert.IsFalse(HacheurMotDePasse.NecessiteRehachage(courant));
        }

        [TestMethod]
        public void EstFormatLegacy_DistingueLesDeuxFormats()
        {
            Assert.IsTrue(HacheurMotDePasse.EstFormatLegacy(
                HacheurMotDePasse.HacherLegacySha256("abc")));
            Assert.IsFalse(HacheurMotDePasse.EstFormatLegacy(
                HacheurMotDePasse.Hacher("abc", ITERATIONS_TEST)));

            // 64 caractères, mais non hexadécimaux
            Assert.IsFalse(HacheurMotDePasse.EstFormatLegacy(new string('z', 64)));
        }

        [TestMethod]
        public void HashDuScriptDeSeed_CorrespondAuMotDePasseInitial()
        {
            // Hash réellement présent dans Database/02_DonneesReference.sql pour
            // le compte « admin ». Si ce test casse, c'est que le format stocké
            // et le code de vérification ont divergé — donc que plus personne ne
            // peut se connecter après une installation neuve.
            const string hashSeed =
                "pbkdf2$sha256$210000$Ll6OoLYW5sYfPEA9H9UicQ==" +
                "$wvG81n78Z1UK5peyALbH9jQJTLRmYWWp9U3SfzDJRhc=";

            Assert.IsTrue(HacheurMotDePasse.Verifier("CamAlert@2026", hashSeed));
            Assert.IsFalse(HacheurMotDePasse.Verifier("CamAlert@2025", hashSeed));
        }

        [TestMethod]
        public void ComparaisonConstanteHex_IgnoreLaCasse()
        {
            Assert.IsTrue(HacheurMotDePasse.ComparaisonConstanteHex("ABCD", "abcd"));
            Assert.IsFalse(HacheurMotDePasse.ComparaisonConstanteHex("ABCD", "ABCE"));
            Assert.IsFalse(HacheurMotDePasse.ComparaisonConstanteHex("ABCD", "ABC"));
            Assert.IsFalse(HacheurMotDePasse.ComparaisonConstanteHex("ABCD", null));
            Assert.IsFalse(HacheurMotDePasse.ComparaisonConstanteHex(null, "ABCD"));
        }
    }
}
