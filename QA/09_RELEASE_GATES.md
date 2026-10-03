# Release gates

These gates are proposed policy for Phase 2 onward. Current production readiness is **NOT ESTABLISHED**. Eight InMemory tests and a successful compiler check cannot satisfy the gates.

| Gate | Mandatory evidence |
| --- | --- |
| Developer | Type check, lint, existing tests; changed business-rule and form regressions; SQL integration for changed persistence; no secret/test-data leakage; pinned source and fixture manifest |
| Staging | Full HTTP authentication/authorization and two-tenant isolation suite; all P0 tests and critical P1 workflows; real SQL migrations and transaction/concurrency cases; Admin create teacher/student → refresh, scheduling/attendance, invoice/payment/reconciliation, payroll, Teacher and Student/Guardian critical E2E; overlay/scroll/device matrix for changed components |
| Production | Staging gate complete on the exact release commit; zero open P0 defects or unexecuted P0 cases; zero unaccepted critical P1 defects; rollback artifact and isolated restore evidence; approved changed visual references; health plus authenticated read-only smoke and deployed revision verification |

A BLOCKED or NOT RUN P0 case fails the gate. Failed P1 security, data integrity, login, onboarding, payment/payroll or core navigation tests block release. Lower-priority exceptions require an explicit owner, rationale, expiry and user impact; do not silently turn skips into PASS. P2/P3 failures are tracked separately from test priority and never ignored without triage.

Current blockers include `BUG-DATA-0001/0002/0003`, `BUG-SEC-0001`, missing SQL/HTTP/browser test infrastructure and failing lint. Static critical findings require reproduction and resolution or evidence-backed reclassification before release.

The current `.github/workflows/deploy-azure.yml` builds/deploys but does not run xUnit or lint, does not validate the complete UI/database chain, and enables migrations on API startup. Future CI should run verification before deploying, preserve immutable artifacts, stage schema changes, record both context migration histories, then perform smoke checks. Do not apply this workflow redesign in Phase 1.

Production smoke must be non-destructive: health, correct deployed commit/artifact, authenticated scope-limited reads, route loading, CORS and static assets. Never create payments, teachers or students in a customer tenant as deployment probes. Rollback of application code does not automatically roll back SQL schema; require a compatible migration plan and a tested restore path.
