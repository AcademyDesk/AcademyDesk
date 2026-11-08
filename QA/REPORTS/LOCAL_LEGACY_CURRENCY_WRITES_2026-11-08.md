# Legacy currency writes — 8 November 2026

The accepted C20 read contract excluded an old INR receipt from a USD
receivable. The independently implemented write queries still counted it.
The same real HTTP and SQL fixture passed **3/11** before the repair
(`2138982b7c8a461bb9ae2ecbe7e09232`).

| Write | Observed baseline |
| --- | --- |
| Record payment | The invoice returned USD 70.15 due. Exact collection returned 400. |
| Set invoice status | A legacy INR 100 receipt let an unpaid USD 100 invoice become Paid, and the valid Overdue status was rejected. |
| Void | The payment became Voided but the invoice incorrectly stayed Paid. |
| Restore | Both restoration to Completed and restoration with bank evidence rejected an otherwise exact adjusted settlement. |
| Approve adjustment | The writer rejected an exact same-currency reduction against the actual outstanding balance. |

The five sums now use one tenant-and-invoice-scoped SQL calculation under the
existing invoice lock. Only matching-currency Completed and Reconciled
payments reduce the invoice balance. Currency comparison trims and normalizes
case. The void and restore checks exclude the current payment. Existing
currency validation for the restoration target and the adjustment, transaction
ownership, status rules, permission gates and required audit behavior remain.

The final SQL run `7d269bf7e292445898385eea91878591` passed **11/11**,
including one-cent overages, wrong-currency restoration denial, correct actor
and route audits, unchanged legacy rows, and a required-audit failure that
rolled back the collection. The existing FinanceIntegrity run
`0c387cd5527e486d8c8364cdb79cc670` passed 12 integrity and 28 reduction cases,
including the existing concurrency and audit-fault controls. The full
combined API suite passed **2,183/2,183**, with zero skips.

The real Payments form presented USD 70.15 and settled it with one new
USD payment and one audit. The API rejected USD 70.16 before that successful
submission. Fresh readback returned Paid with USD 0.00 due, and the original
INR receipt and unrelated captured rows stayed unchanged. Disposable SQL
`8cb6e02508304a1fb7590b0b155464d2` passed all five real browser/SQL phases,
retaining Reports and the payment-transition controls. Edge saw 64 API
responses, zero rate limits and zero page errors. The guarded windows removed
their owned SQL containers and restored both original PENTA services.

Cloud C21's separate Dashboard detail recovery is tracked in the [Cloud
handoff](../HANDOFF/C21_DASHBOARD_DETAIL_AVAILABILITY_2026-11-08.md). The
payment browser proof uses the C20 web tree, which is byte-identical across the
backend repair. C21 passed 60 targeted frontend checks locally and its Cloud
full frontend run retains three missing communication SQL receipt failures. C21 real-host acceptance is pending. The current
packet qualifies the scoped writers. The separate reminder and family balance
readers, physical devices, linked critical flows, five P0 records and exact
release gates remain open.
