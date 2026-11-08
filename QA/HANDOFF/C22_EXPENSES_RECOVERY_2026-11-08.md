# C22 Expenses currency and submission recovery — 8 November 2026

Cloud owner. IMPLEMENTED / CLOUD VALIDATED; combined real-host acceptance pending.
Branch `codex/cloud-c22-expenses-recovery-20261108`.
Clean base `5f4eccba0fee010862289993e5ad5d824d268e18` (published C21).
Latest Desktop `88a3a45218ced1e4995d83fdcc1d4574cffb29c0`: C20 integrated;
2,183 combined API passes and real Reports/payment-transition evidence. Desktop
owns the reproduced legacy currency write repair and protected SQL/P0 work.
No active competing Expenses page claim found. C21 is a separate required return.

Exact paths (expanded lookup claim before backend changes):

- `apps/api/Controllers/ExpensesController.cs` — add read-only branch-options and its id/name DTO; preserve Create/Update
- `tests/AcademyDesk.Api.Tests/ExpenseBranchOptionTests.cs`
- `apps/web/src/app/expenses/page.tsx`
- `QA/tools/expenses-recovery.test.cjs`
- `QA/tools/expenses-recovery-browser.cjs`
- `QA/ISSUES/BUG-DATA-0016.md` — preserve local SQL and previous currency evidence
- `QA/HANDOFF/C22_EXPENSES_RECOVERY_2026-11-08.md`

Group recorded-expense and TeacherPayout totals by actual unit; keep cents and
safe missing/legacy-currency labels using C20's existing pure formatter. Preserve
all-recorded-expense scope, filtering, branch/date/category controls and the
existing explicit INR creation payload. Do not introduce a new expense lifecycle
or payment/approval/worker attribution policy.

Initial required read/loading/denial must not publish ready zero totals or an
editable form without a complete register/branch lookup. Keep read-only retry
and sign-in/account guidance without developer host/port details. Replace the existing branch-management lookup dependency with a minimal Finance-
scoped GET branch-options, inheriting the existing controller permission/module/
tenant gate. Do not grant branch management or change any write authority.

Guard overlapping/stale submits synchronously, preserve definitive-rejection
input, distinguish acknowledged save from failed readback, and lock any unknown
transport/5xx result against replay. Reload financial records performs reads,
not a POST retry. An unconfirmed submission retains its draft and requires a
fresh read plus explicit Start a new expense before clearing that draft and
unlocking another write. Acknowledged saves clear inputs once; failed refresh
stays explicitly saved and blocked until a successful read. Ignore late reads/
saves after unmount and preserve every backend/audit/transaction/SQL path.

Before/after actual TSX handlers, controlled Chromium mixed/empty/legacy/error/
read-only retry, real-browser save/rejection/lost-response/failed-readback/double-
submit cases with clearly synthetic persistence, build/types/lint and full
frontend. The original frontend-only scope initially verified reuse of2,177 API passes.
The new read-only lookup requires targeted actual Identity/HTTP/InMemory and a
fresh full API run; previous source-reuse receipt is historical, not final proof. Actual expense Identity/HTTP/SQL/
audit/race/device/all-role/branch dependency acceptance remains Desktop-owned.
Keep five P0s/BUG-DATA-0016 OPEN, Mini/Plus/Pro unqualified, Ultra deferred and
stop before model loading. No main/Azure/environment/privacy/public access,
new dependencies/images, customer/provider/real-laptop writes or raw evidence
publication. Sol High for financial outcome/recovery; Medium for settled docs.

## Expanded minimal-lookup claim

Source review found the existing GET branches dependency uses MultiBranch and
has no FinanceUser controller mapping. An otherwise permitted Finance expense
user can be denied branch options. Before editing, Cloud adds only the read-
only Expenses branch-options action/test paths above to this packet; no active
competing Expenses-controller source claim exists in the latest Desktop handoff.
Desktop retains all protected finance write repair, SQL and migration paths.
The option JSON must expose only id/name, scope by academy and order stably.
Normal FinanceUsers with Finance (without MultiBranch) must be able to use it;
foreign/anonymous/unprivileged/missing-Finance requests remain denied. Reads
must not change branch, academy or audit data. Create/Update/DTO bytes remain.

## Implementation and compatibility

