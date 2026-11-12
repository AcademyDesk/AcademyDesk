# C41 enrollment lifecycle UI — 12 November 2026

OWNER:Cloud Codex. MODEL:Sol High for session/approval contracts. TOOL:Codex.
REPOSITORY:AcademyDesk/AcademyDesk. ENVIRONMENT:managed Linux cloud.
BRANCH:codex/cloud-c41-enrollment-lifecycle-ui-20261112.
BASE:27291e211fb2753a321c8715c2aefce946d63858 (published C40).
STATUS:CLOUD VALIDATED;live SQL/browser and model qualification OPEN. Latest Desktop read-only tip83452a129c1a7315922dbeeaf1b50de3d62cbdac.
Desktop owns live C39/C40 SQL/browser qualification. Shared trackers stay untouched.

TASK:separate /penta/enrollments/manage screen for bounded verified source lookup,
status/transfer exact canonical review,tool-specific approval,explicit execution,
verified receipt and GET-only same-request recovery. Separate lifecycle purpose
and five-key scoped recovery pointer; no C39 create-session reuse or model calls.
Target batches come only from current protected batch-search results.

MAY MODIFY (exact7 paths):
- apps/web/src/lib/penta-enrollment-lifecycle.ts (new)
- apps/web/src/components/penta/enrollment-lifecycle.tsx (new)
- apps/web/src/app/penta/enrollments/manage/page.tsx (new)
- apps/web/src/components/penta/enrollment-assistant.tsx (navigation link only)
- QA/tools/penta-enrollment-lifecycle.test.cjs (new)
- QA/tools/penta-enrollment-lifecycle-browser.cjs (new)
- QA/HANDOFF/C41_ENROLLMENT_LIFECYCLE_UI_2026-11-12.md (this handoff)

MUST NOT MODIFY:API/SQL/migrations/domain/security/permissions,Desktop finance or
native work,root shared trackers/handoffs,C39 creation/lib/component/UTC fix,
existing assertions/raw evidence,Mini/vendor/lockfiles/dependencies/environment.
No main merge,Azure deployment,environment publication/replacement/deletion,
public internet enablement,workflow/PR dispatch,model loading/download/inference/
training/activation. Mini/Plus/Pro unqualified;Ultra deferred;Manual retained.

VALIDATE:strict runtime parsing/source and target binding/per-tool intent,
receipt consistency,read-only interrupted recovery,stale callback/session/storage
protection;actual native Chromium desktop/mobile synthetic protocol checks,
TypeScript/lint/exact-lock web build/frontend regression;retain genuine missing
SQL receipt failures and all release blockers. Source-bound backend C40 reused.
STOP WHEN:bounded source and sanitized handoff feature-pushed;remote SHA verified.
NEXT ROUTE:Desktop Sol High for native SQL/live browser acceptance;Cloud follow-up
from remaining outcome backlog,without activating models. Production NOT QUALIFIED.


## Completed return

Implementation:`34081365ba27453723c37b61a787db51a9f4ca84` (six source/test paths).
Claim:`91732f5fac410de8bfdba3f676c2b81829724a4d`,published and exact remote SHA
verified BEFORE implementation. Final publication SHA is verified after push
and recorded in the private publication receipt and final response.
All seven claimed paths are accounted for. No shared Desktop file was imported.
The final read-only Desktop ref check still returned
`83452a129c1a7315922dbeeaf1b50de3d62cbdac`.

The managed executor is current and connected with restricted network policy
enforced. No environment,privacy,VPN,TCP or credential setting was changed;
no laptop/D: connectivity or native SQL availability is claimed by this packet.
The runtime skill was used for the process/build/browser execution boundary.
No main merge,Azure deployment,environment publication,replacement/deletion,
public internet enablement,PR/workflow dispatch,dependency/model downloads,
paid compute,model loading,inference,training or activation occurred.

## Delivered behavior

New route:`/penta/enrollments/manage`,linked from the existing enrollment
assistant by one navigation-only replacement. All other C39 lookup,preview,
creation/session/approval/recovery code and styles are byte-exact.
The new page is a guided protected-tool screen,not a model interpreter. It
explicitly says a qualified model is not connected. The deterministic Academy
Desk domain host remains the authority;the new screen does not register tools
or activate the default-disabled Testing-only C40 endpoint.

The page verifies the current AcademyAdmin workspace session and supported real
Owner/AcademyAdmin pilot role,academy and non-platform-owner account. Requests
use a captured bearer and session epoch,AbortSignal,no-store fetch,omitted
cookies,no redirects and no-referrer. Same-tab session/signout events,storage
signout/replacement and focus changes retire the transport and clear rendered
operation data. In-flight late responses cannot restore obsolete state. Current
host401/403 removes the screen's scoped data. Server authority is still rechecked
by C40;these client checks never grant access.

