using System.Data;
using Microsoft.Data.SqlClient;

namespace SimpleHMS.Repositories
{
    public static class DoctorRepository
    {
        public static DataTable GetAll()
        {
            return DB.GetData(
                "SELECT DoctorID, Name, Specialization, Phone, Fee FROM Doctors ORDER BY Name");
        }

        public static DataTable GetAllForPicker()
        {
            return DB.GetData("SELECT DoctorID, Name FROM Doctors ORDER BY Name");
        }

        public static int Insert(string name, string specialization, string phone, decimal fee)
        {
            return DB.SetData(
                @"INSERT INTO Doctors (Name, Specialization, Phone, Fee)
                  VALUES (@Name, @Specialization, @Phone, @Fee)",
                new SqlParameter("@Name", name),
                new SqlParameter("@Specialization", specialization),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@Fee", fee));
        }

        public static int Update(int doctorId, string name, string specialization, string phone, decimal fee)
        {
            return DB.SetData(
                @"UPDATE Doctors
                  SET Name = @Name, Specialization = @Specialization, Phone = @Phone, Fee = @Fee
                  WHERE DoctorID = @DoctorID",
                new SqlParameter("@DoctorID", doctorId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Specialization", specialization),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@Fee", fee));
        }

        public static int Delete(int doctorId)
        {
            return DB.SetData(
                "DELETE FROM Doctors WHERE DoctorID = @DoctorID",
                new SqlParameter("@DoctorID", doctorId));
        }

        public static decimal? GetFee(int doctorId)
        {
            var result = DB.ExecuteScalar(
                "SELECT Fee FROM Doctors WHERE DoctorID = @DoctorID",
                new SqlParameter("@DoctorID", doctorId));

            if (result == null || result == DBNull.Value) return null;
            return Convert.ToDecimal(result);
        }
    }
}
