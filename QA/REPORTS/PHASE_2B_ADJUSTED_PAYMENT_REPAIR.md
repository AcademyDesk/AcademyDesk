# Phase 2B — Approved-adjustment payment guard and settlement repair

2026-10-01. **Local original-case and bounded module retest PASS twice; issue/phase NOT CLOSED.** Accepted Astra audit and twice-failing Phase 2A evidence reused, not re-audited. Agreed Sol High allocation. No commit/push, Azure access/deployment, normal development database change or additional frontend/backend server.

## Local repair and before-fix evidence

[BUG-DATA-0002](../ISSUES/BUG-DATA-0002.md): PaymentsController.Create now uses `TotalAmount - AdjustedAmount` for collection limit and exact-settlement Paid status, retaining the prior Completed/Reconciled collection predicate. Only this application controller changed this slice. Approved/applied aggregate is already stored in AdjustedAmount; pending/rejected proposals must not be deducted separately. No schema, approval workflow, authorization, void-restoration or concurrent-write policy change.

Ten new controller tests before repair: **5 PASS / 5 expected FAIL**, exposing accepted excess 300/200.01, accepted collection after full adjustment, and wrong Paid status on ordinary/mixed-status settlement. [Before-fix evidence](../EVIDENCE/logs/phase-2b-adjusted-before.log) retains build output plus an explicitly summarized observation of the failing result, not complete raw stack traces. Original twice-failing real HTTP/SQL evidence remains unchanged in [Phase 2A report](PHASE_2A_INVOICE_ADJUSTMENT_REPRO.md).

## Final-source checks

- **102/102 API tests PASS**, zero failures/skips: 10 new tests plus existing 92, including seven prior reconciliation tests. [Test log](../EVIDENCE/logs/phase-2b-adjusted-tests.log). InMemory controller tests are not SQL certification.
- SQL harness build PASS, zero warnings/errors. [Build log](../EVIDENCE/logs/phase-2b-adjusted-build.log).
- Two fresh isolated SQL Server 2022 containers: real Program/Identity/TestServer requests and fresh no-tracking SQL snapshots, database-scoped runtime login and exact owned context targets. Both exit 0; 80 application / 7 Identity migrations. Runtime inventory 307 routes, 296 controller method/routes, 10 framework Identity method/routes; digest `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`.

| Run | UTC start | Loopback SQL port | Duration | Evidence |
| --- | --- | --- | --- | --- |
| `f59d05c9a36e4aab9289dab54ccbaf32` | 2026-10-01T05:38:29.1371859Z | 65014 | 27.7 s | [First run](../EVIDENCE/logs/phase-2b-adjusted-sql-run1.log) |
| `e3ba0fc78d9f408f871b3784bab713a2` | 2026-10-01T05:39:10.3331515Z | 60939 | 31 s | [Second run](../EVIDENCE/logs/phase-2b-adjusted-sql-run2.log) |

Both runs pass health 200, anonymous protected 401, actual Identity login/bearer 200 and two-tenant own reads/cross-tenant GET/POST 403 with no foreign student write. FinanceControls entitlement and real approval checks remain enabled.

Three original independent invoices pass in both runs:

1. Invoice 1,000, approved adjustment 200, collected 600: admin balance 200; extra 300 rejected 400 with financial snapshot unchanged.
2. Separate invoice with the same approved adjustment/collection: exact 200 returns 201, fresh SQL collected 800, Paid; admin zero balance/Paid.
3. Full approved adjustment 1,000: zero payments, zero admin balance and Paid; attempted collection 1 rejected 400 with financial snapshot unchanged.

Zero adjustment 400 leaves financial snapshot unchanged; valid proposals persist PendingApproval without applying money; approval applies once with exact amount/timestamps/notes; repeated approval 409 leaves captured financial snapshot unchanged. These are real HTTP controls, not test-only approval bypasses.

Additional independent adjusted invoice passes actual reconciliation of collected 600, synthetic fixture-only Voided 75 exclusion, excess 200.01 rejection/no financial change, accepted 199.99 leaving PartiallyPaid/balance 0.01, final 0.01 producing Paid/collected 800/admin zero, and post-settlement 0.01 rejection/no financial change. PendingApproval/rejected proposal on another invoice leaves adjustment aggregate zero: actual 600 payment, rejection decision then 400 settlement yields Paid/collected 1,000. Voided fixture is not a tested/approved status-restoration policy. No-change assertions cover captured invoice/payment/adjustment rows, not audit logs or every table.

## Scope, cleanup and remaining gates

[70-record source snapshot](PHASE_2B_ADJUSTED_PAYMENT_SOURCE_SNAPSHOT.json) extends prior 67 with adjustment reproduction/regression/test files. Previous captured hashes change only PaymentsController, harness Program and selected-finance runner; frontend/media unchanged. [Validation and cleanup](../EVIDENCE/logs/phase-2b-adjusted-validation.log). HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`; unrelated dirty changes preserved. `git diff --check` PASS, line-ending warnings only.

Runner reuses the prior finance script with explicit `-Module Adjustment` (default Reconciliation retained), opt-in strict `--finance-adjustment`. Regression failure throws/nonzero; historical no-argument reproduction still observes/prints original rules without redefining exit 0 as financial acceptance. Mode deliberately skips unrelated media/payroll/transition/full-critical cases; not a green full critical suite.

Harness ownership cleanup removed both run-owned databases/logins/host folders; exact name/label/loopback validation preceded container stop/removal. Independent final checks confirm both containers, owned temporary roots and SQL listeners absent. Only synthetic current-run data removed; older interrupted resources untouched. No customer data removed.

**Related adjusted-balance presentation remains unfixed:** Student portal projection (`PortalController.cs:225`), Student HTML invoice (`:272`), reminder (`FeeRemindersController.cs:25`) and dashboard outstanding (`DashboardController.cs:64`) still omit AdjustedAmount in their formulas. This slice verifies collection/SQL/Admin invoice views only; it does not claim Student/Guardian document/dashboard/reminder consistency. These source-confirmed follow-ups were already carried from the previous checkpoint; no runtime certificate for them. Keep BUG-DATA-0002 OPEN until required consumer/critical acceptance. Over-adjustment/refund semantics, multiple adjustment types, concurrent approval/collection and broader role/browser/device coverage remain pending. Sequential guard is not protection against concurrent overcollection. No frontend lint/typecheck/build/recording-device rerun.

## Next bounded task

Proceed to already reproduced [BUG-DATA-0003](../ISSUES/BUG-DATA-0003.md), reject negative-net payroll before persistence, using the existing reproduction and guarded boundary regression. Agreed **Sol High**. Do not silently decide whether zero-net payout is allowed; preserve/flag that unresolved business-policy gate. The related adjusted-display follow-up stays queued for finance consumer regression, not forgotten. Phase 2B/issues/critical/media/device/policy/release gates remain open; no deployment.
