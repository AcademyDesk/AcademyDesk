# C33 least-privilege enrollment lookups — 10 November 2026

MODEL: Sol High for authorization/data minimization;Medium for settled UI/docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c33-enrollment-options-20261110.
BASE:28be47a5d85f397ea01ab36dbe12a09eb0b5207f (clean published C32).
DESKTOP latest observed241810e488fcd7a1951dd0b341ae1c63a75b77fa already preserved;
Desktop owns protected expense/payroll finance reads. No overlap.
STATUS: IMPLEMENTED / CLOUD VALIDATED;native acceptance and release remain OPEN.
TASK: enrollment subset of BUG-FUNC-0018; preserve C32 lifecycle/recovery.

Exact MAY MODIFY paths (10):

- `apps/api/Controllers/EnrollmentsController.cs` — GET options and minimal DTOs only;all existing actions unchanged
- `apps/web/src/app/enrollments/page.tsx` — consume scoped options;preserve C32 lifecycle/receipt/recovery
- `tests/AcademyDesk.Api.Tests/EnrollmentOptionsAccessTests.cs`
- `QA/tools/enrollment-options.test.cjs`
- `QA/tools/enrollment-lifecycle.test.cjs` — synthetic fixture route/schema only;retain all assertions
- `QA/tools/enrollment-lifecycle-browser.cjs` — strict scoped lookup fixtures/FrontDesk role;retain40 scenarios and no-replay assertions
- `QA/ISSUES/BUG-FUNC-0018.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C33_ENROLLMENT_OPTIONS_2026-11-10.md`

MUST NOT MODIFY: permission catalog/filter/module rules,Identity/session logic,
existing enrollment mutation/domain/admission/host/SQL/migrations,finance,
other workflow pages,globals/vendor/Mini,unrelated work or existing evidence.
No main merge,Azure/environment publication/public access/model download/load/
inference/training/activation. Mini/Plus/Pro unqualified;Ultra deferred.

Policy: GET enrollments/options inherits the same existing students.manage,
current actor/academy/module/tenant checks as enrollment List. Return only student
id/firstName/lastName and batch id/name, sorted deterministically,no tracking/no
writes/no-store. Include historical inactive/closed references to preserve naming;
server admission still controls selectable target validity. Never return contacts,
financial/teacher/branch/private detail or mutate lookup records. No permission
expansion to full batch reads/writes or unrelated management. Platform-owner
behavior matches existing filter policy. Client rejects malformed/partial options,
retains C32 drafts and recovery and never falls back to broader endpoints.
Remaining0018 batch/schedule/attendance/promotions/makeup/events scopes are separate.

VALIDATE: original/final actual HTTP/Identity/InMemory FrontDesk/delegated-grant/
revoked/expired/denied/inactive/foreign/maintenance/owner/empty/minimal/no-write
matrix;exact client route/schema controls;retain all38 C32 controlled and40 native
sandboxed Chromium scenarios at320/1440 with synthetic FrontDesk fixtures;types,
exact-lock build/lint/full frontend/API/release guard. Preserve failures privately;
no fabricated SQL receipts or weakened assertions. No native SQL claim.
STOP WHEN: scoped implementation/checks/source-bound handoff feature-pushed and
remote SHA verified. No automatic issue closure/production acceptance.
NEXT ROUTE: Desktop Sol High actual SQL/Identity/browser/device acceptance;
Cloud next separately claimed non-AI gap. Model-loading stop remains.

## Source, ownership and publication

Implementation: `9e108672b9c1e43f395ac11684b2e8235cb663ff` (six source/test paths).
Claim published and remote-verified before edits:
`f365ad20fb1d088464a494cf0da647272f3519bf`.
Desktop's latest observed tip remains
`241810e488fcd7a1951dd0b341ae1c63a75b77fa`, already included with provenance in
C32. No new import was needed. Desktop still owns protected expense/payroll read
detail and native host/SQL acceptance. Its root handoff/tracker remain untouched.
This separate packet plus issue and both inventory files complete the10-path
claim. Final publication SHA is verified using push/ls-remote and sealed in
private publication.json/final response;its own documentation commit cannot
contain its own SHA. No main merge,force push/reset/clean/stash,Azure,environment
publication/replacement,public access,PR/workflow dispatch,paid compute or model
changes. Cloud is observed current/running/connected with restricted network
enforced;no live laptop/service/D: access is claimed.

