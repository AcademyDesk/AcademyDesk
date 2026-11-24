# D66 — PENTA conversation, backend and feature-mapping diagnosis

Date: 24 November 2026. Read-only application/model investigation requested by
the owner after the 4B comparison. Documentation only; no application, model,
configuration, permission or database changes. No additional inference initiated.

Academy checkout: `D:\AcademyDesk-penta-host`, branch
`codex/penta-host-integration-20261030`, source
`3540c978cca3bfe7331c2b3446d7421831e1f7e0`.
Mini checkout: `D:\PENTA-Models\operations\mini-academy-conversation-20261123`,
branch `codex/academy-conversation-core-20261123`, source
`d9c3d7550194685d4e3572befbc092d25541ff04`.
Source line references below refer to those commits unless marked untracked.

## Conclusion

**No: all Manual features are not connected and mapped into one working PENTA
conversation.** Existing domain services and protected guided workflows are real
work worth preserving. However, the active free-text chat is a learner/fees read
pilot, not the all-portal operational assistant described in the product contract.
Several newer components exist only as inactive drafts or separate test paths.

This is a proven integration/completeness problem before it is a model-sizing
decision. A larger model cannot call a teacher read that the host never exposes,
return a teacher through a learner-only receipt, or execute a draft with no
execution binding. The observed 4B misselection is also a semantic failure; this
report does not claim that every model is adequate once wiring is repaired.

Earlier component, SQL, security and recovery passes do not demonstrate the
combined all-domain conversation. Treating them as that proof would be wrong.
No reliable 90% outcome percentage can be calculated from declaration/test counts.

## 1. Which model/path actually serves the frontend?

The last verified demo used cached Qwen3.5-2B through llama.cpp and Mini's legacy
`penta_mini.api:app`, with Mini serving source
`4732116bc9a8c4eb9904ee7c4fc09100866816f6`. The 4B comparison was a separate
private probe, not a frontend replacement. Its container was removed.

Source comparison of `api.py`, `protocol.py`, `orchestrator.py`, `inference.py`
between that serving commit and current Mini HEAD produced no differences.
Additive task/native modules at current HEAD do not automatically change imports.
Runtime response headers attest selected service-file identities, not independently
the loaded weight file or every tool's implementation.

Current live verification was attempted: demo fetch failed; Docker's Linux engine
pipe was unavailable. Consequently **current live serving identity is unverified**.
Do not report a fresh successful login/model call or infer today's loaded model
from a selector label. No restart or loading was performed during this diagnosis.

## 2. Actual active free-text request pipeline

```text
/penta
  -> RolePentaEntry, verified Owner/Admin + evaluation flag
  -> MiniConversation + createMiniConversation
  -> /api/academies/{academy}/penta/chat/conversations/.../turns
  -> PentaChatController -> PentaMiniOrchestrator
  -> current actor/tenant/pilot/Finance authority + protected request claim
  -> PentaMiniProvider POST /v1/chat
       user text + learner state + [SearchLearners, GetLearner]
  -> Mini Orchestrator -> constrained JSON generation via llama.cpp
  -> one validated tool request (not a multi-step operational plan)
  -> current host reauthorization -> AcademyDeskConnector
  -> OutstandingFeesService -> scoped SQL data, deterministic balances
  -> encrypted/audited learner receipt + exact-money projection
  -> learner-only frontend receipt validation and Student 360 links
```

Primary evidence:

- `apps/web/src/components/penta/role-penta-entry.tsx:57–68`: actual component gates.
- `apps/web/src/components/penta/mini-conversation.tsx:21`: chat endpoint binding;
  result heading is explicitly "Verified learner read".
- `apps/web/src/lib/penta-mini-conversation.ts:157`: fixed executor capability;
  request/recovery state machine, not a universal workflow dispatcher.
- `apps/api/Controllers/PentaChatController.cs:10–25`: protected chat routes.
- `apps/api/Intelligence/Penta/PentaMiniOrchestrator.cs:180–200`: one provider
  plan and one connector execution; `:221` fixed safe messages.
- `apps/api/Intelligence/Penta/PentaMiniProtocol.cs:8–32,61`: learner-only
  state/one tool call and exact two-tool allowlist.
