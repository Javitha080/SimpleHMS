using SimpleHMS.Services;
using Xunit;

namespace SimpleHMS.Tests.Unit
{
    // TryLoadSettings reads email.config.json from AppContext.BaseDirectory
    // (the test output folder) and falls back to HMS_SMTP_* environment
    // variables. Each test cleans up in Dispose, and tests inside one xUnit
    // class run sequentially, so the shared on-disk config cannot clash.
    public class EmailServiceTests : IDisposable
    {
        private static readonly string[] EnvKeys =
        {
            "HMS_SMTP_HOST", "HMS_SMTP_PORT", "HMS_SMTP_SSL",
            "HMS_SMTP_FROM", "HMS_SMTP_FROM_NAME",
            "HMS_SMTP_USER", "HMS_SMTP_PASS"
        };

        public void Dispose()
        {
            foreach (string key in EnvKeys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }

            DeleteConfigFile();
        }

        private static string ConfigPath => EmailService.ConfigFilePath;

        private static void DeleteConfigFile()
        {
            if (File.Exists(ConfigPath))
            {
                File.Delete(ConfigPath);
            }
        }

        private static void WriteConfigFile(string json)
        {
            File.WriteAllText(ConfigPath, json);
        }

        [Fact]
        public void ReturnsNull_WhenNoConfigFileAndNoEnvironment()
        {
            DeleteConfigFile();

            Assert.Null(EmailService.TryLoadSettings());
        }

        [Fact]
        public void Environment_HostOnly_UsesDefaultsForEverythingElse()
        {
            DeleteConfigFile();
            Environment.SetEnvironmentVariable("HMS_SMTP_HOST", "smtp.test.local");

            EmailSettings? settings = EmailService.TryLoadSettings();

            Assert.NotNull(settings);
            Assert.Equal("smtp.test.local", settings!.Host);
            Assert.Equal(587, settings.Port);
            Assert.True(settings.UseSsl);
        }

        [Fact]
        public void Environment_ReadsAllSupportedVariables()
        {
            DeleteConfigFile();
            Environment.SetEnvironmentVariable("HMS_SMTP_HOST", "smtp.test.local");
            Environment.SetEnvironmentVariable("HMS_SMTP_PORT", "2525");
            Environment.SetEnvironmentVariable("HMS_SMTP_SSL", "false");
            Environment.SetEnvironmentVariable("HMS_SMTP_FROM", "clinic@test.local");
            Environment.SetEnvironmentVariable("HMS_SMTP_FROM_NAME", "Test Clinic");
            Environment.SetEnvironmentVariable("HMS_SMTP_USER", "robot");
            Environment.SetEnvironmentVariable("HMS_SMTP_PASS", "pw");

            EmailSettings? settings = EmailService.TryLoadSettings();

            Assert.NotNull(settings);
            Assert.Equal("smtp.test.local", settings!.Host);
            Assert.Equal(2525, settings.Port);
            Assert.False(settings.UseSsl);
            Assert.Equal("clinic@test.local", settings.FromAddress);
            Assert.Equal("Test Clinic", settings.FromName);
            Assert.Equal("robot", settings.Username);
            Assert.Equal("pw", settings.Password);
        }

        [Fact]
        public void JsonFile_ReadsAllSupportedFields()
        {
            DeleteConfigFile();
            WriteConfigFile(
                """{"Host":"smtp.json.local","Port":465,"UseSsl":false,"FromAddress":"pdf@test.local","FromName":"PDF Clinic","Username":"robot","Password":"pw"}""");

            EmailSettings? settings = EmailService.TryLoadSettings();

            Assert.NotNull(settings);
            Assert.Equal("smtp.json.local", settings!.Host);
            Assert.Equal(465, settings.Port);
            Assert.False(settings.UseSsl);
            Assert.Equal("PDF Clinic", settings.FromName);
            Assert.Equal("robot", settings.Username);
        }

        [Fact]
        public void JsonFile_WithoutHost_FallsBackToEnvironment()
        {
            DeleteConfigFile();
            Environment.SetEnvironmentVariable("HMS_SMTP_HOST", "smtp.env.local");
            WriteConfigFile("""{"Port":465}""");   // host missing → JSON loader bails

            EmailSettings? settings = EmailService.TryLoadSettings();

            Assert.NotNull(settings);
            Assert.Equal("smtp.env.local", settings!.Host);
            Assert.Equal(587, settings.Port);   // port ignored (JSON was rejected wholesale)
        }

        [Fact]
        public void JsonFile_InvalidJson_FallsBackToEnvironment()
        {
            DeleteConfigFile();
            Environment.SetEnvironmentVariable("HMS_SMTP_HOST", "smtp.env.local");
            WriteConfigFile("{ invalid json");

            EmailSettings? settings = EmailService.TryLoadSettings();

            Assert.NotNull(settings);
            Assert.Equal("smtp.env.local", settings!.Host);
        }
    }
}
