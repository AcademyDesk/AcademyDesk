# Staff action save/audit boundary — 25 October 2026

Existing BUG-API-0002/E1/E9 continuation. Preserve the qualified staff role-
replacement repair and current grant/custom-role/payroll/Access Review packets.
This slice covers Create, ResetPassword, UpdateStatus and Offboard only, including
an existing same-academy Owner whose server-side platform flag is set inside the
exact run-owned synthetic fixture. No production identity, role or PENTA tool.

## Required evidence

- Reproduce ordinary Owner audit INSERT failures and the existing platform-owner
  filter bypass against the current immutable development API before changing code.
- Each repaired audit fault must return500with all11domain/Identity/audit table
  snapshots unchanged. Original target access/hash/activity/lockout must remain
  unchanged; password/status/offboard faults must preserve the previously valid
  target bearer. Baseline instead documents unsafe persistence/unaudited success.
- Two actual synthetic Operations targets; normal login, four actions per actor,
  explicit recovery and ten attributed recovery audits after repair. The extra
  normal reactivation is necessary to test a still-valid session before offboard.
- Teacher create and foreign Owner status-write refusals preserve all tables.
  No429, no unrelated writes, no error swallowing. Exactly owned audit denial
  and Owner platform flag restored in finally; full container/service/inventory
  restoration and source pins required.
- Any app repair reuses the existing explicit shared Identity/domain transaction.
  No broader staff-role policy, concurrent-operation or whole-identity closure.

## Current state

- [x]QA fault/proof negative controls and existing SQL transport tests9/9PASS.
- [x]Baseline SQL `18d235b257fe4155b312aaa61eeb390b`, native window
  `054431331aa2455090e7f97c7942a4e1` PASS54ordinary HTTP checks/eight unsafe
  outcomes on API `b9d447d5eba74a19a8ef22a17de66628`. Ordinary Owner four500
  responses persist unaudited identity changes. Same-academy platform-flag Owner
  returns201/200for the same changes with no success audit at all, including on
  normal recovery. Target password/session401and status/offboard403prove real
  consequences; unrelated tables unchanged, five ordinary recovery audits.
  Exact owned cleanup/original healthy services/full inventory/lease PASS.
  Baseline reproduction is not protection acceptance.
- [x]Minimal four existing `AtomicAcademyMutation(IncludeIdentity = true,
  IncludePlatformOwner = true)` markers added only to these actions. Action
  bodies/IsOwner/standard role-replacement safeguards unchanged. Original
  StaffController SHA256 before markers:
  `0c7d21f9477211b84e2817f6b1f1bfc77cbb10bf01c24152aada9a5a7eb7ffbe`.
  Fifteen action-boundary/Identity/platform positive/read-negative tests added.
- [ ]Full API regression/fresh offline development image: first window
  `5c23558ce5ad41fe98ab4c35df3e792e` retained FAIL2967PASS/four historical
  StaffRoleBoundary assertions expected these newly repaired actions not to opt in.
  No image created. Exact original healthy services/full inventory/lease PASS.
  Updated those six-case boundary expectations to require the new four actions'
  explicit Identity/platform protection, retain UpdateRole's original explicit
  Identity protection and default platform bypass, and keep List excluded. Added
  a platform assertion to every case; no test removed or business policy relaxed.
  Corrected window `272f62a5cf6b48a686fbd2f6ea961a24` full2971/2971API tests
  PASS/zero skips/failures; offline image
  `ee9df6365eba476b973a940a45f02b8c`,
  `sha256:34ae1c8c0273201e5e67284021aa40b034f9f861ea39efc01bd93b0ace6714af`
  PASS/source484pins/no network downloads. Build window PASS/exact originals
  healthy/full inventory preserved with only the intended owned image added.
  Repaired native window `cc469949284d4df0a06d4a32004779e6` PASS.
- [ ]Issue/device/source-Dockerfile/release acceptance remain OPEN.

Use the existing approved two-service stop/restore windows, exclusive lease,
unchanged3GiB guard and no downloads/restart/prune/main/Mini/Azure/customer data.
High recommended for identity/security/native transaction work. Application
Only the four markers change StaffController after completed reproduction.

## Qualified repaired result

SQL `0f2bef2c48004bd19cb5304b460d6049` PASS54ordinary HTTP checks, plus
eight actual audit-fault controls: Create/password/status/offboard for Owner and
same-academy platform-flag Owner. Each fault now returns500with all11table
snapshots unchanged, zero success audit, no partial role/account save. Password
fingerprint, activity/lockout and original target session200are preserved on
failed changes. No429/unexpected5xx; teacher/foreign writes remain403with no
changed snapshot. Normal recovery for each actor produces exactly five success
audits: creation, password reset, deactivate/reactivate and offboard. Final
normal offboarding really disables/locks the targets and invalidates their tokens.
The synthetic Owner platform flag and exact audit permission fault are restored.

Final native parent PASS: exact original PENTA container IDs/images healthy,
complete pre/post Docker inventory identical, only owned SQL/API/network removed,
lease released. `staff-audit-qualification.cjs` independently binds the original
54check/eight-unsafe baseline,2971test TRX/full offline build,484source pins,
only-four-markers reversible file hash and repaired native pair. No old grant,
payroll or Access Review receipt is relabelled as a fresh whole-current-image run.

Guard SQL receipt SHA256:
`375fbabb379e7bdb95703141163431c26da4dd1e647933117f36ab71449fd1bc`.
Parent receipt SHA256:
`ca6319ba89b2fd0718bed00150f4ddbf6821d07117b691506677ab27edd9e98a`.
The65file/93,741,059byte manifest is published build evidence; this latest staff
image has not received a separate actual-image payload extraction. Earlier actual
image comparisons remain bound to their prior images. Standard source-Dockerfile
release validation remains OPEN.

Limits retained: arbitrary provider/IdentityResult failures, concurrent status/
password/offboard/protected-role policy, broader platform/cross-store/provisioning,
browser/physical devices and release are not closed by this audit-insert fault
slice. In particular, existing ignored UpdateStatus IdentityResult behavior is
not claimed repaired by adding the transaction marker. BUG-API-0002 remains OPEN.
