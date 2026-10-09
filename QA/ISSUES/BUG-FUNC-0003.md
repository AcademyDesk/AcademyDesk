# BUG-FUNC-0003 — Success notices are missing or cleared immediately by reload helpers

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Governance/Branches/Batch/Course/Schedule variants CONTROLLED-REPRODUCED / local repair; other original forms STATIC-FINDING |
| Final verification | Prior Course49/combined209 retained; Schedule41 controlled +12 synthetic browser PASS; linked/device/other forms/critical OPEN |
| Severity | Major feedback |
| Priority | P1 |
| Category | FUNC |
| Module | OPERATIONS |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /work-queue; /access-review/sign-off; /platform-services; /compliance; /branches; /leads; /sales-campaigns; /trial-bookings; /courses; /curriculum; /academic-governance; /academic-periods; /batch-setup; /enrollments; /batch-promotions; /assessments; /schedule; /attendance; /leave; /makeup; /holidays; /events; /communication-settings; /communications; /communication-preferences; /assignments; /lesson-plans; /submission-review |
| API | Corresponding create actions |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FORM-SUCCESS-001 |
| Evidence classification | Accepted static traces retained; bounded Governance/Branches/Batch/Course actual-handler checks, not live browser/SQL proof |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/work-queue/page.tsx:13 |
| Class/function | create / load; similar sign-off and support flows |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | After isolating async reset defect, submit valid form with immediate successful reload. Observe live status region after list refresh. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local Governance/Branches/Batch/Course feedback variants; uncommitted/not deployed; other original forms pending |
| Retest result | Course durable confirmations/reset/error/pending guards controlled PASS; browser/device/other forms pending |
| Regression result | 209 current combined controlled frontend PASS; no new backend/SQL run; Course lint FAIL (1 error/1 warning after improvement), not all-form/critical acceptance |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Communications feedback checkpoint — 2026-10-10

[Report](../REPORTS/PHASE_2B_COMMUNICATIONS_FEEDBACK_REPAIR.md): direct/banner durable confirmation, empty-body safety, saved/readback-failure distinction, retained rejected/unconfirmed drafts and synchronous pending guard. Existing50 channel assertions retained; frozen72 baseline51 PASS/21 FAIL, final72 +32 synthetic browser PASS, lint0/0, TypeScript/83-page export PASS. Stable Message name fixes browser-observed error-state accessibility. Delivery/consent/authority/timezone unchanged; no new live sends/SQL/device or release acceptance. Issue OPEN; next Communication Preferences feedback gap check.

## Communication Settings feedback check — 2026-10-10

[Report](../REPORTS/PHASE_2B_COMMUNICATION_SETTINGS_FEEDBACK_CHECK.md): existing durable channel confirmation and draft/pending repair reused; mount dependency warning removed without suppression.90/90 controlled before/after (87 retained +3 lifecycle/accessibility),24 synthetic exported-browser channel cases PASS; lint0/0, TypeScript/83-page export PASS. Missing historical receipt resolved via read-only original/main path, assertions unchanged; no fresh SQL/device/provider-security acceptance. Payload/authority/credentials unchanged, issue and related BUG-DATA-0041 remain OPEN. Next Communications existing-feedback gap check.

## Events feedback checkpoint — 2026-10-10

[Report](../REPORTS/PHASE_2B_EVENTS_FEEDBACK_REPAIR.md): durable create/save-readback-failure notice, retained rejected/unconfirmed inputs, shared synchronous write/readback guard and pure mount loader. Frozen15 baseline2 PASS/13 FAIL; final15 controlled +16 synthetic browser PASS, lint0/0, TypeScript/83-page export PASS. Existing payload/default/null/access/time conversion retained; timezone caveat recorded. No fresh linked SQL/device/critical acceptance; issue remains OPEN. Next Communication Settings gap check, reuse existing guards.

## Holidays feedback checkpoint — 2026-10-09

[Report](../REPORTS/PHASE_2B_HOLIDAYS_FEEDBACK_REPAIR.md): create/default-add/remove
feedback and pending protection fixed;22 controlled +16 synthetic exported-browser
cases PASS, lint0/0, TypeScript/export PASS. Saved/readback failures and uncertain
writes distinguished; rejected form retained. Dates/default endpoint/domain/access
unchanged. Initial browser fixture lacked Certificates module and correctly stayed
locked; corrected fixture passes, failures retained. No new SQL/device/critical
acceptance; issue OPEN for remaining forms and broader gates. Next Events feedback.

## Leave feedback checkpoint — 2026-10-09

[Report](../REPORTS/PHASE_2B_LEAVE_FEEDBACK_REPAIR.md): frozen23 baseline2 PASS/
21 FAIL; final23 actual-TSX and16 synthetic exported-browser cases PASS. Durable
create/approve/reject, checked decision rejection, retained uncertain/rejected
inputs, saved/readback-failure guidance and shared synchronous pending protection.
Lint0/0 and TypeScript/export PASS. Student/Teacher identity and domain/backend
contracts unchanged; prior SQL receipts retained, not rerun. Issue OPEN for other
forms, linked live/browser/device/critical gates. Next Holidays feedback gaps.

