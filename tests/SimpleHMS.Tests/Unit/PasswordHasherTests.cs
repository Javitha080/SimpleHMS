using System.Security.Cryptography;
using SimpleHMS.Security;
using Xunit;

namespace SimpleHMS.Tests.Unit
{
    // Format: "PBKDF2$<iterations>$<base64 salt>$<base64 hash>".
    public class PasswordHasherTests
    {
        [Fact]
        public void Hash_UsesPbkdf2FormatWithFourSegments()
        {
            string stored = PasswordHasher.Hash("s3cret");

            Assert.StartsWith("PBKDF2$", stored, StringComparison.Ordinal);
            Assert.Equal(4, stored.Split('$').Length);
        }

        [Fact]
        public void Hash_RecordsIterationCount()
        {
            string stored = PasswordHasher.Hash("s3cret");

            Assert.Equal("100000", stored.Split('$')[1]);
        }

        [Fact]
        public void Hash_IsSalted_SamePasswordProducesDifferentHash()
        {
            string first = PasswordHasher.Hash("s3cret");
            string second = PasswordHasher.Hash("s3cret");

            Assert.NotEqual(first, second);
            Assert.NotEqual(first.Split('$')[2], second.Split('$')[2]);   // salt segment
        }

        [Fact]
        public void Verify_AcceptsCorrectPassword()
        {
            string stored = PasswordHasher.Hash("s3cret");

            Assert.True(PasswordHasher.Verify("s3cret", stored));
        }

        [Fact]
        public void Verify_RejectsWrongPassword()
        {
            string stored = PasswordHasher.Hash("s3cret");

            Assert.False(PasswordHasher.Verify("s3crEt", stored));   // case matters
            Assert.False(PasswordHasher.Verify("", stored));
        }

        [Theory]
        [InlineData("")]
        [InlineData("plaintext")]
        [InlineData("PBKDF2$only-three")]
        [InlineData("MD5$100000$abc$def")]                       // wrong scheme
        [InlineData("PBKDF2$x$b64$hash")]                        // iterations not a number
        [InlineData("PBKDF2$100000$!!not-b64!!$also-not")]       // corrupt base64
        public void Verify_RejectsMalformedStoredValues(string stored)
        {
            Assert.False(PasswordHasher.Verify("s3cret", stored));
        }

        [Fact]
        public void IsHashFormat_DistinguishesHashedFromLegacyValues()
        {
            Assert.False(PasswordHasher.IsHashFormat("admin123"));
            Assert.False(PasswordHasher.IsHashFormat(""));
            Assert.True(PasswordHasher.IsHashFormat(PasswordHasher.Hash("whatever")));
        }

        [Fact]
        public void Verify_AcceptsHashStoredWithDifferentIterationCount()
        {
            // The stored format carries its own iteration count, so a value
            // hashed at another cost round must still verify.
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] derived;
            using (var pbkdf2 = new Rfc2898DeriveBytes("low", salt, 1, HashAlgorithmName.SHA256))
            {
                derived = pbkdf2.GetBytes(32);
            }

            string stored = $"PBKDF2$1${Convert.ToBase64String(salt)}${Convert.ToBase64String(derived)}";

            Assert.True(PasswordHasher.IsHashFormat(stored));
            Assert.True(PasswordHasher.Verify("low", stored));
            Assert.False(PasswordHasher.Verify("high", stored));
        }
    }
}
