# Batch 20 — Assessments, results and assessment policy

2026-09-28. Source specification; runtime NOT RUN. Completes AssessmentsController (List/Create/Publish) and AssessmentResultsController (List/Upsert), both declared in apps/api/Controllers/AssessmentsController.cs. Five actions reuse ACADEMIC-API-010, ACADEMIC-API-011, ACADEMIC-API-012, ACADEMIC-API-016 and ACADEMIC-API-017. Reuse E2E-ACADEMIC-001. TeacherPortal/Portal and grading scheme controllers are previously mapped dependencies, not additional controller completions.

One native form: ACADEMIC-FORM-005, seven lexical controls. Four standalone controls: assessment, score, grade, remarks. Assessment-governance is a read-only policy view with no native form. No application or database mutations were performed.

## G02 — Create assessment

| Field | UI, API and persistence contract / tests |
| --- | --- |
| batch | Handler needs academy/batch; defaults first loaded batch. API checks same-academy existence, not IsActive/open status. Empty/foreign/malformed ID, inactive batch, empty list, lookup denial, and stale selection; decide inactive-batch historical assessment policy |
| title | Required native input; API trims/nonblank; EF250 required. Null/missing/empty/whitespace,250/251, Unicode and HTML-like text rendered inertly. Repeated titles are allowed; index is not unique |
| assessment-type | UI Performance/Exam/Recital/Test/Practical; default Performance. API accepts other trimmed text, null/blank defaults Assessment; EF40. Test omitted/blank,40/41 and arbitrary value. Do not invent a server enum restriction |
| maxScore | Required number min1 step0.01. API only positive decimal; EF decimal(10,2), maximum representable99999999.99. Test blank/0/negative/1/fraction, precision boundaries and malformed/nonfinite/overflow JSON. Direct API accepts positive below1 that UI disallows; values below0.005 can round to0 in SQL despite positive precheck, so real SQL test must inspect stored max and later grading division. No SQL reproduction claimed |
| grading-scheme | Optional empty ->null; API permits only existing active same-academy scheme. Test omitted/null/empty/foreign/disabled/deactivated-during-submit and no schemes configured. Summary omits GradingSchemeId, so readback cannot show which policy is attached; use persisted entity for verification |
| scheduled-date | Optional; blank sends scheduledAtUtc:null regardless of time. Date present uses browser-local Date construction then toISOString. Test valid/leap date, date clear, malformed/default direct API DateTime and DST gap/fold. There is no explicit academy timezone conversion or stated zone label; agree intended zone before classifying timezone differences as defects |
| scheduled-time | StandardTimeField defaults10:00. Hour/minute/AM-PM picker emits24-hour string, default15-minute choices. Test00:00/12:00/23:45, rollover, custom existing minutes, retained time after date clear and mobile keyboard viewport. No manual clear UI is rendered by this control |

UI always sends isPublished:true as a JSON boolean, with no draft/publication choice. API accepts either boolean, default false when omitted under ordinary binding. Test actual JSON true/false versus strings/null, published/draft creation and visibility. Existing teacher string-boolean issue BUG-API-0005 does not apply to this administrative payload.

Create returns201 with summary; Location points to an ID resource for which this controller has no Get-by-ID action. List is available for readback, tenant-scoped and ordered descending scheduled timestamp, including unpublished assessments and null dates. There is no update/delete action. Test stable expected ordering for ties without assuming a secondary sort. Publish PATCH scopes ID+academy, returns404 when absent, and sets either flag with empty200; repeat calls accepted. Publishing neither validates result completeness nor sends notifications here. No academic year/term reference or closure check exists in Assessment; do not claim closing a term locks grading.

## G02 — Result editor and grading

assessment selector defaults to first loaded assessment. Roster joins current Active batch enrollments with loaded students; duplicate active enrollment rows can duplicate learner rows/React keys. Results for no-longer-active students are omitted from this editing roster even when retained in storage. Test no assessment, empty roster, duplicates, switched assessment, former learner and inaccessible lookup independently.

