# C34 least-privilege batch setup lookups — 11 November 2026

MODEL: Sol High for authority/read isolation;Medium for settled UI/docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH:codex/cloud-c34-batch-options-20261111.
BASE:6d508ee6180e39ad3e3c4e1d2f9a9db00426c42a,clean published C33.
DESKTOP:d5f91d344eb5f9706733907d79cbbe2a52685ff1 and
525f5ad365140a47eff61b40e511df9a66e3ec87 imported with provenance;Desktop owns
protected payment collection next. No overlap with batch setup lookup paths.
STATUS:IMPLEMENTED / CLOUD VALIDATED;native acceptance and release remain OPEN.
TASK:batch/setup subset BUG-FUNC-0018;preserve existing batch mutation contracts.

Exact MAY MODIFY paths(10):

- apps/api/Controllers/BatchesController.cs — GET options/minimal DTOs only
- apps/web/src/app/batches/page.tsx — options consumer,valid read readiness/scope
- tests/AcademyDesk.Api.Tests/BatchOptionsAccessTests.cs
- QA/tools/batch-options.test.cjs
- QA/tools/batch-options-browser.cjs
- QA/tools/batch-preservation.test.cjs — valid loaded/scoped fixture only;all payload assertions retained
- QA/ISSUES/BUG-FUNC-0018.md
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md
- QA/HANDOFF/C34_BATCH_OPTIONS_2026-11-11.md

MUST NOT MODIFY:permission/filter/module/Identity/session policy,existing batch
mutation/domain/scheduling/teacher-inheritance/admission/SQL/migrations,finance/
PENTA host/new Desktop chat,other workflow pages,globals/vendor/Mini or prior
raw evidence. No main/Azure/environment publication/public access/model loading/
download/inference/training/activation. Mini/Plus/Pro unqualified;Ultra deferred.

Policy:GET batches/options inherits existing batches.manage and current global
academy/tenant/actor gates. Return only courses(id,name,academyType),teachers
(id,firstName,lastName),branches(id,name),scoped/sorted/no-tracking/no-store.
Historical inactive labels remain;lookup availability never authorizes targets
or adds full management access. Core batch workflow needs these minimal labels
without unrelated academics/workforce/MultiBranch management calls;existing
server membership/eligibility and module policy are unchanged. Client consumes
options+batch register without fallback,rejects malformed/partial/duplicate ids,
invalidates read readiness while loading/failed/changing academy,and ignores
obsolete/unmounted read results. Failed reads do not render authoritative zero
metrics/empty directory or enable direct writes. Preserve payloads/hidden fields,
selectors,drafts and existing mutation outcomes. Broader uncertain-save recovery
and remaining0018 schedule/attendance/promotions/makeup/events remain separate.

VALIDATE:original/final actual HTTP/Identity/InMemory Operations/Manager/Owner/
Admin/custom/direct-grant/revoked/expired/foreign/inactive/maintenance/empty/minimal/
Core-only/no-write and full management denial. Actual page read/race/draft tests,
retained16 batch preservation controls,native sandboxed Chromium at320/1440 for
/batches and /batch-setup with synthetic delegated API,exact-lock types/build/
lint/full frontend/API/release guard. Preserve genuine SQL-receipt failures.
STOP WHEN:source-bound handoff and feature publication verified;no issue closure
or production/native SQL/device qualification inferred.
NEXT ROUTE:Desktop Sol High for native SQL/Identity/browser/device acceptance;
Cloud next independently claimed non-AI scope. Model-loading stop remains.

Claim extension before fixture edits: add QA/tools/batch-feedback.test.cjs and
QA/tools/batch-times.test.cjs (12 paths total). The first full run found their
preloaded fixtures omit the new successful-read scope and old refresh routes.
Seed valid loaded state and scoped options,retain all existing payload/feedback/
pending/no-write assertions;update exact refresh request expectations to the two
new endpoints. No-academy tests must use the real context-change handler and a
retained callback so the new unavailable-directory guard is exercised. Original
failures remain private. No implementation weakening to satisfy old fixtures.

## Exact source and ownership return

Implementation:`e93ed8d7cdfa60b3d8a23426dbd852f0a85d8526`(eight source/test paths).
Initial claim was feature-pushed/remote-verified before editing:
`cfee08edfecd27edfc38af4baa5634cf6a4eb27b`.
Fixture extension was published before those edits at
`745b8d8` on the same feature branch. The original ten-path list plus the two
retained fixtures is the final12-path Cloud-authored scope.

