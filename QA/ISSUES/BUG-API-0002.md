# BUG-API-0002 — Successful write can be followed by an audit-save failure

## Enrollment shared-service/native audit successor — 28 October 2026, OPEN

Real SQL verifies required-audit failure after each service/native create,
lifecycle update and transfer save rolls back all changes, including both source
history and target admission. The shared service owns no audit/commit; its future
conversational host must supply them with exact scoped approval and durable receipt.
Native filter/atomic attributes retained.3,183API/zero skips,57newSQL +28retained
lifecycle cases,15mixed admission races and36CI guards PASS. Qualifier
973096375be64b9aaf67878869808f3a verifies exact source, restoration and owned cleanup.
No AI tool activated; broader audit/conversation/browser/release gates stay OPEN.
See `../REPORTS/PENTA_ENROLLMENT_SHARED_2026-10-28.md`.

## Teacher conversational browser integration — 28 October 2026, OPEN

Teacher chat now uses the protected startup/gateway and existing explicit native
review→approval→create/receipt. Actual exported UI with disposable SQL/Identity:
five new chat plus six retained review journeys pass alongside200SQL cases.
One explicitly approved chat teacher/audit, no accounts/unrelated changes; lost
post-completion prompt response recovers by GET after reload without redispatch;
cancelled draft recovers as cancelled.167client/types/lint/fresh89page export,
retained3,106API,11CoreSDK/35CI guards pass; nine PNGs inspected. Current-source/
restoration/cleanup qualifier9921fe972af04c819c9dafe4d14729c3. Real Mini semantics,
all-role/device and release gates remain open. The broader audit issue is OPEN.
See `../REPORTS/PENTA_TEACHER_CHAT_UI_2026-10-28.md`.

## Teacher startup audit boundary — 28 October 2026, OPEN

Teacher startup stores its session, empty draft, startup marker/protected receipt
and mandatory audits in one SQL/Identity transaction. A real injected required
startup-audit failure returns503, preserves scoped state and produces no phantom
GET result. Same-key concurrent startup returns201/200 with one owned identity;
startup makes no interpretation or domain call. Actual startup→partial→prepare→
separate native approval/create yields one teacher/execution audit and no account.
All 46 startup +154 retained SQL,3,106API/zero skips,113client/types/lint,
11retainedCoreSDK/35CI guards passed; current-source/restoration/cleanup qualifier
68c078d345284c3a9ecc992e273f7f61. No real model/chat UI/release acceptance.
This is a bounded audit qualification; the broader issue remains OPEN.
See `../REPORTS/PENTA_TEACHER_START_2026-10-28.md`.

## Teacher conversation claim/completion return — 27 October 2026 batch, OPEN

The teacher host gateway persists its request claim and audit before synthetic
interpretation, then commits partial/prepared draft, completion audit, protected
receipt and pending-clear atomically. Real mandatory claim-audit failure prevents
planner invocation; completion-audit failure rolls the draft back and keeps the
original pending claim. Recovery does not redispatch.43conversationSQL +retained
111teacherSQL,3,089API/zero skips,11CoreSDK and34CIguards PASS; qualifier
f26d38e4a9c644d8b21c4269ae1b5dc3. Exact restoration/cleanup/main preservation
verified. No actual model/teacher execution or new frontend acceptance. This is
a bounded audit boundary, not broad issue closure. Issue remains OPEN. See
`../REPORTS/PENTA_TEACHER_CONVERSATION_2026-10-27.md`.

## Teacher Core/native UI related return — 27 October 2026, OPEN

Final protected teacher Core boundary qualified21SQLchecks including injected
preparation-audit rollback, request replay, explicit host approval and one-time
receipt. Native teacher UI sixSQL-backed journeys/35HTTP:three teachers/three
execution audits, cancelled draft no create, lost real201response recovered
GET-only.3,065API/52client/types/lint/33CIguards PASS, exact source/cleanup/
restoration qualifier098af81a2b4d488b88f8f7cd29ac9abf. Eight PNGs inspected.
No full model/customer/device/release acceptance or broad audit issue closure.
Issue remains OPEN. See `../REPORTS/PENTA_TEACHER_REVIEW_UI_2026-10-27.md`.

## Related teacher workflow audit/receipt boundary — 27 October 2026, OPEN

