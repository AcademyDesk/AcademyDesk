# Phase 2A — real HTTP and SQL controls

Date: 2026-09-30. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c`; the two existing student UI edits remain untouched. This continues the isolated SQL setup in `PHASE_2A_SQL_PROVISIONING.md`.

## Implemented

`QA/tools/SqlHarness` now links the test-only `QaApiFactory` and runs the actual ASP.NET `Program` with the same generated manifest and database used for SQL provisioning. It keeps the production authentication, authorization, global academy filter and endpoint mappings in the TestServer. The HTTP sequence uses the limited database login: `GET /health`, unauthenticated `GET /api/academies`, a synthetic `.invalid` Identity user created via `UserManager`, `POST /api/auth/login`, then authorized `GET /api/academies` with the issued bearer token. The synthetic user has no tenant and the fresh academy list must be empty. No mock-auth handler is used.

## Executed evidence

Run `eabed3975dfc426da8504d6eed609725` used a new Docker SQL Server 2022 container bound to `127.0.0.1:52206`. A new database was created only after container and SQL identity and absence checks. The following controls passed:

- `/health`: HTTP 200 with a real SQL connectivity check.
- Anonymous protected academy request: HTTP 401.
- Real Identity login: HTTP 200 with an access token.
- Bearer academy request: HTTP 200 with an empty list from the fresh database.
- Both migration sets: 79 application and 7 Identity migrations; runtime login had no sysadmin, ALTER DATABASE or CREATE ANY DATABASE permission.
- Mismatched cleanup marker: refused before cleanup callback. Marker-checked cleanup removed the database and login. A final read-only query returned NULL for both IDs. Run-owned local host folders were marker-checked and removed. The exact container was then stopped and removed; only the pulled image remains cached.

The harness build passed with zero warnings/errors. No customer, development or Azure database was accessed. No product fix, commit, GitHub push or Azure deployment occurred.

## Remaining work

Tenant A/B authorization needs synthetic academy and user fixtures, followed by the five P0 reproductions. The current control proves a working login and SQL-backed authorized request, not cross-tenant isolation. This small slice is suitable for local review before the QA harness and tests are committed. Azure receives application changes only after the relevant product fixes pass local regression; the test executable itself is not an Azure workload.
