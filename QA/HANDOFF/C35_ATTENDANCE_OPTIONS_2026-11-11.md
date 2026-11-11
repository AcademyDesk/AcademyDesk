# C35 least-privilege attendance workspace — 11 November 2026

MODEL:Sol High for authority/roster/read isolation;Medium for settled UI/docs.
TOOL:Cloud Codex. REPOSITORY:AcademyDesk/AcademyDesk.
BRANCH:codex/cloud-c35-attendance-options-20261111.
BASE:c4bd654f01d6b8ebf44219b68ea850e366c3736a,clean published C34.
DESKTOP latest observedfa2e19d30504734a4790341f3546d64e0b073836 already preserved;
Desktop owns Admin payment approval/recovery flow. No overlap.
STATUS:IMPLEMENTED / CLOUD VALIDATED;native acceptance and release remain OPEN.
TASK:attendance subset of BUG-FUNC-0018;preserve current note/session safety.

Exact MAY MODIFY paths(11):

- apps/api/Controllers/AttendanceController.cs — absolute GET attendance/options and minimal DTOs only
- apps/web/src/app/attendance/page.tsx — scoped workspace/read readiness;preserve mark payload/record-generation/note behavior
- tests/AcademyDesk.Api.Tests/AttendanceOptionsAccessTests.cs
- QA/tools/attendance-options.test.cjs
- QA/tools/attendance-options-browser.cjs
- QA/tools/attendance-isolation.test.cjs — scoped synthetic fixture only;retain assertions
- QA/tools/attendance-notes.test.cjs — scoped synthetic fixture only;retain assertions
- QA/ISSUES/BUG-FUNC-0018.md
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md
- QA/HANDOFF/C35_ATTENDANCE_OPTIONS_2026-11-11.md

MUST NOT MODIFY:permission/filter/module/Identity/session policy,attendance List/
Mark action bodies,status/note/active-enrollment validation,teacher portal/host,
scheduling/admission/domain/SQL/migrations,finance/PENTA,new Desktop work,other
pages,globals/vendor/Mini,existing assertions/raw evidence. No main/Azure/
environment publication/public access/model download/load/inference/training/
activation. Mini/Plus/Pro unqualified;Ultra deferred.

Policy:GET api/academies/{academyId}/attendance/options inherits attendance.manage
and current global academy/tenant/actor/module/maintenance checks. Only minimal
batch labels,session id/batchId/startUtc,active enrollment studentId/batchId/status
and referenced student id/firstName/lastName. Scope every relation to local student/
batch/academy,omit nonactive enrollment/private records,deduplicate roster pairs,
no tracking/writes/audit,no-store. Preserve existing server attendance write rules,
including historical session behavior;options are not authorization/reservations.
Client uses options then existing selected-session attendance GET;never falls back
to unrelated full APIs. Validate all required arrays/ids/relations/dates;failed or
obsolete workspace reads must not fabricate zero roster/no schedule. Provide
GET-only reload,preserve note drafts and existing record/read/save generation
checks;retired workspace reads/saves cannot unlock another roster. No business
write retry/replay added;broader uncertain-save recovery is separate.

VALIDATE:original/final actual HTTP/Identity/InMemory role/direct/custom-grant/
revocation/expired/foreign/inactive/maintenance/owner/Core/minimized/active-roster/
no-write matrix;actual TSX route/schema/readiness controls;all retained attendance
notes/isolation tests;native sandboxed Chromium320/1440 Operations/Manager with
synthetic options/records/notes/failures/session transitions;exact-lock types/
build/lint/full frontend/API/release guard. Preserve missing Cloud SQL receipts.
STOP WHEN:bounded source/checks/source-bound handoff feature-pushed and exact
remote SHA verified. No automatic issue closure/native SQL/device qualification.
NEXT ROUTE:Desktop Sol High actual SQL/Identity/browser/device acceptance;Cloud
next separately claimed remaining0018 workflow. Model-loading stop remains.

## Source and ownership return

