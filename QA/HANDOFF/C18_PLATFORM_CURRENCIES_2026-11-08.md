# C18 platform overview currencies — 8 November 2026

Cloud owner. Status IMPLEMENTED / CLOUD VALIDATED; local acceptance pending.
Branch `codex/cloud-c18-platform-currencies-20261108`.
Clean base `cf98371e5edf57b883de41ef5786c2c90ad72e00` confirmed.
Latest Desktop inspected `88fd8e1`; C17 remains a separate published return.
No active competing Platform overview/dashboard source claim found. Keep all
Desktop SQL/protected-host/profile-audit work and shared handoffs intact.

Exact paths:

- `apps/api/Controllers/PlatformControlController.cs` — Overview only
- `apps/web/src/app/platform/page.tsx` — monetary overview tiles only
- `apps/web/src/lib/platform-finance.ts`
- `tests/AcademyDesk.Api.Tests/PlatformCurrencyOverviewTests.cs`
- `QA/tools/platform-currency-overview.test.cjs`
- `QA/tools/platform-currency-overview-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — new helper resolver only
- `QA/tools/provisioning-ui.test.cjs` — real helper import adapter only
- `QA/ISSUES/BUG-DATA-0016.md`
- `QA/HANDOFF/C18_PLATFORM_CURRENCIES_2026-11-08.md`

Reproduce invalid mixed-currency totals and rounded cents. Add explicit
per-currency billed/collected/outstanding groups, retain single-currency and
empty scalar values, and return null mixed monetary scalars rather than a
fictional converted total. Reuse C17's pure currency formatter, not its academy
ledger contract. Keep Draft/Void out of billing, Paid in collections and
Issued/Overdue/Payment submitted in outstanding. No payment confirmation,
status, permission, onboarding, academy, audit or write-flow change.

Static caller review found the owner dashboard and the existing SQL billing
harness. The latter asserts single-INR values and retains that contract.
Both platform/control billing rows and their modal already label each amount
with currency; they have count-only summary tiles and require no money change.
The separate academy Dashboard contract/fees-plus-payroll tile is NOT claimed.

Validate before/after money/state matrices, real Identity/HTTP owner/denial
checks with isolated InMemory stores, actual TSX plus retained provisioning,
actual Chromium, build/types/lint and full API/frontend after final source.
Real SQL, media/provider/device, full critical and production acceptance stay
Desktop-owned. No new dependencies/images, configured SQL, external sends,
main/Azure/environment/public-network changes or model loading/activation.
Keep BUG-DATA-0016 and linked P0/release gates OPEN. Sol High for monetary
contract/authority/SQL; Medium for settled import/docs.

## Implementation and compatibility

Implementation `a8d20d80abc46042a4d3a7bf31b15ead121ae361`; claim `d330bdf`.
Only the ten claimed paths changed. No platform write/status action changed.
The overview's existing stored `IsPlatformOwner` check still runs before data
queries. Grouped invoice aggregation remains an IQueryable/provider operation;
the API does not materialize all invoice entities to calculate totals. Actual
SQL translation, query plan and large-portfolio performance remain local gates.

`GET /api/platform/overview` adds `byCurrency` groups with `currency`,
`totalBilled`, `collectedBilling`, `outstandingBilling`, plus top-level
`currency` and `hasMixedCurrencies`. Flat monetary keys remain numeric for
one currency and zero for no active billing; mixed-currency values are null.
Mixed consumers must use groups. Update API and owner UI together after
acceptance; this deliberately changes nullability of invalid mixed scalars.
Academy/student/support counts, plan groups and recent audit are preserved.

| Unit | Billed | Collected | Outstanding |
| --- | ---: | ---: | ---: |
| INR |1,250.40|200.10|1,050.30|
| USD |120.16|20.01|100.15|

These are separate units, not the old unlabelled billed1,370.56 total. Draft and
Void records contribute no billing group; a portfolio containing only those
records is empty for money. Paid contributes collections; Issued/Overdue/
Payment submitted remains outstanding. Known states handle casing/whitespace.
No receipt is promoted to Paid and no exchange rate is inferred. Amounts in the
tiles and detail dialogs retain cents. Missing, malformed or old unlabelled
overview data shows `Currency details unavailable`; no activity is unitless0.00.
Unknown legacy currency has the shared labelled fallback, not an INR guess.

## Executed evidence

| Run | Actual result |
| --- | --- |
| Unchanged API22-case baseline |11 PASS /11 FAIL /zero skips |
| Unchanged TSX15-case baseline |4 PASS /11 FAIL /zero skips |
| Final platform + retained academy API target |48/48 PASS, zero skips |
| Full API |2,132/2,132 PASS, zero skips |
| New dashboard + retained provisioning target |31/31 PASS, zero skips |
| Full frontend |2,072 PASS /3 FAIL /zero skips/cancellations;77 files,2,075 tests |
| Native browser |10/10 PASS;zero runtime/interceptor errors or API mutations |
| Exact-lock build, TypeScript, lint |PASS;zero lint errors /62 existing warnings |

The25 new API cases use real MVC routing/serialization, Identity login/access
tickets, security-stamp/current-account checks and the existing owner flag.
They cover mixed/global/single/empty portfolios, exact cents, lifecycle states,
excluded draft/void currencies, canonical/legacy case and spacing, three
non-owner role denials, anonymous401 and current flag revocation403. Fresh
contexts preserve billing, academy and platform-audit snapshots on reads and
denials. All25 use isolated InMemory domain/Identity providers, disabled
migrations/bootstrap/media/PENTA and no configured SQL host. The original
baseline ran22 cases; three whitespace-state guards were added subsequently.
Final target also reruns C17's23 academy currency cases, not a weaker subset.

The31 frontend target cases are15 new monetary/count/modal checks plus16
unchanged provisioning cases. The provisioning adapter loads the real pure
helper; no existing creation/recovery assertion was removed. The full suite's
only three failures need genuine communication-SQL receipts absent in Cloud.
They remain FAIL, not skips or synthetic replacements.

Native at320/light and1440/dark covers mixed, USD-only, empty, legacy and
failed-overview data. All30 monetary detail openings preserve each tile value,
route to Billing and close; no API mutation occurs. Normal Chromium sandbox,
owned-profile cleanup and all570 unchanged exported file hashes verified.
The first attempt read the intentional loading/unavailable state too early
(2/10 PASS), and expected a support count from a deliberately failed overview.
That receipt remains retained. The final driver waits for portfolio data and
checks the unchanged zero support fallback when the overview is unavailable;
monetary/grouping assertions were not relaxed. No application repair was
needed for that harness correction.

```sh
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~PlatformCurrencyOverviewTests
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~PlatformCurrencyOverviewTests|FullyQualifiedName~FinanceCurrencySummaryTests'
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --no-build
node --test QA/tools/platform-currency-overview.test.cjs QA/tools/provisioning-ui.test.cjs
node QA/tools/run-frontend-tests.cjs
# apps/web working directory:
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
npm run lint
./node_modules/.bin/tsc --noEmit
# repository root:
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/platform-currency-overview-browser.cjs
git diff --check
node QA/tools/check-production-gates.cjs
```

Actual .NET executable `/workspace/.tools/dotnet/dotnet`10.0.403. Test commands
also used named TRX loggers and `/workspace/c18-platform-currencies-20261108`
results directory. Next16.4.3/React19.2.8 exact lock; no restore/install/image
download. Build generates89 entries,570 files and87 unchanged RSC aliases.
Full API used the exact rebuilt final-query binaries; subsequent work changed
only documentation. Production guard intentionally fails on the five P0s.

All original/final/intermediate logs, TRX, native receipts, full frontend summary
and source/assembly/export/artifact SHA256 manifest stay in that excluded raw
directory. Source, snapshot and publication checks are reproducible without
publishing private local evidence. Remote CI remains UNKNOWN; feature push is
not matched by the restricted verify workflow and no workflow was dispatched.

## Desktop integration and remaining work

Latest Desktop inspected `88fd8e1` has the separate teacher-profile audit repair
and API2,087/SQL13-of13 results. C18 inherits Cloud C17, not those newer profile/
test changes. Select only these ten paths after review; preserve newer shared
handoffs, Profile/AuditOutcome/SQL harness changes and issue evidence. Run the
full combined API suite; this source's2,132 result is not a result for a later
combined candidate. Reconcile issue paragraphs rather than overwrite newer
whole records. C17's academy monetary contract is a separate required return.

Local SQL/HTTP acceptance: owner flag true versus role-only/anonymous/revoked
users; mixed INR/USD, single INR/USD/GBP/EUR and empty/draft/void portfolios;
Paid versus Payment submitted/Issued/Overdue; canonical and schema-valid legacy
case/spacing; exact decimals, raw snapshots, counts and no read-side audits.
Exercise actual SQL group/conditional-sum translation and the existing single-INR
PlatformBilling runner. Validate owner tiles, all three detail dialogs and both
widths against real API values; retain required critical and physical-device
checks and all existing write/verification/audit-failure safeguards.

Original academy Finance and Platform overview now have bounded currency
repairs, not full BUG-DATA-0016 closure. The separate academy Dashboard still
needs its currency-aware outstanding contract and currently combines receivable
fees with payable payroll; this is a future independent packet, not fixed here.
Five P0s, missing Cloud receipts, all-role/critical/device, exact CI/images/audits
and staging/production restore/rollback remain OPEN. Mini/Plus/Pro unqualified,
Ultra deferred; model loading stopped. No main merge or Azure/environment publish.