Summary totals and TeacherPayout subtotals now group the existing recorded
expense rows by their own unit; individual rows preserve cents. Missing or
nonfinite metadata is visibly unavailable; invalid legacy currency strings have
a labelled fallback. Empty successful history is unitless0.00. No FX, payroll
settlement, new approval/payment status or historical-row rewrite is invented.
New expense creation still sends explicit INR, existing categories and nullable
branch/date fields. The category filter changes rows, not whole-register totals.

Initial loading/failed/incomplete reads hide ready monetary indicators and
editable intake. Reload is explicit/read-only;401/403/429 have appropriate
sign-in/access/wait guidance. JSON/root-shape/network errors cannot become an
empty register. Developer host/port instructions are removed. Late reads and
post responses cannot write component state after unmount/replacement.

A synchronous pending ref prevents overlapping submits before React renders.
A draft version rejects stale handlers after success or explicit new-entry reset.
Definitive4xx rejection retains all inputs for correction. Acknowledged save
clears inputs once and refreshes the register; failed readback stays explicitly
saved and blocks new entries until a successful read. Transport/5xx uncertainty
retains the draft and locks repeat submission. Reload reads the latest register
without replay; an unconfirmed draft remains disabled until the user explicitly
chooses Start a new expense, which clears it. No match-by-description heuristic
pretends a particular server row proves success. Draft retention is in page
memory, not a new localStorage/server draft feature. These are page-level intent
guards, not backend idempotency or distributed duplicate detection.

### Finance branch options

`GET /api/academies/{academyId}/expenses/branch-options` returns only `id` and
`name`, tenant-scoped and ordered by name/ID. It inherits Expenses' existing
finance.manage/Finance and current stored account/role/owner/tenant checks.
FinanceUser/Owner/AcademyAdmin can use it without MultiBranch. It does not grant
branch management or return addresses, city, state, postal or lifecycle metadata.
Like the old choice list, it retains all existing owned branches; no new active-
branch policy is invented. The client no longer calls GET branches for options.

Actual HTTP tests reproduce the old branch-management403 under Finance-only
academies, then verify the new scoped endpoint200. Existing Expense Create/
Update methods and old DTO bytes were compared against the base and remain
identical; only the read-only action and minimal record are added. No role,
module, transaction, required audit, SQL schema or migration changes.

## Executed evidence

| Check | Actual result |
| --- | --- |
| Original Expenses45 cases |6 PASS /39 FAIL; zero skips |
| Final shared46-case suite against original page |6 PASS /40 FAIL; zero skips |
| Before adding the option endpoint |13 HTTP cases FAIL with absent-route responses; zero skips |
| Final API target |41/41 PASS;13 new lookup +28 retained expense/import/finance-option controls |
| Final frontend target |78/78 PASS;46 Expenses +32 unchanged Reports cases |
| Full API |2,190/2,190 PASS; zero skips |
| Full frontend |2,197 PASS /3 FAIL;2,200 tests across81 files; zero skips/cancellations |
| Final Chromium |24/24 PASS;12 intercepted POST attempts,8 synthetic commits; zero live API writes |
| Build/types/lint |PASS; zero lint errors /61 warnings |
| Export integrity |570 file SHA256 values unchanged after final native run |
| Production guard |BLOCKED as required by five open P0s |

The13 new API cases use actual MVC/Identity login/tokens with isolated InMemory
stores, not configured SQL. They cover allowed FinanceUser/Owner/Admin without
MultiBranch, minimal JSON and stable ordering, foreign/anonymous and four
unprivileged-role denials, missing Finance module, inactive academy, empty data,
stored owner flag/revocation and no branch-management permission escalation.
Branch/academy/audit snapshots stay unchanged. Restored controls include the
existing expense write integrity tests; none was weakened or skipped.

The46 actual-TSX cases cover mixed/USD/empty/missing/invalid/nonfinite money,
read statuses and JSON/shape/transport errors, empty academy, retry/filter/
branch/date/payload behavior, invalid preflight, same-render double submit and
stale handlers, six definitive rejection statuses, acknowledged-save/readback
failure, sent/unsent/5xx uncertainty, explicit new-entry reset and unmounts.
The new endpoint call is asserted without branch-management reads. The original
45-case log remains; after adding the endpoint test the same46-case suite was
rerun on the unchanged original page. Both baselines are retained.

