using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlert.Helpers;

namespace CamAlert.Tests
{
    /// <summary>
    /// Nettoyage des termes de recherche. Les requêtes sont paramétrées, donc
    /// non injectables, mais les jokers de LIKE restaient interprétés à
    /// l'intérieur du paramètre : une recherche sur « % » renvoyait tout.
    /// </summary>
    [TestClass]
    public class RechercheSqlTests
    {
        [TestMethod]
        public void TermeNormal_EstConserve()
        {
            Assert.AreEqual("Mbida", RechercheSql.Normaliser("Mbida"));
            Assert.AreEqual("Mbida", RechercheSql.Normaliser("  Mbida  "));
        }

        [TestMethod]
        public void TermeTropCourtOuVide_RetourneNull()
        {
            Assert.IsNull(RechercheSql.Normaliser(null));
            Assert.IsNull(RechercheSql.Normaliser(""));
            Assert.IsNull(RechercheSql.Normaliser("   "));
            Assert.IsNull(RechercheSql.Normaliser("a"));
        }

        [TestMethod]
        public void JokersRetires()
        {
            // « %Mbida% » renvoyait les mêmes résultats que « Mbida », mais
            // « % » seul renvoyait toute la table.
            Assert.AreEqual("Mbida", RechercheSql.Normaliser("%Mbida%"));
            Assert.AreEqual("Mbida", RechercheSql.Normaliser("M_bida"));
            Assert.AreEqual("abc", RechercheSql.EchapperJokers("a[b]c"));
            Assert.AreEqual("abc", RechercheSql.EchapperJokers("a^b%c_"));
        }

        [TestMethod]
        public void TermeUniquementComposeDeJokers_RetourneNull()
        {
            // « %% » renvoyait l'intégralité de la table.
            Assert.IsNull(RechercheSql.Normaliser("%%"));
            Assert.IsNull(RechercheSql.Normaliser("%"));
            Assert.IsNull(RechercheSql.Normaliser("___"));
        }

        [TestMethod]
        public void TermeTropLong_EstTronque()
        {
            var long_ = new string('x', 500);
            var resultat = RechercheSql.Normaliser(long_);

            Assert.IsNotNull(resultat);
            Assert.AreEqual(RechercheSql.LONGUEUR_MAXIMALE, resultat.Length);
        }
    }
}
