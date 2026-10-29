# Admission capacity repair — local development checkpoint

## Final scoped outcome — PASS, not release approval

- [x]Original full-batch activation409/no-write;145native checks/15barrier pairs/
  five audit rollback-recovery controls on the fresh development image.
- [x]Full API2879/2879zero failed/skipped;150QA-tool tests PASS. Focused113
  C# cases are included in the full suite, not extra unique product scenarios.
- [x]Affected native enrollment36setup/security checks/four real390/1440light/
  dark browser journeys PASS. Actual201save/409duplicate/200pause/200resume,
  no429/page errors/overflow, pending/cancel/reason guards and SQL/audits exact.
- [x]Actual image65published files exactly match all local-publish hashes.
- [x]All accepted windows restored exact original services/images healthy,
  preserved inventories and removed only owned synthetic resources/leases.
- [ ]Standard source-Dockerfile release build, other admission rules/devices/
  full enterprise/security/AI/production gates remain OPEN. Issue NOT CLOSED.

Current-image browser SQL `3a33f94ffc44417b9b3a27de8c3b8a7f`, window
`7d1a89dbd24b400a8f4ed36d06a3ee64`, both PASS. Four PNGs retained;390dark
inspected and readable. This is real authentication/SQL/native API, not mocks;
it is still desktop-browser emulation, not physicalAndroid/iOS acceptance.
Five explicit audit faults deliberately returned500 and are accounted separately.

Read-only strict qualification (also checks full-suite TRX/build/actual-image
manifest, source identity, guards, exact window linkage and four PNG hashes):

```powershell
node QA/tools/admission-repair-qualification.cjs dce68fefd4ac413e98e81ab1acd638d0 3d80147374d64793a020b9d3d3c79b74 3a33f94ffc44417b9b3a27de8c3b8a7f 7d1a89dbd24b400a8f4ed36d06a3ee64
```

SHA256: repair SQL `e21ca2bcb4fadba2233a0ba082a03f9c5b60c2197900363100ca4910f57bf511`;
SQL window `6e6d9608d104681ed20d6c25bbe1dc45b03303fd4a00bd70690cca30e8e0c770`;
browser SQL `b8cfea8bf5341224d567230f2d577cd2b58be9ff704b5a909e0f24c633db01ed`;
browser window `db6e51d2c474ddd8210113073d65f162aa8097bb0a6e6787d0308e8aeb4f8e1a`.
Build manifest receipt `f565c78c302866126c1e5d8af1d02e5cbb36db8ee5e2c9a41715728bbd783d33`.
Four original finance regression/runner hashes unchanged. Staging empty;
QA/EVIDENCE local, .build-check ignored, main tracked clean/Mini HEAD unchanged.
No active owned validation. New intended development image retained (11images,
not the historical10); original29containers/41volumes/four networks preserved.

23October2026, High, `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`,
unchanged HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved work.
Continues BUG-DATA-0006/E1/E9; no new testing strategy or issue closure.

## Implemented boundary

`EnrollmentAdmissionBoundary` uses a parameterized transaction-owned SQL Server
application lock keyed by authoritative academy ID. Enrollment Create, Update,
Transfer, promotion Decide and batch Update acquire the same lock **before**
their relevant reads. Academy-wide scope avoids inverse transfer source/target
lock order and also serializes capacity reductions. Lock timeout refuses503,
not unlocked execution. Non-SQL relational providers fail closed; InMemory units
are not concurrency proof. This helper does not grant permissions.

Normal academy actions opt into the existing domain save/required-audit boundary,
so the admission lock lasts through commit and an audit failure rolls back the
domain write. Platform-owner bypass keeps its prior audit policy, with an owned
SQL transaction when there is no host transaction. Promotion's existing terminal
decision/per-promotion lock remains intact, acquired after the shared lock.

Activation from non-Active now rejects closed/inactive targets, capacity overflow
and another Active enrollment for the same student/batch. Known lifecycle status
spellings normalize to canonical values. Already-Active reaffirmation does not
consume a second place. Promotion approval also checks target active/open/capacity.
Existing Create/Transfer capacity checks now serialize with those operations.

No migration, new authorization, UI/model/tool change, prerequisite rule or
waitlist/transfer-history policy is introduced. Other BUG-DATA-0006 parts remain
OPEN: this is not a full shared admission-rule implementation or issue closure.
Only four API source paths differ from the prior483/482source image:
EnrollmentsController, BatchPromotionsController, BatchesController and the new
EnrollmentAdmissionBoundary. No pre-existing application work was overwritten.

## Build and test provenance

113focused C# cases PASS, including18new admission cases and95retained promotion/
batch/audit cases. API/test compilation successful, no failed/skipped cases.
Actual TRX is in build window `7e4f0ea8a5f44422982613a19ceb0d65`.

