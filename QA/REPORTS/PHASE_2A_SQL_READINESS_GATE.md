# Phase 2A — SQL readiness gate

Date: 2026-09-30. Read-only checks only; no SQL or storage mutations.

## Observed

- `sqlcmd -S localhost -E -C` connected to local SQL Server Developer Edition 17.0.1000.7.
- `SERVERPROPERTY('IsIntegratedSecurityOnly') = 1`: this instance accepts Windows authentication only.
- The current Windows login has `IS_SRVROLEMEMBER('sysadmin') = 1` and `CREATE ANY DATABASE` permission.
- `contained database authentication` has `value_in_use = 0`.
- No database named `AcademyDesk_QA_%` was returned by the read-only existence query.
- Docker CLI is present, but the Docker Desktop Linux engine is not running.

## Decision

**STOP before QA DB creation or migrations.** The current test process would connect as a SQL sysadmin, which violates the Phase 2A requirement for run-scoped runtime credentials. Enabling mixed authentication or contained database authentication would change server-wide configuration; creating a separate Windows account would change local machine identity; starting Docker Desktop requires an external-state change. None was assumed from a generic continuation request.

The safe next route is an isolated SQL container once Docker Desktop is available, with separate setup and limited runtime credentials. An explicitly approved dedicated local Windows test account is an alternative. Recheck exact server identity, database absence, credential scope and the guard before creating any run-owned resource. No P0 reproduction or HTTP/SQL application test has run yet.
