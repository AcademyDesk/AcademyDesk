# C43 protected class scheduling browser host — 13 November 2026

OWNER:Cloud Codex. MODEL:Sol High. TOOL:Codex. REPOSITORY:AcademyDesk/AcademyDesk.
ENVIRONMENT:managed Linux cloud. BRANCH:codex/cloud-c43-class-scheduling-browser-host-20261113.
BASE:a19d5e244d3f3a22884f23dc5b48b0935c9a55dc (C41 plus C42 blocker report).
STATUS:CLOUD VALIDATED;native SQL/browser/model and release acceptance OPEN. Desktop83452a129c1a7315922dbeeaf1b50de3d62cbdac read-only;
owner says Desktop unavailable. C42 SQL image CDN is403;no native acceptance claimed.

TASK:separate default-disabled/Testing-only class schedule/reschedule browser
protocol,readiness,bounded minimized current-academy batch/source lookup with
fixed audit,purpose-bound explicit start and GET-only recovery,exact draft/
approval/execution/read routes. Reuse existing scheduling workflow/Manual lock/
plan/current-role/domain/audit/receipt,without broadening legacy behavior.
Browser-specific UTC response copies retain original ticks and hash/storage.

MAY MODIFY (exact7):
- apps/api/Controllers/PentaOnboardingController.ClassSessionBrowser.cs (new)
- apps/api/Intelligence/Penta/PentaClassSessionSchedulingWorkflow.Browser.cs (new)
- apps/api/Intelligence/Penta/PentaClassSessionBrowserSession.cs (new)
- apps/api/Intelligence/Penta/PentaClassSessionSchedulingWorkflow.cs (partial declaration and conditional browser guards only)
- tests/AcademyDesk.Api.Tests/PentaClassSessionBrowserAccessTests.cs (new)
- tests/AcademyDesk.Api.Tests/PentaClassSessionBrowserSessionTests.cs (new)
- QA/HANDOFF/C43_CLASS_SCHEDULING_BROWSER_HOST_2026-11-13.md

MUST NOT MODIFY:existing controller/parser/domain/permissions/Identity/migrations/
SQL harness/Desktop native/finance work,shared root handoffs/trackers,frontend,
C38-C41 enrollment sources,Mini/vendor/dependencies/lockfiles/existing assertions/
raw evidence/environment settings. No model loading/download/inference/training/
activation,main merge,Azure deployment,public internet changes or paid compute.

VALIDATE:real HTTP/Identity availability/current-role/tenant/module/maintenance/
malformed/no-store/no-write refusal matrix;real data-protection cross-purpose/
actor/stamp/version/expiry/pending controls;minimal lookup and UTC serialization
contracts;full API and SQL harness compilation;legacy stripping proof/source
preservation/release guard. Positive SQL/browser/races/audit rollback remain OPEN
under the exact C42 registry prerequisite;no mock substitute qualification.
STOP WHEN:bounded tested feature source/report pushed and exact remote SHA checked.
NEXT:separate schedule/reschedule UI and current native/release acceptance backlog.
Mini/Plus/Pro unqualified;Ultra deferred;90% outcome target unmeasured.


## Implementation and source boundaries

Implementation:`0badef52201d1251c2428d74d122bcd0bd39cccf` (six source/test paths).
Claim:`dc150a6afc2d14c953c9c0fe3cada94beb1da87f`,published and exact remote SHA
verified before implementation. Final publication SHA is verified after push and
recorded in the private publication receipt/final response. All seven paths are
accounted for. Latest read-only Desktop ref remains83452a129c1a7315922dbeeaf1b50de3d62cbdac.
No Desktop/shared/native file was imported or replaced.

The recovered environment is current/running/connected with restricted network
policy enforced. C42 diagnosed the SQL image prerequisite at
westus.data.mcr.microsoft.com:the inherited managed proxy returns403 Forbidden.
No environment/network/privacy/VPN/TCP setting was changed;no SQL container or
database was provisioned. The unfinished C42 prototype was privately backed up,
not published as a working test harness. No alternate mirror or policy bypass
was used. C42's report is included by ancestry,not a new native qualification.

