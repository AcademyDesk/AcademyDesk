# Academy Desk handoff

## Approved shared GitHub checkpoint — 29 October 2026

Owner approved one feature-branch commit/push of the validated payroll batch and
shared handoffs to `codex/cloud-profile-sync`. Publication is being performed from
the verified local source; check remote HEAD/CI rather than assuming success.
Earlier no-push/unpublished entries remain chronological history. QA/EVIDENCE,
.build-check, model files and original PENTA work are excluded. No main/Azure/
settings changes. Next cloud profile starts with `CLOUD_PROFILE_START.md` and
`AI_CROSS_PROFILE_HANDOFF.md`. Cloud task selection/execution is still pending.

## Payroll reconciliation validated locally — 29 October 2026

- [x] Cloud positive-net payroll rule plus local exact-money/worker/normalized-
  period locks, identity provenance and teacher own-history safeguards integrated.
  Historical zero-net/null timestamps retained; no new zero-net payouts enabled.
- [x] Existing cloud snapshot migration unchanged; additive missing-provenance/
  nullable-history migration, no duplicate columns or invented verification/date.
- [x] Full cloud-based API1,234/zero skips; retained payroll module plus23new SQL/
  HTTP checks,5/5 exact-resource observed-waiter races, two audit rollbacks and
  blocked unsafe downgrade PASS.84app/7Identity migrations/owned teardown pass.
- [x] Window8ec5a8c6b3fb40c98113b1f93d93d927, SQL920440a23b5647099200f118d64b338e;
  exact services/images/health/API200/inventory restored. Unchanged API TRX/source
  reused from0d4bcd61e02e4951810289716ff85d19 after harness-only repairs. Failures
  and owned-only cleanup reconciliation preserved. No current-source CI claim.
- [ ] Next:payments/invoices/adjustments finance reconciliation, then frontend/
  PENTA/test/CI integration. Local3,183suite remains preserved separately, not
  replaced by this cloud-based count. Payroll UI/browser/device/issues remain OPEN.
- [ ] FiveP0s, qualified Mini, full conversational cohorts and wider release gates
  remain OPEN. No commit/push/merge/main/production DB/Azure/model experiment.
  Code remains local/unpublished; original PENTA working tree preserved.

Report: `QA/REPORTS/CLOUD_LOCAL_PAYROLL_RECONCILIATION_2026-10-29.md`.
Read `AI_CROSS_PROFILE_HANDOFF.md`; continue Sol High for finance/authority
integration, Sol Medium for settled UI. Owner stop before candidate loading stands.

## Shared cloud/local reconciliation — 28 October 2026

- [x] Found and fetched the other-profile cloud branch `codex/release-export-security`,
  exact HEAD `10a60eebc2e193b06de31622e897e755062dd72c` (14commits).
- [x] Independently observed latest GitHub CI40738226886 SUCCESS, exact job/step
  source and artifact metadata. Older failed cloud runs are superseded for this
  checkpoint. Detailed logs/artifact contents require authenticated access; no
  individual case counts are inferred.31fresh focused checks pass locally.
- [x] Owner explicitly chooses cloud work and positive-net payroll/validated
  Voided-payment restoration policies as common ground across three profiles.
- [x] New review worktree `D:\AcademyDesk-cloud-sync`, branch
  `codex/cloud-profile-sync`, starts at the verified cloud commit. Original dirty
  `D:\AcademyDesk-codex-p0` and its3,183API/PENTA/enterprise evidence stay intact.
- [ ] Actual source reconciliation remains:135cloud-changed paths include86
  differently changed local files. Cloud lacks the local PENTA backend/chat/routes.
  Preserve both, resolve transaction/migration/policy conflicts and retain all
  accepted tests and stronger source-bound CI before validating a combined build.
- [ ] FiveP0records, qualified Mini, full conversational cohorts, all-role/customer/
  device and exact security/database/restore/staging/release gates remain OPEN.
  No90%AI/production readiness claim. Owner stop before weights loading remains.

