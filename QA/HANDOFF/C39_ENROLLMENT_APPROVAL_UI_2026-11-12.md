# C39 protected enrollment review,approval and recovery UI — 12 November 2026

OWNER:Cloud Codex,Sol High for binding/recovery;Medium for settled UI/docs.
STATUS:CLOUD VALIDATED;native SQL/device/release acceptance OPEN. BRANCH:codex/cloud-c39-enrollment-approval-ui-20261112.
BASE:e8385f58b23de757928f99481c33ae3dd582b851,clean published C38.
Desktop af4bc7637806de8c5b2834f818beb7fdd10c7245 adjustment browser return
imported with provenance. Desktop owns Manual exact adjustment amount/currency
and C38 native SQL acceptance. These paths remain separate.

Exact MAY MODIFY paths(11):
- apps/web/src/components/penta/enrollment-creation.tsx(new)
- apps/web/src/lib/penta-enrollment-creation.ts(new)
- apps/web/src/components/penta/enrollment-assistant.tsx(bound transport and mount)
- apps/web/src/components/penta/enrollment-assistant.module.css(scoped controls)
- QA/tools/penta-enrollment-assistant.test.cjs(fixtures/hooks only;retain assertions)
- QA/tools/penta-enrollment-assistant-browser.cjs(readiness404 fixture only)
- QA/tools/penta-enrollment-creation.test.cjs(new)
- QA/tools/penta-enrollment-creation-browser.cjs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentCreationWorkflow.Browser.cs(browser DTO UTC serialization only)
- tests/AcademyDesk.Api.Tests/PentaEnrollmentBrowserViewTests.cs(new)
- QA/HANDOFF/C39_ENROLLMENT_APPROVAL_UI_2026-11-12.md

DELIVER:verified host readiness;exact create review→separate explicit approval→
explicit execution→verified receipt;pre-write scoped recovery pointer;GET-only
reload/recovery;no automatic replay/new request after uncertainty;frozen bindings,
blocking overlaps and session-loss/unmount cleanup. Only C38 browser routes.
PreviewOnly remains advisory. Feature remains default-disabled/Testing-only.
Native SQL acceptance is still Desktop-owned/pending;no production activation.

Browser DTO boundary correction:SQL DateTime reloads are Unspecified. C38 draft
views currently pass expiry through without marking UTC(unlike Finance views).
Normalize only new browser DTO expiry/receipt time before JSON;retain UTC ticks,
status/hash/approval/domain fields and all legacy responses. Add explicit tests.

MUST NOT MODIFY:Desktop finance/Manual adjustment/shared draft or SQL harness,
C38 start/session/authority/domain/transaction rules,legacy enrollment controller/
workflow/receipts,permissions/Identity/session policy,root shared handoffs/tracker,
other pages/vendor/Mini,original assertions/evidence. No main/Azure/environment
publish/replace/delete/public access,model download/loading/inference/training/
activation,new dependencies/paid compute or hosted workflow dispatch.

CHECKS:actual protocol/machine/component faults and storage/session binding,
strict schemas and immutable admission/hash/approval/receipt matching,loss before/
after write and GET-only reload,storage failure,malformed/expiry/refusal/pending/
stale/unmount;real Chromium320/1440 synthetic host;retained47 page/93 entry/32
preview checks;UTC JSON regression;full API/frontend/exact-lock web/build/types/
lint/SQL harness compile/release guard;exact Desktop preservation and feature SHA.
STOP:bounded source/results and native handoff pushed/remote-verified.
NEXT:Desktop real SQL/Identity/browser acceptance for C38+C39;Cloud enrollment
lifecycle/status/transfer in a separately claimed packet. Model-loading stop stays.

## Completed source and ownership return

STATUS:IMPLEMENTED / CLOUD VALIDATED;native SQL/model/device/release acceptance OPEN.
Browser UTC correction:`29c12181c8e6ed93921c6c93faa5a87b02f9a108`(two paths),
pushed independently so Desktop could use it during C38 SQL validation.
Frontend/client implementation:`93d69c839c590a43d559e8814eb0051f4d07145b`(eight paths).
Claim1ce308d8cd6651bd7cd6803ae95706e1cc712143 was feature-pushed and exact
remote SHA verified before implementation. All eleven claimed paths accounted for.
Final documentation/publication SHA is verified after push and recorded in private
publication.json/final response;this commit cannot contain its own SHA.

Desktop af4bc7637806de8c5b2834f818beb7fdd10c7245 was imported with provenance
at69feabbf27999c05c873826ee18a5e3096ba7761. During implementation Desktop
published264c1f5c3ba9960c610054369330f3299868b26a,imported with provenance at
1a73afd4dbf153048cbe05acfbfc9d1bd25f055f. All21 distinct imported paths match
Desktop's264c1f5 tree exactly. Desktop completed Manual adjustment amount/currency
and its pinned handoff now assigns C38 native SQL acceptance to Desktop.
No Cloud edit to finance/Manual adjustment/SQL harness/shared root handoffs.

