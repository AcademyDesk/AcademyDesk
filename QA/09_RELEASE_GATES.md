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

## PENTA government-compliance and security extension — 2026-10-10

The preceding records are historical gate definitions, not current proof that every
listed infrastructure gap remains unchanged. Continue existing issues and evidence;
do not restart the program or close an issue from a narrow PENTA pass. Canonical legal
requirements live in Mini/Core; Academy maps them in
[COMPLIANCE/ACADEMY_IMPLEMENTATION_MAP.md](../COMPLIANCE/ACADEMY_IMPLEMENTATION_MAP.md).

| Activation boundary | Required evidence | Current outcome / owner |
| --- | --- | --- |
| Local synthetic/host security tests | Disposable owned data; no production writes/credentials or new model tools; precise source/test manifest | Permitted engineering work, not customer production acceptance / Academy |
| First real PENTA read | Qualified pinned Mini language/security contract; real current host authority, tenant/resource scope, exact facts, malformed/context/replay/downtime/audit-fault tests; linked browser/manual fallback | NOT ACCEPTED; saved Mini86/94 remains blocking / Academy + Mini |
| Customer personal/minor data pilot | Human-reviewed roles/purpose/basis/notices and child safeguards; minimal model context; processor/location/no-training/retention/egress evidence; actual guardian scope where relevant | OPEN; reference strings and generic consent rows are insufficient / legal/privacy + Academy + Mini |
| Commercial India operation | Current-law applicability sign-off; CERT incident/PoC/clock/India ICT-log evidence and drill; applicable SPDI/contract/content/telecom/payment controls | OPEN; DPDP future duties distinct from current duties / legal/security/operations |
| Privacy future dates | Corrected rule/provision mapping, accountable implementation owner and reviewed due date; tested rights/withdrawal/retention/deletion/holds before required commencement | OPEN; canonical one-year/eighteen-month phases retained, no blanket compliance claim / legal/privacy + engineering |
| New writes/sends/deletes/autonomy | Independently authorized domain service, exact actor/tenant/payload approval, expiry/replay/revocation protection, transaction/idempotency/audit and financial/tenant tests | UNAVAILABLE; READ pilot's approval label is not execution authorization / Academy |
| Learning/Academy Brain | Quarantined tenant-scoped validated provenance/purpose/ACL, reviewed basis/retention/deletion and safe feedback contract; no automatic policy/weights changes | UNAVAILABLE; metadata-only records are not training acceptance / Academy + Mini |
| Azure | All prior developer/staging/production gates on exact release; durable key protection/rotation, privileged identity, effective trusted ingress/limits, least-privilege private media/SQL/inference, regions/logging/backups/restore, scans/SBOM/licenses, migration/rollback evidence | NOT APPROVED; source secret refs/managed identity are not deployed proof / engineering/security |
| New geography/standalone/high-impact use | Applicable legal profile and intended-purpose risk review; independently enforcing customer connector, DPA/privacy and human oversight/appeals; current legislation verified | NOT ACTIVATED / legal + Mini + customer host |

Every gate record must include source commit/artifact, canonical control IDs,
implementation/evidence paths, owner, verification date, legal-review status,
blocking severity and next review. Separate DESIGNED, IMPLEMENTED, TESTED,
SECURITY REVIEWED, LEGAL REVIEWED and PRODUCTION APPROVED. Drafts and voluntary
standards must not masquerade as enacted universal requirements or certifications.
No focused PASS overrides OPEN P0/critical P1, device, full-runtime or legal gates.