Desktop imports with provenance:expense/payroll reads d5f91d3→`a401f7e`;
finance chat525f5ad→`22f8c51`. During C34,Desktop published protected payment
collection `fa2e19d30504734a4790341f3546d64e0b073836`;imported at
`c87a8535f91f6139e2ec07198c729e9c4c82068b`.29 distinct paths from these three
returns were byte-compared to Desktop's latest source. Its next task is the
Admin payment approval/recovery browser flow. No Cloud edits to those paths.
Shared root handoffs retain Desktop's changes;this separate packet is the return.

Final local/remote SHA and clean status are verified after feature push and
sealed privately in publication.json plus the final response. A documentation
commit cannot contain its own SHA. No main/Azure/environment publication,
replacement/deletion,public access,repository settings,PR/workflow dispatch,
paid compute or model action. Environment execution recovered in the existing
instance:current observations,running/connected,failure=null,restricted network
enforced. The prior capabilities-query failure is not attributed to application
code without diagnostics. No current laptop/TCP/service/D: readiness is claimed.
Mini remains clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8,33 frozen paths
untouched.16 web vendor paths,including12 upstream pins,match C33 exactly.

## Behavior and limits

GET batches/options lives in the existing controller and retains batches.manage,
current identity/academy/tenant/module/maintenance and owner policy. Current custom
role or direct-grant revocation applies to the same bearer. Wrong/expired/revoked/
foreign grants,unrelated roles and a non-owner PlatformOwner role label are denied.
No permission/filter/Identity/module rule is modified. Existing course/teacher/
branch management APIs stay forbidden to Operations/Manager. Core-only batch
workflows may use minimal labels without enabling unrelated management modules.

No-store,no-tracking queries return only courses[{id,name,academyType}],
teachers[{id,firstName,lastName}],branches[{id,name}],scoped to the route academy
and sorted by labels then id. No compensation,contact,branch address or unrelated
private data. Historical/inactive teacher/branch labels remain to identify earlier
batch references. Labels are not admission,teacher/branch eligibility or mutation
authority;existing server validation and current module rules remain authoritative.
The three reads add no new transaction or atomic-snapshot promise.

Both /batches and /batch-setup share the same page. The page now requests only
options+batch register,validates all arrays/required labels/unique ids and batch
summary fields before committing the read. No broader-endpoint fallback. Current
academy key and read generation invalidate pending/obsolete actions across
refresh,failed reads and academy changes. Late or unmounted reads cannot update
the view. Unavailable reads hide zero KPIs/directory counts and disable mutations.
The academy selector stays usable on lookup refusal. Reload preserves the entered
batch draft;an explicit academy change clears scoped course/branch selections.
Create requires a currently available course. Full action bodies/DTOs,hidden
replacement settings,scheduling/admission locks,substitute inheritance and audit
policy are retained. Broader unconfirmed-write recovery,optimistic concurrency,
and all mutation-result unmount cases are outside this read-only lookup packet.

## Actual verification

Private raw output:`/workspace/c34-batch-options-20261111`,outside Git. Includes
originals,failed/intermediate/final logs and TRX,screenshots,source/artifact hashes
and preservation receipts. No original failure is overwritten or published as a
pass. No skipped/softened test or fabricated SQL receipt.

