# PENTA AI — whole-product conversational operating contract

Owner direction: 23 November 2026. Desktop Codex owns implementation.
Source inspected: AcademyDesk `29f908a494f2c0919cd1b5a29175c72735eee895`
plus the preserved working tree. This document corrects the narrow teacher-first
delivery interpretation; it extends the existing completion ledger and enterprise
program. It is a product/architecture contract, NOT a registered runtime catalog,
authorization policy, model qualification or statement of completed integration.

## 1. Scope that must guide implementation

PENTA is the primary conversational interface over the entire existing ERP.
It must cover supported, permitted business outcomes across Platform Owner,
Academy Owner/Admin, Teacher, Student and linked Guardian workflows. Teachers
were an example of a routing defect, not the product boundary or a sufficient
first release. The goal remains 100% conversational coverage of eligible tasks
and approximately 90% routine operational use through AI. Neither is achieved.

Users should describe their intended outcome in ordinary language. They should
not choose a module, know an API, memorize a command phrase or re-enter details
already provided. One prompt may contain several related reads and mutations.
PENTA must preserve intent through clarification, correction, interruption,
confirmation, execution, readback and recovery in the same conversation.

Understanding why a user asks means retaining their stated goal and using its
relevant relationships. It does not mean inventing an unstated motive or adding
unrequested fees, messages, accounts or other consequential actions.

All tiers understand the fundamental domains. Different analytical/execution
budgets do not justify returning students for a teacher question or omitting
fundamental domains from Mini. Pulse, Executor, Navigator, Twin and Autopilot
share this foundation; they are responsibilities, not five disconnected chats.

## 2. Existing Manual surface and intended conversational parity

The current source contains 89 Next.js page declarations, 94 controller source
files, 79 distinct controller type declarations and 430 HTTP attribute
declarations in `apps/api/Controllers`. These include aliases, protocol helpers,
public/system endpoints and AI routes. They are NOT 430 business outcomes or
proof of permissions, runtime correctness or AI completion. Reuse the existing
QA/COVERAGE semantic inventories and source-bound native reports; do not restart
their audit. Page aliases and repeated portal views must not inflate coverage.

In the table, controller names mean existing files/types in
`apps/api/Controllers`; this is a domain/source map. Operation-level field,
permission and acceptance rows still need runtime registry binding. A listed
Manual capability is not automatically approved for autonomous AI execution.

