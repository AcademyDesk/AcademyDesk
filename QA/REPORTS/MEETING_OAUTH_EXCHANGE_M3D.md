# M3d — provider flow profiles and inactive code-exchange boundary

2026-10-15. Recommended route: Sol High / Codex. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work. No production credentials or external provider accounts accessed.

## Delivered

- Code-owned flow-profile candidates for all three providers. These are reviewed protocol mappings, **not provider/account qualification or deployment approval**. No arbitrary endpoint/scope/issuer URL from prompts, channel drafts or browser bodies.
- Google: confidential web/PKCE, owned-calendar event scope and OpenID/email account-read preparation. Microsoft: confidential delegated work/school meeting flow, explicit canonical nonempty directory GUID, User.Read/OnlineMeetings.ReadWrite/offline_access. Broad common/organizations/consumers aliases, personal-account/application-permission shortcuts and URL tenants are rejected. Zoom: explicitly public-client S256 PKCE with no client secret/Basic header, granular own-user/create/read meeting scopes. Update/delete/list/recording permissions are not silently granted by this create/read profile.
- Registration `/0.2-profile` fingerprint binds profile version, flow, directory, fixed authorize/token/account endpoints, requested/required scopes and registration/callback/return/revision/client. Original `/0.1-unqualified` fingerprint bytes remain stable; legacy registrations cannot exchange. Profile transitions reject old protected browser state without consuming it.
- Inactive internal exchange service accepts only the existing successfully resumed server claim. Its private handoff now carries academy/actor/binding/version/generation/registration/original expiry/security-stamp digest; all remain JsonIgnored. It independently checks current host authority and binding before request, then rechecks fresh Identity/role/stamp and binding after simulated token receipt.
- Uses fixed POST endpoint, form-encoded code/verifier/client/redirect and provider-appropriate secret/scope, never secrets in request URL. Supplied client secret remains a server-only parameter pending an approved vault/rotation resolver, never a chat/UI field. Transport factory disables redirects, cookies, proxy and decompression, retains normal TLS validation, and bounds connect/header/request/body handling. No application retry; timeout/network/redirect/provider-error/malformed response becomes non-retryable unknown, not Connected.
- Token parsing rejects oversized/chunked-over-limit/deep/malformed or duplicate-key JSON, invalid bearer syntax/type/expiry, absent refresh credentials, missing/partial/duplicate/extra privileges and unreviewed Zoom api_url. Bounded unrelated response fields may be ignored, never used as authority or outbound destinations. ID tokens are not decoded/trusted for host or account identity.
- Token material cannot serialize or print through the result. A structurally accepted result says **TokensReceivedAccountUnverified** only. No token is persisted, no account probe is sent and no Connected label is possible in the existing schema.
- One handed-off material object can initiate only one exchange, including simultaneous calls and failed/uncertain responses. This memory fence is explicitly NOT durable cross-process exchange admission. A durable coordinator/intent/audit/unknown-outcome recovery boundary is required before any production call site is enabled.

No DI registration/public routes/connection UI activation, SQL schema/migration, frontend, Mini engine or shared tool-contract change. Existing finance/roles/domain operations remain unchanged. The exchange service has no production call sites; all exercised HTTP messages use a deterministic in-memory fake handler, not provider network access.

## Verification

- **394/394 focused tests PASS**,0 failed/skipped:72 new profile/exchange cases plus322 retained relevant cases. Receipt `QA/EVIDENCE/meeting-oauth-exchange-20261015/exchange-qualified.trx`.
- New cases cover each provider's exact request/secret handling, valid host capture/resume claim, redaction, profile/tenant fingerprint changes and old-callback rejection, host denial/revocation/stamp/expiry/generation, duplicate send fence, post-response authority changes, redirects/429/network/cancellation/HTML/encoding/known and unknown-length oversized bodies, parser shape/duplicates/expiry/scopes/bearer safety. Testing-host DI assertion confirms exchange remains unregistered.
- Final isolated API/harness build PASS,0 warnings/errors; waited for final build completion before runner. API source-build DLL and harness copied DLL SHA both`43032B5FA1BB6095164666E939B8CDC5197BD65C62B71D5AEF1B6BC5B43482B6`. EF model consistency PASS,existing2decimalprecisionwarnings unchanged;no migration added. Tracked diff/newfile whitespace checks PASS.
- Retained **203/203 named SQL-run cases PASS**(90actualHTTP/SQL+113directSQL/internal),0newSQLcases:current readiness/draft/receipt/operation/OAuth/browser/resume/admission suites rerun at finalsources;32HTTPadmission cases preserve all OAuth tables inno-write snapshots. Finalrun`945cc817ddba44b29859b9aec7fdf3db`,loopbackSQL62006,exit0,134.4seconds,94app/7Identitymigrations. All4 migrationguard suites andexistingrace/auditrollback controls PASS,no429/provider calls.
- All8receipt sourcehashes match current sources:`QA/EVIDENCE/meeting-{readiness,preview,receipt,operation,oauth,oauth-browser,oauth-resume,admission}-sql-945cc817ddba44b29859b9aec7fdf3db/result.json`. ExactownedSQL target/login,mismatchedmarker cleanup refusal andowned database/login/container removal verified;onlybothPENTAcontainers remain running,unrelatedcontainers untouched.
- New profile/exchange proof is fakeHTTP+realhostIdentity/InMemory unit coverage,NOT live provider/realSQL exchange admission. Existing SQL pass protects retained store behavior,not the new provider exchange's live interoperability. No whole-enterprise or real provider/browser/device acceptance follows from these checks.

