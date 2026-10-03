# Phase 2A — run-owned SQL provisioning proof

Date: 2026-09-30. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c` with pre-existing student UI changes preserved. This slice replaces the temporary readiness stop recorded in `PHASE_2A_SQL_READINESS_GATE.md`; that read-only observation remains historical evidence.

## Runtime setup

- Docker Desktop Linux engine was running. Microsoft SQL Server 2022 Developer image `mcr.microsoft.com/mssql/server:2022-latest` was pulled (digest `sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090`). The one-off container carried label `academydesk.qa.run=da9a72c13cba45f7808657cf11101f70`, published SQL only on `127.0.0.1`, and used a randomly generated administrator password passed without printing it.
- The guard now requires the exact run-owned Docker host port, database name, generated SQL runtime login and password for both DbContexts. Integrated Windows authentication and administrator credentials are rejected. The host-start and guard tests still pass.
- Added `QA/tools/SqlHarness`: it verifies Docker container name, image, run label, hostname and actual loopback port before connecting. It also verifies SQL server identity and database absence. It creates only `AcademyDesk_QA_da9a72c13cba45f7808657cf11101f70`, writes a run ID / target / token-digest ownership marker, creates a run-specific login with only `db_datareader` and `db_datawriter`, and applies both EF migration sets with the setup administrator account. Runtime login is checked through its own connection for no sysadmin, no database ALTER and no CREATE ANY DATABASE permission.
- Cleanup reads the ownership marker through a fresh SQL connection and requires exact manifest agreement. A deliberately mismatched marker was rejected before the callback could run. Database and login cleanup then ran through the approved boundary.

## Observed results

- First live SQL run: PASS, 79 application migrations and 7 Identity migrations; scoped runtime login verified; database and login removed.
- After the container restarted, Docker assigned a different host port. The harness rejected the stale port before SQL mutation. Using the current `docker port` value, the second run passed with the improved marker that records the target address inside SQL.
- Final read-only SQL query returned `NULL` for both the run database ID and runtime login ID. The exact run-owned container was stopped and removed; the pulled image remains cached. The run container's filesystem was removed by Docker and cannot be recovered from that container. No application data was stored there after cleanup.
- `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --verbosity quiet`: PASS, zero warnings/errors. `dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --verbosity quiet`: 19 passed, zero failed after recovery from a temporary CoreCLR memory error while Docker was active.

## Limits and next step

The executable SQL proof uses the real migrations and a limited runtime login. It does not yet run an HTTP request, seed synthetic tenants, or reproduce a P0 bug. The SQL harness currently generates and cleans its own run, while `QaApiFactory` generates a separate test run. The next Sol High slice should integrate their lifecycle so a single manifest/credential set supplies the provisioner and actual TestServer, then verify HTTP `/health`, real sign-in, anonymous denial and tenant separation. Keep each run serial and run-owned; stop on an unexpected server/port/marker mismatch. No Azure deployment or product repair was performed.
