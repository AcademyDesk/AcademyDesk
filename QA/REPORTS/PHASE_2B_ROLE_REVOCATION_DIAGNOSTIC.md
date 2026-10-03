# Phase 2B — Existing-session role/permission diagnostic

2026-10-02. **RUNTIME-REPRODUCED / OPEN — not fixed.** Existing Astra finding BUG-SEC-0004, SECURITY-ROLE-004; related SECURITY-ROLE-001, SECURITY-SESSION-001 and AUTH-API-015. This is a bounded native test of accepted audit gaps, not a repeat of Phase 1 or an authentication rewrite. Agreed allocation: Sol High.

## Outcome

Two distinct gaps in the **same existing staff replacement defect**, identical on two fresh owned SQL runs:

| Check | Intended replacement behavior | Both native runs |
| --- | --- | --- |
| Response vs SQL roles | Disclose actual resulting role memberships | PATCH200 reports only Operations; fresh SQL retains Operations,QA-Finance,QA-Messages |
| Finance after fresh login | Operations-only/no grants cannot read Finance | Native fresh login200, then invoice GET200 exposes the one synthetic invoice1234, expected403 |

Old credentials are **not** the cause: the standard-role action rotates the stamp, and original access/refresh are correctly401. The user simply signs in again and regains obsolete custom permissions because their database memberships were never removed. No cross-tenant disclosure or weakness in the accepted authentication repair is claimed.

The actual custom-role assignment endpoint, when assigned Operations, removes the custom memberships. Its stamp is unchanged in these observations; the **same existing** bearer then loses finance access403 while retaining Operations batch access200. Live grant add/revoke similarly allows200 then denies403 using that bearer. Offboard rejects its existing access403 and refresh/login401. These positive controls show why the eventual repair should address membership replacement/accurate response, not broaden the authentication middleware or revoke all sessions indiscriminately.

## Executed evidence

| Layer | Actual result |
| --- | --- |
| Build | Isolated `.build-check/role-revocation-sql`,33.15s,0 warnings/0 errors; accepted normal/previous binaries not overwritten |
| First run |82769cb38adc4ca7bd8b57c2da71725c; SQL63379;28.4s |
| Second fresh run |41ddf94afb434583bed063f8da6ccb12; SQL63394;19.8s |
| Per run |30 selected native HTTP requests;37 observations:35 accepted controls,2 captured failing policy observations;18 unchanged fresh-context snapshots;6 exactly attributed success audits |
| Combined |60 selected requests;70 accepted controls and4 failure observations representing the same2 distinct gaps, not74 passing tests |
| Preflight | Real Program/Identity/SQL,313 runtime routes, tenant/anonymous/active positive controls; both migrations82/7 and scoped runtime login verified |
| Cleanup | Each wrapper exit0 is successful **diagnostic completion**, not policy acceptance. Ownership-negative cleanup refuses mismatch; exact run-owned database/login/root/container removed, both SQL listeners0. Previous fixtures and normal dev services retained |

No mock authentication, client token fabrication or browser storage injection. Synthetic staff initially has Sales + two custom roles, one granting finance.manage. AcademyA has relevant modules enabled and exactly one real synthetic invoice, so finance200 cannot be mistaken for missing fixture/empty-data permission proof. AcademyAdminA/B and Teacher use normal native login.

Read/denial snapshots include complete fresh ordered user/role/membership/grant/invoice/audit rows; no raw credential material is printed. Invalid/unknown role, missing staff, wrong-tenant admin, self/nonadmin and Teacher role-change requests reject400/404/403 without changes. Cross-tenant finance403, unrelated admin finance200, exact Operations membership after custom assignment, attributed grant revocation and final inactive/locked staff/invoice preservation are independently checked. All six successful selected management mutations (standard replacement, custom assignment, grant create, revoke, repeated revoke, offboard) have the correct academy/actor audit; repeated revoke retains its existing success-audit behavior and is **not** a no-write HTTP assertion.

## Product and scope boundaries

No product/frontend/API/schema/security-policy changes. QA-only partial module plus one additive dispatcher switch and wrapper selector/output prefix.1007 predecessor pins checked;1003 immutable entries preserved, four declared pinned QA/tracker successors; BUG-SEC-0004 has an additional recoverable historical snapshot. Pre-edit backups were captured before changes; aggregate manifest assembled after QA compilation. A read-only extraction command had a syntax typo before the dispatcher edit; no failed build or runtime attempt was accepted/hidden.

No backend/full critical/browser/device/stress suite rerun. These cases do not certify every endpoint, protected Owner/Admin/Student/Guardian/Teacher replacement, multiple-tenant custom-role policy, grant expiry/module-off, concurrent updates, rollback/faults, role-claims-only routes,2FA/lockout policy or release. The original audit remains accepted; this new runtime evidence changes the finding from proposed reproduction to reproduced. Phase2B/release remain OPEN.

## Evidence

- [First native log](../EVIDENCE/logs/phase-2b-role-revocation-sql.log), [second native log](../EVIDENCE/logs/phase-2b-role-revocation-sql-second.log), [parsed observations](../EVIDENCE/role-revocation-observations.json), [build](../EVIDENCE/logs/phase-2b-role-revocation-build.log).
- [Before manifest/snapshots](../EVIDENCE/role-revocation-before.json), [source receipt](../EVIDENCE/role-revocation-source-receipt.json), [owned cleanup](../EVIDENCE/role-revocation-cleanup.json), [consistency validator](../tools/validate-role-revocation.cjs).
- [Existing issue](../ISSUES/BUG-SEC-0004.md), [new QA diagnostic](../tools/SqlHarness/RoleRevocationDiagnostic.cs).

HEAD20bb6047f9edf733ac8e2a226621cc582ec54b3c, local dev database/services and Azure unchanged; no commit/push/deploy. Only synthetic owned QA resources were removed; logs, source snapshots and isolated binaries are retained.

Next bounded fix: the standard staff role-replacement action must remove obsolete mutable/custom memberships and disclose actual resulting roles, preserve protected identity roles and independent explicit grants, rotate the stamp, and make failures rollback-safe. Retest this exact native reproduction plus protected-role/grant/rollback/no-write controls; keep broader gates open. Continue agreed Sol High. Do not rerun accepted Astra audit; only an unresolved policy exception warrants a narrow review.