Final new-source provenance:

| Source | SHA256 |
| --- | --- |
| `MeetingOAuthFlowProfile.cs` | `A30FA31ACB73C9E8DB5206D8C27325F9A4E66FA35D75340235F6BD3665ECFB69` |
| `MeetingOAuthCodeExchange.cs` | `19A8319AA597A35010E881DD89B871D42FFA59C7AB534C8B96CB9F71CA51BFA2` |
| `MeetingOAuthRegistrationRegistry.cs` | `2F799F7BA423FAAD038B96BD223224EC7B694FE10D7DA299198D2A857D6B8037` |
| `MeetingOAuthAttemptStore.cs` | `F5819C5E7FAD6F216D54CFC8FB2A7F549540CEE438C4A9133850AD49F338D93B` |
| `MeetingOAuthExchangeTests.cs` | `237EA69BBFB31B1557EB1422BF0586507EDA0045401BF545F80D3BC02700F3AE` |

No finalsource unit failure:earlier386 and391 exploratory passing runs preceded additional bearer/chunked/concurrency/profile-transition checks;final394supersedes them without assertionremoval. Nofrontendedits;previousfrontendproofretained,notrerun. Main remains`f7af512f884ca5fe8ac3ddb287c852492592e709`,worktreeHEADunchanged,nothingstaged/committed.

## Protocol review and deliberate limits

Primary sources reviewed: [Google web-server OAuth](https://developers.google.com/identity/protocols/oauth2/web-server), [Calendar event insertion scopes](https://developers.google.com/workspace/calendar/api/v3/reference/events/insert), [Microsoft code flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow), [delegated online meetings](https://learn.microsoft.com/en-us/graph/api/application-post-onlinemeetings?view=graph-rest-1.0), [Zoom public PKCE](https://developers.zoom.us/docs/integrations/oauth/), [Zoom own-user lookup](https://developers.zoom.us/docs/api/users/) and [meeting granular scopes](https://developers.zoom.us/docs/api/meetings/).

This conservative first durable-connection candidate requires an explicit accepted scope list and refresh token. Providers may legally omit scopes or a previously issued refresh token; such a response currently remains unavailable, never assumed sufficient. Existing verified credential reuse, incremental consent, certificate/assertion client authentication, other account types/clouds and provider-specific expiry variants require separate reviewed lifecycle support. No live application registration, consent/license review or SDK/live interoperability proof has occurred. Zoom's public-client registration must explicitly be enabled; confidential credentials cannot be substituted.

The approved Microsoft directory must eventually be resolved from the academy's authorized connection setup, not a model/client tenant string or one hard-coded directory imposed on every customer. The registry remains an unregistered server-owned value object; no multi-academy configuration resolver or client-credential loader is supplied by this slice. Broad tenant aliases are deliberately not used to bypass that missing ownership/verification step.

## Next route / unchanged gates

Stay Sol High: durable exchange admission/audit/one-time cross-process fence and unknown-outcome handling; private token protection/storage/generation/rotation; independent provider account verification and confirmed per-academy binding; refresh/revoke/disconnect. Then approved registration setup and real browser/public callback/login qualification, meeting execution approval/admission/dispatch/reconciliation/history and Manual/PENTA integration. Existing expired-code retention/key-ring/ingress/rate-limit/security/privacy/device/exact-release gates remain open. Real token exchange cannot be enabled merely by registering this helper.

Existing [8workstreams/3unfinished groups](PRODUCTION_REMAINING_20261015.md) unchanged. This is progress inside provider integration, not a completed integration or production readiness. Owner full app→accepted/authorized go-live→Hayansh→product website sequence retained. No main/stage/commit/push/Azure/production-data or credential-store changes, no project switch; prior dirty work/evidence preserved.