Read `AI_CROSS_PROFILE_HANDOFF.md` first; detailed report:
`QA/REPORTS/CROSS_PROFILE_RECONCILIATION_2026-10-28.md`.
This section supersedes older Current state/Active handoff metadata below for
continuation; earlier records remain historical. No application code changed,
commit/push/merge/main edit/model activation or Azure action in this reconciliation.
New coordination records are local/unpublished. Next route Sol High for the
consequential reconciliation; Sol Medium for settled bounded UI/docs.

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

## Latest checkpoint — student and subject-fee editor isolation, 2026-10-28

- Previous bf109723383c481b1003028516f6690f21363df1 checkpoint pushed and remote
  HEAD verified. Continue recurring user-authorized pushes to the same branch.
- BUG-DATA-0018 student/subject-fee slice: 18 failures/one pass before repair;
  all 34 expanded controlled actual-TSX cases now PASS. Student profile reads and
  child callbacks use URL/load generation; administrative component completions
  are scoped to academy/student/profile. Both student route aliases share the
  implementation. Subject-fee drafts/list/frequency/notices reset by selection;
  old loads/POSTs/post-save refreshes cannot overwrite a new student. Failed loads
  lock save/add and offer retry; pending duplicate guards and recovery preserve
  input/success. No financial arithmetic, API, RBAC or schema change.
- Full frontend: 46 files, 1,047 tests, 1,044 PASS/three FAIL, zero skipped or
  cancelled; exit1 retained for the absent historical communication SQL log.
  TypeScript/lint/production webpack build PASS; lint0 errors/63 warnings.
  Prior API/audits retained, not rerun. PentaMini unchanged.
- Report QA/REPORTS/CLOUD_STUDENT_EDITORS_2026-10-28.md; regressions in
  QA/tools/student-editor-isolation.test.cjs. BUG-DATA-0018 remains OPEN for
  tenant/assessment/attendance, browser/device/SQL/critical gates and server races.
- No environment configuration/publication, SQL fixture/migration/seeder,
  customer action, main merge or deployment. Saved CDN requirements still unapplied.
- Next route: Codex for existing data-preservation issues (numeric zero and
  unrepresented settings), remaining editor races and guarded HTTP/SQL validation
  when network access applies. Preserve all existing QA history and open gates.

## Latest checkpoint — zero values and hidden settings, 2026-10-28

- Previous fa0ded212c1289c65d57dccb35ac7ce651c9ca31 student/subject-fee checkpoint
  pushed and remote HEAD verified. Continue authorized pushes to the same branch.
- BUG-DATA-0050/0053/0054: initial41 controlled actual-TSX cases had20 failures;
  all51 expanded cases PASS. Teacher compensation distinguishes blank/null from
  numeric zero across hydration/repeated resaves and retains Monthly/Hourly
  applicability. TeacherPayments also reuses loaded-record guards and recovery.
  Music status PATCH preserves zero scores with nullish fallback. Finance Policy
  preserves its loaded payslip theme because its form has no theme field.
- No backend/RBAC/schema/financial arithmetic change or actual finance/customer
  action. Finance settings still use a loaded replacement snapshot; concurrent
  second-editor preservation is NOT established. SQL/browser/denied persistence,
  music stale responses, server races and critical financial acceptance stay OPEN.
- Final frontend: 47 files, 1,098 tests, 1,095 PASS/three FAIL, zero skipped or
  cancelled. Three original communication-drafts historical SQL-evidence failures
  retained; no evidence fabricated. TypeScript and webpack build PASS; full lint
  PASS with zero errors/62 warnings. Prior API1,073 PASS/audits retained, not rerun.
- This turn added132 focused checks across three slices (47+34+51). Reports:
  CLOUD_RECORD_EDITORS_2026-10-28.md, CLOUD_STUDENT_EDITORS_2026-10-28.md,
  CLOUD_VALUE_PRESERVATION_2026-10-28.md under QA/REPORTS/. Original issue histories
  retained; all affected issues remain OPEN for the broader acceptance chain.
- PentaMini unchanged. No environment configuration/publication, configured app
  DB/migration/seeder, main merge or Azure deployment. Runtime CDN requirements
  are still saved but unapplied. Local Windows access is still not attached and
  its checker has not been run here; never imply local files/Docker were inspected.
