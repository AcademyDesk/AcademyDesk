# Phase 2B — Reconciled-payment balance repair

2026-10-01. **Local original-case and bounded module retest PASS, twice on fresh SQL. Issue/phase NOT CLOSED.** Accepted Astra audit and original twice-failing Phase 2A reproduction reused, not repeated as a new audit. Continue agreed Sol High allocation. No commit/push, Azure access/deployment, normal development database change or new frontend/backend server.

## Repair and before-fix evidence

[BUG-DATA-0001](../ISSUES/BUG-DATA-0001.md): reconciliation changes a collected payment from Completed to Reconciled, but several consumers counted only Completed. Five predicates in four controllers now count Completed **or Reconciled**, excluding Voided: collection guard/status, Student/family invoice aggregation, Student invoice HTML document, dashboard and reminders. Existing invoice-list/profile non-Voided aggregation already includes reconciled payments; untouched. Authorization, schema, payment lifecycle and concurrency handling unchanged.

Seven new controller tests before the repair: **4 PASS / 3 expected FAIL**, exposing ignored reconciled collection, wrong settlement status and reminder for already collected money. [Before-fix log](../EVIDENCE/logs/phase-2b-reconciled-before.log). Original real HTTP/SQL failing evidence remains in [Phase 2A reproduction](PHASE_2A_RECONCILED_PAYMENT_REPRO.md); not overwritten.

## Final-source verification

- **92/92 API tests PASS**, zero failures/skips: seven new reconciliation cases plus existing 85 tests. [Test log](../EVIDENCE/logs/phase-2b-reconciled-tests.log). Controller cases use InMemory, not SQL certification.
- SQL harness build PASS, zero warnings/errors. [Build log](../EVIDENCE/logs/phase-2b-reconciled-build.log).
- Two fresh isolated SQL Server 2022 containers, real Program/Identity/TestServer HTTP and fresh-context SQL snapshots. Both contexts resolve to exact owned target/runtime login; migration histories 80 application / 7 Identity. Both runs exit 0. Runtime inventory 307 routes, 296 controller method/routes, 10 framework Identity method/routes; digest `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`.

| Run | UTC start | Loopback SQL port | Duration | Evidence |
| --- | --- | --- | --- | --- |
| `52e82a32ee184482bdc5072697c91bec` | 2026-10-01T05:15:37.0334207Z | 60283 | 37.8 s | [First run](../EVIDENCE/logs/phase-2b-reconciled-sql-run1.log) |
| `43ec6798a16c4c30a37fbb0aa6462831` | 2026-10-01T05:31:20.1244866Z | 62320 | 28 s | [Second run](../EVIDENCE/logs/phase-2b-reconciled-sql-run2.log) |

Both runs passed anonymous 401, real login/bearer 200, two-tenant own reads/cross-tenant GET/POST 403 with no foreign write. Original case: invoice 1,000, payment 600, reconciliation retains paid 600 / balance 400 in Admin and Student views; excess 500 rejected 400 before and after reconciliation with unchanged SQL. Blank reconciliation reference rejected without SQL change.

Bounded consumer regression adds a synthetic Voided 75 row and actual Completed 100 payment: paid 700, balance 300 in both views, HTML document, dashboard and persisted InApp reminder message. Actual final 300 payment produces SQL Paid status and collected sum 1,000 across four rows (Voided excluded), zero portal balances. One extra cent rejected 400 with complete invoice/payment snapshot unchanged. Settled reminder queues zero and creates no additional notification. Voided row is fixture data, not a tested/approved void-restoration workflow; InApp SQL notification does not certify email/SMS delivery.

## Evidence, cleanup and limits

[67-record source snapshot](PHASE_2B_RECONCILED_PAYMENT_SOURCE_SNAPSHOT.json) extends prior 59 records with eight captured controller/test/harness files. Of previous records, only harness Program changed; frontend/media source unchanged this slice. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`; unrelated dirty changes preserved. [Validation/cleanup](../EVIDENCE/logs/phase-2b-reconciled-validation.log). `git diff --check` PASS; line-ending warnings only.

Harness ownership cleanup removed each synthetic database/login and temporary host folders, then exact name/label/loopback-checked SQL containers were stopped/removed. Independent final inventory confirms both exact containers and listeners absent. Older interrupted resources outside these two runs untouched. No customer data removed.

`--finance-reconciliation` is an explicit bounded mode with strict failure exit; it does not run adjustment/payroll/transition/media modules or claim full critical suite PASS. Full critical combined suite, browser/device finance coverage, broader role/tenant cases and concurrent collection remain pending. Sequential read/write guard is **not** concurrent-overcollection protection. This API-only delta did not rerun frontend lint/typecheck/build or actual recorder/device gates. Earlier client results are historical.

## Next bounded task

Repair already reproduced [BUG-DATA-0002](../ISSUES/BUG-DATA-0002.md), approved invoice adjustments excluded from collection guard/settlement status, then original-case and bounded regression on fresh guarded HTTP/SQL. Agreed **Sol High**; no repeat Astra audit. This slice deliberately does not fix adjusted-balance arithmetic in guard/dashboard/download/reminders. Separate zero-net payroll and voided-payment restoration policy choices remain unresolved. Phase 2B, all issue closure/critical/device/release gates remain open.
