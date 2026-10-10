# Guardian explicit revocation and child-list SQL repair — 2026-10-10

E0 rank2: [BUG-SEC-0006](../ISSUES/BUG-SEC-0006.md), SECURITY-GUARDIAN-003. Newly discovered [BUG-API-0009](../ISSUES/BUG-API-0009.md), GUARDIAN-CHILDREN-SQL-001. **Both OPEN; bounded repair verified, not full enterprise acceptance.** Worktree `D:\AcademyDesk-codex-p0`, `codex/penta-search`, starting `450249938aa61a9aa6e68df13bf55ca401d63d76`.

## Reproduce before application changes

Existing guarded SQL runner extended with `GuardianRevocation`, real Identity tokens/HTTP pipeline, both migration sets and isolated upload/storage. Fresh labelled loopback SQL only, server/DB/run-marker/runtime-privilege checks unchanged. No customer/development/Azure database, production credentials, model/inference or external delivery.

Initial run `5393c389e5764e5a9a5b0d66cb70764e` failed the active guardian child-list200 control: actual500. Explicit diagnostic run `bce44b1494ef485f83b7b620faf06dba` then classified11 cases, without application changes:

- Minor initially readable with existing guardian token.
- Admin PATCH explicit `allowPortalAccess=false`, all four subpermissionsfalse, returned200; fresh grant stilltrue, revoked timestampnull, grant timestamprenewed. Guardian child-detail and Me continued exposing that child. Only the four subpermissions becamefalse.
- Admin PATCH `allowPortalAccess=false` with subpermissions omitted returned200; fresh grant and all default subpermissions became/remainedtrue. Same guardian token retained child-detail/Me access.
- Own child-list route returned500 in all three read states. An isolated logger captured the exception without printing secret/configuration values: EF `InvalidOperationException` containing query-translation failure. Diagnostic500s are **failures classified, not security denials or repaired passes**.

Diagnostic mode is explicit via `QA_GUARDIAN_REVOCATION_BASELINE=1`; the default module requires fixed behavior and never accepts500/429 as denial.

## Narrow changes

`StudentGuardiansController.SetPortalAccess` now sets overall access from the explicit command independently of child age. Subpermissions remain `access && requestedFlag`; grant/revoke timestamps and response contract retain existing semantics. Initial `Link` minor default, initial student onboarding policy, DTO omitted-subpermission defaults and current authorization/tenant rules stay unchanged. A revoked individual guardian link is not equivalent to removing the child's required guardian relationship.

`PortalController.GuardianChildren` sorts by the equivalent `FirstName + " " + LastName` expression **before** constructing `PortalChildSummary`. Ordering by the record's `Name` afterward failed SQL translation. Keep server-side current academy/guardian/grant/revocation filters, fields and enrollment-count expression; no client-side unscoped materialization or access expansion.

No frontend changes, new PENTA tool, schema/migration, permissions, audit rewrite or AI security contract change. Existing host/domain services independently enforce the result; model memory cannot grant guardian access.

## Fixed evidence

Final run `f717a944a28847deaa1703f522c895c8`: **90/90 native SQL/Identity/HTTP cases PASS**, no500/429. Four DOB variants: minor, adult, exactly18 today, unknown. Each verifies active access -> explicitfalse revoke -> restricted regrant -> omitted-subpermission revoke -> default regrant. Reuse the same guardian token throughout; direct detail403 after revocation and current child-list/Me200 omit the child while retaining other permitted children. Birthday fixture uses captured UTC date and refuses a run that crosses its date boundary.

- Fresh target grant and returned flags/timestamps agree exactly; relationship/primary/student/guardian/academy preserved. Explicit and omitted revocations clear all subpermissions.
- Other child/guardian grants, source students/guardians, captured current Identity links/activity, resources, notifications and recursive web/storage file hashes preserved. Reads/denials capture identical snapshots. Successful PATCH adds exactly one correctly attributed admin/academy/route audit, retaining prior audits.
- Minor unlink204 removes only the target grant, adds audit, preserves other rows and prevents current-token detail/list/Me access; regrant without link404.
- Anonymous401, foreign-admin/parent-self-escalation/Teacher403, unlinked/foreign/missing child command404 and foreign/unlinked child detail403 controls unchanged.
-21 successful own-guardian child-list reads prove the query repair across each age/authority state and unlink; exact returned ID set equals fresh eligible grants, foreign/unlinked IDs excluded. Full payload/ordering/count edge cases and all other portal lifecycle variants are not claimed exhaustive.

Unchanged **GuardianFlags55/55** native SQL/HTTP regression PASS, run `35b3b45390564c08b6ac60cca1f3d7e3`: eight finance/document/academic combinations; invoice and certificate restrictions, restricted nested documents, foreign/unlinked/anonymous controls, revoked grants and inactive learners. Existing financial projections and document access retained.

**24 targeted unit tests PASS** (18 new explicit authority/initial Link-default cases +6 existing ClassMaterialAccess cases),0 skipped. Application/harness builds0 warnings/0 errors; runner parsing/diff checks PASS. Both successful hosts apply88 application/7 Identity migrations and verify non-admin scoped runtime privileges before requests. No full enterprise suite rerun.

Intentionally local structured evidence:

- `QA/EVIDENCE/guardian-revocation/bce44b1494ef485f83b7b620faf06dba/results.json` —11 diagnostic cases, three500s retained.
- `QA/EVIDENCE/guardian-revocation/f717a944a28847deaa1703f522c895c8/results.json` —90 fixed cases.

## Failures and cleanup

Initial native positive-control500 led to the separate API issue above; no relaxed fixed-gate assertion. The first unit-test compile omitted required LastName fields in its initial-link fixture; corrected synthetic fixture only and reran successfully. No application change for this compile mistake.

Successful runs verified ownership and removed their exact owned DB/login/container. Initial failed container was separately checked for exact run/name/label/loopback binding before stopping/removing; only disposable synthetic SQL was removed, source and QA evidence preserved. No container pruning. Existing Mini API/inference containers untouched. `.build-check` stays ignored; `QA/EVIDENCE` untracked. Unrelated Mini files and32/28 existing continuity additions retained; main baseline `f7af512f884ca5fe8ac3ddb287c852492592e709` unchanged. No merge/Azure deployment.

## Strict rerun / continuation

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/guardian-revocation-sql
powershell -NoProfile -ExecutionPolicy Bypass -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module GuardianRevocation
```

Unset `QA_GUARDIAN_REVOCATION_BASELINE` for acceptance. Process-only execution-policy override for this inspected local script; no persistent machine/Git configuration change.

Browser/physical devices, concurrent revocation/write integrity, broader role/portal lifecycle and critical release acceptance remain pending; issues remain OPEN. No frontend/Mini/provider training update or cross-project security handoff required. Existing FW1 shared frontend delivery request still prepared, not delivered cross-chat; its pending package is separate from these host repairs.

The E0 census118 records/117 groups is a frozen earlier snapshot. This newly tracked API issue adds one record/group:119 records/118 groups including the unchanged refresh alias, not119 unfixed tasks. No bulk closure/recount of older findings.

NEXT **Sol High**, E0 financial-policy bundle: evidence-backed proposed refund/credit-note, zero-net payroll and Voided restoration contracts, without inventing ledger/tax treatment or executing unsafe mutations. Accountable policy review remains required. **Sol Medium** can continue the seven feedback routes (Trial Bookings first) and settled shared visuals independently. No automatic model/project switch; enterprise/Mini/release gates remain OPEN.
