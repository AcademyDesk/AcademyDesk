# Phase 1 audit report

The authoritative [Phase 1 acceptance decision](PHASE_1_ACCEPTANCE.md) records static-audit closure and its qualifications. This generated inventory does not determine acceptance or production readiness. Follow the current [Phase 2 starting plan](PHASE_2_START_PLAN.md).

Baseline: main / 20bb6047f9edf733ac8e2a226621cc582ec54b3c, dirty working tree with two pre-existing student UI edits. Runtime/tool versions and executed commands: [BASELINE.md](BASELINE.md). This phase creates the QA control centre, source inventories, permanent scenario registry, issue register and implementation plan. No application repair or deployment was performed.

## Results and count definitions

| Metric | Count / outcome |
| --- | --- |
| Functional module groups | 23 |
| Page route files | 79 |
| Native form declarations | 82 |
| JSX field/control source occurrences (includes hidden/shared implementations) | 649 |
| UI action/control/list/card declarations | 843 |
| Controller classes with HTTP actions | 70 |
| Controller HTTP actions | 282 |
| Source-declared endpoint total including health | 283 |
| Framework Identity / development OpenAPI endpoints | Additional; exact runtime enumeration pending |
| Application models / mapped tables | 57 |
| Identity mapped tables | 8 |
| Total mapped tables (excluding migration histories) | 65 |
| Named roles | 12 |
| Workflow families traced | 24 |
| Existing executable tests | 8 |
| Existing passing / failing / skipped | 8 / 0 / 0 |
| Proposed new scenario families | 595 |
| All registered scenarios including 8 existing + 2 quality checks | 605 |
| Proposed P0 / P1 / P2 / P3 | 41 / 461 / 92 / 1 |
| All registered P0 / P1 / P2 / P3 | 41 / 471 / 92 / 1 |
| Visual/responsive scenario families | 83 |
| E2E scenario families | 20 |
| Explicit security scenario families | 18 |
| Open issues | 115 |
| P0 / P1 / P2 / P3 issues | 5 / 97 / 13 / 0 |
| Frontend lint | FAIL: 46 errors, 81 warnings |
| TypeScript compile check | PASS: exit 0 |
| SQL / browser / real-device tests | NOT RUN |
| Authoritative approved visual baselines | 0 |


Proposed scenario counts are not executed tests and do not expand every parameter combination. API scenarios include authorization prerequisites in addition to the explicit security families. The census counts source declarations, not dynamic tab instances or runtime fields. Exact deployed schema and framework endpoint totals need an isolated host.

## Findings that need attention first

| Issue | Priority | Finding |
| --- | --- | --- |
| [BUG-DATA-0010](../ISSUES/BUG-DATA-0010.md) | P0 | Payment status transitions leave invoice state and reconciliation evidence inconsistent |
| [BUG-FUNC-0001](../ISSUES/BUG-FUNC-0001.md) | P1 | Async form reset throws after successful persistence |
| [BUG-API-0001](../ISSUES/BUG-API-0001.md) | P1 | Blank optional dates sent as empty JSON strings |
| [BUG-DATA-0001](../ISSUES/BUG-DATA-0001.md) | P0 | Reconciled payments disappear from some balance calculations |
| [BUG-DATA-0002](../ISSUES/BUG-DATA-0002.md) | P0 | Payment guard ignores approved invoice adjustments |
| [BUG-DATA-0003](../ISSUES/BUG-DATA-0003.md) | P0 | Payroll permits deductions greater than gross |
| [BUG-SEC-0001](../ISSUES/BUG-SEC-0001.md) | P0 | Class material URLs are served outside authorization |
| [BUG-DATA-0004](../ISSUES/BUG-DATA-0004.md) | P1 | Student transaction does not encompass Identity account writes |
| [BUG-API-0003](../ISSUES/BUG-API-0003.md) | P1 | Token refresh retry can reuse the expired Authorization header |


The teacher symptom has a strong source explanation: a saved POST can be followed by event.currentTarget.reset() after await, which prevents success feedback. The same pattern appears at 21 sites in 13 files. Separately, optional hidden date fields serialize as empty strings rather than nullable date values. These findings must be reproduced with browser/HTTP evidence; no customer record was created to prove them.