Teacher workflow now commits the domain teacher, encrypted succeeded draft/
receipt and attributed mandatory audit together under a shared serializable
SQL/Identity transaction. Real injected execution-audit failure returns503 and
leaves protected domain/draft/identity/audit state unchanged; same-draft GET
reconciliation precedes an explicit retry. Actual concurrent replay201/200 gives
one teacher/audit; distinct duplicate drafts201/409. FinalSQL
40c1ba4b99fd44a69daf251d2f6db593:30workflow +15service +45availability checks,
full3,049API,52client/types/lint and32CIguards PASS with linked current-source/
cleanup/restoration verification. No teacher model tool or visible chat enabled.
This qualifies the bounded teacher boundary, not all endpoints or release gates.
Issue remains OPEN. See `../REPORTS/PENTA_TEACHER_WORKFLOW_2026-10-27.md`.

## Related teacher-create audit transaction — 26 October 2026, OPEN

Existing teacher Manual POST now uses current authenticated database authority
and the shared SQL/Identity transaction, including explicit Platform Owner atomic
handling. An injected mandatory generic POST Teachers audit failure returns500
and leaves scoped teacher/identity/role/audit/student/invoice/payment/draft state
unchanged. Final SQL46b63aadcb024445b6b0bb1cab6bd077/window
f6184cea089b4ff0a7395c5a046fe645:15teacher checks +45availability cases PASS;
full3,040API tests/zero skips and31CI guards PASS. Exact original services,
owned cleanup/source-bound recheck verified. No model tool registered.
This qualifies teacher creation only; broader endpoint/AI/browser/device/release
acceptance and the issue remain OPEN. See
`../REPORTS/TEACHER_CREATION_SHARED_SERVICE_2026-10-26.md`.

## Related start-receipt reload recovery — 26 October 2026, OPEN

Client recovery now retains the opaque start request lookup key across reload,
using the existing protected same-actor/tenant GET with no repeat POST. Native
SQL/browser eb5a3c5cc3bf4a38b84ceb68f8fd8c24/window
d86e4a5aed4a4d56b9056cbd33e3d943 PASS five journeys/37HTTP, wrong actor403,
exact3empty drafts/zero native student creates/inference/unrelated changes.
Retained atomic/audit/core/start SQL checks all remain passing; no API/domain
behavior changed. Source-linked verifier/627file export/owned cleanup/exact
restoration PASS. This repairs a related client interruption path, not all
endpoints/audit failures/concurrency/device/release acceptance or issue closure.
See `../REPORTS/PENTA_START_RELOAD_RECOVERY_2026-10-26.md`.

## Sign-off linked native return — 26 October 2026, OPEN

Fresh receipt UI41checks/eight synthetic journeys/four actual Owner-login native
sign-offs and one audit500/draft/read-recovery/retry now qualified against the
2995test API. Native49HTTP/four backend fault controls retained, plus browser
fault no-write/no-audit. Exact four browser reviews/two tasks/eight correlated
audits/13table fingerprint equivalence/cleanup/original services/inventory/API200/
independent qualifier PASS. SQL3a0152f3563f44d883c4310f9355f566/window
a7c362f7409842f8a9915f03dc702168. Wider endpoints/concurrency/devices/release OPEN.

## Access-review audit repair qualified — 26 October 2026, OPEN

API3368e36621f942bfac353b8ac92824da2995tests/65actual files and native
SQL23c4ffbcb9054108a85c6505a62f3b2f49HTTP/five normal-or-recovery sign-offs/
four deliberate500rollbacks/five reviews3tasks10audits PASS. Both ordinary and
platform generic audit failures, correlated domain audit and task-insert failure
leave all13tables unchanged. Exact services/inventory/cleanup/source-bound
qualification PASS. Current client/build/browser continuation does not close the
wider endpoint/concurrency/device/release issue. See provenance report.

## Access-review sign-off successor — 25 October 2026, OPEN

The existing missing reviewer/task link issue also reproduced an audit-integrity
gap: native old-image ordinary Owner500persisted review/task and platform200
bypassed audit. Baseline46ordinary HTTP controls/exact cleanup/restoration PASS.
Scoped sign-off transaction plus correlated reviewer/review/task event/receipt
repair now2995API tests PASS; fresh image and native fault/recovery acceptance
pending. See `QA/REPORTS/ACCESS_REVIEW_PROVENANCE_2026-10-25.md`. Preserve all
earlier qualified finance/people/grant/custom-role/Staff slices; wider issue OPEN.

## Staff-action continuation — 25 October 2026

