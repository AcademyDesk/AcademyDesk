# SWITCH PROJECT CHECKPOINT: ACADEMY DESK → PENTA MINI

## Enrollment domain cohort foundation returned — 28 October 2026

Academy now has one shared implementation for enrollment create, lifecycle update
and transfer. Native Manual uses it under its existing granular access/audit
filter. A new Owner/Admin PENTA facade independently resolves current database
authority and requires a shared SQL/Identity transaction before relational access.
Same admission lock, capacity/waitlist/prerequisite/duplicate and transfer-history
rules are retained. Current actual platform owners do not act through this academy
PENTA facade; Manual access policy is unchanged. Enrollment is an existing Core
capability, not a newly restricted paid module.

Inactive typed field keys:
- AcademyDesk.Enrollment.Create:StudentId,BatchId,optionalStartDate/Status.
- AcademyDesk.Enrollment.Update:EnrollmentId,explicitStatus,optionalEndDate,
  LifecycleReason required for non-Active changes.
- AcademyDesk.Enrollment.Transfer:EnrollmentId,TargetBatchId,optionalTransferDate.
Use operational-draft-0.3.0 typed canonical payloads; unknown keys/authority,
malformed IDs/dates and ambiguous create statuses refuse. These adapters are NOT
registered tools or approved operations. No model dispatch or Mini source changed.

3,183API/zero skips,57new shared SQL/HTTP/service +28 retained lifecycle cases,
15mixed Manual/service admission races and36CIguards passed. Six actual required-
audit faults roll back service/native create/update/both transfer halves. Linked
current-source/receipt/restoration/cleanup qualifier:
QA/EVIDENCE/enrollment-shared-qualified-973096375be64b9aaf67878869808f3a/.
No new frontend or qualified conversation claimed. Service success is not a
committed chat receipt: caller owns scoped approval, replay, required audit/commit.

Next host work is protected draft/preparation/approval/receipt/recovery for these
operations, with current source/target/capacity/prerequisite/authority checks at
execution, then existing Core conversation and UI. Preserve teacher/student flows;
do not connect inactive parsers to an unqualified interpreter. Actual shared-engine
gaps return here. Owner stop before candidate loading stands; fresh6/12/exposed7/12
NOT ACCEPTED. No load/train/inference/activation/download/paid compute/Azure or
automatic project switch. Sol High for the consequential host integration.

## Teacher conversational host UI returned — 28 October 2026

Academy now supplies a visible protected teacher chat at /penta/teachers/chat,
linked from the global PENTA screen. It uses existing startup/gateway/native Core
and review/approval/receipt boundaries, shared Prism/Orbit controls, a prompt
composer and verified canonical draft rail. Sending cannot approve or execute.
Lost responses/reload use the exact opaque request GET, never another prompt.
Current cancelled/approved/succeeded status overrides historical planning text.

Current proof: accepted unchanged3,106API/zero skips,167client/types/lint/fresh
89page export,200realSQL cases, five new actual chat/six retained review browser
journeys,11actual shared Core schema/SDK and35CI guards pass. Nine PNGs inspected.
Qualifier QA/EVIDENCE/penta-teacher-chat-qualified-9921fe972af04c819c9dafe4d14729c3/.
Exact source/restoration/cleanup/lease/main preserved. No Mini source changed.

The genuine remaining engine gap is explicit:
1. A qualified private interpretation adapter must consume the host's existing
   current-authority operational-draft-0.3.0 snapshot and untrusted text, returning
   a strict teacher clarify/host_prepare WorkflowDecision. Scope/request/revision/
   catalog/fields remain host validated. No role, approval, execution, SQL/code,
   invented result or model memory may authorize a real operation.
2. Current IPentaTeacherInterpreter accepts only the explicitly injected QA
   synthetic marker. Do not implement that marker on a real runtime to bypass
   qualification. Real registration/availability requires the established paired
   semantic/privacy/security/host acceptance, not just a flag.
3. The browser fixture accepts invented QA phrases. It is not ordinary teacher
   language evaluation, model intelligence or90% AI coverage. Host messages are
   code-owned; raw model prose/plan details are not rendered as authoritative facts.
4. The main PENTA read chat now has a teacher workflow navigation link. Automatic
   natural-language routing of arbitrary main-chat operations is still unqualified.
   A genuine shared contract/routing incompatibility must return through this file;
   no parallel domain engine or implicit protocol/permission expansion.

Owner's stop before candidate weights loading remains binding. Fresh6/12/exposed
7/12 NOT ACCEPTED; no load/train/inference/activation/download/paid compute/Azure.
Continue other host workflow cohorts on Sol High. A project switch is only needed
for this real shared engine work; the file handoff is ready, not a request to
override the stop. Manual remains independent, and enterprise/release gates OPEN.

## Teacher conversation startup returned — 28 October 2026

Academy now has teacher readiness, explicit startup and same-request startup GET
recovery. The existing SQL/Identity/session/turn/draft/protection/audit foundation
atomically creates one owned empty draft/session and protected startup receipt.
Startup never invokes interpretation or creates a teacher/account. Concurrent
same-key requests return 201/200, same IDs; recovery works with a disconnected
interpreter, during a pending prompt and after explicit creation. Current
authority/expiry/purpose/context/audit and quotas remain independently enforced.

Teacher-specific startup transport follows the retained student recovery pattern,
with a separate teacherStartRequestId URL lookup key and strict teacher-start/1
response handling. The startup summary means conversation_started, never domain
success. No automatic POST/retry/approval; lost/reloaded starts use GET recovery.
No visible teacher composer/global Executor routing is claimed by this library.

3,106 API/zero skips, 46 startup +154 retained teacher SQL, 61 startup +52 retained
operation client checks, TypeScript/lint, 11 retained actual shared Core schema/SDK
checks and 35 CI guards passed. Linked qualifier:
QA/EVIDENCE/penta-teacher-start-qualified-68c078d345284c3a9ecc992e273f7f61/.
Current source, exact restoration/health/inventory, owned cleanup, lease and main
preservation verified. Mini source was read only for schema/SDK compatibility.

No shared protocol revision, actual provider registration/dispatch or permission
expansion. Only explicit synthetic QA interpretation remains accepted; never add
that marker to a real runtime to evade qualification. Next host work is visible
composer/global routing. A real paired private interpretation dependency must
return through this file, retaining teacher and student security behavior.
Owner's stop before weights loading remains binding; fresh6/12/exposed7/12 NOT
ACCEPTED. No candidate load/train/inference/activation/download, commit/push/Azure
or enterprise issue closure. Stay Sol High for host integration; no project switch
is necessary for host wiring and no model is automatically changed.

## Teacher host conversation gateway returned — 27 October 2026 batch

Academy adds teacher-specific claim/interpret/complete and GET-result ports over
the existing protected sessions/turn ledger and native Core adapter. Independent
current SQL/Identity authorization, changed-context/catalog rejection, strict
partial/preparation decisions, mandatory audit rollback, replay/cancellation/
capacity controls passed43real SQL/HTTP cases.3,089API, retained111teacherSQL,
11shared Core schema/SDK checks and34CIguards PASS. Linked qualifier
QA/EVIDENCE/penta-teacher-conversation-qualified-f26d38e4a9c644d8b21c4269ae1b5dc3/.
No model inference, teacher/account creation from planning or actual tool activation.

The optional IPentaTeacherInterpreter accepts only request ID, untrusted text and
the host's current operational-draft-0.3.0 snapshot. It returns a strict shared
WorkflowDecision (teacher clarify or host_prepare), never approval, execution or
arbitrary prose. For now only an explicitly injected QA synthetic marker is
accepted; no real provider is registered. Do not implement the synthetic marker
on a real runtime to bypass qualification. Current shared schema/version remains
unchanged; this is a host adapter, not a new shared Core implementation.

New host summary teacher-conversation/1 contains request/conversation/draft IDs,
current draft revision/hash, kind (pending/partial/prepared/unavailable/
invalid_response/needs_manual_review), status, result (snapshot/preparation/null),
canExecute=false and domainEffect=false. HTTP200 is not domain creation success.
Known requests never redispatch; GET the same request to recover. Native approval
and actual creation/receipt remain separate current-authority operations.

Still open:owned teacher session/draft bootstrap, global Executor routing,
conversation composer/transport/native browser proof, and a qualified paired
private interpretation adapter. The11SDK checks use retained actual Core HTTP
outputs; they do not claim that Mini already implements the new host summary.
Coordinate any actual shared-engine incompatibility here, preserving accepted
student behavior and role/tenant/approval security. No Mini source changed.

Preserve owner's stop before weights loading;fresh6/12/exposed7/12 NOT ACCEPTED.
No training/load/inference/activation/download/paid compute/Azure/production action
or automatic project switch. Continue host wiring here on Sol High; use Sol Medium
for settled bounded UI. Existing enterprise testing and release gates remain OPEN.

## Teacher visible host review returned — 27 October 2026

