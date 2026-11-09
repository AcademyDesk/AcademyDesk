# Release gates

These gates govern release acceptance. Current production readiness is **NOT ESTABLISHED**. Passing builds and unit tests do not satisfy the SQL, security, browser and operational gates below.

The production guard now also enforces the documented `SECURITY-SESSION-001`
acceptance gate through `BUG-AUTH-REVOCATION-001`. This adds the broader P0
session scenario without reclassifying the targeted P1 defect. All 15 guard
tests passed. The live guard blocks on the five open P0 records and this
separate session gate.

The local offline API image also passed 24 Production-mode runtime checks
with disposable SQL, including migrations, two-tenant access, encrypted key
ring reuse and SQL outage recovery. Exact hosted CI, fresh audits and staging
restore remain open. [Container and guard evidence](REPORTS/LOCAL_RELEASE_CONTAINER_ACCEPTANCE_2026-11-09.md)

The 8 November local browser runs cover standard and adjusted collection,
payment transition readback, the payroll one-cent and duplicate boundary,
and private audio recording and download. Physical devices and the linked
critical suite are still required for the five open P0 records. The latest
combined API run passed 2,196 tests.
[Latest shared acceptance](../CLOUD_INTEGRATION_TRACKER.md)

The current P0 finance window passed 12 integrity checks, 23 payroll
reconciliation checks and all 14 guard tests. The guard still requires
completion of the five open P0 records. The guarded deployment workflow is
present on the candidate branch. `main` at `f7af512` still carries the older
workflow without the guard.
[Current finance and release evidence](REPORTS/LOCAL_P0_FINANCE_CONCURRENCY_2026-11-07.md)

The local P0 retest on 7 November passed the original private-file case,
29 legacy media cases, 75 private Blob assertions and the payment status
module with 12 restoration cases. Six local SQL backup/restore checks passed
on 82 tables. The five original P0 records remain open, and staging
recovery and exact-release CI, image, browser and device requirements still apply.
[Current native P0 evidence](REPORTS/LOCAL_P0_RELEASE_ACCEPTANCE_2026-11-07.md)

Local C10 retest on 7 November 2026 passed 19 academic HTTP/SQL checks and
1,991 API tests. The Windows frontend run passed 1,982 of 1,983, including the
three communication checks using the unchanged local SQL receipt. The sole
Windows symlink capability failure has a separate Cloud Linux pass on matching
test/export source. The production guard still reports five open P0 records.
Current release-commit CI and images, critical/device coverage, and staging
restore/rollback remain required.
[C10 candidate evidence](REPORTS/LOCAL_C10_ACADEMIC_ACCEPTANCE_2026-11-07.md)

| Gate | Mandatory evidence |
| --- | --- |
| Developer | Type check, lint, existing tests; changed business-rule and form regressions; SQL integration for changed persistence; no secret/test-data leakage; pinned source and fixture manifest |
| Staging | Full HTTP authentication/authorization and two-tenant isolation suite; all P0 tests and critical P1 workflows; real SQL migrations and transaction/concurrency cases; Admin create teacher/student → refresh, scheduling/attendance, invoice/payment/reconciliation, payroll, Teacher and Student/Guardian critical E2E; overlay/scroll/device matrix for changed components |
| Production | Staging gate complete on the exact release commit; zero open P0 defects or unexecuted P0 cases; zero unaccepted critical P1 defects; rollback artifact and isolated restore evidence; approved changed visual references; health plus authenticated read-only smoke and deployed revision verification |

A BLOCKED or NOT RUN P0 case fails the gate. Failed P1 security, data integrity, login, onboarding, payment/payroll or core navigation tests block release. Lower-priority exceptions require an explicit owner, rationale, expiry and user impact; do not silently turn skips into PASS. P2/P3 failures are tracked separately from test priority and never ignored without triage.

Current blockers include the five open P0 issues `BUG-DATA-0001/0002/0003/0010` and `BUG-SEC-0001`, pending critical SQL/HTTP/browser/concurrency acceptance, and three evidence-dependent frontend failures in this cloud. Lint now passes with zero errors/62 warnings. Existing guarded SQL/HTTP infrastructure is present; current execution is blocked by unapplied network settings. Historical real-SQL/browser reports remain scoped evidence rather than acceptance of the latest release commit.

