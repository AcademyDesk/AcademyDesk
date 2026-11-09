# C24 invoice document and required-read recovery — 9 November 2026

Cloud owner. IMPLEMENTED / CLOUD VALIDATED; combined real-host acceptance pending.
Branch `codex/cloud-c24-invoice-document-readback-20261109`.
Clean base `5fbc75f93c0dc9f0e18bb59a9da4a68811708e9c` (completed C23).
Latest fetched Desktop `9c9658132446124b70c53467e940d9769cf97d44` is inherited;
its protected writes, production guard, SQL and release-container work stay intact.
No newer remote claim or overlapping invoice-page claim found in current notes.

MODEL: Sol High for money/readback/recovery; Medium for settled docs.
TOOL: Codex / managed Cloud.
TASK: fix the administrator invoice preview/print consumer of canonical API
balance, retain billed/approved-adjustment/accepted-collection history and
cancellation meaning, and prevent failed/incomplete initial reads from presenting
ready zero financial totals or an editable invoice intake. Preserve invoice
creation payload, permissions, module/tenant gates, tax/theme/branding/contact,
and existing financial pending/uncertain-save controls.

Exact owned mutation paths:

- `apps/web/src/app/invoices/page.tsx`
- `QA/tools/invoice-document.test.cjs`
- `QA/tools/invoice-document-browser.cjs`
- `QA/tools/finance-lookups.test.cjs` — correct existing document-settings fixture root only; keep all assertions
- `QA/ISSUES/BUG-DATA-0001.md` — append scoped consumer result, retain local evidence
- `QA/ISSUES/BUG-DATA-0002.md` — append, no closure or guard change
- `QA/ISSUES/BUG-DATA-0016.md` — append missing/legacy money metadata result
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json` — regenerate derived snapshot
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md` — regenerate derived snapshot
- `QA/HANDOFF/C24_INVOICE_DOCUMENT_READBACK_2026-11-09.md`

The current page ignores InvoiceSummary.balance and prints totalAmount as Total
payable. Canonical balance must be used directly; missing/nonfinite balance must
be unavailable, never guessed from stale status or arithmetic in the browser.
Cancelled documents retain history and clearly state zero collectible amount.
Existing total billed scope remains face-value history. Currency metadata must
retain valid units/cents; empty/missing/invalid input must not become guessed INR,
crash the page or label a failed read as a genuine empty register.

Validate before/after actual TSX, retained invoice settings/Finance lookups and
financial transitions, exact-lock build/types/lint/full frontend and actual
sandboxed Chromium at mobile/desktop with synthetic APIs. Include print media/
PDF bytes and all four configured themes, no live financial writes. Reuse C23's
full API result only if every relevant application/test/assembly hash is unchanged;
otherwise rebuild and retest. Actual Identity/SQL/provider/device/critical/release
acceptance stays Desktop-owned. Keep all five P0s and broad-session gate OPEN.
Do not alter API/SQL/migrations, shared CSS/vendor files, Desktop claims, model
loading stop, environment privacy/networking or main/Azure. Mini/Plus/Pro remain
unqualified; Ultra deferred. No new dependencies, raw evidence publication or
external audit. Publish source-bound feature commits and an exact Desktop return.

Before changing the retained lookup fixture, its path is added above: it returns
an array for the document-settings URL although the actual endpoint returns an
object. The new required-read shape validation correctly refuses that array.
Return the existing empty default settings object for that specific URL; no
expected permission, contact, lookup or state assertion is weakened.

Actual PDF validation reproduced an additional document defect: all ten A4
exports had an unwanted blank second page, despite correct visible invoice
content on page one. Add print-only styles inside the already-owned invoice
page to remove surrounding workspace layout from print pagination. Do not edit
shared CSS or weaken the single-page/content/no-intake assertions. Screen themes
and all unrelated pages must remain unchanged.

Screenshot review then exposed the shared 600px mobile table minimum clipping
the invoice billed row at320px. Extend the same page-owned style block with
screen-only narrow invoice-table/theme spacing overrides, and strengthen native
checks to require document/table bounds to fit. Shared CSS and the A4 print
rules stay unchanged. This is a scoped consumer fix, not physical-device closure.

## Implemented scope and original result

The original25 actual-TSX cases had1 PASS/24 FAIL. The repaired25 plus47 retained
invoice settings, Finance-only lookup and financial-save cases now pass72/72.
The existing fixture correction is one URL-to-object branch only; no assertion
or genuine evidence case is weakened. Original source and failing TAP are kept.

