# C17 academy financial summary currencies — 7 November 2026

Cloud owner. Status IMPLEMENTED / CLOUD VALIDATED; local acceptance pending.
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

## Implementation and monetary contract

API/screens implementation `a448103e525a83ec1080be2ec2295b63496afe63`;
retained collection adapter and unchanged table formatting at
`ddd34fd6bf6bafc0da5920df966367cd238ba83b`.

`GET finance-governance/summary` now returns `byCurrency`, sorted by normalized
stored currency code. Each group contains `currency`, `grossBilled`,
`approvedAdjustments`, `collected`, `outstanding`, `reconciled`.
`hasMixedCurrencies` identifies multiple groups; `currency` is the single unit
or null. Existing flat monetary keys are unchanged numeric values for one
currency, zero for no activity, and null for mixed currencies. The overdue
count remains numeric. **Mixed-currency consumers must use the groups**;
this is an intentional nullability change, not a converted grand total.
Ship API and both known consumers together after their acceptance gates.

Example independently checked decimal results:

| Currency | Gross | Applied adjustments | Collected | Outstanding | Reconciled |
| --- | ---: | ---: | ---: | ---: | ---: |
| INR |1,000|200|400|400|100|
| USD |100|10|50|40|50|

The mixed example's old scalar billed1,100 and collected450 have no valid
single unit; they are no longer returned as monetary totals. Net billable,
collection rate and awaiting-reconciliation cards also stay per currency:
INR800/50%/300 and USD90/56%/0. There is no exchange-rate inference.

Only Completed/Reconciled payments contribute collections. The summary clamps
outstanding at each non-cancelled invoice, not after netting unrelated debt
against an overpayment. A payment in a different stored unit remains a
collection in that unit and cannot reduce this invoice's debt. Matching tenant,
invoice, currency and collection-state predicates now align list and follow-up
eligibility. Cancellation casing is handled consistently. Gross/adjustment
history is retained; no ledger row, payment state or accounting entry changes.

The shared client module validates grouped money before displaying it. Missing
group metadata or nonfinite/invalid fields show `Currency details unavailable`,
not guessed INR/zero. Empty activity is displayed as unitless0.00. Legacy
invalid currency codes use a labelled fallback. Individual collection-row
formatting, remaining balances, fractional cents and export actions are retained.

## Actual evidence

| Executed run | Result |
| --- | --- |
| Original API17-case baseline |3 PASS /14 FAIL /zero skips |
| Original controlled TSX15-case baseline |0 PASS /15 FAIL /zero skips |
| Final API/retained financial target |50/50 PASS, zero skips |
| Full API at this source |2,107/2,107 PASS, zero skips |
| Final new + retained UI collection target |26/26 PASS, zero skips |
| Full frontend after adapter repair |2,057 PASS /3 FAIL /zero skips/cancellations;76 files,2,060 tests |
| Configured webpack export |PASS;89 generation entries,570 files,87 unchanged aliases |
| TypeScript / lint |PASS; zero lint errors /62 existing warnings |

The API target comprises23 new cases and27 existing collection/balance cases.
The first17 original cases establish before/after behavior; six subsequent
authenticated HTTP cases verify the new serialized contract using real Identity
login/access tickets and the global access filter. AcademyAdmin and FinanceUser
get exact single/mixed JSON; anonymous, foreign, Teacher and missing-subscription
reads deny access without changing captured ledger rows/audits. Fixtures use
unique InMemory domain/Identity stores, disabled migrations/bootstrap/media/PENTA,
not a configured SQL host. These tests are not SQL translation/durability proof.

The frontend target comprises15 new currency cases and11 retained collection
cases. The initial full suite's four additional failures were import adapters
for the new real helper, not skipped cases; all old assertions are unchanged.
The only three remaining failures require genuine communication-SQL receipts
not present in Cloud. Prior local receipts are neither copied nor fabricated.

Commands used existing cached SDK10.0.403 / exact-lock Next16.4.3 and React19.2.8:

```sh
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~FinanceCurrencySummaryTests
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~FinanceCurrencySummaryTests|FullyQualifiedName~CollectionsBalanceTests|FullyQualifiedName~FinanceConsumerTests|FullyQualifiedName~ReconciledBalanceTests'
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --no-build
node --test QA/tools/finance-currency-summary.test.cjs QA/tools/collections-balance.test.cjs
node QA/tools/run-frontend-tests.cjs
# apps/web working directory:
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
npm run lint
./node_modules/.bin/tsc --noEmit
# repository root:
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/finance-currency-summary-browser.cjs
git diff --check
node QA/tools/check-production-gates.cjs
```

Actual SDK executable `/workspace/.tools/dotnet/dotnet`; .NET commands add their
named TRX logger and results directory. No restore/install/image download.
Raw baseline/final logs, TRX, frontend summary, native receipts, original source
and source/assembly/export/artifact SHA256 hashes remain excluded from Git under
`/workspace/c17-finance-currencies-20261107`. Diff check passes; production
guard still exits1 for five open/unverified P0s.

## Integration and Desktop acceptance

Latest Desktop inspected `88fd8e1` has the additional teacher-profile atomic
audit repair and its SQL13/13/full API2,087 evidence. Those newer profile/test
changes are deliberately not overwritten here. C17's2,107 API result belongs
to its stated older branch base plus this packet; run the full combined API
suite after selecting only the11 claimed paths into the current local branch.
Preserve Desktop's updated BUG-DATA-0016/P0/shared records by merging the scoped
evidence paragraphs, not replacing newer whole files.

Desktop return: real SQL/HTTP single INR/USD and mixed groups with exact decimal
amounts; one overpaid invoice plus another unpaid invoice; pending/failed/voided
versus Completed/Reconciled; applied versus pending adjustments; cancelled and
foreign/unsupported-currency rows; empty and legacy currency controls. Assert
summary/queue/follow-up agreement, all ledger and Identity bytes unchanged on
reads/denials, current actor/role/module isolation, actual SQL trim/case behavior
and required audits/fault boundaries for follow-up writes. Then use both real
screens at mobile/desktop widths and finish critical/device acceptance.

Original PlatformControl Overview and its cards, Dashboard monetary fields,
other unclaimed ledger projections and full BUG-DATA-0016 closure remain OPEN.
Do not reuse this academy summary's repair to mark those endpoints qualified.
Keep the five P0s and remaining CI/images/security/staging/production recovery
gates explicit. Feature pushes do not dispatch the restricted verification
workflow; exact remote CI stays UNKNOWN. No main/Azure/environment publication,
new public access or model activation. Mini/Plus/Pro remain unqualified; Ultra
deferred, model loading stopped. Sol High for SQL/integration; Medium for docs.

## Final native publication check

Final Chromium16/16 PASS, zero runtime/interceptor errors or API writes. At
320/light and1440/dark, both screens verify mixed, USD-only, empty and unlabelled
legacy responses. Mixed nullable scalar fields never crash the client or become
a fabricated INR amount. Owned profile cleanup and normal sandbox pass; every
one of570 export file hashes is unchanged. These are synthetic API-rendering
checks, not real SQL, actual-device, visual-reference or model qualification.
The final discoverable branch tip contains the implementation pins above; its
remote SHA and source/assembly/artifact manifest are retained in local artifacts.
