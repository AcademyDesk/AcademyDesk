# Release gates

These gates govern release acceptance. Current production readiness is **NOT ESTABLISHED**. Passing builds and unit tests do not satisfy the SQL, security, browser and operational gates below.

| Gate | Mandatory evidence |
| --- | --- |
| Developer | Type check, lint, existing tests; changed business-rule and form regressions; SQL integration for changed persistence; no secret/test-data leakage; pinned source and fixture manifest |
| Staging | Full HTTP authentication/authorization and two-tenant isolation suite; all P0 tests and critical P1 workflows; real SQL migrations and transaction/concurrency cases; Admin create teacher/student → refresh, scheduling/attendance, invoice/payment/reconciliation, payroll, Teacher and Student/Guardian critical E2E; overlay/scroll/device matrix for changed components |
| Production | Staging gate complete on the exact release commit; zero open P0 defects or unexecuted P0 cases; zero unaccepted critical P1 defects; rollback artifact and isolated restore evidence; approved changed visual references; health plus authenticated read-only smoke and deployed revision verification |

A BLOCKED or NOT RUN P0 case fails the gate. Failed P1 security, data integrity, login, onboarding, payment/payroll or core navigation tests block release. Lower-priority exceptions require an explicit owner, rationale, expiry and user impact; do not silently turn skips into PASS. P2/P3 failures are tracked separately from test priority and never ignored without triage.

Current blockers include the five open P0 issues `BUG-DATA-0001/0002/0003/0010` and `BUG-SEC-0001`, pending critical SQL/HTTP/browser/concurrency acceptance, and three evidence-dependent frontend failures in this cloud. Lint now passes with zero errors/62 warnings. Existing guarded SQL/HTTP infrastructure is present; current execution is blocked by unapplied network settings. Historical real-SQL/browser reports remain scoped evidence rather than acceptance of the latest release commit.

Cloud follow-up (2026-10-28): `.github/workflows/verify.yml` now prepares API regressions/audits, guarded finance/tenant/private-file/Blob SQL checks, genuine communication SQL evidence, full frontend tests, TypeScript/lint, webpack build and API image/startup-failure checks. It runs on the development branch/main pushes and main pull requests and is reusable by deployment. Its remote result is not yet observable from the current blocked GitHub API; wiring a workflow is not an executed pass.

Deployment now requires verification success and `refs/heads/main`. Before Azure login or mutation, `QA/tools/check-production-gates.cjs` requires each known/new P0 record to have exact `Status: CLOSED` and `Final verification: PASS` table values. Set those only after the actual existing closure requirements pass; link the scoped release evidence in the issue. Missing/malformed records fail closed. The current five open issues correctly block deployment. This automated guard covers P0 issue records, not every staging/P1/browser/operational requirement; all remain mandatory.

Startup migration behavior in the existing deployment remains unchanged. Separate staged schema/backup/restore/rollback acceptance and deployed read-only smoke are still needed. No Azure workflow was dispatched or environment published during this follow-up. See [production verification report](REPORTS/CLOUD_PRODUCTION_VERIFICATION_2026-10-28.md).

Production smoke must be non-destructive: health, correct deployed commit/artifact, authenticated scope-limited reads, route loading, CORS and static assets. Never create payments, teachers or students in a customer tenant as deployment probes. Rollback of application code does not automatically roll back SQL schema; require a compatible migration plan and a tested restore path.
