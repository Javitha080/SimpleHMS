namespace SimpleHMS.Services
{
    // Minimal file logger: one file per day under <exe>\logs. Logging must
    // never crash the app, so every failure inside this class is swallowed.
    public static class AppLogger
    {
        private static readonly object Gate = new();

        public static void Info(string message) => Write("INFO ", message, null);

        public static void Error(string context, Exception? ex) =>
            Write("ERROR", context, ex == null ? null : ex.ToString());

        private static void Write(string level, string message, string? details)
        {
            try
            {
                lock (Gate)
                {
                    string dir = Path.Combine(AppContext.BaseDirectory, "logs");
                    Directory.CreateDirectory(dir);

                    string line =
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}" +
                        (string.IsNullOrEmpty(details) ? "" : Environment.NewLine + details) +
                        Environment.NewLine;

                    File.AppendAllText(Path.Combine(dir, $"hms-{DateTime.Now:yyyyMMdd}.log"), line);
                }
            }
            catch
            {
                // Never let logging break the caller (e.g. read-only install dir).
            }
        }
    }
}