- `apps/api/Intelligence/Penta/PentaMiniProvider.cs:26`: `/v1/chat` request.
- `apps/api/Intelligence/Penta/AcademyDeskConnector.cs:18–58`: only learner
  search/open; injected service is OutstandingFeesService, not teacher directory.
- Mini `src/penta_mini/api.py:19–21,66`: legacy provider/orchestrator and chat.
- Mini `src/penta_mini/orchestrator.py:333–341`: system + current user + selected
  learner state, not the complete conversation transcript.

The host's academy path is checked against current stored identity; it must not
be replaced by model-supplied tenant/role authority. `Program.cs:70` registers the
global AcademyAccessFilter. Absence of a local Authorize attribute on an individual
Manual controller alone is not proof of an unauthenticated bypass.

## 3. Proven causes and missing checks

| Finding | Evidence and consequence | Required correction |
| --- | --- | --- |
| D66-01: active catalog has no teacher, calendar, attendance or general finance tools | PentaMiniTools.Available is exactly two learner reads. TeachersController.List already reads real academy-scoped Teachers. No live teacher tool binding calls it. | Expose reviewed typed domain reads through host services and a current permission-filtered catalog; do not merely add names to a prompt. |
| D66-02: the result/context contract is entity-specific | MiniState stores learner IDs/filters. PentaMiniRecovery requires `/student-management?studentId=...`; web penta-chat-contract.ts:41–55 enforces the same. | Version typed entity/result/reference contracts and matching frontend renderers/recovery. Teacher specialties, identities and profile routes cannot be interpreted as learner course/fee fields. |
| D66-03: guided workspace silently falls back to finance | penta-workspace-prompt.ts recognizes student/batch lookup and schedule/reschedule; final return is finance. finance-chat.tsx:185–228 follows that branch. | Explicit supported/unsupported/ambiguous domain routing. Never classify an unknown or teacher request as finance by default. |
| D66-04: newer multi-step system is not on the frontend path | No web references to tasks/conversations or academy-tasks/plan. PentaTaskRuntimeAccess gates Dev/Testing Owner/Admin, nine draft write tools, execution_enabled=false. | Connect one versioned conversation driver after read and task contracts are complete; add reviewed approval/execution adapters using existing services. |
| D66-05: draft dependencies are not completed operations | PentaTaskConversations calls PlanAsync then PentaTaskDialogue.Apply. Batch.Create binding is draft-only. NativeToolRouter is not imported by either serving entrypoint. | Resolve real references, collect fields, prepare/approve/execute, bind created IDs, revalidate dependents, and recover each result without duplicate dispatch. |
| D66-06: conversational history and correction scope are narrow | Model sees learner state + current message; frontend retains last prompt/receipt. New encrypted task history is separate. Shell explicitly says previous history is not connected. | Persist typed turn/task/entity context, distinguish corrections and topic changes, reauthorize retrieved context, and supply a bounded relevant history to the model. |
| D66-07: natural answer generation is absent in the live pilot | Mini discards ungrounded model prose; host SafeMessage/connector return templates. No tool-result-to-grounded-answer model pass exists here. | Compose understandable grounded answers from authorized results, with exact values/sources and explicit unavailable/partial status. Do not pass through invented model facts. |
| D66-08: role-specific composers are different bounded applications | Platform uses three phrase families; Portal uses three read families; Teacher uses assigned attendance workflow. They are not all using the live Mini planner. | Per-role allowed tools in a common conversation architecture; preserve scoped domain enforcement and existing guided fallback. |
| D66-09: five capability labels are ahead of implementation | penta-workspace-shell.tsx disables all except Executor and describes others as unconnected/automation disabled. | Implement and qualify Pulse/Navigator/Twin/Autopilot separately; labels are not engines or accepted workflows. |
| D66-10: qualification measures do not establish the advertised outcome | Source inventories, synthetic dispatcher, model-free contracts, service SQL, draft-only planning and selector probes test different boundaries. | Measure eligible end-to-end outcomes by role/domain/action, including real model, authoritative result, correction, approval, persistence and recovery. |

Important distinction: incorrect teacher-to-learner routing is proven possible
by the architecture and the owner's screenshot. This read-only investigation
does not reconstruct the exact original model logits or prove every teacher
phrase produced the same live response. Cross-user/tenant history leakage was
**not** established; entity/topic contamination and narrow memory are the
supported diagnosis. Existing revocation and tenant tests must remain intact.

