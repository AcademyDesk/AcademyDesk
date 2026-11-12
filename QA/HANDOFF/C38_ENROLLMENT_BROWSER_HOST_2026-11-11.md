# C38 protected enrollment browser host — 11 November 2026

OWNER:Cloud Codex,Sol High for authority/session/transaction boundaries.
STATUS:CLOUD VALIDATED;native SQL/browser acceptance open. BRANCH:codex/cloud-c38-enrollment-browser-host-20261111.
BASE:4c4382ff1cd5fc08e7ccd1c53af7146f29291fb2,clean published C37.
Desktop def038296da5c6b7d54f9d551e090c57daf535b0 imported with provenance at
63aa2b580332bd90a06e0f736e01b3761a0171e5. Desktop owns adjustment review/recovery.

Exact MAY MODIFY paths(7):
- apps/api/Controllers/PentaOnboardingController.EnrollmentBrowser.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentCreationWorkflow.Browser.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentBrowserSession.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentCreationWorkflow.cs(strict browser checks only)
- tests/AcademyDesk.Api.Tests/PentaEnrollmentBrowserSessionTests.cs(new)
- tests/AcademyDesk.Api.Tests/PentaEnrollmentBrowserAccessTests.cs(new)
- QA/HANDOFF/C38_ENROLLMENT_BROWSER_HOST_2026-11-11.md

DELIVER:Testing-only/default-disabled browser-readiness,explicit purpose-bound
conversation start,GET start recovery and strictly bound create draft/read/approve/
execute routes. Browser routes require both existing enrollment enablement and
Penta:EnrollmentBrowserContractsEnabled. Reuse existing enrollment host/domain/
audit/transaction services;new sessions bind scope/version/expiry/security stamp.
Quota/idempotency/start audit remain atomic;readiness never creates or reserves.
Use separate browser routes and a conditional strict path so existing legacy
routes/drafts/receipts retain their behavior. No cross-purpose legacy session
accepted by the browser contract. Create only;status/transfer UI follows later.

MUST NOT MODIFY:Desktop finance/adjustment/payment/invoice/PENTA shared workflow,
legacy controller actions/enrollment fields/domain services,SQL harness or native
fixtures/migrations/permissions/Identity/session policy/frontend/vendor/Mini,
root shared handoffs/tracker,original evidence/assertions. No main/Azure/
environment publication/replacement/deletion/public access,packages/paid compute,
model download/loading/inference/training/activation or workflow dispatch.

VALIDATE:real Identity/HTTP availability/role/scope/malformed/non-SQL refusals,
cryptographic purpose/tenant/actor/stamp/version/expiry/pending tamper controls,
legacy source preservation,combined API and SQL harness compile,release guard,
Desktop/native task matrix/source hashes. InMemory cannot prove SQL start/write/
quota/audit rollback/races;Desktop owns that positive native acceptance before
browser integration/production activation. Model-loading stop remains.
STOP:publish bounded source/results and exact remote SHA;no production qualification.
NEXT:Desktop native host SQL validation;Cloud next bounded enrollment review/
approval/execute/receipt browser using this contract. No frontend write is enabled
by this host packet;C37 remains read/preview only until its separate integration.

## Completed source and publication return

STATUS:IMPLEMENTED / CLOUD VALIDATED;positive native SQL and browser integration OPEN.
Implementation:`9abd3f6d486ac1c8cec809c4d24551492967430d`(six source/test paths).
Claim:`c1370de9097ceaff5b6817c53ae68e3f191d2ff4`,feature-pushed and exact remote
SHA verified before implementation. Final documentation/publication SHA is
recorded after push in private publication.json and the final response;this
commit cannot contain its own SHA. All seven claimed paths are accounted for.

The usage-limit approval failure from the previous turn cleared:a fresh approved
Git fetch succeeded. Desktop's verified source is def038296da5c6b7d54f9d551e090c57daf535b0,
imported with provenance at63aa2b580332bd90a06e0f736e01b3761a0171e5.
The final pre-validation fetch returned the same tip. All13 imported paths remain
byte-exact. Desktop owns adjustment proposal review/submission/receipt recovery.
Shared root handoffs retain Desktop's update;no overlapping finance/SQL work.

Runtime was current/running/connected with restricted network policy enforced.
No environment settings/publication/replacement/deletion/public-access change,
main merge,Azure deployment,PR/workflow dispatch,new package/paid compute,model
loading/download/inference/training/activation or laptop/TCP/D: access claim.
Mini remains clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8;33 frozen paths
untouched.16 vendor files(including12 upstream pins)are byte-exact to C37.

## Registered browser contract

