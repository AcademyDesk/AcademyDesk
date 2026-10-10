# Assignments feedback repair — 2026-10-10

BUG-FUNC-0003, starting `3c24ea2cb1d267e05e59dbd528692a8cde613830`, `codex/penta-search`, `D:\AcademyDesk-codex-p0`. Bounded existing-program frontend feedback repair, not a repeated authority audit.

## Repair and preserved behavior

Durable polite `Assignment created.` survives list readback. Confirmed save followed by failed readback explicitly says created / do not repeat. Rejected and unconfirmed writes retain the draft; safe nonblank server guidance or fallback handles malformed/blank/nonstring errors. Network/5xx are unconfirmed with check-before-retry guidance and no automatic retry. Synchronous pending ref guards write and readback, including stale handlers; fieldset/button disable during pending and restore in finally. Empty successful response bodies are supported.

Pure workspace fetch/scoped mount preserves initial academy/batch loading without captured loader dependencies or state-driven repeated reads. Only title/description/due reset after a confirmed save. Selected batch, assignment type and publication setting remain selected. Existing default Homework/published true, optional description/due nulls, exact POST contract and browser-local datetime-to-UTC conversion remain unchanged. No API/DTO/schema, teacher/student authority, publication policy, status UI, shared styling or Mini change.

## Validation

- Frozen HEAD baseline: **2 PASS /13 FAIL**; final **15/15 controlled actual-TSX checks PASS**. Exact payload/default/null/reset/preservation, initial batch selection, confirmed readback failure, HTTP400/403/500/503, network uncertainty, malformed rejection guidance, write/readback duplicate guards and stable request lifecycle covered. Logs remain local in QA/EVIDENCE.
- **16/16 synthetic exported-browser cases PASS**:320/1440 widths × light/dark × normal201/readback503/rejected400/uncertain-but-fixture-committed500. Actual DOM/select/checkbox edits, exact POST, one write, notice/pending/draft behavior, retained settings, row readback and no horizontal overflow/unexpected console/hydration errors. Deliberate resource errors separated. America/Los_Angeles fixture verifies local10:00 ->17:00Z without changing timezone policy. Receipt `QA/EVIDENCE/assignments-feedback-browser-1791610001762/result.json`; screenshots alongside. Mobile light normal screenshot visually inspected: form/list remain within viewport and notice visible. Earlier passing run retained; only log label corrected before final run, assertions unchanged.
- ESLint **0 errors/0 warnings**, TypeScript PASS, production webpack/static export **83/83 pages PASS**, diff checks required before publication.

Synthetic transport is not live SQL/auth/tenant isolation/device acceptance. No new backend/container suite, physical Android/iOS, cross-device persistence, revocation/rollback or release proof. BUG-FUNC-0003 and broader gates remain OPEN. QA/EVIDENCE local/untracked; unrelated work preserved; main/Mini/Azure unchanged. Mini saved86/94 CONTRACT READY / ENGINE BLOCKED and existing qualification handoff unchanged.

Next **Sol Medium**: Lesson Plans bounded feedback gap check, reuse existing accepted work. **Sol High** if authority, learning-domain policy or persistence changes are required. Continue existing enterprise program; no new testing plan or Azure deployment.
