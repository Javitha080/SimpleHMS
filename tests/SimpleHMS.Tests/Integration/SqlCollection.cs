using SimpleHMS.Data;
using Xunit;

namespace SimpleHMS.Tests.Integration
{
    // Is the configured SQL Server reachable? Computed once per test process.
    // When it is down, integration facts Skip with a clear message instead of
    // failing — so `dotnet test` works on machines without SQL Server.
    public static class SqlAvailability
    {
        private static readonly Lazy<bool> Reachable = new(
            () => DatabaseManager.IsServerReachable(out _),
            LazyThreadSafetyMode.ExecutionAndPublication);

        public static bool IsAvailable => Reachable.Value;
    }

    public sealed class RequiresDatabaseFactAttribute : FactAttribute
    {
        public RequiresDatabaseFactAttribute()
        {
            if (!SqlAvailability.IsAvailable)
                Skip = "SQL Server not reachable; set HMS_CONNECTION_STRING to run integration tests";
        }
    }

    // Run-time fixture: creates SimpleHospitalDB (from the bundled setup script)
    // once for all tests that actually execute. The script is idempotent.
    public sealed class DatabaseFixture
    {
        private static readonly Lazy<(bool Success, string Message)> Provision = new(
            () => DatabaseManager.ProvisionDatabase(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        public void EnsureProvisioned()
        {
            (bool success, string message) attempt = Provision.Value;

            if (!attempt.success)
                throw new InvalidOperationException(
                    $"Could not provision {DatabaseManager.DatabaseName}: {attempt.message}");
        }
    }

    [CollectionDefinition("Sql")]
    public sealed class SqlCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}
