# Batch 25 — Assignments, submission review and lesson planning

2026-09-29. Source specification; runtime NOT RUN. Completes AssignmentsController (List/Create/Publish), AssignmentSubmissionsController (List/Submit/Review), LessonPlansController (List/Create/Status):9 actions. Reuse LEARNING-API-001–006, LEARNING-API-011–013 and existing teacher/family workflow cases. TeacherPortalController and PortalController comparisons reuse batch05; no duplicate controller count.

LEARNING-FORM-001 adds6 lexical controls; LEARNING-FORM-002 adds3. Submission Review has no native form or inventoried JSX input: feedback uses window.prompt, specified under LEARNING-API-006 as an interaction without inventing a field count.

## G02 — Assignment form, contract and visibility

| Field | Source behavior and required cases |
| --- | --- |
| batchId | Required native selector, automatically selects first loaded batch if state empty. API requires same-academy batch existence, not active/open state. Test missing/empty/foreign/deleted/inactive batch, stale lists, no batches and switching during submit |
| title | Required input, API nonblank and trimmed, EF250. Test missing/null/empty/spaces, Unicode,250/251 and escaped display |
| description | Optional textarea; empty string sent null, whitespace trims to empty in API, EF4000. Test omission/null/empty/spaces/explicit optional content,4000/4001 and multiline text |
| type | Homework/Practice/Theory/Project selector, default Homework. API accepts arbitrary nonblank trimmed text, blank defaults Homework; EF40. Test unsupported/case/space/length and supported policy |
| dueAt | Optional native datetime-local; blank sends null, populated browser-local Date becomes ISO. List also formats browser-local time, not declared academy timezone. Test empty/malformed/offset/DST/midnight/leap/year boundaries, past due and a second viewer timezone. Do not impose another page's IST rule without policy |
| published | Checkbox default true; API stores requested flag, omitted bool becomes false. Test draft/published creation, accidental default publication expectations and fresh student visibility |

Create returns201 summary with Location but no GET-by-ID route. List scopes academy and optional batch, descending DueAtUtc, unpaginated, includes drafts and teacher-created private work. Null/tie ordering and large histories require deterministic expectations. Unknown batch filter returns empty rather than validating parent. No edit/delete API or assignment edit/publish action is exposed in this admin page; draft rows display Draft but cannot be published here even though PATCH exists. Specify the intended maintenance route before approving end-to-end draft workflow.

Assignment.StudentId exists for private teacher-created tasks, but admin AssignmentSummary omits it. Admin list cannot distinguish private versus whole-class work. Test mixed private/class fixtures and keep target visibility accurate; do not infer that omission broadens student access. Family reader still checks IsPublished, active batch membership and matching private StudentId.

Admin Publish scopes assignment to academy, replaces boolean, returns empty200. No version, publication actor/time, notification creation or transition constraint. Admin create likewise does not notify, whereas teacher create/publish queues New homework for eligible target(s). Test agreed notification parity and repeated publish (teacher path queues again), publish=false after submissions, stale mutation, repeated patch and correct student visibility.

UI clears title/description/dueAt after successful POST, retains batch/type/published, and reloads both lists; no success notice. Generic error, no busy/catch, no guard against duplicate submits. A committed write followed by failed readback must not imply no save or safe retry (BUG-API-0002/BUG-FUNC-0003). Preserve entered values on rejected request and require correct reset only after confirmed save.

## G02 — Submission API and review interaction

List returns full tenant AssignmentSubmission entities, optional assignmentId, descending SubmittedAtUtc, no student filter/pagination/assignment validation. Test empty/local/foreign/missing assignment filter, null/tie timestamps and malformed data. Projection includes lifecycle and inherited entity metadata; review expected exposure explicitly.

BUG-DATA-0042: administrative Submit binds the domain object. It verifies only local assignment existence, overwrites Id and AcademyId, then saves StudentId/ResponseText/Status/TeacherFeedback/SubmittedAtUtc from caller. It does not check student existence/tenant/enrollment, private assignment target, publication or nonblank response. Mapped model has no student/assignment relationship enforcement for these scalar IDs. Test a synthetic valid control and each invalid/overposted variant; privileged overrides must be explicit, not silently bypassed by model binding.