Implementation:`7485173642d62abf7742acebf9d01c48bb3bb94c`(seven source/test paths).
Claim published/remote-verified before edits:
`97e83b62df19c54cd85659f47d50827f5e5493a2`.
During C35,Desktop published payment review/recovery UI at
`f708879fc09a40a540c5d6f0fd5b3e814507bed4`;imported with provenance at
`17b74f134f59e3a8dcda8f9d2a30d35f2914ee6f`. All13 imported paths are byte-exact
to Desktop's source. Desktop now owns protected invoice issuance. No overlap.
Shared root handoffs retain Desktop's update;this separate packet is the return.
Final local/remote SHA and clean status are verified after feature push and sealed
in private publication.json/final response;the documentation commit cannot contain
its own SHA. Seven implementation paths plus issue,two derived inventories and
this handoff complete the11-path claim.

No main/Azure/environment publication/replacement/public access,PR/workflow
dispatch,paid compute,model or repository-setting change. Cloud was observed
current/running/connected with restricted networking enforced. No laptop/TCP/D:
access is claimed. Mini remains clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8;
33 frozen paths untouched.16 web vendor paths(including12 upstream pins)match C34.

## Behavior and boundaries

The absolute GET api/academies/{academyId}/attendance/options remains inside
AttendanceController's existing attendance.manage/current academy/tenant/actor
pipeline. It does not change catalog,module,Identity,session,owner or maintenance
policy. Operations/Manager and valid custom/direct grants can load the necessary
workspace without unrelated full reads. Invalid/revoked/expired/foreign grants,
unrelated roles and a non-owner PlatformOwner role label cannot elevate access.
Current role/grant removals apply to the same bearer. Core-only use is covered.

No-store,no-tracking selector data is exactly students[{id,firstName,lastName}],
batches[{id,name}],enrollments[{studentId,batchId,status}],sessions[{id,batchId,
startUtc}]. Student names are limited to referenced Active enrollments;all roster
relations and session batch references must belong to the current academy.
Duplicate active student/batch pairs collapse. Paused/nonactive enrollment and
foreign/corrupt relations do not enter the workspace. Inactive students that still
have an Active enrollment remain included,matching existing Mark eligibility.
No contact,medical,finance,teacher/branch or attendance-note detail is projected.
Selectors reserve nothing;the server rechecks active membership and authority on
every mark. No new transaction/atomic snapshot or historical-session policy.

The page fetches options,then existing selected-session attendance. No full
management fallback. All arrays,unique ids,dates and roster references are
validated before committing the workspace. Failed/malformed loading cannot look
like zero students or an empty schedule. Reload is GET-only and retains notes.
Workspace/record generations retire stale saves and late reads;unmount prevents
updates to an obsolete page. Existing note content/clear/trim,session-keyed drafts,
status/mark payload and readback behavior remain. Existing server List/Mark/DTO
bodies and retained note/isolation assertion bodies are byte-exact. Broader
uncertain-write recovery remains a separate acceptance task;no automatic replay.

## Actual validation

Raw evidence is private outside Git at
`/workspace/c35-attendance-options-20261111`. Original/final logs,TRX,screenshots,
source/assembly/export hashes and preservation receipts remain. No hidden skip,
weakened assertion or fabricated SQL receipt.

