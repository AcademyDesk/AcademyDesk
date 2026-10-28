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
| Latest work | Local frontend quality/form-safety/security dependency follow-up; see final dated section below. |
| Next engineering route | Verify isolated SQL/HTTP development setup and continue open security/release gates. |
| Commit state | Latest verified development is being checkpointed on codex/release-export-security. User authorized recurring pushes; verify remote HEAD. Main is unchanged. |

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

## Local follow-up — dependency and lint increment, 2026-10-28

- Base checkpoint b374b35482fa07c3dba41e27f4a746d9efc3a62e is already pushed.
  Subsequent changes are LOCAL and UNCOMMITTED per the user's latest instruction;
  defer further pushes until requested. The other profile cannot retrieve these
  later changes from GitHub yet.
- Pkcs 9.0.11 was pulled in by EFCore.SqlServer → SqlClient 6.1.6. Explicit API
  pin to 9.0.22 addresses the two observed high/moderate advisories without a major
  dependency upgrade. Both API and test transitive-inclusive NuGet audits report
  zero known vulnerable packages; all 1,073 API tests passed.
- Practice-log page now uses API/form types rather than five explicit-any values;
  save/review guard a missing academy. Its lint and frontend TypeScript passed.
  Production webpack build passed. Full frontend lint remains failing at
  34 errors / 77 warnings, improved from 39 / 77; no rule suppression.
- Docker daemon works, but SQL Server image pull is blocked on redirected blobs.
  Observed domains: centralus.data.mcr.microsoft.com and
  westcentralus.data.mcr.microsoft.com. No SQL container/database was created,
  and the configured application connection was not used.
- PentaMini model download remains blocked on us.aws.cdn.hf.co. No model or
  inference success is claimed; PentaMini source remains unchanged.
- All three exact CDN requirements were saved additively in the environment draft;
  the running policy was not changed and no environment was published. Required
  user action is to review/save settings and publish when appropriate. Retry
  affected downloads only after a meaningful policy change.
- Report: QA/REPORTS/CLOUD_DEPENDENCY_AND_LINT_2026-10-28.md.
- Next route: Codex for the existing guarded SQL/HTTP harness and deployment-blocking
  security cases once network access works; Cursor for remaining contextual lint
  fixes. All existing P0, browser, migration/restore and deployment gates remain.

## Latest local work — frontend quality and form safety, 2026-10-28

This section supersedes the earlier local lint/dependency counts, not their history.
Branch remains `codex/release-export-security`, HEAD b374b35. All changes since that
checkpoint are LOCAL/UNCOMMITTED; the user requested that further pushes wait.

- Configured frontend lint now PASSES: zero errors, 65 warnings, improved from
  39 errors/77 warnings. No lint rules were disabled. Async initial/selection
  reads reject late effect completions; controlled drafts reset before the new
  view is committed. Calendar/theme/menu/clock behavior has targeted regressions.
- Contact preferences: three controlled cross-contact/type/clear failures were
  reproduced and fixed; all 13 preference tests pass. Broad BUG-DATA-0018 remains
  open for other editors, pending/failed loads, save races and browser/SQL proof.
- Async form reset: six released-target failures reproduced; seven handlers now
  capture their form before awaiting. Fourteen accepted/rejected checks pass.
  Other reset sites and successful-save/readback notice issues remain open.
- New `npm test` from apps/web runs 43 current regression files. Final result:
  943 tests, 940 PASS, three FAIL, zero skipped/cancelled. Failures are the three
  communication-drafts historical HTTP cases requiring absent ignored evidence
  QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log. Do not invent that
  file or treat the suite as green. Restore actual synthetic evidence or run the
  guarded real-SQL fixture once the network blocker is removed.
- Historical certificate assertions are retained through explicit current
  adapters; original issuance/print/appearance inputs remain unchanged. One
  media transport assertion now reads Headers correctly, preserving bearer/body
  security checks. Logs/summary: .build-check/frontend-tests/ (ignored).
- Initial-load/state/form controlled suite: 37 PASS. Shared-control suite: eight
  PASS. These are controlled TSX/hook checks and some server rendering, not full
  browser, physical-device or SQL acceptance.
