using CamAlert.Helpers;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;

namespace CamAlert.Admin
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Si déjà connecté, rediriger directement
            if (Session["UserLogin"] != null)
                Response.Redirect("~/Admin/Dossiers.aspx");
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Veuillez saisir un login et un mot de passe.");
                return;
            }

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["CamAlertDB"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    string query = @"
                        SELECT u.UtilisateurID, u.Login, u.MotDePasse, r.Libelle AS Role, u.EstActif
                        FROM Utilisateurs u
                        INNER JOIN Roles r ON u.RoleID = r.RoleID
                        WHERE u.Login = @Login";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Login", login);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool estActif = Convert.ToBoolean(reader["EstActif"]);
                                if (!estActif)
                                {
                                    ShowError("Ce compte est désactivé. Contactez l'administrateur.");
                                    return;
                                }

                                string storedHash = reader["MotDePasse"].ToString().Trim();
                                string computedHash = ComputeSha256Hex(password);

                                if (storedHash.Equals(computedHash, StringComparison.OrdinalIgnoreCase))
                                {
                                    Session["UserLogin"] = reader["Login"].ToString();
                                    Session["UserRole"] = reader["Role"].ToString();
                                    Response.Redirect("~/Admin/Dossiers.aspx");
                                    return;
                                }
                            }
                        }
                    }
                }
                ShowError("Login ou mot de passe incorrect.");
            }
            catch (Exception ex)
            {
                ShowError("Erreur technique : " + ex.Message);
            }
        }

        private string ComputeSha256Hex(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                // Retourne une chaîne hexadécimale sans tirets, en majuscules
                return BitConverter.ToString(bytes).Replace("-", "").ToUpperInvariant();
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;      // On utilisera un Label pour le message
            pnlError.Visible = true;
        }
    }
}