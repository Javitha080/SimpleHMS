using System.Security.Cryptography;

namespace SimpleHMS.Security
{
    // Hashing for the Users.Password column (PBKDF2 with HMAC-SHA256).
    // Stored format: "PBKDF2$<iterations>$<base64 salt>$<base64 hash>"
    // Hashes contain a random salt, so the same password produces a different
    // string every time; verification re-derives and compares in constant time.
    // Legacy plain-text rows store something that does not start with "PBKDF2$"
    // and are detected via IsHashFormat so login can migrate them transparently.
    public static class PasswordHasher
    {
        private const int SaltSize = 16;          // bytes
        private const int HashSize = 32;          // bytes
        private const int Iterations = 100_000;   // PBKDF2 rounds

        // Creates a hash string for a new or upgraded password.
        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Derive(password, salt, Iterations);
            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        // True when the stored value looks like our hashed format (not legacy plain text).
        public static bool IsHashFormat(string stored)
        {
            return !string.IsNullOrEmpty(stored)
                && stored.StartsWith("PBKDF2$", StringComparison.Ordinal)
                && stored.Split('$').Length == 4;
        }

        // Verifies a password against a stored hash value.
        public static bool Verify(string password, string stored)
        {
            string[] parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2")
                return false;

            if (!int.TryParse(parts[1], out int iterations) || iterations <= 0)
                return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual = Derive(password, salt, iterations);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(HashSize);
        }
    }
}
