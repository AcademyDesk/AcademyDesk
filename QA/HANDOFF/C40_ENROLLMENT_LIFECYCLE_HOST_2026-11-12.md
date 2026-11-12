# C40 protected enrollment lifecycle browser host — 12 November 2026

OWNER:Cloud Codex,Sol High for purpose/approval/authority boundaries.
STATUS:CLOUD VALIDATED;new native SQL/UI acceptance OPEN. BRANCH:codex/cloud-c40-enrollment-lifecycle-host-20261112.
BASE:94a4f99678abea7b23c050988952e2fa347fab94,clean published C39.
Desktop83452a129c1a7315922dbeeaf1b50de3d62cbdac was inspected READ ONLY.
Its C38 native SQL report records15 new and109 retained groups PASS at the exact
C38 source already included here. C39 UTC correction is preserved. Desktop owns
live C39 UI/SQL acceptance next. No Desktop tracker/evidence/SQL file is imported.

Exact MAY MODIFY paths(7):
- apps/api/Controllers/PentaOnboardingController.EnrollmentLifecycleBrowser.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentCreationWorkflow.LifecycleBrowser.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentLifecycleBrowserSession.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentCreationWorkflow.cs(conditional strict mode only)
- tests/AcademyDesk.Api.Tests/PentaEnrollmentLifecycleBrowserAccessTests.cs(new)
- tests/AcademyDesk.Api.Tests/PentaEnrollmentLifecycleBrowserSessionTests.cs(new)
- QA/HANDOFF/C40_ENROLLMENT_LIFECYCLE_HOST_2026-11-12.md

DELIVER:separate default-disabled/Testing-only lifecycle readiness,audited bounded
source-enrollment search,purpose-bound start/GET recovery and exact Update/Transfer
prepare/read/approval/execute routes. New flag EnrollmentLifecycleBrowserContractsEnabled
also requires existing pilot/enrollment/lifecycle enablement. Reuse existing
CurrentHostRefusal,linked transaction/admission lock,canonical change/source hash,
reason/transfer/prerequisite/capacity/domain rules,mandatory audit and receipt.
New session purpose refuses create/finance/onboarding contexts. Preserve current
create browser and legacy behavior;no implicit lifecycle expansion of C39 UI.
Search returns at most10 local joined enrollment/student/batch summaries,with
hasMore and fixed audit(no raw query/private contact/notes). Readiness/start GET
recovery never create/audit;explicit start atomically creates session+audit.

MUST NOT MODIFY:Desktop SQL/native/finance/Manual work or C39 live-browser paths,
C38/C39 create controller/session/workflow.Browser.cs/UTC helper,legacy controller/
lifecycle/domain methods,permissions/Identity/migrations/session policy/frontend/
vendor/Mini,root shared handoffs/tracker,original assertions/evidence. No main/
Azure/environment publish/replace/delete/public access,new package/paid compute,
model download/loading/inference/training/activation or workflow dispatch.

CHECKS:real Identity/HTTP availability/role/scope/malformed/non-SQL refusals,
real cryptographic purpose/stamp/metadata binding and cross-create separation,
conditional-only legacy/create preservation,full API/SQL harness compilation,
release guard and source/preservation/remote pins. Positive new lifecycle search/
start/update/transfer SQL/audit/receipt/races remain Desktop-owned acceptance;
prior C38 SQL evidence is not C40 qualification. No frontend change this packet.
STOP:bounded source/results/native handoff feature-pushed and remote SHA verified.
NEXT:Desktop C39/C40 native SQL/browser;Cloud separate lifecycle/status/transfer UI.
Mini/Plus/Pro unqualified,Ultra deferred;model-loading stop remains.

Automatic approval review rejected an earlier compound command that would import
shared Desktop trackers/native files. It was not executed. This safe alternative
leaves all those paths untouched and uses only read-only external source/evidence
references;no attempt to bypass that rejection or copy through another route.

## Completed source and publication return

