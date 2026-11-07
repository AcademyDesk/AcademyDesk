# C9 finance batch acceptance — 7 November 2026

Status: SOURCE REPAIR / CONTROLLED FINANCE BATCH COMPLETE; live closure OPEN.
Owner authorized batching all 118 unclosed issue
records; original enterprise tests, closure requirements and source histories
remain authoritative. Base `3f896f63d4b1535585d4fde79576b817832019c7` includes C8;
latest Desktop `746c69f7ef310d8b51ae6c9880cfd55c06b53455` owns live C6 finance
decision and Teacher assigned-batch API/SQL/browser acceptance. Do not overlap.

Exact cloud-owned paths for this batch:

- `apps/web/src/app/payments/page.tsx`
- `apps/web/src/app/invoices/page.tsx`
- `apps/web/src/app/payroll/page.tsx`
- `apps/web/src/app/finance-reconciliation/page.tsx`
- `QA/tools/finance-batch.test.cjs`
- `QA/tools/finance-batch-browser.cjs`
- `QA/tools/payment-balances.test.cjs`
- `QA/tools/invoice-settings.test.cjs`
- `QA/tools/finance-lookups.test.cjs`
- `QA/ISSUES/BUG-DATA-0001.md`
- `QA/ISSUES/BUG-DATA-0002.md`
- `QA/ISSUES/BUG-DATA-0003.md`
- `QA/ISSUES/BUG-DATA-0010.md`
- `QA/ISSUES/BUG-DATA-0016.md`
- `QA/ISSUES/BUG-FUNC-0003.md`
- `QA/HANDOFF/C9_ISSUE_BATCHES_2026-11-07.csv`
- `QA/HANDOFF/C9_FINANCE_BATCH_2026-11-07.md`

Reproduce current source failures before repairs: financial form duplicate/
uncertain saves, pending/stale readback, durable confirmation and distinct-currency
display. Preserve canonical API balances, reconciled/voided collections, required
reconciliation evidence, positive-net payroll and historical saved payslip/print
contracts. Existing three controlled test adapters may gain ref/lifecycle support
only; retain every assertion. Use one final exact-lock build, relevant/full
regressions and a combined native Chromium run with intercepted synthetic APIs.

The batch queue is generated from all current issue records and retains their
unclosed statuses; it is not a new enterprise test plan or a count of newly
confirmed bugs. Original backend/API, SQL harness, session/auth/PENTA, private
materials, finance-governance/Teacher pages, shared pointers and dependencies stay
read-only. Queue real API/SQL/other-client/device and critical acceptance for
Desktop under separate claims. Synthetic browser results never close those gates.

No new package/image/model downloads, npm audit retry, environment/VPN/TCP/privacy
change, candidate model loading/training/inference/activation, main merge, Azure
or customer DB. Mini/Plus/Pro remain unqualified; Ultra deferred. Publish reviewed
feature source and precise results/handoffs; preserve failed and historical proof.

## Returned implementation and scope

Application/tests `4fa371a35be426b9342eb79985ede7d1262c0281`; final native runner
`aa7a42bc41e6e4b5c5da7b3f44ecf3adccea6e17` on
`codex/cloud-c9-finance-batch-20261107`. Nine implementation paths: four pages,
two new QA runners and three existing adapter declarations. No backend/API,
database, dependency, model, shared PENTA or Desktop-active source changed.

Five writes are handled together: recording payment, issuing invoice, creating
payroll profile, recording payout and capturing reconciliation evidence. They
now guard overlapping submits through readback, disable their native form fields
while pending, retain a durable success notice, and avoid state/reset/readback
after unmount. Existing precise payload, captured-form and saved-payslip print
contracts remain. Payroll's positive-net policy now also has a preflight UI guard;
it does not replace or relax the existing backend policy.

Known rejected writes keep their input. Network/5xx uncertainty is never reported
as confirmed failure/success, never clears the draft and cannot submit again from
that page instance. A confirmed write whose read fails reports saved plus refresh
failure. These cases require **Reload financial records** and review; that action
navigates/reloads reads only. It never replays the mutation. This does not provide
server idempotency or another client's concurrency protection.

