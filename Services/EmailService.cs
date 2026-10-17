using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.Json;

namespace SimpleHMS.Services
{
    // SMTP settings for the email sender. Load order:
    //   1. email.config.json next to the executable
    //   2. HMS_SMTP_* environment variables
    // Ports: use 587 (STARTTLS). System.Net.Mail does not support the older
    // implicit-SSL port 465; for that, run a local relay instead.
    public sealed class EmailSettings
    {
        public string Host = "";
        public int Port = 587;
        public bool UseSsl = true;
        public string FromAddress = "";
        public string FromName = "";
        public string? Username;   // empty/missing → default Windows credentials instead of an explicit login
        public string? Password;
    }

    public static class EmailService
    {
        public static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "email.config.json");

        public static EmailSettings? TryLoadSettings()
        {
            return TryLoadFromJson() ?? TryLoadFromEnvironment();
        }

        private static EmailSettings? TryLoadFromJson()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return null;

                EmailSettings settings = new();

                using var document = JsonDocument.Parse(File.ReadAllText(ConfigFilePath));
                JsonElement root = document.RootElement;

                void SetString(string key, Action<string> setter)
                {
                    if (root.TryGetProperty(key, out JsonElement el) && el.ValueKind == JsonValueKind.String)
                        setter(el.GetString() ?? "");
                }

                SetString("Host", v => settings.Host = v.Trim());
                if (string.IsNullOrWhiteSpace(settings.Host))
                    return null;

                if (root.TryGetProperty("Port", out JsonElement portEl) &&
                    (portEl.ValueKind == JsonValueKind.Number && portEl.TryGetInt32(out int port)))
                    settings.Port = port;

                if (root.TryGetProperty("UseSsl", out JsonElement sslEl))
                    settings.UseSsl = sslEl.ValueKind == JsonValueKind.True ||
                        (sslEl.ValueKind == JsonValueKind.String &&
                         string.Equals(sslEl.GetString(), "true", StringComparison.OrdinalIgnoreCase));

                SetString("FromAddress", v => settings.FromAddress = v.Trim());
                SetString("FromName", v => settings.FromName = v.Trim());
                SetString("Username", v => settings.Username = v.Trim());
                SetString("Password", v => settings.Password = v);
                return settings;
            }
            catch (JsonException ex)
            {
                AppLogger.Error("email.config.json is not valid JSON", ex);
                return null;
            }
            catch (IOException ex)
            {
                AppLogger.Error("Could not read email.config.json", ex);
                return null;
            }
        }

        private static EmailSettings? TryLoadFromEnvironment()
        {
            string? host = Environment.GetEnvironmentVariable("HMS_SMTP_HOST");
            if (string.IsNullOrWhiteSpace(host))
                return null;

            return new EmailSettings
            {
                Host = host.Trim(),
                Port = int.TryParse(Environment.GetEnvironmentVariable("HMS_SMTP_PORT"), out int port) ? port : 587,
                UseSsl = !string.Equals(Environment.GetEnvironmentVariable("HMS_SMTP_SSL"), "false", StringComparison.OrdinalIgnoreCase),
                FromAddress = (Environment.GetEnvironmentVariable("HMS_SMTP_FROM") ?? "").Trim(),
                FromName = (Environment.GetEnvironmentVariable("HMS_SMTP_FROM_NAME") ?? "").Trim(),
                Username = TrimOrNull(Environment.GetEnvironmentVariable("HMS_SMTP_USER")),
                Password = Environment.GetEnvironmentVariable("HMS_SMTP_PASS")
            };

            static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // Sends one message; the attachment (usually a PDF receipt) is opened
        // and closed by the caller.
        public static async Task SendAsync(
            EmailSettings settings, string toAddress, string subject, string htmlBody, string? attachmentPath)
        {
            using MailMessage message = new();
            message.From = string.IsNullOrWhiteSpace(settings.FromName)
                ? new MailAddress(settings.FromAddress)
                : new MailAddress(settings.FromAddress, settings.FromName);
            message.To.Add(new MailAddress(toAddress));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            if (!string.IsNullOrEmpty(attachmentPath))
                message.Attachments.Add(new Attachment(attachmentPath, MediaTypeNames.Application.Pdf));

            using SmtpClient client = new(settings.Host, settings.Port)
            {
                EnableSsl = settings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30_000
            };

            if (string.IsNullOrWhiteSpace(settings.Username))
            {
                client.UseDefaultCredentials = true;
            }
            else
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(settings.Username, settings.Password ?? "");
            }

            await client.SendMailAsync(message);
        }
    }
}
