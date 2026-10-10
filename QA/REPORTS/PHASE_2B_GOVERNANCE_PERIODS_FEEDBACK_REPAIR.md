# Academic Governance + Academic Periods feedback — 2026-10-10

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`; frozen starting commit `5cf2a98ed92243dc3bde89badf7cb5739f624be0`. BUG-FUNC-0003 remains OPEN. This is the existing E2 feedback queue, not a new testing strategy or enterprise closure.

## Bounded changes

- Governance: grading-scheme creation, course-prerequisite creation and scheme activation/deactivation retain authoritative success after register refresh.
- Periods: year/term creation and year/term closure retain success after refresh; capture the native form before awaiting, preventing released-event reset errors.
- Confirmed writes clear only their own creation draft. Status/closure actions preserve both unrelated forms; rejected or unconfirmed writes preserve drafts.
- A failed readback after a confirmed write produces a success plus refresh warning, not a failed-save notice. Network/5xx returns an uncertain-result explanation and asks users to check the register before retrying. No automatic write retry.
- A synchronous page-local ref blocks duplicate/opposing handlers throughout write and readback; native fieldsets and custom selects are disabled while pending.
- Checked, uncached initial loads enable the academy only after complete lists are available. Effect cleanup ignores stale initial responses; readback uses the original captured academy, without reselecting it from another academy-list response.
- Polite status regions expose feedback to assistive technology.

Payloads/routes remain unchanged: passing percentage Number conversion including0; blank grade bands `[]`; course IDs; year/term names and ISO dates; checkbox `on` mapping; scheme status boolean; year/term closure PATCH with no body. Closed years remain excluded from term options. Existing backend prerequisite, closure transaction, RBAC and tenant safeguards are untouched. No API, schema, finance, Mini or production changes.

## Verification

- `node --test QA/tools/governance-periods-feedback.test.cjs`:72/72 PASS. Actual transpiled TSX handlers with synthetic hooks/transport, not a reimplementation of handlers.
- Same assertions against frozen starting commit (`QA_UI_BATCH_BASELINE=1`):7 PASS/65 FAIL. These are failing checks, not65 independent defects. Baseline includes released-event exceptions and malformed-list crashes; no weakening of prior runners.
- Seven actions each cover exact request/reset/success, failed readback,400/403/500/503, network uncertainty, duplicate/opposing handler suppression during write and readback. Eight unavailable-workspace cases and required-field checks are included.
- Target ESLint:0 errors/0 warnings. Installed TypeScript `--noEmit`:PASS.
- Next production webpack build/export:83/83 pages PASS.
- `node QA/tools/governance-periods-feedback-browser.cjs`:112/112 synthetic browser cases PASS: seven actions ×320/1440px ×light/dark ×normal/readback503/rejected400/uncertain-but-committed500. Exact requests, correct own/unrelated draft resets, polite notices, no document overflow, no unexpected console/page errors checked. Three mobile screenshots visually inspected (Governance light, Periods light/dark); year register retains its horizontal table scroll, not document overflow.
- Evidence remains local/untracked: `QA/EVIDENCE/governance-periods-feedback-browser-1791636197146/result.json` plus112 screenshots. Browser uses existing exported application, loopback-only static server and synthetic API; no extra dev backend/frontend, real academy writes or SQL containers. Browser/server resources closed by runner.

## Acceptance limits / next

Synthetic tests do not establish SQL/domain correctness, live Identity/RBAC/tenant isolation, linked inference, physical Android/iOS or enterprise release acceptance. Prior prerequisite and year-closure repairs/evidence are retained rather than repeated. Financial FP1/FP2/FP3 remain proposed and unapproved. QA/EVIDENCE and unrelated Mini/continuity work stay local and preserved. No main merge or Azure deployment.

After the responsive checkpoint, the original queue becomes25/28 feedback checkpoints,3 remaining: Enrollments, Batch Promotions and Assessments creation only. Next Sol Medium: Enrollments+Batch Promotions; escalate genuine authority/domain/transaction decisions to Sol High. Existing wider E0–E10 delivery gates and FW1 Mini frontend handoff remain pending as recorded in [progress checklist](../../ACADEMY_PROGRESS_CHECKLIST.md) and [execution path](../../ACADEMY_EXECUTION_PATH.md). No project switch required for this batch.
