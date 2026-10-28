# Cloud/local payroll reconciliation — 29 October 2026

Status: PAYROLL BACKEND SLICE VALIDATED LOCALLY. Wider financial, PENTA and release
integration remain open. No commit, push, merge, production database or Azure use.

## Source and reconciled behavior

Review worktree `D:\AcademyDesk-cloud-sync`, branch `codex/cloud-profile-sync`,
HEAD `10a60eebc2e193b06de31622e897e755062dd72c` plus this uncommitted slice.
The original `D:\AcademyDesk-codex-p0` PENTA/application/test work is preserved.

The owner's cloud policy governs new payouts: deductions must be strictly below
effective gross. Zero/negative net cannot create a new payout. Local protections
are integrated without restoring the superseded new-NoCashDue behavior:

- Exact SQL decimal(18,2) limits, no silent rounding, exact amount response strings.
- Current same-academy, type-consistent Teacher/Staff links. A teacher/student/
  guardian identity cannot be labelled Staff to create, edit or pay a profile.
- One regular statement per worker and normalized period, including alternate
  profiles. Existing recorded worker identity controls history; legacy unverified
  rows block duplicate periods within their original profile without inventing
  cross-profile identity. Additional amounts still require a separate approved
  adjustment workflow, not an ordinary duplicate payout.
- Existing academy-wide transaction application lock plus profile update locks,
  domain/audit transaction, immutable worker/link/time snapshots and UTC readback.
- Teacher payroll reads use recorded teacher ownership and academy scope rather
  than a mutable current profile. Reassignment cannot grant someone another
  teacher's history or remove recorded own-worker history. Unverified legacy
  identity stays with authorized finance review; records are not deleted.
- Historical NoCashDue rows and null payment timestamps remain readable without
  inventing bank payments. This preserves history, not creation of new zero-net
  payouts under the current rule.

Cloud's existing two-column snapshot/backfill migration is unchanged. New
`20261028181901_ReconcilePayrollProvenance` adds only the missing nullable
IdentityRecordedAtUtc/TeacherIdSnapshot/StaffUserIdSnapshot fields and makes
PaidAtUtc nullable for retained history. It neither adds duplicate name/type
columns nor assigns verified provenance to legacy profile-name backfills.
Downgrade checks for null historical payment timestamps before any column drop;
it refuses rather than fabricating dates. No application/customer DB was migrated.
This proves the cloud migration history's upgrade path. Compatibility with an
existing database that already applied the unpublished local payroll migrations
is still a separate reconciliation gate. Do not copy/apply both overlapping
histories or point this review checkout at such a database without that proof.

The existing verified local PKCS10.0.13 servicing pin is retained for net10, with
a loaded-assembly regression. Packages were restored from the existing cache via
an empty offline source. No download/install acquisition or fresh online
dependency-audit claim. Production CI must audit the eventual combined candidate.

## Validation actually completed

- Full unfiltered cloud-based API suite: **1,234 PASS, zero failed/skipped**.
  This extends the cloud1,179 suite by55 cases. The original local3,183 suite is
  preserved separately and still needs its other source/tests integrated; totals
  overlap and must not be added or substituted for overall program acceptance.
- Retained cloud net/history checks keep their status/ledger assertions. Positive
  fixtures now have legitimate worker links. Retained local precision/provenance/
  period cases are adapted to the explicit current policy: historical zero-net
  rows are seeded as history, never created through a new payout request.
- Rebuilt SQL harness with zero warnings/errors. Original payroll/history module
  and pre-column upgrade checks pass, followed by **23new real SQL/HTTP checks**.
- **5/5 deterministic alternate-profile races**: an owned SQL transaction holds
  the exact payroll APPLICATION lock; two runtime requests are observed WAITing
  on that exact resource before release. Every race returns exactly201/409 and
  stores exactly one1000 payout.429 is never accepted as a guarded outcome.
- Two actual required-audit SQL faults roll back payout/profile edits. Invalid
  worker/precision and duplicate-period refusals preserve financial/audit state.
  Teacher own-history/reassignment and legacy/unverified history checks pass.
- Actual downgrade against disposable SQL refuses with51000 and retains both
  applied migration and all payout/provenance data. The original cloud legacy
  upgrade checks retain every scalar of three legacy payouts and their profile.
-84application/7Identity migrations, scoped runtime privileges and run-owned
  database/login/container teardown pass. No other containers are pruned.

Final validation window: `8ec5a8c6b3fb40c98113b1f93d93d927`.
SQL run: `920440a23b5647099200f118d64b338e`.
Source-bound result: `.build-check/payroll-reconcile/8ec5a8c6b3fb40c98113b1f93d93d927/result.json`.
Native result: `QA/EVIDENCE/payroll-reconciliation-sql-920440a23b5647099200f118d64b338e/result.json`.
Original full API TRX: window `0d4bcd61e02e4951810289716ff85d19`; exact API/test
source pins and TRX bytes were verified/reused after harness-only observation
changes. Its failed SQL result is not accepted as new SQL proof.

Original PENTA service IDs/images were restored, API health200 and inference
health passed, complete Docker container/volume/network/image inventory matches
the window's own start. Shared validation lease released. Evidence remains local;
build output ignored. No model inference experiment, activation or weights loading.

## Retained failures

- Preparation340b9e721d9c4b53bd23ff748ff39810 restored offline dependencies, then
  compile caught nullable historical timestamps in the teacher payroll reader.
  Ported only its existing identity-scoped payroll read/DTO; other teacher routes
  remain unchanged. Preparation2eb65b4f8b634467a5c9a46acb0d135a then built/scaffolded
  successfully. Both windows restored exact services and inventory.
- Window0d4bcd61e02e4951810289716ff85d19 passed the full1,234 API suite but failed
  to establish the first strict SQL race criterion. Initial observation depended
  on inner system-procedure sql_handle text. The successor uses the exact held
  APPLICATION resource in dm_tran_locks; all original outcome/waiter assertions
  remain mandatory. Original race statuses were not recorded and are not invented.
  Failed SQLbf6dbe6dcfe64ff88452a3b07b2c7ae0/container retained initially; after
  checking its exact label/name, loopback binding, no mounts and absence from the
  original inventory, only that container was stopped/removed. Original failed
  inventory=false stays unchanged; additive cleanup-reconciliation.json proves
  subsequent restoration. No failed result was rewritten into a pass.

## Remaining scope and next route

Payments/restoration, invoice/adjustment rules and other finance conflicts still
need their reconciled slice. Manual frontend presentation of new provenance,
exact/nullable historical fields and updated lifecycle notices needs its existing
UI integration/browser/device acceptance. This backend slice does not close
BUG-DATA-0003,0009,0056 or the broader release gates.

Then integrate cloud editor/security fixes with PENTA visuals, the preserved
PENTA/security/services/tests and combined source-bound CI. Qualified Mini,
remaining conversational cohorts, all-role/customer/device, privacy/security,
database/restore/staging and explicit release approval remain open. Owner stop
before candidate loading remains binding. Next Sol High for the remaining
financial/authority integration; Sol Medium for settled bounded UI work.
