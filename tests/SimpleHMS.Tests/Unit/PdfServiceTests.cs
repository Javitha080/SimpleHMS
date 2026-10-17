using System.Data;
using SimpleHMS.Services;
using Xunit;

namespace SimpleHMS.Tests.Unit
{
    // BillTemplate.html is copied into the test output by SimpleHMS.csproj
    // (transitively, because PdfService reads it from AppContext.BaseDirectory).
    // These tests exercise the token replacement only; no PDF is rendered, so
    // no IronPdf engine is needed.
    public class PdfServiceTests
    {
        private static readonly string[] TemplateTokens =
        {
            "BillNumber", "BillDate", "DueDate", "PatientName", "PatientID",
            "AgeGender", "DoctorName", "Department", "ConsultationFee",
            "MedicineFee", "TestFee", "OtherFee", "Discount", "TotalAmount",
            "PaymentStatus", "PaymentMethod", "PaymentDate", "PaymentStatusClass",
            "BillItems", "Notes"
        };

        private static BillPdfModel NewFilledModel() => new()
        {
            BillNumber = "BILL-2026-0001",
            BillDate = "17/10/2026",
            DueDate = "24/10/2026",
            PatientName = "Jane Doe",
            PatientIdText = "P-042",
            AgeGender = "34, Female",
            DoctorName = "Dr. Who",
            Department = "General Medicine",
            ConsultationFee = "1500.00",
            MedicineFee = "250.50",
            TestFee = "100.00",
            OtherFee = "75.00",
            Discount = "200.00",
            TotalAmount = "1725.50",
            PaymentStatus = "Paid",
            PaymentMethod = "Credit Card",
            PaymentDate = "17/10/2026",
            Notes = "Thank you."
        };

        private static DataTable NewItemsTable(params (string Name, string Type, int Qty, string Price)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("ItemName");
            table.Columns.Add("ItemType");
            table.Columns.Add("Quantity");
            table.Columns.Add("UnitPrice");
            table.Columns.Add("TotalPrice");

            foreach (var row in rows)
            {
                table.Rows.Add(row.Name, row.Type, row.Qty.ToString(), row.Price, row.Price);
            }

            return table;
        }

        [Fact]
        public void BuildBillHtml_FillsEveryTemplateToken()
        {
            string html = PdfService.BuildBillHtml(NewFilledModel());

            foreach (string token in TemplateTokens)
            {
                Assert.DoesNotContain($"{{{token}}}", html);
            }

            Assert.Contains("BILL-2026-0001", html);
            Assert.Contains("Jane Doe", html);
            Assert.Contains("1725.50", html);
        }

        [Fact]
        public void BuildBillHtml_EncodesFreeTextFields()
        {
            var model = NewFilledModel();
            model.PatientName = "<script>alert('x')</script>";
            model.Notes = "<b>bold</b>";

            string html = PdfService.BuildBillHtml(model);

            Assert.DoesNotContain("<script>", html);
            Assert.Contains("&lt;script&gt;", html);
            Assert.DoesNotContain("<b>bold</b>", html);
        }

        [Fact]
        public void BuildBillHtml_MapsItemCategoriesToBadgeClasses()
        {
            var model = NewFilledModel();
            model.Items = NewItemsTable(
                ("Consult", "Consultation", 1, "1500.00"),
                ("Paracetamol", "Medicine", 2, "125.25"),
                ("CBC", "Lab Test", 1, "300.00"),
                ("Wheelchair", "Referral", 1, "80.00"));

            string html = PdfService.BuildBillHtml(model);

            Assert.Contains("badge-consultation", html);
            Assert.Contains("badge-medicine", html);
            Assert.Contains("badge-test", html);
            Assert.Contains("badge-other", html);
            Assert.Contains(">Paracetamol<", html);
        }

        [Fact]
        public void BuildBillHtml_HandlesMissingItemsTable()
        {
            var model = NewFilledModel();
            model.Items = null!;

            string html = PdfService.BuildBillHtml(model);

            Assert.DoesNotContain("{BillItems}", html);
        }

        [Fact]
        public void BuildBillHtml_UsesDefaultNotesWhenEmpty()
        {
            var model = NewFilledModel();
            model.Notes = "";

            string html = PdfService.BuildBillHtml(model);

            Assert.Contains("Please keep this invoice for your records.", html);
        }

        [Fact]
        public void BuildBillHtml_EncodesAndBreaksMultiLineNotes()
        {
            var model = NewFilledModel();
            model.Notes = $"Line1{Environment.NewLine}<b>bold</b>";

            string html = PdfService.BuildBillHtml(model);

            Assert.Contains($"Line1<br/>&lt;b&gt;bold&lt;/b&gt;", html);
        }

        [Fact]
        public void BuildBillHtml_DerivesPaymentStatusClassFromStatus()
        {
            var model = NewFilledModel();
            model.PaymentStatus = "Partial";

            string html = PdfService.BuildBillHtml(model);

            Assert.Contains("partial", html);
            Assert.DoesNotContain("{PaymentStatusClass}", html);
        }

        [Fact]
        public void Template_IsDeployedNextToTheTestAssembly()
        {
            Assert.True(File.Exists(PdfService.TemplatePath),
                $"Expected the invoice template at {PdfService.TemplatePath}");
        }
    }
}