[Current report](../REPORTS/STAFF_ACTION_AUDIT_ATOMICITY_2026-10-25.md): baseline
SQL `18d235b257fe4155b312aaa61eeb390b`54ordinary checks/eight unsafe outcomes:
Create/password/status/offboard return ordinary500after persisted changes, and
same-academy platform-flag Owner bypass returns201/200without any success audit.
Real password/session/activity/lockout consequences; unrelated tables unchanged,
exact originals/owned cleanup/inventory PASS. Four existing explicit Identity/
domain/platform audit markers added only to those actions; action bodies and
authorization remain unchanged. Corrected full2971API regression PASS after
updating four historical expectations that explicitly excluded the new repair;
failed run retained, no image from failure. Fresh image
`ee9df6365eba476b973a940a45f02b8c`, SQL
`0f2bef2c48004bd19cb5304b460d6049`54ordinary HTTP/eight audit500rollbacks/
ten recovery audits/same original sessions preserved/11table snapshots PASS.
Parent/exact original healthy services/full inventory/owned cleanup and paired
source-bound qualifier PASS. No broad IdentityResult/payload/device acceptance;
broader staff/provisioning/platform/concurrency/browser/device/release OPEN.

## Custom-role continuation — 25 October 2026

[Current report](../REPORTS/CUSTOM_ROLE_AUDIT_ATOMICITY_2026-10-25.md): native
baseline `d0ce1a6259504cbaa72eb0af960011d8` reproduced Create/Assign500persisting
custom-role/membership changes without success audits. Same Teacher bearer
immediately loses students.manage/gains academics.manage despite failed response.
49ordinary checks/two fault cases, unrelated tables unchanged, exact SQL cleanup/
inventory PASS. Minimal existing Identity/domain action markers qualified:
2956API tests, fresh development image and repaired SQL
`8da09467be40454697ff326e61065c72`51ordinary HTTP checks PASS. Two audit
faults and one membership-insert fault roll back all11tables/preserve same-bearer
permissions; four recovery audits/exact original services/full inventory and
independent paired qualifier PASS. No role-policy expansion, PENTA
role tools or broader issue closure. Previous qualified grant/finance/people proof
below stays bound to its original source/image, not weakened for this successor.

## Access-grant continuation — 25 October 2026

[Current report](../REPORTS/ACCESS_GRANT_AUDIT_ATOMICITY_2026-10-25.md): fresh
native baseline `188634bbb1c842aba92dd0396255f2ee` reproduced both Create/Revoke
500responses persisting unaudited permission changes, same Teacher bearer200
after failed creation/403after failed revocation.45ordinary HTTP checks/two fault
cases; unrelated10tables/roles unchanged, owned cleanup/inventory PASS. First
incomplete GUID-casing QA failure retained. Minimal explicit Identity/domain
action markers now qualified:2933API tests, fresh image `f89044e59990403787d3d09f6f14552f`,
SQL `2ff063277dd3477ab2130636a23df11d`44ordinary checks/two500rollback-recovery
controls, SQL `6f49f8c45baf4b57b7ca953e4b03cb46`70normal checks/30permission cases,
65payload files and exact original restoration/inventory PASS. Post-SQL supplemental
RAM refusal retained FAIL; independent qualifier requires later successful same-image
inspection before SQL, not a waiver. Existing issue broader paths remain OPEN.
This expands evidence for the existing issue, not a new issue or closure.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Local finance, student/teacher create and explicit linked Update same-store boundaries PASS twice per batch; other domains/platform/browser/concurrency/critical pending |
| Severity | Major ambiguous outcome |
| Priority | P1 |
| Category | API |
| Module | SHARED |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | All academy-scoped mutations passing audit gate |
| API | Academy POST/PUT/PATCH/DELETE |
| Environment | Fresh guarded local Identity/MVC/SQL; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | API-RELIABILITY-001 |
| Evidence classification | Primary post-save audit fault and rejected-action branch reproduced twice; scoped local repair retested |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Finance and student/teacher create faults each reproduced 2/2; each scoped repair PASS 2/2 final runs |
| Source | apps/api/Security/AcademyAccessFilter.cs:97 |
| Class/function | ExecuteAndAuditAsync |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | PHASE_2B_AUDIT_SAVE_REPAIR.md and PHASE_2B_PEOPLE_CREATION_AUDIT_REPAIR.md, source snapshots and before/final SQL logs; historic excerpt retained |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In test SQL force an audit insert failure after a successful controller save. Observe API result and fresh domain record. Repeat action-level BadRequest handling. |
| API response | Before/final injected fault HTTP 500; rejected 400/404; recovered invoice 201; GET 200 |
| Database before/after | Before fault: invoice/payment/person persisted without audit; scoped repairs: no write on audit failure; successful create plus attributed audit persists |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted finance/result-status and action-level student/teacher creation transaction repair; no deployment |
| Retest result | Latest 26 linked +28 create +six finance cases PASS twice on fresh SQL; other domain/provisioning/platform post-save faults pending |
| Regression result | Latest API 178/178; 60 SQL cases PASS twice; prior 129 finance access cases historical, not rerun; critical/browser/concurrency NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Latest linked-update repair — 2026-10-01

