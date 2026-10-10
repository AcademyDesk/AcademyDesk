# Shared subject-fee loading and save feedback

2026-10-10. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, starting HEAD `164ca21e7da5168a2979bc2bf1bb5609869e2980`.

## Bounded implementation

- Both consuming screens retain the shared editor. Pure uncached list fetch checks HTTP status and array shape; initial loading/failed load is not reported as an empty successful list. Forms remain disabled until a checked initial load.
- Accessible polite status displays durable confirmed addition, denied/invalid failure, or network/5xx uncertainty. Failed/unconfirmed drafts remain; no automatic write retry. A confirmed addition followed by failed refresh stays confirmed with explicit do-not-repeat guidance.
- Synchronous ref guard covers write and readback, including same-tick/stale-handler submissions. Inputs/frequency/button are disabled during the operation; matching-only confirmed reset preserves a later draft.
- Editor is keyed by academy/student. Switching student deliberately starts a fresh draft/list; old mount/write/readback results are ignored after unmount. This is UI response isolation, not a substitute for backend tenant authorization.
- Existing subject/amount/frequency/date payload, min1, step0.01, Monthly selection retention and backend/domain/finance rules unchanged. No role/tenant/schema/Mini/approval changes. Initial load failure requires explicit page refresh, not a new automatic retry loop.

## Verification

| Check | Result |
| --- | --- |
| Actual shared TSX handler/lifecycle tests |17/17 PASS: success/payload/reset,400/403/500/503/network, confirmed-save/failed-refresh, failed/malformed initial read, duplicate write/readback, later draft, keyed scope, unmount at mount/write/readback and stable rerender |
| Exported browser matrix |72/72 PASS: two consuming routes ×320/1440px × light/dark × success/400/403/500/network/refresh-failure/initial-failure/malformed/scope-change |
| Retained decimal browser regression |88/88 PASS, original assertions unchanged |
| Target eslint |0 errors/0 warnings PASS; prior load-effect error/dependency warning removed without suppression |
| TypeScript |PASS |
| Production webpack build/export |83/83 pages PASS |

Browser uses synthetic loopback API only, blocking unrelated network. Held write exercises disabled controls and native duplicate submit dispatch; student switch verifies old scoped request does not reset the new student's draft or show stale success.24 screenshots; mobile light refresh-failure and dark switched-student screenshots inspected. Page exception checks PASS; expected HTTP/network failure responses are intentional, not clean-HTTP claims.

Local evidence: `QA/EVIDENCE/subject-fee-feedback-browser-1791649144750/result.json` and `QA/EVIDENCE/subject-fee-amount-browser-1791649171639/result.json`. Evidence remains intentionally untracked. Earlier decimal baseline and enterprise receipts preserved, not replaced by these narrower results.

## Still open / next

No new live SQL/HTTP/Identity/role/tenant/device/critical-suite run; no container or production database touched. BUG-FUNC-0003 and BUG-FUNC-0008 remain OPEN; original28 feedback-route checkpoint count unchanged (this shared editor is an additional bounded gap). Enterprise/Mini/FW1/release acceptance not completed.

NEXT Sol Medium: `/student-fees` admission-fee save-feedback gap, preserving optional nulls, PUT/minimum/precision/date/domain rules. Sol High for genuine authority, finance policy, concurrency or release decisions. No Mini project switch required. Scoped feature publication only; preserve unrelated32/28 continuity and other Mini edits, `QA/EVIDENCE`, `.build-check`; no main merge/push or Azure deployment.