| Family | Existing Manual entry / backend source | Required conversational outcome |
| --- | --- | --- |
| Platform tenants | `/platform/control`; PlatformAcademies, Academies | Find/onboard tenants; explain lifecycle, subscription/limits and permitted configuration; resolve the target tenant under platform authority. |
| Platform business operations | `/platform/control`, `/platform-services`; PlatformControl, AcademyPlatformServices | Health/usage/onboarding/support summaries, tenant support cases/responses, billing records/status and announcements with correct platform/academy separation. |
| Academy operations | `/`, `/dashboard`, `/admin-intelligence`, `/branches`; Academies, Dashboard, AdminIntelligence, Branches | Profile/branch operations, source-backed priorities, capacity and operational summaries. |
| Students and intake | `/students`, `/student-onboarding`, `/student-management`, `/student-profile`, `/student-overview`; Students, StudentOnboarding, Profiles | Find/360, intake, profile/status changes, duplicates and history; conditional guardian requirements, optional fields and secure account handoff. |
| Families | `/guardians`, `/guardian-profile`; Guardians, StudentGuardians, Profiles | Find/create/update guardians, resolve and manage eligible links, explain current visibility. Link/access changes require their existing safeguards; no model-created authority. |
| Teachers and workforce | `/teachers`, `/teacher-onboarding`, `/teacher-profile`, `/teacher-overview`, `/staff`; Teachers, Profiles, Staff | Intake/search/profile, active status, assignments and permitted workforce operations; distinguish teacher records from portal identities, payroll and security administration. |
| Subjects/courses | `/courses`, `/curriculum`; Courses, CourseModules, CourseModuleStatus | Find/create/update courses/modules and permitted publication/lifecycle operations. |
| Academic governance | `/academic-governance`, `/academic-periods`, `/assessment-governance`; AcademicGovernance, AcademicPeriods, GradingSchemeLifecycle | Years/terms, prerequisite and grading configuration/lifecycle using existing validation. |
| Batches | `/batches`, `/batch-setup`; Batches | Search/create/update batches, resolve course/teacher/branch, delivery mode, capacity, timetable and enrollment status. Batch creation and class-session creation are distinct effects. |
| Enrollment and progression | `/enrollments`, `/batch-promotions`; Enrollments, BatchPromotions | Admit/waitlist, activate/pause/withdraw/complete, transfer and approved promotion; capacity, prerequisites, duplicates and history preserved. |
| Scheduling | `/schedule`, `/calendar`; ClassSessions | Calendar, source-backed availability, schedule/reschedule/cancel, teacher/room clashes, substitutes and time-zone interpretation. |
| Attendance | `/attendance`; Attendance and assigned TeacherPortal endpoints | Read/mark student and teacher attendance under the right scope; roster/session validation and bulk confirmations. Never treat marking time as proven arrival time. |
| Leave and make-up | `/leave`, `/makeup`; LeaveRequests, MakeupClasses | Request/read/decide leave where authorized; plan and schedule make-ups with existing eligibility/conflict rules. InPerson uses room details; Online/Hybrid require a meeting link under domain rules. |
| Holidays/events | `/holidays`, `/events`; Holidays, Events | List/create/update lifecycle or delete only where the existing operation supports it, with impact review. |
| Teaching work | `/assignments`, `/submission-review`, `/lesson-plans`; Assignments, AssignmentSubmissions, LessonPlans | Draft/create/publish assignments, scoped submissions and review/feedback, lesson-plan lifecycle. |
| Assessment/results | `/assessments`; Assessments, AssessmentResults | Create/publish assessments and record/read results under the same roster, grading and publication rules. |
| Practice/progress | `/practice-logs`, `/music`; PracticeLogs, MusicPieces, MusicProgress | Log/review practice, repertoire assignment/progress and authorized summaries; retain specialty support without limiting the school domain to music. |
| Resources/private media | `/resources`, Teacher/Family libraries; LearningResources, ClassMediaUploads, ClassMediaContent, ClassMaterialFiles, ClassMaterialDownloadTickets | Scoped resource creation/publication, notes, resumable attachments/recording, viewing/download fallback and revoked-access handling. File selection/recording can be an inline user interaction; the model cannot invent a file. |
| Fees and arrangements | `/fee-plans`, `/student-fees`; FeePlans, StudentFeeArrangements | Configure/explain eligible plans and student arrangements/admission fees using actual date/currency/business rules. |
| Invoices and collections | `/invoices`, `/payments`, `/finance`, `/finance-summary`, `/finance-reconciliation`; Invoices, Payments | Exact invoice/read/document, issuance, collection, status/reconciliation and recovery; never recalculate authoritative money with model arithmetic. |
| Finance controls | `/finance-adjustments`, `/finance-policy`, `/finance-governance`, `/fee-reminders`; FinanceAdjustments, FinanceGovernance, FeeReminders | Explain dues/follow-up, submit and separately decide permitted adjustments, queue eligible reminders; preserve current transaction, approval and credit/refund boundaries. |
| Expenses/payroll | `/expenses`, `/payroll`, `/teacher-payments`; Expenses, Payroll, TeacherCompensation | Expense intake/status and permitted compensation/payroll profile/period/payout operations, exact currency and duplicate/worker-period safeguards. Teacher self-view is not academy-wide finance access. |
| Admissions/growth | `/leads`, `/sales-marketing`, `/sales-campaigns`, `/trial-bookings`; Leads, SalesMarketing | Lead intake/stage/notes, trials, conversion, campaign planning and supported lifecycle; distinguish recommendations from actual conversions/sends. |
| Communications | `/communications`, `/message-templates`, `/communication-preferences`, `/communication-settings`, `/meeting-links`; Notifications, CommunicationTemplates, CommunicationPreferences, CommunicationSettings | Draft/review messages, templates, audience/consent, current provider readiness and permitted settings. Queued, sent, delivered, read and failed are different statuses; secrets go through secure controls. |
| Documents/compliance | `/compliance`, `/certificates`; Compliance, Certificates, CertificateVerification | Consent/review/withdrawal, scoped documents, certificate issue/status/branding/verification subject to existing rules, eligibility and disclosure limits. |
| Operations/reporting | `/reports`, `/activity`, `/work-queue`, `/data-operations`; AcademyExports, AuditLogs, AdminWorkItems, StudentImports | Authorized queries/exports, CSV validation/import and work-item lifecycle with correct partial outcomes, sanitization and audit. |
| Teacher self-service | `/teacher`; TeacherPortal | Own calendar/classes/batches/rosters/progress; permitted profile/leave, attendance, assignments/publication, submissions/review, lesson plans, assessments/results, resources/materials, practice review, session status and own payments. No ambient access to other teachers' or academy finance records. |
| Student self-service | `/portal`; Portal | Own classes/enrollment/progress, assignments/submission, practice, resources, permitted fees/documents/certificates, profile/leave, notifications/events and read status. Use authenticated student identity, not a requested arbitrary student ID. |
| Guardian self-service | `/portal`; Portal and StudentGuardians policy | Resolve linked child, then only currently granted academic/finance/documents/leave actions; recheck revocation every operation. Do not force a single child context when several are linked. |
| Identity/access boundaries | `/login`, `/register`, `/portal-accounts`, `/staff`, `/access-review`, `/access-review/sign-off`; AuthSession, PortalAccounts, AcademyRoles, AccessGrants, AccessReviews, Staff and relevant PlatformControl actions | Explain allowed access and direct to secure governed controls. Passwords/credentials, role/security changes and security configuration are not unrestricted conversational tools. Preserve the existing prohibition on model role/security changes. |
| AI infrastructure (not Manual outcome count) | `/penta` and subroutes, `/platform/ai`, `/teacher/ai`, `/portal/ai`; Penta, PentaChat, PentaConversations, PentaFinance, PentaOnboarding partials, PentaPrivateContext | Reuse protected planning/draft/approval/execution/recovery adapters. Synthetic/test-only adapters and protocol endpoints are not qualified free-prompt business outcomes. |
| System/public routes (not ERP task count) | WeatherForecast; CertificateVerification; auth/system mappings in Program | Keep sample/system/public endpoints out of the executable business catalog unless a separate justified capability is defined. Public verification is narrower than internal certificate access. |

