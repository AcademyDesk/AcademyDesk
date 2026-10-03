# Phase 2A — two-tenant HTTP/SQL access controls

Date: 2026-09-30. Run ID `8c1240490dde4b1b8494d5afc126ebb4`. This builds on the real HTTP and SQL harness in `PHASE_2A_HTTP_SQL_CONTROLS.md`.

## Implemented and executed

The isolated harness seeded two synthetic academies, one distinct student in each, and two real Identity users assigned the `AcademyAdmin` role and their respective academy IDs. Both users signed in through `/api/auth/login`. With the real ASP.NET middleware, global `AcademyAccessFilter` and SQL DbContexts:

- Admin A's own student-list request returned HTTP 200 and only A's student.
- Admin B's own student-list request returned HTTP 200 and only B's student.
- Admin A's GET against B's student route returned HTTP 403 and no B student data.
- Admin A's POST to create a student in B returned HTTP 403. A fresh SQL context found B still had one student and no attempted cross-tenant row.

The preceding controls also passed in the same run: `/health` 200, anonymous academy request 401, real Identity login 200, authorized academy list 200. Both migration sets (79 application, 7 Identity) and limited runtime login were verified. Ownership-marker mismatch was refused before cleanup.

Final read-only SQL check returned NULL for the run database and login IDs. The run-owned folders and exact Docker container were removed after marker-checked cleanup. No development, customer or Azure data was used. `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --verbosity quiet` passed with zero warnings and errors. This is an AUTH-001 / SECURITY-ROLE-001 control, not a P0 reproduction or release approval.

## Remaining scope and publishing

The five P0 repro cases remain NOT RUN. The audit package in `QA/` is still an untracked local folder, while two earlier student UI edits are also uncommitted. The Phase 1 validator intentionally pins its prior source and commit and will report a baseline mismatch after the Phase 2 host-testability change; its historical result must not be represented as a current clean run. A scoped GitHub commit needs explicit inclusion decisions for the audit artifacts and those earlier UI edits. The Azure deployment workflow is `workflow_dispatch` only, so a GitHub push alone does not deploy production.
