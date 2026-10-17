using System.Data;
using Microsoft.Data.SqlClient;
using SimpleHMS.Services;

namespace SimpleHMS.Repositories
{
    // Values captured from the billing form for a single save operation.
    public class BillHeader
    {
        public string BillNumber = "";
        public DateTime BillDate;
        public int PatientId;
        public int DoctorId;
        public int? AppointmentId;
        public decimal ConsultationFee;
        public decimal MedicineFee;
        public decimal TestFee;
        public decimal OtherFee;
        public decimal Discount;
        public decimal TotalAmount;
        public string PaymentStatus = "Pending";
        public string? PaymentMethod;
        public DateTime? PaymentDate;
        public string? Notes;
    }

    public static class BillRepository
    {
        // Bill numbers are "BILL-YYYY-NNNN"; falls back to a timestamp if the sequence fails.
        public static string GenerateBillNumber()
        {
            int year = DateTime.Now.Year;
            try
            {
                var result = DB.ExecuteScalar(
                    @"SELECT MAX(CAST(SUBSTRING(BillNumber, 11, 4) AS INT))
                      FROM Bills WHERE BillNumber LIKE @Pattern",
                    new SqlParameter("@Pattern", $"BILL-{year}-%"));

                int next = result == null || result == DBNull.Value ? 1 : Convert.ToInt32(result) + 1;
                return $"BILL-{year}-{next:D4}";
            }
            catch
            {
                return $"BILL-{DateTime.Now:yyyyMMdd-HHmmss}";
            }
        }

        public static DataTable GetAllBills()
        {
            return DB.GetData(
                @"SELECT b.BillID, b.BillNumber, b.BillDate,
                         p.Name AS PatientName, d.Name AS DoctorName,
                         b.TotalAmount, b.PaymentStatus
                  FROM Bills b
                  JOIN Patients p ON b.PatientID = p.PatientID
                  JOIN Doctors d ON b.DoctorID = d.DoctorID
                  ORDER BY b.BillDate DESC");
        }

        public static DataTable GetBillDetails(int billId)
        {
            return DB.GetData(
                @"SELECT BillNumber, BillDate, PatientID, DoctorID, AppointmentID,
                         ConsultationFee, MedicineFee, TestFee, OtherFee,
                         Discount, TotalAmount, PaymentStatus, PaymentMethod, PaymentDate, Notes
                  FROM Bills WHERE BillID = @BillID",
                new SqlParameter("@BillID", billId));
        }

        public static DataTable GetPatientInfo(int patientId)
        {
            return DB.GetData(
                "SELECT PatientID, Name, Age, Gender, Email FROM Patients WHERE PatientID = @PatientID",
                new SqlParameter("@PatientID", patientId));
        }

        public static DataTable GetDoctorInfo(int doctorId)
        {
            return DB.GetData(
                "SELECT DoctorID, Name, Specialization FROM Doctors WHERE DoctorID = @DoctorID",
                new SqlParameter("@DoctorID", doctorId));
        }

        public static DataTable GetBillItems(int billId)
        {
            return DB.GetData(
                @"SELECT ItemID, ItemType, ItemName, Quantity, UnitPrice,
                         (Quantity * UnitPrice) AS TotalPrice
                  FROM BillItems WHERE BillID = @BillID",
                new SqlParameter("@BillID", billId));
        }

        // Saves the bill header plus items in one transaction. Items are replaced
        // (delete + insert) because TotalPrice is stored, so recalculated rows must be rewritten.
        // Pass billId = 0 to insert a new bill; otherwise the existing bill is updated.
        // New bills retry with a fresh number when two workstations grab the
        // same sequential BillNumber in the same instant.
        public static int SaveBill(int billId, BillHeader header, DataTable items)
        {
            bool isNew = billId <= 0;

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return SaveBillCore(billId, header, items);
                }
                catch (SqlException ex) when (isNew && attempt < 3 && (ex.Number == 2601 || ex.Number == 2627))
                {
                    // Unique-index race on BillNumber; pick another number and retry.
                    header.BillNumber = attempt >= 2
                        ? $"BILL-{DateTime.Now:yyyyMMdd-HHmmss}"   // timestamp cannot collide again
                        : GenerateBillNumber();
                }
            }
        }

