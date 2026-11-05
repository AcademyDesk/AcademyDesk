# Protected PENTA host integration — 30 October 2026

Status:SOURCE CONTINUATION CHECKPOINT / NOT QUALIFIED. WorktreeD:\AcademyDesk-penta-host; branch
codex/penta-host-integration-20261030; published base64b222db53add47f34cfd4f4bd992ed2919665a7.

## Existing implementation restored

Preserved source6887e6e supplies the PENTA read/search/ledger/budget/private session,
student/teacher draft/approval/receipt/recovery services, guarded controllers,
key-ring setup and retained tests. Existing newer cloud financial and work-item/
security code is kept. Current typed contracts and extracted branch-choice type
are retained; the duplicate historical branch-choice declaration is removed.
Old probe types/tests remain fake-provider contract coverage; its external client
is not registered. No runtime/provider/model activation or application config.

Shared student creation is tightened: Student/Guardian role initialization occurs
only for explicit secure Manual account requests. Credential-free PENTA cannot
bootstrap roles or write Identity. Two added unit cases cover adult/minor inputs;
an added real HTTP/SQL guarded-RoleManager case checks the same boundary. The
existing Manual request/response and optional/guardian rules remain. Extras use
their own synthetic admin so deliberate revocation does not contaminate later
protected-flow fixtures.

## Combined migration

20261030031403_AddCombinedPentaHostPersistence creates12PENTA tables/indexes only;
no existing business table alteration. Snapshot matches the combined model.
Downgrade checks all12tables under transaction-held range locks and throws51028
if any state exists. Empty downgrade removes only these tables. Both contexts
share migration history; the native check partitions declared85app/7Identity IDs,
rejects unknown/changed Identity history, and verifies complete table/schema
fingerprints. Only the cloud84-to85 path is covered, not the older local PENTA
migration history, configured/customer databases or Azure migration.

Teacher snapshots now include actual migrated protected-draft rows. The earlier
absent/present/long-JSON integrity test uses a separate run-owned fixture table,
with an additional assertion that the real mapped table exists. No mapped
application table is created/dropped by that snapshot fixture.

## Current evidence and retained failures

- Prepare196dca506e5c477e8c76ac76d313967f:API compile0warnings/errors and EF scaffold
  PASS; exact original PENTA services restored. EF's existing unrelated decimal
  mapping warnings are retained; no financial precision schema change introduced.
- First APIe9e2c8e3b323499593aec88dafc9cf38:1972passed/2failed/0skipped. Two tests
  expected old separate migration IDs. Updated tests retain protected columns,
  index/schema/no-business-alteration checks against the actual12table migration
  and stronger nonempty rollback protection; no skipped/deleted assertions.
- cc576fb68b674d6bbdcabfae04260abd:full1976API PASS/zero skipped; harness compile
 0warnings/errors. Native migration assertion initially assumed context-private
  history; failure retained. Application and Identity share the history table.
-581b3e386e83444a9b24cfbaadc2e7ff:source-matched1976API reuse; migration4actualSQL
  checks PASS and full foundation/fake-planner-security module PASS. Protected
  student checks progressed through claimed turns/abandonment, then the Python
  bridge script was absent from the new worktree. This is not accepted completion
  of that module. Exact failed owned SQL fixtures/evidence retained stopped.
- Current scripted bridge retest17bd8635db8045fe9e3796e6ce3114bc:REFUSED before
  any test at unchanged3GiB free-RAM guard. Exact original services/images/API200/
  healthy state,complete Docker inventory and released lease PASS. Owner approval
  for one Docker restart is pending; no restart without that approval. Current
  source is published for continuation only, not as fully qualified application.

## Portable paired bridge and CI

Restored QA/tools/penta-private-bridge-fixture.py imports actual Mini/Core
planner/reducer/private transport, but supplies a scripted Provider and synthetic
test-only qualification. It rejects model-runtime imports. C# startup checks the
script/source prerequisites before opening its callback and accepts trusted QA
source/Python path configuration. Local source is pinnedc3c61b9at
D:\PENTA-mini-source-transfer, not weights or an unreviewed model.

CI adds the four host/migration modules and pins paired Mini sourcec3c61b9. Pure
Core dependencies are pinned inrequirements-penta-bridge.txt; no model/trainer
packages. Private checkout may require repository secretPENTA_SOURCE_READ_TOKEN
with read-only access toAcademyDesk/PentaMini. No secret was created, read or
printed; no repository settings changed. Missing access is a failing prerequisite,
not a skipped passing test. Real Mini test flag stays0; fake-planner host security
runs explicitly. Current-source full CI remains unaccepted until actually run.

## Still not a production/90%AI claim

The protected write adapters remain Testing-only with explicit flags. The pilot
is Owner/AcademyAdmin; this does not enable Platform-owner/Teacher/Student AI.
Model interpretation/semantic qualification and broader operational cohorts,
connected PENTA/Manual UI, locked web build/browser/devices, privacy/retention,
legacy migration bridge, backup/restore/staging/critical closure/release remain.
Mini's earlier full cloud CI failed; semantic qualification isNOT ACCEPTED.
Owner's stop before candidate weights loading/training/inference/activation and
paid compute remains. No main merge, Azure/customer DB access or issue closure.
ContinueSol High for this consequential host/authority/migration work; Sol Medium
for settled UI/docs after engineering acceptance. Existing larger local testing
program is preserved; the combined subset does not replace or add to3183tests.
