# Phase 2B — Student/teacher Update branch integrity

2026-10-01. **Invalid branch assignment reproduced twice; local repair PASS: API 184/184 and two final SQL runs ×48 cases. BUG-DATA-0051 and Phase 2B remain OPEN.** Agreed Sol High. Accepted Astra finding and paired student path reused, not re-audited. No commit/push/Azure deployment or normal development database changes.

## Bounded change

[Students.Update](../../apps/api/Controllers/StudentsController.cs) and [Teachers.Update](../../apps/api/Controllers/TeachersController.cs) now validate a nonnull BranchId against `Branches.AnyAsync(Id == request.BranchId && AcademyId == academyId)` before assigning any person fields or reaching linked accounts. Each controller adds exactly **two guard lines**; removing them reproduces its before-source hash. The returned 400 message matches Create: “The selected branch does not belong to this academy.” Missing and Guid.Empty also fail the existence check.

Null and omitted BranchId remain optional and clear the existing assignment, matching the existing full-update contract. Optional email/phone/teacher specialties remain nullable. An inactive branch owned by the academy remains selectable because existing Create checks membership, not active state. This preserves current behavior; it does **not approve a new inactive-branch business policy**. Foreign inactive branches are still rejected. No permission weakening, FK/migration, DTO, global filter, Identity transaction, frontend or storage change.

## Reproduction and verification

[Branch QA module](../tools/SqlHarness/PeopleBranchRegression.cs) reuses the real linked-person fixture/assertions. Each case begins with a valid own branch on a fresh student/teacher and two actual same-tenant accounts plus a foreign-account control. Five real synthetic branches cover own first/second/inactive and foreign active/inactive. Each entity gets eleven cases: own assignment, reassignment, foreign, missing, Guid.Empty, foreign inactive, null clearing, omission, own inactive, explicit nullable optionals, malformed GUID.

Before fix, four invalid selections per entity (foreign/missing/empty/foreign-inactive) return **200**, persist the invalid branch and both own-account updates, and add one success audit. Fresh SQL and actual list GET readback confirm these stored IDs. Both entities lack a configured branch FK, consistent with the accepted finding. Malformed GUID already returns 400 from model binding; it is not a new repaired case.

After repair, those four selections return **400 with the exact branch message** and preserve all captured Students/Teachers/Branches/AuditLogs, selected Identity user/role/link metadata, additional linked-account username/normalized/email/phone fields, and finance/notification rows. No success audit. Malformed GUID also preserves captured state. Successful controls verify fresh domain fields, response ID/trimmed name, exactly two matching own-account updates, one Admin/academy/PUT/route audit and list GET branch readback with no captured read mutation. Foreign/unrelated account metadata stays unchanged. Credentials/stamps/uncaptured tables are excluded; no all-database claim.

[Six unit guards](../../tests/AcademyDesk.Api.Tests/PeopleBranchValidationTests.cs) exercise foreign/missing/empty ID on each controller before mutation or account-store access. They intentionally provide no UserManager and use isolated InMemory only for this early-return guard; these are not transaction/auth/SQL substitutes. All 178 prior API tests plus six new guards PASS (184 total).

Each final SQL run first executes the [26 linked-update regressions](PHASE_2B_LINKED_PEOPLE_REPAIR.md): real Identity validation, first/second SQL UPDATE failure, audit INSERT denial, recovery, nullable fields, active-token gates and route/role/anonymous/missing/blank controls. Those faults remain atomic after the branch guard. Six additional health requests verify active→inactive→reactivated token behavior; they are secondary controls, not additional numbered cases. Thus each final run has **22 branch +26 linked =48 primary cases**, not double-counting duplicate LINKED/BRANCH log lines or GET/health checks.

No shared application filter/factory changed, so previous 28 creation/six finance/129 finance-access results are retained historical and not re-executed or claimed as new passes. This is targeted regression, not a repeat Astra audit or full critical suite.

## Executed evidence

