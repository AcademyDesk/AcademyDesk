# Teacher publication form contract — local checkpoint, 2026-10-11

## Final bounded verification — 2026-10-11

The local-only checkpoint below is retained as history, superseded for the explicitly completed gates here. Original production repair unchanged; only QA integration/browser fixtures and continuity were added.

- Harness build: **0 warnings / 0 errors**. New `TeacherPublication` module reuses the existing guarded runner, preserves every prior module and cleanup check, and uses isolated artifacts under `.build-check/teacher-publication-sql`.
- **20/20 real Identity/HTTP/disposable-SQL cases PASS**, no HTTP429. Both assignment/assessment true/false controls return200; JSON string equivalents return400 without assignment/assessment/notification/audit writes. NULL and populated ISO dates persist exactly; blank/malformed date strings reject. Anonymous401, Admin403, existing foreign-batch400 and nonexistent-batch400 preserve snapshots. Six successful creates each have exactly one title-matching row, response identity/boolean and fresh authorized GET verification. This does not assert HTTP201 or introduce a permissive JSON converter.
- Run `f14bca55a66d48d4895993e6851f2c68`, SQL loopback port49679;88 application/7 Identity migrations. Existing HTTP/tenant preflight and negative ownership-cleanup controls PASS. Exact run-owned database/login/container removed. Existing `pentaaimodels-api-1` and `pentaaimodels-inference-1` untouched.
- **16/16 production-export browser cases PASS**:320/1440px × light/dark × homework/lesson/undated assessment/dated assessment. Exact POST contracts (one each), visible existing confirmation, native title reset, no console/page errors or horizontal overflow. Browser score fixture12 respects unchanged native integer-step constraint; SQL/handler12.5 is separate binding evidence, not native fractional-input acceptance.
- Browser fixture failures retained: missing mounted payments response, icon-inclusive navigation accessible name, hidden desktop nav on mobile, and native fractional-score constraint. Corrected only fixtures/locators, never permission or application score rules; assertions were not relaxed.
- Evidence: `QA/EVIDENCE/teacher-publication-contract-20261011/sql-final.txt` and `QA/EVIDENCE/teacher-publication-browser-1791700972182/result.json`. Two mobile screenshots inspected. Existing fixed bottom navigation/shared notice placement and legacy small controls are **not** accepted as final Manual redesign; this batch changes no visual styling.
- Retained12 handler checks rerun PASS. TypeScript/83-page export and unchanged4-error/3-warning lint comparison are retained from the preceding source-identical checkpoint, not claimed rerun this SQL/browser packet.

BUG-API-0005 remains **OPEN / RETEST** for directly linked browser-to-SQL, physical Android/iOS, full critical regression, broader form pending/network/uncertain-result safeguards and Manual redesign. API persistence and browser proofs here are separate. No Mini/Core/model/production/main/Azure change. Validated scoped feature-branch publication follows secret/scope checks; unrelated work and local evidence excluded. Next bounded existing queue item: BUG-DATA-0053 hidden payslip-theme preservation, inspect/reproduce first, Sol Medium for settled serialization; High if finance semantics/authority changes arise.

## Scope and baseline

BUG-API-0005 / TEACHERPORTAL-CONTRACT-001. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `c8c6acf38fde914920ff3e80443e74f571f48478`.

The current component is named `TeacherTasks`, not the historical `TeachingTools`. Its native hidden inputs produce strings. The actual handler forwarded `isPublished` strings, left blank optional `scheduledAtUtc` as an empty string, and accessed `event.currentTarget.reset()` after awaiting the request. TeacherPortalController's assignment/assessment request DTOs still require boolean publication and nullable assessment dates. No API rule or role change is needed.

## Bounded repair

Only the shared teacher create handler changed: capture its native form before awaiting, explicitly convert hidden publication values to booleans, and normalize an optional blank assessment date to NULL. Existing score conversion, local-to-UTC date conversion, title/type/description/objectives, batch guard, POST endpoints and success/rejection wording are retained. Successful lesson-plan reset benefits from the same captured-form correction. No backend, Mini, permission, financial, database or publication-policy change.

## Verification

- Actual TSX handler test uses native FormData populated with synthetic fields and controlled hooks/transport; it exposes only the component and the otherwise-discarded handler promise in the test-transpiled source. Production exports and handler body are not replaced.
- Frozen starting HEAD with the same assertions: **6 PASS / 6 FAIL**. Four publication type mismatches, one optional date mismatch and one lesson-plan reset failure. The initial harness surfaced asynchronous null reset errors; observing the promise explicitly allowed independent payload assertions without weakening them.
- Repaired actual handlers: **12/12 PASS**. Both boolean values across both routes; optional blank date; exact populated payloads and score/date conversion; captured reset/confirmation; lesson-plan payload; missing-batch no-write and rejected-response draft retention for all three forms.
- Installed TypeScript `tsc --noEmit`: PASS.
- Production webpack build/static export: **83/83 pages PASS**.
- Teacher page ESLint: **NOT PASS**, 4 errors / 3 warnings, identical rules/severities/lines/columns to frozen HEAD. These inherited effect/image findings are not suppressed or represented as clean. First baseline lint invocation from repository root could not locate the web config; rerun from `apps/web` used the installed config successfully.
- Local baseline/final handler and lint evidence: `QA/EVIDENCE/teacher-publication-contract-20261011/`. Evidence remains intentionally local.

## Not accepted / next packet

This is a local handler/build checkpoint, **not** HTTP binding, SQL persistence, rendered-browser confirmation, linked browser/SQL, physical Android/iOS, or full critical regression acceptance. BUG-API-0005 remains OPEN; no commit or push performed in this checkpoint. Unrelated dirty work, local evidence, main and Azure are preserved.

Next: reuse the existing disposable SQL/Identity/HTTP harness to verify JSON true/false succeeds with exactly one row, string equivalents and blank-string date reject without writes, NULL/valid ISO date persist, and existing teacher/batch/tenant authority remains intact. Then verify the real exported teacher forms in a controlled responsive browser and record exact success/reset/console behavior before scoped feature publication. Endpoint success is currently **200**, not an invented 201 expectation. Sol Medium for settled form/browser work; Sol High for consequential host/security/SQL decisions. Existing Manual visual and PENTA qualification queues remain unchanged.