        private static int SaveBillCore(int billId, BillHeader header, DataTable items)
        {
            using var conn = DB.GetConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                bool isNew = billId <= 0;

                string sql = isNew
                    ? @"INSERT INTO Bills (BillNumber, BillDate, PatientID, DoctorID, AppointmentID,
                            ConsultationFee, MedicineFee, TestFee, OtherFee, Discount, TotalAmount,
                            PaymentStatus, PaymentMethod, PaymentDate, Notes)
                       VALUES (@BillNumber, @BillDate, @PatientID, @DoctorID, @AppointmentID,
                            @ConsultationFee, @MedicineFee, @TestFee, @OtherFee, @Discount, @TotalAmount,
                            @PaymentStatus, @PaymentMethod, @PaymentDate, @Notes);
                       SELECT CAST(SCOPE_IDENTITY() AS INT);"
                    : @"UPDATE Bills SET
                            BillDate = @BillDate, PatientID = @PatientID, DoctorID = @DoctorID,
                            AppointmentID = @AppointmentID, ConsultationFee = @ConsultationFee,
                            MedicineFee = @MedicineFee, TestFee = @TestFee, OtherFee = @OtherFee,
                            Discount = @Discount, TotalAmount = @TotalAmount,
                            PaymentStatus = @PaymentStatus, PaymentMethod = @PaymentMethod,
                            PaymentDate = @PaymentDate, Notes = @Notes
                       WHERE BillID = @BillID;
                       SELECT @BillID;";

                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@BillNumber", header.BillNumber);
                cmd.Parameters.AddWithValue("@BillDate", header.BillDate);
                cmd.Parameters.AddWithValue("@PatientID", header.PatientId);
                cmd.Parameters.AddWithValue("@DoctorID", header.DoctorId);
                cmd.Parameters.AddWithValue("@AppointmentID",
                    header.AppointmentId.HasValue ? header.AppointmentId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ConsultationFee", header.ConsultationFee);
                cmd.Parameters.AddWithValue("@MedicineFee", header.MedicineFee);
                cmd.Parameters.AddWithValue("@TestFee", header.TestFee);
                cmd.Parameters.AddWithValue("@OtherFee", header.OtherFee);
                cmd.Parameters.AddWithValue("@Discount", header.Discount);
                cmd.Parameters.AddWithValue("@TotalAmount", header.TotalAmount);
                cmd.Parameters.AddWithValue("@PaymentStatus", header.PaymentStatus);
                cmd.Parameters.AddWithValue("@PaymentMethod",
                    string.IsNullOrEmpty(header.PaymentMethod) ? DBNull.Value : header.PaymentMethod);
                cmd.Parameters.AddWithValue("@PaymentDate",
                    header.PaymentDate.HasValue ? header.PaymentDate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(header.Notes) ? DBNull.Value : header.Notes);

                if (!isNew) cmd.Parameters.AddWithValue("@BillID", billId);

                billId = Convert.ToInt32(cmd.ExecuteScalar());

                using (var deleteCmd = new SqlCommand(
                    "DELETE FROM BillItems WHERE BillID = @BillID", conn, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@BillID", billId);
                    deleteCmd.ExecuteNonQuery();
                }

                foreach (DataRow row in items.Rows)
                {
                    using var insertCmd = new SqlCommand(
                        @"INSERT INTO BillItems (BillID, ItemName, ItemType, Quantity, UnitPrice, TotalPrice)
                          VALUES (@BillID, @ItemName, @ItemType, @Quantity, @UnitPrice, @TotalPrice)",
                        conn, transaction);
                    insertCmd.Parameters.AddWithValue("@BillID", billId);
                    insertCmd.Parameters.AddWithValue("@ItemName", row["ItemName"]);
                    insertCmd.Parameters.AddWithValue("@ItemType", row["ItemType"]);
                    insertCmd.Parameters.AddWithValue("@Quantity", row["Quantity"]);
                    insertCmd.Parameters.AddWithValue("@UnitPrice", row["UnitPrice"]);
                    insertCmd.Parameters.AddWithValue("@TotalPrice", row["TotalPrice"]);
                    insertCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return billId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                AppLogger.Error($"Bill save failed (billId {billId})", ex);
                throw;
            }
        }

        public static void DeleteBill(int billId)
        {
            using var conn = DB.GetConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                using (var cmd = new SqlCommand(
                    "DELETE FROM BillItems WHERE BillID = @BillID", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@BillID", billId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand(
                    "DELETE FROM Bills WHERE BillID = @BillID", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@BillID", billId);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                AppLogger.Error($"Bill delete failed (billId {billId})", ex);
                throw;
            }
        }

        public static List<string> GetPaymentMethods()
        {
            var methods = new List<string>
            {
                "Cash", "Credit Card", "Debit Card", "Insurance", "Bank Transfer",
                "Mobile Payment", "Check", "Online Payment", "Gift Card", "Other"
            };

            var dt = DB.GetData(
                @"SELECT DISTINCT PaymentMethod FROM Bills
                  WHERE PaymentMethod IS NOT NULL
                    AND PaymentMethod NOT IN ('Cash', 'Credit Card', 'Debit Card', 'Insurance', 'Bank Transfer',
                                              'Mobile Payment', 'Check', 'Online Payment', 'Gift Card', 'Other')");

            foreach (DataRow row in dt.Rows)
            {
                string? method = row["PaymentMethod"]?.ToString();
                if (!string.IsNullOrEmpty(method) && !methods.Contains(method))
                    methods.Add(method);
            }

            return methods;
        }
    }
}
