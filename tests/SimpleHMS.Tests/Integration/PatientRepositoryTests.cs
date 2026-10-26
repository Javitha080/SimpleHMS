using System.Data;
using SimpleHMS.Repositories;
using Xunit;

namespace SimpleHMS.Tests.Integration
{
    [Collection("Sql")]
    public class PatientRepositoryTests
    {
        private readonly DatabaseFixture _fixture;

        public PatientRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

        private static string UniqueName() => $"IT Patient {Random.Shared.Next(100000, 999999)}";

        [RequiresDatabaseFact]
        public void GetAll_ReturnsSeededDemoPatients()
        {
            _fixture.EnsureProvisioned();

            Assert.True(PatientRepository.GetAll().Rows.Count > 0);
        }

        [RequiresDatabaseFact]
        public void InsertUpdateDelete_Roundtrip()
        {
            _fixture.EnsureProvisioned();

            string name = UniqueName();
            int patientId = 0;

            try
            {
                // Insert with full optional fields.
                Assert.Equal(1, PatientRepository.Insert(
                    name, 42, "Male", "0771234567", "it.patient@example.com", "12 Test Lane"));

                DataTable all = PatientRepository.GetAll();
                DataRow inserted = all.Rows.OfType<DataRow>()
                    .Single(row => string.Equals(row["Name"], name));

                patientId = Convert.ToInt32(inserted["PatientID"]);
                Assert.Equal("it.patient@example.com", inserted["Email"]);
                Assert.Equal("12 Test Lane", inserted["Address"]);

                // Update clears optional fields and changes the rest.
                Assert.Equal(1, PatientRepository.Update(
                    patientId, name, 43, "Female", "0779999999", null, "5 Updated Road"));

                DataTable after = PatientRepository.GetAll();
                DataRow updated = after.Rows.OfType<DataRow>()
                    .Single(row => Convert.ToInt32(row["PatientID"]) == patientId);

                Assert.Equal(43, Convert.ToInt32(updated["Age"]));
                Assert.Equal("Female", updated["Gender"]);
                Assert.Equal(DBNull.Value, updated["Email"]);
                Assert.Equal("5 Updated Road", updated["Address"]);
            }
            finally
            {
                if (patientId > 0)
                {
                    PatientRepository.Delete(patientId);
                }
            }

            // Delete removed the row again.
            DataTable remaining = PatientRepository.GetAll();
            Assert.DoesNotContain(remaining.Rows.OfType<DataRow>(),
                row => Convert.ToInt32(row["PatientID"]) == patientId);
        }
    }
}