The teacher native shared-Core adapter and protected visible review surface now
have linked local qualification.3,065API/21CoreSQL/30workflow/15service/
45availability,11actual shared schema/SDK checks,33CIguards; UI52client/types/
lint/fresh88page636file export; six real browser/SQL journeys/35HTTP/eight inspected
PNGs. Separate review→exact native approval→creation, cancellation, lost-response
GET-only recovery, denied/unavailable states work against actual disposable SQL.
Only3workflow teachers/3execution audits, no accounts; exact services restored.

Academy reports PENTA_TEACHER_CORE_2026-10-27.md and
PENTA_TEACHER_REVIEW_UI_2026-10-27.md; linked evidence
QA/EVIDENCE/penta-teacher-ui-qualified-098af81a2b4d488b88f8f7cd29ac9abf/.
No new shared version/model catalog or actual model dispatch. Still missing:
qualified teacher conversation interpretation/routing and full family coverage,
physical devices/all-role/release acceptance. Preserve stopped-before-model-load;
6/12fresh7/12exposed NOT ACCEPTED. No inference/activation/Azure or Mini source edit.
Use the existing shared Core, not a parallel business-rule/model implementation.

## Teacher Core wire adapter returned — 27 October 2026

Academy now implements current-authority teacher AI-05 ToolDescriptor and
HostSnapshot, partial/prepare WorkflowDecision acceptance with encrypted request
history/replay, and shared PreparedOperation/Receipt/execute_once/status. All
use existing operational-draft-0.3.0; no protocol revision or model registration.
The same native teacher approval/execution/domain/SQL/Identity/audit boundaries
remain authoritative. Preparation cannot approve; native-only approved drafts
cannot manufacture Core identities. No model-supplied scope grants authority.

Actual3,065API/21CoreSQL/30workflow/15service/45availability plus33CI guards
PASS. Saved real synthetic HTTP outputs passed11checks using PENTA's actual
schema classes/ConnectorSDK without inference or live connector mutation;
shared source hashes retained, no Mini source edits. Academy report
QA/REPORTS/PENTA_TEACHER_CORE_2026-10-27.md and qualifier
QA/EVIDENCE/penta-teacher-core-qualified-c8ed13746d9c4cfbbcdfa2b4632f9cc2/.

Remaining: qualified private interpretation/planning, user conversation routing
and visible integrated UI/native/device acceptance. Native host HTTP ports are
implemented; no Python HTTP connector automatically installed/activated. Shared
SDK replay is compatibility proof, not live engine quality or90%coverage.
Owner's stop-before-model-loading stands;6/12fresh7/12exposed NOT ACCEPTED.
Continue Academy UI/host here; return actual shared-engine gaps through this
file. No new model load/train/inference/activation/download, messages or Azure.

## Teacher native host workflow return — 27 October 2026

Academy implemented and locally qualified the Testing-only teacher host
save/read/review/approve/execute/cancel boundary and TypeScript transport. Tool
key remains AcademyDesk.Teacher.Create revision1 with14existing bounded fields;
no shared contract revision, public model catalog entry or engine registration.
Current actor/tenant/session/revision/authority, conservative duplicate holds,
scoped expiring approval, SQL/Identity domain+audit+receipt transaction and
same-draft GET recovery remain Academy-owned. Review/model yes cannot approve.

Final host3,049API +30workflowSQL +15serviceSQL +45availability; client52/types/
lint and32CIguards PASS. Same-draft race201/200 same result; distinct duplicate
approved drafts201/409. Exact services/images/inventory/cleanup restored.
See Academy QA/REPORTS/PENTA_TEACHER_WORKFLOW_2026-10-27.md and linked evidence
QA/EVIDENCE/penta-teacher-qualified-a686184fde8b440ba279a87eaa9fa258/result.json.

Still missing: current-authority ToolDescriptor/HostSnapshot and versioned
ConnectorSDK prepare/operation/execute_once/status adapter, teacher partial-field
interpretation and shared visible UI acceptance. Native teacher routes do NOT
claim to implement the shared SDK wire shapes. Reuse the existing operational
contracts/frontend; no parallel model/ERP or synthetic production success.
Academy can continue its bridge here. Return any genuine shared-schema/runtime
change through this file; no new project/chat messages or package/model changes
are authorized by this handoff. Owner's stop-before-model-loading stands;
fresh6/12/exposed7/12 remains NOT ACCEPTED. No inference, activation or Azure.

## Academy host teacher-service foundation — 26 October 2026

Academy now has a shared independently authorized TeacherCreationService behind
Manual POST, with SQL/Identity transaction/current actor/tenant/role checks and
mandatory audit rollback. An INACTIVE operational-0.3 field adapter recognizes
AcademyDesk.Teacher.Create revision1 and14bounded existing teacher fields. This
is a host foundation, not a new shared contract revision, enabled catalog entry,
approval or model execution. Unknown/credential/authority/complex metadata fail
closed; caller still needs reviewed duplicate/replay, draft/approval and receipt
recovery. Do not advertise teacher conversation support from the parser alone.

Actual host evidence:3,040API tests/15newSQLchecks/45retainedavailability cases
PASS, original services/inventory restored; see Academy report
QA/REPORTS/TEACHER_CREATION_SHARED_SERVICE_2026-10-26.md. No Mini change or new
request to train/load/activate is needed for this foundation. Owner explicitly
chose stop-before-model-loading; preserve that decision.6/12fresh7/12exposed
remains NOT ACCEPTED. Next Academy work stays here on the protected workflow;
any genuine future shared-contract change returns through this established file.

- [x] Final direct-service authentication/forged-role/removed-membership SQL
  checks PASS; CI floor now3,040 with31artifact/workflow guards PASS locally.

## Completed62record independent follow-up — 26 October 2026

Owner source/use declaration and independent reviewer actual62receipts/all7checks
now verified. New62development packet excludes14exposed validation; original
76findings/source labels untouched. Parent result REVIEWED_FOR_FOLLOWUP62/0;
123focused tests PASS, zero training eligible/may_train=false/model-qualified=false.
Do not repeat missing-author/reviewer requests for the unchanged packet. Next:
concrete checkpoint/trainer/resource experiment proposal from existing safe-runtime
baseline; separate download/install/training/cost approval required. Current
3Boriginal shards absent/no GPUtrainer/safe training fit unmeasured. Actual model
6/12fresh7/12exposed remains NOT ACCEPTED. See new independent follow-up report
and Mini `operations/MODEL_IMPROVEMENT_NEXT_2026-10-26.md`. No activation/Azure/Git.

## Paired work authorized; actual review completed — 26 October 2026

Owner explicitly authorized both projects and an independent review agent.
Actual76example content review completed; immutable findings and exact76record
binding pass,102focused tests,14known-exposed validation deny overlay. All76
needs_clarification for remaining source-owner/rights/lineage; no definite label
mismatch, one contextual-target clarification limit. Original sources/labels/base
exclusions preserved; zero eligible/training/model activation. See independent
Academy report and `D:/PENTA AI Models/operations/INDEPENDENT_REVIEW_2026-10-26.md`.
Do not repeat missing-reviewer preparation. Next:finite source declaration/new
exposure-aware binding, separately approved changed-candidate experiment.6/12
fresh7/12exposed remains NOT ACCEPTED; no downloads/paid compute/deployment.

## Launch critical-path return required — 26 October 2026

Owner retains full90%conversational/10%Manual launch target. Continue the existing
Mini qualification/improvement work; no unchanged-model trial or new audit.
Academy's backend/sign-off/native UI work does not qualify Mini reasoning.
Latest recorded fresh6/12/exposed7/12 NOT ACCEPTED;76pending review examples/
zero eligible remain. Resolve designated independent review, exact changed
candidate and separately authorized experiment/resources before fresh acceptance.
Return versioned implemented/qualified/blocked workflow coverage and the exact
paired host contract, retaining failed receipts and privacy/authority controls.
Academy will reuse operational/onboarding/approval foundations for cohort-based
host integration; inactive parsers are not all-family execution. No Mini edits,
training/download/activation/another-chat message or Azure action authorized by
this file update. See `QA/REPORTS/AI_FIRST_RELEASE_CRITICAL_PATH_2026-10-26.md`.

## E5-R63 delivered — 21 October 2026

- [x] New offline20-example pending proposal/tools/tests in existing Mini branch.
 58new/730distinct related checks PASS/24.95s. Frozen E61 receipt/files/exclusions
  verified; original56 unchanged. No serving contract/prompt/provider/reducer change.
- [x] Artifact `D:/PENTA-Models/operations/e5r63-development/pending-cd17b6109d42`,
  proposal SHA256 `34c5c5fab8ec59f796b32e5b71f90e47d0230b1320cdb269e2f6424b80cf7130`.
- [ ]76pending candidates across separate packets/zero eligible. Actual independent
  author/reviewer designation/review and later scoped experiment approval required.
  English/timezone/broader workflow gaps remain; no quality/weights improvement.
 14old validation templates exposed during source inspection, never unseen acceptance.