- Next route: Codex for remaining tenant/assessment/attendance and music/settings
  races and existing P0 security/finance/HTTP-SQL gates. Apply supported network
  settings through the user environment workflow before SQL/model retries; preserve
  browser/device/restore/release gates and do not claim production readiness.

## Latest checkpoint — approved critical finance policies, 2026-10-28

- User requested faster production progress and explicitly approved needed access.
  User chose **block zero-net payroll** and **allow validated voided-payment
  restoration with balance recalculation**. These supersede earlier policy-pending
  notes; do not ask for the same decisions again.
- Payroll rejects deductions >= effective gross. Payment restoration on both
  Completed and dedicated Reconciled paths validates tenant/invoice lifecycle,
  positive stored amount/currency and remaining adjusted collectible balance,
  recalculating Paid/PartiallyPaid only on accepted writes. Reconciliation retains
  its required reference/time; generic Reconciled remains denied. No real finance
  action, schema/RBAC change or concurrency serialization claim.
- Reproduction51 cases:33 FAIL/18 PASS; after repair51 PASS. New35 tests total
  (32 restoration/three payroll boundaries); full API1,108 PASS, zero failed/skipped.
  Guarded SQL harness build PASS/zero warnings/errors, with updated payroll policy
  and12 added restoration cases. Actual SQL not run; both P0 issues remain OPEN.
- Report QA/REPORTS/CLOUD_FINANCE_POLICY_2026-10-28.md. Prior frontend1,095 PASS/
  three missing-evidence FAIL retained; no web changes in this policy checkpoint.
- Saved additive api.github.com rule in the existing environment draft; save
  confirmed requires_publish=true. Prior SQL/model CDN rules preserved. User must
  review/save and publish in environment settings to apply them. Agent did not
  publish. Runtime HTTP policy unchanged; gh API status inspection was Forbidden.
- Next route: finish verification-before-deploy and critical-P0 status guards;
  run the existing real SQL/private media harness on a disposable CI runner.
  Do not claim a remote run passed unless its actual result can be read. All
  production/browser/concurrency/restore gates continue. Main/Azure untouched.

## Latest checkpoint — production verification wiring, 2026-10-28

- Finance-policy checkpoint0bceb439b95485297e2c05e8bc8cf9a4a19f572e pushed and
  remote HEAD verified. User-authorized recurring development-branch pushes continue.
- New .github/workflows/verify.yml: .NET/Node audits, API tests, existing owned
  SQL finance/tenant/class-file/communication modules, private Blob checks, full
  frontend tests with actual regenerated communication SQL evidence, type/lint/
  webpack and API image/startup-failure checks. No Azure secrets in verification.
  Explicit compiled-harness runner option and Linux Blob disk check added.
- Deployment reuses verification, restricts deployment to main and runs the new
  critical P0 record guard before Azure login/mutation. Known/new/missing/malformed
  P0s fail closed; CLOSED/PASS exact table values required only after real closure
  evidence. Current five open P0s correctly BLOCK. Critical P1/staging/restore/
  browser/device requirements remain separate and mandatory. No issue auto-closed.
- Local actionlint/PowerShell parse/preflight/shell checks PASS. Fourteen fixture
  production-gate tests PASS; five command-stub container simulations PASS (local
  socket, no image pull, health/auth/loopback/ownership cleanup); no real container
  startup claimed. API1,108 PASS retained; SQL harness compiles0 warnings/errors.
  CI restore/audit command, route typegen and TypeScript PASS. Fresh actual-TSX
  payload prerequisite generated24 distinct cases; no SQL evidence fabricated.
- Full frontend48 files,1,112 tests:1,109 PASS/three historical missing-SQL-log FAIL,
  zero skipped/cancelled. Lint/build retained from prior web source; unchanged.
- Report QA/REPORTS/CLOUD_PRODUCTION_VERIFICATION_2026-10-28.md and updated
  QA/09_RELEASE_GATES.md. Verification workflow runs on branch pushes; its remote
  outcome is NOT VERIFIED here because api.github.com is still blocked. Do not
  claim CI/real SQL/private Blob or Docker runtime passed without reading results.
