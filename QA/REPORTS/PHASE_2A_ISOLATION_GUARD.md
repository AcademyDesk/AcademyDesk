# Phase 2A — INFRA-ISOLATION-001 guard slice

Date: 2026-09-30. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. Phase 1's accepted application baseline and the two pre-existing student UI edits were not changed in this slice.

## Implemented

- Added a test-harness-only preflight guard in `tests/AcademyDesk.Api.Tests/Infrastructure/QaRunGuard.cs`. It generates a unique run ID and 256-bit random token, and derives the exact `AcademyDesk_QA_<run-id>` database and four distinct run-owned temporary roots.
- Startup callback runs only after checking `Testing`, the exact token, both context connection strings targeting literal `localhost` and the exact generated database, integrated security with no SQL credentials/file attachment/failover, no automatic startup migration or owner bootstrap, and exact content/web/storage/Data Protection paths. Existing reparse points in the run subtree are rejected.
- Cleanup callback runs only with a matching run manifest and ownership marker (run ID, server, exact database and token digest). Database-name prefix alone is insufficient.
- Negative tests use injected startup/cleanup callbacks and assert they are never invoked for invalid inputs. They perform no SQL or storage mutations.

## Execution

`dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --verbosity quiet`: **12 passed, 0 failed** (8 pre-existing InMemory cases plus 4 guard cases). Guard cases include a matrix of 16 rejected startup inputs, five rejected cleanup markers, and positive boundary controls. No SQL database, HTTP host or file tree was created; no Azure operation, commit, push or deployment was performed.

## Handoff and remaining safety gate

This guard is not wired into `Program.cs` and does not yet protect ordinary application startup. The next Sol High task must build the HTTP/SQL harness so it obtains the **final effective configuration** (including environment overrides), calls this preflight before invoking `WebApplicationFactory` or any setup mutation, and supplies only the approved run target to both DbContexts. Then query a real database ownership marker before invoking cleanup; this slice only validates a supplied marker. Provisioning must recheck local SQL reachability, create a new run-owned DB, use scoped runtime credentials, isolate Data Protection and content roots before `Program.cs` creates `wwwroot`, and prove both context migration histories. Stop if a late override or startup side effect can bypass the guard. The five P0 runtime reproductions remain NOT RUN.