Four pages now display totals separately by the returned currency rather than
combining unlike values as INR. No exchange rate or backend arithmetic is added.
Single-currency and empty INR controls remain; Reconciliation displays cents.
The original Finance/Platform summary API contracts in BUG-DATA-0016 are outside
this repair and remain pending. Payment consumers still use canonical API balance
and the approved collected-status definition. No restoration/adjustment/tax or
banking policy changes.

## Reproduction and actual final results

| Check | Actual result | Scope |
| --- | --- | --- |
| Original new TSX baseline | 5 PASS / 26 FAIL | 31 assertions on unchanged financial source; notice assertions bound to actual notice elements |
| Corrected unchanged-export native baseline | 28 PASS / 21 FAIL | 49 groups, archived unchanged 570-file export; failures and 13 runtime/interceptor entries retained |
| Final new TSX controls | 31/31 PASS, zero skips | Five writes: pending, uncertainty, durable success, failed refresh, unmount; positive-net and mixed currencies |
| Retained financial regressions | 57/57 PASS | Balances7, invoice settings10, lookups6, form targets23, saved-payslip print11; all assertions retained |
| Final native Chromium | 49/49 PASS | Real DOM, native keyboard/forms/validation and reloads; all API responses synthetic |
| Captured browser mutations | 27 requests | 25 write/recovery groups plus one-cent payout and uncommitted-loss control; zero live financial writes |
| Runtime / sandbox / cleanup | PASS | Zero JS/runtime/interceptor errors, normal Chromium sandbox, exact owned profile removed |
| Full frontend | 1,961 PASS / three FAIL / zero skips | 1,964 tests, 71 files; genuine communication SQL receipts absent in cloud |
| Build / types / lint | PASS | Installed locked Next16.4.3 webpack, 89 generation entries, TypeScript/noEmit; zero errors/63 existing warnings |
| Static export / postbuild | PASS | 570 files; 87 segment aliases unchanged; final export hashes unchanged during browser verification |
| Parse / diff / queue integrity | PASS | Both scripts parse, whitespace clean; exactly118 unique original issues, original status/priority preserved |

Native grouping: 25 save/recovery cases (five writes × five outcomes), 16 Payments
balance/status/adjustment/settled/cents contexts at320/1440 light/dark, four mixed
currency contexts, three payroll net boundaries and one additional uncommitted
transport loss. The five lost-response-after-synthetic-commit cases navigate via
the real recovery button, observe fresh reads and assert no extra mutation.
One-cent positive net is a control, not an approval of zero-net payouts.

The canonical-balance consumer fixtures deliberately assert API authority rather
than reconstructing invoice balance from separately read payment rows. Their
mocked ledger is not a SQL fixture or proof of financial aggregate consistency.
The backend remains responsible for adjustments, money precision, current roles,
tenant boundaries, atomic audit, duplicate periods and transaction contention.

Three old controlled test adapters gained only React useRef support. State
indices and every existing assertion remain; no fixture permissions were widened.
The three remaining full-suite failures require genuine private SQL receipts.
Desktop's source-bound communication/SQL passes cannot be borrowed as cloud
receipts; neither missing evidence nor prior green CI is relabeled as PASS.

## Retained QA preparation failures

The first feedback test draft matched page text; it was corrected to inspect the
actual notice element before recording the unchanged-source baseline. Its initial
log remains. The first native attempt encountered CDP DOM-object serialization in
wait conditions; Boolean predicates corrected the prerequisite without removing
assertions. Both attempts and the corrected 28/21 baseline remain local.

The first repaired native run passed all49 cases but rejected two late CDP
callbacks after its own targets closed. The final runner ignores only the exact
session-gone protocol error after that target's owned-close flag. All errors
during test execution and all other late errors still fail. The rejected receipt
is retained; the accepted run has no error entries. No application assertion or
release gate was disabled. Final screenshot/visual approval is not inferred.

