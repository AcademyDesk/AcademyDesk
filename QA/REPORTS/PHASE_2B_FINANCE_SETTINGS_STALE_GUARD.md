# Finance settings stale-editor guard — bounded SQL verification, 2026-10-11

## Verification successor — Docker recovered, strict SQL PASS

Normal Docker Desktop stop/start succeeded after the owner's continuation of the restart request. No factory reset, file deletion, WSL reset or pruning. Read-only inventory before/after confirms the same older QA containers retained and PENTA API/inference running; only this run-owned SQL container removed. Earlier blocker/checkpoint wording below is historical where superseded.

Fresh harness build0 warnings/0 errors; real Identity/HTTP/disposable-SQL run **b418248d40d2479d843a80c2414e6699**, loopback53270,78.2s: **21/21 sequential PUT controls and5/5 concurrent pairs PASS**, each pair exactly one200/one409, one settings row, unmixed winner fields, matching fresh SQL/GET/version and exactly one success audit. First pair covers absent-row insertion; four cover replacement. Stale Compact no longer overwrites newer Professional. Missing version428/manipulated409 and rejected-role/tenant/no-write snapshots pass; ordinary themes and explicit current-version NULL/omitted reset preserved. **No429**.88 application/7 Identity migrations; scoped login/DB removal verified; negative mismatched cleanup refused; only `academydesk-qa-b418248d40d2479d843a80c2414e6699` stopped/removed. Local receipt: `QA/EVIDENCE/finance-settings-stale-20261011/sql-final.txt`.

Retained same-source67 targeted unit,6 handler,36+16 synthetic browser/types/export83 passes are not rerun or expanded claims. Policy lint0/0, Governance3 inherited warnings. Separate API/SQL and synthetic DOM proof is **not linked browser/device/full-critical or same-action SQL audit-fault acceptance**. BUG-DATA-0053 stays OPEN/RETEST. Scoped feature publication only after diff/link/secret/scope/remote checks; unrelated14 tracked files and older32/28 continuity additions excluded. No main/Mini/Core/Azure change. NEXT Sol High same-action audit-fault/linked host acceptance as relevant; Medium settled Manual UI. Existing E0–E10 and PENTA/Mini queues retained.

## Routing / scope

MODEL: Sol High recommended; no automatic model change.
TOOL: Codex.
REPOSITORY: `D:\AcademyDesk-codex-p0`.
BRANCH: `codex/penta-search`.
STARTING HEAD: `03c545ddb315e62b26319e9c459f361b2b0482c1`.
ENVIRONMENT: Local source, InMemory units, synthetic exported DOM and separate disposable real HTTP/SQL.
TASK: BUG-DATA-0053 stale overwrite across Finance Policy and Finance Governance document editors.
MAY MODIFY: Settings endpoint, both editors, scoped tests/tools/continuity.
MUST NOT MODIFY: Unrelated Mini/Platform work, main, credentials, production databases, schema, accounting policy, accepted assertions or Azure.
VALIDATE: Version/no-write controls, original themes/reset contract, both-editor recovery, strict SQL races/audit.
STOP WHEN: Bounded SQL acceptance and scoped feature publication verified; no main merge/Azure or issue closure.
NEXT ROUTE: Sol High audit-fault/linked host gates; Medium settled UI afterward. No Mini project switch.

## Retained baseline

The [ordinary-save repair](PHASE_2B_FINANCE_POLICY_THEME_PRESERVATION.md) was feature-published at03c545d. Its native run59358d6cf5574380a4510f976c42bd83 verified15 ordinary/reset/rejection/authority controls plus3 deliberate stale-reproduction requests: old Compact overwrote newer Professional with200. This was not a guarded race pass. Accepted audits and the whole enterprise suite were not restarted.

## Implemented — bounded SQL verified, broader acceptance OPEN

- GET returns academy-bound SHA256 `settingsVersion`, canonicalized across eleven settings fields plus row ID/created/updated metadata, and `Cache-Control: no-store`. Decimal scale is normalized. The token is state, not a credential or permission.
- PUT rejects missing/blank versions with428 and stale/manipulated versions with409 before settings writes. No wildcard or automatic overwrite retry.
- SQL Server parameterized `UPDLOCK,HOLDLOCK` on the stable academy row precedes settings read/compare/write, including absent-row insertion. Existing atomic host boundary holds the transaction through success audit; controller fallback owns a transaction only when none exists. Independent authorization remains enforced, including opted-in Platform Owner audit. No schema/migration change.
- Accepted writes advance existing update metadata even for identical replacements, preventing managed-write ABA revival. Relational reload makes the returned version reflect persisted precision.
- Existing eleven-field replacement, supported themes, validation and explicit NULL/omitted payslip reset remain unchanged **with a current version**. No permission expansion or finance calculation change.
- Both editors send their loaded version, synchronously guard duplicate submissions and disable pending controls.409/428/network failures retain drafts without retry; confirmed success updates version, and only confirmed document success closes its editor.
- Governance list/approval refresh cannot silently advance the settings snapshot behind an open draft. Four new synthetic browser cases load a newer Professional/version during approval-list refresh, retain the draft's old Compact/token and verify409 plus unchanged newer state.