No main merge,Azure deployment,PR/workflow dispatch,environment publication/
replacement/deletion/public-internet change,new app dependency or paid compute,
model loading/download/inference/training/activation occurred. Mini remains clean
at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8;33 frozen paths and16 vendor files
are untouched. Model-runtime image retained from setup was not run or changed.

## New protected browser protocol

Root:`/api/academies/{academyId}/penta/class-scheduling/browser`.
All nine routes inherit real Identity.Bearer authentication and the existing
academy/tenant/module/maintenance pipeline. Additional pilot policy is actual
current Owner/AcademyAdmin;the platform-owner flag,other roles and granular
scheduling grants do not grant this separate pilot. Availability requires all:
Testing,Penta:Enabled,Penta:ClassSessionSchedulingEnabled,
Penta:ClassSessionReschedulingEnabled and new default-false
Penta:ClassSchedulingBrowserContractsEnabled. No flag is enabled by this packet.
Legacy create-only configuration remains unchanged when rescheduling is disabled.
The combined new browser protocol is unavailable until both operations are enabled.

| Method/path under root | Behavior |
| --- | --- |
| GET /readiness |Current authority plus same-store linked SQL required;class-scheduling-browser.v1,requiresApproval=true,Create/Reschedule tools;no new session,draft,domain or audit writes |
| GET /batches?q=… |Bounded audited academy batch labels and local default references for creating a class |
| GET /sessions?q=… |Bounded audited future Scheduled sources matching batch name;no meeting links or location/contact data |
| PUT /conversations/{startRequestId} |Exact empty body;purpose-bound explicit start;201 new/200 verified replay;session and required start audit commit together |
| GET /starts/{startRequestId} |Read-only same-key recovery;404 missing/410 expired;never creates or audits |
| PUT /conversations/{conversationId}/drafts/{requestId} |Exact contextVersion0 plus existing typed Create/Reschedule payload;strict current pilot before semantic refusal |
| GET same draft |Read exact saved draft or verified receipt,with current access and purpose checks |
| POST same draft /approve |Exact revision/hash and appropriate ApproveClassSessionSchedule/ApproveClassSessionReschedule intent |
| POST same draft /execute |Exact revision/hash/approvalId,existing rechecks/Manual scheduling lock/domain save/required audit/receipt |

Readiness is host protocol availability,not model readiness or a new tool
registry. This packet adds no client UI and grants no new role/Manual permission.
Successful enabled host responses retain no-store/no-cache. No model dispatcher,
provider call,message delivery or meeting-provider integration is introduced.

## Lookup and session boundaries

Both lookup queries accept2–80 trimmed,control-free literal characters and use
parameterized LINQ. Each reads11 records,returns at most10 with hasMore,and sorts
with stable id tie breakers. Batch lookup scopes by academy,sorts by name/id and
returns only sourceId,name,teacherId,branchId,sourcePath=/sessions. Default refs
are returned only if their teacher/branch is in the current academy;foreign or
missing references project null. Historical local batch names remain visible;
lookup is not admission or scheduling authority.

Class lookup joins local ClassSessions and Batches,requires current Scheduled
status and a future start,and filters corrupt/foreign teacher/branch assignments.
It sorts by start,batch name,id. Exact seven-field row is sourceId,batchId,
batchName,startUtc,endUtc,status,sourcePath=/sessions. It selects no meeting link,
room,teacher contact,branch address or attendance data. The time values are copied
to UTC after materialization without changing ticks. Actual SQL translation,
foreign/tie/empty/>10 behavior and native minimization remain pending under C42.

Successful lookups,including empty results,save one fixed
PentaClassSchedulingSearched audit in the same transaction before returning data.
Metadata contains only the constant tool discriminator for batch or class lookup;
raw query/names are not saved there. Audit failure must prevent a data response;
real SQL rollback still needs its native run. Readiness/start recovery do not audit.

