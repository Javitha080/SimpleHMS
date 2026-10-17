using System.Drawing;
using System.Windows.Forms;
using SimpleHMS.Data;
using SimpleHMS.Repositories;
using SimpleHMS.Services;

namespace SimpleHMS
{
    public partial class LoginForm : Form
    {
        private MacOSButton closeButton;
        private MacOSButton minimizeButton;
        private MacOSButton maximizeButton;
        private Point lastPoint;

        public LoginForm()
        {
            InitializeComponent();
            txtpassword.PasswordChar = '•';
            AcceptButton = btnLogin;
            AddMacOSButtons();

            // Runs after the form is painted so a slow/unreachable SQL Server
            // cannot delay the window; the check verifies the Users table,
            // seeds a default admin and offers automatic database setup.
            Shown += (_, _) => StartupDatabaseCheck();

            MouseDown += LoginForm_MouseDown;
            MouseMove += LoginForm_MouseMove;
        }

        // Warms up the Users table so a fresh database is never locked out.
        // When the database cannot be reached or does not exist yet, this
        // offers to run the bundled setup script automatically.
        private void StartupDatabaseCheck()
        {
            try
            {
                UserRepository.EnsureSeededAdminExists();
                return;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Database check on the login screen failed", ex);
            }

            // Say what exactly failed before offering the automated setup.
            string problem = DatabaseManager.IsServerReachable(out string serverError)
                ? DatabaseManager.DatabaseExists()
                    ? "The database server is reachable and SimpleHospitalDB exists, but its tables could not be prepared."
                    : "The database server is reachable, but the SimpleHospitalDB database does not exist yet."
                : "SQL Server itself could not be reached:\n" + serverError;

            DialogResult choice = MessageBox.Show(
                problem + "\n\nSet it up automatically now?\n\n" +
                "Yes  - create the database, tables and a default admin (admin / admin123)\n" +
                "No   - close this dialog and fix the connection manually\n" +
                "        (start SQL Server localhost\\SQLEXPRESS, or set HMS_CONNECTION_STRING,\n" +
                "        or run Database\\Setup_SimpleHospitalDB.sql with any SQL tool).",
                "Database Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (choice != DialogResult.Yes) return;

            (bool success, string message) = DatabaseManager.ProvisionDatabase();
            if (success)
            {
                MessageBox.Show(
                    message + "\n\nDefault login: admin / admin123 (there is no password-change UI yet; update the row in the database after the first login).",
                    "Database Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);

                try
                {
                    UserRepository.EnsureSeededAdminExists();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Seeding the admin after provisioning failed", ex);
                }
            }
            else
            {
                MessageBox.Show(
                    "Automatic setup failed:\n\n" + message +
                    "\n\nYou can run Database\\Setup_SimpleHospitalDB.sql manually with SSMS or sqlcmd instead.",
                    "Setup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void LoginForm_MouseDown(object? sender, MouseEventArgs e)
        {
            lastPoint = new Point(e.X, e.Y);
        }

        private void LoginForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Left += e.X - lastPoint.X;
                Top += e.Y - lastPoint.Y;
            }
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtemail.Text) || string.IsNullOrWhiteSpace(txtpassword.Text))
            {
                MessageBox.Show("Please enter both username and password.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserInfo? user;
            try
            {
                user = UserRepository.ValidateLogin(txtemail.Text.Trim(), txtpassword.Text);
            }
            catch (Exception dbEx)
            {
                // A connection outage previously surfaced as "Invalid username
                // or password" and crashed here; report the real problem instead.
                MessageBox.Show(
                    "Cannot reach the database. Check that SQL Server is running and SimpleHospitalDB exists.\n\n" +
                    $"Details: {dbEx.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (user == null)
            {
                MessageBox.Show("Invalid username or password. Please try again.",
                    "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtpassword.Clear();
                txtpassword.Focus();
                return;
            }

            var mainForm = new MainForm();
            mainForm.FormClosed += (s, args) => Close();
            Hide();
            mainForm.Show();
        }
    }
}
