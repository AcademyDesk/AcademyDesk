# Assessments creation feedback — 2026-10-10

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting commit `78579800cb57b32d45a7d25a52179236e55e43fa`. Continues the existing E2/BUG-FUNC-0003 queue, not a new audit or enterprise closure.

## Bounded implementation

- Confirmed creation retains its success notice after refreshing the assessment register. Failed readback remains a confirmed creation with a warning not to repeat the write.
- Network/5xx means unconfirmed creation, retains the draft and advises checking saved records before retrying. Other non-OK responses display server guidance without false success. No automatic mutation retry.
- Shared synchronous pending ref guards create and result-save handlers against same-tick duplicate/opposing submissions through write and readback. Native fieldset/custom selects/result actions are disabled while pending.
- Initial academy/options/register responses are checked before enabling writes. Narrow academic options and uncached same-academy readback remain; stale selection-effect responses are ignored. Result-load errors do not erase a confirmed creation notice.
- Original create payload retained: batch ID, title, type, numeric maximum, nullable grading scheme, optional local-to-UTC schedule and publication flag. Confirmed reset clears only title/date/scheme; batch/type/maximum/time and existing result drafts remain.
- ResultRow implementation and grading/roster rules unchanged. Existing result-save payload/notices/readback semantics retained, with only shared pending protection added. No backend/API/schema/authorization/domain changes.

## Evidence

- `node --test QA/tools/assessment-create-feedback.test.cjs QA/tools/assessment-options.test.cjs QA/tools/assessment-grade.test.cjs`:46/46 PASS. Existing29 grading/options assertions unchanged; React mocks extended for pending ref. New17 creation checks include exact request/null/time/reset, initial unavailable/malformed workspace,400/403/409/500/503/network, confirmed-readback failure and duplicate/opposing writes.
- Frozen starting source under identical new assertions:3 PASS/14 FAIL,0 cancelled/skipped. Counts are assertions, not14 distinct defects. First frozen run stalled because the test awaited a duplicate readback before releasing its gate; corrected gate release/settlement without weakening assertions, then reran successfully.
- Target ESLint0 errors/0 warnings; installed TypeScript `--noEmit` PASS; production webpack build/export83/83 pages PASS. Initial direct effect state update lint finding corrected before final validation.
- `node QA/tools/assessment-create-feedback-browser.cjs`:32/32 synthetic cases PASS,320/1440px × light/dark × existing/empty register × normal/readback503/rejected400/uncertain-but-committed500. Exact payload, matching-only reset, retained other result draft/selection, polite status, no horizontal document overflow or unexpected console/page errors asserted. Two mobile screenshots visually inspected.
- First browser attempt failed on an invalid harness CSS selector; corrected to standard `:not` selector and reran the entire matrix. Failed evidence retained.
- Local/untracked final evidence: `QA/EVIDENCE/assessment-create-feedback-browser-1791638401289/result.json` and32 screenshots. Browser/context/loopback server cleanup completed; no SQL container created or removed.

## Limits and next task

Synthetic intercepted APIs are not real SQL, Identity/RBAC/tenant, physical Android/iOS, inference or full-workflow acceptance. Earlier accepted assessment SQL/grading/options evidence is retained, not rerun or relabeled as new proof. BUG-FUNC-0003 remains OPEN despite28/28 bounded route feedback checkpoints. No main merge, Azure deployment, Mini modification or financial-policy approval.

Next **Sol Medium**, BUG-FUNC-0016 required enrollment lifecycle-reason UI/integration using the existing server contract. Escalate authority/domain/transaction decisions to High. Financial FP1/FP2/FP3 approval, shared frontend FW1 delivery, Mini qualification and broader E0–E10 enterprise release gates remain open in the [progress checklist](../../ACADEMY_PROGRESS_CHECKLIST.md). No Mini project switch needed for this batch. Preserve unrelated work and local QA evidence.
