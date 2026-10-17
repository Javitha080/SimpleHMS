using SimpleHMS.Services;

namespace SimpleHMS
{
    // Program - Entry point of the application (.NET 9 version)
    // This is where the program starts running
    internal static class Program
    {
        // Main() - The first method that runs when program starts
        // [STAThread] is required for Windows Forms applications
        [STAThread]
        static void Main()
        {
            // .NET 9 uses ApplicationConfiguration instead of Application.EnableVisualStyles()
            // This configures the application for high DPI displays and modern visual styles
            ApplicationConfiguration.Initialize();

            // Route anything that escapes a form handler into the log file and a
            // readable dialog instead of an unreadable .NET crash dialog.
            AppLogger.Info("Application starting.");

            Application.ThreadException += (s, e) => ShowUnhandledException(e.Exception, "Unexpected Error");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    AppLogger.Error("Unhandled non-UI exception (process is terminating)", ex);
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                AppLogger.Error("Unobserved task exception", e.Exception);
                e.SetObserved();
            };

            // Run the application and show the login window; the main menu
            // opens after a successful login.
            var loginForm = new LoginForm();
            Application.Run(loginForm);

            // When MainForm is closed, the program exits
        }

        private static void ShowUnhandledException(Exception ex, string title)
        {
            AppLogger.Error(title, ex);
            MessageBox.Show(
                $"Something went wrong and the last operation was stopped.\n\n{ex.Message}\n\n" +
                "Technical details were saved to the log file in the logs\\ folder.",
                title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