### Compatibility

An older client omitting `settingsVersion` now fails safely with428. The response/DTO member is additive, but PUT is **not backward-compatible without this precondition**. Release both upgraded editors and API together only after gates; stale loaded pages must refresh. Do not deploy only the backend or silently fetch a version and retry an overwrite. No deployment authorized here.

## Results

| Check | Result / boundary |
| --- | --- |
| Targeted .NET tests | **67/67 PASS**:24 new FinanceSettingsConcurrency +7 retained InvoiceDocumentSettings +36 retained AuditOutcome; InMemory/unit, not SQL locking or real HTTP authority proof. |
| SqlHarness build | PASS,0 warnings/0 errors; compilation only. |
| Ordinary-save actual TSX handler | **6/6 PASS**; original assertions retained with additive version-bearing fixture/payload. |
| Both-editor exported DOM | **36/36 PASS**: original32 plus4 open-draft/approval-refresh controls;320/1440px × light/dark, synthetic transport. |
| Retained exported theme matrix | **16/16 PASS**, original preservation/rejection assertions retained with version fixture. |
| TypeScript | PASS. |
| Policy lint |0 errors/0 warnings. |
| Governance lint |0 errors/3 warnings, independently identical to frozen HEAD (effect dependency/two image warnings); not warning-free. |
| Web webpack export | **83/83 PASS**; initial wrong root-directory invocation failed, corrected app-directory build passed. |
| Real settings HTTP/SQL/races | **21 controls +5 pairs PASS**, no429,88/7 migrations/exact owned cleanup; see successor above. |
| Linked browser-SQL / physical Android+iOS / full critical suite | NOT RUN; issue/release gates remain OPEN. |

Commands (worktree, except last three from `apps/web`):

```text
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/finance-settings-concurrency --filter "FullyQualifiedName~FinanceSettingsConcurrencyTests|FullyQualifiedName~InvoiceDocumentSettingsTests|FullyQualifiedName~AuditOutcomeTests" --logger "console;verbosity=minimal"
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/finance-policy-theme-sql
node QA/tools/finance-policy-preservation.test.cjs
node QA/tools/finance-settings-conflict-browser.cjs
node QA/tools/finance-policy-theme-browser.cjs
node node_modules/typescript/bin/tsc --noEmit
node node_modules/eslint/bin/eslint.js src/app/finance-policy/page.tsx src/app/finance-governance/page.tsx
node node_modules/next/dist/bin/next build --webpack
```

Local evidence, intentionally not committed:

- `QA/EVIDENCE/finance-settings-conflict-browser-1791721359840/result.json`/36 screenshots (latest, includes background refresh).
- `QA/EVIDENCE/finance-settings-conflict-browser-1791721117265/result.json`/32 screenshots (earlier; mobile conflict screenshots inspected).
- `QA/EVIDENCE/finance-policy-theme-browser-1791721148469/result.json`/16 screenshots.

No unexpected browser console errors/horizontal document overflow in the asserted matrix. Existing Governance preview has internally clipped presentation at320px; not full visual/device approval or premium Manual redesign. Existing lint/visual gaps remain queued.

## Earlier blocker and acceptance requirements (historical)

Docker Linux engine pipe is unavailable. Normal `docker desktop start` launched backend processes but startup failed opening/renaming local `sailor-ingest.sock`: **The file cannot be accessed by the system**. A fresh status check still cannot retrieve engine status. Only the CLI wait was interrupted; no Docker reset/data deletion. Quit/restart approval was requested, not assumed. Never factory-reset, delete socket/configuration, prune or remove unrelated Mini containers. No run-owned SQL container/database/login was created by this slice; no inventory asserted while daemon is unreachable.

After approved safe recovery, run:

```text
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module FinancePolicyTheme
```

Strengthened acceptance: **21 sequential PUT controls**, retaining ordinary-save/reset/rejection/authority cases and adding stale409/missing428/manipulated409; **five simultaneous pairs**, first absent-row insertion and four replacements. Each pair must yield exactly one200/one409, one row, unmixed winner fields, matching fresh SQL/GET/version and exactly one success audit.429 never passes. Denied snapshots must remain unchanged. Remove only exact run-owned SQL resources; do not weaken assertions. Same-action audit-failure injection, linked browser and devices remain separate; retained AuditOutcome units do not prove this action's real-SQL fault rollback.

## Earlier unpublished checkpoint (historical)

BUG-DATA-0053 **OPEN / RETEST**. No staging/commit/push/main merge/Azure/Mini/Core changes. HEAD still03c545d, `.build-check` ignored and `QA/EVIDENCE` local. Fourteen unrelated tracked dirty files and older32/28 continuity additions preserved. Stay Sol High for SQL gate, then existing E0/Manual/PENTA queues; no new testing plan or enterprise completion claim.
