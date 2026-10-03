# Batch 26 — Music progress, practice logs and learning resources

Learning-form continuation: [Batch 39](39_LEARNING_FORM_SEMANTICS.md) supplies exact G01/G02 control, payload, API and EF rules for LEARNING-FORM-003–006 and the two composed status/review controls. It corrects this batch's inactive-piece auto-selection statement: the loader selects from the active-only piece list; stale prior selection can still reference a deactivated piece. BUG-DATA-0054 records a distinct zero-score loss. Historical cumulative counters below are not current.

2026-09-29. Source specification; runtime NOT RUN. Completes MusicPiecesController (List/Create/SetActive), MusicProgressController (List/Assign/Update) and PracticeLogsController (List/Create/Review):9 actions. LearningResourcesController was mapped in batch03; this batch completes its remaining browser form and reuses LEARNING-API-007–010 without double-counting that controller.

LEARNING-FORM-003 adds4 lexical controls, LEARNING-FORM-004 adds3, LEARNING-FORM-005 adds5 and LEARNING-FORM-006 adds6. Progress-state and teacher-feedback inputs are composed outside native forms and are described below without inflating form counts.

## G02 — Repertoire and student progress

LEARNING-FORM-003 creates a repertoire item: title is required in browser and API, composer is optional, instrument defaults Piano and difficulty defaults Beginner. Genre and duration are absent from the page and sent null. Test blank/space/Unicode/250/251 title, optional composer/genre/instrument values and EF limits, arbitrary difficulty, null/negative/zero/large duration through direct API, duplicate title policy and inactive item display. The UI only reports the fixed “Piece title is required” message for every rejected POST, not the actual server failure; a successful reload clears message state without a durable success notice (existing BUG-FUNC-0003).

LEARNING-FORM-004 assigns a selected learner and repertoire item with optional target date. The page auto-selects the first loaded learner and **active** piece. A previously selected ID can remain stale after that piece is deactivated and omitted by the List endpoint. Test empty academy/dependency lists, stale selection, invalid/foreign/deactivated student or piece, duplicate assignment/409, target-date locale/year boundaries and rapid duplicate submit. Assign validates same-academy existence and the unique learner/piece index is a positive duplicate guard, but does not validate active status or enrollment eligibility; product policy must define whether a historical/inactive repertoire assignment is permitted.

MusicProgress Update accepts the five known status labels case-insensitively, then directly replaces status, score, notes and target date. No score range, completion evidence, transition or version rule is encoded. Test all state-to-state pairs, arbitrary negative/large/precision scores, blank/long notes, date changes after Mastered, stale concurrent updates and a fresh read. Do not assume a score scale or state machine without an approved policy. The inline status selector has no success/error feedback and update failures share the same transient page message; assert row state only after authoritative readback.

## G02 — Practice log integrity and review

LEARNING-FORM-005 contains learner selector, date, minutes, optional focus area and optional notes. Browser min=1 is only client-side; API checks less than one but otherwise accepts domain entity values. Test blank/malformed date, zero/negative/fractional/large minutes, optional whitespace and EF limits, duplicate learner/date records, date boundaries, stale learners and loading/failure states. The page blindly parses academy, student and log responses and lacks catch/failure messaging; a successful Save log only prepends the returned row and supplies no positive status notice (existing BUG-FUNC-0003).

BUG-DATA-0043: PracticeLogsController.Create accepts PracticeLog directly. It replaces Id and AcademyId but takes StudentId, Status, TeacherFeedback and ReviewedAtUtc from the caller and performs no learner existence/academy/eligibility verification. The mapped entity/index has no learner foreign-key guard. Review is the intended server-owned path and sets status Reviewed/time, so creation must not silently bypass it.

Review accepts nullable feedback, trims it, marks the record Reviewed and replaces its row in the page. It has no visible author/reviewer attribution, version guard, required feedback rule or retry/readback confirmation. Test empty/space/2000/2001 feedback, repeated review, concurrent learner create/review, 404/403/500/transport failures, keyboard behavior and the correct retained input after an error. Teacher-feedback input is a composed interaction, not a second native form.

