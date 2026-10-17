# Database: SimpleHospitalDB

Everything below reflects the shipped script
[`Database/Setup_SimpleHospitalDB.sql`](../Database/Setup_SimpleHospitalDB.sql)
and the queries in `Repositories/*`.

## Install

```powershell
sqlcmd -S localhost\SQLEXPRESS -i Database\Setup_SimpleHospitalDB.sql -E
```

The script is **idempotent**: every object is guarded by
`IF DB_ID(...) / IF OBJECT_ID(...) IS NULL`, so re-running it keeps existing data.
It uses SQL Server's default file locations — no hardcoded paths (the legacy
script pinned `C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\...`,
which failed on any other machine).

## Tables

```
Users ──(no FK; standalone auth table)
Patients ─┬─< Appointments >─┬─ Doctors
          │                  │
          └─< Bills >────────┘──(appointmentID optional link to Appointments)
                     └─< BillItems (ON DELETE CASCADE)
```

| Table | Purpose | Notes |
|-------|---------|-------|
| `Users` | login accounts | `Username` UNIQUE; `Password` stores a PBKDF2-SHA256 hash (`PasswordHasher` format); `UserType` ∈ Admin/Receptionist; `IsActive` soft-delete flag |
| `Patients` | patient records | `Age` 0–150 CHECK; `Gender` CHECK (Male/Female); optional `Email NVARCHAR(100)` — used by the billing screen to send PDF receipts |
| `Doctors` | doctor records | consultation `Fee` ≥ 0 CHECK |
| `Appointments` | bookings | FKs to Patients + Doctors; date `DATE` + time `TIME(7)` |
| `Bills` | bill headers | `BillNumber` UNIQUE `BILL-YYYY-NNNN`; fee buckets (consultation/medicine/test/other), discount, total; `PaymentStatus` CHECK ∈ Pending/Partial/Paid/Cancelled/Refunded; FKs to Patients, Doctors, optional Appointment |
| `BillItems` | itemized charges | FK to Bills `ON DELETE CASCADE`; `TotalPrice` stored = `Quantity × UnitPrice` |

Indexes beyond PKs/UNIQUEs: appointment slot lookup
`(PatientID, DoctorID, AppointmentDate, AppointmentTime)`, bills by patient,
items by bill.

## Seed data

| Data | Detail |
|------|--------|
| Admin user | `admin` / `admin123`, hash `PBKDF2$100000$YVE6Qpi91qMtcKG42Hi1lg==$xgfQfDM+wGxtgsqv8Ax6BTc4fJi8eeDLcF6nsyEMLa4=` |
| Doctors | Dr. Sarah Silva (Cardiology, 3500.00), Dr. Nuwan Perera (Pediatrics, 2500.00) |
| Patients | Kasun Fernando (`kasun.fernando@example.com`), Nimali Jayasuriya (`nimali.jayasuriya@example.com`) |

The hash above was produced with the same
`Security/PasswordHasher` parameters (PBKDF2-SHA256, 100 000 iterations,
16-byte salt, 32-byte key), so the app verifies it directly. As a belt-and-braces
backup, `UserRepository.EnsureSeededAdminExists()` creates the same admin at
application startup when the `Users` table has no active rows — a fresh database
can never lock you out, even when this SQL script was not run.

> Change the seeded password before real use. There is no password-change UI yet;
> until then, either run `UPDATE Users SET Password = '<new hash>' ...` with a
> hash from `PasswordHasher.Hash("...")` or let the legacy migration path work:
> temporarily insert a plain-text password, log in once, and the app re-hashes it.

## Column defaults worth knowing

- `Bills.BillDate` default `GETDATE()`, but the column is NULLable — the billing
  form code guards against NULL dates defensively.
- `BillItems.Quantity` default 1 (NULLable).
- `Users.IsActive` default 1.

## Connecting the app

`DB.cs` builds the connection from, in order:

1. `HMS_CONNECTION_STRING` environment variable (any instance/server, CI, LAN);
2. fallback: `Data Source=localhost\SQLEXPRESS;Initial Catalog=SimpleHospitalDB;Integrated Security=True;TrustServerCertificate=True;`

Integrated Security means the Windows account needs `db_datareader`/`db_datawriter`
on `SimpleHospitalDB` (default for local SQL Express).

## Creating / upgrading the database

Three equivalent paths — all run the same idempotent script:

1. **Automatic (first run):** the login screen detects a missing database and
   offers to provision it. `Data/DatabaseManager` connects to `master`, creates
   `SimpleHospitalDB` when absent, then executes the copy of
   `Database/Setup_SimpleHospitalDB.sql` that ships in the output folder
   (split at `GO` lines, batched sequentially).
2. **Manual:** run the script with SSMS or
   `sqlcmd -S localhost\SQLEXPRESS -i Database\Setup_SimpleHospitalDB.sql -E`.
3. **In place for old installs:** the script contains a conditional
   `IF NOT EXISTS (…sys.columns…Email) ALTER TABLE dbo.Patients ADD Email …`
   plus email backfills, so re-running it on a pre-v1.6 database adds the new
   column without touching existing data.

## Deleting records

| Delete | Behavior |
|--------|----------|
| Patient with appointments/bills | blocked by FK (SQL 547) → friendly message; delete the dependent appointments/bills first |
| Doctor with appointments/bills | same as above |
| Appointment referenced by a bill | blocked by FK → friendly message |
| Bill | deletes its `BillItems` automatically (cascade), inside a transaction |