Availability requires the exact lifecycle-browser.v1 protocol,approval=true and
the two supported Update/Transfer tools. Bounded source lookup accepts2–80
literal normalized characters. Responses require the current academy,exact
protocol tool/capability,maxRows10,UTC timestamp,boolean hasMore,no more than10
unique sources and the exact nine-field minimal source row. Foreign links,
unexpected contact data,malformed dates/names/status/ids and duplicate sources
are refused. Historical Transferred labels can be displayed without implying
transfer eligibility. Target batches come only from existing protected current
academy batch-search;an arbitrary pasted batch identifier is never a selection.

A source must be an object from the latest verified source result,not a stale
callback or copied arbitrary object. Transfer also requires a choice from the
latest verified target list,a different target batch,Active source and explicit
transfer date. Update supports Active/Waitlisted/Completed/Withdrawn/Cancelled/
Paused,an optional explicit end date and the existing reason requirement for
non-Active status. A blank end date explicitly clears it. The server retains
all capacity,duplicate,prerequisite and lifecycle/domain checks.

Review,approval and execution are separate gestures. Preparation sends only the
existing typed Update/Transfer envelope through the new lifecycle purpose.
The returned change must exactly match source enrollment,student,source batch,
current and requested status,end date,reason,target and transfer date. The
canonical review displays the host fields and source/target identifiers. It
cannot silently accept a changed source or target. Approval binds revision1,
context0 and frozen lowercase payload hash with the matching
ApproveEnrollmentUpdate/ApproveEnrollmentTransfer intent. Execution requires
that exact authorized draft and approvalId;pending gestures,stale callbacks,
local expiry and unsaved recovery pointers cannot send another write.

Recovery uses a separate sessionStorage key:
`academydesk.penta.enrollment-lifecycle-recovery.v1`. It contains only
academyId,epoch,startRequestId,requestId,conversationId. No names,reasons,
source/target payloads,approvals,receipt bodies or tokens are stored there.
Create pointers are separate and never borrowed. Pointers are untrusted lookup
hints;the host authenticates and binds every read. Reload checks readiness and
uses GET start/draft recovery only. It never starts,re-prepares,re-approves or
replays execution automatically. A verified Authorized result permits only an
explicit same-key execution. Unknown/missing/expired results direct the user to
check the same operation or Manual before starting another. Competing pointers
are not overwritten or erased;closing retires the old source selections.

Success requires a verified Succeeded draft,approval,receiptId,auditId and UTC
completion time. Update receipt must preserve the source enrollment,student,
source batch and requested status/end date;its original start date is also
checked while available in current memory. Transfer receipt must identify a
new replacement enrollment for the same student in the exact target batch,
Active with start=reviewed transfer date and no end date. The source record's
Transferred state still requires native SQL verification. Reload can validate
the canonical host/receipt relationships but does not fabricate source names
or a source start date absent from the recovery contract. Once accepted,the
review/hash/approval and completed receipt identities cannot drift on another
read in the same session. A lost response never becomes a claimed success.

## Validation and evidence boundaries

Private evidence is outside Git:
`/workspace/c41-enrollment-lifecycle-ui-20261112`. It contains preserved failed
execution/harness attempts,final unit/frontend/build/lint/type logs,104-case
native Chromium results and screenshots,source/assembly preservation,export
hashes and the final publication receipt. No raw customer/laptop SQL evidence
was fabricated,copied,removed or made to pass by weakening assertions.

| Check | Actual result |
| --- | --- |
| New lifecycle client |111/111 PASS;actual TypeScript transpiled client with synthetic transport |
| Important client cases |Both tools and all six status payloads;minimal bounded source parsing;source/target binding;canonical/receipt tampering;five-key recovery;cross-create separation;storage failure;stale callbacks;pending/replaced/disposed session;explicit retry after GET recovery;401/403/404/409/410/429/503 refusal |
| Native Chromium |104/104 PASS:2 widths320/1440 ×2 real pilot role labels Owner/AcademyAdmin ×2 operations ×13 cases;synthetic host only |
| Browser cases |Normal,lost start/prepare/approval,before/after execution response loss,storage denial,expiry,access denial,bad receipt,session replacement,disabled endpoint,foreign source result;no auto replay,no false receipt,Prism/Orbit theme,no horizontal overflow |
| Browser resources |Installed /usr/bin/chromium;sandbox enabled;only loopback synthetic API intercepted;temporary owned profile removed;zero runtime/interception errors |
| TypeScript |PASS,tsc --noEmit |
| Lint |PASS,0 errors/61 existing warnings;no new lifecycle warning |
| Exact-lock web build |PASS,Next16.4.3/React19.2.8,webpack static export;89 segment aliases and585 export files hashed;loopback API build intended for QA only |
| Full frontend |96 current files,2,720 tests:2,717 PASS,3 genuine FAIL,zero skips/cancellations;only communication-drafts.test.cjs has the inherited missing raw SQL receipt failures |
| Missing original evidence |QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log is still absent in Cloud;its three tests remain failing,not skipped or replaced |
| Backend source reuse |All755 tracked API/API-test/SQL-harness source paths and C40 built assemblies match27291e21 byte-exact;reuse2,743/2,743 C40 backend passes including78 new host cases;no new backend/SQL run |
| Shared/C39 preservation |Shared root handoffs/tracker,create lib/component/page/styles,UTC helper and SQL harness unchanged;existing assistant changes one navigation link only |
| Mini/vendor preservation |Mini clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8;frozen source untouched;all16 vendor paths match base |
| Release policy |15/15 guard controls PASS;actual guard exit1 retains5P0 plus BUG-AUTH-REVOCATION-001;register120:5P0/102P1/13P2,0 formal Closed+Pass |
| Hosted CI |Unverified;latest actual C31 Actions query was Forbidden;no repeated bypass,dispatch or assumption that a feature push ran CI |
| Native acceptance |C39/C40/C41 combined positive Identity/SQL/browser/races/audit rollback/device qualification remains Desktop-owned and OPEN |

