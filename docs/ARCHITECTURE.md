# Architecture

> Written from a line-by-line code review of this repository — every claim below
> matches the current source, not an older design doc.

## 1. System overview

Single-process WinForms desktop application. There is no web API/service layer:
every "API" is a static repository method talking to SQL Server through a thin
ADO.NET helper. Flow after launch:

```
Program.Main()
   └─ Application.Run(LoginForm)
        │ (successful login → AppSession.Start(user))
        └─ new MainForm().Show()      ← non-modal, menu window
             ├─ PatientForm      (ShowDialog)
             ├─ DoctorForm       (ShowDialog)
             ├─ AppointmentForm  (ShowDialog)
             └─ BillingForm      (ShowDialog)
```

Closing `MainForm` closes the hidden `LoginForm`, the message loop ends, and the
process exits. `MainForm.FormClosed` also clears `AppSession`.

## 2. Layers

### Presentation (`*.cs` forms)

- `LoginForm` — borderless custom chrome (drag-to-move, macOS-style traffic
  light buttons from `MacOSButton`). Validates input, calls `UserRepository`,
  stores the authenticated user in `AppSession`, then opens `MainForm`.
  `AcceptButton` enables Enter-to-login.
- `MainForm` — menu of the four management dialogs, plus a greeting label
  refreshed on a 60 s timer. Shows `AppSession.Current.Username`.
- `PatientForm` / `DoctorForm` — classic master-detail CRUD: input row + action
  buttons + read-only `DataGridView`; row click fills the inputs.
- `AppointmentForm` — patient/doctor pickers + date/time pickers + grid; prevents
  double-booked slots and past-date bookings.
- `BillingForm` — the largest screen. Three work areas: bill header
  (patient/doctor/appointment link), fee breakdown computed from bill items +
  doctor default fee minus discount, and an in-memory `billItemsTable`
  (`DataTable`) edited through Add/Update/Remove before one transactional save.
  Includes the PDF exporter.

### Data access (`Repositories/*` + `DB.cs`)

- `DB` — static helpers over `SqlConnection/SqlCommand/SqlDataAdapter`.
  `GetData` (SELECT to `DataTable`), `SetData` (DML, affected row count),
  `ExecuteScalar` (single value). All call sites pass `SqlParameter`s, so user
  input never concatenates into SQL. DDL/DML defaults:
  `HMS_CONNECTION_STRING` env var → else `localhost\SQLEXPRESS`. Every helper
  logs failures through `AppLogger` before rethrowing. `DB.ConnectionString`
  (internal) feeds `DatabaseManager`'s master-database connection.
- `UserRepository` — credential flow. Fetches the active row by username,
  verifies the stored value:
  - `PBKDF2$…` format → PBKDF2-SHA256 constant-time verification;
  - legacy plain text → equality compare, then the row is transparently
    re-hashed on success (one-time migration).
- `PatientRepository`, `DoctorRepository` — straightforward CRUD. Patients
  carry an optional `Email` (empty → NULL in storage).
- `AppointmentRepository` — CRUD + `GetForPatientDoctor` (billing link) +
  `ExistsDuplicate` (identical patient/doctor/slot) +
  `ExistsDoctorConflict` (doctor already holds the exact slot, any patient).
- `BillRepository` — `GenerateBillNumber` (`BILL-YYYY-NNNN` from max sequence,
  falls back to a timestamp on failure), bill list/detail/items queries,
  transactional `SaveBill` (header upsert + items delete-all/re-insert inside one
  transaction; `BillItems` is FK-cascaded, so deleting a bill is likewise
  transactional), and the payment-method list (fixed set + distinct values
  observed in `Bills`). `SaveBill` retries a new bill at most 3 times when the
  sequential number was taken by another workstation in the same instant
  (SQL 2601/2627; last attempt uses a timestamp number).

### Security (`Security/PasswordHasher.cs` + `AppSession.cs`)

- `PasswordHasher.Hash` — PBKDF2 (HMAC-SHA256, 100 000 iterations, 16-byte
  random salt, 32-byte derived key) serialized as
  `PBKDF2$<iterations>$<base64 salt>$<base64 hash>`.
- `PasswordHasher.Verify` — re-derives and compares with
  `CryptographicOperations.FixedTimeEquals`.
- `AppSession.Current` — `UserInfo(UserId, Username, UserType)` record set right
  after successful validation; consumed by `MainForm` (header) and available for
  future role checks (`UserInfo.IsAdmin`).

### Services (`Services/*`) and provisioning (`Data/DatabaseManager.cs`)

- `AppLogger` — appends to `logs/hms-YYYYMMDD.log` beside the executable
  (always creates the folder); every failure inside the logger is swallowed so
  logging can never break the caller.
- `Validators` — the shared validation rules (name, specialization, phone,
  email; email optional-but-well-formed for patients) and the numeric
  `KeyPress` guards wired onto age/fee/quantity/price/discount fields.
- `PdfService` — `BillPdfModel` (display strings captured once from the UI) +
  `RenderBillToFile` (template → HTML with all tokens replaced/encoded →
  IronPdf A4 render → temp-file cleanup in `finally`).
- `EmailService` — `TryLoadSettings` (`email.config.json` first, then
  `HMS_SMTP_*` env vars) + `SendAsync` (HTML body, optional attachment,
  STARTTLS via port 587; empty `Username` → default credentials).
