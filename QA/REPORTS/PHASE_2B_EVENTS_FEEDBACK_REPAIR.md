# Events feedback repair — 2026-10-10

Scope: BUG-FUNC-0003, `/events` create UI only, starting `6ca567da74f4073a4e446880780a3f29231c81dd` on `codex/penta-search`, worktree `D:\AcademyDesk-codex-p0`.

## Repair

- Confirmed create now leaves an accessible, durable `Event planned.` notice after readback.
- Confirmed save followed by failed readback explicitly says saved / do not repeat; no false failed-write notice.
- Rejections preserve all entered fields and safe server message text, with nonblank fallback for invalid/empty response bodies. Network/5xx outcomes remain unconfirmed with check-before-retry guidance; no automatic retry.
- Synchronous ref guard covers write and readback, including duplicate calls through the same stale handler. Inputs, selects, date/time fieldsets and submit are disabled while pending and restored in finally.
- Pure workspace fetch and scoped initial effect eliminate the captured loader dependency without suppression or state-driven reload loops.

## Preserved contracts and limits

Existing title/type/branch/venue/capacity/notes payload, local wall-time-to-UTC conversion, default Recital/10:00/11:00, null optional fields and four-field reset retained. Type/branch/time remain selected after save. No backend, status-action UI, permissions, module access, SQL schema, timezone policy or shared visual changes. Initial response checks retained. The page displays Asia/Kolkata but creates dates using the browser's local timezone; this pre-existing behavior is explicitly preserved, not newly accepted as an academy-timezone policy. Browser receipt pins Los Angeles and checks exact existing UTC conversion. A policy change requires a separate domain/timezone review.

## Evidence

- Frozen 15-case actual-TSX controlled suite against HEAD: **2 PASS / 13 FAIL**. Final unchanged assertions: **15/15 PASS**. Includes HTTP400/403/500/503, malformed/blank/nonstring messages, network failure, saved/readback failure, payload/reset, optional defaults, write/readback stale-handler guards and mount request counts.
- Exported DOM/browser: **16/16 PASS**, widths320/1440, light/dark, normal201 response / refresh503 / rejected400 / uncertain-but-fixture-committed500. Exact request, input reset/retention, one POST, fixture rows, restored controls, no horizontal overflow, no unexpected console/hydration errors. Earlier16 also passed with empty200 success before matching controller201 response in final runner.
- Final rebuilt-export browser receipt: local `QA/EVIDENCE/events-feedback-browser-1791571826957/result.json`; screenshots alongside it. The prior201-response run `1791571747225` also passed16/16 and its320-light-normal screenshot was visually inspected: labels/fields stacked, confirmation and directory visible, no clipping. Baseline/final controlled logs remain local; QA/EVIDENCE not staged. Deliberate HTTP fault resource errors are distinguished from unexpected errors.
- Events ESLint **0 errors/0 warnings**, TypeScript PASS, production webpack/static export **83/83 pages PASS**. `git diff --check` PASS.

Synthetic transport is not fresh Identity/HTTP-SQL or physical Android/iOS acceptance. No enterprise-suite rerun, issue closure, Azure deploy or main merge. BUG-FUNC-0003, linked/device/critical/release gates remain OPEN. Mini saved86/94 CONTRACT READY / ENGINE BLOCKED and existing qualification handoff unchanged; unrelated work preserved.

Next: **Sol Medium** Communication Settings existing-feedback gap check; reuse its current reconciliation/pending guards rather than redo them. **Sol High** if domain, credential, authority or persistence changes are required. Continue the existing enterprise program, not a new plan.
