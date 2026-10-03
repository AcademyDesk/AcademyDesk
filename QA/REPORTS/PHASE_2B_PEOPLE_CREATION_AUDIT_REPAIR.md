# Phase 2B — Student/teacher creation save/audit boundary

2026-10-01. **Before-fix fault reproduced twice; local action-level repair PASS: API 163/163 and two final SQL runs ×(28 people +six finance cases). BUG-API-0002/Phase 2B remain OPEN.** Agreed Sol High. Accepted Astra lifecycle mappings reused, not re-audited. No commit/push/Azure deployment or normal dev database changes.

## Bounded repair

Only Students.Create and Teachers.Create opt into the shared relational transaction using [AtomicAcademyMutationAttribute](../../apps/api/Security/AtomicAcademyMutationAttribute.cs). The marker is method-only, non-inherited and single-use. [AcademyAccessFilter](../../apps/api/Security/AcademyAccessFilter.cs) retains the existing finance boundary and additionally recognizes the explicit MVC action MethodInfo marker. [Students](../../apps/api/Controllers/StudentsController.cs) and [Teachers](../../apps/api/Controllers/TeachersController.cs) each add one import and one creation-action annotation; DTOs, optional fields, business validation and response contracts are unchanged.

These actions add only academy domain records through the same scoped context. They do not provision login accounts. Their create SaveChanges and generic audit insert now commit together; audit failure still returns 500 through the existing handler but rolls back the person record. No exception is swallowed or missing audit treated as success.

Student/Teacher Update actions remain unmarked: they save domain data and synchronize UserManager accounts through another context. Media completion owns a SQL/Blob lifecycle and also remains unmarked. Eight new [scope tests](../../tests/AcademyDesk.Api.Tests/AuditOutcomeTests.cs) check both Create methods, both excluded Update methods, excluded media Complete, retained invoice boundary, unknown MVC descriptor and method-only attribute usage. They test selection, not cross-store rollback or runtime account synchronization.

Authorization/module checks and platform-flag bypass remain unchanged. This does not repair every academy controller, [teacher account-update consistency](../ISSUES/BUG-DATA-0052.md), or full onboarding/provisioning.

## Before-fix reproduction

Two fresh guarded SQL runs each perform 28 cases. INSERT is denied only on the disposable owned AuditLogs table to its generated runtime principal, then restored in finally. Student and teacher POST each return 500 **after one person has persisted**, with audit delta zero. Captured Identity/finance/notifications stay unchanged. This confirms the existing [BUG-API-0002](../ISSUES/BUG-API-0002.md) beyond finance; it is not a new issue or a repeated static audit.

[Four-record before snapshot](PHASE_2B_PEOPLE_AUDIT_BEFORE_SOURCE_SNAPSHOT.json) preserves filter/controller/unit sources and compiled API/harness digests. Application sources were edited while the already-built baseline assembly was retained for the second `--no-build` run. The assembly hashes were independently checked unchanged through second baseline completion before building the repair. Both baseline runs therefore use the pre-repair application, not an inferred before-state from current annotated sources.

## Executed evidence

| Stage | Run / loopback / UTC start / elapsed | Evidence/result |
| --- | --- | --- |
| Before 1 | `0433941e19b3460b8aea2ed587997e4b` /51764 /06:56:35.1126453 /54.9 s | [log](../EVIDENCE/logs/phase-2b-people-audit-baseline-run1.log): two faults reproduced; 26 other creation/access/read controls PASS; owned cleanup exit 0 |
| Before 2 | `3a3fa2784dcc431ba34ac751cae5eacb` /65041 /06:57:58.8603684 /44.9 s | [log](../EVIDENCE/logs/phase-2b-people-audit-baseline-run2.log): same reproduction/controls; owned cleanup exit 0 |
| API suite | 163/163, zero failed/skipped | [log](../EVIDENCE/logs/phase-2b-people-audit-api-tests.log): previous 155 +eight action-scope tests PASS |
| Harness build | Zero warnings/errors | [log](../EVIDENCE/logs/phase-2b-people-audit-build.log): before and final builds PASS |
| Final 1 | `2b8d678ef6434712bdfc29ee3ef9611d` /60009 /06:59:58.4475712 /47.5 s | [log](../EVIDENCE/logs/phase-2b-people-audit-fixed-run1.log): 28 people +six finance cases PASS; owned cleanup exit 0 |
| Final 2 | `ac86684046714257bd402d80c1638606` /60035 /07:01:22.4502205 /48.4 s | [log](../EVIDENCE/logs/phase-2b-people-audit-fixed-run2.log): 28 people +six finance cases PASS; owned cleanup exit 0 |