The first default-sandbox Next build could not parse its TypeScript child
--showConfig output. The same exact locked build passed through the normal
approved execution path with no package installation or source workaround.
The default-sandbox frontend runner returned96 empty child logs/zero executed
tests;that is unusable execution evidence,not a pass. The approved unchanged
runner produced the actual2,720-test result above. Both attempts are preserved.

The initial new browser harness queried document.body before its creation and
failed all104 cases before application interaction. The harness was corrected
to wait with a null-safe body read;the failed result/log remain preserved.
The final104 cases passed without changing app source or existing assertions.
This is a test-harness correction,not104 application defects repaired.

Commands:node QA/tools/penta-enrollment-lifecycle.test.cjs;
node QA/tools/run-frontend-tests.cjs;
apps/web/node_modules/.bin/tsc --noEmit (from apps/web,using its local binary);
npm run lint;NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack;
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/penta-enrollment-lifecycle-browser.cjs;
node --test QA/tools/production-gates.test.cjs;
node QA/tools/check-production-gates.cjs;
node QA/tools/priority-issue-status.cjs;git diff --check;source and remote checks.

## Remaining prioritized acceptance and ownership

1. Desktop Sol High:finish live C39 approval/recovery on its owned SQL host,
   preserving29c12181 UTC correction. Then integrate the exact C40 source and C41
   return selectively without overwriting newer Desktop finance/shared work.
   Keep endpoint flags Testing-only and use guarded disposable same-store SQL
   with dedicated synthetic pilot identities;do not enable production writes.
2. Desktop:C40 native matrix in C40_ENROLLMENT_LIFECYCLE_HOST_2026-11-12.md.
   Verify bounded sorted local/historical/>10/empty/foreign search and fixed audit,
   real purpose-bound start/replay/GET missing/expiry/shared quotas,audit rollback,
   current-role/stamp/tenant/module/maintenance refusal and same-key races.
3. Desktop:C41 actual exported browser plus native SQL integration. Check exact
   Update receipts for all six existing statuses/reasons/end dates,and Transfer
   source=Transferred plus replacement=Active in the selected target/date.
   Verify stale source/capacity/prerequisite/duplicate refusals,wrong hash/intent/
   approval,required audit rollback,concurrent one-mutation/one-receipt replay,
   response interruption and reload GET-only recovery,current-access loss and
   Manual readback. Preserve original services/data/evidence and report restores.
4. Release integrator/Local:finish five P0 and mandatory broader session acceptance.
   DATA-0001/0002 require linked finance/reminder/family critical/device agreement;
   DATA-0003 requires payroll identity/history/print and critical/device acceptance;
   DATA-0010 requires audited void/evidence restore and linked critical/device
   checks;SEC-0001 requires private-media authorization/recovery and real-device
   capture/upload/view/download. AUTH-REVOCATION001 remains a separate mandatory
   release scenario even if all five P0 records pass. Manual acceptance remains
   required despite its deferred implementation order.
5. Release:exact-source hosted CI/security/dependency/image/runtime evidence,
   remaining role/tenant/customer-critical workflows,private integration/provider
   readiness,migration/backup/restore/rollback and staging acceptance remain open.
   Source fixes,synthetic checks or report links do not establish formal closure.
6. Next Cloud packet:inspect latest Desktop claims and the remaining scheduling/
   attendance protected browser contracts before claiming the next bounded AI
   workflow;do not overlap SQL/native work or restart enterprise QA from zero.
   Mini/Plus/Pro semantic/privacy/performance/model integration and measured90%
   eligible conversational outcomes remain unaccepted;Ultra deferred. Preparing
   host/UI code does not authorize model loading or imply a launch date.

No production qualification,main merge or Azure deployment is claimed. The
existing stop on model loading/download/inference/training/activation remains.
