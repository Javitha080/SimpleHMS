# Contributing

Thanks for helping improve SimpleHMS! This is a small .NET 9 WinForms
application for clinic management — patient/doctor records, appointments,
billing with PDF and email receipts.

## Getting started

1. Prerequisites: .NET 9 SDK, SQL Server (Express or LocalDB), Windows
   (WinForms app).
2. `dotnet restore SimpleHMS.csproj`
3. `dotnet build SimpleHMS.csproj`
4. Create the database: run `Database/Setup_SimpleHospitalDB.sql` (SSMS or
   sqlcmd), or just start the app and accept the automatic setup dialog.
5. Default login: `admin` / `admin123`.

## Tests

- `dotnet test tests/SimpleHMS.Tests/SimpleHMS.Tests.csproj --filter "Category!=Integration"`
  runs the fast unit tests — no database needed.
- Integration tests run against a real SQL Server. They provision from the
  setup script automatically and skip with a message when no reachable server
  is found. Point them at your own instance with:

  ```powershell
  $env:HMS_CONNECTION_STRING = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SimpleHospitalDB;Integrated Security=True;TrustServerCertificate=True;"
  dotnet test tests/SimpleHMS.Tests/SimpleHMS.Tests.csproj --filter "Category=Integration"
  ```

CI runs both categories on every pull request (GitHub-hosted runner with SQL
Server LocalDB for integration tests).

## Ground rules

- Every claim in `docs/ARCHITECTURE.md` and `docs/DATABASE.md` is verified
  against the source; keep those docs in sync when behavior changes.
- SQL access goes through `DB.cs` / repositories with parameterized queries —
  never concatenate user input into SQL.
- User-facing validation lives in `Services/Validators.cs`; form code should
  call it rather than re-rolling its own regexes.
- Keep the change log (`docs/CHANGELOG.md`) updated.
- Follow `.editorconfig`; keep code and comments concise and honest.

## Sending changes

1. Create a feature branch (`git checkout -b my-feature`).
2. Commit with clear messages.
3. Open a pull request against `main`; CI must pass (build + both test
   categories).
