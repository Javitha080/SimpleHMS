using System.Data;
using System.Text.RegularExpressions;
using SimpleHMS.Repositories;
using Xunit;

namespace SimpleHMS.Tests.Integration
{
    // Bills are saved transactionally (header + items replaced on update) and
    // use unique sequential bill numbers. Creates its own patient/doctor rows
    // because Bills reference both.
    [Collection("Sql")]
    public class BillRepositoryTests
    {
        private readonly DatabaseFixture _fixture;

        public BillRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

        private static string Timestamp() => DateTime.Now.ToString("HHmmss") + Random.Shared.Next(10, 99);

        private static DataTable NewItems(params (string Name, string Type, int Qty, decimal Price)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("ItemName");
            table.Columns.Add("ItemType");
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("TotalPrice", typeof(decimal));

            foreach (var row in rows)
            {
                table.Rows.Add(row.Name, row.Type, row.Qty, row.Price, row.Qty * row.Price);
            }

            return table;
        }

        [RequiresDatabaseFact]
        public void GenerateBillNumber_UsesSequentialYearlyFormat()
        {
            _fixture.EnsureProvisioned();

            string number = BillRepository.GenerateBillNumber();

            Assert.Matches(@"^BILL-\d{4}-\d{4}$", number);
            Assert.StartsWith($"BILL-{DateTime.Now.Year}-", number, StringComparison.Ordinal);
        }

        [RequiresDatabaseFact]
        public void SaveBillUpdateDelete_Roundtrip()
        {
            _fixture.EnsureProvisioned();

            string stamp = Timestamp();
            int patientId = 0, doctorId = 0, billId = 0;

            try
            {
                PatientRepository.Insert($"IT Patient {stamp}", 35, "Male", "0777770001", null, null);
                DoctorRepository.Insert($"IT Doctor {stamp}", "Test Specialty", "0717770002", 500m);

                patientId = IdByName(PatientRepository.GetAll(), $"IT Patient {stamp}", "PatientID");
                doctorId = IdByName(DoctorRepository.GetAll(), $"IT Doctor {stamp}", "DoctorID");

                var header = new BillHeader
                {
                    BillNumber = BillRepository.GenerateBillNumber(),
                    BillDate = DateTime.Today,
                    PatientId = patientId,
                    DoctorId = doctorId,
                    ConsultationFee = 1500m,
                    MedicineFee = 250m,
                    TestFee = 0m,
                    OtherFee = 75m,
                    Discount = 100m,
                    TotalAmount = 1725m,
                    PaymentStatus = "Pending",
                    Notes = "integration test bill"
                };

                billId = BillRepository.SaveBill(0, header, NewItems(
                    ("Blood Test", "Lab Test", 1, 500m),
                    ("Consultation", "Consultation", 1, 1225m)));
                Assert.True(billId > 0);

                // Header roundtrip.
                DataTable details = BillRepository.GetBillDetails(billId);
                DataRow detail = Assert.Single(details.Rows.OfType<DataRow>());
                Assert.Equal(header.BillNumber, Convert.ToString(detail["BillNumber"]));
                Assert.Equal(1725m, Convert.ToDecimal(detail["TotalAmount"]));
                Assert.Equal("Pending", Convert.ToString(detail["PaymentStatus"]));
                Assert.Equal("integration test bill", Convert.ToString(detail["Notes"]));

                // Items roundtrip.
                Assert.Equal(2, BillRepository.GetBillItems(billId).Rows.Count);

                // Update replaces all items and rewrites the header.
                header.TotalAmount = 1000m;
                header.PaymentStatus = "Paid";
                header.PaymentMethod = "Cash";
                header.PaymentDate = DateTime.Today;
                Assert.Equal(billId, BillRepository.SaveBill(billId, header, NewItems(
                    ("Paracetamol", "Medicine", 1, 1000m))));

                DataTable itemsAfter = BillRepository.GetBillItems(billId);
                DataRow item = Assert.Single(itemsAfter.Rows.OfType<DataRow>());
                Assert.Equal("Paracetamol", Convert.ToString(item["ItemName"]));
                Assert.Equal(1000m, Convert.ToDecimal(item["TotalPrice"]));

                DataRow paid = Assert.Single(BillRepository.GetBillDetails(billId).Rows.OfType<DataRow>());
                Assert.Equal("Paid", Convert.ToString(paid["PaymentStatus"]));
                Assert.Equal("Cash", Convert.ToString(paid["PaymentMethod"]));
            }
            finally
            {
                if (billId > 0)
                {
                    BillRepository.DeleteBill(billId);

                    DataTable gone = BillRepository.GetBillDetails(billId);
                    Assert.Empty(gone.Rows);
                    Assert.Empty(BillRepository.GetBillItems(billId).Rows);
                }

                if (doctorId > 0)
                {
                    DoctorRepository.Delete(doctorId);
                }

                if (patientId > 0)
                {
                    PatientRepository.Delete(patientId);
                }
            }
        }

        private static int IdByName(DataTable table, string name, string idColumn)
        {
            DataRow row = table.Rows.OfType<DataRow>().Single(r => string.Equals(r["Name"], name));
            return Convert.ToInt32(row[idColumn]);
        }
    }
}
