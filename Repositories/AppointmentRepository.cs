using System.Data;
using Microsoft.Data.SqlClient;

namespace SimpleHMS.Repositories
{
    public static class AppointmentRepository
    {
        public static DataTable GetAllDetailed()
        {
            return DB.GetData(
                @"SELECT a.AppointmentID, a.PatientID, a.DoctorID,
                         p.Name AS Patient, d.Name AS Doctor,
                         a.AppointmentDate AS Date, a.AppointmentTime AS Time
                  FROM Appointments a
                  INNER JOIN Patients p ON a.PatientID = p.PatientID
                  INNER JOIN Doctors d ON a.DoctorID = d.DoctorID
                  ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC");
        }

        public static DataTable GetForPatientDoctor(int patientId, int doctorId)
        {
            return DB.GetData(
                @"SELECT AppointmentID,
                         CONVERT(VARCHAR(10), AppointmentDate, 103) + ' ' +
                         CONVERT(VARCHAR(8), AppointmentTime, 108) AS AppointmentDateTime
                  FROM Appointments
                  WHERE PatientID = @PatientID AND DoctorID = @DoctorID
                  ORDER BY AppointmentDate DESC, AppointmentTime DESC",
                new SqlParameter("@PatientID", patientId),
                new SqlParameter("@DoctorID", doctorId));
        }

        public static int Insert(int patientId, int doctorId, DateTime date, TimeSpan time)
        {
            return DB.SetData(
                @"INSERT INTO Appointments (PatientID, DoctorID, AppointmentDate, AppointmentTime)
                  VALUES (@PatientID, @DoctorID, @Date, @Time)",
                new SqlParameter("@PatientID", patientId),
                new SqlParameter("@DoctorID", doctorId),
                new SqlParameter("@Date", date.Date),
                new SqlParameter("@Time", time));
        }

        // True when an identical patient/doctor/slot already exists, optionally
        // ignoring one appointment (the one being edited).
        public static bool ExistsDuplicate(int patientId, int doctorId, DateTime date, TimeSpan time, int excludeAppointmentId)
        {
            var result = DB.ExecuteScalar(
                @"SELECT COUNT(1)
                  FROM Appointments
                  WHERE PatientID = @PatientID
                    AND DoctorID = @DoctorID
                    AND AppointmentDate = @Date
                    AND AppointmentTime = @Time
                    AND AppointmentID <> @ExcludeID",
                new SqlParameter("@PatientID", patientId),
                new SqlParameter("@DoctorID", doctorId),
                new SqlParameter("@Date", date.Date),
                new SqlParameter("@Time", time),
                new SqlParameter("@ExcludeID", excludeAppointmentId));

            return Convert.ToInt32(result ?? 0) > 0;
        }

        // True when the doctor already holds any appointment in the exact slot,
        // optionally ignoring one appointment (the one being edited). Keeps one
        // doctor from being double-booked even for different patients.
        public static bool ExistsDoctorConflict(int doctorId, DateTime date, TimeSpan time, int excludeAppointmentId)
        {
            var result = DB.ExecuteScalar(
                @"SELECT COUNT(1)
                  FROM Appointments
                  WHERE DoctorID = @DoctorID
                    AND AppointmentDate = @Date
                    AND AppointmentTime = @Time
                    AND AppointmentID <> @ExcludeID",
                new SqlParameter("@DoctorID", doctorId),
                new SqlParameter("@Date", date.Date),
                new SqlParameter("@Time", time),
                new SqlParameter("@ExcludeID", excludeAppointmentId));

            return Convert.ToInt32(result ?? 0) > 0;
        }

        public static int Update(int appointmentId, int patientId, int doctorId, DateTime date, TimeSpan time)
        {
            return DB.SetData(
                @"UPDATE Appointments
                  SET PatientID = @PatientID, DoctorID = @DoctorID,
                      AppointmentDate = @Date, AppointmentTime = @Time
                  WHERE AppointmentID = @AppointmentID",
                new SqlParameter("@AppointmentID", appointmentId),
                new SqlParameter("@PatientID", patientId),
                new SqlParameter("@DoctorID", doctorId),
                new SqlParameter("@Date", date.Date),
                new SqlParameter("@Time", time));
        }

        public static int Delete(int appointmentId)
        {
            return DB.SetData(
                "DELETE FROM Appointments WHERE AppointmentID = @AppointmentID",
                new SqlParameter("@AppointmentID", appointmentId));
        }
    }
}