Mini is clean at `14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8`;33 frozen paths
unchanged. All16 web vendor paths,including12 upstream pins,match C32 exactly.
Unrelated source and raw evidence are preserved. Scope does not edit permission
catalog,global access filter,module definitions,Identity/session lifecycle,
existing enrollment mutations,admission/domain/host/SQL/migrations or finance.

## Result and boundary

FrontDesk already has students.manage for enrollment;it does not have
batches.manage. The old page required full students and batches reads,therefore
its batch lookup blocked the workflow. The new GET options action lives in the
existing enrollment controller and inherits exactly the existing permission,
academy/tenant/current actor and module pipeline. No new bypass or OR-grant
logic. Existing platform-owner policy is retained. An unrelated role,wrong/
expired/revoked/foreign grant or a role merely named PlatformOwner cannot gain
access. Direct grant and custom-role permission removals take effect with the
same existing bearer. Core-only FrontDesk is covered without MultiBranch.

The no-store response contains exactly students[{id,firstName,lastName}] and
batches[{id,name}]. Queries are scoped to the route academy,no-tracking and
deterministically sorted by labels then id. No contacts,medical notes,financial,
teacher,branch or other private detail is projected. Inactive/closed historical
labels remain,as the register must still identify earlier records. Options do
not reserve a place or authorize enrollment:current server admission,capacity,
active/open target,prerequisite,tenant and audit rules still govern each write.
The two reads do not add a new atomic snapshot or concurrency contract.

Client loads scoped options plus enrollment rows;no full management fallback.
Malformed/partial/null options or duplicate ids fail closed and cannot enable
submission. A duplicate id is ambiguous;equal display names with distinct ids
remain supported. Authorized empty arrays remain distinct from failure. Drafts
and explicit selections survive reload and are not silently replaced if a lookup
id disappears. C32 reason/review/preflight/receipt/unknown-save/recovery behavior
stays. Existing enrollment action bodies/DTOs and both C32 assertion bodies were
compared byte-exact;only their synthetic route/schema/actor fixtures changed.

## Actual evidence

Raw output is private at `/workspace/c33-enrollment-options-20261110`,outside Git.
Original/intermediate/final logs and TRX,actual compiled source/artifact hashes,
browser results/screenshots and preserved failures remain. No skips,softened
assertions or fabricated SQL receipt. Synthetic test inputs use invalid example
domains and no customer records. No external security audit upload was retried.

