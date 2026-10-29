# PENTA shared teacher-creation integration — 29 October 2026

Status: SHARED TEACHER DOMAIN/ADAPTER SLICE VALIDATED LOCALLY.
Owner approved this feature-branch commit/push. Verify its remote SHA;
current-source GitHub CI and broader release approval remain separate.

Worktree `D:\AcademyDesk-penta-teacher`, branch
`codex/penta-teacher-service-integration-20261029`, base
`4e707629f23a9ef14a92186890fcf85b65c7d85b`.

## Integrated behavior

Carry forward existing TeacherCreationService, typed teacher field adapter and
their tests from published source6887e6e. Source files are copied byte-for-byte;
the existing branch-choice descriptor is extracted into its own inert record so
it does not import the unintegrated protected conversation/persistence system.
Remove that duplicate declaration from the later CoreContext import, not from
the already integrated descriptor. No field contract is changed by extraction.

Manual POST teachers now uses the same domain service intended for later
protected PENTA calls. The service independently validates current stored user,
role/academy/module and branch authority, requires one SQL/Identity transaction
before relational access, preserves existing optional/null/metadata behavior and
returns authoritative results. Current platform owners retain cross-academy
creation; their opted-in create/audit transaction is now required too. Approval,
duplicates, replay and durable conversation receipts remain the future protected
caller's responsibility; a successful service result is not a chat receipt.

Only the shared service is registered. No teacher model tool, interpreter,
conversation endpoint, protected-draft migration or AI UI is activated. Complex
metadata and credentials remain outside the narrower inactive typed adapter.

Cloud teacher UPDATE, linked-account handling and request contracts are unchanged
after the existing HttpPut boundary, verified against4e70762. Payroll, payments,
invoice reductions, DbContext and migration snapshot are unchanged. Existing
enrollment integration remains. No package/model download, customer DB, main
merge, model loading/training/inference experiment/activation or Azure use.

## Validation and retained failure

- Full unfiltered API **1,564passed, zero failed/skipped**.49 retained/new boundary
  cases over1,515: existing teacher service/typed-field tests and stronger explicit
  action/Identity/platform-audit boundary checks. No overlapping total is added
  to the preserved local3,183 history.
- First windowf37c7fb347b7436fbb70c032d284774f had one failing old test expectation
  that teacher creation did not enlist Identity. The integrated domain operation
  deliberately requires current Identity authority through its transaction.
  Updated that expectation to true and added action-scoped platform-audit checks;
  no assertion was deleted or converted into a skip. No SQL started. Original
  failure and exact original service/health/inventory restoration are retained.
- Window5a1ea15c667943898698989a0e418252 passes full1,564API plus15retained actual
  SQL/HTTP/shared-service cases. Anonymous/teacher/foreign/stale-role denials,
  shared transaction requirement, optional fields, metadata/null roundtrip,
  platform cross-academy one-audit creation, real mandatory-audit rollback,
  explicit typed candidate/domain call and no account creation pass. SQL run
  93de6aca1cbe4e45bd6a35a6d2325d8d; cloud84application/7Identity migrations and
  owned cleanup pass. Exact original services/images/API200/full inventory restored.
- Harness-only successor23e263e73ce04192874fb55ac174be97 failed compilation
  because the new long-row regression reused an enclosing local variable name.
  Renamed only that fixture-local variable; no case/assertion was removed. No SQL
  started in that failed window; exact original service/inventory restoration
  passed. Its failed receipt remains unchanged.
- Final successor0fe1242e875d4264900fd701473c58a0 rebuilds/reruns the corrected
  SQL harness. Full1,564API TRX is reused only after exact unchanged application/
  test source hashes match5a1ea15c667943898698989a0e418252. Actual SQL run
  c85148fed94f4420bfa0149614eeafac passes16cases: all preceding15 plus the real
  optional-table/full-long-row/tail-change regression.84application/7Identity
  migrations, scoped login, owned DB/login/container cleanup pass. Final exact source/fixture pins, original two service IDs/images, health/API200,
  complete Docker inventory restoration and released lease PASS; no old-harness
  proof is relabelled as a newly executed final-harness run.
- Local evidence/build receipts remain excluded from publication. Current-source
  GitHub CI and broader browser/device/critical closure are not established here.

## SQL fixture adaptation

The prior source's no-side-effect snapshot queried a mapped protected-draft
table, which this cloud-based slice intentionally has not migrated yet. The
fixture now queries actual SQL table existence; when present it reads every row
using one nvarchar(max) JSON result, never an invented empty collection. Every
original snapshot comparison and all15original case assertions remain.

The extra case creates a minimal synthetic table only in the validated disposable
run-owned DB after proving it was absent, inserts a12,000-character synthetic
value, verifies complete readback and a tail-only change, then removes only that
owned fixture and verifies absence again. This is a test of capture integrity,
not the actual protected-draft migration or application persistence acceptance.

## Remaining work and continuation

Read WORK_LAPTOP_START.md and CLOUD_INTEGRATION_TRACKER.md. Next integrate the
existing shared student-onboarding service and current Identity/minor/guardian/
optional rules, then protected student/teacher sessions/drafts/authority/approval/
receipt/recovery and a qualified cloud/local migration path. Continue connected
PENTA/Manual frontend and remaining conversational cohorts. Preserve current
cloud fixes, both source trees and all tests; do not merge preservation snapshots
or overlapping payroll migrations wholesale.

Mini cloud-CI portability is a recorded failure, not a passed prerequisite.
Real Mini quality remains NOT ACCEPTED and owner stop before candidate loading
stands. All P0/critical/all-role/device/privacy/security/source-bound CI/database
restore/rollback/staging/release gates remain OPEN. Sol High for consequential
host integration; Sol Medium for settled bounded UI/docs.
