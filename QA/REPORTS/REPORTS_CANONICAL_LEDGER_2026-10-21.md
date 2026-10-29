# Reports — canonical balances, currency and exact decimal presentation

21 October 2026. `D:/AcademyDesk-codex-p0`, `codex/penta-search`, unchanged HEAD.
Read-only consumer repair continuing BUG-DATA-0001 and retained FP2. No backend
ledger, tax, financial policy, permissions, transaction or model rule changed.

## Reproduced before the repair

Four tests execute actual Reports TSX with authoritative synthetic DTOs. Before:
**1passed/3failed**. Reconciled600 displayed collected0; currency-specific totals
were absent/mislabelled INR; exact expense9999999999999999.99 displayed a rounded
10000000000000000.00. Source also reconstructed outstanding from gross and paid,
instead of using existing adjusted/canonical balance, and labelled an incomplete
collections-minus-expenses subtotal as Net cash.

## Delivered

- [x] Both Completed and Reconciled are collected, never Voided. Same list drives
  collected tiles and detail; no model or duplicate local finance rule.
- [x] Outstanding detail/totals use the existing server `balance` field, not gross
  minus paid or a portfolio subtraction that can hide another invoice's debt.
- [x] Currency-specific presentation; no foreign exchange or mixing currencies.
  Reuses existing strict `expenseCents` / `formatExpenseCents` for integer-cent sums.
- [x] Existing `amountExact` / `netAmountExact` used for expense/payroll display.
  Missing or unsafe numeric/currency data is **Amount unavailable**, not invented
  zero/default INR/rounded precision. Empty sets say No amounts recorded.
- [x] Fee receivables and unsettled payroll records are visibly separate, not added
  into one accounting metric. Paid/NoCashDue are not listed as unsettled payroll.
- [x] Renamed subtotal to Collections less recorded expenses and explicitly stated
  it is not a cash-flow statement or payroll-settlement claim.
- [x] Invoice CSVs show currency/gross/approved reductions/collected/outstanding/date;
  expense CSV uses currency and exact amount. Unsafe values remain unavailable.
  Existing CSV text guard still applies; exports remain human spreadsheet views.

No assumption that all supported input amounts arrive exact: invoice/payment JSON
still uses existing numeric fields. Unsafe values fail visibly; adding exact fields
to those APIs would be a separate paired contract/acceptance change, not fabricated
precision in this view. No privileged Reports/API access broadened for FinanceUser.

## Verification boundary

- [x] Four actual TSX projection/detail cases pass, including reconciled/voided,
  canonical adjusted balance, independent invoice debt, currency separation,
  payroll distinction/NoCashDue and maximum exact expenses.
- [x]16 helper cases pass: integer-cent arithmetic, separate currency subtraction,
  max exact/negative/zero decimals, unsafe/missing values, invalid currencies and
  unsettled visibility for unavailable balances.
- [x] Combined Reports/download/import/serializer/inventory suite:101passed.
  The20 Reports checks are included, not added to101 again.
- [x] Final exact-source typecheck/targeted lint and87-route export passed.
- [x] Actual exported React pages:36 full-page journeys across Reports, Data
  Operations and Finance,320/1440 light/dark. Reports' four journeys verify the
  Reconciled600 tile, canonical200 fee balance, collected detail and guarded
  malicious-name native CSV. Eight download-success journeys plus denied/network/
  wrong-type recovery are exercised on the other pages. No page errors; no backend
  writes or external traffic. Synthetic API interception is not SQL/Identity proof.

Full-page receipt:`QA/EVIDENCE/exports-fullpage-1792557864459/result.json`.
This is bounded functional viewport/theme evidence, not all-screen visual approval,
physical Android/iOS, exhaustive mixed-currency or model integration acceptance.

Actual SQL/domain ledger evidence is retained, not rerun for a display-only change.
BUG-DATA-0001, physical devices, full role/mixed-currency accounting review and
enterprise/release gates stay OPEN. No commit/push/main/Azure/training activation.
