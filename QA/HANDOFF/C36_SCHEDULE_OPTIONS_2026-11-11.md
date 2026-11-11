# C36 least-privilege scheduling workspace — 11 November 2026

MODEL:Sol High for authority/read intent;Medium for settled UI/docs.
TOOL:Cloud Codex. REPOSITORY:AcademyDesk/AcademyDesk.
BRANCH:codex/cloud-c36-schedule-options-20261111.
BASE:c2a49077685203a7dfe74a62ba76db14a71d4eb0,clean published C35.
DESKTOP latest f708879fc09a40a540c5d6f0fd5b3e814507bed4 already preserved;
Desktop owns protected invoice issuance. No overlap.
STATUS:CLOUD VALIDATED;source/results and native acceptance handoff below.
TASK:scheduling subset BUG-FUNC-0018;preserve existing scheduling domain rules.

Exact MAY MODIFY paths(10):

- apps/api/Controllers/ClassSessionsController.cs — GET options/minimal DTOs only
- apps/web/src/app/schedule/page.tsx — scoped options,read readiness/pending cleanup
- tests/AcademyDesk.Api.Tests/ScheduleOptionsAccessTests.cs
- QA/tools/schedule-options.test.cjs
- QA/tools/schedule-options-browser.cjs
- QA/tools/schedule-defaults.test.cjs — scoped fixtures/hooks only;retain payload/default assertions
- QA/ISSUES/BUG-FUNC-0018.md
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md
- QA/HANDOFF/C36_SCHEDULE_OPTIONS_2026-11-11.md

MUST NOT MODIFY:permission/filter/module/Identity/session policy,existing server
List/Create/Update actions,admission/clash/teacher inheritance/scheduling services/
SQL/migrations,teacher/attendance/host/finance/Desktop invoice work,other pages,
globals/vendor/Mini,existing assertions/raw evidence. No main/Azure/environment
publication/public access/model loading/download/inference/training/activation.
Mini/Plus/Pro unqualified;Ultra deferred.

Policy:GET sessions/options inherits scheduling.manage/current global academy/
tenant/actor/module/maintenance gates. Return only batch id/name/teacherId/branchId,
teacher id/firstName/lastName and branch id/name,no tracking/writes/audit,no-store,
scoped/sorted. Default references must be local;foreign references project null.
Preserve default/override and IST→UTC intent,all existing server mutation rules.
No full batch/workforce/MultiBranch management grant or fallback. Validate the
workspace and gate writes/empty register until fresh reads succeed;preserve drafts,
reject stale/unmounted reads and duplicate pending writes,release busy state on
rejection,keep useful refusal/confirmed-vs-readback notices. No automatic replay
or new server concurrency policy;broader uncertain-write recovery remains separate.

VALIDATE:original/final actual HTTP/Identity/InMemory role/direct/custom-grant/
revocation/foreign/inactive/maintenance/Core/minimized/default/no-write matrix,
actual page read/race/failure tests,all18 retained scheduling-default controls,
native sandboxed Chromium320/1440 delegated profile/scoped reads/defaults/IST/
create/status/empty/refusal;exact-lock types/build/lint/full frontend/API/guard.
Retain genuine missing SQL receipts;no native SQL/device qualification.
STOP WHEN:scoped source/results/handoff feature-pushed and exact remote SHA verified.
NEXT ROUTE:Desktop Sol High real SQL/Identity/browser/device acceptance;Cloud next
separately claimed promotions/makeup/events scope. Model-loading stop remains.

## Completed source and ownership return

STATUS:IMPLEMENTED / CLOUD VALIDATED;native SQL/device/release acceptance OPEN.
Implementation:`2bf471d3bf1e99d5fd759e991bad9242847b668c`(six source/test paths).
Claim:`1e1513315560fe859c27413d294bb74f047494be`,published and remote-verified
before implementation. Final documentation/publication SHA is recorded after
push in private publication.json and the final response;this commit cannot contain
its own SHA. All ten claimed paths are accounted for.

