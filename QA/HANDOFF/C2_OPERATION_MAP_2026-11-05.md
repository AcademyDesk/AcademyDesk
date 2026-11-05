# C2 — read-only Manual-to-PENTA operation map — 5 November 2026 IST

Owner/environment: Cloud Codex, managed cloud. Status: STATIC MAPPING IMPLEMENTED / structurally VALIDATED; runtime, semantic/model, host and production acceptance NOT ESTABLISHED. Branch in both repositories: `codex/cloud-c2-operation-map-20261105`.

## Source and exact scope

Academy task base: `34395a5155db67e868336f6ca9126fc2889a4fac`; application source is still the assigned `8d7a02c87d867d9f1609aa524d3f3a04f0d7a934` host checkpoint. Mini task base: `d21f3331de968af233e4ba4062c58291e8f18af5`; production Core files still match paired `c3c61b9ef3f239035c8b8f46649c66997e98f2b6`, with C1 fixture/test corrections recorded separately. Both checkouts were clean and remote claims were checked before starting. The transient executor disconnect recovered; no environment was replaced or published.

Cloud owns only the two C2 report/CSV paths in each repository, claimed and pushed before generating maps. No application, API, SQL, migration, frontend, tests, workflow, dependency, original catalog or shared handoff/tracker changes. Local L1 remains owner of protected host/API/SQL/migration validation. This packet reuses existing QA evidence locators and does not reopen the whole audit or rerun already-scoped business suites.

## Read this map correctly

[Current declaration CSV](C2_CURRENT_ACTION_MAP_2026-11-05.csv) has one row per current attributed HTTP controller action. Each row records current source/hash/line, family, domain path/direct reviewed shared-service call, typed tool or limited analogue, binding stage, approval/receipt gap, Manual UI route candidates, existing case/evidence locators and lexical regression candidates. Everything is explicitly NOT_ESTABLISHED for production acceptance. A missing locator is a traceability gap, not proof the function or test does not exist. An existing candidate is not an executed test or an equivalent workflow.

Roslyn syntax parsing used already-installed SDK assemblies, without building/running the application or downloading packages. It includes partial controllers, multiple classes in a file and combined HTTP attributes. Literal controller routes and symbolic route expressions are retained as expressions; no runtime endpoint, authorization, role grant or constant-expression evaluation is invented. The helper/output/validation provenance is retained outside repositories under `/workspace/c2-operation-map-20261105`.

## Census and source reconciliation

| Inventory | Actual bounded result |
| --- | --- |
| Current controller sources | 81 files, 78 declaring controller classes, 359 attributed HTTP action declarations |
| Source-role categories | 285 Manual declaration candidates; 58 PENTA host/protocol declarations; 15 retained security/scaffold exclusions; one additional credential-flow hold |
| Original private catalog | All 317 IDs retained, including its 302 historically eligible declarations and 15 exclusions |
| Catalog joins | 306 direct declaration identities, three explicit declaration reconciliations, one ambiguous legacy parse, seven actions absent from this checkpoint |
| Current rows outside a resolved legacy identity | 51; includes new host/transport declarations and corrected extraction, not 51 newly delivered business workflows |
| Existing case/evidence locators | 281 current rows have legacy case IDs; 101 have direct lexical controller/method test candidates; 290 have literal controller-route UI candidates. These are locators only |

The original catalog contains a duplicate Assessments.List identity on two source lines: the later declaration actually belongs to AssessmentResultsController. The three explicitly reconciled rows are that later List, AssessmentResults.Upsert and the former HolidayDelete.Delete. Both old holiday-delete IDs remain visible and point to one current Holidays.Delete candidate; equivalence/compatibility still needs review. Do not silently drop or double-count them.

The legacy GradingSchemeLifecycle.Status.GET row swallowed both attributes on a one-line controller; current syntax distinguishes Active.GET and Status.PATCH. It remains AMBIGUOUS rather than automatically turning a read into a mutation. The seven Meetings declarations are absent in this checkpoint; the preserved source/program is not claimed lost or newly regressed. The private reconciliation CSV preserves every original ID and its unresolved state. Of 309 resolved legacy rows, only 14 whole-file raw hashes match; 295 differ. Byte differences are not automatically semantic changes, failures or obsolete evidence.

