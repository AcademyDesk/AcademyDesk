# Live Reports and payment transitions — 8 November 2026

Integrated Cloud C20 `d22ca1a` at `512e785`. The eight scoped source, test and
handoff files match Cloud. The issue record combines the new Cloud notes
with the retained local C17–C19 evidence. The complete web tree matches C20.
The exact-lock Next.js 16.4.3 export and postbuild passed and the local browser
verified 658 exported files. All 32 targeted Reports tests passed locally.
[Cloud C20 handoff](../HANDOFF/C20_REPORTING_CURRENCIES_2026-11-08.md)

| Area | Real browser, HTTP and SQL result |
| --- | --- |
| Reports at 320px | The page showed the retry state after an interrupted expense request. Retrying loaded the real tenant data. Collected, Outstanding and Expenses matched the INR, USD and EUR fixtures. The three dialogs showed reconciled receipts, fees and separate payroll. |
| Downloads | Both invoice export buttons produced identical CSVs. The invoice formula-like cell was escaped, headers and currency columns matched, and the foreign invoice was absent. The expense CSV contained INR 20.05 and USD 5.25. |
| Currency readback | The USD invoice returned USD 30.35 paid and USD 70.15 outstanding after the adjustment. Pending, failed, voided and different-currency rows did not reduce the USD receivable. Dashboard and Finance groups agreed. Foreign tenant, Teacher and missing-module invoice reads were denied without captured writes. |
| Payment transition at 1440px | The authenticated API voided the INR 200 reconciled payment on an adjusted invoice and preserved its original evidence. The invoice became PartiallyPaid with INR 200 due. Generic reconciliation and blank evidence were rejected. The dedicated reconciliation API restored Paid with zero balance and new evidence. Fresh browser ledger reads showed the corresponding states. Each successful change wrote one actor audit and preserved unrelated captured data. |
| Legacy collection gap | On the same USD invoice, POST of the displayed USD 70.15 returned 400 because the write calculation still counted the accepted INR 70.15 receipt. The rejected attempt left SQL unchanged. The corrected read contract does not repair the legacy write contract. |

The reconciliation screen presents voided and reconciled states as read-only.
The transition driver used the existing API with the browser's authenticated
session, then verified the actual page's readback. The first attempt matched
the login URL without its query string and timed out after a successful login.
The corrected attempt matched the path and reused the source-verified build.

SQL run `6bb1b9dcd2ea4f35ae12e267901ebb56` passed five browser/SQL phases and
three authority checks. Edge recorded 52 API responses and no rate limit or
runtime error. The run verified 85 application and seven Identity migrations,
removed its owned SQL database, login and container, and restored the original
Docker inventory and both PENTA services. The combined API suite passed all 2,183 tests with zero skips.

Next local scope is the legacy different-currency write sums in invoice status,
collection, void/restoration and adjustment approval. Keep invoice locking,
audit and same-currency concurrency safeguards while verifying each branch.
Cloud C21 owns Dashboard detail availability. BUG-DATA-0016, the five P0
records, remaining device and critical cases, exact-release CI/images, and
staging recovery remain open.
