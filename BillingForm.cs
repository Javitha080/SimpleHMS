using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using SimpleHMS.Repositories;
using SimpleHMS.Services;

namespace SimpleHMS
{
    // Billing management: bill header + itemized bill items, saved through
    // BillRepository, and printable as a PDF built from BillTemplate.html.
    public partial class BillingForm : Form
    {
        private int selectedBillID;
        private DataTable billItemsTable;
        private ToolTip tooltip;

        public BillingForm()
        {
            InitializeComponent();
            InitializeBillItemsTable();
            LoadPatients();
            LoadDoctors();
            LoadBills();
            LoadPaymentMethods();

            cmbPaymentStatus.Items.AddRange(new string[] { "Pending", "Partial", "Paid", "Cancelled", "Refunded" });
            cmbPaymentStatus.SelectedIndex = 0;

            // Fee and quantity fields stay numeric (guards typing and pasting).
            txtQuantity.KeyPress += Validators.AllowDigits_KeyPress;
            txtUnitPrice.KeyPress += Validators.AllowDecimal_KeyPress;
            txtTotalPrice.KeyPress += Validators.AllowDecimal_KeyPress;
            txtConsultationFee.KeyPress += Validators.AllowDecimal_KeyPress;
            txtMedicineFee.KeyPress += Validators.AllowDecimal_KeyPress;
            txtTestFee.KeyPress += Validators.AllowDecimal_KeyPress;
            txtOtherFee.KeyPress += Validators.AllowDecimal_KeyPress;
            txtDiscount.KeyPress += Validators.AllowDecimal_KeyPress;

            cmbItemType.Items.AddRange(new string[] { "Consultation", "Medicine", "Test", "Procedure", "Equipment", "Room", "Service", "Other" });
            cmbItemType.SelectedIndexChanged += CmbItemType_SelectedIndexChanged;
            cmbItemType.SelectedIndex = 0;

            dtpBillDate.Value = DateTime.Now;
            dtpPaymentDate.Value = DateTime.Now;
        }

        private void InitializeBillItemsTable()
        {
            billItemsTable = new DataTable();
            billItemsTable.Columns.Add("ItemID", typeof(int));
            billItemsTable.Columns.Add("ItemName", typeof(string));
            billItemsTable.Columns.Add("ItemType", typeof(string));
            billItemsTable.Columns.Add("Quantity", typeof(int));
            billItemsTable.Columns.Add("UnitPrice", typeof(decimal));
            billItemsTable.Columns.Add("TotalPrice", typeof(decimal));
        }

        #region Data loading