## 4. Model-free reproduction and actual checks

Executed actual TypeScript `workspacePrompt` through the existing cached compiler,
without changing source, downloading packages or making a model request:

| Prompt | Actual guided-workspace route |
| --- | --- |
| Show the teachers in my academy. | finance |
| Find teacher Nisha | finance |
| I said teachers | finance |
| Show teacher attendance today | finance |
| Find student Mira | student lookup, query Mira |
| Find batch Piano | batch lookup, query Piano |
| Create a student and a batch and enroll the student | finance |

This is the **guided FinanceChat parser**, not the separate Mini model classifier.
The distinction matters: there are multiple disconnected routes to correct.

- Existing workspace/platform/portal tests: **15 passed** using their supported
  QA_WEB_TYPESCRIPT override pointing to an existing compiler in D:\AcademyDesk.
- Existing Mini client tests: **74 passed** using a process-local require-path
  redirect for that same compiler; source/assertions unchanged. No main files
  edited. These test synthetic responses/storage, not model semantics or SQL.
- Initial direct attempts could not load TypeScript because this worktree has no
  web node_modules. That is an environment/runner failure, not a semantic failure;
  retained in the task transcript. Successful executions above used the same
  current client sources and existing assertions. No test was weakened.
- D65 real 4B selection: **1/2 passed, seven unrun**, including teacher read wrongly
  selecting teacher creation. No domain operation executed. See D65 report.
- Fresh live API/SQL/browser/model checks: **blocked/unrun** because demo/Docker
  were unavailable. Earlier restored-demo counts 13/4/4 are historical evidence.
- Full enterprise suite, physical devices, production/staging and other model
  tiers were not rerun as part of this diagnosis.

Passing these 89 narrow client/parser assertions alongside the reproduced fallback
shows the acceptance gap: the tests are useful, but not sufficient for the product
claim. Add missing cross-domain acceptance rather than discarding valid tests.

## 5. Portal and Manual-feature reachability map

This reconciles the existing 30 business families in
`docs/PENTA_CONVERSATIONAL_OPERATING_CONTRACT.md:51–80` with the actual entrypoints.
It is a feature-family source diagnosis, **not** a claim that every Manual field,
DTO, permission and database relationship has now passed an exhaustive runtime
audit. All admin families below are absent from legacy Mini beyond its stated
learner/fees read. Separate guided capabilities are called out, not conflated.

