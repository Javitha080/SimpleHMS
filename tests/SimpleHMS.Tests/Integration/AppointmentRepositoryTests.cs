using System.Data;
using System.Globalization;
using SimpleHMS.Repositories;
using Xunit;

namespace SimpleHMS.Tests.Integration
{
    // Covers the booking rules: exact duplicate detection and the
    // doctor-double-booking guard, plus update/delete lifecycle.
    [Collection("Sql")]
    public class AppointmentRepositoryTests
    {
        private readonly DatabaseFixture _fixture;

        public AppointmentRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

        private static string Timestamp() => DateTime.Now.ToString("HHmmss") + Random.Shared.Next(10, 99);

        private static readonly TimeSpan Slot = new(15, 30, 0);

        [RequiresDatabaseFact]
        public void BookingLifecycle_DuplicatesAndDoctorConflicts()
        {
            _fixture.EnsureProvisioned();

            string stamp = Timestamp();
            int patientId = 0, otherPatientId = 0, doctorId = 0, appointmentId = 0;

            try
            {
                PatientRepository.Insert($"IT Patient {stamp}A", 30, "Male", "0775550001", null, null);
                PatientRepository.Insert($"IT Patient {stamp}B", 31, "Female", "0775550002", null, null);
                DoctorRepository.Insert($"IT Doctor {stamp}", "Test Specialty", "0715550003", 100m);

                patientId = Id(PatientRepository.GetAll(), $"IT Patient {stamp}A", "PatientID");
                otherPatientId = Id(PatientRepository.GetAll(), $"IT Patient {stamp}B", "PatientID");
                doctorId = Id(DoctorRepository.GetAll(), $"IT Doctor {stamp}", "DoctorID");

                DateTime date = DateTime.Today.AddDays(7);

                Assert.Equal(1, AppointmentRepository.Insert(patientId, doctorId, date, Slot));

                // Exact duplicate: same patient, doctor, day and time.
                Assert.True(AppointmentRepository.ExistsDuplicate(patientId, doctorId, date, Slot, 0));
                Assert.False(AppointmentRepository.ExistsDuplicate(otherPatientId, doctorId, date, Slot, 0));

                // One doctor cannot hold the same slot for two patients.
                Assert.True(AppointmentRepository.ExistsDoctorConflict(doctorId, date, Slot, 0));
                Assert.False(AppointmentRepository.ExistsDuplicate(otherPatientId, doctorId, date, Slot, 0));

                // Single row comes back formatted "dd/MM/yyyy HH:mm:ss".
                DataTable slots = AppointmentRepository.GetForPatientDoctor(patientId, doctorId);
                DataRow row = Assert.Single(slots.Rows.OfType<DataRow>());
                appointmentId = Convert.ToInt32(row["AppointmentID"]);
                Assert.Equal(
                    date.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    Convert.ToString(row["AppointmentDateTime"], CultureInfo.InvariantCulture));

                // Moving the appointment frees the old slot.
                TimeSpan later = new(16, 45, 0);
                Assert.Equal(1, AppointmentRepository.Update(appointmentId, otherPatientId, doctorId, date, later));
                Assert.False(AppointmentRepository.ExistsDoctorConflict(doctorId, date, Slot, 0));
                Assert.True(AppointmentRepository.ExistsDoctorConflict(doctorId, date, later, 0));

                Assert.Equal(1, AppointmentRepository.Delete(appointmentId));
                appointmentId = 0;   // already gone; nothing to clean up in finally
            }
            finally
            {
                if (appointmentId > 0)
                {
                    AppointmentRepository.Delete(appointmentId);
                }

                if (doctorId > 0)
                {
                    DoctorRepository.Delete(doctorId);
                }

                if (patientId > 0)
                {
                    PatientRepository.Delete(patientId);
                }

                if (otherPatientId > 0)
                {
                    PatientRepository.Delete(otherPatientId);
                }
            }
        }

        private static int Id(DataTable table, string name, string idColumn)
        {
            DataRow row = table.Rows.OfType<DataRow>().Single(r => string.Equals(r["Name"], name));
            return Convert.ToInt32(row[idColumn]);
        }
    }
}