Readiness/lookup/start run under existing linked Academy/Identity Serializable
transaction and Manual academy scheduling lock. They clear the pre-lock Identity
tracking state,reload the current security stamp,role,active actor/academy and
module scope,then recheck flags. SQL is mandatory;InMemory is refused. The new
session purpose is academy.class-session-browser-session.v1 with a separate
scope-derived start-key namespace. It binds session id,academy,actor,version0,
expiry ticks and current security stamp,and refuses pending,foreign,tampered,
legacy,onboarding,finance and enrollment cursors. Existing separate Mini state is
untouched. Shared daily session limits remain100 per academy/20 per actor and a
24-hour conversation expiry;no new expiry replacement or automatic replay.

## Existing scheduler and UTC behavior preserved

The shared workflow becomes partial and accepts optional browser=false. Added
single-line conditional guards enforce availability,current stamp,context0,
typed payload and purpose binding for the new routes only. Stripping those
additions recreates the old workflow byte-exact. Existing controller/parser,
scheduling service,domain rules,hash,transaction,approval,audit and legacy paths
are unchanged. The original direct paths retain their earlier purpose behavior;
this packet does not claim a global redesign of every legacy conversation route.

Create/reschedule preparation resolves the original server plan and defaults.
Existing approval intent and exact payload hash bind the current source/target
slot. Existing execution rechecks source/update timestamp,assignment,teacher/
branch/room clashes and future time under the shared Manual lock. It retains
batch teacher inheritance,source class id/attendance on a move,mandatory audit
and same-request receipt. Existing expiry remains the minimum of its conversation,
plan/source start and10-minute draft limit. No new status-changing or assignment
mutation is added to the reschedule command.

Browser-only response copies mark draft expiry,plan/source start/end and receipt
completion as UTC using DateTime.SpecifyKind. SQL datetime2 carries no Kind;the
original ticks are preserved. Stored state,hash,approval,receipt ids and legacy
responses are not rewritten. Unit checks verify copy semantics and JSON Z output.

## Actual validation

Evidence is private,outside Git:
`/workspace/c43-class-scheduling-browser-host-20261113`. It includes scoped/full
TRX/logs,new-case inventory,harness build,guard/source/export preservation and
final source/assembly/publication hashes. Failures/attempts are retained.

| Check | Actual result |
| --- | --- |
| New focused tests |76/76 PASS:40 real HTTP/Identity/access controls and36 real crypto/UTC/schema/availability controls;zero skips |
| HTTP scope |All nine routes gated independently by each missing/false flag;Production refusal;current bearer role removal;unsupported roles,foreign/inactive/maintenance/platform-owner refusal;strict body/context/hash/intent/approval and bounded queries;no-write snapshots |
| Supported actors |Owner/AcademyAdmin return503 against InMemory without writes;intentional fail-closed linked-SQL prerequisite,not positive scheduling execution |
| Crypto |Independent scope-derived keys/purpose,current stamp,tenant/actor/id/version/expiry/pending/ciphertext binding;enrollment create/lifecycle namespace and cursor separation;legacy state preserved |
| UTC/minimal schema |Response-copy ticks/hash/original Kind preserved;UTC Z fields;exact5-field batch and7-field class row JSON;literal Unicode/control/length query boundaries;Development/Production short-circuit before dependencies |
| Full backend |2,819/2,819 PASS,zero skips;76 new controls included;fresh scoped compile followed by full --no-build on identical source |
| SQL harness compilation |PASS,zero warnings/errors;unchanged sources;no native SQL execution |
| Release guard |15/15 actual controls PASS in-process;actual guard exit1 retains five P0 plus mandatory session gate |
| Issue register |120:5P0/102P1/13P2,0 formal Closed+Pass;unchanged;not a completion percentage |
| Legacy preservation |Conditional stripping gives prior shared workflow byte-exact;old controllers/parser/scheduler/domain and C38–C41 untouched |
| Frontend evidence reuse |Entire frontend and all585 C41 export hashes unchanged;C41 TypeScript/lint/build,111client and104synthetic Chromium checks retained,not rerun |
| Frontend original failures |Retained96-file result2,717PASS/3missing SQL receipt FAIL/zero skip/cancel;no assertion or evidence replaced |
| Source preservation |357 tracked frontend/SQL-harness paths,shared roots,Mini/frozen/vendor paths verified |
| Hosted CI |Current status unverified;latest actual C31 Actions query Forbidden;no dispatch/retry bypass or feature-push CI claim |
| Native acceptance |New positive SQL lookup/start/prepare/approve/execute/audit/race and exported browser/device checks NOT RUN;exact SQL CDN prerequisite recorded in C42 |