STATUS:IMPLEMENTED / CLOUD VALIDATED;new positive native SQL/UI acceptance OPEN.
Implementation:`4d42483096297581402eac1217edc3ee5abe4688`(six source/test paths).
Claim:`5d8894350d0fc4163df6d3011dde076704babc69`.
The first claim push returned HTTP503;subsequent verification was briefly blocked
by approval-review usage capacity. An initial local prototype was written before
that failed publication was checked;this sequencing mistake was disclosed and
further edits paused. The prototype was backed up locally. Normal approval later
resumed;the claim was pushed and exact remote SHA verified before the remaining
implementation/tests resumed. No prototype code was committed/pushed during the
pause. Final publication SHA is verified after push and recorded in private
publication.json/final response;this commit cannot contain its own SHA.
All seven claimed paths are accounted for.

The proposed compound Desktop import was rejected by automatic approval review
because it would overwrite shared trackers and Desktop-owned validation files.
That command was not executed. No shared import was retried by another route.
The safer completed packet modifies only the seven claimed Cloud paths. All root
handoffs,SQL harness sources,C39 frontend/UTC correction and legacy files remain
unchanged;Desktop evidence is referenced read-only rather than imported.

Desktop's verified read-only tip remained83452a129c1a7315922dbeeaf1b50de3d62cbdac
on the final pre-validation fetch. Its
[actual C38 SQL report](https://github.com/AcademyDesk/AcademyDesk/blob/83452a129c1a7315922dbeeaf1b50de3d62cbdac/QA/REPORTS/PENTA_ENROLLMENT_BROWSER_SQL_2026-11-12.md)
records15 new native groups and109 retained groups PASS at exact C38 source.
This qualifies that recorded scope,not C40. Desktop next owns C39 live browser/
SQL acceptance. Source here retains C39's additional UTC response correction.

No main merge,Azure deployment,PR/workflow dispatch,environment settings/
publication/replacement/deletion/public-access change,new dependency/paid compute,
model download/loading/inference/training/activation or laptop/TCP/D: access claim.
Runtime was current/running/connected with restricted network policy enforced.
Mini remains clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8,33 frozen paths
untouched;16 vendor files(including12 upstream pins)are byte-exact.

## New lifecycle browser contract

Root:`/api/academies/{academyId}/penta/enrollments/lifecycle-browser`.
Eight new routes stay inside the authenticated Identity.Bearer controller and
current global academy/tenant/module/maintenance pipeline. Availability requires
Testing,Penta:Enabled,Penta:EnrollmentCreationEnabled,Penta:EnrollmentLifecycleEnabled
and new Penta:EnrollmentLifecycleBrowserContractsEnabled. The new flag defaults
false and was not enabled here. The create-browser flag is not a prerequisite
for lifecycle operation;likewise this flag never enables create. Current pilot
scope remains real Owner/AcademyAdmin,not other roles or the platform-owner flag.

| Method/path under root | Contract |
| --- | --- |
| GET /readiness |Current supported actor and linked SQL authority required;protocol=enrollment-lifecycle-browser.v1,requiresApproval=true,explicit Update/Transfer tools;no session/draft/domain/audit effects |
| GET /search?q=… |2–80 normalized noncontrol characters;audited bounded lookup of local source enrollments,not authority to change one |
| PUT /conversations/{startRequestId} |Body exactly{};purpose-bound idempotent explicit start;201 new/200 verified replay |
| GET /starts/{startRequestId} |Same-scope read-only recovery;missing404/expired410;never creates a session |
| PUT /conversations/{conversationId}/drafts/{requestId} |Exactly contextVersion0 plus typed Enrollment.Update or Enrollment.Transfer payload;create and unknown tools refused |
| GET same draft |Current canonical change/status/verified committed receipt |
| POST same draft /approve |Exact revision/hash plus ApproveEnrollmentUpdate or ApproveEnrollmentTransfer matching the stored change |
| POST same draft /execute |Exact revision/hash/approvalId;current host/domain checks and existing atomic mutation/audit/receipt |

Enabled responses retain no-store/no-cache and bounded exact command parsing.
Readiness is not model readiness and cannot grant authority. Tool strings in the
response/audit are not a new model dispatcher or model tool registration.
No client UI is added/expanded by this host-only packet.

## Data,authority and compatibility boundaries

Search joins enrollments to current-academy student and batch records and scopes
all three relations independently. Query matches learner first/last name or batch
name through parameterized LINQ. Ordering is first name,last name,batch name,id;
SQL requests11 rows,returns at most10 and exposes hasMore. Each row contains only
sourceId,studentId,studentName,batchId,batchName,status,startDate,endDate and fixed
/enrollments sourcePath. Historical/inactive local labels remain available;
foreign/corrupt joins and contact,guardian,medical,finance,notes,old reason data
are not selected. Readiness/recovery perform no audit;successful search(including
empty results)saves one fixed PentaEnrollmentLifecycleSearched audit atomically
before returning data. Raw search text/names are not stored in audit metadata.
Actual query translation,minimization and rollback require native SQL acceptance.

Lifecycle sessions use an independent data-protection purpose and derived start
key namespace. Identical raw keys cannot reuse create-session identities. Scope,
actor,security stamp,version0,expiry ticks and pending-state checks are retained;
create/lifecycle cursors refuse each other. Existing shared UTC-day limits remain
100 per academy/20 per actor. Start session plus mandatory lifecycle-start audit
commit together under the existing linked Academy/Identity authority boundary.
Current lifecycle flags are rechecked after the lock;no silent expired replacement.

The shared enrollment workflow adds only an optional lifecycleBrowser=false mode
and conditional strict branches. Stripping those additions yields C39 byte-exact
code. All legacy calls and create-browser calls retain their previous behavior.
Lifecycle mode accepts change bodies only;create mode remains admission-only.
Existing canonical change/source hashing,exact per-tool approval intent,reason/
transfer/admission/lock/domain rules,required audit and verified receipt are reused.
C39's UTC response-copy normalization also covers the new lifecycle outcomes.
No change to stored timestamps,legacy responses,global serializer,permissions,
transaction rules,SQL/migrations or create session/controller code.

## Actual validation

Private evidence remains outside Git:
`/workspace/c40-enrollment-lifecycle-host-20261112`,including local prototype
backup/initial review,scoped/full TRX,logs,source/assembly hashes,preservation and
publication receipts. No original failure/SQL receipt/assertion was fabricated,
removed or weakened. A build of the local prototype was not treated as security
qualification;full validation followed verified claim publication.

| Check | Observed result |
| --- | --- |
| Focused new scope |78/78 PASS:37 purpose/search/schema controls and41 availability/Identity/HTTP/command controls(includes Development gating unit) |
| Real data protection |Current stamp,tenant/actor/id/version/expiry/pending/ciphertext binding,empty scopes,cross-create namespace/cursor refusal,UTC ticks and untouched legacy Mini state |
| HTTP boundaries |All eight routes independently gated by pilot/base enrollment/lifecycle/new/missing flags,Production refusal,unrelated roles,foreign scope,inactive actor/academy,maintenance,real owner flag,same-bearer role removal and no-write snapshots |
| Operation contracts |Exact Update and Transfer payload/intent support reach required-store refusal;create/unknown intent,missing nonactive reason,wrong revision/hash/approval/body/context/query refused |
| Supported Cloud actor |Owner/AcademyAdmin return503 against InMemory with no domain/session/draft/audit writes;intentional fail-closed SQL prerequisite,not positive new SQL execution |
| Search contract |Normalized literal Unicode/control/length boundaries and exact minimized JSON row schema verified;actual joined query/audit not executed against SQL here |
| Full backend |2,743/2,743 PASS;zero skips;fresh scoped compile followed by full --no-build on identical source;78 new tests included |
| SQL harness build |PASS,0 warnings/0 errors;no restore,BuildProjectReferences=false;unmodified local harness;no SQL execution or Desktop harness import |
| Release guard |15/15 controls PASS;actual guard exit1 retains five P0 records plus mandatory broader session-security gate |
| Register |120 records:5P0/102P1/13P2;0 formal Closed+Pass;unchanged;not120 untouched bugs or a completion percentage |
| Source preservation |Shared/root trackers,SQL harness,frontend,C38/C39 create/UTC/controller/lifecycle/domain code preserved;shared workflow conditional-only diff verified |
| Retained web evidence |Entire apps/web tree and all578 C39 export hashes match;C39 build/types/lint/84 synthetic Chromium checks and frontend2,606PASS/3 inherited receipt FAIL remain source-bound reuse,not new runs |
| Mini/vendor preservation |Clean Mini/frozen paths untouched;all16 vendor paths exact |
| Native boundary |Desktop C38 report15 new/109 retained groups is read-only inherited evidence;C40 positive search/start/update/transfer/quota/audit/race SQL and C41 UI not qualified |
| Hosted CI |Current status unverified;latest actual C31 Actions query was Forbidden;no retry/bypass/dispatch;feature push excluded from verify.yml filters |

Commands:dotnet build apps/api/AcademyDesk.Api.csproj --no-restore
-p:UseSharedCompilation=false(local prototype compile only);
dotnet test --no-restore -p:UseSharedCompilation=false --filter
FullyQualifiedName~PentaEnrollmentLifecycleBrowser with scoped.trx;
dotnet test --no-restore --no-build with full.trx;
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore
-p:BuildProjectReferences=false -p:UseSharedCompilation=false;
node --test QA/tools/production-gates.test.cjs;
node QA/tools/check-production-gates.cjs;node QA/tools/priority-issue-status.cjs;
git diff --check;exact read-only source/export preservation and remote checks.

## Native acceptance and next owners

1. Desktop Sol High:finish C39 live SQL/browser acceptance from published94a4f996,
   including29c12181 UTC correction. Then selectively integrate C40's six source/
   test paths and this handoff;do not overwrite newer Desktop/finance/shared work.
2. On guarded disposable same-store SQL/Identity and a dedicated synthetic pilot
   actor,verify readiness no effects;bounded sorted search(local/historical/tied/
   >10/empty/foreign-corrupt rows),minimal data and one fixed audit without raw
   terms. Inject audit failure and confirm no data response/committed audit/domain
   effect. Validate current role/stamp/tenant/module/maintenance and query refusal.
3. Verify real lifecycle start/replay/GET-missing/expiry/same-key races,shared quota
   limits and required start-audit rollback. Create,finance,onboarding,foreign and
   tampered sessions must fail. Use fresh bearer credentials after advancing the
   test clock so conversation410 is distinguishable from token401,as learned in
   the C38 native run. Do not relax quotas or manufacture fixture authority.
4. Execute real Paused/Completed/Withdrawn/Cancelled/Active/Waitlisted updates under
   existing rules,with exact reason/end-date intent;then transfer with exact source,
   target,date and replacement receipt. Check wrong approval intent/hash/revision,
   stale source changes,current admission/capacity/prerequisites/duplicates where
   existing services enforce them,required-audit rollback and concurrent replay.
   Confirm one domain mutation/audit/receipt and GET-only same-request recovery.
   Retain old legacy/create SQL evidence and do not reclassify it as C40 coverage.
5. Cloud next:separately claim lifecycle status/transfer UI using the new protocol,
   bounded source search and current canonical change. Keep exact review,per-tool
   approval,explicit execution,session-bound recovery and frozen source/target
   bindings. Do not broaden or borrow C39 create sessions. No model calls implied.
6. Production still requires five P0 records plus AUTH-REVOCATION001,current CI/
   security/images/runtime,real SQL/critical/role/device workflows,migration/
   backup/restore/rollback and staging acceptance. Existing Manual acceptance is
   deferred in implementation order,not waived. No honest percentage or launch
   date follows from passing automated counts.

Mini/Plus/Pro remain unqualified;Ultra deferred.90% eligible conversational outcomes
remain unmeasured. Model loading/download/inference/training/activation remains
stopped. No production qualification,main merge or Azure deployment is claimed.