Preview/print consumes the server balance directly, retains billed face value,
approved adjustments and accepted collections, and distinguishes cancelled
history from zero Amount due. Missing/nonfinite/negative balance is unavailable;
no browser balance arithmetic or Paid-status zero guess is used. Existing valid
currency formatting stays, with cents, normalized unit groups, safe invalid-unit
fallback and honest missing metadata. Genuine empty history is unitless0.00.

The required academy/student-options/fee-plans/invoices/document-settings reads
must all succeed with valid response roots before showing ready register/KPIs or
enabling creation. Loading, denial, invalid roots, JSON and transport failure
stay unavailable; retry performs reads only and fetches current academy context.
The retained synchronous save/uncertainty guard also checks completed reads.
Post-save refresh hides stale indicators until readback; unknown outcomes retain
the existing blocked review and explicit reload flow. Late results cannot mutate
an unmounted page. Invoice creation payload, themes, billing contact, tax and
branding, server permissions and every protected writer remain unchanged.

Initial type/build verification caught a response-array/tuple typing error; an
intermediate lint pass also caught synchronous effect-driven state updates.
Both were corrected without suppressions. Initial state updates now follow the
actual API response callback; redundant unmount-version mutation was removed,
with the existing alive guard and per-load version retained. Final build/types/
lint and complete suites are being recorded against the final source.

Full API2,235/2,235 is reused from the C23 final run, not rerun: all540 API source,
API-test and assembly SHA256 identities match its retained manifest exactly.
These were HTTP/Identity/EF InMemory checks, not new SQL or release-image proof.
C23, including newer Desktop writes and the six required release guard records,
is preserved. Current hosted CI remains UNKNOWN; feature push alone does not
trigger the inherited verify.yml branch filter.

Raw source/TAP, full frontend, build/types/lint, original/final PDF bytes and
browser receipts/hashes stay outside Git at
`/workspace/c24-invoice-document-readback-20261109`. No QA/EVIDENCE is published.
The initial native run passed14/24 and failed all ten print groups because of a
blank second page. Its actual PDFs/text and normal owned-profile cleanup are
retained separately. Final native rerun must pass the unchanged content,
single-A4-page, exact theme and no-intake/print-action assertions.

## Prioritized acceptance and return owners

1. Desktop first integrates the C23 family/reminder return and this bounded C24
   invoice consumer. Retain its newer financial writers and real SQL/audit/race
   evidence; do not import old controllers from a prior Cloud branch wholesale.
2. With real current Identity/SQL, open the same partially paid/adjusted invoice
   in Payments, invoice register/preview/print, family details/HTML and authorized
   reminder queue. Verify matching currency/academy/accepted receipts and exact
   canonical balance. Use100.55 billed,0.05 adjustment,30.35 accepted collections
   and70.15 due, plus wrong-unit/foreign receipts, reconciled/voided states, fully
   adjusted/paid, cancelled and one-cent controls. Read-side captures must show
   no ledger/audit changes; actual reminder queue must keep its own existing audit.
3. Desktop validates fresh/revoked FinanceUser and delegated/tenant/guardian
   permissions, document settings and actual save/failed-readback/no-replay
   behavior. The Cloud browser fixture uses a synthetic FinanceUser session and
   does not establish real server authorization or a persisted new invoice.
4. Complete the remaining five P0 critical/device cases and broad-session
   SECURITY-SESSION-001 separately. C24 adds invoice paper/PDF coverage, not
   payroll, financial-write concurrency, Blob/device capture or session closure.
   Check all four invoice themes with long real academy/contact/name records on
   actual phone/tablet/printer clients. Cloud viewport/PDF tests are scoped evidence.
5. The release integrator still needs exact hosted CI/unmodified image/security
   and staging backup/restore/rollback acceptance. Keep120 issue records traced
   through original/cause/fix/regression/module/critical/documented/device gates.
   Five P0 records plus the P1 broad-session record remain production-blocking.
   Do not mark a gate closed from this packet or overwrite historical/local proof.
6. Mini/Plus/Pro are unqualified, Ultra deferred, and the90% eligible conversation
   target is unaccepted. The stop before model loading/training/inference/activation
   remains. Main, Azure, environment settings, private access and laptop data are
   untouched. Next route is Desktop combined SQL/device acceptance, Cloud bounded
   remaining source repairs; Sol High for consequential authority/money work.