The first default-sandbox .NET test command stalled without test output;only its
owned process was stopped before the normal approved execution retry. That attempt
is not passing test evidence. The scoped approved run compiled and executed all76.
The initial sandbox node --test child reported only a wrapper-level file case;
that result was not counted as15 controls. Running the same unchanged guard test
file directly in-process produced the actual15/15 result above. No test assertion
or application source workaround was used for these execution-path limitations.

Commands:dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj
--no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~PentaClassSessionBrowser;
dotnet test same project --no-restore --no-build;
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore
-p:BuildProjectReferences=false -p:UseSharedCompilation=false;
node QA/tools/production-gates.test.cjs;node QA/tools/check-production-gates.cjs;
node QA/tools/priority-issue-status.cjs;git diff --check;source/export/remote checks.

## Prioritized remaining work

1. Native prerequisite:review the exact westus.data.mcr.microsoft.com image CDN
   denial before attempting owned cloud SQL again. Do not enable unrestricted
   internet,replace/delete the current environment,load unchecked image bytes,
   use a mirror/proxy bypass or confuse a saved draft with applied settings.
2. Native enrollment:C39/C40/C41 live Identity/API/SQL/exported-browser acceptance
   remains pending as described in C41/C42. A registry diagnosis closes no runtime,
   rollback,race,quota,current-role or receipt acceptance scenario.
3. Next Cloud:separately claim the schedule/reschedule browser UI using C43's
   bounded batch/future-class lookup,current canonical plan/source,exact per-tool
   review/approval/explicit execution and session-bound GET-only recovery. Preserve
   Manual scheduling/IST behavior and source/default teacher/branch binding.
   Then handle assigned-teacher attendance browser integration under its own role.
4. New C43 native matrix:verify real SQL minimized sorted local/foreign/corrupt/
   historical/tied/empty/>10 queries and fixed audits;current stamp/role/module/
   maintenance after locks;purpose-bound start/missing/replay/expiry/shared quotas/
   races and start audit rollback. Prepare/create/move InPerson/Online/Hybrid,
   inherited/override assignments,clash/default/source drift,wrong approval/hash,
   past/source expiry,Manual move-away-and-back,one execution/receipt/audit under
   races,required-audit rollback and same-key GET recovery. Reuse the exact legacy
   source-bound SQL reports without relabelling them as new browser qualification.
5. Release:five P0(DATA0001/0002 finance/reminder/family critical/device agreement;
   DATA0003 payroll identity/history/print;DATA0010 audited void/evidence restore;
   SEC0001 private media authorization/recovery and actual capture/upload/view/
   download)plus separate AUTH-REVOCATION001 broader session gate remain open.
   Exact-release CI/security/dependency/image/runtime,migration/backup/restore/
   rollback,remaining role/device/customer-critical journeys and staging acceptance
   are required. Existing Manual acceptance is deferred in order,not waived.
6. Mini/Plus/Pro remain unqualified;Ultra deferred. Qualified interpretation,
   semantic/privacy/performance and measured90% eligible conversational outcomes
   remain pending. Host/UI code does not authorize candidate model loading.

No production qualification,main merge or Azure deployment is claimed.
