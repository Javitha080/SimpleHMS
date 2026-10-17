using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using SimpleHMS.Repositories;

namespace SimpleHMS
{
    // CRUD for appointments, backed by AppointmentRepository.
    public partial class AppointmentForm : Form
    {
        private ComboBox cmbPatient, cmbDoctor;
        private DateTimePicker dtpDate, dtpTime;
        private DataGridView dgv;
        private Button btnBook, btnUpdate, btnDelete, btnClear;
        private ToolTip tooltip;
        private int selectedAppointmentID;

        public AppointmentForm()
        {
            InitializeComponent();
            LoadPatients();
            LoadDoctors();
            LoadAppointments();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            Icon = (Icon)resources.GetObject("$this.Icon");
            Text = "Appointments";
            Size = new Size(700, 550);
            StartPosition = FormStartPosition.CenterScreen;

            tooltip = new ToolTip();
            tooltip.AutoPopDelay = 5000;
            tooltip.InitialDelay = 500;

            Label lblPatient = new Label() { Text = "Patient:", Location = new Point(20, 20), Size = new Size(100, 20) };
            cmbPatient = new ComboBox() { Location = new Point(130, 20), Size = new Size(200, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            tooltip.SetToolTip(lblPatient, "Patient for the appointment");
            tooltip.SetToolTip(cmbPatient, "Choose a patient from the dropdown list");
            Controls.Add(lblPatient);
            Controls.Add(cmbPatient);

            Label lblDoctor = new Label() { Text = "Doctor:", Location = new Point(20, 50), Size = new Size(100, 20) };
            cmbDoctor = new ComboBox() { Location = new Point(130, 50), Size = new Size(200, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            tooltip.SetToolTip(lblDoctor, "Doctor for the appointment");
            tooltip.SetToolTip(cmbDoctor, "Choose a doctor from the dropdown list");
            Controls.Add(lblDoctor);
            Controls.Add(cmbDoctor);

            Label lblDate = new Label() { Text = "Date:", Location = new Point(20, 80), Size = new Size(100, 20) };
            dtpDate = new DateTimePicker() { Location = new Point(130, 80), Size = new Size(200, 20), Format = DateTimePickerFormat.Short };
            tooltip.SetToolTip(lblDate, "Appointment date");
            tooltip.SetToolTip(dtpDate, "Open the calendar and choose a date");
            Controls.Add(lblDate);
            Controls.Add(dtpDate);

            Label lblTime = new Label() { Text = "Time:", Location = new Point(20, 110), Size = new Size(100, 20) };
            dtpTime = new DateTimePicker() { Location = new Point(130, 110), Size = new Size(200, 20), Format = DateTimePickerFormat.Time, ShowUpDown = true };
            tooltip.SetToolTip(lblTime, "Appointment time");
            tooltip.SetToolTip(dtpTime, "Use up/down arrows to set the time");
            Controls.Add(lblTime);
            Controls.Add(dtpTime);

            btnBook = new Button() { Text = "Book Appointment", Location = new Point(130, 150), Size = new Size(120, 30) };
            btnBook.Click += BtnBook_Click;
            tooltip.SetToolTip(btnBook, "Book a new appointment");
            Controls.Add(btnBook);

            btnUpdate = new Button() { Text = "Update", Location = new Point(260, 150), Size = new Size(90, 30) };
            btnUpdate.Click += BtnUpdate_Click;
            tooltip.SetToolTip(btnUpdate, "Update the selected appointment");
            Controls.Add(btnUpdate);

            btnDelete = new Button() { Text = "Delete", Location = new Point(360, 150), Size = new Size(90, 30) };
            btnDelete.Click += BtnDelete_Click;
            tooltip.SetToolTip(btnDelete, "Cancel/delete the selected appointment");
            Controls.Add(btnDelete);

            btnClear = new Button() { Text = "Clear", Location = new Point(460, 150), Size = new Size(70, 30) };
            btnClear.Click += (s, e) => ClearFields();
            tooltip.SetToolTip(btnClear, "Clear all selections");
            Controls.Add(btnClear);

            dgv = new DataGridView() { Location = new Point(20, 200), Size = new Size(650, 300) };
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.CellClick += Dgv_CellClick;
            tooltip.SetToolTip(dgv, "Click a row to select an appointment for update or delete");
            Controls.Add(dgv);
        }

        private void BtnBook_Click(object sender, EventArgs e)
        {
            if (cmbPatient.SelectedValue == null || cmbDoctor.SelectedValue == null)
            {
                MessageBox.Show("Please select patient and doctor!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (dtpDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("New appointments cannot be booked in the past. Choose today or a future date.",
                    "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // The database allowed identical rows; clicking "Book" twice on the
            // same slot created duplicate appointments unnoticed.
            if (AppointmentRepository.ExistsDuplicate(
                    Convert.ToInt32(cmbPatient.SelectedValue),
                    Convert.ToInt32(cmbDoctor.SelectedValue),
                    dtpDate.Value, dtpTime.Value.TimeOfDay, 0))
            {
                MessageBox.Show("The patient already has an appointment with this doctor at the selected date and time.",
                    "Duplicate Appointment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // A doctor cannot be in two places at once: the slot may already be
            // taken by a different patient.
            if (AppointmentRepository.ExistsDoctorConflict(
                    Convert.ToInt32(cmbDoctor.SelectedValue),
                    dtpDate.Value, dtpTime.Value.TimeOfDay, 0))
            {
                MessageBox.Show("The doctor is already booked at the selected date and time. " +
                    "Choose another slot or doctor.",
                    "Doctor Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int result = AppointmentRepository.Insert(
                    Convert.ToInt32(cmbPatient.SelectedValue),
                    Convert.ToInt32(cmbDoctor.SelectedValue),
                    dtpDate.Value, dtpTime.Value.TimeOfDay);

                if (result > 0)
                {
                    MessageBox.Show("Appointment booked successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadAppointments();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error booking appointment: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedAppointmentID == 0)
            {
                MessageBox.Show("Please select an appointment from the list to update!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbPatient.SelectedValue == null || cmbDoctor.SelectedValue == null)
            {
                MessageBox.Show("Please select patient and doctor!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Editing keeps its own row, so exclude it from the duplicate check.
            if (AppointmentRepository.ExistsDuplicate(
                    Convert.ToInt32(cmbPatient.SelectedValue),
                    Convert.ToInt32(cmbDoctor.SelectedValue),
                    dtpDate.Value, dtpTime.Value.TimeOfDay, selectedAppointmentID))
            {
                MessageBox.Show("The patient already has another appointment with this doctor at the selected date and time.",
                    "Duplicate Appointment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Same guard as booking: editing can still collide with another
            // patient's appointment in the same slot.
            if (AppointmentRepository.ExistsDoctorConflict(
                    Convert.ToInt32(cmbDoctor.SelectedValue),
                    dtpDate.Value, dtpTime.Value.TimeOfDay, selectedAppointmentID))
            {
                MessageBox.Show("The doctor already has another appointment at the selected date and time. " +
                    "Choose another slot or doctor.",
                    "Doctor Busy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int result = AppointmentRepository.Update(
                    selectedAppointmentID,
                    Convert.ToInt32(cmbPatient.SelectedValue),
                    Convert.ToInt32(cmbDoctor.SelectedValue),
                    dtpDate.Value, dtpTime.Value.TimeOfDay);

                if (result > 0)
                {
                    MessageBox.Show("Appointment updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadAppointments();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating appointment: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedAppointmentID == 0)
            {
                MessageBox.Show("Please select an appointment from the list to delete!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show("Are you sure you want to cancel this appointment?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                int result = AppointmentRepository.Delete(selectedAppointmentID);

                if (result > 0)
                {
                    MessageBox.Show("Appointment cancelled successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                    LoadAppointments();
                }
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // The appointment is referenced by a saved bill.
                MessageBox.Show("Cannot delete this appointment because it is referenced by other records in the system. " +
                    "Please remove those references first.", "Record In Use", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting appointment: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;

                DataGridViewRow row = dgv.Rows[e.RowIndex];

                if (row.Cells["AppointmentID"].Value == DBNull.Value) return;

                selectedAppointmentID = Convert.ToInt32(row.Cells["AppointmentID"].Value);

                if (row.Cells["PatientID"].Value != DBNull.Value)
                {
                    cmbPatient.SelectedValue = Convert.ToInt32(row.Cells["PatientID"].Value);
                }

                if (row.Cells["DoctorID"].Value != DBNull.Value)
                {
                    cmbDoctor.SelectedValue = Convert.ToInt32(row.Cells["DoctorID"].Value);
                }

                if (row.Cells["Date"].Value != DBNull.Value)
                {
                    dtpDate.Value = Convert.ToDateTime(row.Cells["Date"].Value);
                }

                if (row.Cells["Time"].Value != DBNull.Value && row.Cells["Time"].Value != null)
                {
                    dtpTime.Value = DateTime.Today + (TimeSpan)row.Cells["Time"].Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointment details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearFields();
            }
        }

        private void LoadPatients()
        {
            cmbPatient.DataSource = PatientRepository.GetAllForPicker();
            cmbPatient.DisplayMember = "Name";
            cmbPatient.ValueMember = "PatientID";
            cmbPatient.SelectedIndex = -1;
        }

        private void LoadDoctors()
        {
            cmbDoctor.DataSource = DoctorRepository.GetAllForPicker();
            cmbDoctor.DisplayMember = "Name";
            cmbDoctor.ValueMember = "DoctorID";
            cmbDoctor.SelectedIndex = -1;
        }

        private void LoadAppointments()
        {
            dgv.DataSource = AppointmentRepository.GetAllDetailed();

            if (dgv.Columns.Contains("AppointmentID"))
                dgv.Columns["AppointmentID"].Visible = false;
            if (dgv.Columns.Contains("PatientID"))
                dgv.Columns["PatientID"].Visible = false;
            if (dgv.Columns.Contains("DoctorID"))
                dgv.Columns["DoctorID"].Visible = false;
        }

        private void ClearFields()
        {
            selectedAppointmentID = 0;
            cmbPatient.SelectedIndex = -1;
            cmbDoctor.SelectedIndex = -1;
            dtpDate.Value = DateTime.Now;
            dtpTime.Value = DateTime.Now;
        }
    }
}
