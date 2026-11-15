# C37 PENTA enrollment assistant — 11 November 2026

OWNER:Cloud Codex,Sol High for session/contract boundary;Medium for settled UI/docs.
STATUS:CLOUD VALIDATED;completion/remaining acceptance below. BRANCH:codex/cloud-c37-penta-enrollment-assistant-20261111.
BASE:b60d035d66526792153bcbed1ce18bee9cc575c3(clean published C36).
Desktop681db440355ff28286340bf455a811e8b0b5923d imported with provenance at
b62b076;Desktop owns invoice issuance browser review/approval/recovery next.
User prioritizes AI workflow integration now;broader Manual acceptance may follow.
Model-loading/activation stop and all production release gates remain.

Exact MAY MODIFY paths(9):
- apps/web/src/app/penta/enrollments/page.tsx(new)
- apps/web/src/components/penta/enrollment-assistant.tsx(new)
- apps/web/src/components/penta/enrollment-assistant.module.css(new)
- apps/web/src/lib/penta-enrollment-assistant.ts(new)
- apps/web/src/components/penta/role-penta-entry.tsx(protected workflow link only)
- apps/web/src/components/workspace-frame.tsx(add route to standalone PENTA chrome only;session boundary byte-exact)
- QA/tools/penta-enrollment-assistant.test.cjs(new)
- QA/tools/penta-enrollment-assistant-browser.cjs(new)
- QA/HANDOFF/C37_PENTA_ENROLLMENT_ASSISTANT_2026-11-11.md

MUST NOT MODIFY:Desktop finance/invoice/payment/chat/API/SQL/migration paths,
shared CSS/vendor/Mini/permissions/Identity/session policy,Manual pages,existing
assertions/evidence,root shared handoff/tracker. No main/Azure/environment publish/
replace/delete/public access,model download/loading/inference/training/activation,
new package/paid compute or hosted workflow dispatch.

DELIVER:Admin PENTA conversational student/batch search using existing audited
host tools,explicit source selection and typed enrollment eligibility preview.
No model claimed:finite supported prompts,clear qualified-model-unavailable label.
No enrollment mutation/approval/execution in this bounded read/preview packet.
PreviewOnly requires approval and reserves nothing. Do not invent a conversation
start for protected enrollment writes;that separate host boundary/UI comes next.
Bind academy/role to verified session,freeze bearer/epoch,abort/clear on replacement,
validate bounded JSON/host schemas/source references,never render host prose/URLs,
prevent overlapping requests,preserve Manual fallback,no data in browser storage.

CHECKS:actual new lib/component behavior,malformed/foreign/late/session loss and
PreviewOnly/no-write controls;native Chromium mobile/desktop synthetic host;
exact-lock types/lint/build/full frontend;combined API after Desktop import;
release guard;preservation/source hashes. Native SQL/model/physical-device/hosted
CI/90% conversational outcome acceptance remains open. Publish feature results
and exact next-owner tasks;no issue closure from synthetic or unit totals.

CLAIM AMENDMENT before shared-frame edit:the new assistant uses the same standalone
chrome as existing onboarding routes. Only add /penta/enrollments to that route
list;do not change WorkspaceSessionBoundary,authentication or shared navigation.

## Completed source and integration return

STATUS:IMPLEMENTED / CLOUD VALIDATED;native SQL/model/device/release acceptance OPEN.
Implementation:`99643ad69e1bd7eb99c5eae33561516dead4960a`(eight source/test paths).
Original claim617d308bb3ba611ffea54ec3bcd888f910e0ceae and presentation amendment
f1ea8ce6ec78262ab2a476711f568f20902b1747 were pushed/remote-verified before their
respective edits. Final publication SHA is recorded after push in the private
publication receipt and final response;this documentation commit cannot contain
its own SHA. All nine claimed paths are accounted for.

Desktop published invoice browser work during this packet:
`1d23c77b5ea5e344320041d6c7897bac45bdc0e5`,imported with provenance at
`d4bb797eb198651475a36d6cf7293e1d3e9e65d1`. Initial681db44 was imported at
b62b076. All30 distinct Desktop paths touched by these two commits match the
latest Desktop tree exactly. Shared root handoffs retain Desktop's updates.
Desktop's next packet is protected invoice adjustments;Cloud's academic paths
are separate. C36 scheduling and preceding Cloud returns remain included.

No main merge,Azure deployment,PR/workflow dispatch,environment publication/
replacement/deletion/public-access change,model download/loading/inference/
training/activation or new dependency/version. Runtime was current/running/
connected with restricted network policy enforced. No laptop/TCP/D: readiness
claim. Mini remains clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8,33 frozen
paths untouched;all16 vendor files(including12 upstream pins)are byte-exact.