28 people cases comprise eight per entity type (audit fault; minimal/full optional create; blank name/foreign branch/cross-tenant/anonymous refusals; GET), plus six current actor classes ×two create actions. Admin/Owner can create both; FrontDesk/custom students.manage can create students but not teachers; FinanceUser/Teacher without grants cannot create either. TeachersController is currently unmapped for delegated permission: this check preserves that policy rather than inventing teachers.manage or broadening access. Not all system roles or grant combinations are sampled.

Successful 201 requests compare response ID, trimmed first name and Location with fresh SQL; row academy/audit actor/academy/action/route attribution is checked. Minimal payload omits optional fields and confirms Email/Phone/BranchId/DateOfBirth null plus active default. Populated student fields and selected teacher contact/branch/date/specialties/qualifications/employment/address/JSON fields round-trip trimmed/as supplied. This is not all profile fields, arbitrary file formats, duplicate-person policy or malformed-JSON business validation certification.

Refusals and repaired faults compare all captured Students/Teachers/Branches/AuditLogs, selected Identity user/role/link metadata and finance invoice/payment/adjustment/profile/payout/notification rows across the two tenants. No captured changes or success audit. Successful creation does not create/update captured Identity or finance/notification state. Identity comparison deliberately excludes credentials/stamps/other tables; no all-database atomicity claim. GET checks no captured writes/audit and excludes the known foreign student/teacher fixture from each own list. Recovery is one later successful synthetic request after restoring audit INSERT, not exactly-once retry or lost-response acceptance.

Each final run first repeats the six finance fault/recovery/read/rejection cases from [prior repair](PHASE_2B_AUDIT_SAVE_REPAIR.md). Previous 129-case finance access and frontend results are retained historical evidence, not rerun or claimed as new passes in this slice. Real Identity, MVC, exception handling, rate limiter, SQL and audit remain; no mock auth or new listening frontend/backend. Runtime digest unchanged `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`, routes 307/controller method-routes 296/Identity method-routes 10. Both resolved contexts use the exact owned target/runtime login, migrations 80 application/7 Identity; health/auth/two-tenant controls PASS.

## Source/cleanup/limits

[96-record final snapshot](PHASE_2B_PEOPLE_AUDIT_SOURCE_SNAPSHOT.json) extends prior 92. Prior filter/unit outcome tests and two QA entry/runner files changed; four captures added (People QA module, new marker, two newly annotated controllers). Of 58 previous application/test captures, only filter/unit tests changed, 56 unchanged. The two controllers' before hashes and narrow annotation-only diff are retained separately. No unrelated application changes overwritten. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`.

[Validation/cleanup](../EVIDENCE/logs/phase-2b-people-audit-validation.log) records final hashes/links/counts and exact four resources absent. Each wrapper refuses mismatched ownership cleanup, removes run-owned database/login/host root, then checks exact container name/run label/loopback binding before stopping/removing it. Only current disposable QA resources removed; previous interrupted containers and normal dev/Azure untouched. No migrations, frontend changes, cloud credentials or storage changes.

Browser/UI success messages, Android/iOS, concurrent writes, duplicate retry/response loss, commit/result serialization faults, all-table/Identity atomicity, portal-account provisioning, platform-owner bypass and critical suite NOT RUN. Other shared mutation paths remain OPEN. No issue/phase/release closure.

## Next bounded task

Reproduce and repair **linked student/teacher update partial persistence**, using accepted [BUG-DATA-0052](../ISSUES/BUG-DATA-0052.md) and paired student lifecycle mapping. Coordinate domain and Identity consistency rather than applying a domain-only marker to these actions. Continue **Sol High**. Preserve unresolved restoration/zero-net policy and broader media/device/lookup/concurrency/critical/release gates; no deployment or repeat Astra audit.
