# Changelog

All changes were made after a full line-by-line read of the codebase. Package
versions and the .NET 9 target were kept as-is (both current).

## [1.6.0]

Release 1.6.0 — the v1.6 feature set (v1.6 upgrades plus repository
shipping: xUnit tests, CI/CD, packaging).

### New: infrastructure services
- **`Services/AppLogger`** — file logging to `logs/hms-YYYYMMDD.log` next to the
  executable. All `DB.*` calls, failed saves/deletes, PDF/email failures and
  unexpected exceptions are recorded with timestamps and stack traces. Logging
  never crashes the app (a read-only install directory simply means no logs).
- **Global error handlers** (`Program.cs`) — `Application.ThreadException`,
  `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` now
  show a readable dialog instead of the .NET crash box and write to the log.
- **`Services/Validators`** — one shared source for the name/specialization/
  phone/email rules plus numeric `KeyPress` guards (`AllowDigits_KeyPress`,
  `AllowDecimal_KeyPress`). Patient, Doctor and Billing forms now use them, so
  the three copies of the same regexes are gone and fee/quantity fields reject
  non-numeric typing and pasting.
- **`Data/DatabaseManager`** — server reachability check and automatic database
  provisioning: splits `Setup_SimpleHospitalDB.sql` at `GO` lines and executes
  the batches; the script is copied to the output folder by the project file.

### New: first-run setup
- The login screen's startup check now offers "Does the database need setting up?"
  — choosing Yes runs the provisioning flow (create DB, tables, seeds, default
  admin) and reports the outcome. Manual setup with the same script still works.

### New: email receipts
- **`Services/EmailService`** — SMTP sender using only `System.Net.Mail`
  (no new package). Settings come from `email.config.json` next to the
  executable or `HMS_SMTP_*` environment variables (JSON wins). Port 587/
  STARTTLS is the documented path; empty `Username` uses default credentials.
- Billing form gains a **Send Email** button: it renders the bill PDF with the
  new `Services/PdfService`, attaches it and sends it to the patient's email
  address with a proper error/workflow (no email configured → config guidance;
  patient without email → hint to add one; failures go to the log).
- Patients have a new optional **Email** field (validated when present, stored
  NULL when empty): `Patients.Email NVARCHAR(100)` in the schema, repository
  insert/update, the patient form, and the bill PDF patient lookup. The setup
  script also upgrades pre-existing databases in place (conditional `ALTER`)
  and backfills the sample patients' emails.

### New: PDF pipeline refactor
- `Services/PdfService` owns the bill HTML build + render (all 20 template
  tokens, HTML-escaping, A4 margins), so BillingForm's print path and the new
  email path share one implementation; the temp HTML file is always cleaned up.
- `Bills` save race closed: `BillRepository.SaveBill` retries with a fresh bill
  number when two workstations grab the same sequential number in the same
  instant (SQL 2601/2627, up to 3 attempts, timestamp fallback), and the form
  shows the regenerated number after saving.

### Appointment scheduling
- **Doctor double-booking blocked**: `AppointmentRepository.ExistsDoctorConflict`
  refuses a slot already held by the same doctor (different patient or not) on
  both book and edit paths, alongside the existing duplicate-patient check.

### Documentation
- README: first-run setup, email configuration section, updated structure/roadmap.
- ARCHITECTURE and DATABASE updated for the new services/flow; this file.

## [1.5.0]

### Security
- **Hashed passwords.** Users' passwords are now verified with PBKDF2-SHA256
  (100k iterations, random salt, constant-time compare) via the new
  `Security/PasswordHasher`. Legacy plain-text passwords still work and are
  re-hashed transparently the first time their owner logs in.
- **Never-locked-out first run.** `UserRepository.EnsureSeededAdminExists()`:
  when the `Users` table has no active rows, `admin` / `admin123` is created
  automatically. Previously a fresh database had no users at all and nobody
  could log in.
- Honest "Database Unavailable / Database Error" dialogs on login; before, a
  SQL outage showed "Invalid username or password" and could crash the app.

### Auth/session
- Real login state: new `AppSession.Current` (`UserInfo` record). `MainForm`
  now shows **who actually logged in** — it used to display whichever admin
  row happened to be first.
- `MainForm.FormClosed` clears the session (closing the app = logging out).
- Login form: "Email" label corrected to "Username", Enter key submits.

### Bug fixes
- **Billing totals double-counted consultation items** (`UpdateFeeSummary`
  added a consultation item's total both in the item loop and again via
  `consultationFee`). Doctor's default fee is now added exactly once and only
  when no consultation item exists.
- **Double-booked appointments** — same patient + doctor + date + time can no
  longer be created (book path blocks, edit path excludes itself);
  `AppointmentRepository.ExistsDuplicate` backs this up.
- **Past-date bookings blocked** on new appointments.
- PDF generation no longer runs the patient/doctor info queries twice per
  print; pending bills print `-` for the payment date instead of the current
  date, and a NULL `Bills.BillDate` no longer crashes the date picker.
- "New" in Billing no longer leaves the payment status empty (which made
  validation fail until it was picked manually).
- Deleting a patient/doctor referenced by appointments or bills now explains
  the situation instead of dumping the raw SQL exception (SQL error 547).

### Dead code removed
- `Properties/Resources.resx` + `Properties/Resources.Designer.cs` and their
  two orphan PNG assets — no code referenced `Properties.Resources`; the
  window images live inside `LoginForm.resx`/`MainForm.resx`.
- Legacy `SimpleHospitalDB.sql` (SSMS dump with hardcoded `MSSQL16` file paths,
  no seed users) — replaced by `Database/Setup_SimpleHospitalDB.sql`.
- `DB.GetConnectionString()` and `DB.IsConnected()` (zero callers after the
  auth/session fixes; connectivity is now proven by seeding the admin).
- `UserRepository.GetActiveAdminName()` (superseded by `AppSession`).
- Stray "// ← This line" comments in `MacOSButton`, stale run-main-window
  comment in `Program.cs`, and the cosmetic "Login successful" interstitial
  message box.

### Documentation (new / rewritten)
- `README.md` — rewritten to match reality (previous text advertised an email
  feature, iText7 and .NET Framework 4.7.2; none were true — the app uses
  IronPdf, WinForms on .NET 9, no email).
- `docs/ARCHITECTURE.md` — layer map, data flows, auth flow, roadmap.
- `docs/DATABASE.md` — schema, FKs, indexes, seeds, connection configuration.
- `docs/CHANGELOG.md` — this file.

### Known limitations (documented, not hidden)
- ~~Bill-number generation has a theoretical race~~ — fixed in v1.6 (retry loop).
- No password-change/user-management UI yet; see ARCHITECTURE § Roadmap.
- `BillTemplate.html` pulls Google Fonts from the CDN — PDF layout degrades
  gracefully to system fonts when offline.
