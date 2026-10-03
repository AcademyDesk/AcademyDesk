# Phase 2B — Linked student/teacher update consistency

2026-10-01. **Partial persistence reproduced twice; bounded local repair PASS: API 178/178, two final runs ×60 SQL cases (26 linked +28 creation +six finance). BUG-DATA-0052, BUG-API-0002 and Phase 2B remain OPEN.** Agreed Sol High. Accepted Astra mappings reused, not re-audited. No commit/push/Azure deployment or normal development database changes.

## Repair boundary

[Students.Update](../../apps/api/Controllers/StudentsController.cs) and [Teachers.Update](../../apps/api/Controllers/TeachersController.cs) explicitly opt into `[AtomicAcademyMutation(IncludeIdentity = true)]`. The [method-only marker](../../apps/api/Security/AtomicAcademyMutationAttribute.cs) defaults to domain-only, so both Create actions and finance retain their earlier behavior. Media/file/Blob/provisioning and other mutations are not enlisted.

[AcademyAccessFilter](../../apps/api/Security/AcademyAccessFilter.cs) validates and enlists the existing scoped Identity context through [AcademyIdentityTransaction](../../apps/api/Security/AcademyIdentityTransaction.cs), using the academy SQL connection and transaction. Domain save, every linked UserManager update and generic audit commit together. Pending 400/500, action exception or audit-save exception leaves the boundary uncommitted; disposal rolls back. No account-store mock, authorization bypass, swallowed failure or schema change.

The guard requires SQL Server on both sides, identical configured connection settings including database/principal, no current context transaction, a closed Identity connection and explicit connection strings. Unsupported configurations fail before business writes. It reads immutable options rather than an authenticated connection string that may have hidden its password. Cleanup detaches Identity without the canceled request token and restores a **new** EF-owned connection; it does not reuse the disposed old connection or globally share connections. Domain owns the transaction/shared connection. Current Program config uses the same SQL string for both stores.

For the repaired boundary an IdentityResult failure now says **“No changes were saved.”** The unchanged platform-flag bypass does not enter this boundary: its legacy partial-save message is retained rather than falsely claiming rollback. No platform-owner, permission or module behavior is broadened. This bypass remains an explicit open gap. Direct controller calls outside the filter are also not an atomicity guarantee.

## Before-fix reproduction and fault method

Each entity uses a fresh synthetic domain row with two real same-academy linked accounts and a foreign-academy account carrying the same person link GUID as a scoping control. Real default UserValidator rejection is induced by storing malformed synthetic usernames directly in the disposable fixture after normal UserManager provisioning. This is a controlled historical-bad-account scenario, **not a claim that email uniqueness is configured** or that optional email must be supplied.

The [linked QA module](../tools/SqlHarness/LinkedPeopleRegression.cs) installs a trusted Identity-only DbCommandInterceptor through the [guarded QA factory](../../tests/AcademyDesk.Api.Tests/Infrastructure/QaApiFactory.cs). It is inactive by default, activated only after the exact owned SQL marker is revalidated, and selects a fixed synthetic DisplayName parameter on an actual AspNetUsers UPDATE. The first or second selected command is prefixed with SQL THROW; SQL Server produces the save failure through the actual UserManager/EF/MVC pipeline. There are no test triggers, fake IdentityResults or replacement databases. Fault interception is disarmed in finally.

Before repair, validation/first-store failure leaves the person saved and zero accounts changed; second-store failure leaves the person and **one** account saved; audit INSERT denial leaves person and **both** accounts saved. All return 400/500 with zero success audit. Fresh account comparisons prove the second-save partial result without assuming unordered query order. Before/final commands both use actual SQL; final second-save fault reaches two UPDATE attempts but all captured changes roll back.

[Five-record before snapshot](PHASE_2B_LINKED_BEFORE_SOURCE_SNAPSHOT.json) records source and compiled API/harness hashes. Application sources were edited while the compiled baseline assemblies were retained. Their hashes were checked unchanged before the second `--no-build` baseline run and again after it, before compiling the repair. This preserves actual before-state evidence, not a reproduction using the repaired application.

## Executed evidence

| Stage | Run / loopback port / UTC start / elapsed | Result |
| --- | --- | --- |
| Baseline 1 | `0daafc3f5fe74eec82aacd67c35c099a` /49754 /07:15:50.9872062 /82.6 s | [log](../EVIDENCE/logs/phase-2b-linked-baseline-run1.log): 26 cases; eight partial-save faults reproduced, 18 success/refusal controls PASS; cleanup exit 0 |
| Baseline 2 | `546c610ba2a84187884987becaf8bb04` /53235 /07:17:31.3824690 /68.2 s | [log](../EVIDENCE/logs/phase-2b-linked-baseline-run2.log): same reproduction/controls; cleanup exit 0 |
| API tests | 178 passed, zero failed/skipped | [log](../EVIDENCE/logs/phase-2b-linked-api-tests.log): previous 163 +seven Identity opt-in +eight configuration-guard tests |
| Harness build | Zero warnings/errors | [log](../EVIDENCE/logs/phase-2b-linked-build.log) |
| Final 1 | `8a4278ec1c5e4311a069e5995f129fe0` /53390 /07:20:22.1472069 /101.6 s | [log](../EVIDENCE/logs/phase-2b-linked-fixed-run1.log): all 60 cases PASS; cleanup exit 0 |
| Final 2 | `5580c6cad26c4072b0bdfc55f35f7b3b` /61624 /07:23:02.9328483 /75.2 s | [log](../EVIDENCE/logs/phase-2b-linked-fixed-run2.log): all 60 cases PASS; cleanup exit 0 |

