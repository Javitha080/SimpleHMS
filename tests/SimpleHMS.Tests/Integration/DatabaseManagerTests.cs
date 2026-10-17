using SimpleHMS.Data;
using Xunit;

namespace SimpleHMS.Tests.Integration
{
    [Collection("Sql")]
    public class DatabaseManagerTests
    {
        private readonly DatabaseFixture _fixture;

        public DatabaseManagerTests(DatabaseFixture fixture) => _fixture = fixture;

        [RequiresDatabaseFact]
        public void IsServerReachable_ReportsReachableServer()
        {
            bool reachable = DatabaseManager.IsServerReachable(out string error);

            Assert.True(reachable, $"Expected the server to be reachable, got: {error}");
            Assert.Equal(string.Empty, error);
        }

        [RequiresDatabaseFact]
        public void SetupScript_IsShippedWithTheApp()
        {
            _fixture.EnsureProvisioned();

            Assert.True(DatabaseManager.SetupScriptExists,
                $"Expected {DatabaseManager.SetupScriptPath} to exist");
        }

        [RequiresDatabaseFact]
        public void ProvisionDatabase_IsIdempotent()
        {
            _fixture.EnsureProvisioned();   // first provisioning (creates if missing)

            var (ok, message) = DatabaseManager.ProvisionDatabase();   // second pass over an existing DB

            Assert.True(ok, message);
            Assert.True(DatabaseManager.DatabaseExists());
        }
    }
}