## Delivered user flow and boundaries

The verified Admin PENTA entry now links to `/penta/enrollments`. The new screen
uses the existing PENTA theme bridge,Prism light/Orbit dark and standalone
workflow chrome. Only one route string was added to WorkspaceFrame;the complete
WorkspaceSessionBoundary and its signout/session replacement policy are byte-exact.
Role entry changes only add the link. No Manual,shared finance CSS,API,SQL,
migration,permission or model configuration was changed by the Cloud implementation.

The assistant accepts bounded commands `Find student <query>` and
`Find batch <query>`,including Unicode names,using existing audited host search
endpoints. It is explicitly limited command support;a qualified AI model is not
connected. It does not simulate arbitrary natural-language understanding or call
an interpreter. Search is an explicit gesture;mount only verifies the authenticated
session. No tool discovery/search/preview or model call is speculative.

The current host session supplies academy and supported Owner/AcademyAdmin role.
Requests freeze the bearer and login epoch,omit cookies,refuse redirects,use
no-store/no-referrer and abort on unmount or session change. No tokens are decoded
for authority. JSON reads use the existing bounded streamed reader;schemas verify
academy/tool/capability/source/timezone/timestamp/row limits/unique source ids and
canonical source paths. Rendering forwards only verified ids/labels and safe
constructed paths,not private extra fields,raw host prose or arbitrary links.
Unsupported prompts,foreign/malformed/refused reads never appear as no matches.
Only an authorized successful empty read gets an empty-result notice.

Users explicitly select a verified student and batch,date and Active/Waitlisted
intent. The only POST is the existing typed enrollment eligibility preview.
The response must match those exact requested fields,PreviewOnly and
requiresApproval=true. It cannot become an approval,execution,receipt or seat
reservation. Host admission rules/current authority/audit remain independent.
The preview operation writes its existing host audit;it creates no enrollment.
Search requires existing Penta:Enabled in Development/Testing;preview additionally
requires Testing and Penta:EnrollmentPreviewEnabled. No flags were enabled here;
these workflows remain unavailable in Production by existing policy.

A synchronous request lock blocks duplicate/overlapping gestures. Failed reads
release busy state. Selection/date/status changes remove obsolete preview notices.
Session loss clears prompt,results,selection,date and preview;late/unmounted
responses cannot repopulate them. The shared session boundary hides the whole
screen on replacement. State is memory-only;no prompts,labels or results go to
browser storage. Manual links remain available. Approval/execution/recovery for
enrollment writes require the next separately claimed host/UI packet.

## Actual checks

Raw evidence remains private outside Git at
`/workspace/c37-penta-enrollment-assistant-20261111`,with initial/final logs,TRX,
synthetic Chromium screenshots/results,source/assembly/export hashes and
preservation/publication receipts. No genuine missing SQL receipt was fabricated.

| Check | Result and scope |
| --- | --- |
| New actual contract/component checks |47/47 PASS;bounded Unicode commands,invalid dates/ids,foreign/malformed/private-extra/duplicate/over-limit sources,exact typed preview,no execution/approval,401/403/404/429/503,empty/refused reads,pending duplicates,session replacement/late/unmounted responses,theme and optional batch code |
| Retained role-entry checks |93/93 PASS;existing assertions unchanged |
| Initial Chromium |28 PASS/4 FAIL;session assertions expected the component notice,but the unchanged global boundary renders Session ended;original results preserved |
| Final Chromium |32/32 PASS;Owner/AcademyAdmin ×320/1440 ×Active preview/Waitlisted/refusal/foreign schema/empty/unsupported/mismatched preview/session replacement |
| Native transport |44 explicit search GETs and16 preview POSTs;zero approval,execution,draft/conversation creation or inference calls;typed payload and exact selection verified |
| Native safety/presentation |Private data removed by the real Session ended boundary;no horizontal overflow;Prism/Orbit attributes and computed canvas colors verified;installed Chromium sandbox enabled and owned profile removed |
| Combined full API after681db44 |2,597/2,597 PASS;zero skips;source-bound InMemory/unit/HTTP evidence,not native SQL proof |
| Combined full API after1d23c77 |2,597/2,597 PASS;fresh compile and TRX;zero skips |
| SQL harness compilation |PASS,0 warnings/errors;initial --no-restore attempt lacked project.assets.json;generated assets using existing cached dependencies with an empty local feed,then compiled without rebuilding project references;no network/package download and no SQL execution |
| Exact-lock production export |PASS,cached Next16.4.3/React19.2.8 optimized webpack build;88 aliases/578 files;no npm install |
| Types/lint |TypeScript PASS;lint0 errors/61 retained warnings |
| Full frontend |94 files;2,550 tests:2,547 PASS/3 FAIL,zero skips/cancellations |
| Only frontend failures |communication-drafts45/48;Cloud lacks QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log;Desktop's historical receipt not fabricated |
| Release guard |15/15 controls PASS;actual gate exit1 preserves five P0 issues plus mandatory broader-session acceptance |
| Preservation |30 Desktop paths,16 vendor paths,clean Mini,old role-entry code and full session boundary verified |
| Hosted CI |Current status unverified;latest actual Actions API query in C31 returned Forbidden;no retry/bypass/dispatch;feature pushes excluded from verify.yml filters |