Desktop was fetched again before final validation;actual published tip remains
`f708879fc09a40a540c5d6f0fd5b3e814507bed4`,already integrated by C35 at17b74f1.
Its13 payment/receipt/handoff paths remain byte-exact. Desktop retains protected
invoice issuance ownership;no shared root handoff/tracker edit or overlapping
invoice/payment/PENTA/SQL/migration change. Mini remains clean at
14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8;33 frozen paths untouched.16 vendor
files(including12 upstream pins)are byte-exact to the published C35 base.

No main merge,Azure deployment,PR/workflow dispatch,environment publication/
replacement/deletion/public-access setting change,paid compute,package install,
model download/loading/inference/training/activation. No laptop/TCP/D: readiness
claim. The runtime worked during this packet;the initial network status was
unknown,so this report does not assert fresh provider enforcement/configuration
verification. Private access requirements remain in force.

## Delivered behavior

GET api/academies/{academyId}/sessions/options stays inside the existing
ClassSessionsController scheduling.manage/current global academy/tenant/actor/
module/maintenance pipeline. Minimal no-store/no-tracking projection is exactly
batches[{id,name,teacherId,branchId}],teachers[{id,firstName,lastName}],
branches[{id,name}]. Every record is current-academy scoped;batch defaults are
projected only when their related teacher/branch belongs to that same academy.
Missing/foreign defaults become null. Historical local labels remain available;
no private contact/address/compensation data or new management authority.
Ordering uses label fields and id for deterministic ties. These separate reads
are not a transactional snapshot or reservation. Existing List/Create/Update and
request/response bodies are byte-exact;write-time scheduling services retain all
admission,clash,inheritance,transaction and mandatory audit enforcement.

The page loads scoped options plus the existing session register with no full
management fallback. It validates required shapes/unique ids/local default refs,
register dates/mode/status and optional fields before committing either read.
Loading,error and malformed reads cannot appear as an authoritative empty
schedule. Generation/current-academy checks retire stale callbacks;late reads and
write responses cannot update an unmounted page. The page has no academy switcher;
existing session replacement/global authentication behavior is retained.

GET-only reload preserves dates,deliberate overrides and a deliberately cleared
batch choice. Existing batch default switching and IST→UTC conversion remain;
status updates keep the exact original start/end/mode/room/status payload without
teacher/branch replacement. A synchronous pending guard prevents duplicate and
opposing starts;form/status/reload controls remain disabled while pending,with
finally cleanup on refusal/loss. Definite server refusals retain their message;
network/5xx responses are unconfirmed. Confirmed success remains visible after
readback;failed readback disables further changes until reload. Broader
unknown-write recovery/session acceptance remains separate. No automatic retry,
replay or new server concurrency policy is introduced.

## Actual validation

Raw evidence remains private,outside Git:
`/workspace/c36-schedule-options-20261111`. It contains original/final logs,TRX,
Chromium screenshots/results,source/assembly/export hashes and preservation/
publication receipts. No hidden skip,weakened assertion or fabricated SQL receipt.

