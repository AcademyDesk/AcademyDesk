# Production acceptance work — 29 October 2026 IST

Current state:release is BLOCKED under the existing release policy. The latest observed fully successful development run is [40738226886](https://github.com/AcademyDesk/AcademyDesk/actions/runs/40738226886) for `10a60eebc2e193b06de31622e897e755062dd72c`. Payroll print/PDF source is pushed at `128604b`; its own run is being checked. No issue has been closed solely because CI passed.

## Highest remaining gates

| Gate | Current evidence | Remaining work |
| --- | --- | --- |
| Exact final source | Earlier financial/record/payroll SQL and full CI green; print has11 unit/4 real synthetic-transport PDFs | Final candidate CI, including newly added print engine check |
| Authentication/security | Existing bounded SQL modules and earlier reports | Rerun role replacement/revocation, session revocation, refresh and concurrent refresh on this candidate |
| Recovery | New guarded recovery module compiles; actual execution pending | Execute full copy-only/checksum backup, VERIFYONLY/header checks, actual restore and full table/column fingerprints; confirm runtime privileges and HTTP afterward |
| Five P0 issues | Repairs and bounded automated checks exist | Original and complete required regressions, current browser/role/security evidence and documented closure protocol |
| Critical P1 workflows | Numerous repairs; broad register still OPEN | Resolve and accept remaining wrong-record forms and critical operational workflows; no silent exceptions |
| Staging | No latest-source full acceptance | Real authenticated Admin/Teacher/Student/Guardian journeys, financial/private-file access, changed-device checks and configuration validation |
| Production recovery/deploy | No new deployment or main merge | Azure backup/PITR and rollback evidence, exact candidate acceptance, reviewed main integration, then deployment and read-only smoke |
| Broad AI | PentaMini49 research tests pass; synthetic planner | Authenticated read-only gateway/connector pilot first. Broad90%AI coverage is not implemented and is not part of a ready production claim |

## New bounded verification batch

The existing guarded runner now schedules `RoleReplacement`, `RoleRevocation`, `SessionRevocationRepair`, `SessionRefresh` and `RefreshConcurrency` in CI, followed by `Recovery`. None of the legacy reproducer-only modes is selected.

`RecoveryRegression.cs` runs only after the harness creates and marks a fresh GUID-named database inside an exact run-labelled loopback SQL container. It seeds financial/Identity/audit rows through the existing HTTP regression, fingerprints all user tables in primary-key order plus column schema, and performs a full copy-only/checksum backup under that owned container's data directory. It checks VERIFYONLY and backup source/type/checksum/copy-only metadata. A QA-only canary changes after backup; real RESTORE must revert it and restore every fingerprint. The destructive restore rechecks Docker identity, SQL server name, database ownership and run-token digest immediately beforehand. It accepts no configured or external database target. After restore it checks the ownership marker, both EF migration sets, unprivileged runtime login, health, anonymous denial and real Identity login/authorized read. Raw table data and backup files are never published. Successful owned-container cleanup removes the ephemeral backup.

This is isolated synthetic SQL Server recovery proof, not proof of Azure SQL PITR, customer backup retention, private-media recovery or production rollback. Those remain separate required gates.

No product code, configured application database, customer records, normal development seed, Azure settings or PentaMini code changed in this verification-only batch. Historical evidence is retained. Next route:Codex/Sol High inspect real CI outcomes, repair actual failures and continue critical acceptance; no repeated permission needed for authorized feature-branch work.

Local verification of the new QA batch:SQL harness compilation0warnings/errors;11 existing host/ownership guard cases PASS;14 production-gate fixtures PASS; PowerShell parsing, actionlint and diff checks PASS. The actual production-gate CLI correctly remains BLOCKED by all five open P0 records. These are control checks, not an executed backup/restore result.

Follow-up:GitHub payroll-print job123322240946 in run40836452644 for128604b is observed SUCCEEDED in13m58s. This includes the mandatory Chrome/PDF check. The newer security/recovery and assessment generations need their own candidate verification.

Actual next-run result:eaa95e5/run40838396103 FAILED at the native session-refresh adapter. A missing VM AbortController global was reproduced and fixed without changing application code. RoleRevocation is now explicit strict acceptance rather than its historical diagnostic-only mode. Later concurrency/restore stages were not reached. See [native adapter report](CLOUD_NATIVE_SESSION_QA_2026-10-29.md); next candidate execution remains required.