[Canonical report](QA/REPORTS/PENTA_PENDING_DEVELOPMENT_EXTENSION_E5R63.md).
Full-plan6/12fresh7/12exposed NOT ACCEPTED. Next High independent review/resource/
qualification gate; Medium for settled UI/docs. No acquisition/install/train/inference/
activation/restart/prune/Git/Azure/main/app source change;29containers/41volumes.
Existing host/domain/history/device/enterprise/release/BUG-DATA-0001 OPEN.

## Concrete model-data follow-up — 21 October 2026

[Use this bounded packet](QA/HANDOFFS/PENTA_MINI_CORPUS_GAPS_2026-10-21.md).
Developer pre-review inspected42 train examples only; six coverage gaps documented.
Frozen E61 receipt/three hashes match; no validation/evaluation answers used to tune.
No actual review attestations, approved labels, eligibility or weights changed.

The three rows lessons start with no rows; the three partial lessons omit no required
field; successful reference sets0; all42 English/Executor, with repeated lifecycle
phrasing. These are limited-coverage findings, not proof of incorrect literal labels
or failed host authorization. Retain old examples/receipts; propose new pending
development data separately, without reusing excluded families/evaluation answers.

56 actual reviews pending/zero eligible; full-plan fresh6/12/exposed7/12 NOT ACCEPTED.
Next High: versioned pending proposal, designated independent semantic/privacy review,
then scoped checkpoint/trainer/resource experiment approval. Do not appoint yourself,
download/install/train/provision/retry unchanged inference or activate defaults.

[Inspection report](QA/REPORTS/PENTA_DEVELOPMENT_CORPUS_PREREVIEW_2026-10-21.md).
No Mini/application/main edits or tests/model calls rerun. Prior host/UI evidence
retained; host/domain/history/device/enterprise/release gates/BUG-DATA-0001 OPEN.
No commit/push/Azure/resources changed. Direct local access needs no project switch;
this is a file handoff only, not a sent message or newly created task.

## Mounted Academy privacy repair — 21 October 2026

- [x] Academy now hides idle expired onboarding draft views and checks tab resume;
  verified lifetime/binding/late-read fences, no network poll or contract expansion.
- [x] 225 focused checks,lint0/0,TypeScript/87-route build PASS. Real SQL/API461 /
  browser33 PASS, run `8766bdab56a34b7eace88d5d6144bb39`, exit0/188.4s/exact cleanup.
  Both selected expiry contexts hide before recheck without an additional request.
- [ ] Mini full-plan fresh6/12/exposed7/12 NOT ACCEPTED.56 pending examples/zero
  eligible: independent designated review and scoped improvement/resource authority
  remain required. Host privacy tests neither improve weights nor qualify reasoning.

[Report](QA/REPORTS/PENTA_ONBOARDING_IDLE_EXPIRY_2026-10-21.md).
No Mini/backend/schema/vendor/model/main change, commit/push/Azure/training/download/
install/activation/restart;29containers/41volumes,two PENTA services running. Existing
durable-history/domain/security/device/enterprise/release gates and BUG-DATA-0001 OPEN.
Next High: actual reviewed model improvement/qualification and consequential host
integration. Medium for settled UI/docs. Do not rerun unchanged failed weights or
self-approve training examples; no project switch required for independent app work.

## Host expiry successor — 21 October 2026

- [x] Academy independent onboarding expiry gate:169 controlled checks,461 retained
  SQL/API checks and33 actual browser contexts PASS; four new selected/pending ×
  conversation/draft-only expiry cases. Run `4a6621078bf54a989fa9d241f8b7692e`,
  exit0/202s. No inference or new student in those four cases; pending claims retained,
  expired details withheld/reload blocked and the Manual fallback link retained.
- [x] Eight exact read audits/two result410s and unchanged selected SQL snapshots;
  exact successful owned cleanup. Prior failed logout-fixture run retained/stopped.
- [ ] Model full-plan remains fresh6/12/exposed7/12 NOT ACCEPTED;56 examples pending/
  zero training-eligible. No model improvement, new policy/tool contract or engine
  activation follows from host expiry tests. Existing review/resource gates remain.

[Report](QA/REPORTS/PENTA_ONBOARDING_EXPIRY_2026-10-21.md). All11broad delivery packages,
broader history/domain/device/enterprise/release gates and BUG-DATA-0001 remain OPEN.
No Mini/main/application edits, commit/push/Azure/training/download/install/resource
restart. Observed29containers/41volumes, two PENTA services running. Next High for
qualification/authority integration; Medium for settled UI/docs. No project switch
required for independent Academy work; do not rerun the unchanged failed model.

## Final continuation checkpoint — 21 October 2026

- [x] Mini exact-content review boundary:672distinct related tests PASS;56unsigned examples still pending/ineligible, no training or weight changes.
- [x] CSV security:API2861/2861,32actual export SQL/Identity/HTTP checks plus retained Access regression PASS; run7a2076155e0744069a61a9d18e9c16b6/exit0/160.8s/102+7migrations/no429/exact owned cleanup.
- [x] Download recovery+Reports canonical balance/Reconciled/currency/exact-decimal consumer repair:101combined frontend tests,20native handler journeys,36full exported-page journeys,typecheck/targetedlint and87-route export PASS. Browser APIs are synthetic, not silently promoted to linked SQL/device acceptance.
- [x] Selected issue reconciliation:119unique existing IDs,all indexed;28/28 original bounded feedback checkpoints retained;FP1/FP2/FP3 approval/repairs retained. No blanket issue closure.
- [ ] 11broad packages remain open at their outstanding gates. Model full-plan fresh6/12/exposed7/12 NOT ACCEPTED;actual review/training artifacts/trainer/measured safe resources/scoped approval still needed. Physical/client/host/domain/enterprise/release gates and BUG-DATA-0001 OPEN. No stage/commit/push/main/Azure/default change.

Next High: qualified model/resource and remaining host/domain/security gates; Medium: settled UI/docs/routine execution. Do not repeat accepted repairs, run an unchanged failed model, train on exposed answers, relax memory guards or start paid resources. Original25containers/41volumes/88frozen model-source files and four idle inference slots preserved. No project switch required with direct local access.

[Review](QA/REPORTS/PENTA_CURRICULUM_REVIEW_E5R62.md) · [CSV](QA/REPORTS/CSV_EXPORT_GUARD_2026-10-21.md) · [Recovery](QA/REPORTS/CSV_DOWNLOAD_RECOVERY_2026-10-21.md) · [Reports](QA/REPORTS/REPORTS_CANONICAL_LEDGER_2026-10-21.md) · [Remaining](ACADEMY_REMAINING_WORK_2026-10-21.md).


Final SQL successor /21 October2026: run7a2076155e0744069a61a9d18e9c16b6 passed32actual CSV export checks and retained Access regression,exit0/160.8s/102+7migrations/no429/exact owned cleanup. Initial below3GiB startup was blocked; RAM recovered naturally and unchanged guard passed, no restart or limits relaxed. CSV SQL gate now PASS; spreadsheet/device/enterprise/release and model qualification remain OPEN. Earlier NOT RUN notices below are historical. No push/Azure/production action.

## Current continuation — 21 October 2026

- [x] E62 exact-content offline review boundary:41new/672distinct related tests PASS; unsigned56-example packet, zero actual reviews/training-eligible examples or weight changes.
- [x] Academy/audit/browser Reports CSV guards:API2861/2861,TS27,four native downloads,typecheck/lint and isolated SQL harness build PASS.
- [x] Selected queue/index reconciliation:119unique IDs/119files,5P0/100P1/14P2; both existing auth records now indexed. Five inventory tests PASS; no issue closed.
- [ ] CSV SQL/Identity/HTTP gate NOT RUN:below3GiB host RAM stopped before container creation. No restart/lowered guard; existing Access assertions retained after opt-in checks.
- [ ] Training NOT READY; real full-plan fresh6/12/exposed7/12 NOT ACCEPTED. Actual data review, designated reviewers, exact checkpoint/trainer/measured resources and scoped approval remain required; no unchanged-model rerun.

11existing broad delivery packages remain open, not11small fixes. Retain28/28 original bounded form-feedback checkpoints and approved FP1/FP2/FP3; no repeated repair or enterprise closure. BUG-DATA-0001 and host/domain/Manual/device/enterprise/release gates OPEN. No stage/commit/push/main/Azure/model-default change. Next High for consequential model/resource/security/host work; Medium for settled UI/docs/routine execution. Direct local access needs no project switch.

[E62 review](QA/REPORTS/PENTA_CURRICULUM_REVIEW_E5R62.md) · [CSV result](QA/REPORTS/CSV_EXPORT_GUARD_2026-10-21.md) · [Queue](QA/REPORTS/DELIVERY_QUEUE_RECONCILIATION_2026-10-21.md) · [Pending](ACADEMY_REMAINING_WORK_2026-10-21.md).


## E5-R61 complete — 21 October 2026