## G02 — Learning-resource form and audience scope

LEARNING-FORM-006 has required title, either optional web link or uploaded file, type, optional subject and optional audience batch. File selection clears the link and disables it; clearing a selected file without a browser reset must be tested. Link creation requires an absolute URI; upload permits listed filename extensions with a 50 MB request limit. Test title/link/file combinations, URI schemes, empty file, extension/MIME/content mismatch, 50 MB boundary, filename Unicode/path payloads, optional type/scope fields, retry after upload/save ambiguity, reload and responsive file input behavior.

Existing BUG-SEC-0001 remains the governing P0: uploads under webroot are publicly statically served. This batch adds no duplicate security issue. Test it with synthetic resources and direct URL/HEAD/Range access under anonymous, foreign-tenant, revoked and unpublished conditions; do not access customer files.

BUG-DATA-0044: ValidateScope only confirms the batch and course independently belong to the academy. A Batch has CourseId, but resource creation/upload never requires it to match supplied courseId. The family reader applies both filters, allowing a stored published resource that is contradictory and absent from its intended batch audience. Test matching/mismatched/null scopes for both links/uploads and fresh family visibility.

Resources UI has no publish toggle (always submits true), no edit/unpublish/list visibility control and no success notice after load clears “Publishing resource…”. List returns all academy resources including unpublished rows; portal reader filters published. Define publication ownership and intended admin read versus learner visibility. Native link anchors open the stored URL in a new tab; test safe scheme/content policy, accessible label/type/audience, missing lookup fallbacks and link failure. No file, link, message or real learner action was performed.

## G03 — Access, failure and visual acceptance

Correction, Batch 37: MusicPieces, MusicProgress and PracticeLogs map **Core**; LearningResources maps **Certificates**. There is no Learning module in SubscriptionPlanCatalog. All four lack a PermissionCatalog mapping, so ordinary delegated callers are denied even with all catalog grants; same-tenant Owner/AcademyAdmin callers must first pass academy-active/module checks. The platform-owner flag bypasses those filter checks, but not Program's inactive-user check or action-local scope validation. [Batch 37](37_EVIDENCE_RECONCILIATION.md#corrected-action-permissions) specifies each action and the dashboard/import exceptions. These are static expectations, not runtime authorization results.

Music, resources and practice page loads depend on academy plus multiple lists. Test empty/denied/partial/malformed/slow/reordered responses, stale route/academy switch and failed refresh after a committed write. Ensure feedback distinguishes a rejected write from an unknown committed result. All forms need a durable success state and safe return/readback; current cleared or absent messages must not be treated as confirmation.

At 320/375/390/430, 768 and 1280/1440 plus 200% zoom, test field labels/optional status, native date-year navigation, standard-select positioning/layering, file picker focus, long repertoire/resource names, many progress rows, mobile keyboard, page scrolling and Review focus return. Shared dropdown and keyboard defects remain governed by BUG-UI-0001/0002; no visual PASS is claimed.

## Outcome

Two new OPEN P1 source findings: BUG-DATA-0043 (practice-log entity binding and unchecked learner/review fields) and BUG-DATA-0044 (inconsistent resource batch/subject scope). Existing BUG-FUNC-0003 and BUG-SEC-0001 extend to this area. Adds3 controllers/9 actions and4 forms/18 lexical controls; the composed progress/review interactions are specified without inventing standalone inventory entries. Cumulative74/82 forms,438 lexical+122 standalone/composed=560/649 controls,59/70 complete controllers. Source counters are not runtime pass or percentage completion.

Next: documents, operations and residual forms/shared controls. G01/G02/G03 remain open; Phase1 NOT CLOSED and Phase2 not started. No app repairs, commit or Azure deployment.