Raw source snapshots, archived baseline, failure/final receipts, installed-lock
provenance inherited unchanged from C8, build/lint/type/test logs and SHA256
manifest stay outside tracked source at `/workspace/c9-finance-batch-20261107`.
Active browser output is excluded under `.build-check/finance-batch-browser`.
No raw laptop/customer evidence or compiled artifact is published.

## All118 execution queue and next parallel work

The [batch CSV](C9_ISSUE_BATCHES_2026-11-07.csv) continues the existing issue files,
not a new audit or test plan. Its snapshot contains5P0,100P1,13P2; no issue is
closed by grouping or these synthetic checks. Many rows have accepted partial
repairs and need remaining acceptance; these are not118 newly confirmed defects.

| Batch | Records | Next owner boundary |
| --- | ---: | --- |
| 01 P0 finance/private materials | 5 | Cloud consumer work here; Desktop real finance/private-host/critical closure |
| 02 Finance/payroll | 13 | Preserve active Desktop C6 decisions; complete disjoint returned consumers and remaining API currency contract |
| 03 Identity/roles/private data | 11 | Source-bound security review and separately claimed real host/tenant acceptance |
| 04 People/portals | 22 | Preserve C6 Teacher ownership; expand remaining real role/workflow cases |
| 05 Academics/scheduling/learning | 34 | Independent cloud batches; C8 music source awaits Desktop live acceptance |
| 06 Platform/sales/communications | 17 | Separate claims, reuse prior repair evidence; actual provider/privacy acceptance |
| 07 Cross-form/navigation/operations | 16 | Group shared mechanisms and existing module regressions; no wholesale UI rewrite |

Prioritized Desktop handoff after its active C6 packet:

1. Integrate the nine implementation paths plus six issue records and report/queue updates
   by reviewed feature source; preserve newer Desktop work. Rebuild its isolated
   exact-lock artifact because four pages changed. Original C8/C7 implementation
   is inherited, not reapplied wholesale; no main merge.
2. Run genuine Identity/HTTP/SQL and actual-browser financial journeys using valid
   owned fixtures: adjusted/reconciled/voided balances, required evidence, positive
   net and duplicate periods; exact saved rows, attributed audits and rollback.
   Check both accepted and committed-but-lost responses, fresh read review and
   independent-client/server races. Synthetic browser commits are not proof.
3. Complete the original P0/security critical/tenant/device closure protocols.
   Link new evidence to the actual final candidate before changing any P0 record
   to CLOSED/PASS. Current cloud results close none of those five release gates.

## Reproduce and release limits

```bash
node QA/tools/finance-batch.test.cjs
node QA/tools/payment-balances.test.cjs
node QA/tools/invoice-settings.test.cjs
node QA/tools/finance-lookups.test.cjs
node QA/tools/remaining-form-targets.test.cjs
node QA/tools/payroll-print.test.cjs
node QA/tools/run-frontend-tests.cjs
cd apps/web
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
./node_modules/.bin/tsc --noEmit
npm run lint
cd ../..
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/finance-batch-browser.cjs
git diff --check
```

`QA_FINANCE_API_ORIGIN` accepts only canonical unprivileged HTTP127.0.0.1origins;
it must match the compiled artifact (Desktop may select54999). Every API path and
mutation method is intercepted/allowlisted; unexpected requests fail the run.
This fixture runner is not a live financial acceptance client. No PENTA/model/
inference request occurred. No environment, public-access, VPN/TCP, privacy or
credential configuration changed; direct laptop TCP grants remain absent.

Exact-current CI, actual production image/full runtime, remaining audits, P0/P1/
roles/privacy/physical-device/restore/staging/release and AI outcome qualification
remain open. Mini/Plus/Pro unqualified, Ultra deferred; model loading/train/
inference/activation stop preserved. No main merge, Azure or customer DB action.
Next: Sol High for real financial/security boundaries; Medium for settled UI QA.