- Saved additive GitHub API domain still requires user review/save/publish via
  environment settings, alongside earlier SQL/model CDN domains. Agent has not
  published, dispatched deployment, logged into Azure, merged main or touched
  customer/configured application DBs. PentaMini remains unchanged.
- Next route: inspect actual GitHub verification run after policy applies; repair
  any real failure, then continue financial concurrency/audit/RBAC and private
  media/browser/restore acceptance on the exact release source. Critical gates
  remain OPEN; publishing source is not production readiness.

## Latest checkpoint — invoice ledger/write integrity, 2026-10-28

- User authorized sustained larger batches without repeated continue prompts.
  Branch started clean at pushed52495e2; recurring pushes remain authorized.
- Ordinary GitHub Actions overview works despite blocked API. It reports run
  40728363572 COMPLETED/SUCCESS for exact52495e2; source workflow includes required
  SQL/private media/frontend/build/container checks. Detailed logs request sign-in.
  Do not describe the API block as complete inability to read public run metadata.
  This older green run does not validate subsequent source changes.
- Added InvoiceWriteScope: SQL tenant/invoice UPDLOCK/HOLDLOCK before mutable rows,
  reuse existing finance/audit transaction or own transaction for bypass/direct
  calls. Refresh unchanged cached SQL invoices under lock; do not discard unsaved
  changes. All payment writers, adjustment decisions and invoice status use it.
- BUG-DATA-0055 labels now follow same-tenant Completed/Reconciled collections,
  adjustments and existing due-date rules; explicit cancellation canonicalized.
  Cancelled collections rejected; approval does not uncancel invoices. No new
  adjustment/refund/payroll rules or schema/RBAC change.
- Initial31 tests had20 product failures plus two QA body-type expectation errors;
  corrected empty400 assertions. Final33 focused cases/full API1,141 PASS with no
  failures/skips. Twelve deterministic SQL race/audit/ledger cases prepared in
  FinanceIntegrityRegression.cs; compile0 warnings/errors, NOT RUN here. Includes
  two observed SQL waiters before release and audit rollback/stale tracked state.
- CI/runner wires FinanceIntegrity and cancels outdated push/PR runs; manual
  deployment verification protected. Existing cancelled fixture now expects its
  canonical stored value. Actionlint/PowerShell parse/diff checks PASS.
- Report QA/REPORTS/CLOUD_INVOICE_INTEGRITY_2026-10-28.md. All affected issues and
  release gates remain OPEN for new SQL/HTTP/concurrency/browser/restore acceptance.
  Prior local frontend1,109 PASS/three missing-evidence FAIL retained. No evidence
  fabricated, configured/customer DB used, environment published, main merged or
  Azure deployed. PentaMini unchanged.
- Next route: inspect this checkpoint's actual CI; while it runs, continue tracked
  holiday-route ambiguity and operational-dashboard integrity, then remaining
  release/security/browser gaps. Continue useful independent work without asking
  the user to repeat authorization.

## Latest checkpoint — operational/record integrity batch, 2026-10-28

- Invoice batchc25b6b83381b28bf759a47406cefc707baa7ff1c pushed/remote HEAD verified.
  Continuing larger authorized batches without repeated continue prompts.
- Removed duplicate holiday DELETE controller, retained canonical Holidays route
  and original module/role rules. Actual Program endpoint uniqueness and anonymous
  DELETE401 tests now pass; prior ambiguous routing failure reproduced.
- Admin intelligence safely serializes missing/foreign teacher references without
  exposing their name; own/inactive teacher totals unchanged. Student import
  validates trimmed/case-insensitive same-file duplicate emails and null rows,
  trims existing email lookup and preserves optional/cross-tenant controls.
  Expense update checks branch ownership and only assigns implemented Recorded
  status; omitted status/currency remain intact. No new financial approval flow.
- Imports and holiday DELETE opt into existing normal-actor domain/audit boundary.
  Initial routing/dashboard10 cases had4 failures; import/expense18 had11 failures.
  Final30 focused/atomic cases and full API1,171 PASS, zero failures/skips.