- Frontend audit found critical Next.js plus high sharp/source-map-js/zod issues.
  Next.js and eslint-config-next are now pinned together at 16.4.3. Lock refresh
  resolves sharp 0.35.5, source-map-js 1.2.2, zod 4.6.7. `npm audit` now reports
  ZERO known vulnerabilities including dev dependencies. Production webpack
  build passes with 16.4.3; package/lock declarations agree. Final post-upgrade
  `npx tsc --noEmit` also PASSED.
- Earlier API full regression (1,073 passed) and API/test NuGet audits (zero known
  vulnerable packages after Pkcs 9.0.22) are retained; not rerun for frontend-only
  work. No complete runtime/application security guarantee is implied.
- SQL Server and model CDN access still needs the saved environment network
  requirements. No customer database/production seeder was used, and no SQL
  fixture, inference readiness, Azure deployment or new environment publication
  is claimed. PentaMini source remains unchanged.
- Full report: QA/REPORTS/CLOUD_FRONTEND_QUALITY_2026-10-28.md.
- Before switching profiles, checkpoint/push these later changes when the user
  requests it; the existing remote b374b35 does not contain this local work.
- Next route: Codex for guarded SQL/HTTP fixtures and critical finance/file-access
  gates; continue existing browser/device, migration/restore and acceptance work.
  The full product and enterprise testing program remain unfinished.

## GitHub continuation checkpoint — 2026-10-28

The user has now explicitly authorized continued work and recurring GitHub pushes.
This supersedes the earlier instructions to defer pushes. This checkpoint includes
all dependency, frontend quality, state/form-safety repairs and test instructions
recorded above. Commit/push only to the development branch
`codex/release-export-security`; do not merge main, deploy Azure or publish an
environment without separate authorization. Verify remote HEAD after each push.

Resume by checking the current branch/working tree, reading the latest QA report,
and preserving the three evidence-dependent frontend failures as failures. The
runtime still lacks the three pending CDN domains; the saved draft alone does not
unblock SQL Server or model downloads. Do not use the configured application DB
for destructive tests. Next independent work should continue the known form/state
regressions or existing security gates, with focused tests and handoff updates.

## Latest checkpoint — remaining form recovery, 2026-10-28

- Previous full frontend/security checkpoint f9a0cd7d7569aaab2846c0afeacb308546dd2067
  was pushed and verified on GitHub. User explicitly authorizes continued work and
  recurring pushes on codex/release-export-security. No main merge or deployment.
- Captured forms at all nine remaining direct currentTarget.reset sites in teacher,
  portal, academic governance, platform and payroll frontend handlers. Rejected
  platform support/invoice requests retain input. Confirmed platform/payroll writes
  retain success plus a refresh warning if their separate readback fails.
- Reproduction: initial 21 cases had 14 failures/seven passes. Two added payroll
  readback cases independently failed before their repair. All 23 now pass.
  Exact payroll request payload/worker/null handling remains covered; no financial
  arithmetic, backend rules or real disbursement was changed.
- Extracted controlled runtime to QA/tools/frontend-page-runtime.cjs; all previous
  37 initial-load/form checks remain intact and pass. New file:
  QA/tools/remaining-form-targets.test.cjs.
- Full current npm test: 966 tests, 963 PASS/three FAIL, zero skipped/cancelled,
  across 44 current files. The same three failures require the absent historical
  communication-roundtrip SQL log. Do not fabricate evidence or call this green.
- TypeScript, zero-error lint (65 inherited warnings), production webpack build
  on Next.js 16.4.3 and git diff checks PASS. Prior audits and API test result
  retained; no dependency/backend changes in this increment.
- Report: QA/REPORTS/CLOUD_FORM_RECOVERY_2026-10-28.md. Root issues remain OPEN
  for actual browser/SQL acceptance, idempotency/races and other notice workflows.
- Runtime CDN restrictions remain unresolved; draft settings alone are not applied
  network policy. No SQL fixture/model readiness or production acceptance claimed.
- Next route: continue guarded SQL/HTTP fixture and critical finance/file-security
  gates after network settings apply; independent work may continue the existing
  record-selection/notice/security backlog with targeted regressions. Preserve
  the QA history and update this handoff after each checkpoint.