Root:`/api/academies/{academyId}/penta/enrollments`.
All actions inherit the authenticated Identity.Bearer controller and current
global academy/tenant/module/maintenance pipeline. Availability requires Testing,
Penta:Enabled,Penta:EnrollmentCreationEnabled and the new
Penta:EnrollmentBrowserContractsEnabled flag. The new flag defaults false and
was not enabled in environment/appsettings. Production and Development cannot
activate this contract by flags alone. It never reports or invokes model readiness.

| Method/path under root | Contract |
| --- | --- |
| GET /browser-readiness |Current supported authority and real linked SQL transaction required;returns protocol=enrollment-create-browser.v1,requiresApproval=true;no session/draft/domain/audit write |
| PUT /browser-conversations/{startRequestId} |Body exactly{};explicit idempotent start;returns conversationId,version0,expiresAtUtc;201 new or200 same verified session |
| GET /browser-starts/{startRequestId} |Read-only recovery of that actor/academy/start key;missing404,expired410;never creates a session |
| PUT /browser-conversations/{conversationId}/drafts/{requestId} |Exactly contextVersion0 and typed create payload;prepare current host-reviewed enrollment draft |
| GET /browser-conversations/{conversationId}/drafts/{requestId} |Read current same operation/status/verified receipt |
| POST same draft /approve |Exactly revision,payloadHash,intent=ApproveEnrollmentCreate;distinct explicit approval |
| POST same draft /execute |Exactly revision,payloadHash,approvalId;execute the same approved enrollment through existing service |

Enabled responses are no-store/no-cache through the existing controller safety
wrapper. IDs/hashes/bodies are bounded and exact. No lifecycle update/transfer,
arbitrary tool/SQL,client-defined authority or automatic approval/replay is added.
Browser prepare semantic validation follows current pilot authorization;an
unsupported actor cannot substitute a validation error for the authority refusal.

## Purpose,transaction and compatibility boundaries

The start key is a deterministic SHA-256-derived Guid scoped by fixed enrollment-
browser purpose,academy,actor and caller retry key. Raw request IDs are not global
session IDs. New sessions keep the independent Mini ProtectedState untouched and
store only a protected current security stamp in ProtectedOnboardingState.
Data-protection purpose binds session id,academy,actor,version0 and expiry ticks;
pending requests,changed stamp/metadata,missing/tampered or other-purpose cursors
are refused. This binding is not a permission,approval or expiry authorization.
The host separately checks the current clock,role/module/tenant and domain rules.
SQL's Unspecified DateTime kind preserves the same UTC ticks for verification.

Readiness/start/recovery use the existing same-store linked Academy/Identity SQL
transaction. CurrentHostRefusal holds the current academy/identity membership
boundary;browser enablement is rechecked after that boundary. Start counts the
existing shared UTC-day limits(100 per academy,20 per actor)under that lock.
Session plus mandatory PentaEnrollmentConversationStarted audit commit together;
replay reads the existing protected session without consuming a second quota/audit.
A missing GET never creates one. Expired/invalid sessions are not silently replaced.
No new schema,migration,finance policy/shared workflow or admission logic is added.

The original enrollment controller and lifecycle/domain service/SQL fixture files
are byte-exact. The existing workflow adds only an optional browser=false branch
parameter and conditional strict checks. Removing those additions reproduces C37
byte-exactly. Legacy callers retain their original routes and behavior,including
existing saved drafts,approval and receipt recovery;no data is migrated or deleted.
New browser callers require an enrollment-specific purpose-bound session and only
create bodies;other legacy/finance/onboarding/Mini sessions do not become valid
browser sessions. Existing create/admission,approval hash/revision binding,
transaction,audit and committed receipt implementation is reused.

## Actual validation and limits

Private evidence remains outside Git at
`/workspace/c38-enrollment-browser-host-20261111`:initial/final logs,TRX,new-case
list,source/assembly hashes,preservation and publication receipts. No new SQL
receipt is fabricated and no original evidence/assertion is replaced.