[Linked student/teacher repair](../REPORTS/PHASE_2B_LINKED_PEOPLE_REPAIR.md) reproduces both Update actions returning 500 after person and two linked accounts persist on generic-audit INSERT failure, twice. Explicit IncludeIdentity enlists the existing Identity context in the same configured SQL transaction; original store/validation and audit faults now roll back captured person/accounts/audits. API 178/178, two final runs ×60 cases PASS; current cleanup/no deployment. This supersedes the historical Update exclusion below, not every cross-store/mutation path. Platform-flag bypass, provisioning/media/other mutations/concurrency/browser/critical remain OPEN. Next accepted BUG-DATA-0051 paired Update branch-validation gap, agreed Sol High.

## Historical action-level creation repair — 2026-10-01

[Student/teacher creation repair](../REPORTS/PHASE_2B_PEOPLE_CREATION_AUDIT_REPAIR.md) reproduces both Create actions returning 500 after persisting a person without audit, twice with guarded SQL permission fault. Method-only marker extends the existing domain transaction boundary to these two Create methods. API 163/163 and two final runs ×(28 people +six finance cases) PASS. Faults roll back person records; minimal/populated optional fields and fresh attributed 201 success are verified. Captured Identity/finance/notification state remains unchanged; current role/tenant/branch refusals preserve captured state and add no success audit.

OPEN: Student/Teacher Update are deliberately unmarked and still require coordinated domain/Identity repair; other domain mutations/provisioning/media/platform bypass/response-loss/concurrency/critical/browser remain pending. No account provisioning, permission broadening, schema/frontend change or deployment. Next paired linked student/teacher updates using existing BUG-DATA-0052/lifecycle mappings, agreed Sol High.

## Historical scoped finance repair — 2026-10-01

[Finance save/audit repair](../REPORTS/PHASE_2B_AUDIT_SAVE_REPAIR.md) reproduces the primary fault twice using run-owned AuditLogs INSERT denial after actual controller business saves. Local filter repair encloses domain-only ordinary finance mutations in one SQL transaction and checks pending action-result status for success auditing. Two final runs pass six fault/recovery/read/rejection cases and 129 role/tenant cases each; API 155/155 PASS. Rejected 400/404 and seven foreign-row refusals now add no generic success audit. Faulted invoice/payment writes roll back, rather than returning 500 after persisting.

OPEN: this is not a shared all-controller/cross-store solution. Non-finance domain writes, Identity-coupled student/teacher updates/provisioning, Blob/media transactions, platform-flag bypass, lost-response/commit faults, concurrency and full critical/browser gates remain. No audit exception is swallowed or permission broadened. Next bounded slice is domain-only student/teacher creation with action-level boundaries, agreed Sol High. No deployment/issue closure.

## Historical partial rejected-action observation — 2026-10-01

[Finance access regression](../REPORTS/PHASE_2B_FINANCE_ACCESS_REGRESSION.md) observes seven foreign-row controller refusals (five 404/two 400) on each of two fresh Identity/HTTP/SQL runs. All captured financial/notification rows remain unchanged, but generic audit count increases by one per refused action. Scope is the existing result-status/audit branch only; this is not the primary audit-insert failure after a successful business save. Keep STATIC-FINDING for that primary failure and NOT RUN for fault injection, atomicity/retry/durable client recovery. No audit/application change or issue closure. Next bounded task tests that primary failure through the real pipeline.

## Exact reproduction

In test SQL force an audit insert failure after a successful controller save. Observe API result and fresh domain record. Repeat action-level BadRequest handling.

## Expected

Business save and audit have defined atomicity; client is not told an already-committed operation simply failed. Failed actions are not audited as successes.

## Before-fix actual / evidence

Filter runs a second SaveChangesAsync after controller action; no encompassing transaction. It checks Response.StatusCode before IActionResult executes rather than inspecting result status.

Historical source snapshot (not current repaired filter):

```text
96:     {
97:         var executed = await next();
98:         if (executed.Canceled || executed.Exception is not null || context.HttpContext.Response.StatusCode >= StatusCodes.Status400BadRequest) return;
99:
```

## Suspected root cause

Post-action audit persistence and response lifecycle are treated as one successful operation.

## Business impact and blast radius

All audited mutations; retries can duplicate work.

## Related / required regression

API-RELIABILITY-001: Audit write fault + rejected action tests through full MVC pipeline, compare actual HTTP status and DB changes.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