The authoritative per-operation authorization comes from current Identity,
host/domain policies, subscription modules and relationships, not this table,
navigation visibility or the model. Platform permission does not grant blanket
access to private student/teacher/finance records in every academy.

## 3. The operational knowledge system — the requested “bible”

Implement a versioned, host-owned capability/task registry, derived from and
validated against real services. Do not put every record or an entire ERP manual
into every prompt. The model needs a compact domain map and the relevant allowed
task schemas/rules; retrieve current records through authorized tools as needed.

Each task descriptor must identify:

- Stable ID/version; goal, entity relationships and supported read/mutation kind.
- Manual equivalent, actual API/shared service and typed request/result schema.
- Required, optional and conditionally required fields from domain validators;
  types, limits, enums, units/currency, locale/time zone and permitted defaults.
- Reference resolution and disambiguation sources; same-name people are not
  proof of identity. Detect likely duplicates, and rely on transactional unique/
  business checks at commit. Never silently merge people or reuse a record.
- Preconditions, dependent tasks, returned-ID bindings and allowed transitions.
- Current actor/tenant/module/record policy; sensitive fields, redaction and
  forbidden operations; approval/confirmation scope and expiry.
- Preview/commit transaction boundary, locks, concurrency/version requirements,
  idempotency, mandatory audit, receipt/recovery and compensation constraints.
- Output evidence/source/time, paging/limits, unavailable-data semantics and
  permitted frontend artifacts (record card, comparison, review, attachment).
