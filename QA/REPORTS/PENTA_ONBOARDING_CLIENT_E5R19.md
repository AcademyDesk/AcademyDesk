# E5-R19 — one-shot authenticated draft-turn client

18 October2026. Continuation of E5-R18/E5-R17 and the existing enterprise program.
Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Existing dirty work preserved.
No primary-main, backend, database, PENTA engine, model, flag or deployment changes.

## Delivered / precise boundary

- [x] New `apps/web/src/lib/penta-onboarding-channel.ts`: fixed AcademyAdmin
  authenticated one-shot `/penta/onboarding/conversations/{id}/turns` transport.
  IDs/current host binding checked, exact five-field POST, no model roles/tenant
  authority, arbitrary tool endpoints, approval or execution dispatch.
- [x] One POST per channel, never reset after a lost/malformed/denied response.
  Explicit same-request GET `/claims/{requestId}/result` for recovery, including
  GET-only hydration from an authenticated pending cursor after reload. Caller
  must resolve current host binding first; local cursor alone is not authority.
- [x] Pin Admin bearer/workspace; refuse borrowing its token from Teacher/Student/
  Platform routes and reject late responses after cross-portal navigation. Reject
  changed credentials and delivered local/
  cross-tab logout (even when the same token is restored). Dispose aborts waiting
  and removes listeners. Stop waiting does not mean cancel/rollback on the host.
- [x] Separate single-fetch path: no authentication-refresh replay for this turn.
  The existing common helper and its legitimate renewal behavior are unchanged.
  No automatic request retry/polling, cookies, caching, redirects or referrer.
- [x] Bounded64KiB JSON stream, strict conversation/request/version/draft/scope/
  tool/status/operation binding; no accepted execution grant, fabricated success,
  stale/foreign identity, hidden diagnostic or raw field/operation/URL forwarding.
  Returned minimized summary always has canExecute=false/domainEffect=false.

This is an **unmounted client adapter**, not the complete shared UI/projection or
approval adapter. Summaries are not operational-ui Projections or domain receipts;
the full raw snapshot/catalogue/value contract has NOT been accepted by this client.
Callers must not derive a permission, action button, approval or success from it.
The browser checks supplement, never replace host current authorization/SQL/audit.

## Verification

```text
node QA/tools/penta-onboarding-channel.test.cjs
node QA/tools/penta-chat-contract.test.cjs
node QA/tools/session-refresh.test.cjs
node QA/tools/refresh-concurrency.test.cjs
# apps/web
node_modules/.bin/tsc --noEmit --incremental false
node_modules/.bin/eslint src/lib/penta-onboarding-channel.ts
```

- **72/72 new checks PASS**, including one owned actual loopback HTTP server:
  native fetch sends one authenticated POST, gets503, recovers with the same-ID
  GET200. Synthetic server/data only; server is closed in finally. Other tests
  cover each supported neutral/lifecycle/partial outcome, nine HTTP failure codes,
  scope/receipt corruption, false approval/success, stream/JSON bounds, logout,
  caller-abort/late provider delivery, pinned input, duplicate-submit rejection,
  explicit pending202 and post-reload GET-only recovery.
- Retained read contract **57/57**, session **37/37**, refresh concurrency **20/20**
  PASS. TypeScript and focused ESLint PASS. Diff check PASS.
- Final receipt `QA/EVIDENCE/penta-onboarding-channel-20261018/client-final3.json`,
  SHA256 `141a43242ce1a3936979cf85d01677692161a31852d32d4c39bccc327f7a0fd0`.
  Earlier69/70-case passing receipts retained, superseded after reload and route guards.
- Retained read receipt `read-contract-retained.json` in the same directory,
  SHA256 `2aa76fabfd3e18a1e0aa6355db74ceecf06a6fc3c24db1afb3452257fb365866`.
- New client SHA256 `266888622bca7b270df0a1fbb6e1112f597134a56d6c492e1697da9cce8e92d0`.
  New test SHA256 `083496b2b94bebefefd1496a6e704cb51d631279fc36427ed17e4f76703ca636`.

No real SQL/.NET/model/browser/physical-device acceptance was rerun or claimed.
Prior400 focused .NET/422SQL/347selected Python evidence is retained, not fresh
proof of this client. Backend wire types were inspected: actual tool is
`AcademyDesk.StudentOnboarding.Create`, host prepares awaiting_approval without
approval_ref, and partial/no-change revisions differ. Synthetic fixtures follow
these declarations; actual paired client-to-host SQL integration remains open.

## Next / still open

- [ ] Authenticated bootstrap/current selection and pending cursor hydration from
  real APIs, not client-supplied role/tenant IDs or invented conversation state.
- [ ] Import exact pinned shared operational UI with unchanged sibling foundation;
  build full validated current Projection and host intent adapter. Preserve the
  existing read chat/Manual fallback; do not expose a synthetic gallery or model
  selector. No full UI or layout changes were delivered by this adapter alone.
- [ ] Host-issued preview/challenge/confirmation and once-only execution/result
  adapters, explicit pending/unknown reconciliation, real disposable SQL/HTTP and
  authenticated browser/device tests. Never map chat text to an execution grant.
- [ ] CORE-01 measured exact-model correctness/latency and separately approved
  profile/resources. E5-R18 candidate is inactive, not improved accuracy evidence.
- [ ] Full other families/roles/languages, Manual light/dark/device parity, provider/
  privacy/security/operations/enterprise and exact pre-Azure release gates.
  BUG-DATA-0001 stays OPEN;90% conversational coverage remains a target.

Next route: Sol High for authenticated projection/intent/approval integration;
Medium only after contracts are settled for bounded visual refinements. No Mini
contract change or external project handoff is required for this client seam.
No commit/push/Azure/model activation. Production NOT READY. Evidence stays local,
.build-check ignored, unrelated work/history intact.