## Make-up effect-lint checkpoint — 2026-10-09

[Report](../REPORTS/PHASE_2B_MAKEUP_EFFECT_LINT.md): inherited dependency warning
cleared; final0 errors/0 warnings with no suppression. All31 retained plus2
lifecycle controls33/33 PASS before/after;16 synthetic exported-browser cases,
TypeScript/83-page export PASS. No draft/notice-triggered reload or automatic retry,
business/API/location/authority change or fresh SQL/device acceptance. Issue OPEN
for remaining forms and linked/device/critical gates. Next Leave feedback gaps.

## Make-up feedback checkpoint — 2026-10-09

[Repair](../REPORTS/PHASE_2B_MAKEUP_FEEDBACK_REPAIR.md): frozen actual-handler
baseline5/20 PASS/15 FAIL. Durable create/status confirmations, separate saved but
failed-readback guidance, server rejection/unconfirmed outcome handling and shared
synchronous pending protection now pass20 new plus11 unchanged location checks.
16 synthetic exported-browser viewport/theme/fault cases, TypeScript/export PASS.
Lint zero errors/one inherited dependency warning remains OPEN; no new live
SQL/Identity/device/critical acceptance. Prior Online/Offline/Hybrid and inherited
session rules unchanged. Issue OPEN for remaining forms and broader gates.

## Attendance effect-lint checkpoint — 2026-10-09

[Report](../REPORTS/PHASE_2B_ATTENDANCE_EFFECT_LINT.md): inherited effect lint
cleared (HEAD1 error/2 warnings, final0/0), without suppression or read/write
contract changes. Original32 plus2 lifecycle guards34/34 PASS on both HEAD and
final;16 synthetic exported-browser cases, TypeScript/83-page export PASS.
Session-scoped notes/readback/uncertainty/pending protection retained; no automatic
refetch from drafts/notices/saving state. No new SQL/Identity/device acceptance.
Issue OPEN for remaining forms/linked/device/critical gates. Next Make-up gap check.

## Attendance feedback checkpoint — 2026-10-09

[Gap check](../REPORTS/PHASE_2B_ATTENDANCE_FEEDBACK_CHECK.md): earlier durable
Attendance success/draft/readback handling already exists and original 24 controls
pass; not reimplemented. Narrow 5xx outcome guidance now says unconfirmed/check
before retry, retaining server guidance and drafts. Final 32 controlled and 16
synthetic exported-browser width/theme/fault cases PASS, TypeScript/export PASS.
Lint remains FAIL with the same one inherited effect error/two warnings as HEAD.
No new live SQL/Identity/device/critical proof; issue OPEN. Next bounded effect-lint
cleanup, Sol Medium; Sol High only if authority/persistence contracts change.

## Schedule feedback checkpoint — 2026-10-09

[Schedule repair](../REPORTS/PHASE_2B_SCHEDULE_FEEDBACK_REPAIR.md): baseline 4/23
PASS, 19 FAIL; final 23 new feedback/failure/pending checks plus all 18 existing
Schedule-default checks PASS (41/41). Accessible create/status confirmations survive
readback, confirmed-save refresh failures retain success with reload/no-repeat
guidance, failed/unconfirmed writes retain inputs, and synchronous guards prevent
duplicate/opposing requests through refresh. Twelve synthetic exported-DOM cases
PASS at 320/1440 light/dark, no unexpected console errors; not live SQL/device proof.
TypeScript/83-page export PASS; lint exit 0, zero errors/one inherited warning.
No backend/domain/default/timezone/permission changes. Issue remains OPEN for
other forms, linked acceptance, physical devices and critical regressions.

## Course feedback checkpoint — 2026-10-01

[Course feedback repair](../REPORTS/PHASE_2B_COURSE_FEEDBACK_REPAIR.md):49 new actual-handler checks and combined209/209 PASS; TypeScript PASS. Baseline15:10 PASS/5 FAIL. Create now confirms and resets; existing edit/status successful notices remain positive controls. Failed refresh retains confirmed success with guidance; rejected/unconfirmed writes keep details; pending duplicate/opposing requests are guarded through readback. Course lint improves2 to1 error/1 warning; gate remains FAIL. Frontend-only; all147 previous captures and backend assemblies unchanged,3 new captures. No new API/SQL/browser proof or deployment. Known Course configuration-loss BUG-DATA-0028 remains separate and OPEN, queued next with Sol High. Other forms/browser/device/critical acceptance remain OPEN.

## Batch feedback checkpoint — 2026-10-01 (historical)