| Check | Observed result |
| --- | --- |
| Original HTTP/Identity/InMemory |34 cases:3 PASS/31 FAIL;absent GET contract;not31 independent defects |
| Original actual-page controls |0/21 PASS;new route/readiness/reload controls absent;not21 independent bugs |
| Focused API after implementation |106/106 PASS:34 new lookup and72 retained batch/enrollment admission checks |
| Full API before latest Desktop payment import |2,521/2,521 PASS;zero skips |
| Combined full API after fa2e19d |2,521/2,521 PASS;zero skips;fresh compile/full run;not native SQL acceptance |
| Actual-page scoped controls |21/21 PASS:both routes,only scoped calls,denial/malformed/empty,drafts,read race and unmount |
| Retained batch controls |16/16 complete replacement preservation,44/44 feedback/pending/rejection/readback,6/6 schedule payload cases PASS |
| Native sandboxed Chromium |32/32 PASS:2 roles ×2 routes ×320/1440 ×loaded/denied/malformed/empty;synthetic API only |
| Native writes |Four loaded setup cases each create,rename and deactivate;12 intended writes,exact retained update payloads;no course/teacher/branch management request |
| Native resources/visuals |Sandbox enabled;owned profile removed;mobile/desktop no page overflow;actual overview detail dialog labels inspected |
| Exact-lock build |Cached locked Next16.4.3/React19.2.8 optimized webpack export PASS;87 aliases,571 files;no install |
| Types/lint |TypeScript PASS;lint0 errors/61 retained warnings |
| First full frontend |2,407 PASS/51 FAIL/2,458 tests;48 old-fixture failures in feedback/times plus3 missing SQL receipts;retained |
| Final full frontend |91 files;2,455 PASS/3 FAIL/2,458 tests;zero skips/cancellations |
| Exact remaining failures |communication-drafts45/48 only;missing Cloud QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log;historical Desktop receipt not fabricated |
| Production guard |15/15 controls PASS;real guard exit1 retains five P0 records plus mandatory broader-session acceptance |
| Issue inventory |120 records:5P0/102P1/13P2;0 formal Closed+Pass;not120 untouched defects or a completion percentage |
| Hosted CI |C34 unverified;latest Actions query in C31 returned Forbidden,not bypassed/retried;feature push is outside verify.yml push filters;no dispatch/PR |

The fixture extension seeds a successful read context instead of allowing writes
from unloaded state. Its options responses match the new contract;times still
asserts exact two-route readback and all payload fields. Feedback retains complete
notices,pending/opposing-write/no-write assertions;when the directory is correctly
hidden during reload it invokes the previously retained opposing callback. Its
no-academy cases exercise the actual context-change handler and a retained callback,
instead of searching for an edit button on an unavailable directory. No acceptance
assertion is removed. Existing batch-preservation payload assertions are retained.

Commands:dotnet test --no-restore -p:UseSharedCompilation=false(original/focused/
combined),--no-build(first full);TRX retained;node batch-options.test.cjs,
batch-preservation.test.cjs,batch-feedback.test.cjs,batch-times.test.cjs from
QA/tools;QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/batch-options-browser.cjs;
node QA/tools/run-frontend-tests.cjs;tsc --noEmit;npm run lint;
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack;
node --test QA/tools/production-gates.test.cjs;
node QA/tools/check-production-gates.cjs;node QA/tools/priority-issue-status.cjs
--write. No external audit upload or dependency installation. The final Desktop
payment import changes no web source;the build/native web source stays identical.

## Prioritized remaining acceptance / next owners

1. Desktop Sol High:integrate eight C34 implementation/test paths and this report
   selectively,preserving newer payment/finance/PENTA work. On isolated real
   SQL/Identity validate Operations/Manager/custom/direct-grant scoped lookups,
   minimized schema/history/ordering,Core-only behavior,revocation/foreign/inactive/
   maintenance/owner boundaries,no read writes/audit and unchanged full related
   management denial. Retain actual batch create/update/admission/clash/substitute
   and required-audit rollback controls;unit InMemory is not SQL proof.
2. Desktop:actual SQL-backed delegated browser /batches and /batch-setup,lookup
   failures/recovery,scoped academy changes and late reads,create/edit/status
   with hidden settings and teacher inheritance,physical devices/other engines.
   Continue existing uncertain-save/no-replay and broader0012 cases separately.
3. Cloud:next independently claim scheduling/attendance/promotions/makeup/events
   scopes of0018 or remaining recovery gaps;reuse already accepted code/evidence.
   Do not grant broad management access for selectors. Desktop owns protected
   payment approval/recovery UI. C34 closes no broad0018 issue.
4. Release:five P0 records remain open—DATA0001/0002 finance/reminder/family/
   currency/permission agreement,DATA0003 positive-net payroll identity/history/
   print,DATA0010 audited void/evidence restore,SEC0001 private media recovery/
   capture/devices. AUTH-REVOCATION001's broader SECURITY-SESSION001 remains an
   additional mandatory role/device/critical scenario. No bulk issue closures.
5. Integrator:exact-release hosted CI and fresh security/dependency audits,
   combined images/runtime,migration/backup/restore/rollback and staging. Keep
   genuine missing-receipt failures until source-bound input exists. No main/Azure
   action follows feature publication. Non-AI/native/release work remains.

Mini/Plus/Pro remain unqualified,Ultra deferred;90% eligible conversational
outcomes unmeasured. Testing-only deterministic host checks are not model
qualification. Candidate loading/download/inference/training/activation stopped.
