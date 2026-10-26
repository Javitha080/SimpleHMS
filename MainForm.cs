using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SimpleHMS
{
    // Main menu after login; opens the four management dialogs.
    public partial class MainForm : Form
    {
        private ToolTip tooltip;
        private Timer greetingTimer;
        private MacOSButton closeButton;
        private MacOSButton minimizeButton;
        private MacOSButton maximizeButton;

        public MainForm()
        {
            InitializeComponent();
            LoadLoggedInUser();
            AddMacOSButtons();

            // Returning to the login screen ends the session as well.
            FormClosed += (_, _) => AppSession.Clear();
        }

        private void AddMacOSButtons()
        {
            const int size = 14;
            const int y = 23;
            const int x = 25;
            const int spacing = 8;

            closeButton = new MacOSButton(MacOSButton.ButtonType.Close)
            {
                Location = new Point(x, y),
                Size = new Size(size, size)
            };
            closeButton.Click += (s, e) => Close();

            minimizeButton = new MacOSButton(MacOSButton.ButtonType.Minimize)
            {
                Location = new Point(x + size + spacing, y),
                Size = new Size(size, size)
            };
            minimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;

            maximizeButton = new MacOSButton(MacOSButton.ButtonType.Maximize)
            {
                Location = new Point(x + (size + spacing) * 2, y),
                Size = new Size(size, size)
            };
            maximizeButton.Click += (s, e) =>
                WindowState = WindowState == FormWindowState.Normal
                    ? FormWindowState.Maximized
                    : FormWindowState.Normal;

            Controls.Add(closeButton);
            Controls.Add(minimizeButton);
            Controls.Add(maximizeButton);
            closeButton.BringToFront();
            minimizeButton.BringToFront();
            maximizeButton.BringToFront();
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            tooltip = new ToolTip(components);
            btnPatient = new Button();
            btnDoctor = new Button();
            btnAppointment = new Button();
            btnBilling = new Button();
            greetingTimer = new Timer(components);
            lblTitle = new Label();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            //
            // tooltip
            //
            tooltip.AutoPopDelay = 5000;
            tooltip.InitialDelay = 500;
            tooltip.ReshowDelay = 100;
            //
            // btnPatient
            //
            btnPatient.BackColor = Color.LightSteelBlue;
            btnPatient.FlatStyle = FlatStyle.Flat;
            btnPatient.Location = new Point(508, 163);
            btnPatient.Name = "btnPatient";
            btnPatient.Size = new Size(228, 42);
            btnPatient.TabIndex = 1;
            btnPatient.Text = "Patient Registration";
            tooltip.SetToolTip(btnPatient, "Manage patient records (add, update, delete)");
            btnPatient.UseVisualStyleBackColor = false;
            btnPatient.Click += btnPatient_Click;
            //
            // btnDoctor
            //
            btnDoctor.FlatStyle = FlatStyle.Flat;
            btnDoctor.Location = new Point(508, 228);
            btnDoctor.Name = "btnDoctor";
            btnDoctor.Size = new Size(228, 42);
            btnDoctor.TabIndex = 2;
            btnDoctor.Text = "Doctor Registration";
            tooltip.SetToolTip(btnDoctor, "Manage doctor records (add, update, delete)");
            btnDoctor.Click += btnDoctor_Click;
            //
            // btnAppointment
            //
            btnAppointment.FlatStyle = FlatStyle.Flat;
            btnAppointment.Location = new Point(508, 296);
            btnAppointment.Name = "btnAppointment";
            btnAppointment.Size = new Size(228, 42);
            btnAppointment.TabIndex = 3;
            btnAppointment.Text = "Appointments";
            tooltip.SetToolTip(btnAppointment, "Manage appointments (book, update, cancel)");
            btnAppointment.Click += btnAppointment_Click;
            //
            // btnBilling
            //
            btnBilling.FlatStyle = FlatStyle.Flat;
            btnBilling.Location = new Point(508, 363);
            btnBilling.Name = "btnBilling";
            btnBilling.Size = new Size(228, 42);
            btnBilling.TabIndex = 4;
            btnBilling.Text = "Billing";
            tooltip.SetToolTip(btnBilling, "Create bills and generate PDF receipts");
            btnBilling.Click += btnBilling_Click;
            //
            // greetingTimer
            //
            greetingTimer.Interval = 60000;
            greetingTimer.Tick += greetingTimer_Tick;
            //
            // lblTitle
            //
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Bebas Neue", 43F);
            lblTitle.ForeColor = Color.SteelBlue;
            lblTitle.Location = new Point(-3, 27);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(300, 58);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Welcome To";
            //
            // label1
            //
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Arial", 9F, FontStyle.Bold);
            label1.ForeColor = Color.Black;
            label1.Location = new Point(469, 443);
            label1.Name = "label1";
            label1.Size = new Size(331, 18);
            label1.TabIndex = 5;
            label1.Text = " ©2025 Project X - ESOFT™ H.M.S. All Rights Reserved.";
            label1.TextAlign = ContentAlignment.BottomCenter;
            //
            // label2
            //
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Bebas Neue", 40F);
            label2.ForeColor = Color.DimGray;
            label2.Location = new Point(144, 81);
            label2.Name = "label2";
            label2.Size = new Size(100, 58);
            label2.TabIndex = 5;
            label2.Text = "HMS";
            //
            // label3
            //
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("Bebas Neue", 40F);
            label3.ForeColor = Color.Navy;
            label3.Location = new Point(1, 81);
            label3.Name = "label3";
            label3.Size = new Size(134, 58);
            label3.TabIndex = 6;
            label3.Text = "ESOFT";
            //
            // label4
            //
            label4.AutoSize = true;
            label4.BackColor = Color.Transparent;
            label4.Font = new Font("Calibri", 40F);
            label4.ForeColor = Color.White;
            label4.Location = new Point(452, 28);
            label4.Name = "label4";
            label4.Size = new Size(146, 66);
            label4.TabIndex = 7;
            label4.Text = "Good";
            label4.Click += label4_Click;
            //
            // label5
            //
            label5.AutoSize = true;
            label5.BackColor = Color.Transparent;
            label5.Font = new Font("Open Sans", 13F, FontStyle.Bold);
            label5.ForeColor = Color.Azure;
            label5.Location = new Point(568, 111);
            label5.Name = "label5";
            label5.Size = new Size(97, 26);
            label5.TabIndex = 8;
            label5.Text = "Loading...";
            //
            // pictureBox1
            //
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(115, 88);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(39, 49);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 9;
            pictureBox1.TabStop = false;
            //
            // MainForm
            //
            BackColor = Color.LightSteelBlue;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(832, 493);
            Controls.Add(pictureBox1);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lblTitle);
            Controls.Add(btnPatient);
            Controls.Add(btnDoctor);
            Controls.Add(btnAppointment);
            Controls.Add(btnBilling);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Hospital Management System";
            TransparencyKey = Color.DarkSlateGray;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void btnDoctor_Click(object sender, EventArgs e)
        {
            OpenDoctorForm();
        }

        private void OpenDoctorForm()
        {
            try
            {
                using var form = new DoctorForm();
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Doctor Form: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnPatient_Click(object sender, EventArgs e)
        {
            OpenPatientForm();
        }

        private void OpenPatientForm()
        {
            try
            {
                using var form = new PatientForm();
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Patient Form: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAppointment_Click(object sender, EventArgs e)
        {
            OpenAppointmentForm();
        }

        private void OpenAppointmentForm()
        {
            try
            {
                using var form = new AppointmentForm();
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Appointment Form: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBilling_Click(object sender, EventArgs e)
        {
            OpenBillingForm();
        }

        private void OpenBillingForm()
        {
            try
            {
                using var form = new BillingForm();
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Billing Form: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private System.ComponentModel.IContainer components;
        private Button btnPatient;
        private Button btnDoctor;
        private Button btnAppointment;
        private Button btnBilling;
        private Label lblTitle;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private PictureBox pictureBox1;

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateGreeting();
            label4.BringToFront();
            greetingTimer.Start();
        }

        private void label4_Click(object sender, EventArgs e)
        {
            UpdateGreeting();
        }

        private void greetingTimer_Tick(object sender, EventArgs e)
        {
            UpdateGreeting();
            label4.BringToFront();
        }

        private void UpdateGreeting()
        {
            var hour = DateTime.Now.Hour;
            string greeting;

            if (hour >= 5 && hour < 12)
                greeting = "Good Morning";
            else if (hour >= 12 && hour < 17)
                greeting = "Good Afternoon";
            else if (hour >= 17 && hour < 21)
                greeting = "Good Evening";
            else
                greeting = "Good Night";

            label4.Text = greeting;
            label4.ForeColor = Color.Black;
            label4.BringToFront();
        }

        // Shows the user that actually logged in. The old version fetched the
        // first active Admin row, which could name someone else entirely.
        private void LoadLoggedInUser()
        {
            label5.Text = string.IsNullOrWhiteSpace(AppSession.Current?.Username)
                ? "Admin"
                : AppSession.Current.Username;
        }
    }
}