- [x] Offline review curriculum and local training-readiness assessment completed: 56 synthetic examples (42 development, 14 validation), 246 known exposed case IDs excluded, 631 related tests passed.
- [x] All examples remain pending label/privacy review and ineligible for training. No training, inference, installs, downloads, model activation, Git push or Azure deployment occurred in this batch.
- [ ] Training readiness: **NOT READY**. The configured library lacks both original 3B checkpoint shards; no verified GPU trainer or measured training-memory fit exists. Quantized/offloaded training fit is unproven, not declared impossible.
- [ ] Model qualification remains open: the prior actual full-plan result is still fresh 6/12 and exposed 7/12, **NOT ACCEPTED**. This preparation does not improve weights or qualify the application.

Next: Sol High for a reviewed development-only improvement/resource plan and fresh semantic qualification. Do not tune on exposed answers or start acquisition/training/paid resources without scoped approval. All host, Manual, device, enterprise and release gates, including BUG-DATA-0001, remain open. No project switch is required while direct local access works.

[Batch evidence](QA/REPORTS/PENTA_TRAINING_READINESS_E5R61.md) · [Remaining work: 11 existing open packages, not 11 small jobs](ACADEMY_REMAINING_WORK_2026-10-21.md).

## E5-R60 FINAL /20 October 2026

[Real full-plan result](QA/REPORTS/PENTA_FULL_PLAN_E5R60.md):24turns/48requests/fresh6/12exact,exposed7/12exact/NOT ACCEPTED. Wrong removals/ambiguous-reference guess/protectedrequest misrouting blockqualification;no host/role/audit mutation. Original25exact/healthy4idle/frozen65unchanged/noactive trial.377relatedPASS/382grammar assertions. Next High Mini development-only field-edit/safety curriculum+checkpoint/hardware training-readiness assessment;all24nowexposed regression-only,nottraining/promptpatchtargets. No project switch needed while direct local access continues;no training/download/paidGPU/default/Git/Azure. Fresh qualification+app/enterprise/release/BUG-DATA-0001 OPEN.

## E5-R59 FINAL /20 October 2026

[Inactive2stage provider](QA/REPORTS/PENTA_TWO_STAGE_PROVIDER_E5R59.md) implemented/shared30s/max2calls/64+448tokens/nativegate+mandatoryreducer.66new mocked-native tests/318relatedPASS12.00s,10reviewpins;no actual inference/activation. R58real3B11/12NOT ACCEPTED remains. Next High fresh full-plan cases+nativegrammar/both-stage token/schema/budget harness,then bounded real3Bqualification. No project switch needed/no training/default/tier/Git/Azure;host/app/Manual/enterprise/release/BUG-DATA-0001 OPEN.

## E5-R58 FINAL /20 October 2026

[Actual3Bcomparison](QA/REPORTS/PENTA_3B_INTENT_RESULT_E5R58.md):12requests/11correct/1wrong/NOT ACCEPTED;IE-09clarify→refine. Original25exact/healthy4idle/frozen57unchanged/noactive trial;actualtemplate/library/GPU/batch/schema/token verified. Next High inactive bounded2stage draft-plan composition/shared30s/64+448tokens/catalog+reducer,then fresh independently authored fullplan/safety evaluation. No project switch needed/no training/tier/default/Git/Azure;full app/enterprise/release gates OPEN.

## E5-R57 FINAL / E58 PREPARED /20 October 2026

[3B verifier outcome/source correction](QA/REPORTS/PENTA_3B_INTENT_COMPARISON_E5R57.md): R57stopped82.25s before classifier/0requests/no score, original25exact/healthy4idle/frozen55unchanged. E58source-proven template transformation prepared/252focusedPASS/9.83s/read-onlypreflightPASS; no E58modelrun/no active trial. Next High one separately bounded corrected comparison/same R56prompt+scoring+guards. No project switch needed; no training/restart/prune/default/tier/Git/Azure changes; enterprise/release/BUG-DATA-0001 OPEN.

## Latest FINAL E5-R56 /20 October2026

[Fresh intent-only result/next plan](QA/REPORTS/PENTA_INTENT_PROBE_E5R56.md):new8dev/12evaluationfamilies/inactiveintentcontract/215focusedPASS;12fresh intent replies5exact/NOT ACCEPTED. Differentmetricfromfullplans;two-stageoperationalprovider notimplemented. Original25exact/healthy4idle/frozen64unchanged. Next High controlled already-downloaded planned3Bcomparison with exactartifact/native/template/schema/token/memory/offload/restoration gates;noautomatictierchange/answertraining/relaxed4Bguard/Azure. No project switch needed/no active trial;all enterprise/release/BUG-DATA-0001 OPEN/no main/Gitchanges.

## Latest E5-R55 FINAL /20 October2026

[Actual isolated candidate comparison](QA/REPORTS/PENTA_CATALOG_TRIAL_E5R55.md):12requests/2exact/9semanticwrong/1reducerrejected,NOT ACCEPTED. All12actualschema hashes/distinctprofile/GPUproof verified/original25exact/healthy4idle/frozen61unchanged. Repeatedsteps8→0butall12prepare+replace. Next High reviewed general intent/transition improvement and independent fresh split;no repeated tuning/training on exposed12/hostrule changes/rebuild/default activation. No automatic project switch needed;no active trial/main/commit/push/Azure;all enterprise/release/BUG-DATA-0001 OPEN.

## Latest completed packet — E5-R52–R54 /20 October2026

[R52build/binary/trial](QA/REPORTS/PENTA_RETRY_BUILD_E5R52.md),[R53/R54diagnosis/candidate/next requirements](QA/REPORTS/PENTA_PLAN_DIAGNOSIS_E5R53.md):native75proofPASS/12actual replies/1exact/NOT ACCEPTED,original25exact/healthy4idle/frozen59unchanged. Offline12wirevalid/8reducerrejected/3semanticwrong;inactive catalogdecoder182focusedPASS/120nativegrammar assertionsPASS/no newinference/training/hostcalls. Next High isolated candidate provider+distinct qualification fingerprint/current trusted Turn+actual schema capture/native/tokenfit before bounded same-budget evaluation;independent fresh safety/semantic gates remain. No project switch required or serving/default activation/rebuild/cache sweep/exposedanswertraining. All enterprise/device/release/BUG-DATA-0001 OPEN,no main/Git/Azure changes,no active build/trial.

## Latest final result — E5-R51 /20 October2026

[Corrected build attempt](QA/REPORTS/PENTA_CORRECTED_BUILD_E5R51.md):ownerapproved,no restart;85.96s/BLOCKED_BUILD_MEMORY_GUARD/max3553157120<3758096384bytes/0installs-compiles-model calls/no builder. Original25exact/healthy4idle restored/continuation stopped.212focusedPASS;24resume-routePASS(overlap). Same-cache CMake empty-directory fix prepared,real corrected build/binary/trial NOT RUN. Next High scoped sufficient-memory attempt,no automatic retry/restart/guard relaxation/project switch/Azure. All enterprise/device/release/BUG-DATA-0001 OPEN,no app/default/main/Git changes.

## Latest native build checkpoint — E5-R49/R50 /20 October2026

[R49 final timeout/restoration](QA/REPORTS/PENTA_NATIVE_BUILD_E5R49.md),[R50 incremental current checklist/result](QA/REPORTS/PENTA_INCREMENTAL_BUILD_E5R50.md). Continue existing local runtime work,not a new project switch. R49compile2400s timeout/no OOM/original25restored/4idle/188testsPASS. R50oneapproved incremental attempt/no restart/4785members/3684pinned sources/146objects/full exact packages verified/49focusedPASS. Build/binary/unchanged trial acceptance remain gated on actual final receipts. No model/default/app/DB/Git/main/Azure activation;enterprise/device/release/BUG-DATA-0001 OPEN. Next High.

## Direct model JIT evidence — E5-R48 /20 October2026

[Report](QA/REPORTS/PENTA_JIT_TRACE_E5R48.md):repaired trace run a5f768b46bbd valid report/stop0/export,18PTX→CUBIN compiles64.613s,limited kernel visibility. Firstrequest30.050s/no complete response/quarantine/11unrun.148focused testsPASS/original25restored/healthy/4idle/frozen61unchanged. Next High bounded75-real candidate build at same revision;official builder/source metadata pinned,D:plan,no layers pulled/build/activation. Resolve resource/time/storage/package caps before acquisition,verify75ELF/revision then unchanged uninstrumented semantic/latency qualification. No further cache/loader sweeps/no driver/global install/restart/Azure. Appstopped/no app/default/DB/Git/main changes/all enterprise/release/BUG-DATA-0001 OPEN.

## Native tools/trace checkpoint — E5-R46/R47 /20 October2026

[Inspection](QA/REPORTS/PENTA_NATIVE_TOOLS_E5R46.md)/[trace results](QA/REPORTS/PENTA_NATIVE_TRACE_E5R47.md):137focused testsPASS;actual75PTX/no75cubin,device CUDA capture/import/SQLite analysis and no-model finalization PASS without extra privileges. Firstmodel30.064s/no plan/incomplete trace because name-vs-ID cleanup hook;fixed/tested,old capture retained/recovery failed. Corrected f6a2a1cd9036 memoryblocked0loads/requests/12unrun;original25/config/host/mount/state restored/healthy/4idle. Next High sufficient-memory guarded full trace,then actual timing attribution/fix;no automatic retry/restart/guard relaxation/Azure/model activation. No Academy app/DB/Git change;app3000/5092stopped,all semantic/enterprise/release/BUG-DATA-0001 OPEN. No need to resend requirements/restart backend or enterprise plan.