[Batch feedback repair](../REPORTS/PHASE_2B_BATCH_FEEDBACK_REPAIR.md):44 new actual-handler controls, combined160/160 PASS and TypeScript PASS. Create/edit/deactivate/reactivate notices survive successful or failed readback; confirmed create resets all per-batch inputs, failed/unconfirmed writes keep details, pending guards block duplicate/opposing requests through refresh. Existing successful edit/toggle notices are retained positive controls, not missing-notice reproductions. Baseline15:10 PASS/5 FAIL(create notice, three duplicates, blank create). Frontend-only; lint improves2 to1 error/3 warnings, gate still FAIL, backend sources/assemblies unchanged, no API/SQL rerun or new server/database. Other forms/live browser/device/critical remain OPEN. Next accepted Course create feedback variant, Sol High.

## Branches feedback checkpoint — 2026-10-01 (historical)

[Branches feedback repair](../REPORTS/PHASE_2B_BRANCH_FEEDBACK_REPAIR.md) fixes durable create/edit/deactivate/reactivate confirmations, confirmed-save-but-refresh-failed guidance, failed-write detail retention and pending duplicate/opposing request guards. Network/5xx saves are labelled unconfirmed; no automatic retry. Controlled create fields clear only after confirmed response, edit closes only after success, pending state always releases. Corrected before baseline 8/15 PASS (four erased confirmations/three duplicate failures); final Branches 45/45 and combined frontend 88/88 PASS with TypeScript/lint. Only the Branches application file changes; prior 123 source captures and backend assemblies unchanged, no API/SQL rerun or deployment. Other forms/live browser/device/full critical remain OPEN. Branch address/postcode preservation is separate accepted BUG-DATA-0025, queued next.

## Governance feedback checkpoint — 2026-10-01 (historical)

[Bounded Governance repair](../REPORTS/PHASE_2B_GOVERNANCE_ACCESS_REPAIR.md) extends this feedback finding to adjustment decisions, collection follow-up and escalation saves. Corrected before-handler fixture allows the generic admin lookup to isolate empty-notice clearing; separate failed-refresh controls lose saved confirmation. `load` now retains supplied confirmation after readback or adds an explicit refresh warning after a confirmed save. Nine Governance controlled cases and prior 13 frontend controls PASS; no live React/browser/device proof. Original forms, async/stale-response/concurrency/critical gates remain unverified/unfixed; this is not whole-issue closure. No commit/deployment.

## Exact reproduction (original forms)

After isolating async reset defect, submit valid form with immediate successful reload. Observe live status region after list refresh.

## Expected

Durable success message remains visible after successful reload; list shows newly saved record.

## Actual / evidence

Some handlers set success then await load(); load clears the shared message state. Branch create, edit and active-state handlers also set success then loadBranches clears it; these handlers do not require the async-reset defect to reproduce. Lead conversion does the same. Lead create/stage and campaign/trial create/status flows provide no positive notice after successful readback. Curriculum add/publication notices are erased by load; course creation supplies none, while course edit/toggle notices survive loadCourses. Governance create/status and academic-period create/close likewise clear positive notices during reload. Batch create and enrollment create provide no success; enrollment status and promotion create/decision notices are cleared by load. Batch edit/toggle success survives loadWorkspace. Assessment creation and result saving also provide no positive notice after readback; failed result refresh is described as score validation failure even if save already committed. Schedule create and attendance mark provide no success; schedule status success is cleared by load. Attendance refresh failure is reported as failed attendance despite a potentially committed write. Leave/event/holiday creation and status actions also omit durable success; make-up scheduling and holiday defaults messages are erased by load. Sender and meeting-provider Save notices are also erased by load, including the message about the missing secure-connection next step. Message queue/banner and consent Save notices are cleared by load. Template create and starter-add notices survive their load and are positive controls, not instances of this issue. Assignment and lesson-plan creation supply no success notice; submission review updates its row but shows no durable success and does not clear an earlier save error. Independent feedback defects remain after fixing currentTarget.

Source snapshot:

```text
12:   useEffect(() => { void load(); }, []);
13:   async function create(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!academy) return; const form = new FormData(event.currentTarget); const dueAtUtc = dueDate ? new Date(`${dueDate}T${dueTime}:00`).toISOString() : null; const response = await academyApi(`/api/academies/${academy.id}/admin-work-items`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ type, title: form.get("title"), description: form.get("description") || null, priority, entityType: null, entityId: null, assignedUserId: assignedUserId || null, dueAtUtc }) }); if (!response.ok) return setMessage("Work item could not be created."); event.currentTarget.reset(); setAssignedUserId(""); setDueDate(""); setDueTime("09:00"); setMessage("Work item added to the operational queue."); await load(); }
14:   async function move(item: Work, status: string) { if (!academy) return; const response = await academyApi(`/api/academies/${academy.id}/admin-work-items/${item.id}/status`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ status }) }); if (!response.ok) return setMessage("Work item could not be updated."); setMessage(`Work item moved to ${status}.`); await load(); }
15:   const highPriority = items.filter(item => item.priority === "Critical" || item.priority === "High").length;
```

## Suspected root cause

Loading/error and success feedback share state and reload clears it.

## Business impact and blast radius

Multiple form handlers; do not assume all setMessage calls provide visible confirmation.

## Related / required regression

FORM-SUCCESS-001: Fast and slow refresh with controlled network; assert visible success after reload, no false success on partial failure.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