These declarations are not user-outcome denominators. GET/HEAD/POST transport alternatives, protocol callbacks, duplicate identities, security exclusions and credential-bearing subsets must be reconciled before measuring 90% conversational operations. Neither 302 old declarations nor 285 current candidate declarations is an approved workflow denominator; no percentage or production-qualified operation count is claimed.

## Concrete binding findings

- Student intake and teacher record creation have actual shared services, typed field adapters, protected review/approval/execute/receipt code. They remain Testing-only, credential-free subsets. This does not cover Manual account/role provisioning, all fields, all roles, real interpretation or L1 acceptance.
- Enrollment Create/Update/Transfer share deterministic domain operations and have three typed field/tool constants. A protected enrollment conversation, approval/execute/receipt/UI binding is not established by those constants.
- student.search.v1, batch.search.v1 and academy.context.v1 are fixed limited pilots; the policy is Owner/AcademyAdmin in Development/Testing with flags. The two older fee-read tools SearchLearners/GetLearner are not financial mutations or full Manual collections management.
- Synthetic diagnostic and draft-preview endpoints explicitly perform no business effect. Pulse/Executor/Navigator/Twin/Autopilot names and 58 protocol actions do not establish operational coverage.
- No case-insensitive PENTA reference was found in tracked `apps/web/src` at the pinned Academy source. Manual pages exist; preserved PENTA/shared UI candidates exist in their original source packets, but integrated PENTA entry/mode/context/draft/approval/status rendering is not established here. This is a source-search result, not browser execution or proof against every possible indirect runtime path.
- The older catalog treats Portal.ChangePassword as eligible, despite its CurrentPassword/NewPassword host flow. The overlay flags ADDITIONAL_CREDENTIAL_FLOW_HOLD and keeps the original eligibility visible. It must remain secure Manual UI, never a conversational credential tool. C2 has not edited catalog policy or any dispatch behavior.

## First-academy priority map

All rows below are source findings and test locators, not passes in C2. The listed existing regression may cover only part of the journey; preserve full SQL/HTTP/browser/device requirements. Do not infer scheduling acceptance from an enrollment test or all-role acceptance from a foundation test.