- Availability: implemented host adapter, gated/off, provider unavailable,
  unsupported or accepted model-inclusive outcome; do not conflate them.
- Positive and negative test/evidence references on exact source/model pins.

Permission-filter descriptors before model interpretation and authorize again
before each actual read or commit. An unavailable operation may be explained
with a useful Manual path; it must not be silently replaced by a different tool.
Learning/memory can store scoped preferences or authorized corrections, never
grant permission, redefine a fee rule or substitute for authoritative data.

## 4. One persistent conversation and dependent task plan

Maintain a versioned, actor/tenant-bound goal and task graph, not one global
“current learner” or one tool-call slot. Each task keeps its entity references,
supplied fields/provenance, missing/invalid fields, dependencies, proposal version,
approval binding, dispatch/request ID, status, receipt and verified results.

Expected lifecycle:

`understand → resolve/inspect → clarify → validate/preview → confirm/approve`
`→ execute dependent steps → verify/read back → summarize or recover`

These are observable workflow states, not a requirement to expose hidden model
reasoning. Show useful progress and results, not internal chain-of-thought.

- Recognize multiple requests and preserve them when asking one concise question
  or a small related group. Apply answers only to the intended task/field.
- Accept several details at once, corrections and natural references (“that
  batch”, “her”, “the second one”) only when source-backed and unambiguous.
- Distinguish replacing an entity/topic from refining the current task. A
  teacher request must clear incompatible learner filters and references.
- A correction to a shared prerequisite invalidates affected previews and
  approvals; retain valid unaffected information and completed receipts.
- “No, use the existing batch” replaces that task and its dependent reference;
  it does not silently discard the enrollment or recreate the student.
- Date phrases use academy time zone and explicit year/range when ambiguous.
- Optional fields remain optional; present legitimate defaults before approval.
  Never fabricate birth dates, email, capacities, teachers, courses or consent.
- Server-resolved IDs and dependency outputs are authoritative. Treat model
  references as proposals; reject cross-tenant or type-mismatched references.
- Re-plan after validated tool observations within bounded calls/time/cost.
  Do not blindly run a precomputed list when a preceding step changes facts.
- Unknown dispatch outcome enters recovery. Read receipts before another write;
  refresh/retry must not recreate the same student, batch, enrollment or payment.
- Cancellation stops unexecuted work; it cannot undo already committed records
  without a separately supported and approved domain operation.
- Persist safe transcript/structured state with minimization, retention and
  current-visibility rechecks. Avoid sending the full raw record history to Mini.

## 5. Canonical acceptance journey: student + batch + enrollment

Example prompt (illustrative invented data, NOT executed):

> Create student Anika Rao, born 2015-04-10. Create an online beginners batch
> called Tuesday Explorers, capacity 8, and assign her to it.

PENTA must:

1. Recognize intake, batch creation and enrollment as linked tasks. Batch name
   is not a course name; “assign her” means enrollment after identity resolution.
2. Resolve current permissions and permitted course/teacher/branch choices,
   inspect likely existing matches and keep all supplied values.
3. Use current `StudentOnboardingRules.RequiredFields`: first/last name and DOB;
   minor guardian first/last name and email; optional student phone/email/address
   remain optional. The minor condition changes with the relevant date.
4. Ask for missing guardian details, course and meeting link; resolve optional
   teacher/branch/date/schedule choices according to the selected operation.
   Do not invent them. If the user wants actual scheduled classes as well as a
   batch pattern, identify that additional operation explicitly.
5. Validate with existing shared rules. Batch capacity and delivery/link/course
   rules belong to Batches; admission and duplicate/waitlist/prerequisite rules
   belong to EnrollmentMutationOperations/AdmissionRules, not model prose.
6. Show a concise combined review: student, guardian/link settings, batch,
   enrollment status/date and defaults/omissions. Bind confirmation to the
   actual proposal and current policy; obtain any separately required approval.
7. Execute dependencies with actual returned IDs and revalidated authority.
   Use an atomic shared domain/Identity/audit transaction only where supported.
   Existing separate endpoint calls are NOT automatically one atomic operation.
   If the combined flow needs a new coordinator, design/test its transaction
   boundaries before exposing it. Never hold a DB transaction over model calls
   or user clarification. External effects cannot be promised SQL rollback.
