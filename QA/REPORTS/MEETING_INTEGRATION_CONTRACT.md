# Meeting host integration — contract and execution packet

2026-10-15 · `meeting-host/0.1-draft` · Sol High · local planning only.
Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`.

Latest[M3d](MEETING_OAUTH_EXCHANGE_M3D.md):all3codeownedproviderflow/scopes/tenant/endpointsprofiles inregistrationfingerprint,inactivehost-bound tokenexchange/strictredactedresponsechecks.394focusedtests PASS(72new);fakeHTTP only,nocredentials/realprovider/DI/routes/Connected. Nextdurableexchangeintent/audit/fence/unknownoutcome+securestorage/accountverification/lifecycle/publicbrowserbeforeactivation;existingreleasegatesretained.

Latest[M3c](MEETING_OAUTH_RESUME_M3C.md) adds internal protectedcallbackcapture andauthenticatedresume/currentauthority/singleconsume/atomic audit/codeerasure for all3.322focusedtests PASS;SQLqualification inreport. Not publicroute/realbrowserreauth/tokenexchange/Connected;nextqualifiedproviderprofiles/account/tokenstorage/lifecycle andpublicadapter/browser/retention/recovery gates beforeactivation.

Current successor[M3b](MEETING_OAUTH_BROWSER_M3B.md):internal approved-registration/exactHTTPS callback-return/browsercookie/strict authenticated callback guards now tested for all3;no publicroutes,actual reauthentication/resume,token exchange orconnectedaccounts. NextHigh must implement secure callbackcapture/reauth/resume andqualified providerprofiles/scopes/account/token/storage lifecycle before UI activation.294focusedtests/163namedcases PASS,including21newdirectbridge/syntheticHttpContext+realSQL. [Production remaining](PRODUCTION_REMAINING_20261015.md) maps8workstreams/3unfinishedgroups;no releaseclearance.

Current secure connection foundation: [M3a OAuth intents/bindings](MEETING_OAUTH_INTENTS_M3A.md). All3provider records and protected expiring single-use state/PKCE/generation/actor-browser-registration binding now have realSQL race/rollback proof. No public OAuth initiation/callback/token exchange or connected account is enabled. Continue approved registration/callback/correlation/reauth plus provider-specific account verification, not another provider-selection step. Earlier source findings below remain historical.

Owner decision2026-10-15: support ALL THREE Google Meet, Microsoft Teams and Zoom with independent in-app integration options. [Current UI and connection queue](MEETING_THREE_PROVIDER_OPTIONS.md). No provider-choice question remains. All three controls are presently disabled; secure OAuth/account lifecycle and per-academy/provider bindings/adapters remain unimplemented. A single legacy Meeting draft is not three connected accounts. Sequential adapter qualification remains a safety process, not a reduced one-provider product scope; no external access/credential/deployment authority granted.

Current successor: [M2e](MEETING_ADMISSION_M2E.md) implements protected draft receipts, internal durable persistence/recovery, a denial-only create-request boundary and private stored-operation status. No provider/real execution approval/successful admission/worker is enabled. Earlier source findings and proposed endpoints below are historical planning, not a statement that every endpoint is still absent or that the proposed full workflow is delivered. First provider target is awaiting owner choice; no account access or sends authorized.

This is a proposed Academy Desk domain/API contract, not provider integration, a PENTA tool contract change or release approval. Subsequent checkpoint:[M1 implementation](MEETING_READINESS_M1.md) adds only the protected read-only capabilities endpoint with all operations unavailable. Other endpoints remain unimplemented. Preserve the existing enterprise program and Manual/PENTA parity goal. Do not enable the draft screen from this document.

## 1. Repository findings and reuse boundary

| Existing source | Verified responsibility | Not supplied by it |
| --- | --- | --- |
| `apps/web/src/app/meeting-links/page.tsx` | Draft fields, Online/Hybrid/no-valid-link eligibility, Active enrolled invitees, GET-only source loading | No create/update/cancel request; history state never populated |
| `apps/api/Controllers/CommunicationSettingsController.cs` | Tenant-scoped Owner/AcademyAdmin list/save, configured-field checks, channel audit | No OAuth callback/token resolver/connection verification; configuration does not create meetings |
| `apps/api/Domain/Entities/CommunicationChannel.cs` | Provider identity/reference, status, `HasSecureConnection` marker | No credential reference, verification expiry, provider account ownership or revocation generation |
| `apps/api/Security/AcademyAccessFilter.cs` | Current server user/academy/module, role/grant boundary, mutation audit, opt-in atomic local transactions | A future controller needs explicit Engagement mapping and its own Owner/Admin/resource checks; generic filter is not provider authorization |
| `apps/api/Security/SubscriptionPlanCatalog.cs` | Existing communication controllers map to Engagement | No current Meetings controller mapping |
| `Batch`, `Enrollment`, `Teacher` entities | Academy-owned batch/link/delivery/active status, memberships and teacher identities | No provider event/operation/history persistence |
| `BatchesController` / `MakeupClassesController` | Existing manually supplied links; Offline venue vs Online/Hybrid link | No provider-event creation; must not rewrite these workflows |
| `NotificationsController` | Notification records, consent/connection checks, queued/blocked status | Not proof of a functioning external invitation or message delivery worker |

Source searches found no dedicated meeting/provider implementation under the current API. The only non-migration `HasSecureConnection` uses found are configuration/summary and notification readiness checks, not a secure connection lifecycle. No credentials were inspected. Existing configuration summaries and hidden optional fields remain compatible; never repurpose `messagesEnabled` as meeting execution permission.

## 2. Authority and readiness

1. Resolve actor from current Identity store, derive its academy server-side, and compare the route. Initially retain existing channel Owner/AcademyAdmin-only access. A platform-owner flag alone does not grant meeting access to another academy. No Teacher/Student/Manager/custom-grant broadening in this packet.
2. Enforce active academy and Engagement entitlement through the existing filter plus explicit domain checks. New controller methods must not rely on frontend navigation or model roles. Recheck actor/tenant/entitlement immediately before dispatch and before returning protected history/receipts.
3. Resolve every batch, organizer, enrollment and attendee within that academy. Require a current active eligible Online/Hybrid batch without a conflicting existing URL and active enrolled invitees; define any exceptional trial/guest path separately. Current UI's all-batches draft selection is not authority to invite someone.
4. Provider readiness requires an implemented adapter, current authorized provider-account binding, secure credential reference, granted scopes/capabilities, usable credential lifetime, revocation/configuration generation and supported requested settings. Stored `Configured`/`HasSecureConnection` alone is insufficient. Default unavailable if any proof is absent or cannot be checked.
5. Configuration/reference fields, prompts and arbitrary URLs must never become token endpoints or outbound destinations. Fixed reviewed adapter endpoints only; credentials stay in an approved secret mechanism, outside SQL summaries/browser/chat/audit. Do not retrieve/deploy secrets in local planning.
6. PENTA is not enabled for meeting writes. A later tool must call the same host preview/approval/execution service; no model-supplied permissions/tenant/approval flags. A genuine shared tool-contract change uses the established Mini handoff before activation, not a new engine.

## 3. Endpoints — M1 capabilities and non-executable draft preparation

Successor [M2b](MEETING_DRAFT_HTTP_M2B.md) adds authorized `POST /draft-preview` for audited preparation only. It is not the proposed signed/approved `/preview` below;qualified provider operations still report unavailable. No creation orhistory route enabled.

All routes below are under `/api/academies/{academyId}/meetings`, authenticated and server-tenant-resolved. Return bounded non-secret reason codes. Existing 401/403 authority semantics remain; never disclose foreign resource details in denial bodies.

| Endpoint | Contract |
| --- | --- |
| `GET /capabilities` | Read-only readiness. Per-provider implemented/available flags, permitted operations, bounded unavailable reasons, supported settings, `asOfUtc`; no credential material or attendee records |
| `POST /preview` | No provider writes/invites. Strict bounded DTO validation and source authorization; canonical recipients/times/settings, policy/approval requirements, server-bound expiring versioned preview receipt |
| `POST /` | Current authority + preview/hash/version + required approval + idempotency key. Persist operation/outbox/audit atomically;202 means PendingDispatch, NOT Created. Unavailable provider must reject before enqueue |
| `GET /operations/{operationId}` | Actor/tenant-authorized durable status and safe recovery guidance; unknown dispatch remains AwaitingReview, not eligible for a fresh create |
| `GET /` / `GET /{meetingId}` | Tenant-scoped paginated stored meeting history/detail. Filter domain dates explicitly; never return another actor's unauthorized attendee details. Reads do not send invitations |
| `POST /{meetingId}/preview-change` / `PATCH /{meetingId}` | Future update after creation is qualified. Expected record version, current scope and reapproval; provider side effects and local result correlated |
| `POST /{meetingId}/cancel` | Future explicit cancellation with reason, approval/idempotency/version checks; no hard-delete shortcut or assumed provider cancellation |

First readiness response must report every provider operation unavailable while adapters/storage are absent. History must report unavailable, not a successful empty list from an unimplemented store. UI exposes only backend-permitted operations and retains Manual navigation on outages.

### Future preview input

Use typed fields: provider enum, batch/organizer/student IDs, title, local start plus academy time-zone identity and explicit offset (or an unambiguous offset-bearing instant), bounded duration, typed recurrence, access/chat/recording settings. Do not accept actor, academy, role, secure-connection or approval claims in the body. Unknown fields are rejected rather than silently ignored.

- Resolve organizer/current batch assignment and revalidate immediately before execution. No caller-supplied organizer email replacing an authorized identity.
- Prefer record IDs; server resolves and deduplicates recipients. Missing/invalid emails and foreign/inactive/completed enrollments produce field-specific errors. Do not silently omit an invitee. Bulk/external invitations require the applicable explicit approval and consent policy.
- No free-text recurrence execution. One-time, weekly or custom schedules require a bounded structured rule/end/count and a provider-qualified mapping. Unsupported settings fail explicitly; selecting a UI option is not evidence the provider supports it.
- Validate academy timezone and offset; ambiguous/nonexistent local times require correction/explicit disambiguation. Store UTC instants and zone/rule metadata. Never infer a timezone from the developer laptop. Keep full-minute requirements in scope for eventual integration; current draft's15-minute picker is not the final scheduling contract.
- Provider access/chat/automatic recording/guest options are not interchangeable. Until product/privacy policy and adapter capabilities are approved, do not enable unrestricted joining, guest invitations, automatic recording or unsupported provider restrictions. These are proposed fail-closed defaults, not newly approved academy business rules.
- Bind preview/approval to actor, tenant, canonical body hash, source versions, connection generation and expiry. Any relevant change invalidates it. A chat “yes” or client Boolean is not an approval record.

## 4. Durable execution and uncertainty

Do NOT hold a SQL transaction open across a remote provider call. Existing local transaction/audit helpers cannot roll back a meeting that an external provider has already created.

1. In a local transaction persist operation, canonical request/hash, actor/academy/approval/version binding, durable outbox and required audit. A unique `(AcademyId, OperationKind, IdempotencyKey)` prevents duplicate insertion. Same key/different body conflicts; actor mismatch is denied; replay still requires current authorization.
2. A leased worker claims one eligible outbox item with a concurrency fence. Recheck current authority, source versions, approval and connection generation. Revoked rights or changed inputs block dispatch, even if previously previewed/approved.
3. Record Dispatching with a durable provider correlation key before network access. Call only the qualified adapter with documented timeout, bounded retries and provider-specific reconciliation behavior. Do not assume provider idempotency is available or equivalent across vendors.
4. Definite rejection → Rejected with safe reason. Confirmed provider result → persist provider event ID, verified join URL, canonical returned settings, operation transition and audit atomically before reporting Created. Invitations/delivery get their own truthful states, not “delivered” from event creation.
5. Timeout/disconnect/5xx/crash after possible dispatch → AwaitingReview. Reconcile by provider-supported correlation/event lookup when qualified; never blindly resend because a local response was lost. If definitive lookup is impossible, require controlled human reconciliation, not guessed failure/success.
6. Audit/local persistence failure AFTER external success is an uncertain external outcome. Retain correlation/recovery evidence, quarantine dispatch and reconcile; never claim a SQL rollback cancelled the provider meeting. No automatic compensation/cancellation without approved authority.
7. Update/cancel have separate versioned operation records; they do not overwrite history or revive cancelled events. Retain bounded audit, actor, timestamps and prior versions without secrets or unnecessary raw student data.

Suggested states: PendingDispatch → Dispatching → Created or Rejected or AwaitingReview. Cancellation/update states are separate future operations. Client unmount/refresh cannot discard or restart the durable operation.

## 5. Implementation queue — continue, do not restart enterprise testing

| Packet | Route | Deliverable / gate |
| --- | --- | --- |
| M1 — next | Sol High | Read-only capabilities/readiness service + authorized endpoint + explicit Engagement mapping. Default all execution/history unavailable; no credentials/network/migration needed. API tests for own Owner/Admin,foreign academy,platform-owner bypass,Teacher/Student/Manager,expired/current identity,disabled academy/module and configured-marker spoofing. No frontend create enablement |
| M2 | Sol High | Typed preview, deterministic validation/versions/approval boundary and durable operation/outbox/history model. Reviewed migration and disposable SQL tests before dispatcher. Simulated adapters are test-only and must not make production readiness true |
| M3 | Sol High | Qualify one provider adapter at a time with approved secure connection lifecycle and bounded reconciliation. External setup/provider choice/account/scopes/consent policy require explicit decisions/authorization; no automatic Azure provisioning. Other providers stay unavailable |
| M4 | Sol High integration; Sol Medium settled visuals | Wire Manual create/history/receipts/approval/recovery to real host APIs, then separately approved PENTA tool adapter. Browser,realSQL/runtime/security/device/provider-sandbox/release gates before enabling and Azure approval |

M1 can proceed locally without choosing/purchasing a provider or involving the owner. M3 cannot honestly complete without an approved provider account/connection and provider-specific capability verification. This is not a revised whole-app delivery plan or a claim that four executions finish the app. Existing academic/portal visual work remains queued,not abandoned.

## 6. Acceptance matrix to extend existing QA

- Authority: cross-tenant route/resource/organizer/enrollment/operation IDs, current actor changes,platform-owner special path,Teacher/Student/Manager denial and entitlement off. Repeat through actual HTTP/Identity/disposable SQL, not only controller unit calls.
- Inputs: malformed/unknown tools or DTO fields, invalid emails/URLs, duplicate invitees, stale batch link,noneligible delivery mode/enrollment,timezone gaps/overlaps/offset mismatch,recurrence limits and unsupported restrictions. Do not alter Offline venue or existing manual-link workflows.
- Operations: simultaneous same-key requests;changed payload/key collisions;worker lease/crash/fencing;post-approval revocation;timeout before/after provider commit;lost response;callback replay if used;provider outage/rate limits;required-audit failure before/after dispatch;restart/status lookup and bounded reconciliation. No duplicate meetings/invitations from unknown outcomes.
- UI: creation unavailable vs configured vs qualified readiness;preview/confirmation/approval;pending vs created vs rejected vs unknown;truthful history unavailable/empty/loaded;readback failure;exact date/time;all screen sizes/themes;physical Android/iOS/laptop acceptance.
- PENTA later: hostile/malformed/stale planner calls,approval bypass and AI downtime with independent Manual fallback. A model upgrade never changes host permissions.

No new runtime/SQL/provider tests were executed for this documentation-only packet. Prior18focused/30synthetic meeting-draft checks remain valid at unchanged application source,not rerun or promoted to integration evidence. Existing open enterprise,UI2/Mini/domain conversation/privacy/device/E9–E10 gates remain open.

## 7. Boundaries and owner sequence

No application/schema/Mini/test strategy/credential/main/staging/commit/push/Azure/container/production-data changes. No broad cleanup;prior dirty work/evidence retained. No new Mini contract is required for M1. Owner sequence remains full agreed PENTA+Manual app → authorized go-live → Hayansh → Academy Desk product website after first customer live.90% AI remains an outcome target,not measured coverage.