Unique academy/assignment/student index prevents repeated insert for same tuple, but Submit always adds instead of upserting. Test first/repeated/concurrent request, clear validation response versus generic SQL conflict, and no extra audit/notification on failure. Student/guardian portal instead upserts and validates published assignment, private target, active enrollment and access flags. These are intentionally distinct endpoints; compare supported admin-on-behalf policy without weakening student protections.

EF ResponseText4000, TeacherFeedback2000, Status30 required. Test null/empty/whitespace/maximum/+1 and forged review state/time. Administrative Review scopes row to academy, copies nullable Feedback without trim, sets Reviewed, returns entity. Blank feedback is accepted; teacher review trims it. No reviewer/time/version/current-state guard, and repeat review overwrites previous feedback. Define whether empty review and corrections are supported; optional feedback must not become mandatory solely to mask a bug.

BUG-FUNC-0030: admin review displays status/response/feedback but no student, assignment, batch or submission time. Identical answers cannot be safely distinguished. API IDs exist but frontend type/lookup/render omits their context. Each Review control and prompt needs unambiguous target context; tests must assert clicked intended record and persisted target separately.

window.prompt cancellation returns null and performs no request (positive control). Empty OK does send blank feedback and marks reviewed. On success row is replaced by response; no durable success, earlier error remains. Network/JSON failure lacks catch; no busy guard. Reviewed rows lose Review action, so no UI feedback correction after confirmation although API accepts it. Test cancellation, empty/long feedback,403/404/500, malformed success, rapid duplicate prompts, keyboard focus after button disappears, and reload consistency.

Review versus learner resubmission is a stale-version risk: learner upsert sets Submitted and clears old feedback, while admin review unconditionally sets Reviewed. Delay prompt/PATCH while learner resubmits; require approved revision-awareness so feedback on an older response is not presented as review of new content. No concurrency token exists. Record evidence before defining final conflict strategy.

Portal file submission appends an Attachment path into ResponseText; review page renders plain text rather than an attachment viewer/link. File-only answers show stored path, not the actual material. Define secure authorized review access; existing BUG-SEC-0001 public-upload access must not be bypassed by simply linking private files. Test text/file/both/empty upload and actual submitted revision using synthetic files only. No upload or content download was performed.

Family response currently returns assignments without submission status/feedback in PortalAssignment projection. Do not claim a completed submit→teacher feedback→student read loop solely because review row saved; the previously mapped family/teacher UI contract must be exercised end to end. Teaching summaries join students for names; invalid admin-origin StudentId could omit a row in inner join. Foreign-tenant joins need negative fixtures; no runtime data disclosure is claimed.

## G02 — Lesson form, related IDs and lifecycle

LEARNING-FORM-002 controls: batch selector (no required attribute/placeholder; auto-first when available), title required trimmed nonblank EF250, objectives optional textarea sent null when empty and trimmed server-side EF2000. Test no academy/no batches, inactive/foreign/stale batch, missing/space/Unicode/length title, optional omission/null/empty/length objectives. Button remains enabled while no academy; handler silently returns. Initial load does not inspect academy HTTP status before parsing and dereferences first element; empty/denied load is reported as migration/restart guidance.

UI sends courseModuleId:null and classSessionId:null; text explicitly labels module/session linking as a later refinement. No fields are counted for these absent controls. API accepts both optional IDs.

BUG-DATA-0015 extends from teacher endpoint to LessonPlansController.Create: only primary batch/title are validated; supplied module/session IDs are copied without existence, academy, course or batch checks. Test omitted/null IDs as valid controls, local matching references, foreign/nonexistent/wrong-course/wrong-batch references and future/cancelled session. Inspect SQL row and reader behavior, not just return status. A reference being optional does not permit invalid supplied values.