| Stage | Run / loopback port / UTC start / elapsed | Result |
| --- | --- | --- |
| Baseline 1 | `ecfe4aac5ef94f4e9ef6344d55c3a882` /56593 /07:30:16.4044651 /57.6 s | [log](../EVIDENCE/logs/phase-2b-branch-baseline-run1.log): 22 cases, eight invalid-assignment failures reproduced, 14 controls PASS; cleanup exit 0 |
| Baseline 2 | `60bffd86f289477bbd5f5a3a55b89391` /64512 /07:31:42.1971572 /51.1 s | [log](../EVIDENCE/logs/phase-2b-branch-baseline-run2.log): same reproduction/controls; cleanup exit 0 |
| API tests | 184 passed, zero failed/skipped | [log](../EVIDENCE/logs/phase-2b-branch-api-tests.log) |
| Harness builds | Zero warnings/errors | [baseline](../EVIDENCE/logs/phase-2b-branch-baseline-build.log), [repair](../EVIDENCE/logs/phase-2b-branch-build.log) |
| Final 1 | `252948c7e72e4cb9939d59806d1bb3a8` /53321 /07:34:08.9763549 /109.4 s | [log](../EVIDENCE/logs/phase-2b-branch-fixed-run1.log): all 48 cases PASS; cleanup exit 0 |
| Final 2 | `ab0596cdb07842188dd1be44addaa113` /53349 /07:36:17.5701615 /106.9 s | [log](../EVIDENCE/logs/phase-2b-branch-fixed-run2.log): all 48 cases PASS; cleanup exit 0 |

Runtime route digest unchanged `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`; 307 routes/296 controller method-routes/10 Identity method-routes. Both resolved contexts use the exact owned SQL target/runtime principal. Migrations remain 80 application/7 Identity, real health/auth/two-tenant preflight PASS. No new listening frontend/backend.

## Source/cleanup/limits

[Before snapshot](PHASE_2B_BRANCH_BEFORE_SOURCE_SNAPSHOT.json) records both controller hashes and compiled API/harness digests. Application sources were edited while baseline assemblies were retained for two `--no-build` runs. Their hashes were checked unchanged before run two and again after its completion before compiling the repair. Baseline therefore used the actual pre-guard code. Final compiled API SHA256 `8b0df80dbb2f562282c542f48f326f0159604cf02d8596a624240fb37e715def`, harness `90819ed04e230f251af1ff6720bdadf3bf888c0eef13d1b7b5119c9d56c4e75a`.

[103-record checkpoint](PHASE_2B_BRANCH_SOURCE_SNAPSHOT.json) extends previous 100: five prior captures changed (two controllers, QA entry/runner and linked QA helper's exact branch-error assertion); three additions (branch QA module, six unit guards, scoped validator). All 95 other prior captures remain unchanged, including shared transaction/filter/frontend. HEAD still `20bb6047f9edf733ac8e2a226621cc582ec54b3c`.

[Scoped validator](../tools/validate-branch-repair.cjs) checks hashes/HEAD/counts/links and independent absence of all four current owned containers/host roots/ports; [validation log](../EVIDENCE/logs/phase-2b-branch-validation.log) records results after final completion. Every wrapper validates exact run/name/label/loopback binding before cleanup, refuses wrong ownership, and removes only its owned database/login/root/container. Current disposable resources removed; older retained resources and normal dev/Azure data are not touched. No commit/deployment or machine policy change.

BUG-DATA-0051 remains OPEN pending full acceptance. Browser branch-name rendering, native mobile, all-role/platform-flag execution, stale/deleted-branch races, active-branch policy, old invalid-record reconciliation, concurrency/cancellation/commit/response-loss, other branch-bearing editors and full critical suite NOT RUN. Invalid branch IDs are rejected, not historical records silently repaired. Related linked-update/platform/media/optional-save issues and phase/release gates remain open.

## Next bounded task

Address the **already reproduced FinanceUser lookup dependency gap** [BUG-FUNC-0006](../ISSUES/BUG-FUNC-0006.md), starting with Payments/Invoices student selector dependencies. Use a least-privilege finance lookup/consumer contract rather than granting broad student management; preserve tenant/module/role boundaries and retain governance dependency as a separate subcase where needed. **Sol High** for implementation/targeted tests; Astra only if an unresolved permission-policy decision requires focused review. No repeat static audit or deployment. Restoration/zero-net, broader lookup/media/device/concurrency/critical/release gates remain queued.