## Commands and evidence boundaries

- `node --test QA/tools/invoice-document.test.cjs` against the original page,
  then with `QA/tools/invoice-settings.test.cjs QA/tools/finance-lookups.test.cjs
  QA/tools/finance-batch.test.cjs` against the repaired actual TSX.
- `./node_modules/.bin/tsc --noEmit`, `npm run lint`, and
  `NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack` from web.
- `node QA/tools/run-frontend-tests.cjs`; preserve genuine receipt failures.
- `QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/invoice-document-browser.cjs`
  with real print-media CSS, Page.printToPDF, pdftotext content and pdfinfo page
  count. All external API requests must be intercepted synthetic loopback GETs.
- `node --test QA/tools/production-gates.test.cjs`; actual
  `node QA/tools/check-production-gates.cjs` must still exit1 for open gates.
- `node QA/tools/priority-issue-status.cjs --write`; verify deterministic output
  and all120 unique IDs with zero missing/duplicate/unknown-priority errors.

The all-priority generator retains its original inventory seed base, current
issue hashes/appendices and inherited Desktop9c provenance; it does not declare
that seed or any later source qualified. The current C24 source pin belongs to
this packet's publication record, not to an inferred CLOSED/PASS status.

## Final verification and concurrent Desktop return

| Check | Actual result / limit |
| --- | --- |
| Original actual TSX |25 cases:1 PASS/24 FAIL; original source/TAP retained |
| Final targeted actual TSX |72/72 PASS:25 new +47 retained settings/lookup/financial controls |
| Final complete frontend |2,241 PASS/3 FAIL/0 skip/cancel;2,244 tests in83 files |
| Three retained failures |Genuine phase-2b-communication-roundtrip-sql.log absent in Cloud; no assertion/receipt fabricated |
| Backend reuse |C23 full2,235 PASS;540 API/test/assembly hashes identical, no new run |
| Final types/build/lint |tsc PASS; Next16.4.3/React19.2.8 webpack export PASS;0 lint errors/61 existing warnings |
| Static alias/export preservation |87 aliases unchanged;570 export hashes unchanged across final testing |
| Final native Chromium |24/24 PASS;320/light +1440/dark, strict mobile document/table bounds, missing/invalid metadata, denial/pending/503/retry/mixed/empty cases |
| Real PDF exports |10/10 single-page A4 PDFs:four themes plus cancellation at both widths; expected amounts/history/identity and no intake/actions in extracted text |
| API effects |202 strictly intercepted GETs;zero API mutations;normal sandbox;owned browser profile removed |
| Release guard |15/15 assertions PASS;actual guard exit1 for five P0s +broad-session record |
| Issue inventory |120 unique records:5 P0/102 P1/13 P2;zero input errors or inferred closures;deterministic regeneration |

The first functional/PDF rerun after pagination repair passed24/24 and all ten
PDFs. Screenshot review then found mobile billed-row clipping, and stricter
geometry reproduced ten mobile failures (600px shared table minimum). The final
screen-only overrides fix widths/wrapping and Modern/Minimal spacing; all strict
bounds now pass. Original print and geometry failures remain in separate raw
receipts. No PDF/content/no-write/bounds assertion was removed or loosened.
Screen styles do not apply to print and shared CSS is byte-identical. This is
viewport/PDF coverage, not actual phone/tablet or printer acceptance.

Desktop advanced during validation to
`069e318012deea96da5415518af1bbd07a124b6c`. Its five-path update was imported
exactly with cherry-pick provenance at `fc3ec32`:15 live Edge/SQL cross-tab
checks, retained77 logout/refresh controls, browser helper and evidence notes.
Its tested application source was9c965813/C22, not C23/C24. The broader session
gate remains open for role/device/live-overlapping-refresh/critical/release cases.
No C24-owned production path overlapped. All five Desktop files remain exact;
its new helper is not a frontend-test input or dependency. Application/API/test/
invoice browser/export bytes from the final C24 run remain unchanged by import.

This packet's ten paths are the C24 delta against5fbc75f, excluding that exact
five-path Desktop inheritance. Desktop should retain its latest evidence and
import C23 first, then the bounded C24 return. Three issue appends preserve its
older SQL history; do not overwrite whole newer local issue files on import.
C24's full source-bound raw hashes and publication manifest remain outside Git.
