# Security policy

## Supported versions

| Version | Supported |
|---------|-----------|
| 1.6.x   | yes       |
| < 1.6   | no        |

## Reporting a vulnerability

Please do **not** report vulnerabilities through public GitHub issues.

Use GitHub's private security advisory flow:
**Security tab → Report a vulnerability**, or contact the maintainers so a fix
can be prepared and released before the details are published.

Please include: affected version, steps to reproduce (or a proof of concept),
and the impact you believe it enables. Expect an initial response within 7 days.

## Notes on third-party components

- PDF generation uses [IronPdf](https://ironsoftware.com/csharp/pdf/), which is
  a commercial library — its runtime licensing does not extend to other uses of
  this repository. Review your IronPdf licensing terms before redistributing
  builds.
- Database connectivity uses `Microsoft.Data.SqlClient`. The application ships
  with `TrustServerCertificate=True` for local development; if you deploy
  against a remote SQL Server, provide a certificate chain and disable that
  trust flag.