| Check | Observed result |
| --- | --- |
| Original actual HTTP/Identity/InMemory |33 cases:3 PASS/30 FAIL;GET options absent/MethodNotAllowed;not30 independent defects |
| Original actual-page scoped fixture |18 cases:10 PASS/8 FAIL;strict fixture refuses broader endpoints;some failure guards already passed |
| Initial focused API after implementation |161/161 PASS:33 initial new +20 C32 HTTP +108 retained enrollment controls |
| Final new HTTP scope |36 cases in full API;adds Core-only access,current custom-role revocation and non-owner PlatformOwner-label refusal;expanded no-write snapshots include students/batches/grants/roles |
| Full API |2,487/2,487 PASS;zero skipped |
| New actual-page scoped options controls |18/18 PASS:exact read routes/no-store,no fallback,401/403/429/503,malformed/duplicate/partial/empty/history and retained explicit draft |
| Retained actual C32 lifecycle TSX |38/38 PASS;assertion body byte-exact |
| Native Chromium |40/40 PASS;20 retained variants ×320/1440,synthetic FrontDesk profile/API,actual forms/calendar/review/recovery;zero full student/batch requests;assertions byte-exact |
| Browser resources |Sandbox enabled;owned temporary profile removed;strict loopback API/static requests;no live Identity/SQL/model/device claim |
| Exact-lock optimized export |Cached locked Next16.4.3/React19.2.8 webpack build PASS;87 static aliases,570 export files;no install |
| Types/lint |TypeScript PASS;lint0 errors/61 retained warnings |
| Full current frontend |90 files;2,434 PASS/3 FAIL/2,437 tests;zero skipped/cancelled |
| Exact frontend failures |Only communication-drafts45/48;missing Cloud `QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`;historical Desktop receipts not fabricated here |
| Production guard |15/15 controls PASS;actual guard exit1 retains five P0 records plus required broader-session gate |
| Issue inventory |120 records:5P0/102P1/13P2;0 formal Closed+Pass;not120 untouched defects or completion percentage |
| Hosted CI |C33 unverified;latest Actions query in C31 returned Forbidden,not retried/bypassed;verify.yml feature push filters exclude C33;no dispatch/PR |

Commands: dotnet test --no-restore -p:UseSharedCompilation=false with original/
focused filter,then full suite,retaining TRX;
`node QA/tools/enrollment-options.test.cjs`;
`node QA/tools/enrollment-lifecycle.test.cjs`;
`QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/enrollment-lifecycle-browser.cjs`;
`node QA/tools/run-frontend-tests.cjs`;`tsc --noEmit`;`npm run lint`;
`NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack`;
`node --test QA/tools/production-gates.test.cjs`;
`node QA/tools/check-production-gates.cjs`;
`node QA/tools/priority-issue-status.cjs --write`.

## Prioritized acceptance / next owners

1. Desktop Sol High:selectively integrate the six C33 source/test paths and this
   return without replacing newer finance/host work. On isolated real SQL/Identity
   verify FrontDesk/custom students.manage/direct-grant lookup→create/lifecycle
   workflow,Core-only academy,minimized fields/foreign omission,query ordering,
   active/expired/revoked/foreign grant,current role removal,owner/tenant/suspended/
   maintenance boundaries,no read writes/audit and retained full batch denial.
   Follow C32's real admission/audit rollback/race and source-bound SQL tasks.
2. Desktop:actual authenticated SQL-backed browser and physical devices for
   FrontDesk entry,reason confirmation/cancel,stale/unknown save and readback,
   session changes,empty/unavailable lookup and no mutation replay. Chromium here
   uses synthetic API state and does not establish native SQL/device acceptance.
3. Cloud next:separately review/claim remaining0018 batch/setup/schedule/attendance/
   progression/makeup/events lookups,or remaining branch/course/batch0012 recovery.
   Reuse existing accepted repairs;do not expand management grants just to enable
   selectors. Desktop retains protected expense/payroll reads. C33 covers only
   enrollment;the broad0018 issue remains OPEN.
4. Release:further acceptance for five P0 records DATA0001/0002(finance/reminder/
   family/currency/permission agreement),DATA0003(positive-net payroll identity/
   history/print),DATA0010(audited void/evidence restore),SEC0001(private media
   authorization/recovery/capture/devices),plus AUTH-REVOCATION001 broader
   SECURITY-SESSION001 across remaining roles/devices/critical cases. No bulk
   closure or acceptance inferred from unit/browser totals.
5. Integrator:exact-release hosted CI,fresh security/dependency audits,combined
   images/runtime,SQL migration/backup/restore/rollback and staging. Preserve
   genuine receipt failures until source-bound input exists. No main/Azure action
   is authorized by feature publication. Native/release work remains before AI
   becomes the only next area.

Mini/Plus/Pro remain unqualified;Ultra deferred.90% eligible conversational
outcomes remains unmeasured. Deterministic Testing-only host checks are not model
qualification. Loading/download/inference/training/activation stop remains.