## Local GPU permission/EAGER blocked — E5-R45 /19 October2026

[Report](QA/REPORTS/PENTA_EAGER_RESULT_E5R45.md):owner authorizes bounded local NVIDIA dev,not paid Azure/driver/model activation.94testsPASS/57pins0drift,run089c8964c6ea load150sdeadline blocked0generation/12unrun/no loaded-offload acceptance;originals restored/healthy/4idle/owned cleanup. Next High stop cache/loader tuning,inventory bounded native trace/sm75same-revision build;no tools installed/downloaded/restart/Azure. Both returns updated,all semantic/enterprise/release gates OPEN.

## First-use initialization review — E5-R44 /19 October2026

[Report](QA/REPORTS/PENTA_NATIVE_TIMING_E5R44.md):read-only cache-write/native-return spacing and sampler-init0.14ms review,first-use compilation lead not causal proof; no new tests/code/inference/service changes. Next High single empty-cache EAGER proposal vs R41cold, unchanged150sload/30srequest/guards/model/scoring/restore, NOT IMPLEMENTED/RUN/pending scoped approval. No priming/restart/build/sweep/Azure/recursive cache retry. Both returns updated, all semantic/enterprise/release gates OPEN.

## Actual pre-populated cache result — E5-R43 /19 October2026

[Report](QA/REPORTS/PENTA_WARM_RESULT_E5R43.md):approved unchanged-wrapper retry/guard passed/57pins0drift/exit0/161.74s. First660token request30.063s/no complete reply/quarantine/11unrun,NOT ACCEPTED. Native prompt656tokens/17.87s/first sampled token~29.811s; no complete reply/accuracy. Cache20→22files/originalsource unchanged; originals restored/healthy/4idle/owned cleanup.83prior tests retained NOT rerun/no code redo/restart/Azure. Next High read-only long-work/cache-timing analysis then specific profiling plan, no blind retries/priming/relaxed gate; enterprise/release/BUG-DATA-0001 OPEN.

## Warm comparison guarded attempt — E5-R42 /19 October2026

[Report](QA/REPORTS/PENTA_WARM_CACHE_E5R42.md):83testsPASS/57pins0drift/exit0/95.54s, source cache/driver verified but host guard got only2of3 sufficient samples. BLOCKED_MEMORY_GUARD/0loads/generations/12unrun/no new cache copy or containers. Originals restored/healthy/four idle slots/source receipts/cache unchanged. Warm quality/speed NOT TESTED. Next High separately approved further guarded attempt after memory settles, same wrapper/no code redo/restart/relaxation/Azure; cold/semantic/release gates remain OPEN. Both returns updated without protocol/domain activation.

## Actual cache-only diagnostic — E5-R41 /19 October2026

[Report](QA/REPORTS/PENTA_CUDA_CACHE_TRIAL_E5R41.md):74testsPASS/55pins0drift/exit0/151.35s; actualD:cache/128batch/25GPUlayers verified, first660token request30.051s/no reply/quarantine/11unrun,NOT ACCEPTED. Cache16files/46.35MB ready→19/63.60MB aftereval→20/69.79MB final retained privately; no exact JIT/kernel timing attribution. Originals restored/healthy/four idle slots/owned cleanup; no restart/retry/Azure. Next High proposed separately approved verified-copy warm-cache comparison, same model/runtime/driver/profile/30s/guards/scoring/restore, NOT RUN/no priming/automatic retry. Warm pass cannot close cold/semantic/release gates; both project returns updated.

## Native cold compilation/cache lead — E5-R40 /19 October2026

[Report](QA/REPORTS/PENTA_CUDA_JIT_REVIEW_E5R40.md): read-only saved CPU/GPU comparison, pinned build75-virtual defaults/upstream29513/static readonly cache-policy review. JIT/writable cache is a supported hypothesis, not confirmed root cause. No inference/tests/service changes. Next High proposed one D:compiler-cache metadata diagnostic, same128/128/model/profile/context/output/30s/guards/scoring/quarantine/12cases/15minutes/restore; NOT RUN/pending scoped interruption approval, no priming/warm retry/restart/build/Azure. Both returns updated, enterprise/release/BUG-DATA-0001 OPEN.

## Actual smaller-batch experiment — E5-R39 /19 October2026

[Report](QA/REPORTS/PENTA_GPU_BATCH_E5R39.md): owner continued R38 proposal, actual128/128/fullGPU/12token fits but first660token request30.032s/no reply/quarantine/11unrun,NOT ACCEPTED.59offline testsPASS/53pins0drift/exit0/173.25s; exact originals/config/host/mount/state restored/four idle slots/healthy/owned cleanup. No Docker restart/retry/download/Azure. Next High targeted native-backend diagnosis/profiling plan; no same configuration/batch sweep/looser gates. Academy app already stopped/remains stopped; broader qualification/enterprise/release/BUG-DATA-0001 OPEN. Both project returns updated without protocol/default/domain activation.

## Offline timeout diagnosis — E5-R38 /19 October2026

[Report](QA/REPORTS/PENTA_GPU_DIAGNOSIS_E5R38.md):47focused tests PASS,0new inference/service changes. Native initial prompt work did not return before30s; startup129.42s was outside that timer.25/25 layers GPU includes397.85MiB CPU-mapped embedding, not all-GPU operations. Exact kernel bottleneck unproven, model NOT QUALIFIED. Next Sol High proposal: only `-b128` (effective128/128), same pinned2B/profile/lv5/context/output/30s/guards/scoring/quarantine,12cases/15minutes/restore/owned cleanup; NOT RUN, separate scoped approval before interruption. No restart/sweep/download/Azure automatically, existing enterprise gates retained.

## Approved restart and GPU inference — E5-R37 /19 October2026

[Results](QA/REPORTS/PENTA_GPU_RESTART_E5R37.md): owner-approved single Desktop restart released RAM; original25containers/config/state/images/41volume metadata retained (only bridge ID regenerated). First e5r36-399a762bec70 failed offload log verification/0generations, retained. Diagnostic-only -lv5 wrapper/shortened remaining15min cap: e5r36-046c6840ff55 proves25/25GPU offload/1301MiBused/2638free/12token fits, but first660token request30.063s/no completed reply/quarantine/11unrun. NOT ACCEPTED.38offline testsPASS,51/52pins/four idle slots/exact baseline restore/healthy/owned cleanup verified; both observed process exit0. Next Sol High focused runtime/prefill/decode/log-overhead diagnosis before changed experiment, no identical retries/guard relaxation/new models/Azure automatically. No application/default/weights/training/promotion/main/publication/DB changes; all qualification/enterprise/release gates OPEN.

## GPU runtime checkpoint — E5-R36 /19 October2026

[Actual import/device check](QA/REPORTS/PENTA_GPU_CHECK_E5R36.md): pinned CUDA12manifest e8318a7... imported/verified D:, actual isolated CUDA0 T1000 checkPASS. First verifier config-ID assumption failed after successful load; fixed/tested, existing exact manifest/rootfs reverified/no reimport. Model trial e5r36-a1da046126c4 BLOCKED_MEMORY_GUARD,0loads/0generations/12unrun,51pins/exact original restore/four idle slots/healthy,35offline testsPASS. WSL Cached6049712kB observed; requested separate owner approval for supported Docker restart and retry only if unchanged guards pass, awaiting reply at checkpoint. No settings/cache-drop/app termination/model/default/training/Azure/Git publication. Next Sol High recovery/guarded actual GPU test; all model/host/enterprise/release gates OPEN.

## Current Docker storage — E5-R35 /19 October2026

[Report](QA/REPORTS/PENTA_DOCKER_STORAGE_E5R35.md): owner approved supported Desktop C:→D: move, now D:/DockerDesktopData/DockerDesktopWSL/disk/docker_data.vhdx. Offline hash-verified recovery D:/DockerDesktopRecovery/e5r35 retained/private/local.25 container identities/config/state,6 images,41 volume metadata inventories exact; only built-in bridge ID regenerated, strict mismatch retained/reviewed. Original two PENTA services healthy/four idle slots,Academy200. No CUDA import/GPU generation/app changes/pruning/Azure. Next Sol High guarded pinned CUDA import/compatibility after unpacked-space check; no model qualification or release gate closure.

## Current D:runtime archive — E5-R34 /19 October2026

[Report](QA/REPORTS/PENTA_CUDA_D_DRIVE_E5R34.md):ownerD:downloadinstruction,
officialpinnedCUDA12OCI filesdirectD:/PENTA-Models,all14blobs+manifestverified,
exit0/266.38s/0partials/10offline safetytestsPASS. No Dockerimport/GPUtrial or
C:imagecache/servicechange. DockerworkingdataVHDstillC:;separate supportedD:
relocation approval/planned interruption/recovery needed beforeexecutionstorage.
No manualVHDmove/prune/delete/Azure/promotion/publication. ModelgatesOPEN.