| Manual family / source | Existing PENTA connection | Gap to unified conversational parity |
| --- | --- | --- |
| Platform tenants; PlatformAcademies, Academies | Guided platform onboarding aggregate | Tenant onboarding/lifecycle/configuration operations not universal chat tools |
| Platform business; PlatformControl, AcademyPlatformServices | Guided health/onboarding/support summaries | Support/billing/announcement operations beyond summaries |
| Academy operations; Dashboard, Branches, AdminIntelligence | Academy-context pilot | Branch/profile/capacity operations and proactive summaries |
| Students/intake; Students, StudentOnboarding, Profiles | Live learner fee read; separate protected intake | General 360/profile/status/update and intake joined to common conversation |
| Families; Guardians, StudentGuardians | Guardian-linked reads in Portal | Guardian creation/update/link operations and scoped references |
| Teachers/workforce; Teachers, Staff, Profiles | Separate protected teacher creation | No teacher directory/get tool in live Mini; updates/assignments/workforce parity |
| Courses/curriculum; Courses, CourseModules, CourseModuleStatus | References in guided academic flows | Typed course/module reads and permitted mutations |
| Academic governance; AcademicPeriods, AcademicGovernance, GradingSchemeLifecycle | No binding in live Mini | Period/prerequisite/grading operations |
| Batches; Batches | Guided search; new Batch.Create draft descriptor | Creation execution and broader batch reads/updates |
| Enrollment/progression; Enrollments, BatchPromotions | Protected guided create/status/transfer | Common planner bindings; progression/promotions and all permitted outcomes |
| Scheduling; ClassSessions | Protected guided class creation/reschedule | Calendar/availability/cancel and general multi-step scheduling conversation |
| Attendance; Attendance, TeacherPortal | Assigned teacher guided attendance | Broader authorized reads/bulk/admin outcomes; natural routing |
| Leave/make-up; LeaveRequests, MakeupClasses | Backend protected make-up workflow | Connected make-up UI and leave workflows; Offline/Online/Hybrid field logic |
| Holidays/events; Holidays, Events | No binding in live Mini | Scoped list/lifecycle/impact and permitted writes |
| Teaching work; Assignments, AssignmentSubmissions, LessonPlans | Portal due-work summary | Authoring/publication/submission/review/lesson operations |
| Assessment/results; Assessments, AssessmentResults | No binding in live Mini | Assessment lifecycle, rosters/grading/results operations |
| Practice/progress; PracticeLogs, MusicPieces, MusicProgress | No binding in live Mini | Scoped log/progress/review and non-music school coverage |
| Resources/private media; LearningResources, ClassMedia* | Existing Manual storage/access infrastructure | Chat tool binding, user file/record interaction, permission-safe retrieval |
| Fees/arrangements; FeePlans, StudentFeeArrangements | Outstanding-fee read is only a subset | Plan/arrangement reads/mutations using actual domain rules |
| Invoices/collections; Invoices, Payments | Guided reads/issuance/collection/recovery | General Mini tool bindings; resolve entities and exact amounts across dialogue |
| Finance controls; FinanceAdjustments, FinanceGovernance, FeeReminders | Guided adjustment submission; protected finance reads | Separate decisions and follow-ups/reminders under required approvals |
| Expenses/payroll; Expenses, Payroll, TeacherCompensation | Guided finance summaries, existing Manual rules | Permitted operational mutations and worker/self scope in common conversation |
| Admissions/growth; Leads, SalesMarketing | No binding in live Mini | Leads/trials/campaigns/conversion; Navigator not implemented by its tab |
| Communications; Notifications, Communication* | Mini intent vocabulary has draft/send names, but host allows neither | Actual provider/consent/audience/approval/queued-delivered distinctions |
| Documents/compliance; Compliance, Certificates | Existing Manual/domain capabilities | Scoped document/consent/certificate conversation and eligible issue workflow |
| Operations/reporting; AcademyExports, AuditLogs, AdminWorkItems, StudentImports | No binding in live Mini | Safe report/export/import/work-item actions, partial outcomes |
| Teacher self-service; TeacherPortal | Separate assigned attendance composer | Own calendar/work/resources/review/payments etc.; no general model connection |
| Student self-service; Portal | Guided next class, due work, outstanding fees | Own assignments/submissions/progress/resources/profile/leave/documents etc. |
| Guardian self-service; Portal, StudentGuardians | Same three guided reads with linked-child checks | Remaining currently granted child operations; multi-child/context/revocation |
| Identity/access; AuthSession, PortalAccounts, Roles, Access* | Secure Manual boundaries retained | Explain/direct secure actions; no model role/security changes or credential tools |

Sources for guided scope: `PENTA_HOST_IMPLEMENTATION_STATUS.md:13–55`,
`apps/web/src/lib/penta-platform-reads.ts:3–14`,
`penta-portal-reads.ts:3–30`, `penta-teacher-attendance.ts:24–27`, and corresponding
PlatformControlController.Penta, PortalController.Penta and
PentaOnboardingController.TeacherAttendance server partials.
Platform `/platform/ai` uses PlatformConversation directly; it must not be
mistakenly diagnosed using RolePentaEntry's otherwise-unused platform fallback.

`PentaManualCapabilityIndex.cs:12–17` explicitly calls its reflection output an
internal source inventory, NOT a permission decision or automatic registration.
Its narrow Binding switch is not a complete inventory of every separately guided
adapter. The two untracked teacher/learner contract/interpreter drafts remain
preserved, unserved and unqualified; their tool names are not connected features.

## 6. Prompts, tiers, parameters, fallback and data integrity

- Legacy Mini has four intent specifications in connector.py (learner search/get,
  draft/send), while Academy supplies only the two allowed read names. `/v1/capabilities`
  uses MockConnector metadata; this is NOT the authoritative host capability list.
  The observed Academy read is executed by the real C# connector, not by the
  Python synthetic learner fixture. Never call that fixture real academy retrieval.
- Legacy inference.py:35–39 uses temperature 0, max_tokens 256, non-streaming,
  strict JSON schema, with an 80-second provider default. This is a bounded planner,
  not a ChatGPT-style multi-turn grounded answer pipeline. More tokens alone do
  not supply absent tools, schema or domain results.
