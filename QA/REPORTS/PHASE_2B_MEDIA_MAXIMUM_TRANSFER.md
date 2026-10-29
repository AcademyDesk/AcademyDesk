# Batch1 — actual maximum media transfer / streaming recovery

2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. QA-only bounded native transport gate; continue existing enterprise program.

## Status

BOUNDED NATIVE PASS. Run `4ed1c9a93b9445c7a9a99a7b4a3ff9cc`,308.5s/exit0: unchanged75 original Blob assertions plus **actual2,000,000,000 bytes/239 chunks+one retry/120-chunk fresh-host recovery/272 HTTP/275 unchanged snapshots/590 additive assertions/no429 PASS**. Counts include repeated chunk status/snapshot/control/metadata checks, not590 distinct journeys or defects. Full streamed source/download SHA256 agrees: `C23FE4A82DD1C7E2CEE2F1DF179D76DE35FA712B05ACA42C3E56F97983AF84F8`.

One-time publication, metadata/previous records/ETags, current-role/tenant controls, maximum HEAD, five exact boundary ranges, client-canceled partial download then separate full streamed request all PASS.90 application+7 Identity migrations, non-admin runtime, root HTTP/two-tenant controls, mismatched-marker cleanup refusal PASS. Exact private Blob container/SQL database/runtime login and only this run's two Docker containers removed; all nine prior containers preserved. Native API is ASP.NET TestServer's in-process HTTP pipeline with real SQL/private loopback Azurite network I/O, **not** browser/Kestrel/containerized-API/reverse-proxy/Azure acceptance.

No application/frontend/storage limits/permissions/schema/Mini changes or application defect found in this bounded run. The previous six lifecycle cases remain accepted at their original receipt; they are not rerun or replaced by this size gate. Original75 `BlobUploadRegression.cs` assertions remain unchanged and precede the additive module.

## Scope and acceptance oracle

- Actual **2,000,000,000 bytes (decimal2GB, not2GiB)** cross the original Teacher HTTP upload API in239 production-sized chunks; final chunk3,511,296 bytes. No maximum-size array/source file is created. Two8MiB generation buffers and one64KiB read buffer, original per-chunk server buffering/storage path, are used. This is not a measured whole-process memory/load certification.
- Real persisted Identity tokens, SQL and private run-owned Azurite; no mock responses, auth bypass, storage/limiter relaxation, production database or Azure credentials.
- Negative, zero, maximum+1 and Int64-max declarations return exact400/guidance without protected SQL/Identity/files/committed-Blob changes. Bad chunk index/length return400; incomplete completion409.
- Pattern is chunk-index stamped at each chunk's beginning/end, including the partial last chunk. Incremental source SHA256 verifies actual complete streamed download; a repeated identical-block oracle would miss ordering errors and is not used.
- One first-chunk retry remains idempotent. At120 staged chunks, a fresh DI HTTP host and fresh Blob adapter resolve all120 durable blocks using the original bearer/request/session. No bytes are replayed and no new login/session is created. This is new-host/adapter continuity, not an OS-process crash/network/browser restart test.
- Each paced HTTP operation compares the15 existing protected lifecycle snapshot fields, complete upload rows and committed Blob name/ETag/length/modified-time inventory where unchanged state is expected. Identity projection remains the established safe five-field users plus role assignments, not every authentication metadata field. Intentional create/complete have exact allowed-delta checks.
- Completion adds exactly one scoped resource/one intended Student notification/one Blob; existing rows/ETags and session metadata remain unchanged except its completion timestamp. Duplicate completion must not republish; completed overwrite409; anonymous401 and foreign-tenant404 retained at maximum size.
- HEAD reports exact length and empty stream; no2GB preallocation is used to inspect its empty body. Five range cases cover a chunk boundary, suffix37, last-byte clipping, unsatisfiable offset and unsupported multi-range, including exact bytes/length/content-range.
- A download is canceled after initial bytes, then a separate request streams the entire file with exact count/SHA256 and attachment/private-no-store/nosniff headers; protected state and ETag remain unchanged. Not automatic browser resume/preview or whole-file MIME validation.

## Execution and safety

Fresh ignored artifact `.build-check/media-large-sql` required. Runner refuses inherited/mixed browser/lifecycle/fault modes before provisioning; minimum8GiB free both on hostC and inside the owned Blob filesystem; real authenticated SQL SELECT1 readiness. Original limiter unchanged;650ms pacing and any429 fails. Separate DI host after retained75 isolates the additive module's request window without changing persisted data.

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore -o .build-check/media-large-sql -v minimal
& QA/tools/SqlHarness/Run-BlobMedia.ps1 -LargeTransfer
```

Initial build27.50s/zero warnings-errors. Review improved HEAD inspection to read one byte from its empty stream instead of potentially preallocating advertised maximum size; full-read buffer iterations are not counted as separate test assertions. Final build11.02s/zero warnings-errors. Runner parser/diff and combined large/lifecycle refusal before provisioning PASS.

Accepted run `4ed1c9a93b9445c7a9a99a7b4a3ff9cc`, SQL loopback63628. Local evidence: `QA/EVIDENCE/batch1-20261012/build-media-large.txt`, `build-media-large-final.txt`, `sql-media-large.txt`. Final summary/exit/ownership-checked cleanup recorded, no runtime failure. Evidence intentionally untracked and build ignored. Parser/diff/scoped high-confidence credential scan PASS (not whole repository/history/security certification); no staged files, feature/main HEAD unchanged. Never prune/remove another container.

## Remaining boundaries / next route

BUG-SEC-0001/BUG-FUNC-0007 and Batch1 remain OPEN. No fresh browser codec/preview/recording/Android/iOS/physical-device/high-load/real-network/after-admission-revocation/Azure RBAC proof. The synthetic `.mov` payload is inert patterned transport data, not a valid playable video. Previous real small-file recovery/browser/lifecycle evidence remains separately bounded; original legacy failure and operational-release gates remain pending. Prior1484 units/83-page export/payroll/finance/linked UI receipts retained, not unnecessarily rerun for QA-only changes.

Next stay on the existing Sol High private-media/security queue, then Batch2 Manual Orbit+Aura/Prism shared four-portal visuals after actual versioned design handoff, settled Medium implementation. AI frontend remains PENTA project's responsibility; no Core/model/tool contract change or cross-project handoff needed here. No stage/commit/push/main merge/Azure/production access.
