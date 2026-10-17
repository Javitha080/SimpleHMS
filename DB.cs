using System.Data;
using Microsoft.Data.SqlClient;
using SimpleHMS.Services;

namespace SimpleHMS
{
    // Thin ADO.NET helper. All queries are parameterized; call sites pass SqlParameters.
    public static class DB
    {
        // Local SQL Server Express by default; override with the HMS_CONNECTION_STRING
        // environment variable for other deployments (CI, LAN server, etc.)
        private static readonly string conn =
            Environment.GetEnvironmentVariable("HMS_CONNECTION_STRING")
            ?? "Data Source=localhost\\SQLEXPRESS;Initial Catalog=SimpleHospitalDB;Integrated Security=True;TrustServerCertificate=True;";

        // Exposed for DatabaseManager (provisioning, reachability checks).
        internal static string ConnectionString => conn;

        public static SqlConnection GetConnection() => new SqlConnection(conn);

        public static DataTable GetData(string query, params SqlParameter[] parameters)
        {
            try
            {
                using var con = new SqlConnection(conn);
                using var cmd = new SqlCommand(query, con);
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                AppLogger.Error("DB.GetData failed", ex);
                throw;
            }
        }

        public static int SetData(string query, params SqlParameter[] parameters)
        {
            try
            {
                using var con = new SqlConnection(conn);
                using var cmd = new SqlCommand(query, con);
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppLogger.Error("DB.SetData failed", ex);
                throw;
            }
        }

        public static object? ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            try
            {
                using var con = new SqlConnection(conn);
                using var cmd = new SqlCommand(query, con);
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                con.Open();
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                AppLogger.Error("DB.ExecuteScalar failed", ex);
                throw;
            }
        }
    }
}