| Check | Observed result |
| --- | --- |
| Original HTTP/Identity/InMemory |34 cases:1 PASS/33 FAIL;missing scoped GET contract;not33 independent defects |
| Original actual-page controls |19 cases:1 PASS/18 FAIL;strict scoped fixture/new readiness behavior absent;not18 independent defects |
| Initial current API |34/34 PASS;new options authority/roster schema/no-write controls |
| Final new HTTP scope |38 in full API:adds read→mark with notes/audit for Operations,Manager and custom attendance permission plus refusal after enrollment is paused |
| Full API before Desktop f708879 |2,559/2,559 PASS;zero skips |
| Combined full API after f708879 |2,559/2,559 PASS;zero skips;fresh compile/full regression;not native SQL proof |
| New actual-page controls |19/19 PASS;scoped calls,401/403/429/503,no fallback,malformed/duplicate/foreign/date/empty,draft reload,retired callback and unmount |
| Retained actual-page controls |24/24 attendance-note payload/clear/Unicode/500-character/readback/failure and10/10 session/cache/save isolation PASS;assertion bodies byte-exact |
| Initial and integrated Chromium |32/32 PASS each;2 roles ×320/1440 ×mark/denied/malformed/empty/record-refusal/draft-refresh/session-switch/delayed-record |
| Native scope |Synthetic Operations/Manager profile/API,actual roster/notes/selectors;two deliberate marks in each mark case,eight intended writes total;zero full people/batch/enrollment/scheduling requests |
| Native resources |Browser sandbox enabled;owned profile removed;no horizontal page overflow;no actual SQL/device/model claim |
| Integrated exact-lock build |Cached locked Next16.4.3/React19.2.8 optimized webpack export PASS;87 aliases,571 files;no install |
| Integrated types/lint |TypeScript PASS;lint0 errors/61 retained warnings |
| Initial/integrated full frontend |92 files;2,474 PASS/3 FAIL/2,477 tests;zero skips/cancellations |
| Exact frontend failures |Only communication-drafts45/48;missing Cloud QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log;Desktop's historical receipt is not fabricated |
| Release guard |15/15 controls PASS;actual guard exit1 retains five P0 records plus mandatory broader-session acceptance |
| Issue inventory |120 records:5P0/102P1/13P2;0 formal Closed+Pass;not120 untouched defects or completion percentage |
| Hosted CI |C35 unverified;latest Actions query in C31 returned Forbidden,not retried/bypassed;feature push is outside verify.yml push filters;no dispatch/PR |

Commands:dotnet test --no-restore -p:UseSharedCompilation=false for original,
target,full and combined runs;TRX retained;node attendance-options.test.cjs,
attendance-isolation.test.cjs,attendance-notes.test.cjs from QA/tools;
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/attendance-options-browser.cjs;
node QA/tools/run-frontend-tests.cjs;tsc --noEmit;npm run lint;
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack;
node --test QA/tools/production-gates.test.cjs;node QA/tools/check-production-gates.cjs;
node QA/tools/priority-issue-status.cjs --write. No external audit upload.

## Prioritized remaining acceptance / next owners

1. Desktop Sol High:selectively integrate the seven C35 implementation/test paths
   and this handoff,preserving newer payment/invoice/PENTA work. On isolated
   SQL/Identity verify translated correlated queries/Distinct/order,minimized
   active roster,duplicate/corrupt/foreign omission,Core-only authority,revocation,
   suspended/maintenance/owner boundaries,no read writes/audit and full read denial.
   Exercise real mark/readback/notes/audit and membership change between options
   and mark,with required-audit rollback/current tenant policy. InMemory is not
   SQL transaction/concurrency proof. Teacher portal/assigned-teacher policy stays.
2. Desktop:actual authenticated SQL-backed Operations/Manager/custom attendance
   browser,empty/unavailable/read-retry,note retention/clear,workspace reload,
   A→B→A sessions,late results/logout/replacement,real devices/other engines.
   Broader unknown-save recovery/no-replay and linked critical cases remain.
3. Cloud:next independently claim scheduling/promotions/makeup/events0018 scopes
   or remaining recovery gaps. Reuse accepted repairs;no broad management grants
   merely for selectors. Desktop owns protected invoice issuance now. Broad0018
   remains OPEN despite completed enrollment/batch/attendance source returns.
4. Release:five P0 records—DATA0001/0002 finance/reminder/family/currency/permission
   agreement,DATA0003 positive-net payroll identity/history/print,DATA0010 audited
   void/evidence restore,SEC0001 private media authorization/recovery/capture/devices;
   plus AUTH-REVOCATION001 broader SECURITY-SESSION001 across remaining roles/
   devices/critical cases. No issue/priority closure is inferred from test totals.
5. Integrator:exact-release hosted CI,fresh security/dependency audits,combined
   images/runtime,migration/backup/restore/rollback and staging acceptance. Keep
   genuine receipt failures until source-bound input exists. No main/Azure action
   follows publication. Non-AI/native/release work remains.

Mini/Plus/Pro remain unqualified,Ultra deferred;90% eligible conversational
outcomes unmeasured. Testing-only deterministic host checks are not model
qualification. Candidate loading/download/inference/training/activation stopped.