Standalone score is a number input min0/max selected.maxScore, step0.01, outside a native form. Save is disabled for empty string, but string0 is nonempty and can save. Number.isNaN protects only NaN; native min/max/step are not automatically enforced by the type=button handler. Server checks inclusive0..MaxScore and decimal(10,2). Test exact boundaries, fractional precision, missing score binding, invalid JSON, range rejection and oversized values. Fresh SQL readback, not response echo, establishes persisted rounding.

Standalone grade and remarks are optional; empty strings ->null, whitespace remains nonempty in UI then trims to empty onAPI. EF limits30/1000. Test null/omitted/empty/spaces,30/31 and1000/1001, Unicode and safe text rendering. For an existing result omitted optionals intentionally replace old values withnull; approve full-replacement contract and verify callers preserve fields they do not intend to clear. UI always sends result isPublished:true, so resaving a draft result publishes it without a separate switch.

Upsert requires same-tenant assessment and student, then finds unique academy/assessment/student result. New and updated rows return200. List scopes result rows to academy+assessment but does not verify parent existence; unknown/foreign assessment ID yields empty list instead of404 once route tenant access passes. This is a contract distinction, not proof of data disclosure. Unique index prevents duplicate stored results, but concurrent first inserts may fail one request; concurrent existing updates have last-writer behavior without a version token. Specify conflict/retry behavior and retained drafts.

BUG-DATA-0034: administrator upsert does not verify enrollment in assessment batch; teacher writer and administrator UI require active batch enrollment. Test never-enrolled/wrong-batch/waitlisted/paused/completed/active/foreign students. Historical result correction may need an explicit exception; do not silently impose active-only policy on all historical work.

Admin grading: with scheme and request grade null, compute score*100/max >= PassingPercent ->Pass elseFail. Nonnull grade overrides, including trimmed empty. Without scheme, grade is nullable manual text. BandsJson is not used for letter-grade calculation. Threshold below/at/above with precise decimals,0/100percent, manual override, whitespace and missing scheme tests required. Existing linked scheme is loaded without IsActive check; deactivation blocks new assessment selection but does not stop grading existing ones. Scheme does not snapshot into assessment; document effect of any later supported policy change.

BUG-DATA-0033: after automatic grade is read back, ResultRow copies it into editable grade state. Changing only score then resubmits old Pass/Fail as an explicit override, preventing recalculation. Automatic and deliberate manual grades need distinct behavior. Teacher result writer never consults grading scheme, so null-grade saves for the same assessment remainnull. Compare identical score/grade inputs across writers and preserve approved manual overrides.

BUG-DATA-0018 extends to assessment selection: assessmentId changes immediately but old results remain during pending/failed GET; rows are keyed only by student.id. Shared learner rows retain old score/grade/remarks while save targets new selected assessment. Late GET and post-save reload can overwrite the result array for another selection. Test A/B shared student, distinct scores, slow/error/reversed responses, switching during save and correct request target/body. Row useEffect can also overwrite unsaved edits when another learner save reloads the entire results list; include two edited learners and save one before the other.

## G03 — Feature access and dependency gates

Global academyId filter plus active Identity middleware applies to both admin controllers. Assessments requires AcademicGovernance module and academics.manage for delegated roles; Owner/AcademyAdmin bypass permission check but still require module/active tenant. Intentional platform-owner cross-academy bypass remains separate. Test anonymous, inactive account, suspended tenant, revoked/expired grant, foreign route/entity IDs and allowed controls through real HTTP.

BUG-FUNC-0020: AssessmentResults has no catalog permission and falls back to Core module. Delegated academics.manage can create assessments but cannot read/write results; admin with Core can access existing results after AcademicGovernance is disabled. This differs from setup and needs explicit mapping/policy.

Assessments page requires successful batches,students,enrollments,assessments and active-schemes lookups before populating all lists. An academics.manage-only user lacks batches.manage/students.manage; active schemes controller is unmapped (existing BUG-FUNC-0015). Test each failure separately from results permission failure; no generic extra-role grants as a substitute for approved minimal lookup access. Optional no-scheme selection still depends on successful schemes lookup.