Create saves Planned row and empty200; UI clears title/objectives then load clears notice. List returns tenant id/batch/module/session/title/objectives/status ordered CreatedAtUtc descending, no pagination. UI displays Batch fallback if lookup missing; separate unavailable lookup from actual missing record. Empty lesson list has no explicit message in its ul; include empty guidance in visual acceptance.

Status PATCH allows exact Planned/Delivered/Skipped/MakeupNeeded; tenant-scoped404, assigns and saves empty200. No current-state guard, delivery timestamp, session completion, module progress change or automatic make-up scheduling. Define transitions/reopening and intended downstream effects; do not assume selecting Delivered marks attendance or completion. Teacher status route additionally checks batch ownership; same reference guard issue on teacher creation reused.

No admin status editor, link editor or plan edit/delete workflow is exposed. Existing teacher UI supports its own status controls. Test admin-created plan discovery by owning teacher, reassignment ownership, course/module changes and family progress display under permitted flags. Plan status is not a substitute for publication/privacy: family progress queries batch lesson plans without draft publication field.

## G03 — Access, integrity and failure boundaries

Assignments/LessonPlans map Core; AssignmentSubmissions maps AcademicGovernance. All three lack PermissionCatalog mappings, so global administrator paths work while ordinary academics.manage/scheduling.manage delegates are rejected. An academy may create Core assignments but not access admin review without AcademicGovernance. Confirm product plan intent and UI gating; no direct-controller PASS proves real HTTP access. Teacher self-service routes use their separate linked-teacher/batch ownership guards, previously mapped; do not count them again.

For List/Create/Publish/Submit/Review/Status test anonymous, foreign academy/entity, inactive account, suspended academy, missing module, revoked/expired grant, valid Owner/AcademyAdmin, PlatformOwner and appropriate teacher/family controls. Verify denied requests cause no rows, files, audit-success entries or notifications. Preserve private-student assignment targeting and no data from other academies.

Test400 model binding for invalid nullable timestamps/Guid, duplicate unique key, length constraint, unavailable SQL, canceled request and post-domain-save audit failure. Distinguish rejection from committed/unknown save. Frontend lists require batch lookup plus primary read; partial/403/500/transport/parse errors need actionable messages rather than incorrect database migration advice. Loads and saves lack version/request identity protection; test route switching and reversed responses.

No external delivery, customer homework, feedback or plan status was changed. No app runtime tests executed; real HTTP/SQL/review-ui cases remain proposals.

## G01 — Visual/accessibility acceptance

All three pages: loading, empty, denied, dependency failure, validation, saving, successful submit/review, failed save and readback failure. Viewports320/375/390/430,768,1280/1440 plus200% zoom; long student/batch/title/response, multiline feedback, many rows and native datetime controls. Check page scrolling, dropdown layers/anchor, date year usability, mobile keyboard and focus/return.

Assignment and lesson selectors/textareas rely on placeholders; verify accessible names, required/optional clarity and field errors. Publication checkbox must announce its consequence. Review action requires student/assignment context in visual and accessible name; prompt cannot provide multi-line preview/history and must have safe cancellation/focus behavior. Do not approve current browser screenshots as standard style without visual review. Success must be durable, not merely disappearance of inputs or a changed row.

## Outcome

Two new OPEN P1 source findings: BUG-DATA-0042 and BUG-FUNC-0030. Existing lesson-reference BUG-DATA-0015 and feedback BUG-FUNC-0003 extended. Adds3 controllers/9 actions,2 forms/9 lexical controls, no standalone JSX controls. Review prompt interaction specified separately. Cumulative70/82 forms,420 lexical+122 standalone=542/649 controls,56/70 complete controllers. Source counters are not runtime pass or percentage completion.

Next: music pieces, learner progress, practice logs and remaining learning-resource form. Then documents, operations and residual forms/shared controls. G01/G02/G03 remain open; Phase1 NOT CLOSED, Phase2 not started. No app repairs, commit or Azure deployment.