- Safety validation enforces tool name/argument/state shape and access, but does
  not establish that a valid learner query semantically answers a teacher request.
  Add wrong-domain and read-versus-write intent validation; retain strict safety.
- Finance-module policy currently gates the generic-looking live connector.
  General teacher/calendar access needs its own domain policy; do not remove
  Finance authorization globally to make unrelated reads work.
- Legacy non-tool replies use fixed host explanations. Guided unknown requests
  enter finance. Neither should be advertised as universal conversational support.
  Manual fallback exists and should remain available during AI downtime.
- Mini task endpoint requires the separate academy_tasks_api entrypoint plus an
  explicit enabled flag. Native router has no serving imports. Nine task drafts
  and protected DAG/history machinery are reusable but not domain completion.
- `model-library/tier-registry.json`: Mini 2B, Plus 3B candidates, Pro 4B, Ultra
  deferred 9B plan; all four have approved_default=null, selectable=false and
  production_available=false. None is a qualified production tier. No automatic
  model-upgrade permission increase is allowed.

## 7. What was missed in verification

1. A test of the exact owner prompt through the exact frontend component, serving
   model, current host catalog, real data and final rendered response.
2. A single source-bound map joining Manual action -> current role/module policy ->
   executable AI descriptor -> schema -> shared service -> result -> recovery UI.
3. Negative semantic tests: teacher read must never become learner search or teacher
   creation; unsupported domains must not silently use finance.
4. Verifying that new planner/catalog/history modules were actually imported,
   enabled, served and called by the frontend before describing integration progress.
5. Dependent outcome tests with real created IDs: student + batch + enrollment,
   corrections after clarification, duplicate/capacity rules and partial failure.
6. A definition of production qualification covering every permitted role and
   eligible outcome, separate from API counts and model-free protocol coverage.
7. Fresh runtime/build identity and capability health distinct from simple model
   process readiness. Historical passes remain useful, but are not today's live pass.

## 8. Corrective execution order — recommendation, not implementation

| Order | Work using existing foundation | Acceptance before moving on |
| --- | --- | --- |
| 1 | Make source/runtime/catalog/feature status observable; explicit unsupported/ambiguous routing; unify reviewed domain read descriptors and typed results | Admin teacher/student/batch/attendance/schedule/finance queries reach correct authorized sources; missing tool never substitutes a different entity/action; current identity/module denial remains enforced |
| 2 | Connect one conversation with entity-aware history/corrections and grounded answer composition; retain GET-only receipt recovery | Same-name teacher/student, "I said teachers", topic switches, pronouns, exact balances, unavailable provider and reload all return truthful scoped results |
| 3 | Bind current task drafts to existing protected approval/domain workflows; complete missing Batch mutation adapter | Student + batch + enrollment from one prompt; ask only missing required fields; explicit review; true saved IDs, duplicate/capacity/transaction checks, revocation/audit rollback and no redispatch |
| 4 | Expand according to the 30-family parity map with per-role outcomes and reviewed read/write/delete/tool policies | Each eligible Manual outcome has an executable binding and real role/tenant/domain/recovery tests; blocked security operations remain secure/manual |
| 5 | Qualify models against that same complete host contract; then qualify P/E/N/T/A and premium responsive UI | Real multi-turn outcome score and latency by role/domain, unsupported/refusal correctness, adversarial tests; tier names/visual tabs never stand in for qualification |
| 6 | Finish existing release gates, CI/private archive recovery, devices, staging/rollback and Azure preparation | Current combined-source evidence and explicit release approval; no production claim based on a narrow local demo |

Do not rebuild the ERP or restart the audit. Reuse accepted shared services,
Identity/tenant policy, financial transaction locks, draft/approval/receipt and
recovery work. Do not add a teacher-only patch and call it whole-product parity.
No further model swaps/training should precede these wiring corrections.

Recommended implementation model: **Sol High** for integration/contracts/domain
security; smaller routine documentation/style tasks can be delegated only under
the established routing policy. No new external project handoff is required.

**Status: diagnosis complete at source/targeted-client level; full AI integration
and production acceptance NOT COMPLETE. Application implementation intentionally
not started under this read-only request.**
