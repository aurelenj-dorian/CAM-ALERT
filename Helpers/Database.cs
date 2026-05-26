using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CamAlert.Helpers
{
    public class Database
    {
        private static string connectionString =
            ConfigurationManager.ConnectionStrings["CamAlertDB"].ConnectionString;

        public static SqlConnection GetConnection()
        {
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            return con;
        }

        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        public static int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }
    }
}