| Check | Observed result |
| --- | --- |
| Initial focused run |55 cases:52 PASS/3 FAIL;two typed-fixture serialization mistakes and Development startup's normal seeder credential prerequisite,not three production defects |
| Corrected focused run |57/57 PASS;exact typed field casing restored;Development gating tested directly before any store dependency while actual Production HTTP coverage remains |
| Final new scope in full run |58/58 PASS:24 real data-protection/key/scope/tamper controls and34 availability/Identity/HTTP/schema controls(includes one Development gate unit) |
| HTTP boundaries |All seven routes independently disabled by pilot/enrollment/browser/missing-browser flags,Production refusal,unrelated roles,foreign scope,inactive actor/academy,maintenance,real owner flag,same-bearer role removal,malformed and create-only intents,no-write snapshots |
| Supported actors in Cloud store |Owner/AcademyAdmin return503 with no writes against InMemory;this intentionally proves fail-closed SQL prerequisites,not successful SQL start or execution |
| Full combined API |2,655/2,655 PASS;zero skips;fresh compile and full.trx;58 new cases included with Desktop def0382 |
| SQL harness compilation |PASS,0 errors/0 warnings;existing cached dependencies,no restore/package install;BuildProjectReferences=false keeps the tested API assembly |
| Release guard |15/15 controls PASS;actual guard exit1 retains five P0 records plus mandatory broader session-security gate |
| Issue register |120 records:5P0/102P1/13P2;0 formal Closed+Pass;no issue metadata changed;not120 untouched bugs or a completion percentage |
| Legacy preservation |Old controller,lifecycle/domain service and native fixture byte-exact;workflow conditional-only diff verified |
| Desktop/vendor/Mini |13 imported paths and16 vendor files verified;clean Mini/frozen paths untouched |
| Frontend evidence reuse |Entire apps/web tree and all578 C37 export-file hashes match;no frontend change or new browser write UI in C38 |
| Retained C37 frontend result |2,547 PASS/3 inherited missing-SQL-receipt FAIL of2,550;47 assistant+93 entry checks and32 synthetic native Chromium cases;not rerun this API-only packet |
| Retained C37 build |Exact-lock Next16.4.3/React19.2.8 export,types and lint0 errors/61 warnings;88 aliases/578 files;source-bound reuse,not a new build |
| Not established |Positive native SQL readiness/start/replay/quota/audit rollback/races/new strict-route execution or live browser/device/model acceptance |
| Hosted CI |Current status unverified;latest actual C31 Actions query returned Forbidden;no retry/bypass/dispatch;feature push is outside verify.yml push filters |

Commands:dotnet test --no-restore -p:UseSharedCompilation=false --filter
FullyQualifiedName~PentaEnrollmentBrowser for initial/focused runs;full dotnet
test --no-restore -p:UseSharedCompilation=false with full.trx;
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore
-p:BuildProjectReferences=false -p:UseSharedCompilation=false;
node --test QA/tools/production-gates.test.cjs;
node QA/tools/check-production-gates.cjs;node QA/tools/priority-issue-status.cjs;
git diff --check;exact source,web/export and remote-preservation checks.
No SQL host,customer data,Docker service,model or external audit upload was used.

## Exact native and AI-first continuation

1. Desktop Sol High:selectively integrate six implementation/test paths and this
   handoff,preserving newer finance adjustment work. Add a separately claimed
   native enrollment-browser regression;do not replace the old Session() fixture
   or weaken legacy tests. Use a dedicated synthetic supported actor and guarded
   disposable same-store Academy/Identity SQL fixture with the three feature flags.
2. Verify readiness has zero session/draft/domain/audit effects;real HTTP start
   creates exactly one correctly bound session/start audit. GET recovery and
   same-key PUT replay return the same session with no second audit. Race a pair
   of identical starts. GET-missing returns404 without creating anything.
3. Exercise actor/academy daily quotas independently with dedicated fixtures;
   expired start/recovery return410 without replacement. Inject required start-
   audit failure and confirm session/audit rollback. Check current role/stamp/
   tenant/module/maintenance refusals,flags removed after boundary,foreign/legacy/
   finance/onboarding sessions and tampered purpose/version/expiry/pending state.
4. Run a real start→typed Active/Waitlisted prepare→exact approval→explicit execute
   →GET receipt flow. Cover current capacity/prerequisite/duplicate/closed-batch
   changes after review,domain/audit rollback,concurrent same-request execution,
   one enrollment/audit/receipt,loss before dispatch/after commit and GET-only
   recovery. Retain existing legacy create/lifecycle/receipt SQL results separately.
5. Cloud next,Sol High:claim the bounded C37 enrollment browser review/approval/
   execute/recovery UI. Use only the new browser routes/readiness;never borrow a
   finance/onboarding session or use fixture-created sessions. Persist only a
   session/academy-bound recovery pointer before a possible write. Match canonical
   admission/hash/revision/approval/receipt;reload is GET-only and retries explicit
   on the same approved key. Keep PreviewOnly separate and preserve Manual access.
6. Desktop retains adjustment review/submission/recovery ownership. Subsequent
   Cloud AI packets cover enrollment lifecycle and remaining protected scheduling/
   makeup/attendance UI. These are not completed by the new host start contract.
7. Production remains blocked on five P0 records plus AUTH-REVOCATION001,current
   hosted CI/security/images/runtime,real SQL/device/critical workflows and
   migration/backup/restore/rollback/staging acceptance. Existing broader Manual
   acceptance is deferred in implementation order,not waived. No launch date or
   completion percentage follows from automated test counts.

Mini/Plus/Pro remain unqualified;Ultra deferred.90% eligible conversational outcomes
remain unmeasured. Explicit model loading/download/inference/training/activation
stop remains. No main/Azure deployment follows this feature publication.
