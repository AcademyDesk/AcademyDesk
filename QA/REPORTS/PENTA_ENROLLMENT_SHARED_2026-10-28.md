# Enrollment operational backend cohort — 28 October 2026

Status: SHARED OPERATIONAL BACKEND QUALIFIED LOCALLY. The shared create/lifecycle/
transfer operations, independently authorized PENTA facade and inactive typed
field adapters passed3,183API/zero skips,85SQL cases and36CI guards. This is
not an enabled enrollment AI tool, conversational workflow or production approval.

## Scope and preserved behavior

Continue AI-06/E6 from the existing roadmap; do not repeat accepted teacher UI or
restart enterprise testing. Worktree `D:\AcademyDesk-codex-p0`, branch
`codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`, plus
preserved legitimate uncommitted work. No changes to main or Mini source.

The existing `EnrollmentsController` create, lifecycle update and transfer bodies
move into internal `EnrollmentMutationOperations`. Manual routes, DTOs, response
codes/messages/location, access filtering, atomic audit attributes, defaults,
operation order and admission lock remain. Controller result mapping preserves
native BadRequestObjectResult/ConflictObjectResult, not only equivalent wire codes.

Existing deterministic rules remain shared, not duplicated in a model:

- Current own student/batch references, open/active batch and completed course
  prerequisites, including completed history in an inactive batch.
- Capacity/waitlist limits and the academy-wide transaction admission applock,
  shared with promotions and batch edits. No new enrollment may duplicate an
  Active/Waitlisted admission; activate the existing waitlist row instead.
- Update status normalization, required lifecycle reason, null end dates and
  trimming. No new status/terminal/date policy is invented by this extraction.
- Transfer requires active source and open target, validates capacity/duplicates/
  prerequisites, preserves transferred source history and creates target admission
  atomically with the caller's required audit transaction.
- Manual's existing unknown-create-status default Active and UTC date defaults
  remain. The stricter inactive PENTA adapter refuses ambiguous admission intent
  rather than silently activating it. That restriction does not change Manual.
- Enrollment remains Core; empty paid-module lists do not disable it. Manual
  granular permissions (including FrontDesk students.manage) remain with the
  existing filter; this PENTA facade initially admits current Owner/Admin only.

## Authoritative service and inactive field contracts

`EnrollmentMutationService` reuses the same internal operations after verifying
authenticated identity, current database actor/role/active academy/tenant. Claimed
roles or model tenant IDs do not grant access; actual platform owners do not act
through this academy Owner/Admin PENTA facade. SQL callers must already own the
same physical SQL/Identity transaction; mixed/idle contexts refuse before access.
Academy, current identity and memberships are locked until the caller's commit.

The service owns no approval, replay ledger, audit or commit. Its successful return
is not a committed chat receipt. A future host workflow must implement exact scoped
approval, revision/source-state/duplicate checks, required audit and persisted
receipt/recovery under its transaction. No controller directly exposes this facade.
Manual uses its existing independent access/audit filter over the shared operations.

Inactive `PentaEnrollmentFields` adapters provide:

- AcademyDesk.Enrollment.Create: StudentId, BatchId, optional StartDate/Status.
- AcademyDesk.Enrollment.Update: EnrollmentId, explicit Status, optional EndDate,
  required LifecycleReason for a non-Active status, bounded to1000characters.
- AcademyDesk.Enrollment.Transfer: EnrollmentId, TargetBatchId, optional TransferDate.

Operational-draft-0.3.0 typed fields, exact tool/revision/allowed field keys,
canonical nonzero reference IDs, exact dates, known statuses, no privilege/credential/
SQL fields, duplicate JSON keys or uncontrolled text. Parsing is not current-record
validation, approval or registration. No model catalog/tool/inference/UI activation.

## Validation

- 77 new service/field tests extend the existing suite to3,183 passed cases.
  Existing admission/prerequisite controller tests are retained unchanged.
- 36CI artifact/workflow guards pass; minimum API floor3,183.
- Native add-on retains all28 enrollment lifecycle SQL/Identity/HTTP cases.
  It adds57 shared-service/native cases, including15 mixed HTTP/service races:
  five last-seat, five duplicate admission and five transfer-versus-create.
  Each uses real SQL, not InMemory concurrency assertions. Required audit fault
  injection covers create/update/both transfer halves for direct and native paths.