## Execution and evidence

Eight existing xUnit tests passed against EF InMemory. Three only inspect rejected result types; five inspect stored values in the same context. None verifies SQL constraints, HTTP model binding, the global access filter, actual JSON serialization or React success behavior. See [existing test audit](EXISTING_TEST_AUDIT.md) and [TRX](../EVIDENCE/logs/phase1-existing.trx).

Lint failed twice with the same aggregate count; machine-readable diagnostics are [eslint.json](../EVIDENCE/logs/eslint.json). Type checking exited 0. The local API schema probe failed to connect, so no read-only API result is counted as PASS. No Next production build was rerun for this phase. [Run manifest](PHASE1_RUN.json) distinguishes tests from quality checks, includes TRX timing and links the executed failure to its issue. Full audit wall-clock duration was not instrumented; it is not fabricated.

## Infrastructure gaps and untested areas

No full HTTP test host, disposable SQL test fixtures, browser/component runner, approved visual baselines, role-isolation suite, contract validation suite, concurrency suite or load baseline exists. CI deploys without running the current xUnit/lint gates. All portal runtime states, production schema/storage/key configuration, actual backups and physical iOS/Android behavior remain untested here. Static extraction inventories every route/controller/field declaration but does not prove every dynamic branch. Critical code paths were semantically traced; exhaustive runtime enumeration is a Phase 2 prerequisite.

## Data-integrity, UI and security risks

Data: inconsistent payment/adjustment totals, negative payroll net, multi-context onboarding, optimistic response flags, audit-after-save ambiguity, inconsistent enrollment/scheduling transition guards and non-object JSON readers. UI: post-await resets, notices cleared by refresh, optional date serialization, select geometry, keyboard/focus gaps, native/custom time differences and route-specific CSS overflow. Security: publicly hosted private uploads, unverified role/tenant matrices, CSV formula export, refresh retry headers, development/default framework routes and deployment proxy/storage configuration. Scale thresholds are BASELINE REQUIRED; no invented response-time SLA is reported.

## Coverage confidence

Source census: high for explicit route/form/field/controller declarations, with parser limitations documented. Root-cause confidence is recorded per issue. End-to-end confirmation: absent in this phase. No static finding has been labeled a successful runtime test. Current production readiness is **NOT ESTABLISHED**. All issues are OPEN; no visual candidate is approved.

## NEXT SAFE ACTION

Use the current decision in [Phase 1 acceptance](PHASE_1_ACCEPTANCE.md) and the bounded task in [Phase 2 starting plan](PHASE_2_START_PLAN.md). The sequence below is the long-term roadmap; the starting plan controls the immediate scope.

1. Review this report and desired role/financial policies; retain the pinned baseline and issue IDs. Start Phase 2 only after review.
2. Implement INFRA-ISOLATION-001: a Testing host, fail-closed DB/storage allowlist and disposable SQL Server with both context migrations; prevent bootstrap/outbound side effects.
3. Implement P0 data-integrity reproductions first: reconciliation, adjusted balances, payroll deductions, onboarding transaction boundaries and duplicate/concurrent writes. Capture exact before/after SQL and serialized responses.
4. Implement AUTH-001/AUTH-002 and SECURITY-ROLE-001 across the full pipeline with two synthetic tenants and all roles. Include guardian flags, private uploads, revocation and module gating.
5. Add critical API/database/serialization tests for onboarding, teacher records, accounts, enrollment, scheduling, attendance, invoices/payroll and metadata shape. For each issue reproduce before changing code.
6. Introduce a small React/browser harness for teacher/student submit success, optional dates, duplicate taps, session expiry and unknown save outcome. Then fix one reproduced defect at a time with regression evidence.
7. Add the critical Admin → Teacher → Student/Guardian E2E journeys and overlay/scroll protocol at mobile/desktop breakpoints. Expand source-linked form/state cases by risk.
8. Human-review corrected visuals before approving golden images; validate real devices, capture performance baselines and test isolated restore.
9. Enforce Developer/Staging/Production gates in CI; deploy only a reviewed release with no outstanding mandatory failures.

Phase 1 stops here. Do not bulk-fix application code, install hundreds of tests, approve broken screenshots or deploy during review.