The environment recovered from the previous executor-capabilities failure.
Observed current/running/connected and restricted network enforcement;no settings
changed. No environment publication/replacement/deletion/public-access change,
main merge,Azure deployment,PR/workflow dispatch,package install/paid compute,
model download/loading/inference/training/activation or laptop/TCP/D: access claim.
Mini remains clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8,33 frozen paths
untouched;16 vendor files(including12 upstream pins)are byte-exact.

## Delivered user flow

The existing `/penta/enrollments` assistant retains bounded student/batch commands,
verified selection and PreviewOnly eligibility. A separate creation panel checks
only the existing C38 browser-readiness GET after authenticated session verification.
Absent/disabled/refused readiness leaves Manual available and no creation control.
No feature flag was enabled;C38 remains default-disabled/Testing-only.
A qualified AI model remains disconnected;no interpreter is called here.

For an eligible selection,the user chooses Review exact enrollment. Only then
are a purpose-bound conversation and draft prepared through the C38 browser routes.
The panel displays verified student/batch labels plus authoritative IDs,start date
and Active/Waitlisted intent. Approve exact enrollment is distinct from Create
approved enrollment. Only an exact Authorized review can submit execution.
Creation success requires the complete verified host receipt,enrollment and audit
references,with all admission fields/hash/revision/approval/expiry bindings retained.
No arbitrary tool,legacy conversation,URL,model prose or permission is accepted.

While an operation is open,the parent selection/preview controls are locked. The
operation holds its immutable intent;late callbacks cannot approve/execute a later
review. Preview-only text is hidden during the operation to avoid contradicting
a committed result. Closing clears the old eligibility preview and retires that
candidate;another review requires a fresh selection/eligibility check. Closing a
local review does not delete/cancel a saved server operation or revoke approval.

The shared transport freezes the verified academy,bearer and login epoch,uses
no-store/omitted cookies/no-referrer/error-on-redirect and bounded JSON reads.
Creation JSON additionally requires the no-store response contract.403/401 and
session replacement clear private UI;late/unmounted results cannot repopulate it.
Browser time can prevent an expired action locally;the server independently
rechecks clock,current authority,domain rules,approval and mandatory audit.

## Recovery and no-replay behavior

Only five fields are saved in sessionStorage:academyId,epoch,startRequestId,
requestId and nullable conversationId. No prompt,name,payload,hash,approval or
receipt is stored as browser authority. A matching current scope and valid IDs
are required. Recovery persistence occurs before the next modifying request;
storage failure or another active pointer prevents that step from being sent.
A different saved operation is never overwritten/deleted by executing or closing
the current one. Initialisation and availability rechecks retain an already-open
operation rather than silently switching it to another saved pointer.

Reload and Check enrollment status perform GET only. Lost start uses the same
start key GET,then the same draft GET. Missing prepare does not silently create
one. Lost approval can recover its current Prepared/Authorized state;lost execution
can recover the verified receipt. A recovered Authorized state permits only a new
explicit click on that same approved operation. No autoapproval,autoexecution,
automatic retry or new request after an uncertain outcome. Expired/missing/
malformed/unconfirmed outcomes offer status/Manual reconciliation rather than a
success claim. Done clears the matching local pointer;other pointers remain intact.

## Browser UTC boundary correction

C38's browser draft wrappers now copy expiry and receipt completion time with
UTC Kind before JSON. SQL datetime2 reloads preserve UTC ticks but have Unspecified
Kind,which otherwise serializes without Z. The correction preserves ticks,
request/status/hash/approval/admission/receipt/audit fields and error outcomes.
It changes only new browser response copies;no database write,global serializer,
legacy response,session protection or transaction/domain logic is changed.
The new consumer rejects naive/invalid UTC/calendar values rather than guessing
an authority-sensitive timezone. Desktop must include29c12181 when validating
actual SQL roundtrips for C38/C39.

## Actual validation

Private raw evidence is outside Git at
`/workspace/c39-enrollment-approval-ui-20261112`:initial/final logs,TRX,synthetic
Chromium results/screenshots,source/assembly/export hashes and preservation/
publication receipts. No original assertion,missing receipt or failure was hidden.