## Current read-onlyGPUfeasibility — E5-R33 /19 October2026

[Report](QA/REPORTS/PENTA_GPU_FEASIBILITY_E5R33.md):T1000/4GB/CC7.5/WSL2/NVIDIA
runtime present,cached llamaCPU/no GPUrequest. OfficialCUDA12same-versionamd64
digest identified metadataonly,~2.592GBcompressed;C:8.9GiBfree/DockerdataonC:,
unpacked/scratch/reserveunknown. No download/install/GPUtrial or servicechange.
Next High explicitpinnedacquisition/boundedGPUtestapproval+storagecheck;no
prune/data move/driverupdate/Azure/promotion. Prior386tests/model failuresretained,
no rerun;fullrealmodel/host/enterprisegatesOPEN. PairedreturnMinioperations/
GPU_FEASIBILITY_E5R33.md,not an automatic chat/projectswitch.

## Current approved4CPU trial — E5-R32 /19 October2026

[Report](QA/REPORTS/PENTA_CPU4_TRIAL_E5R32.md):NOT ACCEPTED,first2Bcompact
request30.051s/no completed answer/11unrun,actual4CPU4threads/12token fits verified.
386Python PASS/50pins/exit0/147.12s/exact original restoration/4idle native slots/
healthy/owned cleanup,Academy app200. No repeated CPU trial; next High read-only
localGPU/runtime feasibility proposal, no runtime acquisition/install/GPU test/
paid Azure/training/promotion/publication. Original read/semantic/full host/
enterprise/release gates OPEN; Mini operations/CPU4_TRIAL_E5R32.md paired return.

## Current actual compact trial — E5-R31 /19 October2026

[Report](QA/REPORTS/PENTA_COMPACT_TRIAL_E5R31.md):2Bcompact-v3 same2CPU/30s,
72token checks fit,first generation30.053s/no raw completion/71unrun. NOT ACCEPTED.
382Python PASS,50stagepins,exit0/147.29s,original restoration/native4idle slots/
owned cleanup verified. Academy app retained, old read/R29failures remain. Next
proposed bounded4CPU/4thread same-model/image experiment(max12generations/15min)
awaits explicit approval; no repeated2CPU run/GPU download/Azure/training/promotion.
No publication/main or external project switch. Existing broader gates OPEN.

## Inactive compact development — E5-R30 /19 October2026

[Current report](QA/REPORTS/PENTA_COMPACT_CANDIDATE_E5R30.md):378Python PASS,
trusted opt-in compact-v3 instruction, old profiles and host boundaries retained.
Actual72baseline tokenizer comparisons save71tokens each/9.96%input total,
0generation/load/no accuracy or latency improvement measured. New source pins
operations/compact-source-pins-e5r30.json; no default activation. R29 failures
remain, CORE-01 BLOCKED. Next High review exact changed-profile/resource proposal,
not identical reruns/gate relaxation/GPU assumptions or Azure consent. Academy
normal local app restored safely/health200/anonymous401, Admin sign-in unverified.
No publication/model promotion/training/main/Azure or external project switch.

## Current executed comparison — E5-R29 /19 October2026

Owner approvedR28 and requested all models. [Actual report](QA/REPORTS/PENTA_MODEL_COMPARISON_E5R29.md):4eligible text models loaded,7generation requests/3complete answers/1exact/2semantic mismatches/4native30s deadline outcomes;281unrun pairs. None accepted.9B/Gemma remain above-cap deferred,embedding is separate retrieval model.354Python PASS/47source pins/4receipts/exact original PENTA restoration/native4idle slots/owned cleanup verified. Next High semantic-development and measured runtime acceleration proposal; no identical failed trial or score/deadline relaxation. No production activation/weights/engine/default change/Azure/training/publication. Mini operations/MODEL_COMPARISON_E5R29.md is the paired current return; R28 below is historical approved request, not pending approval for the completed trial.

## Current qualification decision — E5-R28 /19 October2026

Host E5-R25–27 terminal/conversational-QA/pending-recovery work is complete at its
named evidence level. Latest actual Mini return remains inactive E5-R18, CORE-01
BLOCKED; no qualified model was found. See [exact bounded local2B trial request](QA/HANDOFFS/PENTA_MINI_OPERATIONAL_TRIAL_E5R28.md).
All5 transition source pins and existing2B artifact freshly verified. This is
**AWAITING OWNER APPROVAL**, not inference authorization or engine completion:
2B Q4_0 +transitions-v2,1slot/2CPU/2048context/512output/30s bound, max72operational
generations/60minutes, guarded exact idle-PENTA pause/restore, no Azure/download/
training/promotion. Fresh12 cases and evaluation wiring remain to be prepared
after approval. Unchanged failed read86/94 remains OPEN; operational trial cannot
qualify it. No Mini edits/container changes/new tests/inference/project switch or
automatic message occurred. Older sections below are retained history.

Date: 2026-10-09. Packet **N2u: read-language qualification dependency**.
Development route: **GPT-6.1 Sol High / Codex**, in the existing Mini project.
Status: **BLOCKED real-read acceptance; precise engine handoff**.

## Permanent independent host enforcement — 2026-10-10

### FW1 — shared AI frontend delivery dependency — 2026-10-10

Owner explicitly confirms PENTA AI's frontend is not ready and must still be
implemented in the PENTA project. Latest local
`docs/PENTA_FRONTEND_DELIVERY_PLAN.md` supersedes older frontend-ownership text:
Mini owns reusable customer AI frontend/design system/standalone shell;
Academy owns Manual Workspace, integrated host adapter and domain authority.
Academy currently `codex/penta-search` at `fc0354134790adb482727dcb3075ea9faabe2f30`.
See [execution path](ACADEMY_EXECUTION_PATH.md) and
[E0 queue](QA/REPORTS/EXECUTION_E0_RECONCILIATION.md). Request prepared here,
not automatically delivered to another chat, not a project switch or release.

**Required next PENTA frontend slice:** preserve current dirty engine/model/
policy/benchmark work; inventory the existing Academy pilot and build a coded,
interactive synthetic prototype using Orbit integrated/Prism light/Aurora
standalone directions. Raster mockups alone do not satisfy delivery. Supplied
gallery values/logos are illustrations, not production facts. Reconcile older
ownership docs to the latest directive rather than publishing conflicting copies.

Prototype must demonstrate conversation/composer, five capability help popovers,
typed result cards/tables/work canvas, verified-source/as-of labels, clarification,
empty/denied/stale/error/offline/unknown-outcome recovery and explicit unavailable
features. Real text/status only, no fake token streaming or reasoning. Approval/
draft views may be visibly synthetic; do not enable CRUD/send/delete/Autopilot
or persist customer transcripts on the basis of mockups or draft schemas.

Integrated prototype supplies host extension points for academy/current actor/
capability context; Academy owns PENTA AI / Manual Workspace switch and draft
handling. Standalone has no manual switch and no Academy ERP route/type imports;
optional connected-app destinations come from trusted adapters, not model URLs.
All4 Academy roles need separate host-supplied capability projections. Selecting
a role/academy in a prototype must not be portrayed as granting permission.

Return first **PROTOTYPE READY FOR VISUAL REVIEW**, with exact source/files,
local viewing steps, explicitly synthetic state inventory and responsive/theme/
keyboard/focus/overlay/no-overflow evidence. Then version reusable components
after accepted migration checks; return frontend source/version, token revision,
component registry/props, supported Tool Protocol/response versions, peer/build
requirements, adapter integration instructions, tests/limitations and rollback.
No unversioned copy or pilot replacement. Academy retains own Manual forms and
protected operation rules while adopting matching visual foundations.

**Independent engine gate:** existing saved86/94 NOT ACCEPTED remains N2u's
dependency. UI delivery does not fix or qualify language planning. Use the
existing candidate/contract handoff; do not rerun unchanged failed inference,
add hosted fallback, obtain weights or change runtime without authority.
Mini/Core may reject unsafe plans; Academy independently authenticates/current-
scope-authorizes every actual operation. No roles/security/finance-policy changes
from conversation or memory. No Azure/customer data/tool enablement in FW1.

Model routing: Sol High for ownership/versioned adapter/security decisions;
Sol Medium for settled prototype/components/styling. Return an explicit handoff
at shared interface changes or prototype/package delivery, then Academy consumes
only the accepted version and runs its integration/manual fallback regressions.

### Shared regulatory/policy register acknowledged — 2026-10-10

Observed and remote-verified Mini commit `2f8806967709a1a7ec29e5176e7c31968adbf6aa`,
register v0.1 blob `956e5b37b5fdfe982a23862e5a696f142e7205c0`, and preceding
`18250d395ad89835afcb0c4f68e8eeac78c8d2f8` policy foundation. Read
`COMPLIANCE/HANDOFF_TO_ACADEMY_DESK.md`, register/control plan and
`docs/PENTA_POLICY_V1_HANDOFF_TO_ACADEMY.md`/non-negotiable policy. Academy keeps
one [local implementation/evidence map](COMPLIANCE/ACADEMY_IMPLEMENTATION_MAP.md),
not a competing regulatory register or duplicate model/learning engine.