- `DatabaseManager` — `MasterConnectionString` derived from
  `DB.ConnectionString` with `Initial Catalog = master`;
  `IsServerReachable` (used by… login flow), `DatabaseExists`,
  `ProvisionDatabase` (creates the DB when missing and executes the read-only
  copy of `Setup_SimpleHospitalDB.sql` split at `GO` lines, 120 s timeout per
  batch). The script is idempotent, so provisioned-by-app and provisioned-by-
  SSMS databases end up identical.

## 3. Key data flows

### Login
```
LoginForm bootstrap (after Shown, so slow servers don't paint-delay):
  StartupDatabaseCheck
    → UserRepository.EnsureSeededAdminExists()
    → on failure: ask "set up automatically?" → DatabaseManager.ProvisionDatabase()
      (create DB + run setup batches) → reseed admin → report result
LoginForm.btnLogin_Click
  → UserRepository.ValidateLogin(user, pass)
      → SELECT user by username (IsActive = 1)
      → verify hash (or migrate plain text → hash)
      → AppSession.Start(UserInfo)
  → hide LoginForm, show MainForm
```

### Booking an appointment
```
AppointmentForm.BtnBook_Click
  → past-date guard (no new appointments before today)
  → AppointmentRepository.ExistsDuplicate(patient, doctor, date, time)
       // blocks the exact-same slot for the pair; edit path excludes itself
  → AppointmentRepository.ExistsDoctorConflict(doctor, date, time)
       // rejects a slot already held by the doctor for a different patient
  → AppointmentRepository.Insert(...)
```

### Creating/saving a bill
```
BillingForm
  New      → GenerateBillNumber (repository-side sequence query)
  Add item → billItemsTable.Rows.Add (qty × unit price computed live)
  Save     → UpdateFeeSummary (fee buckets from item types + doctor fee once)
             ValidateBillForm (bill #, patient, doctor, status, discount ≤ fees)
             → BillRepository.SaveBill(billId, header, items)
                  new bills: up to 3 attempts on unique violations (2601/2627)
                  single transaction:
                    INSERT/UPDATE Bills (SCOPE_IDENTITY / SELECT @BillID)
                    DELETE BillItems WHERE BillID = @BillID
                    INSERT each item row
  Print PDF   → BuildBillPdfModel → PdfService.RenderBillToFile → SaveFileDialog
  Send Email  → BuildBillPdfModel → render temp PDF → EmailService.SendAsync
               (SMTP config: email.config.json first, then HMS_SMTP_* env vars)
```

Fee summary rules (`UpdateFeeSummary`):
- Sum item totals into buckets by item type: `Medicine`, `Test`/`Lab`,
  `Consultation`, everything else → `Other`.
- If no consultation item exists, the selected doctor's fee is added as the
  consultation fee exactly once. Consultation items are NOT added twice.
- `Total = Σ items + doctorFee(if none itemized) − discount`.

### PDF receipt
`BillTemplate.html` uses `{Placeholder}` tokens. `Services/PdfService` owns the
whole pipeline: `BillPdfModel` captures the display strings from the form once,
`BuildBillHtml` replaces every token (free text HTML-encoded) and renders with
`ChromePdfRenderer` (A4, Print CSS media); the temp HTML file is removed in a
`finally` block. Payment-date token renders `-` for non-paid bills. Note: the
template loads Google Fonts from the CDN; without internet the layout falls
back to system fonts.

## 4. Cross-cutting behavior

- **Dense dialogs**: every input control has tooltips.
- **Delete safety**: FK-violating deletes (SQL error 547) surface friendly
  messages in Patient/Doctor/Appointment forms; bills cascade items.
- **Validation**: all regex rules and numeric KeyPress guards live in one place
  (`Services/Validators`) — names `^[a-zA-Z\s\.]+$` (dots allowed, e.g. "Dr."),
  specialization letters/dashes, phone `^[\d\s\-\+]{10,15}$`, email
  `^[^@\s]+@[^@\s]+\.[^@\s]+$` when present, patient age 1–100, doctor fee > 0,
  billing discount ≤ fee totals.
- **Nullable-friendly formatting**: `ComboBox.SelectedValue` interactions use
  `int.TryParse` before `Convert.ToInt32` in billing paths.
- **Logging**: every SQL failure, failed save/delete, PDF/email failure and
  unexpected exception lands in `logs/hms-YYYYMMDD.log`; unhandled exceptions
  show a readable dialog (global handlers in `Program.cs`).

## 5. Dead-code policy state

No known dead code remains after the v1.4 → v1.5 cleanup:
`Properties/Resources.*` (unused designer resource pair), `DB.GetConnectionString`,
`DB.IsConnected`, `UserRepository.GetActiveAdminName` and `MacOSButton` "← This
line" artifacts were removed because they had zero callers/references.

## 6. Roadmap

Ranked by value if this app grows:

1. **Password change panel** (self-service) and a **Users management screen**
   (admin-only; schema already has `UserType` + `IsActive` for roles).
2. **Reports / dashboard** — daily revenue, pending bills, appointment counts
   per doctor (all data exists; aggregation queries + a chart control).
3. **Appointment book view** — calendar/weekly grid instead of a flat list.
4. **Search & filters** on every grid (patients by phone/name, bills by status).
5. **Backup/restore helper** (SMO `Backup` task) for clinic-level safety.
6. Localization of all strings.

Already delivered after v1.5 (removed from the roadmap): DB auto-provisioning
on first run, doctor-overlap check, bill-number race hardening, and a real
file-logging sink.