| Check | Observed result |
| --- | --- |
| New client/actual child component |55/55 PASS;strict draft/receipt and canonical binding,pointer scope,explicit phases,loss before/after requests,GET-only reload,same-key explicit retry,storage denial/other pointer,expiry/refusal,stale callbacks,pending duplicates,dispose/session loss |
| Retained checks |47/47 assistant and93/93 role-entry PASS;old assertion bodies byte-exact;only parent fixture adds useCallback and a stub for the separately tested child |
| Combined focused run |195/195 PASS;zero skips/cancellations |
| Initial new client run |52/54 PASS;two child fixture UUID mismatches corrected with deterministic injected IDs,not protocol/implementation stubs |
| Additional recheck regression |Original54 PASS/1 FAIL showed stale ready state and pointer selection on recheck;final55/55 PASS after loading-state reset and retention of the open key |
| UTC regression |10/10 PASS;Prepared/Authorized/Succeeded ×UTC/SQL-style Unspecified values plus unchanged error outcomes;actual JSON ends in Z with original ticks/bindings |
| Full combined API |2,665/2,665 PASS;fresh compile/full.trx;10 new UTC cases included with Desktop264c1f5;zero skips |
| Initial and final creation Chromium |52/52 PASS each;Owner/AcademyAdmin ×320/1440 ×13 cases |
| Thirteen native cases |Normal Active,Waitlisted,lost start,lost prepare,lost approval,loss before execution dispatch,loss after commit,storage denial,expiry,403,invalid receipt,session replacement,disabled readiness |
| Native synthetic effects |44 start PUTs,40 prepare PUTs,40 approval POSTs,40 execution attempts and32 simulated committed enrollments across all cases;retry/loss cases enforce one domain effect per operation;these are mock-host counters,not SQL evidence |
| Retained preview Chromium |32/32 PASS initially and finally;only fixture addition is readiness404;assertions unchanged;preview remains usable with creation disabled |
| Native resources/presentation |Both suites sandbox enabled,owned profiles removed,no horizontal overflow;Prism/Orbit canvas values verified;no model/native SQL/physical-device claim |
| Exact-lock production export |PASS,cached Next16.4.3/React19.2.8 webpack build;88 aliases/578 files;no install |
| Types/lint |PASS;0 lint errors/61 retained warnings |
| Initial combined frontend |95 files;2,608 tests:2,605 PASS/3 inherited receipt FAIL |
| Final combined frontend |95 files;2,609 tests:2,606 PASS/3 inherited receipt FAIL;zero skips/cancellations;includes Desktop's additional finance display controls |
| Only frontend failures |communication-drafts45/48;Cloud lacks original QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log;no fabricated substitute |
| SQL harness compilation |PASS,0 errors/0 warnings;no restore and BuildProjectReferences=false;no SQL execution |
| Release guard |15/15 controls PASS;actual gate exit1 preserves five P0 records plus mandatory broader session-security acceptance |
| Preservation |21 Desktop paths,6 legacy/C38 host files,16 vendor paths,clean Mini and original47/32 assertion bodies verified |
| Hosted CI |Current status unverified;latest actual C31 Actions query was Forbidden;no retry/bypass/dispatch;feature push excluded by verify.yml filters |

Commands:node --test QA/tools/penta-enrollment-creation.test.cjs
QA/tools/penta-enrollment-assistant.test.cjs QA/tools/penta-entry.test.cjs;
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node
QA/tools/penta-enrollment-creation-browser.cjs and penta-enrollment-assistant-browser.cjs;
node QA/tools/run-frontend-tests.cjs;tsc --noEmit;npm run lint;
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack;
dotnet test --no-restore -p:UseSharedCompilation=false with utc.trx/full.trx;
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore
-p:BuildProjectReferences=false -p:UseSharedCompilation=false;
node --test QA/tools/production-gates.test.cjs;
node QA/tools/check-production-gates.cjs;git diff --check.

## Native acceptance / AI-first continuation

1. Desktop Sol High:integrate29c12181 UTC correction and93d69c83 UI plus this
   handoff selectively,preserving newer Manual/finance work. Complete C38 native
   start/readiness/purpose/authority/quota/audit rollback/races with the actual
   combined source. Unit JSON and InMemory refusals do not establish that result.
2. Exercise C39 against real authenticated SQL-backed Owner/Admin browser:verified
   student/batch lookup and admission,canonical review,separate exact approval,
   explicit create,one enrollment/audit/receipt and current data after reload.
   Include Active/Waitlisted,capacity/prerequisite/duplicate/stale intent changes,
   required-audit failure,revoked/current actor/tenant/module/maintenance and expiry.
3. Drop requests before dispatch and responses after commit for start/prepare/
   approval/execution. Reload must issue GET only;recover the same saved operation.
   Verify explicit same-approved retry creates no second enrollment/audit. Check
   storage blocked/full/changed-pointer,other-tab/logout/epoch/session replacement,
   late responses,real devices/other engines and no private prompt/approval storage.
4. Cloud next:separately claim enrollment status/transfer browser contracts and UI,
   with exact lifecycle reasons and current source/target binding. C38/C39 supports
   create only;do not broaden it implicitly. Then continue remaining protected
   scheduling/makeup/attendance UI under their own current domain policies.
5. Five P0 issue records plus AUTH-REVOCATION001 remain OPEN. Current CI/security/
   images/runtime,real SQL/critical/device workflows,migration/backup/restore/
   rollback and staging acceptance remain. Broader Manual acceptance is deferred
   in implementation priority,not waived. Source fixes/test totals are not closure.

Mini/Plus/Pro remain unqualified;Ultra deferred.90% eligible conversational outcomes
remain unmeasured. Model loading/download/inference/training/activation remains
stopped. No production qualification,main merge or Azure deployment is claimed.
