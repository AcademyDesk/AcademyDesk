# Phase 2B — Payment void and reconciliation-evidence repair

2026-10-01. **Local void/evidence original-case/module retest PASS twice; restoration POLICY-PENDING; issue/phase NOT CLOSED.** Accepted Astra audit and twice-failing Phase 2A evidence reused, not re-audited. Continue agreed Sol High allocation. No commit/push, Azure access/deployment, normal development database change or additional listening development frontend/backend. TestServer is the existing isolated in-process QA harness.

## Repair and before-fix evidence

[BUG-DATA-0010](../ISSUES/BUG-DATA-0010.md): PaymentsController.UpdateStatus now updates the associated invoice when a payment is voided. Remaining Completed and Reconciled amounts exclude the changed row; balance subtracts approved AdjustedAmount. Result is Paid for no remaining balance, PartiallyPaid for remaining collected money, otherwise Overdue or Issued according to due date. Cancelled state is preserved case-insensitively. Payment and invoice changes use one SaveChanges; this is not concurrent financial-write serialization. Repeated Voided requests return success without another write. Existing reconciliation reference/timestamp remain historical evidence after voiding.

Generic Reconciled status changes now return 400 directing callers to the dedicated reference-taking action, including when a voided row retains historical evidence. Dedicated reconciliation reference trimming/timestamp behavior is unchanged. **Voided restoration is deliberately not repaired or accepted pending the human policy choice**; neither generic Voided→Completed nor dedicated Voided→Reconciled is newly approved. No schema, permissions/approval workflow, historical ledger corrections or zero-net payroll policy change.

Sixteen new before-fix unit cases: **6 PASS/10 expected FAIL**; complete [before-fix log](../EVIDENCE/logs/phase-2b-transition-before.log). Original twice-failing real HTTP/SQL evidence remains unchanged in [Phase 2A report](PHASE_2A_PAYMENT_TRANSITION_REPRO.md).

## Final-source verification

- **134/134 API tests PASS**, zero failures/skips: existing 118 plus 16 transition cases. [Test log](../EVIDENCE/logs/phase-2b-transition-tests.log). Cases cover Completed/Reconciled full/partial/adjusted void, mixed remaining collected rows, cancelled state, repeat void, generic reconciliation refusal, dedicated reference trim/timestamp, invalid status/blank reference and foreign payment refusal. InMemory tests are not full HTTP authorization certification.
- Harness build PASS, zero warnings/errors. [Build log](../EVIDENCE/logs/phase-2b-transition-build.log).
- Two fresh isolated SQL Server 2022 containers with real Program/Identity/TestServer, exact owned contexts/runtime login and fresh no-tracking financial snapshots. Both exit 0; migrations 80 application / 7 Identity. Runtime inventory 307 routes, 296 controller method/routes, 10 framework Identity method/routes; digest `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`.

| Run | UTC start | Loopback SQL port | Duration | Evidence |
| --- | --- | --- | --- | --- |
| `61b20af12c544cdab32aad158089233b` | 2026-10-01T06:00:53.9527482Z | 65143 | 41.4 s | [First run](../EVIDENCE/logs/phase-2b-transition-sql-run1.log) |
| `14748bdbe546472184489dbf5794529f` | 2026-10-01T06:02:54.4325432Z | 57043 | 31 s | [Second run](../EVIDENCE/logs/phase-2b-transition-sql-run2.log) |

Both runs pass health 200, anonymous protected 401, real Identity login/bearer 200, two-tenant own student reads and cross-tenant GET/POST 403 with no foreign student write. These controls are not exhaustive payment-action permissions.

Each run confirms:

- Original full 1,000 / partial 600 / approved-adjusted 800 settlement: void returns 200, fresh SQL becomes Overdue with payment Voided; Admin paid 0 and balance 1,000 / 1,000 / 800; overdue collections include the invoice. Repeat void returns 200 with captured financial snapshot unchanged.
- Generic Completed→Reconciled returns 400 with payment still Completed and no evidence; invalid status and blank dedicated reference reject without captured financial changes. Actual dedicated reconciliation returns 200 with reference/timestamp.
- Two mixed fixtures, approved adjustment 0 / 200: reconcile both collected rows using actual dedicated actions, then void 600. Other Reconciled amount 400 / 200 remains unchanged, invoice is PartiallyPaid with balance 600 and eligible for collections. Original reference/timestamp and all other captured payment fields survive void. Adjustment ledger is unchanged. Repeated void and rejected generic reconciliation, even with historic reference, leave captured financial snapshots unchanged.
- Future-due fixture: void reopens Issued, Admin paid 0 / balance 1,000, due date unchanged. Due date setup is an owned QA fixture variation, not an API due-edit claim.
- Actual invoice status action sets lowercase `cancelled`; void preserves that exact state in SQL/Admin with paid 0 / balance 1,000. **Cancelled status preservation only**: lowercase collection eligibility/normalization is not fixed or certified.
- Legacy restoration observation still accepts Voided→dedicated reconcile (200, Reconciled, reference/timestamp) while invoice state is not recomputed. This is **POLICY-PENDING, not a financial acceptance PASS**. Generic Completed restoration is also outside accepted coverage. The issue remains open.

No-write assertions concern captured invoice/payment/adjustment fields, not all tables or audit middleware. Fixtures are synthetic; no real money moved. Sequential scenarios do not certify concurrent writes or historical invalid states.

## Evidence, cleanup and limits

[77-record source snapshot](PHASE_2B_PAYMENT_TRANSITION_SOURCE_SNAPSHOT.json) extends prior 74. Of previous records, only PaymentsController, harness Program and selected-finance runner changed; previous payroll/frontend/media hashes unchanged. Added captures cover existing transition reproduction plus new regression and unit tests. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`; unrelated dirty changes preserved. [Validation/cleanup](../EVIDENCE/logs/phase-2b-transition-validation.log); `git diff --check` result recorded there.

Runner reuses `Run-ReconciledPayment.ps1 -Module Transition` with explicit `--payment-transition`, throwing/nonzero on confirmed void/evidence failures before reporting module success. Historical no-argument reproduction remains observational. This bounded mode skips other finance/media modules and is not a full critical regression, phase closure or release acceptance.

Harness ownership cleanup removed current generated databases/logins/temporary host roots, then exact name/label/loopback-checked containers. Independent final inventory checks exact current containers, owned roots and SQL listeners absent. Only synthetic current-run resources removed; older interrupted resources and normal dev/Azure data untouched. No frontend lint/typecheck/build/browser/mobile/device rerun in this API-only slice.

Restoration policy/authorization/recomputation, concurrency, broader payment-specific role/tenant/browser/device and full critical regression remain pending. Zero-net payroll decision remains pending. Adjusted Student/document/dashboard/reminder balance consumers and frontend Completed-only payment totals are follow-up work, not silently certified by this repair.

## Next bounded task

Continue the queued finance read-view consistency repair: approved adjustments in Student/document/dashboard/reminder balances and collected-payment status parity in frontend payment totals. Agreed **Sol High**; reuse accepted source findings and targeted regression, no repeat Astra audit. Restoration policy remains awaiting the previously asked human choice and is not permission to add an owner-approval workflow. Phase 2B/issues/media/device/critical/policy/release gates remain open; no deployment.