26 linked cases =13 per entity: Identity validation; first and second SQL store faults; audit fault; populated recovery; nullable optional omission; deactivate/reactivate; blank name; missing person; foreign route; anonymous; Teacher-role refusal. Six additional health requests check a previously issued linked token: active 200 → inactive 403 → reactivated 200 for each entity. This tests the active-user middleware, not all portal screens or all role/grant combinations.

Successful responses check ID/trimmed name and fresh domain FirstName/LastName/Email/Phone/BranchId/IsActive plus teacher Specialties. Both own accounts synchronize DisplayName/IsActive/PhoneNumber and nonblank Email/NormalizedEmail. Omitted Email/Phone/Specialties remain optional; domain email/phone and teacher specialties become null, linked phone becomes null, existing linked login emails are retained. No login-clearing or email-uniqueness policy is invented. Foreign/unrelated accounts remain unchanged.

Successful writes have exactly one generic audit attributed to the actual Admin/academy/PUT action/route. Rejected actions and repaired faults preserve all captured people/branches/audits, selected Identity users/roles/links and finance/notification state across both academies, with no success audit. Extra account capture includes username/normalized username, phone and normalized email. Credentials/stamps/uncaptured tables are excluded and never printed. Recovery is a later valid request after fault removal, not lost-response or exactly-once retry certification.

Each final run repeats [28 creation cases](PHASE_2B_PEOPLE_CREATION_AUDIT_REPAIR.md) and [six finance audit cases](PHASE_2B_AUDIT_SAVE_REPAIR.md) before adding linked fixtures. This is targeted regression for the changed shared filter/factory, not a repeated Astra static audit. Earlier 129 finance-role cases and frontend evidence remain historical, not newly executed. Existing runtime inventory unchanged: routes 307, controller method/routes 296, Identity method/routes 10, digest `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`; migrations 80 domain/7 Identity. Real health/auth/two-tenant preflight PASS.

## Source, cleanup and limits

[100-record current snapshot](PHASE_2B_LINKED_SOURCE_SNAPSHOT.json) extends previous 96: eight exact prior captures changed (two controllers, filter, marker, audit tests, QA factory, QA entry and runner), four additions (transaction helper, guard tests, linked QA module, scoped evidence validator). Unrelated captures remain unchanged. Final API assembly SHA256 `94dec0727ee4138adb112d51f3482b4062119e97234b11fb6d0b035fee19ec86`; harness `b64e1432dad7b474b76414c85d9d2bcaeeb787432818b102f9a922e2c7cdcecf`. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`.

[Scoped validator](../tools/validate-linked-repair.cjs) checks hashes/links/counts/HEAD and independent absence of all four exact current containers, host roots and ports. [Validation log](../EVIDENCE/logs/phase-2b-linked-validation.log) records results. Each run refuses bad ownership cleanup, removes only its owned SQL database/login/host root and then validates the exact name/run label/loopback binding before container removal. Current disposable resources removed; earlier retained resources, laptop dev DB and Azure untouched. Initial Windows PowerShell script-policy refusal occurred before container creation; rerun used process-only ExecutionPolicy Bypass, no machine-policy changes or excluded business run.

BUG-DATA-0052 and BUG-API-0002 stay OPEN pending broader acceptance. Platform-flag bypass, direct invocation, mismatched-store runtime hosting, post-request Identity reuse, concurrency/cancellation/commit/response-loss faults, all-role/client success notice/reload, mobile/native devices, account provisioning, media/Blob atomicity and full critical suite NOT RUN. Foreign/nonexistent branch selection on Update is **not repaired by this transaction**; accepted BUG-DATA-0051 remains queued. No optional fields made mandatory, frontend/schema/cloud changes or release certification.

## Next bounded task

Use accepted [BUG-DATA-0051](../ISSUES/BUG-DATA-0051.md) for teacher Update branch validation and the paired student Update path: reproduce own/foreign/missing/null branch behavior, preserve optional null, repair confirmed tenant integrity and retest linked-account/audit no-write behavior. **Sol High** remains appropriate. Keep inactive-branch policy explicit rather than inventing it. No repeat static audit or deployment; unresolved restoration/zero-net, media/device, lookup/concurrency/critical/release gates remain queued.