Reference `penta-non-negotiable-1.0` and compliance-profile modules are not wired
into live `/v1/chat`. ReviewedProcessingFacts presence checks and fake ledger tests
do not prove human/legal approval, durable audit/quota or actual runtime enforcement.
Active Tool Protocol0.1/two reads remains unchanged. Do not add draft DTO fields or
claim a deployed privacy profile merely because references exist in a test.

Precise canonical-owner follow-ups (not permission to rewrite the engine):

1. REG002: [G.S.R.892(E)](https://www.meity.gov.in/static/uploads/2025/12/3c7ebbae0e5456f493f486e6845df86b.pdf)
   independently read here: dated10 December2025, Gazette11 December. The16 December
   date is website listing, not the notification date. Map the corrected wording
   and schedule lettering; retain phase lengths and require corrected-rule review.
2. Add applicable telecom/DLT scope: [TRAI](https://trai.gov.in/release-publication/regulations/amendments-page/7617)
   lists final Third Amendment18 September2026. Exact PDF commencement/exceptions
   were not retrieved here; retain OPEN. Do not rely on March2026 draft or2025 alone.
3. Add conditional RBI/payment/card-processing and UK-law profiles after official
   source/applicability review; do not apply payment-provider rules to all fee ledgers.
   Independently verify REG016 current consolidated EU AI Act/amendment timeline
   before geographic/high-impact use; Academy has not accepted that date assertion.
4. Before customer-data inference, pair a genuinely versioned reviewed-purpose/
   child-data/minimization/retention/processor/egress contract and runtime evidence.
   Keep no training/global memory by default. References must point to actual
   accountable approvals; caller/model flags are not authority. No active DTO change
   is required to run today's synthetic hostile-planner host regression.

Existing read-language86/94 NOT ACCEPTED and qualified-candidate return remain
the first real-feature dependency. Preserve the blocked-run evidence; no unchanged
model rerun, inference upgrade or Mini/container edit from Academy. Shared policy
never replaces Academy current DB authority, transactions, approvals and audit.
The [new checkpoint](QA/REPORTS/PENTA_COMPLIANCE_PHASE0_SECURITY_CHECKPOINT.md)
extends host completion-audit/revocation tests; fake planner is not engine acceptance.
This is an in-repository handoff, not an automatic message to another chat.

The owner's non-negotiable security directive now lives in Academy's
[host policy](PENTA_HOST_SECURITY_POLICY.md), policy ID `academy-host-security/1.0`.
It applies independently of Mini/Plus/Pro/Ultra. Core may reject unsafe plans,
but Academy still resolves the authenticated actor/current tenant, validates
allowed tools, reauthorizes, requires any domain approval, invokes existing
services and records required audit. No role/tenant/memory/model-tier assertion
can authorize an operation. Customer connector hosts need the same independence.

This policy adds no wire change: Tool Protocol 0.1 and the two read tools remain
unchanged. No genuine breaking Core change was identified in this slice. Future
policy/tool/learning/approval changes require the established versioned paired
handoff; do not silently activate drafts or new tools. Feedback/memory may not
override host policy, finance, permissions, approvals or security configuration.

Academy's [report](QA/REPORTS/PENTA_HOST_SECURITY_ENFORCEMENT.md) records 32 passing
hostile-planner controls through real disposable SQL/Identity/HTTP, 78 targeted
backend tests and 16 synthetic frontend recovery cases. These do not qualify
actual Mini reasoning or Plus/Pro/Ultra engines. Existing 86/94 semantic failure,
qualified-candidate return and linked security/browser/release gates stay OPEN.
No Mini source/runtime/model changes or automatic cross-project messages here.

Read-only synchronization observed Mini now at `e2f89dc7fcbdaa087d19f193ef90763009065e7e`
after independent model-library/provisional-tier registry commits. Its new
`docs/PENTA_FOUR_TIER_HANDOFF.md` explicitly retains protocol0.1, the same tools,
unchanged authority and 86/94 NOT ACCEPTED. No enabled tier picker or new catalog
is authorized by metadata. Historical Mini HEADs below are earlier observations,
not a claim this task froze another project's branch. Academy changed no Mini file.

## Returned contract acknowledged — 2026-10-09 (N2w)

Academy reviewed Mini's local `PENTA_RETURN_TO_ACADEMY_DESK.md`, frontend
synchronization contract, usage-interface draft and N2u candidate decision.
Return status is **CONTRACT READY / ENGINE BLOCKED**, not a fixed planner or
permission to activate drafts. Mini HEAD remains `a50bbd3`; returned documents
are local/uncommitted, not a published artifact manifest. Saved 86/94 and all
eight semantic failures remain controlling; no unchanged inference rerun here.

Academy continues bounded source-backed UI work under active host receipts and
protocol 0.1. N2w adds a local generic Cards/Table renderer without host-domain,
route or inference dependencies; it does not implement draft artifacts, usage
metering, billing, memory or new tools. [N2w report](QA/REPORTS/PENTA_N2W_VERIFIED_RESULT_VIEWS.md)
records synthetic browser/build checks, not model/SQL acceptance.

Mini's separately scoped pinned Qwen3.5-4B trial is a proposal only: acquisition,
runtime and training approval are missing, and observed free memory was below
the proposed start guard. Academy downloaded/activated nothing and made no Mini
edits. A qualified 94/94 plus strict safety/provenance return is still required
before the existing linked SQL/security/15-turn browser and owner-view gate.

## 1. Ownership, branch and current integration

Academy Desk is the lead product and owns the complete integrated frontend,
UX, connector, domain/API/SQL logic, current authorization, receipts, audit,
end-to-end tests and enterprise release gates. This owner directive supersedes
the earlier request to skip the AI frontend. Standalone frontend is a later
PENTA commercial track. Mini owns shared reusable Core, inference, interpretation,
Tool Protocol, model-level context, learning interfaces and engine evaluation.
Do not edit another project's application files to resolve this packet.

Application worktree: `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`.
Inspected application HEAD before this documentation commit:
`d3c4ccfd6ea8344a73e390e866b43f512cb51e17`.
The eventual handoff commit is a documentation successor, not an application
runtime/model update. Recheck branch/HEAD/status before continuing.
`D:\AcademyDesk` main remains untouched; existing uncommitted work/evidence stays.

Mini worktree: `D:\PENTA AI Models`, branch `codex/academy-language-contract`,
observed HEAD `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe` plus prior dirty work.
No automatic synchronization of these chats or uncommitted files is assumed.

The existing first read feature already has a prompt-first PENTA AI/Workspace
switch, five capability helpers/context rail, authenticated scoped sessions,
provider, only SearchLearners/GetLearner, protected state, deterministic
Completed+Reconciled balance calculation, source artifacts and required audit.
Earlier narrow real Mini/SQL/browser passes remain historical evidence.
Current broad reliability and source-linked correction/reset acceptance is open;
no READY TO VIEW, production, all-five-capability or 90% completion claim.

## 2. Exact engine dependency and why the host cannot repair it

N2r source candidate 0.1.14/planner-0.12 has **86/94 exact** (NOT ACCEPTED).
243 installed Mini unit tests passed; shape correctness did not repair semantics.
All eight failed IDs below remain from N2n. The two old query-removal policy
false denials now reach inference, but the generated arguments are still wrong.
Current normal serving was last reported 0.1.13/planner-0.12; that observation
is from the supplied Mini checkpoint, not a fresh service probe in N2u.

Wrong valid filters can select the wrong authorized cohort. A record code
used as learner_id must fail closed, but denying it does not fulfill the request.
Academy cannot safely infer missing intent, remove model-generated criteria,
parse prompts into substitute tool calls or fabricate the requested result.
Those changes would duplicate shared intelligence and hide the model defect.
Host authority, finance calculations and strict validation must stay intact.

## 3. Exact saved reproductions

Each active protocol 0.1 request has `user_message`, `conversation_id`, supplied
`state` and `available_tools=["SearchLearners","GetLearner"]`. State shorthand
below lists filters, sort and displayed IDs relevant to the reproduction.
Expected arguments are the **complete flat execution arguments**, not sparse
internal planner edits. Tool = SearchLearners for every row below.

| Failed ID / exact message | Entry state | Expected arguments | Observed wrong proposal |
| --- | --- | --- | --- |
| N2-04: `plese find studnts with pendng fees` | Empty filters/results, null sort/selection | `{balance_status:Pending}` | Adds `subject:Piano` |
| FRESH-04: `New lookup: student code STU-77. Drop the old piano, unpaid and ordering filters.` | Piano/Pending, outstanding_desc, displayed `11111111-1111-4111-8111-111111111111`, selection null | `{name:STU-77}` | GetLearner for the old displayed UUID |
| OMISSION-FRESH-05: `Switch the subject to Cello, keep pending balances, and discard the ordering.` | Violin/Pending, outstanding_desc, two displayed IDs | `{subject:Cello,balance_status:Pending}` | Retains outstanding_desc |
| OMISSION-FRESH-06: `Remove the name, fee restriction and sorting. Keep only Violin and list those students.` | Mira/Violin/Pending, outstanding_desc, one displayed ID | `{subject:Violin}` | Retains outstanding_desc |
| TRANSITION-FRESH-01: `Leave just the Flute subject filter; no name, fee condition or order.` | Leela/Flute/Pending, outstanding_desc, three displayed IDs | `{subject:Flute}` | Retains name, Pending and sort |
| SPARSE-FRESH-06: `Forget the previous search; look up student record REC-617.` | Nila/Drums/Pending, outstanding_desc, empty displayed IDs | `{name:REC-617}` | GetLearner with learner_id=REC-617 |
| DECODER-FRESH-02: `Remove the name and the ordering; retain the course and unpaid condition. List students.` | Anika/Harp/Pending, outstanding_desc, no displayed IDs | `{subject:Harp,balance_status:Pending}` | Drops Harp and retains outstanding_desc |
| DECODER-FRESH-04: `Keep only the Harp course. Remove every other criterion and ordering, then list students.` | Anika/Harp/Pending, outstanding_desc, no displayed IDs | `{subject:Harp}` | Retains Anika, Pending and sort |

All selected learner IDs above are null. Complete UUID sets, request fixtures,
outputs and raw direct-probe plans are in the existing evidence/benchmarks.
Do not reconstruct shortened table state as a replacement for frozen fixtures.

Local Academy report: `QA/REPORTS/PENTA_MINI_N2R_POLICY_LANGUAGE_GATE.md`.
Evidence: `QA/EVIDENCE/penta-mini-n2r-20261008/manifest.json` and eleven result
files. Mini original results: `D:\PENTA AI Models\results\n2r-eval\`.
N2u read-only preservation verified all **11/11** result hashes against the
manifest; no new model/SQL/browser run. Report records 94 contracts/state valid,
93 diagnostic host compatible, required no-call pass and zero observed raw
Search contradictions. API raw plans for the original 26 cases were not captured;
do not claim those as observed. Source hashes are not weight/deployment attestation.

Reproduction procedure: read the N2r manifest and the existing frozen suite for
each ID; load exact entry state/tools; inspect the corresponding saved output.
Only evaluate again after an explicitly scoped candidate change. Use the
existing unchanged Academy `grade()` and Mini API/direct-probe runners, serialized
and without output edits, answer repair, repeated retries or overlapping runs.
Do not use this table or PC fixtures as model-training material.

## 4. Canonical contracts and compatibility

Consume Mini `docs/PENTA_PLATFORM_CONTRACT.md`,
`schemas/platform-contract/`, `docs/conversation-draft-n2t.md`,
`schemas/conversation-draft-n2t/` and frozen `benchmarks/conversation-draft-v0.2/`.
Mini is the canonical shared contract owner; Academy is its consumer.

Active Tool Protocol **0.1** and `/v1/chat` remain unchanged. Expected read
response: protocol_version=0.1, kind=TOOL_REQUEST, fixed bounded message,
tool_call with code-owned name/validated flat arguments, risk=READ, original
supplied state unchanged. Host owns state advancement from verified outcomes.
Record codes are search values; GetLearner uses an authorized stable identity
from the current displayed set. No model-provided source/permission grants.

`conversation-draft-0.2.0` and `core-boundary-draft-0.1` are **isolated drafts**.
They are not a live shared conversational endpoint or a protocol migration.
ReferenceCore is not imported into the current API. Its mock adapter is the
existing mock, not a second independent ERP. Natural free-form grounded prose,
durable memory, real approvals/actions and authenticated standalone gateway
remain pending. Saved N2t 264 tests and Core 63 tests validate synthetic boundary
behavior, not actual model decisions or source/tenant execution.

The host deliberately uses tighter limits than generic protocol maxima:
2,000-character input, ten displayed UUIDs and at most three nonempty filters.
Keep those safety limits; do not weaken the host to match a looser generic DTO.
Shared future native IDs require an agreed adapter/version, not silently
relaxing Academy's source references. Any breaking wire change requires a
separate proposal and coordinated adapter/tests before runtime activation.

## 5. Specific Mini task: one bounded qualification decision, then candidate

First reconcile this handoff with current source and N2t/platform work. Do not
restart the model project, repeat audits or redo completed fixture construction.

Prepare **one concrete private candidate/decoder/resource packet** using the
eight saved semantic failures and existing N2t evaluation plan. Determine
whether the selected settings/schema can represent the required decisions and
fit the complete tokenized prompt/output without truncation. Check local
available artifacts, exact base/quantization/tokenizer/template/runtime identity,
license, context/KV/OS headroom, and the measured laptop bounds. Separate model
semantics from deterministic policy and host errors. A larger model is a
hypothesis, not a guaranteed fix.

If an already-authorized local candidate change is available, implement/evaluate
only that bounded change with unchanged compatibility/safety controls. Otherwise
return the exact proposed artifact, resource requirement, acquisition/training
or setting change needed and a cost/quality rationale for approval. The owner's
offer of an Azure VM is not permission to provision it. No download, training,
GPU purchase, production install or hosted fallback is granted by this handoff.
Stop on an unmet resource/authorization boundary with a concrete decision,
not a vague "improve Mini" or an unchanged benchmark rerun.

Avoid further case-specific text rewrites or host substitutes. Preserve current
strict shape/compiler rejection, allowlist, no-call controls, bounded admission,
unknown-outcome quarantine, no retries and response-bound provenance.
Maintain independent fresh acceptance families; exposed 94 and PC fixtures
remain regression controls outside training, not unbiased final holdouts.

## 6. Acceptance, regression and exact return condition

Return **SWITCH PROJECT CHECKPOINT: PENTA MINI → ACADEMY DESK** with:

- Exact engine branch/commit and dirty candidate/artifact manifest; identify
  committed versus uncommitted source. No assumption that HEAD includes changes.
- Actual serving versus source package/prompt/protocol versions, artifact and
  tokenizer/config hashes; reported versus verified provenance clearly separated.
- Candidate decision/change, all test counts/failures, original eight dispositions,
  measured resource/latency/capacity observations, and limitations.
- Unchanged 0.1 compatibility or a separately proposed version/adapter request.
- Exact evaluation commands, frozen fixture/evaluation hashes and durable result
  locations accessible on this laptop; no customer credentials/raw private data.
- Application integration instructions, recovery/rollback and approved runtime
  enablement scope, or the exact resource/approval blocker if still unresolved.

**ENGINE READY FOR LINKED RETEST** only after a genuine changed candidate passes
all **94/94 exact existing read regressions**, no-call/state/host-boundary checks
and preserved safety/provenance tests. Record failures rather than replace
expectations. If qualifying the new conversational draft, its 48 applicable
engine turns and strict evidence/schema assertions also need actual candidate
evaluation; that is separate from keeping the read-only 0.1 pilot compatible.
Passing 48 expected-payload validators alone does not qualify model reasoning.

If the candidate does not pass, return **BLOCKED / NOT ACCEPTED** and the
evidence-backed next decision. Do not advertise a resolved integration, broaden
tools/history/writes, automatically try another model or train to the scoreboard.
Do not edit Academy code, deploy Azure, close enterprise issues or delete evidence.
Any Mini feature-branch publication needs its own scoped validation/governance;
this outgoing Academy documentation commit does not publish Mini's dirty work.

## 7. Academy task when Mini returns

Verify contract/artifact/source identity and reproduce the current accepted
candidate through Academy's unchanged provider/strict validation. Then rebuild
the existing SQL harness and run the prepared three original plus three
correction/reset real Mini/Identity/HTTP/disposable-SQL journeys, authority/
replay/audit/fault checks and the existing 15-turn linked browser protocol.
Use real sorted source identity, exact balances and persisted truthful provenance;
tear down only exact run-owned resources and retain all observations.

Complete/refine the existing integrated frontend under Academy ownership;
real receipts and source-linked cards, accessible clarification/recovery,
light/dark/responsive and manual fallback. No new parallel standalone app.
Only after source/security/browser acceptance provide **PENTA FEATURE CHECKPOINT:
READY TO VIEW**, exact local URL/role/conversation/branch/commit/limitations,
then stop major expansion for human review. Physical-device, privacy, enterprise
and pre-Azure gates remain independently explicit.

While this dependency is open, Academy may implement isolated consumer contract
tests/defined presentation work without enabling a draft route, copying Core,
altering engine files or claiming actual-model acceptance. Preserve the blocked
read feature; do not switch repeatedly for cosmetic issues. Sol High owns
consequential integration; Sol Medium fits settled bounded UI changes.

## 8. Publication and validation boundary

N2u changes Academy coordination documentation only. No runtime/model/DB/UI,
fresh inference/SQL/browser, main merge or Azure action. Existing application,
Mini and QA/EVIDENCE changes remain local. The outgoing handoff is self-contained;
local evidence/contract paths are explicitly not promised as remote artifacts.
Normal feature-branch documentation push is allowed by the lead directive;
it does not merge to main or trigger an Azure deployment request.
