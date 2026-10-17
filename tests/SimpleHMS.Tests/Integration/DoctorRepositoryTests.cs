using System.Data;
using SimpleHMS.Repositories;
using Xunit;

namespace SimpleHMS.Tests.Integration
{
    [Collection("Sql")]
    public class DoctorRepositoryTests
    {
        private readonly DatabaseFixture _fixture;

        public DoctorRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

        private static string UniqueName() => $"IT Doctor {Random.Shared.Next(100000, 999999)}";

        [RequiresDatabaseFact]
        public void GetAll_ReturnsSeededDemoDoctors()
        {
            _fixture.EnsureProvisioned();

            Assert.True(DoctorRepository.GetAll().Rows.Count > 0);
            Assert.True(DoctorRepository.GetAllForPicker().Rows.Count > 0);
        }

        [RequiresDatabaseFact]
        public void InsertGetFeeUpdateDelete_Roundtrip()
        {
            _fixture.EnsureProvisioned();

            string name = UniqueName();
            int doctorId = 0;

            try
            {
                Assert.Equal(1, DoctorRepository.Insert(name, "Test Specialty", "0712223333", 2500.50m));

                DataRow inserted = DoctorRepository.GetAll().Rows.OfType<DataRow>()
                    .Single(row => string.Equals(row["Name"], name));

                doctorId = Convert.ToInt32(inserted["DoctorID"]);
                Assert.Equal(2500.50m, DoctorRepository.GetFee(doctorId));
                Assert.Null(DoctorRepository.GetFee(-4711));   // no such doctor

                Assert.Equal(1, DoctorRepository.Update(
                    doctorId, name, "Test Specialty II", "0712224444", 3100.00m));

                DataRow updated = DoctorRepository.GetAll().Rows.OfType<DataRow>()
                    .Single(row => Convert.ToInt32(row["DoctorID"]) == doctorId);

                Assert.Equal("Test Specialty II", updated["Specialization"]);
                Assert.Equal("0712224444", updated["Phone"]);
                Assert.Equal(3100.00m, Convert.ToDecimal(updated["Fee"]));
            }
            finally
            {
                if (doctorId > 0)
                {
                    DoctorRepository.Delete(doctorId);
                }
            }
        }
    }
}
