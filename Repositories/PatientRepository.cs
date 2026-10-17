using System.Data;
using Microsoft.Data.SqlClient;

namespace SimpleHMS.Repositories
{
    public static class PatientRepository
    {
        public static DataTable GetAll()
        {
            return DB.GetData(
                "SELECT PatientID, Name, Age, Gender, Phone, Email, Address FROM Patients ORDER BY Name");
        }

        public static DataTable GetAllForPicker()
        {
            return DB.GetData("SELECT PatientID, Name FROM Patients ORDER BY Name");
        }

        public static int Insert(string name, int age, string gender, string phone, string? email, string? address)
        {
            return DB.SetData(
                @"INSERT INTO Patients (Name, Age, Gender, Phone, Email, Address)
                  VALUES (@Name, @Age, @Gender, @Phone, @Email, @Address)",
                new SqlParameter("@Name", name),
                new SqlParameter("@Age", age),
                new SqlParameter("@Gender", gender),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@Email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email),
                new SqlParameter("@Address", (object?)address ?? DBNull.Value));
        }

        public static int Update(int patientId, string name, int age, string gender, string phone, string? email, string? address)
        {
            return DB.SetData(
                @"UPDATE Patients
                  SET Name = @Name, Age = @Age, Gender = @Gender, Phone = @Phone, Email = @Email, Address = @Address
                  WHERE PatientID = @PatientID",
                new SqlParameter("@PatientID", patientId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Age", age),
                new SqlParameter("@Gender", gender),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@Email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email),
                new SqlParameter("@Address", (object?)address ?? DBNull.Value));
        }

        public static int Delete(int patientId)
        {
            return DB.SetData(
                "DELETE FROM Patients WHERE PatientID = @PatientID",
                new SqlParameter("@PatientID", patientId));
        }
    }
}
