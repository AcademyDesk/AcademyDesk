# Phase 2B — Bounded staff-role replacement repair

2026-10-03. BUG-SEC-0004 / SECURITY-ROLE-004 / AUTH-API-015. **LOCAL REPAIR VERIFIED; broader identity/session/release acceptance OPEN.** Agreed allocation: Sol High. The existing Astra finding and two retained diagnostics are reused, not audited again.

## Delivered result

The standard staff role PATCH now removes all obsolete mutable/custom memberships, preserves Owner, AcademyAdmin, Student, Guardian and Teacher identities, assigns the canonical requested standard role, checks stamp-update success and returns actual resulting memberships. Independent explicit grants, platform-owner flag, linked person IDs and other account fields are not revoked.

Only [StaffController.UpdateRole](../../apps/api/Controllers/StaffController.cs) changes product behavior. It opts into the existing Identity/domain/success-audit SQL transaction. The existing same-academy administrator with a platform-owner flag bypasses the global filter; this action supplies its own matching-store transaction and attributed success audit. Existing controller owner/admin and tenant guards remain unchanged. Other staff actions, global authentication, permission catalog, custom-role assignment, grant/offboard semantics, frontend and schema are not rewritten.

## Verification actually executed

| Layer | Result |
| --- | --- |
| Build | Final isolated harness build25.19s;0 warnings/0 errors |
| Targeted backend |44/44 PASS:6 new action-boundary tests and38 existing audit/Identity-transaction tests; not a new full backend suite |
| Core native regression |30 selected requests,37/37 strict observations,0 policy gaps,18 unchanged fresh snapshots; actual response/SQL Operations only and fresh-login invoice403 |
| Extended native regression |56/56 cases:21 injected failure/rollback +21 successful retries across Admin, Owner and existing platform bypass;5 individual protected identities,1 combined protected/link/platform case,1 grant-preservation mutation,7 validation/tenant denials |
| Additional positive control |Fresh native login with preserved independent grant reads the actual synthetic invoice200; this is correct access, not obsolete-role leakage |
| Totals, scoped |94 accepted observations:37 core +56 extended +1 grant-read.92 selected HTTP calls including5 extended native logins.46 exact unchanged/rollback fresh-context snapshots; do not combine HTTP requests and observations as independent tests |
| Audits |Core6 correctly attributed successful mutations; each28 extended success adds exactly one correctly attributed academy/actor/route audit. Failed/rejected cases leave complete captured audit rows unchanged |
| Owned native run |e02af722f00146cfb224649b2c7eacbc;SQL50428;wrapper exit0;99.9s |
| Preflight |Native Program/Identity/SQL;313 routes and retained runtime digest;82 domain/7 Identity migrations; scoped runtime login; anonymous/tenant controls |
| Cleanup |Mismatch marker refused; owned database/login/root/container removed; independent inspection finds root/container absent and listeners0 |

Failure injection uses the real Identity stores and SQL. Returned Identity validation failures at removal, assignment and stamp update plus actual SQL THROW at membership deletion/insertion, stamp persistence and audit insertion are each reached exactly once for all three authorized actor paths. Whole fresh ordered snapshots of users, roles, memberships, grants, academies, students, teachers, guardians, invoices and audit rows are identical after every failure. Successful retry uses the normal, uninjected host; roles and stamp then persist with one audit. Secrets are compared in memory, never printed; evidence contains snapshot/body digests.

The core strict successor accepts the exact original two failing expectations and retains the positive live-permission/grant/offboard controls. Old bearer/refresh401, Operations batches200, wrong-tenant403, live grant revoke403 and offboard403/401 remain accepted. All five protected identity memberships and typed links survive; response equals fresh SQL. Case-insensitive role input canonicalizes to Operations. Explicit grant rows remain identical and their permissions remain effective after fresh login.

## Evidence continuity and disclosed limitations

Before product work, the diagnostic package was sealed:1003 immutable predecessor pins passed. That historical package still records70 accepted controls and4 failure observations across two runs, not74 passes. Its first consistency attempt used an incorrect cleanup property name and failed; the retained final log passes after fixing only the validator. Two earlier read-only receipt-extraction syntax attempts changed no files. An initial matrix patch failed before changes; it was retried using UTF-8 source.

The repair's initial build had one QA-only compile error:the fixture used the wrong access-grant class name. The actual existing AccessGrant type was used, and final build/native checks pass. Initial and final build logs are retained, not overwritten. No runtime attempt failed or was hidden.

Repair before manifests retain eight recoverable pre-edit source/tracker/assessment snapshots.1024 predecessor pins resolve;1019 immutable predecessors remain unchanged, with five declared pinned QA/tracker successors. The new product file baseline was separately backed up. Earlier native/browsers/full-critical/Astra audits are not rerun or relabelled.

This proves the bounded standard replacement defect and selected rollback/protection/grant paths. It does not certify concurrent competing role edits, cross-database stores, grant expiry/module-off policy, every role-sensitive endpoint, custom assignment's broader identity behavior, browser recovery after changing the signed-in actor,2FA/lockout, Android/iOS hardware, staging or Azure. Protected accounts legitimately retain their independent permissions; changing their staff role is not de-admin/offboarding. The existing platform bypass is not newly granted to foreign tenants.

Local normal dev data/services, previous retained fixtures, HEAD and Azure unchanged. No commit/push/deploy, migration or AI implementation. Phase2B and release remain OPEN. Remediation register moves this one group from OPEN to PARTIALLY FIXED:56 bounded groups,58 open,2 unable to verify;116 distinct groups,0 full closures.

## Evidence

- [Native log](../EVIDENCE/logs/phase-2b-role-replacement-sql.log), [parsed observations](../EVIDENCE/role-replacement-observations.json), [independent cleanup](../EVIDENCE/role-replacement-cleanup.json).
- [Initial build](../EVIDENCE/logs/phase-2b-role-replacement-build.log), [final build](../EVIDENCE/logs/phase-2b-role-replacement-build-final.log), [44 targeted tests](../EVIDENCE/logs/phase-2b-role-replacement-tests.log).
- [Repair before manifest](../EVIDENCE/role-replacement-before.json), [source receipt](../EVIDENCE/role-replacement-source-receipt.json), [consistency validator](../tools/validate-role-replacement.cjs).
- [Retained diagnostic](PHASE_2B_ROLE_REVOCATION_DIAGNOSTIC.md), [sealed diagnostic consistency](../EVIDENCE/logs/phase-2b-role-revocation-consistency-final.log), [original issue with successor](../ISSUES/BUG-SEC-0004.md).

## Next safe task and model

The approved prerequisite is delivered. Next is a **bounded Phase1 AI foundation design**, not live AI execution:tenant/identity context, deny-default tool contracts, approval/execution binding, audit/usage/idempotency invariants and manual My Academy fallback. Recommend **Astra High** for this new consequential authority boundary, then **Sol High** to implement the accepted disabled foundation. Reuse the readiness proposal and existing ERP/filter/transaction code; do not restart the Enterprise audit. Terra can handle settled routine UI/docs; no lower-model delegation of the security design. No enabled AI writes or Azure changes are authorized by this repair.
