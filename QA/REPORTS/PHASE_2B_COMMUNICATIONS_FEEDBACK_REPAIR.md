# Communications feedback repair — 2026-10-10

BUG-FUNC-0003, starting `63b556bbf1b003054fd2c40d7fdd0fd43f266e40`, `codex/penta-search`, `D:\AcademyDesk-codex-p0`. Existing delivery-channel repair reused, not audited/reimplemented anew.

## Confirmed gaps and bounded repair

- Initial retained50 actual-TSX channel-choice cases PASS. Existing template detach/availability/channel guards, canonical InApp banner, audience-only variables and recipient clearing remain unchanged.
- Save confirmation was erased by readback; direct empty-success body dereferenced null; write/readback errors lacked safe distinction and repeated submits were unguarded. Now durable polite status, safe optional status fallback, retained rejected/unconfirmed draft, saved-but-readback-failed guidance and synchronous pending ref cover direct messages and banners. No automatic retry; 5xx/network advise checking the message log first. Safe nonblank rejection guidance retained.
- Existing nine-field reset after confirmed save retained. Message type/channel/duration/audience defaults and local wall-time conversion unchanged. Shared fieldset disables compose controls and button while write/readback pending, restored in finally. Pure fetch/scoped initial effect removes inherited load dependency warning without suppression or reload loop.
- Browser error-state diagnostic proved textarea label text included `Synthetic body` after rerender, making its accessible name unstable. Explicit `aria-label="Message"` retains the visible label and fixes field identification without changing its value or payload.

No API/DTO/schema/permission/provider/dispatch/consent/template policy or notification lifecycle change. No real email/WhatsApp/portal send. UI response status is displayed, not proof of external delivery. Pre-existing announcement confirmation wording and schedule timezone behavior retained; their broader policy/lifecycle acceptance remains separate.

## Validation

- Frozen unchanged72 controls on HEAD: **51 PASS /21 FAIL** =50 retained channel passes plus1 mount lifecycle pass;21 new feedback failures. Final **72/72 PASS** =50 original assertions +22 feedback controls. Faults400/403/500/503, network, empty successful body, malformed/blank/nonstring rejection, direct/banner payload/reset, saved/readback failure, stale-handler duplicates through write/readback and request lifecycle. Harness adds refs/fault/deferred options; original50 assertion bodies unchanged. Local baseline/final logs retained under QA/EVIDENCE.
- **32/32 exported-DOM browser cases PASS**:320/1440 × light/dark × direct/banner × normal201/readback503/rejected400/uncertain-but-fixture-committed500. Exact payload, one POST, state reset/retention, saved-fixture count, restored fieldset, polite notice, no horizontal overflow or unexpected console/hydration error. Deliberate HTTP resource errors separated. Local receipt `QA/EVIDENCE/communications-feedback-browser-1791572845907/result.json`; screenshots alongside.
- Earlier browser runs `1791572714487`, `1791572767016`, `1791572801663` retain Message-label timeout evidence. Third includes DOM diagnostic/screenshot identifying changed label content. Not counted as acceptance; no weakened locator or retention assertion. An intermediate export overlapped a failed run; final acceptance uses the repaired label and final export, not earlier screenshots.
- ESLint baseline0 errors/1 inherited dependency warning, final **0/0**; TypeScript PASS; production webpack/static export **83/83 PASS**; diff checks PASS.

Prior real Identity/HTTP/SQL56 and backend557 delivery-channel evidence is historical, not rerun. Synthetic browser transport is not live SQL/auth/device/provider acceptance. Physical Android/iOS, linked workflows, notification dispatch/retry/cancel/read-state, timezone policy, real-server concurrency and critical/release remain OPEN. BUG-FUNC-0003 remains OPEN. Main/Mini/Azure/unrelated changes/evidence untouched; Mini saved86/94 CONTRACT READY / ENGINE BLOCKED unchanged.

Next **Sol Medium**: Communication Preferences bounded feedback gap check, reuse existing preference/consent behavior. **Sol High** if consent policy, authority, external-send or persistence changes are needed. Continue existing enterprise program, not a new plan.