Cloud follow-up (2026-10-28): `.github/workflows/verify.yml` now prepares API regressions/audits, guarded finance/tenant/private-file/Blob SQL checks, genuine communication SQL evidence, full frontend tests, TypeScript/lint, webpack build and API image/startup-failure checks. It runs on the development branch/main pushes and main pull requests and is reusable by deployment. Its remote result is not yet observable from the current blocked GitHub API; wiring a workflow is not an executed pass.

Deployment requires verification success and `refs/heads/main`. Before Azure login or mutation, `QA/tools/check-production-gates.cjs` requires each known or new P0 record and the separate session acceptance record to have exact `Status: CLOSED` and `Final verification: PASS` table values. Set those only after the existing closure requirements pass and link the scoped release evidence in the issue. Missing or malformed records fail closed. The remaining staging, critical P1, browser and operational requirements are also mandatory.

These workflow changes are on `codex/release-export-security`; the default-main workflow is unchanged until a reviewed merge. Ordinary GitHub metadata now confirms verification40728363572 SUCCESS for exact52495e2. Newer changes require their own successful result; a prior green run is not approval of a later release.

Startup migration behavior in the existing deployment remains unchanged. Separate staged schema/backup/restore/rollback acceptance and deployed read-only smoke are still needed. No Azure workflow was dispatched or environment published during this follow-up. See [production verification report](REPORTS/CLOUD_PRODUCTION_VERIFICATION_2026-10-28.md).

Production smoke must be non-destructive: health, correct deployed commit/artifact, authenticated scope-limited reads, route loading, CORS and static assets. Never create payments, teachers or students in a customer tenant as deployment probes. Rollback of application code does not automatically roll back SQL schema; require a compatible migration plan and a tested restore path.

## Observed development verification — 2026-10-29 IST

Run [40738226886](https://github.com/AcademyDesk/AcademyDesk/actions/runs/40738226886) completed SUCCESS for exact commit `10a60eebc2e193b06de31622e897e755062dd72c`. The workflow enforces finance/record SQL and audit cases, payroll upgrade/history, private media/Blob, fresh communication evidence, full frontend, API regressions, builds and container smoke. Earlier API-observability limitations and failed candidates above remain historical. Detailed logs/artifacts have not been imported here. This does not close the five P0 issues or prove the full staging/production gate; exact final-source browser/security/critical workflow and isolated restore/rollback/deployed configuration acceptance remain necessary. The new payroll-print batch requires its own candidate run. Workflow/deploy guards remain development-branch changes until reviewed integration into `main`; no new deployment occurred.

Security/recovery follow-up (29 October IST):candidate verification adds existing role/session/refresh SQL modules and a new ownership-guarded synthetic backup/restore drill. Compilation is observed; actual execution is pending. See [current acceptance report](REPORTS/CLOUD_RELEASE_ACCEPTANCE_2026-10-29.md). This does not establish Azure PITR/retention/private-media restoration or production rollback. The five P0 records and all existing release requirements remain unchanged.

Observed continuation, 29 October IST: exact c7fca5f run40840527086 SUCCESS is
verified through actual job steps/logs. Strict role/session/refresh/concurrency
and all6 owned Recovery stages passed, including actual restore and exact70-table
fingerprints/Identity/HTTP. Full frontend1,152/1,152 PASS with generated SQL logs;
API1,179 PASS. This supersedes pending execution/observability statements above
for that source only. See [current scoped evidence](REPORTS/CLOUD_WORK_ITEM_VALIDATION_2026-10-29.md).
The five P0 records still block release; production Azure PITR/retention/media
recovery/rollback, full browser/device and critical acceptance remain mandatory.
New work-item validation source requires its own successful candidate CI.

Exact97f301e continuation run40920810451 is SUCCESS, actual job/logs read:
work-item SQL module exit0 with audit rollback/owned cleanup, API1,217 and web1,152
PASS; role/session/concurrent refresh and full owned Recovery repeated PASS.
Application source is unchanged in the follow-up export-verification-only batch.
All5P0, broad critical/browser/device/staging and production recovery gates remain.