- Final window: `6959bfbf18d64ba89ad2117b24d4ae8f`, SQL
  `4ba761839fc44a62bf2d2f6afa362666`; rebuilt harness zero warnings/errors.
  All57 new shared-service/native plus28 retained lifecycle checks PASS.
  Mixed last-seat5/5 and duplicate5/5 races return201/409 with one active row;
  transfer-versus-create5/5 gives one winner/409, one active admission, and a
  losing transfer leaves source Active. No429 accepted as a guarded result.
  Six required-audit fault controls (create/update/transfer, service/native)
  verify no partial admission/source/target survives failure.
- Authoritative controls include same physical SQL/Identity transaction before
  access, current Teacher/anonymous/foreign role/tenant refusals despite claimed
  Owner, stale role membership, native permission refusal, retained FrontDesk
  Manual permission, defaults/nullable dates/reason trimming, current record
  references, duplicate waitlist/activation, capacity and completed prerequisites
  including inactive history. Existing28 lifecycle status/reason/no-write/readback
  assertions are retained.
- API pass was produced by full unfiltered run
  `068e947d688c4618918035698fd48f5a`. Application and relevant test source hashes
  are unchanged by subsequent fixture/runner repairs. Final window reuses its
  exact TRX bytes after checking source/step/counters; failed SQL from that
  earlier window is not accepted. This avoids rerunning unchanged API tests.
- Final read-only qualifier:
  `QA/EVIDENCE/enrollment-shared-qualified-973096375be64b9aaf67878869808f3a/result.json`.
  Reconciles current API/validation source, original/reused TRX/native receipts,
  all36CI guards, exact original service IDs/images/running/health/API200, full
  Docker inventory, owned SQL database/login/container removal, released lease
  and unchanged main. QA/EVIDENCE remains local/untracked; .build-check ignored.
  No frontend/native-browser/all-role/device acceptance is claimed for this facade.
- Earlier window `efc80abaf8984418a5bbde71a22bb5b6` stopped at API validation:
  result-type compatibility failures and three incorrect test fixtures assuming
  Core required a paid module. Corrected the mapping and fixture, preserving
  existing assertions/business policy. No SQL created. Exact original services/
  images/health/inventory restored, failure and all evidence retained. Its source
  pins differ after the correction; it is not a current acceptance record.
- `068e947d688c4618918035698fd48f5a` then passed all3,183API, but its SQL
  stopped at the older lifecycle success fixture's disabled waitlist. Give that
  success fixture WaitlistCapacity3; retain all28 assertions and independent
  zero/full-capacity refusal coverage. This is valid fixture configuration, not
  relaxing the admission rules. Failed SQL19257188455a48aa82c34664cd55627e retained.
- SQL-only successor `cb2d342772a94fc5a7620c149054a057` passed all28 lifecycle
  cases, then its new fixture reused batch names and hit the existing unique
  academy/batch-name index. Add unique synthetic names for independent cases;
  do not alter the constraint/assertions. Failed SQLeb40192e7bc945b198032a37acb5bf75
  retained. For both failed SQL windows, finally captured the retained failed
  container before explicit ownership-verified cleanup. Their original
  inventoryPreserved=false/results stay unchanged; additive cleanup-reconciliation
  records verify subsequent owned-only removal and exact inventory/services.

## Next integration and standing limits

Next: enrollment operational draft/preparation/approval/execute/receipt/recovery
using existing host ledgers; current source and target references/capacity/
prerequisites must be revalidated before execution. Then integrate actual shared
Core conversation/qualified Mini and UI. Do not dispatch these inactive adapters
through the read-only pilot or mark AI-06 complete from service tests alone.

Qualified Mini remains blocked by its independent acceptance (fresh6/12,
exposed7/12 NOT ACCEPTED) and owner's explicit stop before candidate weights
loading. Preserve setup; no load/train/inference/activation/download/paid compute,
commit/push/merge/main/production DB/Azure or broad enterprise issue closure.
Teacher UI evidence remains its historical accepted slice. Other AI cohorts,
all-role/Manual/customer-critical/device and exact release gates remain open.

Route: Sol High for transaction/current authority/conversation integration;
Sol Medium for settled bounded UI work. No automatic model change/project switch.
