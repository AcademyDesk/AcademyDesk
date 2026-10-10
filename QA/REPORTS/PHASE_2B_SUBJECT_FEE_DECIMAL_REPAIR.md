# Subject-fee decimal input — bounded repair

2026-10-10; worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting commit `bd59b23364008a092dba8eed4447ee027ece86f6`.

## Scope and result

BUG-FUNC-0008: shared `StudentFeeArrangements` amount input now specifies `step="0.01"`. Existing `min="1"`, required field, number conversion, payload, frequency, reset, success text and API authorization/domain behavior remain unchanged. Both `/student-fees` and `/student-profile` consume this same component. No backend, money-policy, rounding, duplicate, loading or feedback refactor.

## Evidence

- Before editing application source, exported starting application reproduced native `stepMismatch` for 1.01 and 1250.50 on both routes.72 baseline browser checks PASS (asserting the defect and retained constraints), local `QA/EVIDENCE/governance-periods-feedback-browser-1791639502735/result.json`. The inherited runner prefix used that directory name; corrected for final evidence, nothing moved/deleted.
- Fixed export:88 checks PASS, two routes ×320/1440px × light/dark. Each combination checks native validity for1,1.01,1250.50,0,-1,0.50,1.001 and blank, plus three valid submissions. Invalid inputs send no POST. Valid submissions send exactly the existing payload with numeric amount, Monthly frequency and UTC date; reset and existing success text verified. Local `QA/EVIDENCE/subject-fee-amount-browser-1791639571881/result.json`,8 screenshots. Two mobile screenshots visually inspected; no page exceptions.
- TypeScript `npx tsc --noEmit` PASS; production `npx next build --webpack` PASS,83 exported pages.
- Target eslint **NOT PASS**, unchanged before/after:1 `react-hooks/set-state-in-effect` error and1 `react-hooks/exhaustive-deps` warning in existing `load` effect. Not suppressed or refactored in this one-attribute fix. Remains a next bounded loading/feedback task.
- Static API trace: positive decimal validation remains unchanged; `AcademyDeskDbContext` maps amount with `HasPrecision(18,2)`. UI minimum1 remains deliberately separate from API positive-only minimum; no new policy inferred. API over-precision/rounding behavior not accepted by this frontend test.

## Acceptance boundary / next

Browser transport is synthetic, not real HTTP/SQL/Identity/role/tenant proof. No SQL container or production data used. Physical Android/iOS, fresh SQL persistence/readback, endpoint matrix and critical regression remain NOT RUN in this slice; issue stays OPEN. Earlier enrollment real SQL and28 feedback checkpoints retained, not rerun or replaced. Existing unrelated Mini edits/evidence preserved.

Next Sol Medium: shared fee editor loading/feedback lint gap with draft/pending/uncertain-response proof, preserving minimum/payload/domain. Sol High for finance/authority/transaction decisions. Shared FW1 UI/Mini qualification, enterprise/release gates remain pending. No main merge, Azure deployment or new testing plan.