| Check | Actual result |
| --- | --- |
| Original HTTP/Identity/InMemory |38 cases:3 PASS/35 FAIL;missing options GET contract,not35 independent defects |
| Original actual-page controls |21 cases:8 PASS/13 FAIL;strict scoped transport and new readiness behavior absent |
| New HTTP scope |38/38 PASS;role/custom/direct grants,current-bearer revocation,expired/revoked/wrong/foreign grants,owner flag,anonymous/inactive/maintenance,Core-only access,minimal schema/local defaults/history/foreign omission/no-store/no writes or audit |
| Real HTTP create flows |Operations,Manager and custom scheduling.manage read scoped defaults then POST through existing scheduling service;persisted teacher/branch and audit verified while full teacher read remains403 |
| Full API |2,597/2,597 PASS;zero skips;fresh scoped compile,then full --no-build on identical API/test source;InMemory is not SQL transaction proof |
| Actual-page checks |26/26 new scoped/default/IST/status/readiness/refusal/pending/readback/draft/reload/unmount checks PASS |
| Retained default assertions |18/18 PASS;assertion bodies byte-exact;fixture alone now serves scoped options and stable useRef hooks |
| Native Chromium |32/32 PASS;Operations/custom delegated synthetic profile ×320/1440 ×create/denied/malformed/empty/readback/defaults/override/pending |
| Native writes |20 explicit POSTs and4 status PUTs;exact IST/default/override/status payloads checked;no unrelated full lookup request |
| Native resources |Sandbox enabled;owned c36 profile removed;no horizontal overflow;synthetic API transport/persistence only,not live Identity/SQL or physical-device proof |
| Exact-lock production build |Cached Next16.4.3/React19.2.8 optimized webpack export PASS;87 aliases/571 files;no dependency install |
| Types/lint |TypeScript PASS;lint0 errors/61 inherited warnings;initial ref-in-render lint errors corrected and final checks rerun |
| Full frontend |93 files;2,503 tests:2,500 PASS/3 FAIL/zero skips or cancellations |
| Exact frontend failures |Only communication-drafts45/48;missing Cloud QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log;historical Desktop receipt not fabricated |
| Release guard |15/15 controls PASS;real guard exit1 preserves five P0 issue records and broader-session mandatory gate |
| Issue register |120 records:5P0/102P1/13P2;0 formal Closed+Pass;not120 untouched defects or a completion percentage |
| Hosted CI |Current status unverified;latest actual C31 Actions query returned Forbidden;no retry/bypass/dispatch;feature push excluded from verify.yml push filters |
| Preservation |Existing server actions/DTOs,18 retained assertion bodies,16 vendor files,13 Desktop paths and clean Mini verified |

Commands:dotnet test --no-restore -p:UseSharedCompilation=false --filter
FullyQualifiedName~ScheduleOptionsAccessTests;dotnet test --no-restore --no-build
with retained TRX;node --test QA/tools/schedule-options.test.cjs
QA/tools/schedule-defaults.test.cjs;QA_BROWSER_EXECUTABLE=/usr/bin/chromium node
QA/tools/schedule-options-browser.cjs;node QA/tools/run-frontend-tests.cjs;
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack;
tsc --noEmit;npm run lint;node --test QA/tools/production-gates.test.cjs;
node QA/tools/check-production-gates.cjs;node QA/tools/priority-issue-status.cjs
--write;git diff --check. Final raw evidence seals exact checked source.

## Prioritized remaining acceptance / next owners

1. Desktop Sol High:selectively integrate the six implementation/test paths and
   this handoff,preserving protected invoice/payment work. On isolated actual SQL
   and Identity verify correlated default projection translation,local/foreign/
   missing/history/tied labels,no writes/audit,minimal data,current role/grant
   revocation,Core-only operation and existing full management denial.
2. Desktop:real authenticated SQL-backed Operations/Manager/custom scheduling
   browser:assigned/unassigned/partial/default override switches,IST boundaries,
   create/status and audit,authority/eligibility/clash change after lookup,
   required-audit rollback,read failure/readback/draft reload/late/logout/session
   replacement,unknown-write recovery,real devices/other engines and linked
   critical cases. Synthetic browser and InMemory results do not close these.
3. Cloud next:separately claim promotions/makeup/events lookup scopes under0018;
   retain completed enrollment/batch/attendance/scheduling fixes. Broad0018 stays
   OPEN. Desktop retains protected invoice issuance;avoid overlapping paths.
4. Release priorities remain five P0s:DATA0001/0002 finance/reminder/family/currency/
   permission agreement;DATA0003 positive-net payroll identity/history/print;
   DATA0010 audited void/evidence restore;SEC0001 private media authorization/
   recovery/capture/devices. AUTH-REVOCATION001 broader SECURITY-SESSION001 is an
   additional mandatory release scenario across remaining roles/devices/critical
   cases. Existing unit counts are not final acceptance or issue closure.
5. Release integrator:exact-release hosted CI,current security/dependency audits,
   combined images/runtime,migration/backup/restore/rollback and staging proof.
   Keep genuine receipt failures until matching original input is available.
   No main/Azure deployment follows this feature publication.

Mini/Plus/Pro remain unqualified;Ultra deferred.90% eligible conversational
outcomes remain unmeasured. Deterministic Testing-only host checks are not real
model qualification. Candidate loading/download/inference/training/activation
remain stopped. Non-AI/native/security/release work is still pending.
