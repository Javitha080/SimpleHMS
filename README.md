# SimpleHMS — Hospital Management System

[![CI](https://github.com/Javitha080/SimpleHMS/actions/workflows/ci.yml/badge.svg)](https://github.com/Javitha080/SimpleHMS/actions/workflows/ci.yml)
[![Release](https://github.com/Javitha080/SimpleHMS/actions/workflows/release.yml/badge.svg)](https://github.com/Javitha080/SimpleHMS/actions/workflows/release.yml)
[![CodeQL](https://github.com/Javitha080/SimpleHMS/actions/workflows/codeql.yml/badge.svg)](https://github.com/Javitha080/SimpleHMS/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A Windows desktop app for managing patients, doctors, appointments, and billing at a small clinic, built with C# / .NET 9 Windows Forms and SQL Server.

## Download

Prebuilt, self-contained Windows builds are attached to each
[GitHub Release](https://github.com/Javitha080/SimpleHMS/releases)
(`SimpleHMS-<version>-win-x64.zip`, with a SHA256 checksum file). They include the
.NET 9 runtime — nothing needs to be installed. Unzip and run `SimpleHMS.exe`,
then verify the engagement metadata version matches `docs/CHANGELOG.md` for the
release you downloaded.

## Features

- **Login** — account check against the `Users` table; passwords are stored as PBKDF2-SHA256 hashes, and legacy plain-text passwords are re-hashed automatically after a successful login.
- **First-run database setup** — when the database is missing or unreachable, the login screen offers to run the bundled setup script automatically (`Data/DatabaseManager`); no SSMS visit required.
- **Patient Registration** — add, update, delete patient records (name, age, gender, phone, email, address).
- **Doctor Registration** — add, update, delete doctor records (name, specialization, phone, consultation fee).
- **Appointments** — book, update, delete appointments with duplicate-slot, past-date, and doctor-double-booking protection.
- **Billing** — itemized bills (consultation / medicine / test / other), discounts, payment status tracking, auto-generated bill numbers (`BILL-YYYY-NNNN`) that retry cleanly on a rare same-instant collision between two workstations.
- **PDF receipts** — professional invoice PDF rendered from `BillTemplate.html` (rendering lives in `Services/PdfService`, IronPdf).
- **Email receipts** — the saved bill is emailed to the patient as a PDF attachment (`Services/EmailService`, plain `System.Net.Mail`; configured by `email.config.json` or environment variables).

## Tech stack

| Component     | Technology                     |
|---------------|--------------------------------|
| UI            | WinForms, .NET 9 (`net9.0-windows`) |
| Data access   | ADO.NET, Microsoft.Data.SqlClient 6.x |
| Database      | SQL Server (`SimpleHospitalDB`) |
| PDF           | IronPdf                        |

## Quick start

### 1. Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (9.0.x)
- SQL Server (Express is fine) or an existing SQL Server instance you can reach locally

### 2. Create the database

**Automatic (recommended):** just run the app — when it cannot find `SimpleHospitalDB` it asks
"Do you want to set it up automatically now?" and runs the setup script itself.

**Manual:** run [`Database/Setup_SimpleHospitalDB.sql`](Database/Setup_SimpleHospitalDB.sql) against your SQL Server instance (SSMS or `sqlcmd`). The script is idempotent and safe to run repeatedly; it creates the database, all tables, indexes, a seeded default admin, and a small set of demo records:

```powershell
sqlcmd -S localhost\SQLEXPRESS -i Database\Setup_SimpleHospitalDB.sql -E   # Windows auth
```

### 3. Build and run

```powershell
dotnet build SimpleHMS.csproj
dotnet run --project SimpleHMS.csproj
```

(Or press F5 in Visual Studio / VS Code — `.vscode/tasks.json` has build/publish/watch tasks.)

### 4. Log in

Default seeded credentials from the setup script:

| Username | Password   |
|----------|------------|
| `admin`  | `admin123` |

If the `Users` table was created without the seed rows, the app automatically creates this same admin on startup.

> **Change the password after first login.** There is no password-change UI yet (see roadmap); edit the row in the database or generate a new hash with `PasswordHasher.Hash("newpassword")`.

## Tests

The solution has an xUnit test suite in `tests/SimpleHMS.Tests` with two categories:

- **Unit** (`Category!=Integration`) — pure logic: password hashing, validators,
  bill HTML rendering, email config loading. No database required.
- **Integration** (`Category=Integration`) — real SQL Server: repository CRUD,
  bill-number uniqueness, the seed admin, and automatic provisioning. They
  provision `SimpleHospitalDB` from the bundled setup script themselves and
  **skip with a message** when no SQL Server is reachable.

```powershell
# Fast unit tests only (no database needed)
dotnet test tests/SimpleHMS.Tests/SimpleHMS.Tests.csproj --filter "Category!=Integration"

# Integration tests against your instance (skips automatically if unreachable)
$env:HMS_CONNECTION_STRING = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SimpleHospitalDB;Integrated Security=True;TrustServerCertificate=True;"
dotnet test tests/SimpleHMS.Tests/SimpleHMS.Tests.csproj --filter "Category=Integration"
```

CI (`.github/workflows/ci.yml`) runs both categories on every pull request and
push to `main`; the integration job uses the preinstalled SQL Server LocalDB on
GitHub's Windows runners.

## Configuration

### Database connection

The connection string points at `localhost\SQLEXPRESS` by default. To target a different instance/server, set the environment variable before launching:

```powershell
$env:HMS_CONNECTION_STRING = "Data Source=YOURSERVER;Initial Catalog=SimpleHospitalDB;Integrated Security=True;TrustServerCertificate=True;"
```

### Email receipt configuration

The "Send Email" button in Billing sends the selected bill as a PDF. Configure SMTP
in one of two ways:

1. **`email.config.json`** next to the executable (`bin/Debug/net9.0-windows/...`):

   ```json
   {
     "Host": "smtp.example.com",
     "Port": 587,
     "UseSsl": true,
     "FromAddress": "clinic@example.com",
     "FromName": "MediCare Plus",
     "Username": "clinic@example.com",
     "Password": "your-app-password"
   }
   ```

2. **Environment variables** (used when the JSON file is missing): `HMS_SMTP_HOST`,
   `HMS_SMTP_PORT` (default 587), `HMS_SMTP_SSL` (`false` to disable),
   `HMS_SMTP_FROM`, `HMS_SMTP_FROM_NAME`, `HMS_SMTP_USER`, `HMS_SMTP_PASS`.

Notes:
- Use port **587** (STARTTLS). `System.Net.Mail` does not support the old
  implicit-SSL port 465.
- The patient must have an email address in Patient Registration (the column
  `Patients.Email` is optional).
- Leave `Username` empty to hand off to the local SMTP relay with Windows auth.
- The config file stays on the workstation, so treat it like any other credential.

## Project structure

```
Program.cs            Entry point (starts LoginForm; global error handlers)
AppSession.cs         Current-user session state shared across forms
DB.cs                 Static ADO.NET helper (query/execute/scalar, logged)
Security/
  PasswordHasher.cs  PBKDF2-SHA256 hashing + verification
Data/
  DatabaseManager.cs  Server reachability + automatic DB provisioning
Services/
  AppLogger.cs        Daily rolling log file under logs\
  Validators.cs       Shared validation rules + numeric KeyPress guards
  PdfService.cs       Bill HTML/PDF rendering (IronPdf + BillTemplate.html)
  EmailService.cs     SMTP sender + email.config.json/env loading
Repositories/
  UserRepository.cs       Login + admin seeding
  PatientRepository.cs    Patients CRUD
  DoctorRepository.cs     Doctors CRUD + fee lookup
  AppointmentRepository.cs Appointments CRUD + duplicate/doctor-conflict checks
  BillRepository.cs       Bills + items (transactional save), PDF data
LoginForm.cs(.Designer)  Login screen (+ first-run setup dialog)
MainForm.cs              Main menu + greeting
PatientForm.cs           Patient management UI (incl. email field)
DoctorForm.cs            Doctor management UI
AppointmentForm.cs       Appointment booking UI
BillingForm.cs           Billing + bill items + PDF/email export UI
BillTemplate.html        Invoice HTML template (placeholder-based)
Database/
  Setup_SimpleHospitalDB.sql  Idempotent DB + seed setup script
tests/
  SimpleHMS.Tests/       xUnit tests (unit + SQL integration)
```

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — how the layers fit, data flow, auth flow
- [Database](docs/DATABASE.md) — schema, relationships, scripts, credentials
- [Changelog](docs/CHANGELOG.md) — fixes applied to the v1.4 codebase

## Contributing

Bug reports and pull requests are welcome — see
[CONTRIBUTING.md](CONTRIBUTING.md) and [SECURITY.md](SECURITY.md) (for
vulnerabilities, please use the private advisory flow rather than a public
issue).

## License

Released under the [MIT License](LICENSE). Note that PDF rendering depends on
[IronPdf](https://ironsoftware.com/csharp/pdf/), a commercial library — review
your own IronPdf licensing terms before redistributing builds.

## Roadmap

Items intentionally left out so far: password-change UI, user management, reports/dashboard, appointment calendar view. Already implemented after v1.5: patient email + email receipts, DB auto-provisioning, doctor-overlap check, file logging, global error handlers.
