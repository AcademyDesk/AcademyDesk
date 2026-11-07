# C14 fee-reminder eligibility and balance — 7 November 2026

Cloud owner. Status: IMPLEMENTED / CLOUD VALIDATED; local acceptance pending.
Branch `codex/cloud-c14-fee-reminders-20261107`.
Clean base `508c9bb34c68364c85be2f8ac23f163505a120d9` includes C12/C13.
Latest inspected Desktop `b474042` imports C12; P0 packet at `ad54028` complete.
No active competing fee-reminder source claim found in these handoffs.

Exact paths:

- `apps/api/Controllers/FeeRemindersController.cs`
- `apps/web/src/app/fee-reminders/page.tsx`
- `tests/AcademyDesk.Api.Tests/FeeReminderEligibilityTests.cs`
- `QA/tools/fee-reminder-eligibility.test.cjs`
- `QA/tools/fee-reminder-eligibility-browser.cjs`
- `QA/ISSUES/BUG-DATA-0019.md`
- `QA/HANDOFF/C14_FEE_REMINDERS_2026-11-07.md`

Reproduce cancelled reminders at bulk/targeted entry points. Exclude cancelled
invoices in the API; preserve existing positive net, adjustments, Completed/
Reconciled, due-date and tenant rules. Make the screen use the existing invoice
API's authoritative Balance, exclude cancellations and display each currency
separately. This advances the linked BUG-DATA-0001/0002 consumer paths, not their
full P0 closure. Preserve current queue channel/retry/audit policy; no provider send.

Validate baseline/final controlled TSX, API contract tests and retained finance
regressions, actual Chromium with clearly synthetic replies, exact-lock build/
types/lint and full API/frontend suites once after final source. Reuse C13 backend
evidence until the API source changes. No dependency/image downloads, SQL host,
models, main/Azure/environment publication. Return owned SQL/HTTP acceptance to
Desktop; preserve its shared handoff and P0 record ownership. High for financial
rules/SQL; Medium for settled read-display/import work.

## Source and reproduced defect

Implementation `ce9470114cedb7fd5371b7078fbc41883785d373`; claim `a2ea187`.
Original controller:3/12 pass,9 fail. All eight cancellation-casing/entry-point
cases created an unwanted reminder, and the mixed bulk case queued three
instead of two. Original TSX:1/8 pass,7 fail, including a targeted cancelled ID.
Original exported screen:zero of six browser scenarios passed; failure receipt
retained with zero interceptor/runtime errors and successful owned cleanup.

The API excludes cancellation case-insensitively in its existing scoped query.
The screen uses the existing invoice projection's `balance`, which already
accounts for applied adjustments and Completed/Reconciled collections. It
excludes cancelled/nonpositive/nonfinite balances, removes the redundant
payments request, and shows separate currency totals with explicit codes.
Malformed legacy currency has a text fallback. No FX, schema, channel or
notification retry policy change; queued records are not sent by these tests.

## Executed verification

| Run | Result |
| --- | --- |
| New controlled TSX |8/8 PASS |
| New API contract cases + retained finance consumers/reconciled balances/invoice status |58/58 PASS |
| Full API |2,058/2,058 PASS, zero skips |
| Full frontend |2,016 PASS /3 FAIL /zero skips or cancellations;74 files,2,019 tests |
| TypeScript |PASS |
| Lint |PASS, zero errors /62 existing warnings |
| Final isolated Chromium |6/6 PASS;2 synthetic POSTs;zero runtime/interceptor errors |
| Configured production export |PASS;89 static generation entries,570 files,87 aliases unchanged |

The three frontend failures are unchanged genuine communication-SQL evidence
checks; Cloud lacks those receipts. They remain failures, not skips or synthetic
passes. Desktop's previous scoped receipt is not copied or fabricated here.

Commands (stdout/stderr and TRX/summary retained in run artifacts):

```sh
node --test QA/tools/fee-reminder-eligibility.test.cjs
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~FeeReminderEligibilityTests
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~FeeReminderEligibilityTests|FullyQualifiedName~FinanceConsumerTests|FullyQualifiedName~ReconciledBalanceTests|FullyQualifiedName~InvoiceLedgerStatusTests'
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --no-build
node QA/tools/run-frontend-tests.cjs
# apps/web working directory:
npm run lint
./node_modules/.bin/tsc --noEmit
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
# repository root:
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/fee-reminder-eligibility-browser.cjs
git diff --check
node QA/tools/check-production-gates.cjs
```

Actual SDK executable `/workspace/.tools/dotnet/dotnet`10.0.403; installed
Next16.4.3/React19.2.8 exact lock. Test commands additionally pass run-specific
TRX logger/results-directory arguments. No restore/install/image download.
InMemory contract tests preserve invoice/payment bytes, exclude foreign and
settled/overpaid/future bulk rows, preserve explicit future-target policy,
verify separate INR200/USD50 notices and confirm queued/not-sent state.
They do not establish SQL translation, HTTP authorization, atomic audit or
concurrency acceptance. Actual Chromium uses intercepted synthetic responses.

The first new build omitted the test API setting and compiled the application
default `http://localhost:5092`. Strict interception refused that origin. Its
browser receipt (4 pass/2 fail,24 intercepted-origin errors) is retained and
not counted as acceptance. The configured loopback rebuild corrects this
without loosening the guard or contacting that default API.

Artifacts excluded from Git: `/workspace/c14-fee-reminders-20261107`, including
original sources, original/final logs and TRX, native receipts, frontend summary,
export manifest and source/assembly/artifact SHA256 manifest. Final native
receipt/configured export and remote publication verification follow below.

Final configured native run uses `NEXT_PUBLIC_API_URL=http://127.0.0.1:49142`
compiled into the export and the same strict API-origin interception. At
320/light and1440/dark, mixed, cancelled-only and settled-only fixtures pass.
Mixed rows show INR200 and USD50 separately and send exactly one synthetic
targeted POST each; ineligible fixtures expose no reminder action. Chromium's
normal sandbox remains enabled. Owned browser profiles are removed and every
one of the570 exported file hashes is unchanged after the browser run.
Both source and test assertions are unchanged between their original/final
qualified runs. The intermediate currency display run failed1/8 because a
bare dollar sign was ambiguous; explicit currency codes repair that case.
All preliminary failures remain in the artifact directory, not relabelled PASS.

## Desktop acceptance and remaining blockers

Inspect/import only the seven C14 paths. C13 at `508c9bb` is a separate return;
this branch includes it as ancestry, but do not overwrite Desktop's handoffs
or import the branch wholesale. Latest Desktop inspected `9269c64` adds real
finance/payroll concurrency evidence without conflicting source edits.

Run owned SQL/authenticated HTTP bulk+targeted cancellation cases, including
mixed case and an active unrelated invoice. Verify no cancelled notice, correct
adjusted/reconciled remaining amounts, exact recipients/currencies, denials,
audits/failure rollback and unchanged money rows. Validate the new UI against
the real invoice projection. Keep notification consent/channel/cooldown/retry
policy acceptance explicit per existing module coverage; this packet does not
authorize actual customer/provider messages. Finish linked critical and device
checks before closing BUG-DATA-0019 or its P0 balance dependencies.

Five P0s, exact candidate CI/images/security checks, missing Cloud SQL receipts,
remaining critical/device and staging/production recovery stay OPEN. Desktop's
new concurrency and prior media/restore results are scoped evidence, not closure
of this new reminder source. Mini/Plus/Pro remain unqualified, Ultra deferred,
model loading stopped. No main merge, Azure or environment/public-network change.
