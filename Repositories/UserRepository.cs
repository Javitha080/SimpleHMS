using System.Data;
using Microsoft.Data.SqlClient;
using SimpleHMS;
using SimpleHMS.Security;

namespace SimpleHMS.Repositories
{
    // Authentication against the Users table. Passwords are verified with
    // PBKDF2 (PasswordHasher); before this upgrade rows stored plain text,
    // so a successful legacy login is re-hashed in place (one-time migration).
    public static class UserRepository
    {
        private const string DefaultAdminUsername = "admin";
        private const string DefaultAdminPassword = "admin123";

        // Validates credentials and returns the user, or null when invalid.
        public static UserInfo? ValidateLogin(string username, string password)
        {
            var dt = DB.GetData(
                @"SELECT UserID, Username, Password, UserType
                  FROM Users WHERE Username = @Username AND IsActive = 1",
                new SqlParameter("@Username", username));

            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];
            string stored = row["Password"]?.ToString() ?? "";

            bool ok = PasswordHasher.IsHashFormat(stored)
                ? PasswordHasher.Verify(password, stored)
                : SlowEquals(stored, password);

            if (!ok)
                return null;

            // One-time transparent migration: replace the legacy plain-text
            // password with a salted PBKDF2 hash. Best-effort; a failure here
            // means the next login simply migrates again.
            if (!PasswordHasher.IsHashFormat(stored))
            {
                try
                {
                    DB.SetData(
                        "UPDATE Users SET Password = @Hash WHERE UserID = @UserID",
                        new SqlParameter("@Hash", PasswordHasher.Hash(password)),
                        new SqlParameter("@UserID", Convert.ToInt32(row["UserID"])));
                }
                catch (SqlException)
                {
                    // Migration failed; login still succeeds this time.
                }
            }

            var info = new UserInfo(
                Convert.ToInt32(row["UserID"]),
                row["Username"]?.ToString() ?? username,
                string.IsNullOrWhiteSpace(row["UserType"]?.ToString())
                    ? "Admin"
                    : row["UserType"].ToString()!);

            AppSession.Start(info);
            return info;
        }

        // Guarantees at least one active admin exists so a freshly created
        // database is never locked out. Creates admin / admin123 when the
        // table is empty. Safe to call repeatedly.
        public static void EnsureSeededAdminExists()
        {
            var result = DB.ExecuteScalar("SELECT COUNT(1) FROM Users WHERE IsActive = 1");

            if (Convert.ToInt32(result ?? 0) > 0)
                return;

            DB.SetData(
                @"INSERT INTO Users (Username, Password, UserType, IsActive)
                  VALUES (@Username, @Password, 'Admin', 1)",
                new SqlParameter("@Username", DefaultAdminUsername),
                new SqlParameter("@Password", PasswordHasher.Hash(DefaultAdminPassword)));
        }

        // Constant-time string comparison for legacy plain-text passwords so
        // timing cannot be used to probe characters before migration runs.
        private static bool SlowEquals(string a, string b)
        {
            ReadOnlySpan<char> x = a.AsSpan();
            ReadOnlySpan<char> y = b.AsSpan();

            int diff = x.Length ^ y.Length;
            int max = Math.Min(x.Length, y.Length);

            for (int i = 0; i < max; i++)
                diff |= x[i] ^ y[i];

            return diff == 0;
        }
    }
}
