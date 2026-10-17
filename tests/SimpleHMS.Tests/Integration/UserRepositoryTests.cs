using SimpleHMS;
using SimpleHMS.Repositories;
using Xunit;

namespace SimpleHMS.Tests.Integration
{
    [Collection("Sql")]
    public class UserRepositoryTests
    {
        private readonly DatabaseFixture _fixture;

        public UserRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

        [RequiresDatabaseFact]
        public void SeededAdmin_LogsWithDefaultPassword()
        {
            _fixture.EnsureProvisioned();
            UserRepository.EnsureSeededAdminExists();

            UserInfo? user = UserRepository.ValidateLogin("admin", "admin123");

            Assert.NotNull(user);
            Assert.Equal("admin", user!.Username);
            Assert.Equal("Admin", user.UserType);
        }

        [RequiresDatabaseFact]
        public void AdminLogin_RejectsWrongPassword()
        {
            _fixture.EnsureProvisioned();
            UserRepository.EnsureSeededAdminExists();

            Assert.Null(UserRepository.ValidateLogin("admin", "wrong-password"));
        }

        [RequiresDatabaseFact]
        public void AdminLogin_RejectsUnknownUser()
        {
            _fixture.EnsureProvisioned();

            Assert.Null(UserRepository.ValidateLogin("no-such-user-it", "whatever"));
        }
    }
}
