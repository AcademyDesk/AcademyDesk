# Academy Desk handoff

## Current state

| | |
| --- | --- |
| Project | Academy Desk |
| Product | AI-native SaaS operating platform for academies |
| Initial market | Music academies, with architecture capable of supporting other instructor-led academies such as dance and arts |
| Repository | `D:\AcademyDesk` |
| Accepted source of truth | GitHub |
| Branch | `main` |
| Current verified baseline | `535e6784e38b20fa1c9486b889544706f3c454a7` |
| GitHub `origin/main` | Same verified commit |

## Tech stack

- Next.js frontend
- ASP.NET Core API
- EF Core
- SQL Server
- Docker
- Azure target deployment
- GitHub source control

## Baseline validation completed

The following checks passed at commit `535e6784e38b20fa1c9486b889544706f3c454a7`:

- .NET build passed
- 1,042 API tests passed
- TypeScript validation passed
- Dockerfile static validation passed
- QA parsing validation passed
- `git diff` checks passed
- staged secret scan passed

## ENTERPRISE TESTING IS NOT COMPLETE

The baseline above does not complete enterprise testing. Continue the existing testing program. Do not restart it from zero, and do not treat a later unit or API pass as a substitute for the remaining gates in `ENTERPRISE_TESTING_ROADMAP.md`.

Dockerfile static validation is not equivalent to:

- complete image builds
- container startup
- runtime validation
- database integration
- end-to-end integration

`QA/EVIDENCE` remains primarily local and is intentionally not broadly committed. Missing committed evidence files do not mean the underlying QA work is absent. Do not delete useful QA work because GitHub governance has been introduced.

Development seeder credentials were removed from current source. Local development values were rotated using secure local secrets. Do not reintroduce hard-coded credentials.

## DO NOT RESTART THE EXISTING TESTING PROGRAM

Continue from the existing baseline, QA work, tests, and evidence.

Start from these living records rather than inventing a replacement plan:

- `QA/00_QA_README.md`
- `QA/02_TEST_MASTER_PLAN.md`
- `QA/03_TEST_MATRIX.md`
- `QA/08_SECURITY_TEST_MATRIX.md`
- `QA/09_RELEASE_GATES.md`
- `QA/ISSUES/INDEX.md`
- `QA/REPORTS/`

Earlier QA checkpoints record their own historical counts and commits. Those records stay as history. The verified GitHub baseline for new work is `535e6784e38b20fa1c9486b889544706f3c454a7`.

## Current known work categories

- Authentication and security
- Authorization
- Role isolation
- Tenant and academy isolation
- Responsive UI
- Dropdown layering
- Dropdown positioning on scroll
- Mobile and null failures
- Time and clock standardization
- Layout and alignment
- Integration testing
- Transaction and concurrency testing
- Enterprise remediation

Known shared UI cases already tracked in QA include `UI-DROPDOWN-001`, `UI-SCROLL-001`, and `UI-TIME-001`, with issue records such as `BUG-UI-0001`. Open issues remain in `QA/ISSUES/INDEX.md`. Phase 2B repair reports in `QA/REPORTS/` are continuation points, not a closed release.

## Product direction in force

Academy Desk is evolving into an AI-native academy operating system on top of the existing deterministic platform. The current ERP, APIs, permissions, and business rules stay. Read `AI_PRODUCT_VISION.md` before changing product behaviour, architecture, UX, or AI functionality.

Azure deployment waits for the pre-Azure quality gate in `ENTERPRISE_TESTING_ROADMAP.md`. Do not deploy unless explicitly instructed.

## Active handoff

| | |
| --- | --- |
| Latest checkpoint | Cloud CSV export security increment; see the dated handoff and report below. |
| Next engineering route | Verify isolated SQL/HTTP development setup and continue open security/release gates. |
| Commit state | Feature-branch checkpoint includes this handoff; verify the remote branch before resuming elsewhere. Main is unchanged. |

Update this file after substantial work. Record the branch, commit, what changed, what was validated, what remains open, and the next route.

