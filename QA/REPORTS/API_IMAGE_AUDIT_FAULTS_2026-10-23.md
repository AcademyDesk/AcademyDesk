# Native required-audit rollback and recovery — 23 October 2026

Status: **ACCEPTED BOUNDED PASS —42ordinary checks +5expected500 faults +5positive
recovery controls; linked window exit0/exact restoration/full inventory PASS.**
This is one release-gate slice, not enterprise or production approval.

SQL run `acc60573df364b189dd871b62936a466`, window
`d576b3eac4d74cfb82c2948f042af0b7`. Accepted API build
`0a5f8d2c708a40a994dd97e9f6d12425`, image
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`.
No source/package/configuration-rule change.109migration histories match current
source. SQL/API network private, no public ports or external inference. Memory
admission3.305GiB; no lowered guard, Docker restart or image download.

## Real failure and recovery

The existing AuditSaveRegression permission-fault technique is applied to the
exact run-owned runtime login/database only, after label/image/database checks.
`DENY INSERT` on dbo.AuditLogs is revoked in `finally`, including failure paths.
No app-wide audit bypass, mocked persistence or production permission change.

| Operation | Audit insert denied | After permission restored |
| --- | --- | --- |
| Payment create |500; protected SQL unchanged |201; exactly one success audit |
| Reduction approval |500; protected SQL unchanged |200; exactly one success audit |
| Reconciled payment void |500; protected SQL unchanged |200; exactly one success audit |
| Payroll payout |500; protected SQL unchanged |201; exactly one success audit |
| Student onboarding + new Identity account |500; protected SQL unchanged |200; exactly one success audit and verified Student/user/role link |

Each failure compares full-row SHA256 snapshots of twelve tables:Students,
Teachers,Guardians,StudentGuardians,Invoices,Payments,FinanceAdjustments,
PayrollProfiles,PayrollPayouts,AuditLogs,AspNetUsers,AspNetUserRoles. Five failures
and the aggregate fault window leave these snapshots unchanged. This scope is
not every application/Identity table or every operation. Raw HTTP bodies,
passwords, tokens and Identity values are not retained; only metadata/hash
comparisons and explicit invariants. Generated account password stays in memory.

After removing the fault, all five real operations succeed with exactly one
required success audit each. The onboarding response confirms account creation;
an independent SQL join verifies one matching same-academy student/Identity/Student
role link. Final count2academies/6identities/2payments. Existing tenant/teacher/
student financial restrictions and deactivation checks remain retained.

Run passed with sourceUnchangedtrue,ownedResourcesRemovedtrue,inventoryPreservedtrue;
only its synthetic API/SQL/network removed. Three portable tests exercise wrong
status/partial writes, mandatory permission recovery, complete positive controls,
linked identity and no credential retention. Pinned read-only qualification has
negative fixtures for missing faults, false successes, residual writes, missing
permission restoration, duplicate audits and missing linked identity.

Windowd576b3eac4d74cfb82c2948f042af0b7passedtrue,runtimeExit0,
restorationPassedtrue,inventoryPreservedtrue. Exact original container IDs/images
restored,API running,inference healthy;29containers41volumes and current networks/
images match its admission snapshot. The earlier interrupted window's old-bridge
ID discrepancy remains retained separately, not rewritten by this new window.

## Limits and next gate

This is native failure/transaction proof for five existing domain actions, not
PENTA model qualification, live browser uncertain-outcome acceptance, every audit
failure mode, all SQL/Identity tables or production security certification. It does
not close existing P0issues. Physical devices, complete critical workflows,
production least privilege/storage/monitoring, full restore/rollback, independent
model/data/privacy reviews and staging/release approval remain OPEN. Continue
the existing financial transition matrix and four-role Manual fallback browser.
No main/Mini edit,commit,push,Azure or production database operation.