| Order / journey | Current Manual/domain source and UI | PENTA gap / boundary | Existing regression or review locator |
| --- | --- | --- | --- |
| 1. Login, refresh, roles and mode entry | `apps/api/Controllers/AuthSessionController.cs`; `apps/web/src/app/login/page.tsx` | PENTA pilot remains Owner/AcademyAdmin in Development/Testing; no current PENTA web entry found. Password/role/account operations stay secure Manual flows. | `tests/AcademyDesk.Api.Tests/PentaFoundationTests.cs` |
| 2. Find students, batches and academy context | `apps/api/Controllers/PentaController.cs`; `apps/web/src/app/students/page.tsx` | student.search.v1, batch.search.v1 and academy.context.v1 are fixed limited read pilots, not every Manual field or a qualified language workflow. | `tests/AcademyDesk.Api.Tests/PentaFoundationTests.cs` |
| 3. Student intake with optional guardian | `apps/api/Controllers/StudentOnboardingController.cs`; `apps/web/src/app/student-onboarding/page.tsx` | StudentOnboardingService.CreateAsync is shared. AcademyDesk.StudentOnboarding.Create is a credential-free Testing-only subset with protected approval/receipt code; L1 and real interpretation remain pending. | `tests/AcademyDesk.Api.Tests/StudentOnboardingServiceTests.cs` |
| 4. Teacher-record intake | `apps/api/Controllers/TeachersController.cs`; `apps/web/src/app/teacher-onboarding/page.tsx` | TeacherCreationService.CreateAsync is shared. AcademyDesk.Teacher.Create has Testing-only review/approval/execute/receipt code; no account/role grant or complete Manual-equivalence follows. | `tests/AcademyDesk.Api.Tests/TeacherCreationServiceTests.cs` |
| 5. Enroll, change status and transfer | `apps/api/Controllers/EnrollmentsController.cs`; `apps/web/src/app/enrollments/page.tsx` | Manual uses shared EnrollmentMutationOperations. Three typed tool keys/field decoders exist; protected conversational prepare/approval/execute/receipt/UI binding is not established. | `tests/AcademyDesk.Api.Tests/EnrollmentMutationServiceTests.cs` |
| 6. Batches and scheduling | `apps/api/Controllers/ClassSessionsController.cs`; `apps/web/src/app/schedule/page.tsx` | Manual endpoint/domain paths exist; limited batch search does not schedule classes. Per-action conversational bindings and approval/recovery evidence are not established. | `tests/AcademyDesk.Api.Tests/EnrollmentAdmissionTests.cs` |
| 7. Attendance and teacher daily work | `apps/api/Controllers/TeacherPortalController.cs`; `apps/web/src/app/attendance/page.tsx` | Teacher portal/calendar/attendance APIs are not covered by the Owner/Admin-only AI pilot. Fresh portal authority and domain-specific execution/dialogue/receipt/UI acceptance remain required. | `QA/COVERAGE/SEMANTIC_CHECKLIST.md` |
| 8. Invoices, collections and reconciliation | `apps/api/Controllers/PaymentsController.cs`; `apps/web/src/app/payments/page.tsx` | SearchLearners/GetLearner provide bounded fee reads via OutstandingFeesService, not payment execution. Invoice/payment/adjustment approval, race, audit and financial P0 closure remain independent. | `tests/AcademyDesk.Api.Tests/PaymentRestorationTests.cs` |
| 9. Payroll profiles and payouts | `apps/api/Controllers/PayrollController.cs`; `apps/web/src/app/payroll/page.tsx` | Manual payroll domain/tests exist; no qualified conversational payout binding is established. Preserve reject-new-zero-net policy and required financial approval. | `tests/AcademyDesk.Api.Tests/PayrollNetTests.cs` |
| 10. Private learning material and uploads | `apps/api/Controllers/ClassMediaUploadsController.cs`; `apps/web/src/app/resources/page.tsx` | Storage/authorization transport endpoints include multiple verbs; they are not separate user outcomes. Protected sharing, revocation, approval and real-device recovery remain mandatory. | `tests/AcademyDesk.Api.Tests/ClassMaterialAccessTests.cs` |
| 11. Communications and reminders | `apps/api/Controllers/NotificationsController.cs`; `apps/web/src/app/communications/page.tsx` | Queued notification rows, consent and channel flags are not observed delivery. No operational AI send/receipt/provider callback workflow is established by generic SDK declarations. | `tests/AcademyDesk.Api.Tests/NotificationChannelTests.cs` |
| 12. Meeting links and provider meetings | `apps/api/Controllers/CommunicationSettingsController.cs`; `apps/web/src/app/meeting-links/page.tsx` | Manual meeting UI exists, but the seven preserved Meetings controller actions are absent here. Reconcile original source/contracts and actual provider support; never infer creation from a connection flag. | `QA/COVERAGE/SEMANTIC_CHECKLIST.md` |
| 13. Student/guardian portal and documents | `apps/api/Controllers/PortalController.cs`; `apps/web/src/app/portal/page.tsx` | Current row/guardian/document permissions remain authoritative; conversational portal coverage is not established. Portal.ChangePassword needs an additional secure-UI-only hold in the future catalog policy review. | `tests/AcademyDesk.Api.Tests/RecipientNotificationVisibilityTests.cs` |
| 14. Pulse, work queue, Navigator and Twin | `apps/api/Controllers/AdminWorkItemsController.cs`; `apps/web/src/app/work-queue/page.tsx` | Five capability names and synthetic draft previews are infrastructure, not real work-item/alert/insight acceptance. academy.context.v1 is a bounded context read only. | `tests/AcademyDesk.Api.Tests/PentaFoundationTests.cs` |
| 15. Autopilot, memory and release | `apps/api/Intelligence/Penta/PentaFoundation.cs`; `apps/web/src/app/dashboard/page.tsx` | A synthetic capability is not a reviewed automation mandate, persistent memory policy, schedule or production execution. Keep approvals, audit, outage recovery and all release gates. | `QA/09_RELEASE_GATES.md` |