Development image build `3798bb0533ff4ba9a57ee3f3bafad484`, image
`sha256:c1f777063d6a773b3c80875bc56ae00f1205e342d8e2ff6ebd9aaf09f08810c2`:
offline local SDK10.0.401 Release publish, no restore/download,65published file
hashes/93,731,915bytes,483API source pins. Published app/config screened with the
existing artifact heuristic; runtime uses cached ASP.NET image
`sha256:8af87eba5e7cdafb1514d7388be287365da97c132509e84dfe252ffd552398ca`.
This is explicitly **supplementary DEVELOPMENT image proof, not the standard
application source-Dockerfile/release build** or whole-image security approval.
Build window PASS, originals restored healthy, original inventories retained
and exactly the intended labelled new image added; exclusive lease released.

Failed attempts retained:

- `374fa688eb9b4739afae679d240c343e` memory guard refused before compilation,
  originals/inventory restored. Genuine memory recovery permitted a later run.
- `92e27e6f5f8c4e6bacbf98a1be9e262d`:113tests passed; standard Dockerfile build
  `d999714ce0274d86a3b51e4c174d83bb` failed NU1301 at restore with networknone.
  No network-enable retry, project/dependency edits or secret acquisition.
- `efda69eee1c14d03a4b4a30995d5b32b`:local publish passed; supplementary image
  `57b20d19f4454e4c84b844b46ae6c1a1` failed because Docker interpreted an image
  ID as an external repository in FROM. Corrected to the existing cached runtime
  reference, whose exact ID is checked first; no base pull or package download.
- First native repair SQL `dc3e16541b7d475db4edd9db5b1735c7`, window
  `d0d2d5d57fbf45a3bafd3d3a7af6c6a8`:sequential409/no-change and first9races
  passed, then actual promotion setup403 because the synthetic academy lacked
  AcademicGovernance. First FAIL retained; exact cleanup/restoration/inventory
  PASS. Corrected only the disposable academy's module fixture, not authorization.

## Native SQL and regression details — completed

Corrected SQL `dce68fefd4ac413e98e81ab1acd638d0`:145ordinary checks,
15barrier pairs and five audit rollback/recovery controls PASS; owned SQL/API/
network cleanup and child inventory PASS. Independent narrow receipt oracle
accepts the actual assertions. Window `3d80147374d64793a020b9d3d3c79b74` PASS,
including exact originals healthy/full inventory/owned cleanup/lease release.
Verified:sequential full-batch409/no-change;
15barrier pairs across Create/activation, activation/activation, transfer/
activation, promotion/activation and capacity-edit/activation. Exactly one write/
audit wins, active count never exceeds committed capacity, source/promotion state
matches the winner. Five real required-audit DENY INSERT500/no-write controls,
then recovery/one audit. Native acceptance here is narrow development proof.
Affected native enrollment browser PASS as recorded in the final outcome above.
Full API suite PASS2879/
2879, zero failed/skipped (18seconds test duration), regression window
`273aa93c4dc9472a9c3a87741e7133c6`. Published actual-image artifact inspection
`161592085b01429f8242183e0b0f24e0` PASS65files/93,731,915bytes, all path/byte/
SHA256 entries exactly equal the offline published manifest. Inspection container
never started and was removed. Not a whole-image/OS vulnerability scan or the
standard Dockerfile build. Regression window final restoration/inventory/lease
PASS before the browser window began; no overlapping service windows.
Full-suite window `4d8b9a17f1c846fd99337de6b50d3d7a` refused1.476GiB before
test execution; not a suite failure or pass. Its exact original restoration and
inventory PASS; retained as refused. Subsequent windows wait up to30seconds
for genuine post-stop/cache recovery, retaining the unchanged3GiB guard.

Observed pair orders, three per group:

| Left action / right activation | Actual statuses |
| --- | --- |
| Create |409/200,409/200,409/200|
| Activation |200/409,200/409,200/409|
| Transfer |409/200,200/409,200/409|
| Promotion |409/200,200/409,200/409|
| Capacity reduction |400/200,200/409,200/409|

Exactly one mutation/audit per pair. Capacity-edit winner may leave capacity2/
active2 or capacity1/active1; the test does not insist on a preferred winner.
Required-audit snapshots cover Batches/Enrollments/BatchPromotions/AuditLogs/
AspNetUsers, not every application table or production audit operation.

The old19qualified receipts remain historical on their original API source.
Their strict current-source qualifier correctly rejects the changed application;
do not weaken it or relabel those results as new-image proof. The new narrow
development qualifier is separate and preserves standard-Dockerfile gate OPEN.

All11broader packages/HIGH braces/Mini semantic/full90%AI/physical devices/
qualified reviews/operations/staging/release remain OPEN. No main/Mini changes,
reset/clean/prune, model training/activation, commit/push, production DB or Azure.
Stay High for this consequential integration; Medium only for settled visuals.
