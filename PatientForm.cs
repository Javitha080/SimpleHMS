using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using SimpleHMS.Repositories;
using SimpleHMS.Services;

namespace SimpleHMS
{
    // CRUD for patient records, backed by PatientRepository.
    public partial class PatientForm : Form
    {
        private TextBox txtName, txtAge, txtPhone, txtEmail, txtAddress;
        private ComboBox cmbGender;
        private DataGridView dgv;
        private Button btnAdd, btnUpdate, btnDelete, btnClear;
        private ToolTip tooltip;
        private int selectedPatientID;

        public PatientForm()
        {
            InitializeComponent();
            LoadPatients();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            Icon = (Icon)resources.GetObject("$this.Icon");
            Text = "Patient Registration";
            Size = new Size(700, 540);
            StartPosition = FormStartPosition.CenterScreen;

            tooltip = new ToolTip();
            tooltip.AutoPopDelay = 5000;
            tooltip.InitialDelay = 500;

            Label lblName = new Label() { Text = "Name:", Location = new Point(20, 20), Size = new Size(80, 20) };
            txtName = new TextBox() { Location = new Point(100, 20), Size = new Size(200, 20) };
            txtName.MaxLength = 100;   // Patients.Name NVARCHAR(100)
            tooltip.SetToolTip(lblName, "Patient's full name");
            tooltip.SetToolTip(txtName, "Type the patient's full name here");
            Controls.Add(lblName);
            Controls.Add(txtName);

            Label lblAge = new Label() { Text = "Age:", Location = new Point(20, 50), Size = new Size(80, 20) };
            txtAge = new TextBox() { Location = new Point(100, 50), Size = new Size(200, 20) };
            tooltip.SetToolTip(lblAge, "Patient's age");
            tooltip.SetToolTip(txtAge, "Age in years (1-100)");
            Controls.Add(lblAge);
            Controls.Add(txtAge);

            Label lblGender = new Label() { Text = "Gender:", Location = new Point(20, 80), Size = new Size(80, 20) };
            cmbGender = new ComboBox() { Location = new Point(100, 80), Size = new Size(200, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGender.Items.AddRange(new string[] { "Male", "Female" });
            tooltip.SetToolTip(lblGender, "Patient's gender");
            tooltip.SetToolTip(cmbGender, "Choose Male or Female from the dropdown");
            Controls.Add(lblGender);
            Controls.Add(cmbGender);

            Label lblPhone = new Label() { Text = "Phone:", Location = new Point(20, 110), Size = new Size(80, 20) };
            txtPhone = new TextBox() { Location = new Point(100, 110), Size = new Size(200, 20) };
            txtPhone.MaxLength = 15;   // Patients.Phone NVARCHAR(15)
            tooltip.SetToolTip(lblPhone, "Contact phone number");
            tooltip.SetToolTip(txtPhone, "Type the patient's phone number");
            Controls.Add(lblPhone);
            Controls.Add(txtPhone);

            Label lblEmail = new Label() { Text = "Email:", Location = new Point(20, 140), Size = new Size(80, 20) };
            txtEmail = new TextBox() { Location = new Point(100, 140), Size = new Size(200, 20) };
            txtEmail.MaxLength = 100;   // Patients.Email NVARCHAR(100)
            tooltip.SetToolTip(lblEmail, "Email address (optional)");
            tooltip.SetToolTip(txtEmail, "Used to send the patient their bill as a PDF");
            Controls.Add(lblEmail);
            Controls.Add(txtEmail);

            Label lblAddress = new Label() { Text = "Address:", Location = new Point(20, 170), Size = new Size(80, 20) };
            txtAddress = new TextBox() { Location = new Point(100, 170), Size = new Size(200, 20) };
            txtAddress.MaxLength = 200;   // Patients.Address NVARCHAR(200)
            tooltip.SetToolTip(lblAddress, "Patient's address");
            tooltip.SetToolTip(txtAddress, "Type the patient's residential address");
            Controls.Add(lblAddress);
            Controls.Add(txtAddress);

            btnAdd = new Button() { Text = "Add Patient", Location = new Point(100, 210), Size = new Size(100, 30) };
            btnAdd.Click += BtnAdd_Click;
            tooltip.SetToolTip(btnAdd, "Add a new patient to the database");
            Controls.Add(btnAdd);

            btnUpdate = new Button() { Text = "Update", Location = new Point(210, 210), Size = new Size(90, 30) };
            btnUpdate.Click += BtnUpdate_Click;
            tooltip.SetToolTip(btnUpdate, "Update the selected patient");
            Controls.Add(btnUpdate);

            btnDelete = new Button() { Text = "Delete", Location = new Point(310, 210), Size = new Size(90, 30) };
            btnDelete.Click += BtnDelete_Click;
            tooltip.SetToolTip(btnDelete, "Delete the selected patient");
            Controls.Add(btnDelete);

            btnClear = new Button() { Text = "Clear", Location = new Point(410, 210), Size = new Size(90, 30) };
            btnClear.Click += (s, e) => ClearFields();
            tooltip.SetToolTip(btnClear, "Clear all input fields");
            Controls.Add(btnClear);

            dgv = new DataGridView() { Location = new Point(20, 260), Size = new Size(650, 230) };
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.CellClick += Dgv_CellClick;
            tooltip.SetToolTip(dgv, "Click a row to select a patient for update or delete");
            Controls.Add(dgv);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                int result = PatientRepository.Insert(
                    txtName.Text.Trim(), int.Parse(txtAge.Text), cmbGender.Text,
                    txtPhone.Text.Trim(), txtEmail.Text.Trim(), txtAddress.Text.Trim());

                if (result > 0)
                {
                    MessageBox.Show("Patient added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadPatients();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding patient: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedPatientID == 0)
            {
                MessageBox.Show("Please select a patient from the list to update!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInputs()) return;

            try
            {
                int result = PatientRepository.Update(
                    selectedPatientID, txtName.Text.Trim(), int.Parse(txtAge.Text), cmbGender.Text,
                    txtPhone.Text.Trim(), txtEmail.Text.Trim(), txtAddress.Text.Trim());

                if (result > 0)
                {
                    MessageBox.Show("Patient updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadPatients();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating patient: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedPatientID == 0)
            {
                MessageBox.Show("Please select a patient from the list to delete!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show("Are you sure you want to delete this patient?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                int result = PatientRepository.Delete(selectedPatientID);

                if (result > 0)
                {
                    MessageBox.Show("Patient deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadPatients();
                }
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // The patient is referenced by appointments or bills.
                MessageBox.Show("Cannot delete this patient because appointments or bills still reference them. " +
                    "Remove those records first.", "Record In Use", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot delete this patient: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgv.Rows[e.RowIndex];

            selectedPatientID = Convert.ToInt32(row.Cells["PatientID"].Value);
            txtName.Text = row.Cells["Name"].Value?.ToString() ?? "";
            txtAge.Text = row.Cells["Age"].Value?.ToString() ?? "";
            cmbGender.Text = row.Cells["Gender"].Value?.ToString() ?? "";
            txtPhone.Text = row.Cells["Phone"].Value?.ToString() ?? "";
            txtEmail.Text = row.Cells["Email"].Value?.ToString() ?? "";
            txtAddress.Text = row.Cells["Address"].Value?.ToString() ?? "";
        }

        private void LoadPatients()
        {
            dgv.DataSource = PatientRepository.GetAll();

            if (dgv.Columns.Contains("PatientID"))
            {
                dgv.Columns["PatientID"].Visible = false;
            }
        }

        private void ClearFields()
        {
            selectedPatientID = 0;
            txtName.Clear();
            txtAge.Clear();
            cmbGender.SelectedIndex = -1;
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
        }

        private bool ValidateInputs()
        {
            if (!Validators.IsValidName(txtName.Text))
            {
                MessageBox.Show("Please enter a valid name (minimum 3 characters, letters only).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            if (!int.TryParse(txtAge.Text, out int age) || age < 1 || age > 100)
            {
                MessageBox.Show("Please enter a valid age (1-100).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAge.Focus();
                return false;
            }

            if (cmbGender.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a gender.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbGender.Focus();
                return false;
            }

            if (!Validators.IsValidPhone(txtPhone.Text))
            {
                MessageBox.Show("Please enter a valid phone number (10-15 digits).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhone.Focus();
                return false;
            }

            // Email is optional; when present it must be well-formed because
            // the billing screen sends PDF receipts to it.
            if (!Validators.IsOptionalEmailValid(txtEmail.Text))
            {
                MessageBox.Show("Please enter a valid email address (or leave the field empty).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return false;
            }

            return true;
        }
    }
}