- New RecordIntegrity SQL module22 cases compiles0 warnings/errors and is wired
  into CI/owned runner. Includes scoped dashboard/holiday/import/expense calls and
  three audit rollback cases. Actual SQL execution NOT RUN here; concurrent
  imports/manual identity creation and all broader closure gates remain OPEN.
- Workflow writes source SHA/real step outcomes to its summary. Actionlint,
  PowerShell parsing/diff checks PASS. Report
  QA/REPORTS/CLOUD_OPERATIONAL_RECORDS_2026-10-28.md. All four issues remain OPEN.
- Older GitHub run40728363572 SUCCESS is observed for52495e2. Read run metadata via
  ordinary github.com Actions overview; detailed logs/artifacts need sign-in and
  api.github.com is still blocked. Newer source requires its own run. Local missing
  communication SQL log was not fabricated; prior web results retained.
- Workflow guards are development-branch changes, not activated default-main
  policy until reviewed merge. No main merge, environment publication, configured
  customer DB access, Azure deployment or PentaMini change. Critical and staging/
  browser/device/concurrency/restore gates remain OPEN.
- Next route: inspect candidate CI and repair real failures; continue remaining
  API/form integrity and production acceptance work without waiting for another
  continue request. Do not close issues or certify production from unit counts.

## 2026-10-28 — Candidate CI failure triage

- Remote branch verified at aaacb95fdde921c429c8bd76152cb9130d35946b.
- GitHub run40731742967 for c25b6b8 completed FAILURE in the guarded SQL
  step. Exact failed module/assertion is not visible anonymously; no acceptance
  claim applies to this candidate. Older52495e2 success remains historical only.
- Added fail-closed module outcome summaries and source-file/line-only harness
  stack diagnostics. Raw stack arguments/SQL connection details are excluded
  from those summaries. Detailed evidence remains in the existing artifact.
- Prior1,171 API pass retained; diagnostic change is workflow/runner only.
  No customer database access or deployment. Next route: inspect the diagnostic
  candidate run and repair its actual SQL failure before financial acceptance.

## 2026-10-28 — Payroll history and supplied teacher links

- BUG-DATA-0056:6 failures reproduced; new payouts freeze name/type, register
  reads snapshots and retains orphan visibility without joining foreign profiles.
- Additive migration20261028173216 backfills only same-academy profiles. Legacy
  identity before prior edits is unrecoverable; unknown stays explicit. Quiesce
  old writers during cutover; downgrade drops snapshots and requires preservation.
- BUG-DATA-0009:2 failures reproduced; update now rejects supplied foreign/missing
  teachers before mutation, matching create. Staff identity/required-link/type
  policy remains open. All issues remain OPEN.
- Final full API1,179 PASS/0fail/skip. Offline scaffold/upgrade SQL PASS. Prepared
  owned SQL migration check(3legacy rows/profile),6HTTP history/link checks awaits
  runtime. Receipt migration test remains pinned and upgrades to latest afterward.
- Report:QA/REPORTS/CLOUD_PAYROLL_HISTORY_2026-10-28.md. Local SQL image access and
  missing communication SQL log remain unresolved; no fabricated evidence.
- c25b6b8 run40731742967 FAILURE; c4a6ffd diagnostic run pending at checkpoint.
  Do not treat52495e2 historical success as validation of latest source.
- No main merge, configured DB migration, deployment, publication or PentaMini
  change. Next route:Codex/Sol High actual SQL triage and release acceptance.

## 2026-10-28 — Real SQL fixture failure located

- c4a6ffd run40736314010 completed FAILURE. Public diagnostic annotations identify
  FinanceIntegrityRegression.cs:43/56, fixture SaveChanges before the first race.
- Fixture invoice identifier was45characters against model/SQL max40. Shortened
  to35 without weakening the constraint or any race/assertion. Historical failed
  runs remain failed; new source needs its own actual SQL acceptance.
- Payroll local commit b5e4814 API1,179 PASS; model/upgrade SQL and final harness
  compilation PASS. Push this plus fixture repair for source-bound CI.
- No issue closure/deployment. Next route:Codex/Sol High inspect latest run and
  repair actual failures before production gates can pass.