        private void LoadPatients()
        {
            try
            {
                cmbPatient.DataSource = PatientRepository.GetAllForPicker();
                cmbPatient.DisplayMember = "Name";
                cmbPatient.ValueMember = "PatientID";
                cmbPatient.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patients: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDoctors()
        {
            try
            {
                cmbDoctor.DataSource = DoctorRepository.GetAllForPicker();
                cmbDoctor.DisplayMember = "Name";
                cmbDoctor.ValueMember = "DoctorID";
                cmbDoctor.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading doctors: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAppointments()
        {
            try
            {
                if (!int.TryParse(cmbPatient.SelectedValue?.ToString(), out int patientId) ||
                    !int.TryParse(cmbDoctor.SelectedValue?.ToString(), out int doctorId))
                {
                    cmbAppointment.DataSource = null;
                    return;
                }

                cmbAppointment.DataSource = AppointmentRepository.GetForPatientDoctor(patientId, doctorId);
                cmbAppointment.DisplayMember = "AppointmentDateTime";
                cmbAppointment.ValueMember = "AppointmentID";
                cmbAppointment.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadBills()
        {
            try
            {
                dgvBills.DataSource = BillRepository.GetAllBills();

                if (dgvBills.Columns.Count > 0)
                {
                    dgvBills.Columns["BillID"].Visible = false;
                    dgvBills.Columns["BillNumber"].HeaderText = "Bill #";
                    dgvBills.Columns["BillDate"].HeaderText = "Date";
                    dgvBills.Columns["PatientName"].HeaderText = "Patient";
                    dgvBills.Columns["DoctorName"].HeaderText = "Doctor";
                    dgvBills.Columns["TotalAmount"].HeaderText = "Total";
                    dgvBills.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
                    dgvBills.Columns["PaymentStatus"].HeaderText = "Status";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading bills: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadBillDetails(int billId)
        {
            try
            {
                DataTable dt = BillRepository.GetBillDetails(billId);
                if (dt.Rows.Count == 0) return;

                DataRow row = dt.Rows[0];

                txtBillNumber.Text = row["BillNumber"].ToString();
                // BillDate is nullable in the schema; a NULL row must not crash the picker.
                dtpBillDate.Value = row["BillDate"] == DBNull.Value
                    ? DateTime.Now
                    : Convert.ToDateTime(row["BillDate"]);

                if (cmbPatient.Items.Count > 0 && !string.IsNullOrEmpty(cmbPatient.ValueMember))
                    cmbPatient.SelectedValue = Convert.ToInt32(row["PatientID"]);

                if (cmbDoctor.Items.Count > 0 && !string.IsNullOrEmpty(cmbDoctor.ValueMember))
                    cmbDoctor.SelectedValue = Convert.ToInt32(row["DoctorID"]);

                if (row["AppointmentID"] != DBNull.Value && cmbAppointment.Items.Count > 0
                    && !string.IsNullOrEmpty(cmbAppointment.ValueMember))
                    cmbAppointment.SelectedValue = Convert.ToInt32(row["AppointmentID"]);

                txtConsultationFee.Text = Convert.ToDecimal(row["ConsultationFee"]).ToString("0.00");
                txtMedicineFee.Text = Convert.ToDecimal(row["MedicineFee"]).ToString("0.00");
                txtTestFee.Text = Convert.ToDecimal(row["TestFee"]).ToString("0.00");
                txtOtherFee.Text = Convert.ToDecimal(row["OtherFee"]).ToString("0.00");
                txtDiscount.Text = Convert.ToDecimal(row["Discount"]).ToString("0.00");
                txtTotalAmount.Text = Convert.ToDecimal(row["TotalAmount"]).ToString("0.00");

                if (cmbPaymentStatus.Items.Count == 0)
                {
                    cmbPaymentStatus.Items.AddRange(new string[] { "Pending", "Partial", "Paid", "Cancelled", "Refunded" });
                }
                cmbPaymentStatus.Text = row["PaymentStatus"].ToString();

                if (row["PaymentMethod"] != DBNull.Value)
                    cmbPaymentMethod.Text = row["PaymentMethod"].ToString();
                else
                    cmbPaymentMethod.SelectedIndex = -1;

                dtpPaymentDate.Value = row["PaymentDate"] != DBNull.Value
                    ? Convert.ToDateTime(row["PaymentDate"])
                    : DateTime.Now;

                txtNotes.Text = row["Notes"] == DBNull.Value ? "" : row["Notes"].ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading bill details: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadBillItems(int billId)
        {
            try
            {
                billItemsTable.Clear();
                billItemsTable.Merge(BillRepository.GetBillItems(billId));
                dgvBillItems.DataSource = billItemsTable;

                if (dgvBillItems.Columns.Contains("ItemID"))
                {
                    dgvBillItems.Columns["ItemID"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading bill items: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPaymentMethods()
        {
            try
            {
                cmbPaymentMethod.Items.Clear();
                foreach (string method in BillRepository.GetPaymentMethods())
                {
                    cmbPaymentMethod.Items.Add(method);
                }

                if (cmbPaymentMethod.Items.Count > 0)
                {
                    cmbPaymentMethod.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading payment methods: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Fee calculations

        // Recomputes the fee breakdown from bill items; adds the doctor's
        // consultation fee when no consultation item is present.
        private void UpdateFeeSummary()
        {
            try
            {
                decimal medicineFee = 0, testFee = 0, consultationFee = 0, otherFee = 0, totalBeforeDiscount = 0;

                if (billItemsTable != null && billItemsTable.Rows.Count > 0)
                {
                    foreach (DataRow row in billItemsTable.Rows)
                    {
                        decimal itemTotal = Convert.ToDecimal(row["TotalPrice"]);
                        string itemType = row["ItemType"].ToString()?.ToLower() ?? "";

                        switch (itemType)
                        {
                            case "medicine":
                                medicineFee += itemTotal;
                                break;
                            case "test":
                            case "lab":
                                testFee += itemTotal;
                                break;
                            case "consultation":
                                consultationFee += itemTotal;
                                break;
                            default:
                                otherFee += itemTotal;
                                break;
                        }

                        totalBeforeDiscount += itemTotal;
                    }
                }

                // The doctor's default fee supplements the items only when no
                // consultation item exists yet; consultation items already
                // contributed their totals inside the loop (adding the sum
                // again double-counted them).
                decimal doctorFee = 0;
                if (consultationFee == 0 &&
                    int.TryParse(cmbDoctor.SelectedValue?.ToString(), out int doctorId))
                {
                    doctorFee = DoctorRepository.GetFee(doctorId) ?? 0;
                    consultationFee = doctorFee;
                }

                totalBeforeDiscount += doctorFee;

                decimal discount = decimal.TryParse(txtDiscount.Text, out decimal d) ? d : 0;
                decimal totalAfterDiscount = totalBeforeDiscount - discount;

                txtMedicineFee.Text = medicineFee.ToString("0.00");
                txtTestFee.Text = testFee.ToString("0.00");
                txtConsultationFee.Text = consultationFee.ToString("0.00");
                txtOtherFee.Text = otherFee.ToString("0.00");
                txtTotalAmount.Text = totalAfterDiscount.ToString("0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating fee summary: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtQuantity_TextChanged(object sender, EventArgs e)
        {
            CalculateItemTotalPrice();
        }

        private void TxtUnitPrice_TextChanged(object sender, EventArgs e)
        {
            CalculateItemTotalPrice();
        }

        private void CalculateItemTotalPrice()
        {
            int quantity = int.TryParse(txtQuantity.Text, out int q) ? q : 0;
            decimal unitPrice = decimal.TryParse(txtUnitPrice.Text, out decimal up) ? up : 0;
            txtTotalPrice.Text = (quantity * unitPrice).ToString("F2");
        }

        #endregion

        #region Combo handlers

        private void CmbPatient_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadAppointments();
        }

        private void CmbDoctor_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadAppointments();
            UpdateFeeSummary();
        }

        private void CmbPaymentStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isPaid = cmbPaymentStatus.Text == "Paid" || cmbPaymentStatus.Text == "Partial";

            if (isPaid)
            {
                dtpPaymentDate.Value = DateTime.Now;
            }

            dtpPaymentDate.Enabled = isPaid;
            cmbPaymentMethod.Enabled = isPaid;
        }

        private void CmbItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Default the unit price to the doctor's fee for consultation items.
            if (cmbItemType.SelectedItem?.ToString() != "Consultation" ||
                !int.TryParse(cmbDoctor.SelectedValue?.ToString(), out int doctorId))
            {
                return;
            }

            try
            {
                decimal? fee = DoctorRepository.GetFee(doctorId);
                if (fee.HasValue)
                {
                    txtUnitPrice.Text = fee.Value.ToString("0.00");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting doctor fee: " + ex.Message);
            }
        }

        #endregion

        #region Grid handlers

        private void DgvBills_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                DataGridViewRow row = dgvBills.Rows[e.RowIndex];
                selectedBillID = Convert.ToInt32(row.Cells["BillID"].Value);

                billItemsTable.Clear();
                LoadBillDetails(selectedBillID);
                LoadBillItems(selectedBillID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error selecting bill: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvBillItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                DataGridViewRow row = dgvBillItems.Rows[e.RowIndex];
                txtItemName.Text = row.Cells["ItemName"].Value?.ToString() ?? "";
                cmbItemType.Text = row.Cells["ItemType"].Value?.ToString() ?? "";
                txtQuantity.Text = row.Cells["Quantity"].Value?.ToString() ?? "";
                txtUnitPrice.Text = row.Cells["UnitPrice"].Value?.ToString() ?? "";
                txtTotalPrice.Text = row.Cells["TotalPrice"].Value?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error selecting item: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Bill actions

        private void BtnNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            txtBillNumber.Text = BillRepository.GenerateBillNumber();
            cmbPaymentStatus.SelectedIndex = 0;
            cmbPatient.Focus();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            txtBillNumber.Text = BillRepository.GenerateBillNumber();
            cmbPaymentStatus.SelectedIndex = 0;
            cmbPaymentMethod.SelectedIndex = 0;
            MessageBox.Show("Form cleared successfully!", "Clear", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateBillForm()) return;

            try
            {
                bool isPaid = cmbPaymentStatus.Text == "Paid" || cmbPaymentStatus.Text == "Partial";

                var header = new BillHeader
                {
                    BillNumber = txtBillNumber.Text.Trim(),
                    BillDate = dtpBillDate.Value,
                    PatientId = Convert.ToInt32(cmbPatient.SelectedValue),
                    DoctorId = Convert.ToInt32(cmbDoctor.SelectedValue),
                    AppointmentId = cmbAppointment.SelectedValue != null
                        && int.TryParse(cmbAppointment.SelectedValue.ToString(), out int apptId)
                        ? apptId : null,
                    ConsultationFee = decimal.TryParse(txtConsultationFee.Text, out decimal cf) ? cf : 0,
                    MedicineFee = decimal.TryParse(txtMedicineFee.Text, out decimal mf) ? mf : 0,
                    TestFee = decimal.TryParse(txtTestFee.Text, out decimal tf) ? tf : 0,
                    OtherFee = decimal.TryParse(txtOtherFee.Text, out decimal of) ? of : 0,
                    Discount = decimal.TryParse(txtDiscount.Text, out decimal disc) ? disc : 0,
                    TotalAmount = decimal.TryParse(txtTotalAmount.Text, out decimal total) ? total : 0,
                    PaymentStatus = cmbPaymentStatus.Text,
                    PaymentMethod = cmbPaymentMethod.SelectedIndex >= 0 ? cmbPaymentMethod.Text : null,
                    PaymentDate = isPaid ? dtpPaymentDate.Value : null,
                    Notes = txtNotes.Text
                };

                selectedBillID = BillRepository.SaveBill(selectedBillID, header, billItemsTable);
                txtBillNumber.Text = header.BillNumber;   // a retry may have regenerated the number

                MessageBox.Show("Bill saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBills();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving bill: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedBillID == 0)
            {
                MessageBox.Show("Please select a bill to delete.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this bill?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            try
            {
                BillRepository.DeleteBill(selectedBillID);

                MessageBox.Show("Bill deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadBills();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting bill: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateBillForm()
        {
            if (string.IsNullOrEmpty(txtBillNumber.Text))
            {
                MessageBox.Show("Bill number is required. Click New to generate one.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmbPatient.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a patient.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmbDoctor.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a doctor.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmbPaymentStatus.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a payment status.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // A discount larger than every fee would store a negative total.
            decimal sumBeforeDiscount =
                ParseAmount(txtConsultationFee) + ParseAmount(txtMedicineFee) +
                ParseAmount(txtTestFee) + ParseAmount(txtOtherFee);

            if (ParseAmount(txtDiscount) > sumBeforeDiscount)
            {
                MessageBox.Show("Discount cannot be larger than the fees charged.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDiscount.Focus();
                return false;
            }

            return true;
        }

        private static decimal ParseAmount(TextBox box)
        {
            return decimal.TryParse(box.Text, out decimal value) ? value : 0;
        }

        #endregion

        #region Bill items

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (!ValidateItemFields()) return;

            try
            {
                int quantity = int.Parse(txtQuantity.Text);
                decimal unitPrice = decimal.Parse(txtUnitPrice.Text);

                DataRow newRow = billItemsTable.NewRow();
                newRow["ItemID"] = 0;
                newRow["ItemName"] = txtItemName.Text.Trim();
                newRow["ItemType"] = cmbItemType.Text;
                newRow["Quantity"] = quantity;
                newRow["UnitPrice"] = unitPrice;
                newRow["TotalPrice"] = quantity * unitPrice;

                billItemsTable.Rows.Add(newRow);
                dgvBillItems.DataSource = billItemsTable;

                ClearItemFields();
                UpdateFeeSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding item: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdateItem_Click(object sender, EventArgs e)
        {
            if (dgvBillItems.CurrentRow == null)
            {
                MessageBox.Show("Please select an item to update.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateItemFields()) return;

            try
            {
                int quantity = int.Parse(txtQuantity.Text);
                decimal unitPrice = decimal.Parse(txtUnitPrice.Text);

                DataRow row = ((DataRowView)dgvBillItems.CurrentRow.DataBoundItem).Row;
                row["ItemName"] = txtItemName.Text.Trim();
                row["ItemType"] = cmbItemType.Text;
                row["Quantity"] = quantity;
                row["UnitPrice"] = unitPrice;
                row["TotalPrice"] = quantity * unitPrice;

                dgvBillItems.DataSource = null;
                dgvBillItems.DataSource = billItemsTable;

                ClearItemFields();
                UpdateFeeSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating item: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvBillItems.CurrentRow == null)
            {
                MessageBox.Show("Please select an item to remove.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                DataRowView itemRowView = (DataRowView)dgvBillItems.CurrentRow.DataBoundItem;
                billItemsTable.Rows.Remove(itemRowView.Row);
                dgvBillItems.DataSource = null;
                dgvBillItems.DataSource = billItemsTable;

                ClearItemFields();
                UpdateFeeSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error removing item: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateItemFields()
        {
            if (string.IsNullOrEmpty(txtItemName.Text))
            {
                MessageBox.Show("Please enter an item name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmbItemType.SelectedIndex < 0)
            {
                MessageBox.Show("Please select an item type.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Please enter a valid quantity (positive number).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!decimal.TryParse(txtUnitPrice.Text, out decimal unitPrice) || unitPrice < 0)
            {
                MessageBox.Show("Please enter a valid unit price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void ClearItemFields()
        {
            txtItemName.Clear();
            cmbItemType.SelectedIndex = -1;
            txtQuantity.Text = "1";
            txtUnitPrice.Text = "0.00";
            txtTotalPrice.Text = "0.00";
        }

        #endregion

        #region PDF and email

        private void BtnPrintPDF_Click(object sender, EventArgs e)
        {
            if (selectedBillID == 0)
            {
                MessageBox.Show("Please select a bill to print.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"Bill_{txtBillNumber.Text}.pdf"
                };

                if (saveDialog.ShowDialog() != DialogResult.OK) return;

                PdfService.RenderBillToFile(BuildBillPdfModel(), saveDialog.FileName);

                if (MessageBox.Show("PDF bill generated successfully! Do you want to open it?", "Success",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Bill PDF generation failed", ex);
                MessageBox.Show("Error generating bill: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Gathers the currently shown bill's display values for PdfService.
        private BillPdfModel BuildBillPdfModel()
        {
            int patientId = int.TryParse(cmbPatient.SelectedValue?.ToString(), out int pid) ? pid : 0;
            int doctorId = int.TryParse(cmbDoctor.SelectedValue?.ToString(), out int did) ? did : 0;

            string? patientEmail = null;
            string ageGender = "";
            if (patientId > 0)
            {
                DataTable patientInfo = BillRepository.GetPatientInfo(patientId);
                if (patientInfo.Rows.Count > 0)
                {
                    ageGender = $"{patientInfo.Rows[0]["Age"]} / {patientInfo.Rows[0]["Gender"]}";
                    patientEmail = patientInfo.Rows[0]["Email"] == DBNull.Value
                        ? null
                        : patientInfo.Rows[0]["Email"].ToString();
                }
            }

            string department = "";
            if (doctorId > 0)
            {
                DataTable doctorInfo = BillRepository.GetDoctorInfo(doctorId);
                if (doctorInfo.Rows.Count > 0)
                    department = doctorInfo.Rows[0]["Specialization"].ToString() ?? "";
            }

            // Pending bills have no payment date and should not present one.
            bool isPaid = string.Equals(cmbPaymentStatus.Text, "Paid", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(cmbPaymentStatus.Text, "Partial", StringComparison.OrdinalIgnoreCase);

            return new BillPdfModel
            {
                BillNumber = txtBillNumber.Text,
                BillDate = dtpBillDate.Value.ToShortDateString(),
                DueDate = dtpBillDate.Value.AddDays(30).ToShortDateString(),
                PatientName = cmbPatient.Text,
                PatientIdText = patientId.ToString(),
                PatientEmail = patientEmail,
                AgeGender = ageGender,
                DoctorName = cmbDoctor.Text,
                Department = department,
                ConsultationFee = txtConsultationFee.Text,
                MedicineFee = txtMedicineFee.Text,
                TestFee = txtTestFee.Text,
                OtherFee = txtOtherFee.Text,
                Discount = txtDiscount.Text,
                TotalAmount = txtTotalAmount.Text,
                PaymentStatus = cmbPaymentStatus.Text,
                PaymentMethod = cmbPaymentMethod.Text,
                PaymentDate = isPaid ? dtpPaymentDate.Value.ToShortDateString() : "-",
                Items = billItemsTable,
                Notes = txtNotes.Text
            };
        }

        // Sends the saved bill as a PDF receipt to the patient's email address.
        // SMTP details come from email.config.json or HMS_SMTP_* environment
        // variables (see README -> "Email receipt configuration").
        private async void BtnSendEmail_Click(object sender, EventArgs e)
        {
            if (selectedBillID == 0)
            {
                MessageBox.Show("Please select a saved bill first (click a bill in the list, or save the current one).",
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (cmbPatient.SelectedValue == null)
            {
                MessageBox.Show("Please select a patient.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            EmailSettings? settings = EmailService.TryLoadSettings();
            if (settings == null)
            {
                MessageBox.Show(
                    "Email is not configured yet.\n\n" +
                    "Create " + EmailService.ConfigFilePath + " with content like:\n\n" +
                    "{\n" +
                    "  \"Host\": \"smtp.example.com\",\n" +
                    "  \"Port\": 587,\n" +
                    "  \"UseSsl\": true,\n" +
                    "  \"FromAddress\": \"clinic@example.com\",\n" +
                    "  \"FromName\": \"MediCare Plus\",\n" +
                    "  \"Username\": \"clinic@example.com\",\n" +
                    "  \"Password\": \"your-app-password\"\n" +
                    "}\n\n" +
                    "or set the HMS_SMTP_HOST / HMS_SMTP_PORT / HMS_SMTP_FROM / HMS_SMTP_USER / HMS_SMTP_PASS environment variables.",
                    "Email Not Configured", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BillPdfModel billModel = BuildBillPdfModel();
            string? toAddress = billModel.PatientEmail;
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                MessageBox.Show("This patient has no email address on file. Add one in Patient Registration first.",
                    "No Patient Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string pdfPath = Path.Combine(Path.GetTempPath(), $"Bill_{txtBillNumber.Text}.pdf");
            UseWaitCursor = true;
            try
            {
                PdfService.RenderBillToFile(billModel, pdfPath);

                string body =
                    "<html><body style='font-family:Segoe UI,Arial,sans-serif'>" +
                    $"<p>Dear {WebUtility.HtmlEncode(cmbPatient.Text)},</p>" +
                    $"<p>Thank you for visiting MediCare Plus. Your invoice <b>{WebUtility.HtmlEncode(txtBillNumber.Text)}</b> " +
                    $"for <b>{WebUtility.HtmlEncode(txtTotalAmount.Text)}</b> is attached as a PDF.</p>" +
                    "<p>If you have any questions about this bill, please contact the clinic.</p>" +
                    "<p style='color:#64748b'>MediCare Plus &mdash; automated message, do not reply.</p>" +
                    "</body></html>";

                await EmailService.SendAsync(settings, toAddress,
                    $"Invoice {txtBillNumber.Text} - MediCare Plus", body, pdfPath);

                MessageBox.Show($"Invoice sent to {toAddress}.", "Email Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Sending the bill email failed", ex);
                MessageBox.Show("The email could not be sent:\n\n" + ex.Message +
                    "\n\nCheck the SMTP settings in email.config.json; technical details are in the logs\\ folder.",
                    "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                try
                {
                    if (File.Exists(pdfPath)) File.Delete(pdfPath);
                }
                catch
                {
                    // Temp file cleanup must never mask the dialog result.
                }
            }
        }

        #endregion

        #region Form reset

        private void ClearForm()
        {
            txtBillNumber.Text = "";
            dtpBillDate.Value = DateTime.Now;
            cmbPatient.SelectedIndex = -1;
            cmbDoctor.SelectedIndex = -1;
            cmbAppointment.SelectedIndex = -1;
            cmbPaymentStatus.SelectedIndex = -1;
            cmbPaymentMethod.SelectedIndex = -1;
            dtpPaymentDate.Value = DateTime.Now;

            txtConsultationFee.Text = "0";
            txtMedicineFee.Text = "0";
            txtTestFee.Text = "0";
            txtOtherFee.Text = "0";
            txtDiscount.Text = "0";
            txtTotalAmount.Text = "0";
            txtNotes.Text = "";

            billItemsTable.Clear();
            dgvBillItems.DataSource = billItemsTable;

            txtItemName.Text = "";
            cmbItemType.SelectedIndex = -1;
            txtQuantity.Text = "1";
            txtUnitPrice.Text = "0";
            txtTotalPrice.Text = "0";

            selectedBillID = 0;
        }

        #endregion

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new Container();
            tooltip = new ToolTip(components);
            lblBillNumber = new Label();
            lblBillDate = new Label();
            lblPatient = new Label();
            lblDoctor = new Label();
            lblAppointment = new Label();
            lblConsultationFee = new Label();
            lblMedicineFee = new Label();
            lblTestFee = new Label();
            lblOtherFee = new Label();
            lblDiscount = new Label();
            lblTotalAmount = new Label();
            lblPaymentStatus = new Label();
            lblPaymentMethod = new Label();
            lblPaymentDate = new Label();
            lblNotes = new Label();
            lblItemName = new Label();
            lblItemType = new Label();
            lblQuantity = new Label();
            lblUnitPrice = new Label();
            lblTotalPrice = new Label();
            txtBillNumber = new TextBox();
            txtBillNumber.MaxLength = 20;   // Bills.BillNumber NVARCHAR(20)
            txtConsultationFee = new TextBox();
            txtMedicineFee = new TextBox();
            txtTestFee = new TextBox();
            txtOtherFee = new TextBox();
            txtDiscount = new TextBox();
            txtTotalAmount = new TextBox();
            txtNotes = new TextBox();
            txtNotes.MaxLength = 500;   // Bills.Notes NVARCHAR(500)
            txtItemName = new TextBox();
            txtItemName.MaxLength = 200;   // BillItems.ItemName NVARCHAR(200)
            txtQuantity = new TextBox();
            txtUnitPrice = new TextBox();
            txtTotalPrice = new TextBox();
            cmbPatient = new ComboBox();
            cmbDoctor = new ComboBox();
            cmbAppointment = new ComboBox();
            cmbPaymentStatus = new ComboBox();
            cmbPaymentMethod = new ComboBox();
            cmbItemType = new ComboBox();
            dtpBillDate = new DateTimePicker();
            dtpPaymentDate = new DateTimePicker();
            dgvBills = new DataGridView();
            dgvBillItems = new DataGridView();
            btnNew = new Button();
            btnSave = new Button();
            btnDelete = new Button();
            btnPrintPDF = new Button();
            btnAddItem = new Button();
            btnUpdateItem = new Button();
            btnRemoveItem = new Button();
            btnClear = new Button();
            btnSendEmail = new Button();
            grpBillInfo = new GroupBox();
            grpFeeBreakdown = new GroupBox();
            grpPaymentInfo = new GroupBox();
            grpBillItems = new GroupBox();
            ((ISupportInitialize)dgvBills).BeginInit();
            ((ISupportInitialize)dgvBillItems).BeginInit();
            grpBillInfo.SuspendLayout();
            grpFeeBreakdown.SuspendLayout();
            grpPaymentInfo.SuspendLayout();
            grpBillItems.SuspendLayout();
            SuspendLayout();
            //
            // tooltip
            //
            tooltip.AutoPopDelay = 5000;
            tooltip.InitialDelay = 500;
            tooltip.ReshowDelay = 100;
            //
            // lblBillNumber
            //
            lblBillNumber.Location = new Point(19, 30);
            lblBillNumber.Name = "lblBillNumber";
            lblBillNumber.Size = new Size(100, 23);
            lblBillNumber.TabIndex = 0;
            lblBillNumber.Text = "Bill #";
            //
            // lblBillDate
            //
            lblBillDate.Location = new Point(20, 60);
            lblBillDate.Name = "lblBillDate";
            lblBillDate.Size = new Size(100, 23);
            lblBillDate.TabIndex = 2;
            lblBillDate.Text = "Bill Date";
            //
            // lblPatient
            //
            lblPatient.Location = new Point(20, 90);
            lblPatient.Name = "lblPatient";
            lblPatient.Size = new Size(100, 23);
            lblPatient.TabIndex = 4;
            lblPatient.Text = "Patient";
            //
            // lblDoctor
            //
            lblDoctor.Location = new Point(20, 120);
            lblDoctor.Name = "lblDoctor";
            lblDoctor.Size = new Size(100, 23);
            lblDoctor.TabIndex = 6;
            lblDoctor.Text = "Doctor";
            //
            // lblAppointment
            //
            lblAppointment.Location = new Point(20, 150);
            lblAppointment.Name = "lblAppointment";
            lblAppointment.Size = new Size(100, 23);
            lblAppointment.TabIndex = 8;
            lblAppointment.Text = "Appointment";
            //
            // lblConsultationFee
            //
            lblConsultationFee.Location = new Point(20, 30);
            lblConsultationFee.Name = "lblConsultationFee";
            lblConsultationFee.Size = new Size(100, 23);
            lblConsultationFee.TabIndex = 0;
            lblConsultationFee.Text = "Consultation Fee";
            //
            // lblMedicineFee
            //
            lblMedicineFee.Location = new Point(20, 60);
            lblMedicineFee.Name = "lblMedicineFee";
            lblMedicineFee.Size = new Size(100, 23);
            lblMedicineFee.TabIndex = 2;
            lblMedicineFee.Text = "Medicine Fee";
            //
            // lblTestFee
            //
            lblTestFee.Location = new Point(20, 90);
            lblTestFee.Name = "lblTestFee";
            lblTestFee.Size = new Size(100, 23);
            lblTestFee.TabIndex = 4;
            lblTestFee.Text = "Test Fee";
            //
            // lblOtherFee
            //
            lblOtherFee.Location = new Point(20, 120);
            lblOtherFee.Name = "lblOtherFee";
            lblOtherFee.Size = new Size(100, 23);
            lblOtherFee.TabIndex = 6;
            lblOtherFee.Text = "Other Fee";
            //
            // lblDiscount
            //
            lblDiscount.Location = new Point(20, 150);
            lblDiscount.Name = "lblDiscount";
            lblDiscount.Size = new Size(100, 23);
            lblDiscount.TabIndex = 8;
            lblDiscount.Text = "Discount";
            //
            // lblTotalAmount
            //
            lblTotalAmount.Location = new Point(260, 151);
            lblTotalAmount.Name = "lblTotalAmount";
            lblTotalAmount.Size = new Size(100, 23);
            lblTotalAmount.TabIndex = 10;
            lblTotalAmount.Text = "Total Amount";
            //
            // lblPaymentStatus
            //
            lblPaymentStatus.Location = new Point(20, 180);
            lblPaymentStatus.Name = "lblPaymentStatus";
            lblPaymentStatus.Size = new Size(100, 23);
            lblPaymentStatus.TabIndex = 10;
            lblPaymentStatus.Text = "Payment Status";
            //
            // lblPaymentMethod
            //
            lblPaymentMethod.Location = new Point(14, 32);
            lblPaymentMethod.Name = "lblPaymentMethod";
            lblPaymentMethod.Size = new Size(100, 23);
            lblPaymentMethod.TabIndex = 0;
            lblPaymentMethod.Text = "Payment Method";
            //
            // lblPaymentDate
            //
            lblPaymentDate.Location = new Point(264, 33);
            lblPaymentDate.Name = "lblPaymentDate";
            lblPaymentDate.Size = new Size(100, 23);
            lblPaymentDate.TabIndex = 2;
            lblPaymentDate.Text = "Payment Date";
            //
            // lblNotes
            //
            lblNotes.Location = new Point(563, 38);
            lblNotes.Name = "lblNotes";
            lblNotes.Size = new Size(100, 23);
            lblNotes.TabIndex = 4;
            lblNotes.Text = "Notes";
            //
            // lblItemName
            //
            lblItemName.Location = new Point(28, 33);
            lblItemName.Name = "lblItemName";
            lblItemName.Size = new Size(100, 23);
            lblItemName.TabIndex = 0;
            lblItemName.Text = "Item Name";
            //
            // lblItemType
            //
            lblItemType.Location = new Point(278, 30);
            lblItemType.Name = "lblItemType";
            lblItemType.Size = new Size(100, 23);
            lblItemType.TabIndex = 2;
            lblItemType.Text = "Type";
            //
            // lblQuantity
            //
            lblQuantity.Location = new Point(26, 61);
            lblQuantity.Name = "lblQuantity";
            lblQuantity.Size = new Size(100, 23);
            lblQuantity.TabIndex = 4;
            lblQuantity.Text = "Quantity";
            //
            // lblUnitPrice
            //
            lblUnitPrice.Location = new Point(484, 24);
            lblUnitPrice.Name = "lblUnitPrice";
            lblUnitPrice.Size = new Size(100, 23);
            lblUnitPrice.TabIndex = 6;
            lblUnitPrice.Text = "Unit Price";
            //
            // lblTotalPrice
            //
            lblTotalPrice.Location = new Point(481, 63);
            lblTotalPrice.Name = "lblTotalPrice";
            lblTotalPrice.Size = new Size(100, 23);
            lblTotalPrice.TabIndex = 8;
            lblTotalPrice.Text = "Total Price";
            //
            // txtBillNumber
            //
            txtBillNumber.Location = new Point(120, 30);
            txtBillNumber.Name = "txtBillNumber";
            txtBillNumber.Size = new Size(100, 23);
            txtBillNumber.TabIndex = 1;
            //
            // txtConsultationFee
            //
            txtConsultationFee.Location = new Point(150, 30);
            txtConsultationFee.Name = "txtConsultationFee";
            txtConsultationFee.ReadOnly = true;
            txtConsultationFee.Size = new Size(100, 23);
            txtConsultationFee.TabIndex = 1;
            //
            // txtMedicineFee
            //
            txtMedicineFee.Location = new Point(150, 60);
            txtMedicineFee.Name = "txtMedicineFee";
            txtMedicineFee.ReadOnly = true;
            txtMedicineFee.Size = new Size(100, 23);
            txtMedicineFee.TabIndex = 3;
            //
            // txtTestFee
            //
            txtTestFee.Location = new Point(150, 90);
            txtTestFee.Name = "txtTestFee";
            txtTestFee.ReadOnly = true;
            txtTestFee.Size = new Size(100, 23);
            txtTestFee.TabIndex = 5;
            //
            // txtOtherFee
            //
            txtOtherFee.Location = new Point(150, 120);
            txtOtherFee.Name = "txtOtherFee";
            txtOtherFee.ReadOnly = true;
            txtOtherFee.Size = new Size(100, 23);
            txtOtherFee.TabIndex = 7;
            //
            // txtDiscount
            //
            txtDiscount.Location = new Point(150, 150);
            txtDiscount.Name = "txtDiscount";
            txtDiscount.Size = new Size(100, 23);
            txtDiscount.TabIndex = 9;
            txtDiscount.TextChanged += txtDiscount_TextChanged;
            //
            // txtTotalAmount
            //
            txtTotalAmount.Location = new Point(350, 150);
            txtTotalAmount.Name = "txtTotalAmount";
            txtTotalAmount.ReadOnly = true;
            txtTotalAmount.Size = new Size(100, 23);
            txtTotalAmount.TabIndex = 11;
            //
            // txtNotes
            //
            txtNotes.Location = new Point(616, 17);
            txtNotes.Multiline = true;
            txtNotes.Name = "txtNotes";
            txtNotes.Size = new Size(234, 67);
            txtNotes.TabIndex = 5;
            //
            // txtItemName
            //
            txtItemName.Location = new Point(133, 30);
            txtItemName.Name = "txtItemName";
            txtItemName.Size = new Size(100, 23);
            txtItemName.TabIndex = 1;
            //
            // txtQuantity
            //
            txtQuantity.Location = new Point(133, 60);
            txtQuantity.Name = "txtQuantity";
            txtQuantity.Size = new Size(100, 23);
            txtQuantity.TabIndex = 5;
            txtQuantity.TextChanged += TxtQuantity_TextChanged;
            //
            // txtUnitPrice
            //
            txtUnitPrice.Location = new Point(568, 22);
            txtUnitPrice.Name = "txtUnitPrice";
            txtUnitPrice.Size = new Size(100, 23);
            txtUnitPrice.TabIndex = 7;
            txtUnitPrice.TextChanged += TxtUnitPrice_TextChanged;
            //
            // txtTotalPrice
            //
            txtTotalPrice.Location = new Point(568, 60);
            txtTotalPrice.Name = "txtTotalPrice";
            txtTotalPrice.ReadOnly = true;
            txtTotalPrice.Size = new Size(100, 23);
            txtTotalPrice.TabIndex = 9;
            //
            // cmbPatient
            //
            cmbPatient.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPatient.Location = new Point(120, 90);
            cmbPatient.Name = "cmbPatient";
            cmbPatient.Size = new Size(121, 23);
            cmbPatient.TabIndex = 5;
            cmbPatient.SelectedIndexChanged += CmbPatient_SelectedIndexChanged;
            //
            // cmbDoctor
            //
            cmbDoctor.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDoctor.Location = new Point(120, 120);
            cmbDoctor.Name = "cmbDoctor";
            cmbDoctor.Size = new Size(121, 23);
            cmbDoctor.TabIndex = 7;
            cmbDoctor.SelectedIndexChanged += CmbDoctor_SelectedIndexChanged;
            //
            // cmbAppointment
            //
            cmbAppointment.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAppointment.Location = new Point(120, 150);
            cmbAppointment.Name = "cmbAppointment";
            cmbAppointment.Size = new Size(121, 23);
            cmbAppointment.TabIndex = 9;
            //
            // cmbPaymentStatus
            //
            cmbPaymentStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentStatus.Location = new Point(120, 180);
            cmbPaymentStatus.Name = "cmbPaymentStatus";
            cmbPaymentStatus.Size = new Size(121, 23);
            cmbPaymentStatus.TabIndex = 11;
            cmbPaymentStatus.SelectedIndexChanged += CmbPaymentStatus_SelectedIndexChanged;
            //
            // cmbPaymentMethod
            //
            cmbPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentMethod.Location = new Point(120, 30);
            cmbPaymentMethod.Name = "cmbPaymentMethod";
            cmbPaymentMethod.Size = new Size(121, 23);
            cmbPaymentMethod.TabIndex = 1;
            cmbPaymentMethod.Enabled = false;
            //
            // cmbItemType
            //
            cmbItemType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemType.Location = new Point(321, 27);
            cmbItemType.Name = "cmbItemType";
            cmbItemType.Size = new Size(121, 23);
            cmbItemType.TabIndex = 3;
            //
            // dtpBillDate
            //
            dtpBillDate.Format = DateTimePickerFormat.Short;
            dtpBillDate.Location = new Point(120, 60);
            dtpBillDate.Name = "dtpBillDate";
            dtpBillDate.Size = new Size(200, 23);
            dtpBillDate.TabIndex = 3;
            //
            // dtpPaymentDate
            //
            dtpPaymentDate.Format = DateTimePickerFormat.Short;
            dtpPaymentDate.Location = new Point(350, 30);
            dtpPaymentDate.Name = "dtpPaymentDate";
            dtpPaymentDate.Size = new Size(200, 23);
            dtpPaymentDate.TabIndex = 3;
            dtpPaymentDate.Enabled = false;
            //
            // dgvBills
            //
            dgvBills.AllowUserToAddRows = false;
            dgvBills.AllowUserToDeleteRows = false;
            dgvBills.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBills.Location = new Point(10, 339);
            dgvBills.Name = "dgvBills";
            dgvBills.ReadOnly = true;
            dgvBills.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvBills.Size = new Size(870, 150);
            dgvBills.TabIndex = 7;
            dgvBills.CellClick += DgvBills_CellClick;
            //
            // dgvBillItems
            //
            dgvBillItems.AllowUserToAddRows = false;
            dgvBillItems.AllowUserToDeleteRows = false;
            dgvBillItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBillItems.Location = new Point(20, 117);
            dgvBillItems.Name = "dgvBillItems";
            dgvBillItems.ReadOnly = true;
            dgvBillItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvBillItems.Size = new Size(830, 100);
            dgvBillItems.TabIndex = 10;
            dgvBillItems.CellClick += DgvBillItems_CellClick;
            //
            // btnNew
            //
            btnNew.Location = new Point(30, 729);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(75, 23);
            btnNew.TabIndex = 8;
            btnNew.Text = "New";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += BtnNew_Click;
            //
            // btnSave
            //
            btnSave.Location = new Point(135, 729);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 9;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            //
            // btnDelete
            //
            btnDelete.Location = new Point(342, 729);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(75, 23);
            btnDelete.TabIndex = 10;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += BtnDelete_Click;
            //
            // btnPrintPDF
            //
            btnPrintPDF.Location = new Point(239, 729);
            btnPrintPDF.Name = "btnPrintPDF";
            btnPrintPDF.Size = new Size(75, 23);
            btnPrintPDF.TabIndex = 11;
            btnPrintPDF.Text = "Print PDF";
            btnPrintPDF.UseVisualStyleBackColor = true;
            btnPrintPDF.Click += BtnPrintPDF_Click;
            //
            // btnAddItem
            //
            btnAddItem.Location = new Point(650, 30);
            btnAddItem.Name = "btnAddItem";
            btnAddItem.Size = new Size(75, 23);
            btnAddItem.TabIndex = 11;
            btnAddItem.Text = "Add Item";
            btnAddItem.UseVisualStyleBackColor = true;
            btnAddItem.Click += BtnAddItem_Click;
            //
            // btnUpdateItem
            //
            btnUpdateItem.Location = new Point(650, 60);
            btnUpdateItem.Name = "btnUpdateItem";
            btnUpdateItem.Size = new Size(75, 23);
            btnUpdateItem.TabIndex = 12;
            btnUpdateItem.Text = "Update Item";
            btnUpdateItem.UseVisualStyleBackColor = true;
            btnUpdateItem.Click += BtnUpdateItem_Click;
            //
            // btnRemoveItem
            //
            btnRemoveItem.Location = new Point(750, 60);
            btnRemoveItem.Name = "btnRemoveItem";
            btnRemoveItem.Size = new Size(75, 23);
            btnRemoveItem.TabIndex = 13;
            btnRemoveItem.Text = "Remove Item";
            btnRemoveItem.UseVisualStyleBackColor = true;
            btnRemoveItem.Click += BtnRemoveItem_Click;
            //
            // btnClear
            //
            btnClear.Location = new Point(438, 729);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(87, 23);
            btnClear.TabIndex = 14;
            btnClear.Text = "Clear";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += BtnClear_Click;
            //
            // btnSendEmail
            //
            btnSendEmail.Location = new Point(538, 729);
            btnSendEmail.Name = "btnSendEmail";
            btnSendEmail.Size = new Size(96, 23);
            btnSendEmail.TabIndex = 15;
            btnSendEmail.Text = "Send Email";
            btnSendEmail.UseVisualStyleBackColor = true;
            btnSendEmail.Click += BtnSendEmail_Click;
            //
            // grpBillInfo
            //
            grpBillInfo.Controls.Add(txtBillNumber);
            grpBillInfo.Controls.Add(lblBillDate);
            grpBillInfo.Controls.Add(dtpBillDate);
            grpBillInfo.Controls.Add(lblPatient);
            grpBillInfo.Controls.Add(cmbPatient);
            grpBillInfo.Controls.Add(lblDoctor);
            grpBillInfo.Controls.Add(cmbDoctor);
            grpBillInfo.Controls.Add(lblAppointment);
            grpBillInfo.Controls.Add(cmbAppointment);
            grpBillInfo.Controls.Add(lblPaymentStatus);
            grpBillInfo.Controls.Add(cmbPaymentStatus);
            grpBillInfo.Controls.Add(lblBillNumber);
            grpBillInfo.Location = new Point(18, 9);
            grpBillInfo.Name = "grpBillInfo";
            grpBillInfo.Size = new Size(342, 227);
            grpBillInfo.TabIndex = 0;
            grpBillInfo.TabStop = false;
            grpBillInfo.Text = "Bill Information";
            //
            // grpFeeBreakdown
            //
            grpFeeBreakdown.Controls.Add(lblConsultationFee);
            grpFeeBreakdown.Controls.Add(txtConsultationFee);
            grpFeeBreakdown.Controls.Add(lblMedicineFee);
            grpFeeBreakdown.Controls.Add(txtMedicineFee);
            grpFeeBreakdown.Controls.Add(lblTestFee);
            grpFeeBreakdown.Controls.Add(txtTestFee);
            grpFeeBreakdown.Controls.Add(lblOtherFee);
            grpFeeBreakdown.Controls.Add(txtOtherFee);
            grpFeeBreakdown.Controls.Add(lblDiscount);
            grpFeeBreakdown.Controls.Add(txtDiscount);
            grpFeeBreakdown.Controls.Add(txtTotalAmount);
            grpFeeBreakdown.Controls.Add(lblTotalAmount);
            grpFeeBreakdown.Location = new Point(370, 10);
            grpFeeBreakdown.Name = "grpFeeBreakdown";
            grpFeeBreakdown.Size = new Size(510, 226);
            grpFeeBreakdown.TabIndex = 1;
            grpFeeBreakdown.TabStop = false;
            grpFeeBreakdown.Text = "Fee Breakdown";
            //
            // grpPaymentInfo
            //
            grpPaymentInfo.Controls.Add(cmbPaymentMethod);
            grpPaymentInfo.Controls.Add(dtpPaymentDate);
            grpPaymentInfo.Controls.Add(txtNotes);
            grpPaymentInfo.Controls.Add(lblNotes);
            grpPaymentInfo.Controls.Add(lblPaymentDate);
            grpPaymentInfo.Controls.Add(lblPaymentMethod);
            grpPaymentInfo.Location = new Point(10, 242);
            grpPaymentInfo.Name = "grpPaymentInfo";
            grpPaymentInfo.Size = new Size(870, 90);
            grpPaymentInfo.TabIndex = 2;
            grpPaymentInfo.TabStop = false;
            grpPaymentInfo.Text = "Payment Information";
            //
            // grpBillItems
            //
            grpBillItems.Controls.Add(txtItemName);
            grpBillItems.Controls.Add(cmbItemType);
            grpBillItems.Controls.Add(txtQuantity);
            grpBillItems.Controls.Add(txtUnitPrice);
            grpBillItems.Controls.Add(txtTotalPrice);
            grpBillItems.Controls.Add(dgvBillItems);
            grpBillItems.Controls.Add(lblTotalPrice);
            grpBillItems.Controls.Add(lblUnitPrice);
            grpBillItems.Controls.Add(lblQuantity);
            grpBillItems.Controls.Add(lblItemName);
            grpBillItems.Controls.Add(lblItemType);
            grpBillItems.Location = new Point(10, 492);
            grpBillItems.Name = "grpBillItems";
            grpBillItems.Size = new Size(870, 231);
            grpBillItems.TabIndex = 3;
            grpBillItems.TabStop = false;
            grpBillItems.Text = "Bill Items";
            //
            // BillingForm
            //
            ClientSize = new Size(897, 759);
            Controls.Add(grpBillInfo);
            Controls.Add(grpFeeBreakdown);
            Controls.Add(grpPaymentInfo);
            Controls.Add(btnNew);
            Controls.Add(btnSave);
            Controls.Add(btnDelete);
            Controls.Add(btnAddItem);
            Controls.Add(btnUpdateItem);
            Controls.Add(btnPrintPDF);
            Controls.Add(btnRemoveItem);
            Controls.Add(btnClear);
            Controls.Add(btnSendEmail);
            Controls.Add(grpBillItems);
            Controls.Add(dgvBills);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "BillingForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Billing Management";
            ((ISupportInitialize)dgvBills).EndInit();
            ((ISupportInitialize)dgvBillItems).EndInit();
            grpBillInfo.ResumeLayout(false);
            grpBillInfo.PerformLayout();
            grpFeeBreakdown.ResumeLayout(false);
            grpFeeBreakdown.PerformLayout();
            grpPaymentInfo.ResumeLayout(false);
            grpPaymentInfo.PerformLayout();
            grpBillItems.ResumeLayout(false);
            grpBillItems.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private void txtDiscount_TextChanged(object sender, EventArgs e)
        {
            UpdateFeeSummary();
        }

        private System.ComponentModel.IContainer components;

        // Labels
        private Label lblBillNumber;
        private Label lblBillDate;
        private Label lblPatient;
        private Label lblDoctor;
        private Label lblAppointment;
        private Label lblConsultationFee;
        private Label lblMedicineFee;
        private Label lblTestFee;
        private Label lblOtherFee;
        private Label lblDiscount;
        private Label lblTotalAmount;
        private Label lblPaymentStatus;
        private Label lblPaymentMethod;
        private Label lblPaymentDate;
        private Label lblNotes;
        private Label lblItemName;
        private Label lblItemType;
        private Label lblQuantity;
        private Label lblUnitPrice;
        private Label lblTotalPrice;

        // Selections
        private ComboBox cmbPatient;
        private ComboBox cmbDoctor;
        private ComboBox cmbAppointment;
        private ComboBox cmbPaymentStatus;
        private ComboBox cmbPaymentMethod;
        private ComboBox cmbItemType;

        // Bill fields
        private TextBox txtBillNumber;
        private TextBox txtConsultationFee;
        private TextBox txtMedicineFee;
        private TextBox txtTestFee;
        private TextBox txtOtherFee;
        private TextBox txtDiscount;
        private TextBox txtTotalAmount;
        private TextBox txtNotes;
        private TextBox txtItemName;
        private TextBox txtQuantity;
        private TextBox txtUnitPrice;
        private TextBox txtTotalPrice;

        // Dates
        private DateTimePicker dtpBillDate;
        private DateTimePicker dtpPaymentDate;

        // Grids
        private DataGridView dgvBills;
        private DataGridView dgvBillItems;

        // Actions
        private Button btnDelete;
        private Button btnAddItem;
        private Button btnRemoveItem;
        private Button btnNew;
        private Button btnSave;
        private Button btnPrintPDF;
        private Button btnUpdateItem;
        private Button btnClear;
        private Button btnSendEmail;

        // Groups
        private GroupBox grpBillInfo;
        private GroupBox grpFeeBreakdown;
        private GroupBox grpBillItems;
        private GroupBox grpPaymentInfo;
    }
}
