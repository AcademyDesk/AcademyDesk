# Communication Preferences feedback repair — 2026-10-10

BUG-FUNC-0003, starting `5935e18a4a0979fcf334f8ece57912c4f302ddd0`, `codex/penta-search`, `D:\AcademyDesk-codex-p0`. Existing narrow lookup/consent repair reused, not a repeated authority audit.

## Bounded repair

Durable polite `Contact preferences saved.` survives refreshed data. Successful save followed by failed readback explicitly says saved / do not repeat, retaining the current consent draft. HTTP rejection preserves entered values and safe nonblank server guidance; malformed/empty/nonstring messages use the existing fallback. Network/5xx remain unconfirmed with reload/check-before-retry guidance; no automatic retry. Synchronous pending ref covers write and readback, blocking stale-handler duplicate submits. Compose fieldset and save button disable while pending, then restore in finally. Notes textarea has a stable accessible name.

Pure narrow workspace fetch and scoped mount effect remove the captured loader dependency. Contact-selection hydration moves from a synchronous set-state effect to selector event handlers; successful readback explicitly hydrates the selected contact from returned preferences. Existing selected consent/defaults, type-change recipient clearing and notes behavior preserved without lint suppression or state-driven extra GETs. No contact reset after save: current recipient stays selected.

Only preference page application code changed. `/communication-preferences/recipients` remains the narrow lookup; no broad Students/Guardians GET. Parent/Student types, default false/new-contact flags, complete emailAllowed/whatsAppAllowed/marketingAllowed/notes PUT, optional empty notes, saved-preference hydration and server authority remain unchanged. No API/DTO/schema/Manager permission/consent policy/provider/notification/send change.

## Evidence

- **28 controlled actual-TSX checks**:10 original lookup/consent assertion bodies unchanged plus18 feedback/lifecycle/selection controls. Frozen HEAD baseline **12 PASS /16 FAIL**; final **28/28 PASS**. Covers Parent/Student selection, exact payload, retained contact and draft, empty successful body, readback failure, HTTP400/403/500/503, network uncertainty, invalid rejection messages, write/readback stale-handler duplicates and stable mount request counts. Harness adds ref/deferred/fault options only. Baseline/final logs local in QA/EVIDENCE.
- **32/32 exported browser cases PASS**:320/1440 × light/dark × Guardian/Student × normal200/readback503/rejected400/uncertain-but-fixture-committed500. Actual React/select/checkbox/textarea DOM, saved-contact/default hydration, explicit opt-out edits, exact PUT, one write, retained contact/values, restored fieldset, polite notice, no broad people GET, no horizontal overflow or unexpected console/hydration errors. Deliberate HTTP resource errors separated. Local receipt `QA/EVIDENCE/preferences-feedback-browser-1791573598177/result.json`; screenshots alongside. Synthetic transport is not real SQL/auth/consent-delivery acceptance.
- Initial browser run `1791573559453` failed because the harness waited for a hidden select backing input to become visible. Corrected to attached-state readiness, leaving all option/value/consent assertions unchanged; failed evidence retained. No app fix for this harness error.
- ESLint baseline **1 error/1 warning**, final **0/0**, no suppression. TypeScript PASS; production webpack/static export **83/83 PASS**; diff checks PASS.

Prior Manager/authority **74 HTTP-SQL /609 backend** acceptance is historical, not rerun. No new live SQL/container/backend suite, physical Android/iOS, revocation race, cross-device persistence, audit-fault rollback, linked/critical/release acceptance. BUG-FUNC-0003 and related BUG-FUNC-0028 remain OPEN. QA/EVIDENCE untracked; unrelated work preserved; main/Mini/Azure unchanged. Mini saved86/94 CONTRACT READY / ENGINE BLOCKED and existing qualification handoff unchanged.

Next **Sol Medium**: Assignments bounded feedback gap check, preserving existing batch/teacher/student policy and publication defaults. **Sol High** if authority, learning-domain policy or persistence changes are required. Continue existing enterprise program, not a new plan.