8. Read back and report each committed result with references. On partial
   completion, say exactly what succeeded, what failed and what is pending.
   Recover/retry only unfinished steps; never manufacture an all-or-nothing
   promise or delete successful records to hide a later failure.

Additional required turns: “Actually she is an adult” must request the corrected
DOB rather than override age rules; “use the existing Tuesday Explorers” must
disambiguate and check eligibility/capacity; a double click/reloaded browser must
not duplicate effects; enrollment denial must not be reported as full success.

Sources: `StudentOnboardingController.cs`, `StudentOnboardingService.cs`,
`StudentOnboardingRules.cs`, `BatchesController.cs`, `EnrollmentsController.cs`,
`EnrollmentMutationOperations.cs`, `EnrollmentAdmissionRules.cs` and existing
PentaOnboarding/Enrollment protected workflow adapters. No shared service for
batch creation is claimed from the current inline controller implementation.

## 6. Natural answers and meaningful speed

Respond to the actual question first. Use narrative, a table or a result card
as useful, with source/time/currency where material. Separate verified facts,
recommendations, unavailable data, pending approval and completed actions.
If two people match, explain the choice needed; do not guess. Empty data is not
permission denial, and neither is a successful completion of an unrelated task.

Use narrow permission-aware catalog retrieval, bounded/paged tools, safe parallel
reads where independent, deterministic aggregates and scoped short-lived context
to reduce latency. Mutations follow dependencies and consistency rules. Measure
end-to-end p50/p95, clarification burden and effect correctness; current CPU
inference timing is not a promise of immediate answers. Larger weights/training
are considered only after orchestration/tool defects are addressed and evaluated.

## 7. Mutation, deletion and unavailable features

Read/create/update/delete/lifecycle operations are conversational where the
existing application and security policy permit them. Do not implement a
generic arbitrary table/SQL write or promise DELETE for every entity.
Deactivation, withdrawal, cancellation, voiding, consent withdrawal and physical
deletion are different domain operations. Ask which is intended when ambiguous;
show impact and obtain required confirmation. Retain finance/audit/history and
relationship safeguards. Conversational memory cannot override those rules.

Credentials, role/security administration and production infrastructure retain
their secure human-governed boundaries. The conversation can guide the authorized
user there without collecting secrets into the chat. If a needed capability is
missing, return an honest limitation and useful next option; never fake success.

## 8. Completion evidence and next implementation boundary

- Existing Manual/domain implementations and protected guided adapters are
  substantial and retained. Historical source-bound tests remain valid evidence
  for what they tested, not proof of unrestricted live conversation.
- Current live Mini exposes `SearchLearners`/`GetLearner`, a learner-specific
  state and one tool call; it is not whole-product conversational parity.
- The two-domain untracked AcademyRead contracts and Mini interpretation draft
  are inactive, untested scaffolding. Preserve them; do not present them as the
  universal planner, wire them in as the final solution or claim a teacher fix.
- First implementation boundary: common task/capability descriptors and typed
  dependent-plan/clarification/history/recovery contracts that cover the entire
  mapped surface, then bind existing host adapters incrementally. Validate both
  the compound intake/batch/enrollment journey and entity-switching read cases.
- Acceptance per role/outcome must include real model + current Identity +
  disposable SQL + browser, missing/optional fields, corrections, duplicates,
  partial failure/recovery, denied/revoked authority, mandatory audit failure,
  AI-down Manual fallback and existing enterprise/native regression gates.
- Track each task separately as source-mapped, host-bound, conversationally
  integrated, native-tested, model-qualified, browser/device-tested and release-
  accepted. A catalog entry or fluent answer must not tick every stage.

Continue with Sol High for this architecture/domain/security integration. Routine
registry binding and visual work can use Sol Medium after the foundation is
validated. Preserve Orbit+Aura dark / Prism light, Geist and shared AI/Manual
controls. No deployment, model loading/activation change or production action is
authorized by this scope clarification.
