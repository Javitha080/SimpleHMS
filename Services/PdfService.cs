using System.Data;
using System.Net;
using System.Text;
using IronPdf;

namespace SimpleHMS.Services
{
    // Values captured from the UI for one PDF receipt. Strings carry the exact
    // display text; PdfService escapes the free-text fields while building HTML.
    public sealed class BillPdfModel
    {
        public string BillNumber = "";
        public string BillDate = "";
        public string DueDate = "";
        public string PatientName = "";
        public string PatientIdText = "";
        public string? PatientEmail;
        public string AgeGender = "";
        public string DoctorName = "";
        public string Department = "";
        public string ConsultationFee = "0.00";
        public string MedicineFee = "0.00";
        public string TestFee = "0.00";
        public string OtherFee = "0.00";
        public string Discount = "0.00";
        public string TotalAmount = "0.00";
        public string PaymentStatus = "Pending";
        public string PaymentMethod = "";
        public string PaymentDate = "-";   // "-" until the bill is Paid/Partial
        public DataTable Items = new();
        public string Notes = "";
    }

    // Renders bill PDFs from BillTemplate.html through IronPdf.
    public static class PdfService
    {
        public static string TemplatePath => Path.Combine(AppContext.BaseDirectory, "BillTemplate.html");

        // Builds the HTML from the template, renders it and cleans up the temp
        // file even on failure.
        public static void RenderBillToFile(BillPdfModel model, string pdfPath)
        {
            if (!File.Exists(TemplatePath))
                throw new FileNotFoundException("Bill template is missing next to the executable.", TemplatePath);

            string html = BuildBillHtml(model);
            string tempHtmlPath = Path.Combine(Path.GetTempPath(), $"Bill_{model.BillNumber}_temp.html");

            try
            {
                File.WriteAllText(tempHtmlPath, html);

                var renderer = new ChromePdfRenderer
                {
                    RenderingOptions = new ChromePdfRenderOptions
                    {
                        PaperSize = IronPdf.Rendering.PdfPaperSize.A4,
                        MarginTop = 20,
                        MarginBottom = 20,
                        MarginLeft = 20,
                        MarginRight = 20,
                        CreatePdfFormsFromHtml = true,
                        CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print
                    }
                };

                using var pdf = renderer.RenderHtmlFileAsPdf(tempHtmlPath);
                pdf.SaveAs(pdfPath);
            }
            finally
            {
                if (File.Exists(tempHtmlPath))
                    File.Delete(tempHtmlPath);
            }
        }

        // Fills BillTemplate.html with the bill data; free text is HTML-encoded.
        public static string BuildBillHtml(BillPdfModel model)
        {
            string html = File.ReadAllText(TemplatePath);

            return html
                .Replace("{BillNumber}", WebUtility.HtmlEncode(model.BillNumber))
                .Replace("{BillDate}", model.BillDate)
                .Replace("{DueDate}", model.DueDate)
                .Replace("{PatientName}", WebUtility.HtmlEncode(model.PatientName))
                .Replace("{PatientID}", WebUtility.HtmlEncode(model.PatientIdText))
                .Replace("{AgeGender}", WebUtility.HtmlEncode(model.AgeGender))
                .Replace("{DoctorName}", WebUtility.HtmlEncode(model.DoctorName))
                .Replace("{Department}", WebUtility.HtmlEncode(model.Department))
                .Replace("{ConsultationFee}", model.ConsultationFee)
                .Replace("{MedicineFee}", model.MedicineFee)
                .Replace("{TestFee}", model.TestFee)
                .Replace("{OtherFee}", model.OtherFee)
                .Replace("{Discount}", model.Discount)
                .Replace("{TotalAmount}", model.TotalAmount)
                .Replace("{PaymentStatus}", WebUtility.HtmlEncode(model.PaymentStatus))
                .Replace("{PaymentMethod}", WebUtility.HtmlEncode(model.PaymentMethod))
                .Replace("{PaymentDate}", model.PaymentDate)
                .Replace("{PaymentStatusClass}", model.PaymentStatus.ToLower())
                .Replace("{BillItems}", BuildBillItemsRows(model.Items))
                .Replace("{Notes}", BuildNotesHtml(model.Notes));
        }

        private static string BuildBillItemsRows(DataTable items)
        {
            var rows = new StringBuilder();

            if (items == null)
                return rows.ToString();

            foreach (DataRow row in items.Rows)
            {
                string category = row["ItemType"].ToString() ?? "";
                string categoryLower = category.ToLower();
                string categoryClass = "badge-other";

                if (categoryLower.Contains("consultation"))
                    categoryClass = "badge-consultation";
                else if (categoryLower.Contains("medicine"))
                    categoryClass = "badge-medicine";
                else if (categoryLower.Contains("test") || categoryLower.Contains("lab"))
                    categoryClass = "badge-test";

                rows.AppendLine("<tr>");
                rows.AppendLine($"    <td class=\"item-name\">{WebUtility.HtmlEncode(row["ItemName"].ToString())}</td>");
                rows.AppendLine($"    <td><span class=\"category-badge {categoryClass}\">{WebUtility.HtmlEncode(category)}</span></td>");
                rows.AppendLine($"    <td class=\"text-center\">{row["Quantity"]}</td>");
                rows.AppendLine($"    <td class=\"text-right amount\">${row["UnitPrice"]}</td>");
                rows.AppendLine($"    <td class=\"text-right amount\">${row["TotalPrice"]}</td>");
                rows.AppendLine("</tr>");
            }

            return rows.ToString();
        }

        private static string BuildNotesHtml(string? notes)
        {
            if (string.IsNullOrEmpty(notes))
                return "Please keep this invoice for your records.";

            return WebUtility.HtmlEncode(notes).Replace(Environment.NewLine, "<br/>");
        }
    }
}
