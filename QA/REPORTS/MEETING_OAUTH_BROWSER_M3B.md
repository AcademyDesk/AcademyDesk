# M3b — approved registration and internal browser/callback guards

2026-10-15 · Recommended route: Sol High / Codex · Local/Docker only.
Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work.

## Scope and result

Completed the next **internal** connection-security slice for GoogleWorkspace, Microsoft365 and Zoom. This is NOT public OAuth initiation/callback, an actual reauthentication/resume flow, token exchange, a connected provider account, meeting execution or production acceptance. No provider credentials, registrations or customer accounts were accessed. Connection controls stay disabled and readiness capabilities stay false.

- `MeetingOAuthRegistrationRegistry`: server-owned, disabled by default; known providers, positive configuration revision, bounded client identifiers, canonical HTTPS origins and exact provider-specific callback/Workspace-return paths. Reject incomplete/duplicate/invalid configuration as a whole. No request/model/draft-derived origin, arbitrary provider endpoint, secret input or redirect target.
- Registration fingerprint binds version/provider/revision/client/callback/return. The explicitly **unqualified** registration profile does not approve scopes or token endpoints. Before adapter activation, include its reviewed scope/flow/profile in a new versioned fingerprint so pending attempts cannot silently inherit changed authority.
- `MeetingOAuthBrowserBridge`: internal/no DI/no routes. Authenticated POST with exact API host, TLS and approved Workspace Origin prepares the existing atomic intent and an independently protected, five-minute, attempt-specific `__Host-` cookie: Secure, HttpOnly, Path=/, no Domain, SameSite=Lax. State/browser correlation is not a bearer credential or role grant.
- Authenticated callback primitive checks exact TLS host/path, GET, bounded exact-case query keys, single values, code/error exclusivity including empty mixed parameters, one correlation cookie, protected purpose/provider/academy/state/registration/expiry and current Identity authorization through the existing store. State is claimed atomically with audit; success deletes the cookie and remains `VerificationRequired`, never `Connected`.
- Anonymous callbacks return `ReauthenticationRequired` without consuming the attempt. **Ordinary provider redirects do not carry Academy's browser bearer token.** The future public adapter still needs reviewed encrypted callback capture and authenticated resume, with lifecycle/replay limits. These direct service tests do not demonstrate that missing flow.
- Authorization code, state and verifier are server-only and omitted from JSON/ToString. Provider error descriptions/URLs are ignored, never rendered or followed. All responses set no-store/no-cache/no-referrer. Valid provider denial consumes the attempt, returns only a bounded rejection code and never exposes exchange material.

No database schema, Program/DI registration, controller routes, frontend, PENTA engine or tool contract changed. Existing store authorization, transaction/generation/concurrency/audit rules are reused, not replaced.

## Validation

| Check | Final evidence |
| --- | --- |
| Focused API/security tests | **294/294 PASS**, 0 failed/skipped; 55 new registry/browser tests plus 239 retained relevant tests. `QA/EVIDENCE/meeting-oauth-browser-20261015/qualified-final.trx` |
| Isolated SQL harness build | PASS, 0 warnings/errors; completed before runner startup |
| Run-owned SQL runner | `328b03bcb625418b8369df775670e66f`, loopback SQL port57591, exit0,105.9seconds |
| New internal bridge/SQL/Identity cases | **21/21 PASS**: each provider secure cookie, anonymous rejection, missing-cookie rejection, duplicate-state rejection, current-authenticated claim-only, replay denial and durable single claim/two audits/no connection |
| Retained current-source SQL suites | 21 readiness +20 draft +17 receipt +20 internal operation +32 internal OAuth +32 admission =142 PASS; with21 new =**163 named cases**. Of these,90 are actual HTTP/SQL cases and73 direct SQL/internal-service cases. New cookie cases use synthetic HttpContext, NOT real browser/public OAuth HTTP |
| Persistence safeguards | Receipt/operation/OAuth migration upgrades, populated-down refusal, empty down/up; old synchronized claim/reconnect races and commit-fault rollback retained and rerun |
| Runtime/source provenance | 93 application/7 Identity migrations;333 routes/322 controller method-routes/10 Identity; route SHA `3CA8D1FABA64A043749774D75B9EE01ABC90DA1428443AE2C2C7517BE1477728`. All7 receipts' source hashes match final sources |
| Loaded harness artifact | API source-build DLL and harness-copy SHA both `8B7309D5392B6D3FAB5C3A5E36847A061CB092C160D065ECFA3E9A2C48571C0C` |
| EF consistency | No model changes since migration; existing PassingPercent/AdjustedAmount precision warnings remain, not introduced or resolved here |
| Cleanup | Runner proved exact owned SQL target/runtime login and mismatched-marker cleanup refusal; owned database/login/container removed. Existing stopped containers and both PENTA containers untouched |

Local SQL result receipts: `QA/EVIDENCE/meeting-{readiness,preview,receipt,operation,oauth,oauth-browser,admission}-sql-328b03bcb625418b8369df775670e66f/result.json`. No raw authorization material written into these receipts.

Exploratory test failures were fixture assertions, retained in earlier TRX files: empty StringValues was implicitly cast to null by Assert.Empty, and framework cookie deletion uses expired empty cookie rather than a required max-age=0. Corrected to explicit header-count and parsed cookie-name/value/expiry/security checks. No authority/state/replay assertion weakened. The final294-case pass supersedes those exploratory runs; no failed run is reported as accepted.

No frontend files changed; prior115 focused/84 synthetic frontend browser cases retained at their recorded evidence level, not rerun. No physical device, real provider, actual callback-browser or whole-enterprise acceptance claimed.

## Provider protocol review

Checked official [Google web-server OAuth guidance](https://developers.google.com/identity/protocols/oauth2/web-server), [Microsoft authorization-code flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow) and [Zoom OAuth/PKCE guidance](https://developers.zoom.us/docs/integrations/oauth/). Exact registered callbacks and state/PKCE bindings informed the guards. Provider-specific client type/scopes/consent/token/account verification remain adapter qualification, not a generic HTTP-200 shortcut. In particular, do not assume Zoom's public PKCE flow and a confidential registration are interchangeable. This batch does not exchange codes or build authorization URLs.

## Next / production position

Stay **Sol High** for actual authenticated callback capture/reauthentication/resume and qualified provider profiles/token exchange/account verification/secure token lifecycle for ALL THREE. Then refresh/revoke/confirmed disconnect, real execution approval/atomic admission/dispatch/reconciliation/history and Manual/PENTA integration. Do not ask for a first-provider choice again or enable the UI from these helper tests. No current external account/access/secret/deployment authority.

The [production remaining report](PRODUCTION_REMAINING_20261015.md) consolidates the existing queue into8 workstreams across3 unfinished delivery groups, not8 small fixes or guaranteed turns. Manual full-page/state/device acceptance, real UI2/qualified Mini/operational conversations/intelligence, enterprise/runtime/security/privacy/operations and exact-release approval remain open. No issue closed, main edit, stage/commit/push/Azure/prod-data change or project switch.
