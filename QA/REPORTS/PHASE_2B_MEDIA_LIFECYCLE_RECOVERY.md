# Batch1 — private-media lifecycle denial and recovery

2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. Sol High security slice; existing enterprise program, not a new audit.

## Status

BOUNDED NATIVE PASS; Batch1 and both issues remain OPEN. No application permission/storage/frontend/schema/Mini changes. Existing `BlobUploadRegression.cs` assertions remain unchanged.

Final run `a9ea188721fe48c2a45664a3a6bdf83d`,112s/exit0: **75 retained Blob assertions +6 new lifecycle cases/110 HTTP/66 exact400-404 denials/107 unchanged-state comparisons/313 new assertions PASS**. Assertions include status/snapshot/control/metadata checks, not313 distinct journeys or bugs. Original-stage8MiB+3-byte file restored and downloaded byte-for-byte; final SQL upload count unchanged, only its completion timestamp changes, exactly one scoped resource/Student notification/committed Blob added. Previous rows and committed Blob identities/ETags unchanged. No429;90 application+7 Identity migrations, non-admin runtime/root HTTP/two-tenant controls/mismatched-marker cleanup refusal PASS. Exact owned private Blob container/database/login and the two owned Docker containers removed. All nine pre-existing containers preserved.

## Missing boundary exercised

Existing assignment-revocation/browser recovery receipts are retained. This additive gate covers current **domain Teacher** and **academy** activity across a partially staged upload and an already published private Blob, three repetitions each. Authority is changed only on selected synthetic SQL rows; this is not an Admin lifecycle-editor test or queued/concurrent revocation after authorization.

Real persisted Identity logins, original bearer tokens, private Azurite, normal API pipeline, actual SQL and original 60-second native download tickets are used. No mocked responses, clock override, auth bypass, limiter relaxation or external Azure account is involved.

- Revoked create/retry: exact400 and existing assigned-scope guidance.
- Revoked progress, overwrite of staged chunk0, tail chunk1 and completion: exact404.
- Revoked Teacher GET/HEAD, ticket mint and redemption of the pre-revocation ticket: exact404.
- Inactive academy also denies Student GET/HEAD, mint and pre-revocation ticket; domain Teacher deactivation alone must not deny a separately authorized enrolled Student.
- Restoration: same token/ticket/session, original progress, exact private attachment bytes and no-store/attachment headers.
- Each no-write HTTP request compares all15 existing lifecycle snapshot fields plus complete upload rows and committed Blob name/ETag/length/modified-time inventory. Identity projection is the established safe five-field user projection and role assignments, not every authentication metadata field.
- Control mutations compare exact expected selected IsActive change and every unrelated snapshot field. Pending staged progress stays uncommitted block0. Final original-byte download additionally detects any unauthorized same-block overwrite that a block-list-only comparison could miss.
- Final resumption must preserve all previous rows/Blob objects and upload metadata, add exactly one correctly scoped resource, one intended Student notification and one committed Blob; no new upload session.

## Harness and execution

`MediaLifecycleRegression.cs` is additive, opt-in through guarded `Run-BlobMedia.ps1 -Lifecycle`; it runs after all unchanged75 original Blob assertions. A fresh DI host uses the same owned SQL/Blob and unchanged production limiter, isolating this additive module's request window without changing persisted data. Requests are paced650ms; unexpected status/429 fails.

Runner now requires an actual authenticated SQL `SELECT1` after the startup log (as other existing QA runners do), refuses combined lifecycle/browser/fault modes before provisioning, and verifies exact ownership plus loopback port before removing its two containers. Fresh dedicated ignored `.build-check/media-lifecycle-sql` artifact is required. Never prune other containers.

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore -o .build-check/media-lifecycle-sql -v minimal
& QA/tools/SqlHarness/Run-BlobMedia.ps1 -Lifecycle
```

Use the existing PowerShell environment. Initial Windows PowerShell `-File` launch was refused by its execution policy before provisioning; no policy was changed, no failed native acceptance claimed. Receipt retained at `QA/EVIDENCE/batch1-20261012/sql-media-lifecycle.txt`. Initial build44.66s/0 warnings/errors; final strengthened snapshot build10.32s/0 warnings/errors. Parser and unsafe combined-mode refusal passed.

Accepted run `a9ea188721fe48c2a45664a3a6bdf83d`, SQL loopback61287. Final native receipt `QA/EVIDENCE/batch1-20261012/sql-media-lifecycle-final.txt`, build receipt `QA/EVIDENCE/batch1-20261012/build-media-lifecycle.txt`. Evidence intentionally local/untracked; builds ignored. No native failure or application defect found in this bounded run; the refused Windows PowerShell launch is not a failing application case.

## Acceptance boundaries / next

BUG-SEC-0001, BUG-FUNC-0007 and Batch1 remain OPEN. This is not fresh browser/draft retention/recording/physical Android/iOS, actual2GB transfer, every role/guardian policy, concurrent already-admitted upload/download cancellation, external Azure, malware inspection or full release acceptance. Original legacy upload-failure reproduction is still separate. Prior1484 API units/83-page export/linked lifecycle/payroll/finance evidence is retained, not redundantly rerun for QA-only changes.

Continue the remaining existing private-media/security/operational gates on Sol High; later Batch2 Manual four-portal Orbit+Aura/Prism alignment after actual design handoff on the settled Medium route. No new Mini/Core security contract or PENTA-side handoff is needed for this host-only verification. No staging, commit, push, main merge, Azure deployment or production access.
