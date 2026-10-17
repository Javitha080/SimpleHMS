using Microsoft.Data.SqlClient;
using SimpleHMS.Services;
using System.Text;

namespace SimpleHMS.Data
{
    // Owns database-install knowledge for the app:
    //   * IsServerReachable  - talks to [master] to distinguish "SQL down" from "DB missing"
    //   * ProvisionDatabase  - runs Database\Setup_SimpleHospitalDB.sql (GO-batch executor)
    //                          to create the database, tables, indexes and seed rows
    // The setup script itself is idempotent, so provisioning is safe to repeat.
    public static class DatabaseManager
    {
        public const string DatabaseName = "SimpleHospitalDB";

        // Master DB connection for CREATE DATABASE / existence checks.
        private static string MasterConnectionString =>
            new SqlConnectionStringBuilder(DB.ConnectionString) { InitialCatalog = "master" }.ConnectionString;

        public static string SetupScriptPath =>
            Path.Combine(AppContext.BaseDirectory, "Database", "Setup_SimpleHospitalDB.sql");

        public static bool SetupScriptExists => File.Exists(SetupScriptPath);

        // Opens a connection to [master] and returns the server error when it fails.
        public static bool IsServerReachable(out string error)
        {
            try
            {
                using var connection = new SqlConnection(MasterConnectionString);
                connection.Open();
                error = "";
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("SQL Server is not reachable (master)", ex);
                error = ex.Message;
                return false;
            }
        }

        // True when the SimpleHospitalDB database exists on the server.
        public static bool DatabaseExists()
        {
            try
            {
                using var connection = new SqlConnection(MasterConnectionString);
                connection.Open();
                using var command = new SqlCommand(
                    "SELECT CONVERT(INT, DB_ID(@Name))", connection);
                command.Parameters.AddWithValue("@Name", DatabaseName);
                return Convert.ToInt32(command.ExecuteScalar() ?? 0) != 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("DatabaseExists check failed", ex);
                return false;
            }
        }

        // Creates the database (when missing) and runs the idempotent setup script.
        public static (bool Success, string Message) ProvisionDatabase()
        {
            if (!SetupScriptExists)
                return (false, $"Setup script not found:\n{SetupScriptPath}\n\nRun it manually with SSMS or sqlcmd.");

            try
            {
                using var connection = new SqlConnection(MasterConnectionString);
                connection.Open();

                using (var create = new SqlCommand(
                    $"IF DB_ID(N'{DatabaseName}') IS NULL CREATE DATABASE [{DatabaseName}]", connection))
                {
                    create.ExecuteNonQuery();
                }

                int batchCount = 0;
                foreach (string batch in SplitBatches(File.ReadAllText(SetupScriptPath)))
                {
                    if (string.IsNullOrWhiteSpace(batch))
                        continue;

                    using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
                    command.ExecuteNonQuery();
                    batchCount++;
                }

                AppLogger.Info($"Database provisioned OK ({batchCount} SQL batches).");
                return (true, $"Database '{DatabaseName}' is ready ({batchCount} SQL batches executed).");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Database provisioning failed", ex);
                return (false, ex.Message);
            }
        }

        // Splits a .sql file into executable batches at GO lines (GO can carry
        // arguments, so an exact whole-line match is required).
        internal static IEnumerable<string> SplitBatches(string sql)
        {
            var batches = new List<string>();
            var current = new StringBuilder();

            foreach (string rawLine in sql.Replace("\r\n", "\n").Split('\n'))
            {
                if (rawLine.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    batches.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.AppendLine(rawLine);
                }
            }

            if (current.Length > 0)
                batches.Add(current.ToString());

            return batches;
        }
    }
}
