# C16 support and C17–C18 currency acceptance — 8 November 2026

Imported all 19 distinct paths from C17 and C18 at `3db7bf4`, preserving the
teacher-profile transaction and earlier SQL evidence. The two Cloud browser
scripts needed a trailing blank line removed for Git's whitespace check. All
application files match the Cloud return for the imported scope, and the web
tree matches C18 exactly. The 57 targeted frontend checks passed locally.

[Cloud C17 handoff](../HANDOFF/C17_FINANCE_CURRENCIES_2026-11-07.md)
[Cloud C18 handoff](../HANDOFF/C18_PLATFORM_CURRENCIES_2026-11-08.md)

## Real Identity, HTTP and SQL

| Area | Verified behavior |
| --- | --- |
| Academy finance | Empty, single USD and mixed INR/USD summaries agree with the collections queue. Cent values remain exact. An overpaid INR invoice does not offset a second INR invoice. Pending, failed, voided and wrong-currency payments do not reduce an invoice balance. Cancelled and foreign rows stay out of the eligible queue. Follow-up writes the exact invoice reference and one actor audit, and an audit fault rolls the write back. |
| Platform billing | Empty, single GBP and mixed GBP/INR/USD summaries use SQL aggregation and conditional sums. Draft and void amounts are excluded. Issued, overdue and payment submitted remain outstanding. Paid is collected. Casing and status whitespace are covered. Role-only, revoked and anonymous accounts receive denials. |
| Support intake and triage | Academy Admin creates a Critical ticket with one actor audit. The foreign academy cannot read the ticket or write into its tenant. Platform Owner reads it alongside High, Urgent and Normal controls and can resolve and close it with exact status readback and one audit for each transition. An owner triage audit fault rolls back the transition. |

The unchanged support intake baseline `367edeaf176b4102b619d4e936ad1fec` passed
26 checks before the required intake audit fault returned 500 with the ticket
already persisted. The repair opts only the academy support creation action
into the existing domain transaction, including when a Platform Owner uses
that route. The final suite includes a separate Platform Owner intake audit
fault check. The finance and owner triage fault controls are retained.

The repaired combined run passed **28/28** checks on SQL
`2adba3451ffb44fdaebfa57cc87fea8e`. The original platform billing module passed
**51/51** on SQL `7a95a4f6897d4022b0249710ec4b8d0c`. The combined API suite
passed **2,138/2,138** tests with zero skips. Both final SQL runs verified 85
application and seven Identity migrations and removed their owned databases,
logins and containers. The guarded window verified the original Docker
inventory and restored both PENTA services. Raw evidence stays local.

Cloud's exact-web export, lint, typecheck and controlled browser evidence apply
to the matching web tree. Cloud's full frontend result still records three
missing communication SQL receipts. The current run does not replace the
exact-source browser and physical-device checks against real API values, the
wider currency matrix, or critical acceptance. BUG-FUNC-0011, BUG-API-0002 and
BUG-DATA-0016 stay open. Cloud C19 owns the separate academy Dashboard monetary
contract. The five P0 and release gates remain open.
