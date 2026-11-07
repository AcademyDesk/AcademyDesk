# C17 academy financial summary currencies — 7 November 2026

Cloud owner. Status CLAIMED; implementation/verification pending.
Branch `codex/cloud-c17-finance-currencies-20261107`.
Clean base `8ed2a57774597b7aeeb419a8a45c40635421e7fd` confirmed.
Latest inspected Desktop `e9a0946`; no active competing FinanceGovernance or
academy finance-screen claim in the current handoffs. Preserve Desktop SQL work.

Exact paths:

- `apps/api/Controllers/FinanceGovernanceController.cs` — summary and monetary
  eligibility predicates shared by collection list/follow-up
- `apps/web/src/lib/finance-summary.ts`
- `apps/web/src/app/finance/page.tsx`
- `apps/web/src/app/finance-summary/page.tsx`
- `tests/AcademyDesk.Api.Tests/FinanceCurrencySummaryTests.cs`
- `QA/tools/finance-currency-summary.test.cjs`
- `QA/tools/finance-currency-summary-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — resolver for this shared module only
- `QA/tools/collections-balance.test.cjs` — load the real new shared module;
  retain existing amount/unit/net-balance assertions
- `QA/ISSUES/BUG-DATA-0016.md`
- `QA/HANDOFF/C17_FINANCE_CURRENCIES_2026-11-07.md`

Reproduce original mixed-currency API/cards. Return explicit per-currency
monetary groups without inventing exchange rates. Retain existing numeric
summary fields for single-currency data; mixed-currency flat monetary fields
become null rather than an invalid sum, with explicit currency metadata.
Both known academy consumers are claimed; the SQL harness's only summary
consumer reads the unchanged overdueInvoices count. No other runtime caller
found by scoped source search. Unknown/old monetary contracts must be visibly
unavailable in the UI rather than assigned an invented INR currency.

Count only Completed/Reconciled collections, preserve decimal amounts and
approved adjustments, and derive positive outstanding per eligible invoice
within its currency, not cross-currency or cross-invoice netting. Raw ledger
rows stay unchanged. Group money by its stored currency; different units do
not offset one another. Preserve existing gross/adjustment history and overdue
queue rules, roles, audit policy, write workflows and exports.

Original PlatformControl Overview and Dashboard money contracts are NOT claimed;
return their remaining scope explicitly. BUG-DATA-0016 stays OPEN, as do linked
P0 balance and full SQL/HTTP/browser/device/critical requirements.

Validate original/final API contract matrix, actual TSX/pure formatting, actual
Chromium with synthetic APIs, build/types/lint and full API/frontend once after
final source. Existing installed packages only; no customer/configured SQL,
external sends, image/model loading, main/Azure/environment/public-network change.
Use this bounded report rather than overwrite shared integration handoffs.
Sol High for monetary contract/authority/SQL; Medium for settled import/docs.

Pre-edit scope refinement: the collection list and follow-up debt check also
count every non-Voided state. Claim their monetary predicates in the same file
so Pending/Failed/foreign-currency payments cannot make the summary disagree
with collection eligibility. Preserve all assignment, priority, ownership,
status, audit and write payload logic. Add direct contract consistency tests;
real SQL/concurrency/required-audit acceptance remains Desktop-owned.

Regression refinement: four existing collections tests use a separate TSX
loader and reject the new summary import. Claim that adapter only; no assertion
or collection fixture is removed. Preserve existing per-invoice queue formatting
while summary cards use explicit currency codes. The first full run (2,053
PASS /7 FAIL) remains retained: four loader errors plus the three genuine SQL
receipt failures. The final run must resolve the loader errors, not skip them.
