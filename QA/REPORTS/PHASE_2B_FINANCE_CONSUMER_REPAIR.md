# Phase 2B — Finance balance-consumer consistency repair

2026-10-01. **Local consumer repair and two fresh HTTP/SQL module runs PASS; Phase 2B/issues remain OPEN.** Accepted Astra findings reused, not re-audited. Agreed Sol High allocation. No commit/push/Azure deployment, normal development database change or additional listening development frontend/backend; the existing isolated in-process TestServer supplies real HTTP/middleware/Identity/SQL verification.

## Bounded repair

- Portal Student invoice balances and downloaded HTML invoices now subtract applied `AdjustedAmount` as well as Completed/Reconciled collections, with the existing Admin invoice balance's zero floor. Download explicitly labels the applied adjustment amount.
- Dashboard OutstandingBalance subtracts applied adjustments; TotalInvoiced remains gross and TotalPaid remains actual Completed/Reconciled money. No response-contract or gross-total definition change.
- Fee reminders subtract applied adjustments; zero/negative remaining balance queues no notification. PendingApproval/Rejected proposals do not reduce debt.
- Payments page uses the invoice API's canonical `balance` for invoice options, directory, selected detail, amount prefill/max and open/outstanding KPIs. Collected KPI includes Completed and Reconciled, excludes Voided. This removes a second gross-only calculation on the client. Invoice responses already expose `balance`; no guessed fallback to gross totals.

Product changes are limited to three controllers and one Payments page. Previous payment guard/void/evidence, payroll and media changes are preserved. Cancellation eligibility, over-adjustment/negative-credit policy, restoration, cross-currency grouping, concurrent writes, historical ledger repair and zero-net payroll policy are not changed or certified. Dashboard preserves its aggregate signed arithmetic; no new negative-credit policy is implied.

## Before/final evidence

- New reminder unit tests before repair: **1 PASS/5 expected FAIL**. [Before API log](../EVIDENCE/logs/phase-2b-consumer-before-api.log). The first `--no-restore` invocation returned empty output and is not execution proof; only the subsequent actual six-test run counts.
- Actual Payments page executed with controlled hook state before repair: **1 PASS/6 expected FAIL** (wrong Collected/outstanding/options/detail/prefill and fully adjusted invoice still selectable). [Before client log](../EVIDENCE/logs/phase-2b-consumer-before-client.log). These are actual page arithmetic/render/event-handler tests, not a live browser or React lifecycle certification.
- Final **140/140 API tests PASS**, no failures/skips: previous 134 plus six reminder cases. [API test log](../EVIDENCE/logs/phase-2b-consumer-tests-api.log). Existing earlier guard/transition/payroll/access tests run in this suite; InMemory does not substitute for SQL or exhaustive permissions.
- Final **7/7 controlled Payments page tests PASS**. [Client test log](../EVIDENCE/logs/phase-2b-consumer-tests-client.log). Covers collected status parity, canonical balance throughout the page, selected maximum/prefill, fully adjusted exclusion and empty ledger.
- Full frontend typecheck PASS, no emit/incremental file. [Typecheck](../EVIDENCE/logs/phase-2b-consumer-typecheck.log). Targeted Payments lint: 0 errors/1 existing `load` dependency warning, down from 0 errors/2 warnings before repair. [Before/final lint](../EVIDENCE/logs/phase-2b-consumer-lint.log). This is not a whole-frontend lint pass.
- Harness build PASS, 0 warnings/errors. [Build](../EVIDENCE/logs/phase-2b-consumer-build.log).

## Real Identity/HTTP/SQL verification

Two fresh SQL Server 2022 containers; exact owned runtime login and both contexts, 80 application/7 Identity migrations. Real health 200, anonymous protected 401, login/bearer 200 and two-tenant own reads/cross-tenant GET/POST 403 controls PASS. Runtime digest remains `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9` (307 routes/296 controller method-routes/10 framework Identity method-routes).

| Run | UTC start | Loopback SQL port | Duration | Evidence |
| --- | --- | --- | --- | --- |
| `77f29c12cdd34e6ab33bc8c4f870471c` | 2026-10-01T06:12:44.9551337Z | 64281 | 59.9 s | [First run](../EVIDENCE/logs/phase-2b-consumer-sql-run1.log) |
| `89d4822bee9646cf89447a1e83b014a5` | 2026-10-01T06:14:03.2433326Z | 64303 | 34.9 s | [Second run](../EVIDENCE/logs/phase-2b-consumer-sql-run2.log) |

Each run passes six stages, all on gross invoices of 1,000:

| Stage | Applied adjustment | Collected | Balance | New reminder notifications |
| --- | --- | --- | --- | --- |
| Mixed Reconciled 600 + Completed 100, Voided 75 excluded | 200 | 700 | 100 | 1, amount 100 |
| Same invoice exactly settled | 200 | 800 | 0 | 0 |
| Fully adjusted, no payment | 1,000 | 0 | 0 | 0 |
| Pending proposal 200, not applied | 0 | 600 | 400 | 1, amount 400 |
| Rejected proposal still unapplied | 0 | 600 | 400 | 1, amount 400 |
| Cent boundary | 200 | 799.99 | 0.01 | 1, amount 0.01 |

At each stage, fresh SQL invoice/payment/adjustment snapshots, Admin invoice list, actual Student portal and HTML download agree; dashboard gross/paid/outstanding agree with fresh same-tenant aggregate SQL. Reminder count, fresh notification delta and exact message amount agree. Reads/reminder execution preserve captured financial snapshots; audit/notification writes are expected, not a claim of no writes to any table. Payments/adjustment creation and approval/rejection/reconciliation use real actions. The Voided 75 row is isolated fixture data, not restoration-policy acceptance. Three negative HTTP controls pass: foreign Admin reminder, Student reminder and foreign Student document denied 403. These are bounded controls, not exhaustive all-role/all-tenant action authorization.

## Source, cleanup and remaining gates

[81-record source snapshot](PHASE_2B_FINANCE_CONSUMER_SOURCE_SNAPSHOT.json) extends prior 77. Changed previous captures: Portal/Dashboard/FeeReminders controllers and harness Program/runner. Four added captures: Payments page, reminder tests, SQL consumer module and page tests. Earlier payment/transition/payroll/media hashes unchanged; HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`, unrelated dirty changes preserved. [Validation and cleanup](../EVIDENCE/logs/phase-2b-consumer-validation.log).

Explicit `Run-ReconciledPayment.ps1 -Module Consumers` selects fail-closed `--finance-consumers`; no rate-limit/audit/auth bypass. Module success is not the full critical suite. Ownership cleanup removes current-run databases/logins/temporary roots and exact name/label/loopback-checked containers; independent inventory checks current container/root/listener absence. Only disposable synthetic resources removed; older interrupted resources/dev/Azure untouched.

No live Payments browser, Guardian-specific finance permission, mobile/visual approval or production build/deployment performed in this slice. Whole-project lint, broad role/tenant/critical/concurrency acceptance and media/device gates remain pending. [BUG-DATA-0001](../ISSUES/BUG-DATA-0001.md) and [BUG-DATA-0002](../ISSUES/BUG-DATA-0002.md) remain OPEN despite the bounded consumer gaps now passing. Restoration and zero-net payroll decisions remain outstanding; no policy inferred from repeated “next”.

## Next bounded task

Finance action-level role/tenant regression on already identified permission gates, using synthetic fixtures and existing accepted mapping. **Sol High** remains appropriate for this planned implementation/verification task; no repeat Astra source audit. Keep concurrent financial-write, browser/device, full critical and policy gates separate and do not declare Phase 2B closed.