Teacher paths use linked teacher/academy and OwnsBatch; their same-tenant ownership checks and active-enrollment score guard are present. Lifecycle/module guard gaps from BUG-SEC-0003 require academic endpoint variants too, because no-academyId paths bypass the global route filter. Domain teacher-disabled, Identity-disabled, academy-suspended and batch reassignment must be tested separately. Do not equate administrator grants with teacher ownership.

Post-domain generic audit is a second SaveChanges (BUG-API-0002); distinguish a committed result and failed audit/readback from rejected score. Response status alone cannot establish rollback. SQL assertions inspect result, assessment, audit and notification separately.

## Publication and family readback

Teacher published result save queues an InApp notification every time, including repeated edits/retries; administrator save does not queue one. Teacher queues based on result.IsPublished even if assessment.IsPublished=false. Test idempotent notification policy and deliberate result edits before claiming notification delivery succeeded.

Portal.Student filters results by correct academy/student, result.IsPublished and currently active enrollment batch IDs. Its assessment join does not check parent assessment.IsPublished. Thus unpublishing an assessment does not hide an already-published result from an otherwise eligible learner. Treat parent-vs-result publication semantics as an explicit unresolved policy; test all four flag combinations and define whether parent unpublish must revoke visibility. No claim of access to another student is made.

After a learner completes/transfers out of a batch with no remaining active enrollment, that batch's published results disappear from portal response even though result rows remain. Specify approved historical transcript retention and assert behavior before/after transfer/completion. Guardian CanViewAcademicProgress gates output; test current/revoked guardian links and per-category visibility using existing family coverage. These read dependencies do not add controllers to completion totals.

## G01 — Feedback, policy view and visuals

Create has no busy/catch/finally: transport rejection and double taps need recovery tests. On success it clears title/date/scheme and reloads with no positive message; selected batch/type/max/time persist and an existing assessment selection is retained, so new assessment need not become selected. Result save uses try/finally but no success notice. Shared savingStudentId covers one learner: overlapping saves can replace/clear each other's busy indicator. Specify per-operation state and verify other learner's unsaved data survives readback.

Result save catches both POST and reload failures and always says score must be within range, including403/network/failed GET after successful commit. Create similarly labels any nonOK as title/max-score error. Extend BUG-FUNC-0003; no new async currentTarget reset finding. Require actionable validation/permission/transport/readback distinctions, durable announced success and correct focus/return after verified save.

Assessment-governance loads first academy then all schemes from previously reviewed AcademicGovernanceController. Academy response status is not checked before json; malformed/non-array/error responses reach generic catch. Active count and empty message render alongside Loading/error when schemes remains empty. Specify separate loading/error/empty/ready states; successful list includes inactive schemes, displays active/inactive and passing percentage, no bands details or editing controls. No form or new controller is counted for this read-only page.

Desktop/mobile320/360/390/768/1280,200%zoom,both themes, landscape and virtual keyboard: clear labels/filled field bounds, readable long names/title/remarks, grid stacking, result row association and tap targets. Standard select/date/time anchors must stay adjacent and inside viewport on scroll/resize. Source time picker sets fixed left and minimum280 width without horizontal clamp; test narrow right-column trigger and keyboard-shrunk height. Reuse shared geometry/focus cases, including Escape, focus return and screen-reader labels; approved browser/device evidence remains outstanding. Navigation between assessment/policy/academic pages must update selected label and preserve page scrolling.

## Outcome / next scope

Three new OPEN static P1 findings: BUG-DATA-0033 grading inconsistency, BUG-DATA-0034 wrong-batch result eligibility, BUG-FUNC-0020 results permission/module mismatch. Existing stale-selection and feedback findings extended. Cumulative58/82 native forms,332 lexical plus108 standalone controls =440/649 and42/70 complete controllers. NotificationsController remains partial;28 controllers remain incomplete.

Next: sessions, scheduling, attendance, leave and make-up flows, split into bounded reviews as needed. Remaining learning/communications/documents/operations gaps and final cross-check still determine Phase1 closure. Earlier5–7 jobs was a planning estimate, not guaranteed completion after a fixed number of turns. Phase1 G01/G02/G03 remain open and Phase2 has not started. This batch provides source specifications; application runtime remains NOT RUN.