The native assertion correction retains the privacy/no-replay requirements and
asserts the actual shared Session ended view,absent private inputs,absent previous
names/preview and the sign-in link. It does not change product session policy.
Theme bridge integration was corrected before final export;component and native
checks now validate the real attribute/class contract and computed colors.

Commands:node --test QA/tools/penta-enrollment-assistant.test.cjs
QA/tools/penta-entry.test.cjs;QA_BROWSER_EXECUTABLE=/usr/bin/chromium node
QA/tools/penta-enrollment-assistant-browser.cjs;node QA/tools/run-frontend-tests.cjs;
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack;
tsc --noEmit;npm run lint;dotnet test --no-restore -p:UseSharedCompilation=false
with retained TRX;dotnet restore QA/tools/SqlHarness/SqlHarness.csproj
--source /tmp/c37-empty-feed -p:NuGetAudit=false(offline assets only,not an audit);
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore
-p:BuildProjectReferences=false -p:UseSharedCompilation=false;
node --test QA/tools/production-gates.test.cjs;
node QA/tools/check-production-gates.cjs;git diff --check.

## AI-first continuation and launch blockers

User priority is now AI workflow completion before broader Manual acceptance.
This reorders independent implementation work;it does not waive server safeguards,
Manual parity,AI-inclusive acceptance or release gates. There is no measured launch
date or90% conversational completion figure.

| Priority / owner | Next concrete result |
| --- | --- |
| Cloud next,Sol High |Separately claim protected enrollment conversation start/readiness and browser create review→exact approval→explicit execution→same-request GET receipt recovery. Existing enrollment backend must retain current authority/admission/audit. Do not borrow finance/onboarding conversations or rely on fixture-created sessions. Then connect status/transfer UI with exact reasons and target/source binding. |
| Desktop,Sol High |Protected invoice adjustments under existing Manual review/approval/ledger rules;owns those finance/API/SQL paths. Preserve invoice issuance/payment browser work and their SQL evidence. |
| Cloud after enrollment |Connect remaining protected scheduling/make-up/assigned-teacher attendance browser workflows to existing typed host contracts;claim paths separately. Student/teacher onboarding host/UI already exists;do not rebuild it or equate deterministic tests with model intelligence. |
| Desktop native acceptance |Integrate the eight Cloud implementation/test paths selectively;verify real authenticated SQL-backed student/batch search and PreviewOnly audit/current tenant/role/revocation/capacity/prerequisite/duplicate/empty/refusal behavior,unchanged domain rows and audit rollback. Run actual browser login/logout/session replacement and real devices/other engines. |
| Separate private model evaluation |Mini/Plus/Pro remain unqualified;Ultra deferred. Preserve the explicit loading/download/inference/training/activation stop. Prepare a source-bound approved evaluation packet on the laptop before any actual model run;host/UI source work does not lift that stop. |
| AI acceptance |Measure supported eligible operations and all-role outcomes across Pulse/Executor/Navigator/Twin/Autopilot,including clarification,approval/refusal,AI downtime,receipt recovery,privacy,latency and Manual parity. The90%AI/10%Manual goal is not achieved by counting tool names or test totals. |
| Release integration |Five P0 records(DATA0001/0002/0003/0010,SEC0001)and broader AUTH-REVOCATION001 session gate stay OPEN. Current-source hosted CI/security/audits/images/runtime,migration/backup/restore/rollback,critical scenarios,staging and model qualification remain. No main/Azure launch from this feature push. |

No issue closure or production qualification is claimed. The broad0018 Manual
promotions/makeup/events/native acceptance queue is preserved and deferred in
implementation priority,not erased. No rewrite/audit restart or model activation.
