# Phase 2B — Circular Course prerequisite repair

Historical checkpoint: superseded by [closed-year term creation](PHASE_2B_YEAR_CLOSURE_REPAIR.md). Backend341 and its next closed-year task below are retained historical evidence; current backend is363. Prerequisite browser/device/legacy/critical gates remain open.

2026-10-01. Sol High. Reuses accepted [BUG-DATA-0029](../ISSUES/BUG-DATA-0029.md); no repeated Astra audit. One application controller changed locally. Issue/Phase2B remain OPEN; not release acceptance.

## Repair and reproduced evidence

[Controller](../../apps/api/Controllers/AcademicGovernanceController.cs): before inserting course -> required, traverse own-academy required-course edges and reject a path back to course with400 and an actionable message. Iterative traversal/visited nodes terminate even with legacy cycles. Self/missing/foreign-course400, duplicate409, successful empty200 and MustBeCompleted=true remain unchanged. Existing grading-scheme/list semantics, permissions, modules, schema and enrollment rules are not changed.

An academy-specific, parameterized, transaction-owned SQL application lock protects graph check + insert across replicas. Only this action opts into the existing domain save/audit boundary; no Identity enlistment. Platform-owner bypass retains its existing no-audit behavior but uses an owned transaction for its graph write. Failed lock acquisition returns503 without an edge. Lock lifetime follows transaction commit/rollback, as specified in [Microsoft's SQL documentation](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver17). No new platform authorization policy is inferred.

The [19-case regression](../../tests/AcademyDesk.Api.Tests/CoursePrerequisiteTests.cs) against unchanged application source [failed6/passed13](../EVIDENCE/logs/phase-2b-prerequisite-baseline.log): five confirmed cycle defects (two/three/long/diamond/legacy-connected) plus one expected future atomic-boundary assertion, not six independent product defects. After repair, the [complete backend suite](../EVIDENCE/logs/phase-2b-prerequisite-suite.log) passed341/341 (previous322 +19); [TRX](../EVIDENCE/prerequisites/prerequisite-suite.trx) records no failures/skips. Those direct-controller/InMemory checks alone do not certify SQL/auth/concurrency.

## Real Identity/HTTP/SQL verification

The [scoped harness](../tools/SqlHarness/CoursePrerequisiteRegression.cs) adds32 exact cases on each of two fresh runs:

- 13 graph controls: empty, two/three/long cycle, transitive DAG, valid/closing diamond, disconnected DAG, duplicate/self, and three legacy-cycle termination/reachability controls. Synthetic foreign edges sharing node IDs do not influence own-tenant reachability.
- 5 missing/foreign membership controls; foreign Admin403, Teacher403, anonymous401; own scoped GET and foreign GET403. Success/rejection checks compare every captured prerequisite row and audit, plus courses/modules/batches/enrollments/grading schemes and existing Governance people/academy/Identity/finance/task snapshots. Not every database table.
- 3 deterministic concurrent pairs: reciprocal, duplicate and three-course closing edges. An external transaction holds the exact graph lock; SQL DMV evidence requires both HTTP requests to be in APPLICATION WAIT before release. Each pair produces one200 plus400/409, exactly one matching stored edge and actor audit, no changed prior rows. This is bounded two-request concurrency, not load/stress certification.
- Other academy remains writable while A's graph lock is held; same-academy timeout503 changes no captured rows/audits. Deliberate run-owned AuditLogs INSERT denial yields500 with complete edge rollback; restoring the permission allows a valid save and proves lock release. Platform-owner valid save and reverse-cycle rejection cover its controller-owned transaction path without changing bypass audit policy.

[Final run1](../EVIDENCE/logs/phase-2b-prerequisite-sql-final-run1.log) and [final run2](../EVIDENCE/logs/phase-2b-prerequisite-sql-final-run2.log) each32/32 PASS using identical final application/harness binaries. [Final build](../EVIDENCE/logs/phase-2b-prerequisite-sql-build-final.log):0 warnings/errors. The [initial SQL fixture attempt](../EVIDENCE/logs/phase-2b-prerequisite-sql-run1.log) stopped with403 because the test academy lacked AcademicGovernance. Only synthetic fixture modules were corrected; that attempt is excluded from product passing/failing counts, retained and its exact labelled container removed. No application permission was relaxed.

## Isolation and remaining limits

Existing dispatcher/wrapper gains only the selected CoursePrerequisites module. Build outputs stay under `.build-check/prerequisite-fixed` and `.build-check/prerequisite-sql`; normal dev API/harness and historical test/runtime binaries remain unchanged. Existing Testing ownership guard resolves both contexts to the exact generated loopback-only SQL target, with database-scoped runtime credentials.80 domain/7 Identity migrations and runtime inventory311 routes/300 controller method-routes/10 Identity method-routes retain the established digest. Normal dev services/database and Azure/Blob/customer data untouched; no commit/push.

Each successful run refuses a bad ownership marker before removing only its database/login/root and labelled container. The failed fixture container is separately ownership-checked then removed; disposable synthetic data is regenerable, logs retained. [Snapshot](PHASE_2B_PREREQUISITE_SOURCE_SNAPSHOT.json)/[validator](../tools/validate-prerequisites.cjs)/[output](../EVIDENCE/logs/phase-2b-prerequisite-validation.log):163 source captures,157 prior unchanged,2 QA dispatcher/wrapper changes and4 first captures (repaired controller, new test/harness/validator); controller pre-repair hash retained separately. No blanket dirty-tree reset or repeated frontend suite/lint/browser audit.

Legacy stored cycles are not repaired or deleted, and adding a non-cyclic edge into a legacy cycle is not prohibited by this new-cycle-only guard. UI edge removal, enrollment completion workflows, browser/device/error-message rendering, all linked scenarios/roles/module policies and critical/release gates remain unverified. Arbitrary SQL writes or future graph writers must use the same lock; no database-level DAG constraint is claimed. Existing frontend233/TypeScript/lint evidence is historical, not rerun after this backend-only change.

Next bounded accepted gap: closed academic-year term creation (BUG-DATA-0030), Sol High. Reuse the audit; reproduce/fix/test only that gap. Do not reopen unchanged Astra work.
