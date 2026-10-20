using System;
using System.Web.UI;

namespace CamAlert
{
    /// <summary>
    /// Page maîtresse du site.
    ///
    /// Elle exposait une seconde méthode <c>SearchAllDossiers</c> marquée
    /// <c>[WebMethod]</c>. Elle était inatteignable — les méthodes de page
    /// ASP.NET ne sont appelables que sur une page, pas sur une page maîtresse,
    /// et le JavaScript du menu appelle bien <c>/Default.aspx/SearchAllDossiers</c> —
    /// et de toute façon inutilisable, puisqu'elle renvoyait un
    /// <c>DataTable</c>, que le sérialiseur JSON d'ASP.NET refuse. Elle a été
    /// supprimée : une seule implémentation subsiste, dans Default.aspx.cs.
    /// </summary>
    public partial class Site : MasterPage
    {
        /// <summary>
        /// Indique au balisage s'il faut afficher les entrées de menu réservées
        /// aux administrateurs.
        /// </summary>
        public bool IsAdmin { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            IsAdmin = Session[PageBase.SESSION_ROLE] != null
                   && string.Equals(Session[PageBase.SESSION_ROLE].ToString(),
                                    "Administrateur",
                                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