## Validation and limits

Structural checks passed: 359 unique current keys; every legacy ID/exclusion retained; every resolved join targets a current row; all 81 controller source hashes match; cited current source, binding, UI and regression paths/lines exist; family assignments complete; CSV acceptance fields are uniformly static/unaccepted. The holiday alias collision and the ambiguous grading declaration are reported rather than deduplicated away. Controller syntax parsed with zero diagnostics. No business tests, SQL/HTTP services, browser, model or deployed runtime were executed by C2.

Framework Identity routes (ten declarations in existing `QA/COVERAGE/identity-endpoints.json`) and the `/health` minimal API in Program.cs are outside this attributed-controller census, not silently eligible chat actions. Other runtime-discovered/framework routes, conditional branches, effective authorization/roles, all form fields/controls and complete UI event semantics need their existing runtime/semantic acceptance. The older 82-form/649-control review and its limitations remain history, not re-proven by this map. Five P0 and critical P1/release gates remain unchanged.

CSV hashes:

- Current map SHA256: `f174df7160ec9448298539e8a08c08b1a1bc60b0eedfb81e80bbc65e50a8290c`.
- Private catalog reconciliation SHA256: `eb956645e06c96ea43c61eb1856e6e5af90ae3c2e2d6ba498145c5678804eacc`.

Private report `operations/CLOUD_C2_OPERATION_MAP_2026-11-05.md` and its CSV hold the full 317-ID reconciliation and per-family counts. Raw syntax bodies/working scripts remain outside Git; published CSVs contain source locators, not customer data or credentials. The enclosing report commit is the publication SHA; actual remote tips are independently verified in the final reply. Mapping validation is not a current GitHub CI conclusion.

## Exact next work and local return

1. Local L1: retain assigned host source and compile current bridge; finish PentaHostMigration, PentaFoundation, PentaOnboardingProtected and PentaTeacherProtected on fresh owned SQL with unchanged resource/authority/approval/audit/race guards, cleanup and exact-service restoration. Return exact source/run IDs, outcomes and artifact hashes. C2 did not change or accept any of these modules.
2. Designated integrator: review assessment/holiday identity reconciliation, resolve the ambiguous grading GET/PATCH row, reconcile missing Meetings source and keep Portal.ChangePassword securely outside conversational dispatch. Do not edit original IDs or promote private SDK status to host/model acceptance merely to improve a metric.
3. Next cloud C3: read the preserved PENTA/shared UI and current client/session sources; claim a bounded file list before entry/mode/context/approval/status integration. Reuse sources without importing older finance/migration policy wholesale. Do not concurrently edit L1 backend files. First connected UI acceptance should use clearly labelled model-free QA contracts, not an activated or falsely qualified provider.
4. Following bounded packets: protected enrollment workflow; then scheduling/attendance, finance/payroll, learning/media, communication/provider and remaining-role families. Each needs explicit typed scope, independent host authority, domain preview/approval/effect/audit/receipt/recovery and UI tests. These are prioritized gaps, not automatic ownership claims or a new architecture.
5. Local L2 remains blocked on explicit model experiment authorization and independent qualification. All Mini/Plus/Pro, language/device, provider delivery, resource, security, restore/staging and release gates stand; Ultra remains deferred. Manual must remain usable throughout AI downtime.

Next route: Codex / Sol High for consequence/authority decisions and L1; settled UI follows the existing routing. No main merge, force push, deployment, customer database, model acquisition/load/inference/training/activation, new packages, paid compute or environment publication.