## Cloud development increment — 2026-10-28

- Branch: `codex/release-export-security`; base HEAD
  `f7af512f884ca5fe8ac3ddb287c852492592e709`. This increment and handoff
  are being checkpointed on the feature branch; main remains unchanged.
- Started from a clean checkout. Reproduced BUG-SEC-0002 using 30 synthetic
  controller export cases (all failed before repair).
- Added formula-neutralizing CSV encoders for academy/audit exports and frontend
  reports, preserving stored values, export scope and typed API numeric values.
- Validation: full API suite 1,073 passed / zero failed / zero skipped;
  frontend encoder 17 passed; TypeScript and new helper lint passed;
  production frontend webpack build passed both before and after the repair.
- Existing full frontend lint failures and NuGet transitive Pkcs vulnerability
  warnings remain. Default Turbopack was not revalidated; webpack is the tested mode.
- BUG-SEC-0002 remains OPEN: real HTTP/SQL export verification, browser download,
  supported spreadsheet review and critical release regression are still required.
- SQL-backed startup, migration/restore, concurrency, physical-device acceptance,
  financial/file-access P0 gates and production readiness are not established.
  Configured database/seed variables report ready, but this increment did not
  connect to that database or run its seeder: it has not been established as an
  approved disposable test database.
- PentaMini was unchanged. Prior 49-test result retained, not rerun;
  no inference-readiness or AcademyDesk integration claim is made.
- Report: `QA/REPORTS/CLOUD_EXPORT_SECURITY_2026-10-28.md`.
- No environment configuration changes, publication or deployment. The feature
  branch is checkpointed separately from main for continuation across profiles.
- Next route: Codex for guarded SQL/HTTP and security gate validation; Cursor
  for routine lint remediation per AI_MODEL_ROUTING.md. Continue the existing
  QA program; do not start a replacement test plan or mark enterprise work complete.

## Continue from another profile

1. Connect the new profile to `AcademyDesk/AcademyDesk` and, if needed,
   `AcademyDesk/PentaMini`. Access is per account/workspace; do not assume the
   previous cloud machine, chat, ignored files, credentials or processes transfer.
2. Select/fetch `codex/release-export-security` and verify the latest remote
   commit and a clean working tree. Do not reset a dirty checkout. Do not merge
   into main or deploy merely to continue development.
3. Read `AGENTS.md`, this handoff, `AI_MODEL_ROUTING.md`,
   `ENTERPRISE_TESTING_ROADMAP.md`, `AI_PRODUCT_VISION.md`, and
   `QA/REPORTS/CLOUD_EXPORT_SECURITY_2026-10-28.md`.
4. Use the published environment if the new profile has access. Otherwise set
   up .NET 10 (tested SDK 10.0.403), Node 24 (tested 24.19.0), and Python 3.12+
   for PentaMini. In `apps/web`, use `npm ci`; in the AcademyDesk root, use
   `dotnet restore AcademyDesk.slnx`. Restore credentials only through secure
   environment settings; never copy secret files into GitHub or chat.
5. Recorded validation commands, from the AcademyDesk root unless stated:
   - `dotnet test AcademyDesk.slnx --nologo -m:4`
   - `node QA/tools/test-csv-export.cjs` (Node 24 executes the TypeScript helper)
   - From `apps/web`: `npx tsc --noEmit`, `npx eslint src/lib/csv.ts`,
     `npm run build -- --webpack`
   In the prepared cloud instance, .NET is in `/workspace/.tools/dotnet`;
   set DOTNET_ROOT and add that directory to PATH if required.
6. Next substantive task: establish an approved disposable SQL database and
   guarded HTTP/SQL harness, then continue existing P0/security cases.
   Never run development seeding/migrations against an unverified configured
   database. Keep spreadsheet/browser export acceptance and all remaining
   production gates OPEN until their actual checks pass.

PentaMini has no code changes from this increment. Its original repository main
branch and existing handoff remain the starting point; real model inference and
AcademyDesk integration are not established by the prior unit-test passes.