Native at320/light and1440/dark covers mixed/empty/legacy/missing currency,
initial read retry/denial, pending-double-submit success, validation rejection,
lost response after synthetic commit, failure before synthetic commit, ambiguous
5xx with synthetic commit, and acknowledged save with failed refresh. It reads
the real controlled inputs, dispatches two submit events, checks fieldset/aria
busy, retains exact draft values, inspects persisted fixture rows, and proves
read-only reload/new-entry reset never sends another POST. Twelve attempts and
eight commits are exclusively intercepted in-memory fixture state. No request
reaches a real financial service or SQL database. Normal Chromium sandbox,
owned-profile removal and zero runtime/interceptor errors verified. Final24
cases specifically use the new Finance option path. The earlier24-case run
against the old lookup is separately retained, not substituted for final proof.

The first lint run caught synchronous resets in an effect; resets were moved
to the explicit reload event. No lint rule was suppressed. Its failure and all
intermediate target/build/native results remain recorded. The branch-lookup
scope expansion then required another exact-source build/full API/frontend and
native run; final counts above refer to that final source. The initial534-path
API-reuse receipt is historical and explicitly superseded by the new API tests.
The only full frontend failures remain genuine communication SQL receipts absent
in Cloud (Email/WhatsApp/Meeting); no fake receipt or passing skip was added.

.NET10.0.403; Next16.4.3/React19.2.8 exact lock,89 entries/87 unchanged RSC aliases.
Loopback49142 is test configuration, not production. No package/image download,
restore, model loading or outbound private-dependency audit request added.
Original/final source, unchanged tests, TRX, per-file frontend logs, both native
runs/screenshots, write-preservation proof and source/assembly/export/artifact/
publication SHA256 manifest remain outside git at
`/workspace/c22-expenses-recovery-20261108`.

## Latest Desktop return and remaining acceptance

Desktop `c64ef51c6b4d86bf384230a2c171cba9e2fece74` integrated C21 and explicitly retains Cloud ownership
of C22. Its protected writer repair now uses a matching-currency paid sum under
the existing lock. Legacy SQL moved3/11 to11/11; forty retained finance checks
and2,183 combined API tests passed. Actual Payments accepted USD70.15 with one
new payment/audit after rejecting USD70.16; legacy INR rows stayed unchanged.
C21's60 targeted checks passed locally; real-host optional-feed acceptance is
still pending. [Exact local repair and source scope](https://github.com/AcademyDesk/AcademyDesk/blob/c64ef51c6b4d86bf384230a2c171cba9e2fece74/QA/REPORTS/LOCAL_LEGACY_CURRENCY_WRITES_2026-11-08.md).

Apply ONLY these seven C22 paths to the newer Desktop candidate. Do not adopt
this older whole Cloud branch/backend or overwrite its current writer fixes,
shared InvoiceWriteScope, SQL harness, issue history or governance. The Cloud
2,190 result is not a result for the later combined candidate; run its full
API suite after import. Reconcile issue paragraphs, not whole newer records.

Priority local acceptance:

1. Real FinanceUser/Owner/Admin login and Finance-only academy: expense ledger
   and minimal options succeed without branch-management permission. Confirm
   same/foreign/missing/empty branch scope, raw JSON privacy, current account/
   owner flag/module denials, no GET-side audit, SQL query translation and the
   actual unchanged create/update branch validation. Preserve legacy history.
2. Real expense POST/readback with attributed audit: exact INR amount, branch/
   nullable date/category; successful once, overlapping submits, rejection,
   genuine response loss before/after commit, required-audit failure, readback
   failure and explicit read/new-entry recovery, checking fresh SQL/HTTP/UI.
3. Actual currency/history/filter and physical-device/engine acceptance, C21
   real-host optional reads, family/reminder readers and wider financial/P0
   critical program. Keep transaction/race/restoration safeguards. Page guards
   do not qualify the backend's concurrency, audit or idempotency semantics.
4. Five P0s and BUG-DATA-0016 remain OPEN; exact CI/images/security/staging
   backup/restore/rollback and all-role/AI qualification remain. Remote CI is
   UNKNOWN: feature pushes do not match verify.yml and no dispatch was made.

Next route: Sol High for SQL/authority/combined integration; Medium for settled
imports/docs. Mini/Plus/Pro unqualified; Ultra deferred; stop before model
loading/training/inference/activation. No main merge, Azure/model/environment
publication, privacy/public-internet changes, customer/provider sends or raw
QA/EVIDENCE publication.