## Latest checkpoint — Windows inventory and cloud-first work, 2026-10-28

- User confirmed Windows with PowerShell and wants maximum cloud use. This chat
  still has only the managed Linux executor; no local Windows machine, mount or
  VPN is attached. Do not claim access to the PC or its Docker/files.
- Started clean on codex/release-export-security at pushed/verified
  161420bd7baca7dbdb1ac9041694d76f963d9030. Recurring development-branch pushes remain
  authorized. PentaMini remains clean and unchanged.
- Added QA/tools/check-machine.cjs and a Windows fetch/extraction workflow that
  preserves the PC's branch and local changes. Reports only tool/repository/
  local-Docker/artifact metadata; no raw errors, paths, secrets or artifact contents.
  Does not install, connect to databases, start/pull containers, write files or
  make network requests. Explicitly selects a local socket rather than following
  a saved remote Docker context. Script requires Node 18+; app uses Node 24.
- Syntax/help and cloud execution PASS: Linux x64, five logical CPUs, 33 GiB RAM,
  Node 24.19.0, Git 2.52.0, .NET 10.0.403, Python 3.12.14, Docker 28.4.0,
  Compose 2.40.3. SQL image, pinned models and communication SQL evidence absent.
  Invalid-argument and missing-tools/repositories/artifacts JSON smoke checks PASS.
  Node child-process output is suppressed by this cloud's default sandbox; the
  first negative smoke produced no JSON. Repeated outside that sandbox and passed.
  Windows/PowerShell/Docker Desktop execution is NOT VERIFIED until the user runs it.
- Read runtime policy and current draft only. The existing three CDN requirements
  remain saved but absent from enforced policy; VPN/TCP destinations unconfigured.
  No repeated denied downloads, DB/seeder access, new configuration, publication,
  SQL fixtures, model inference, application changes or deployment.
- Report: QA/REPORTS/CLOUD_LOCAL_CONTINUATION_2026-10-28.md. Prior application
  validation retained (frontend 963/966 PASS with three missing-evidence failures;
  API 1,073 PASS; TypeScript/lint/webpack build PASS); not rerun for inventory/docs.
- Next route: user runs the checker on Windows and shares its JSON only. Codex
  interprets useful original synthetic evidence/cache availability, while cloud
  code work continues. Real SQL/inference needs applied supported network settings
  or a separately attached appropriate executor. Do not expose local Docker/SQL
  publicly, transfer secrets or treat artifact presence as valid execution proof.

## Latest checkpoint — profile and admission editor isolation, 2026-10-28

- Started clean at pushed 0310dbf on codex/release-export-security. Continuing
  user-authorized feature-branch pushes; main/PentaMini unchanged.
- BUG-DATA-0018 guardian/teacher/admission-fee slice: 28 failures in 30 controlled
  actual-TSX cases before repair, all 47 expanded cases now PASS. New shared
  useRecordEditor binds drafts/reads/saves to exact URL and load generation,
  invalidates before selection commit, ignores old/unmounted responses, locks
  pending/failed loads and exposes retry. Same-generation pending writes are
  guarded; failures preserve input and release busy state. Confirmed writes with
  unreadable readback retain success with reload guidance. Date-null, zero-fee
  and teacher availability payloads retained; no backend rules changed.
- Full npm test: 45 current files, 1,013 tests, 1,010 PASS/three FAIL, zero
  skipped/cancelled. Only absent communication-roundtrip SQL evidence fails.
  TypeScript and production webpack build PASS; lint zero errors/63 warnings.
  API 1,073 PASS/audits retained, not rerun for frontend-only edits.
- Report QA/REPORTS/CLOUD_RECORD_EDITORS_2026-10-28.md; focused test file
  QA/tools/record-editor-isolation.test.cjs. Historical QA records preserved.
- Runtime policy still lacks saved CDN additions. No repeated denied downloads,
  app-DB/seeder access, actual SQL/browser validation, environment configuration,
  publication, main merge or deployment. Existing enterprise gates remain OPEN.
- Next route: Codex for remaining StudentProfile/StudentAdminProfile and subject
  fee save/selection isolation, then tenant/assessment/attendance and existing
  critical security/SQL/browser gates. No overall production-readiness claim.
