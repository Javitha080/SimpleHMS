using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using SimpleHMS.Repositories;
using SimpleHMS.Services;

namespace SimpleHMS
{
    // CRUD for doctor records, backed by DoctorRepository.
    public partial class DoctorForm : Form
    {
        private TextBox txtName, txtSpecialization, txtPhone, txtFee;
        private DataGridView dgv;
        private Button btnAdd, btnUpdate, btnDelete, btnClear;
        private ToolTip tooltip;
        private int selectedDoctorID;

        public DoctorForm()
        {
            InitializeComponent();
            LoadDoctors();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            Icon = (Icon)resources.GetObject("$this.Icon");
            Text = "Doctor Registration";
            Size = new Size(700, 500);
            StartPosition = FormStartPosition.CenterScreen;

            tooltip = new ToolTip();
            tooltip.AutoPopDelay = 5000;
            tooltip.InitialDelay = 500;

            Label lblName = new Label() { Text = "Name:", Location = new Point(20, 20), Size = new Size(100, 20) };
            txtName = new TextBox() { Location = new Point(130, 20), Size = new Size(200, 20) };
            txtName.MaxLength = 100;   // Doctors.Name NVARCHAR(100)
            tooltip.SetToolTip(lblName, "Doctor's full name");
            tooltip.SetToolTip(txtName, "Type the doctor's full name here");
            Controls.Add(lblName);
            Controls.Add(txtName);

            Label lblSpec = new Label() { Text = "Specialization:", Location = new Point(20, 50), Size = new Size(100, 20) };
            txtSpecialization = new TextBox() { Location = new Point(130, 50), Size = new Size(200, 20) };
            txtSpecialization.MaxLength = 100;   // Doctors.Specialization NVARCHAR(100)
            tooltip.SetToolTip(lblSpec, "Doctor's specialization");
            tooltip.SetToolTip(txtSpecialization, "e.g. Cardiology, Pediatrics");
            Controls.Add(lblSpec);
            Controls.Add(txtSpecialization);

            Label lblPhone = new Label() { Text = "Phone:", Location = new Point(20, 80), Size = new Size(100, 20) };
            txtPhone = new TextBox() { Location = new Point(130, 80), Size = new Size(200, 20) };
            txtPhone.MaxLength = 15;   // Doctors.Phone NVARCHAR(15)
            tooltip.SetToolTip(lblPhone, "Contact phone number");
            tooltip.SetToolTip(txtPhone, "Type the doctor's phone number");
            Controls.Add(lblPhone);
            Controls.Add(txtPhone);

            Label lblFee = new Label() { Text = "Fee:", Location = new Point(20, 110), Size = new Size(100, 20) };
            txtFee = new TextBox() { Location = new Point(130, 110), Size = new Size(200, 20) };
            txtFee.KeyPress += Validators.AllowDecimal_KeyPress;
            tooltip.SetToolTip(lblFee, "Consultation fee");
            tooltip.SetToolTip(txtFee, "Positive number only");
            Controls.Add(lblFee);
            Controls.Add(txtFee);

            btnAdd = new Button() { Text = "Add Doctor", Location = new Point(130, 150), Size = new Size(100, 30) };
            btnAdd.Click += BtnAdd_Click;
            tooltip.SetToolTip(btnAdd, "Add a new doctor to the database");
            Controls.Add(btnAdd);

            btnUpdate = new Button() { Text = "Update", Location = new Point(240, 150), Size = new Size(90, 30) };
            btnUpdate.Click += BtnUpdate_Click;
            tooltip.SetToolTip(btnUpdate, "Update the selected doctor");
            Controls.Add(btnUpdate);

            btnDelete = new Button() { Text = "Delete", Location = new Point(340, 150), Size = new Size(90, 30) };
            btnDelete.Click += BtnDelete_Click;
            tooltip.SetToolTip(btnDelete, "Delete the selected doctor");
            Controls.Add(btnDelete);

            btnClear = new Button() { Text = "Clear", Location = new Point(440, 150), Size = new Size(90, 30) };
            btnClear.Click += (s, e) => ClearFields();
            tooltip.SetToolTip(btnClear, "Clear all input fields");
            Controls.Add(btnClear);

            dgv = new DataGridView() { Location = new Point(20, 200), Size = new Size(650, 250) };
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.CellClick += Dgv_CellClick;
            tooltip.SetToolTip(dgv, "Click a row to select a doctor for update or delete");
            Controls.Add(dgv);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                int result = DoctorRepository.Insert(
                    txtName.Text.Trim(), txtSpecialization.Text.Trim(),
                    txtPhone.Text.Trim(), decimal.Parse(txtFee.Text));

                if (result > 0)
                {
                    MessageBox.Show("Doctor added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadDoctors();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding doctor: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedDoctorID == 0)
            {
                MessageBox.Show("Please select a doctor from the list to update!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInputs()) return;

            try
            {
                int result = DoctorRepository.Update(
                    selectedDoctorID, txtName.Text.Trim(), txtSpecialization.Text.Trim(),
                    txtPhone.Text.Trim(), decimal.Parse(txtFee.Text));

                if (result > 0)
                {
                    MessageBox.Show("Doctor updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadDoctors();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating doctor: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedDoctorID == 0)
            {
                MessageBox.Show("Please select a doctor from the list to delete!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show("Are you sure you want to delete this doctor?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                int result = DoctorRepository.Delete(selectedDoctorID);

                if (result > 0)
                {
                    MessageBox.Show("Doctor deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadDoctors();
                }
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // The doctor is referenced by appointments or bills.
                MessageBox.Show("Cannot delete this doctor because appointments or bills still reference them. " +
                    "Remove those records first.", "Record In Use", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot delete this doctor: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgv.Rows[e.RowIndex];

            selectedDoctorID = Convert.ToInt32(row.Cells["DoctorID"].Value);
            txtName.Text = row.Cells["Name"].Value?.ToString() ?? "";
            txtSpecialization.Text = row.Cells["Specialization"].Value?.ToString() ?? "";
            txtPhone.Text = row.Cells["Phone"].Value?.ToString() ?? "";
            txtFee.Text = row.Cells["Fee"].Value?.ToString() ?? "";
        }

        private void LoadDoctors()
        {
            dgv.DataSource = DoctorRepository.GetAll();

            if (dgv.Columns.Contains("DoctorID"))
            {
                dgv.Columns["DoctorID"].Visible = false;
            }
        }

        private void ClearFields()
        {
            selectedDoctorID = 0;
            txtName.Clear();
            txtSpecialization.Clear();
            txtPhone.Clear();
            txtFee.Clear();
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

            if (!Validators.IsValidSpecialization(txtSpecialization.Text))
            {
                MessageBox.Show("Please enter a valid specialization (minimum 3 characters, letters only).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSpecialization.Focus();
                return false;
            }

            if (!Validators.IsValidPhone(txtPhone.Text))
            {
                MessageBox.Show("Please enter a valid phone number (10-15 digits).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhone.Focus();
                return false;
            }

            if (!decimal.TryParse(txtFee.Text, out decimal fee) || fee <= 0)
            {
                MessageBox.Show("Please enter a valid fee amount (positive number).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFee.Focus();
                return false;
            }

            return true;
        }
    }
}
