# Private Mini transport v1 — inactive integration contract

Reviewed against AcademyDesk `2e8e12c36c0252d339c78e8a83f794f954bd9c80` and
Mini `2686d90e3d3fb57e58889e09969a336312e6d9a5`, 23 November 2026.
This is an outbound foundation and a receiving-service integration proposal.
It is not a registered provider, an authenticated deployed service, permission
to load models or production qualification. Existing Production/Staging gates
remain closed; do not run production in Testing to work around them.

## Genuine existing shared contract

Mini's reviewed `src/penta_mini/api.py` serves `POST /v1/chat` and
`GET /readiness`; its existing handlers do not validate workload bearer tokens.
The chat request remains protocol0.1's four fields: conversation_id,
user_message, state and available_tools. The bounded state and current
SearchLearners/GetLearner allowlist are unchanged. The response remains the
existing Mini plan plus X-Penta-Mini-Runtime source declaration. That declaration
is not signed model provenance or authorization.

Transport authentication must identify the Academy service independently of the
user. Never forward a user access/refresh token, model-supplied identity, academy
role, credential or actor header to establish authority in Mini. AcademyDesk
continues to resolve the actual actor/tenant, validate the current tool and domain
rules, acquire approval where needed, commit real results and audit them.

## Outbound candidate implementation

`PentaPrivateTransport.CreateClient` requires an exact HTTPS DNS origin on443,
a nonempty Entra audience application ID and an optional valid user-assigned
managed identity client ID. Null client ID selects system-assigned identity.
There is no interactive/developer/shared-secret credential fallback. The token
scope is exactly `api://<audience-application-id>/.default`; the identity and
audience come from trusted server configuration, never the prompt/request body.

The factory owns its SocketsHttpHandler. Redirects, proxies, cookies and automatic
decompression are disabled; standard TLS chain/hostname validation remains.
HTTP1.1 is fixed per request to prevent an alternate HTTP3/QUIC connection path
from bypassing the TCP address binding. Default buffered responses are capped at
16KiB; ResponseHeadersRead callers must still enforce the existing streaming bound.
Connection timeout is10seconds, total client timeout125seconds, maximum two
connections per service and pooled connection lifetime five minutes. A future
readiness caller must impose the existing shorter three-second deadline. These
are transport bounds, not a production latency SLO or a concurrency qualification.

Every new connection verifies the expected host/port, resolves DNS once, refuses
empty/oversized or mixed public/private results and connects directly to the
selected private address. The TLS hostname remains the configured DNS name.
IPv4 RFC1918 and IPv6 ULA are allowed, including mapped RFC1918 addresses;
loopback, metadata/link-local, multicast, public and CGNAT addresses are not.
The sender does not validate DNS and then ask a second client to resolve again.
This policy is for the new serving route, not a change to the existing loopback
adapter, laptop/Tailscale access or cloud networking. Actual private DNS/routing
and TLS still require environmental verification.

Only JSON POST/v1/chat and bodyless GET/readiness are admitted. Alternate hosts,
paths, ports, queries, fragments, methods and caller identity/cookie/auth headers
fail before token acquisition or HTTP dispatch. The sender sets the scoped
Bearer token and `X-Penta-Transport: penta-private.v1` per request and clears those
headers in finally, including failed/canceled requests. Token expiry must exceed
60seconds and malformed/oversized tokens fail closed. Exceptions do not retain
credential/network error details. There is no planner-call retry or redirect
forwarding. Managed identity SDK token-acquisition behavior is separate from
planner dispatch; token presence is not an inference or qualification result.

The transport returns HTTP responses; it does not deserialize or authorize model
plans. A later provider must retain the existing16KiB response bound, strict JSON/
duplicate/schema/runtime checks, typed tool validation and durable host claim/
reauthorization/audit/GET-recovery semantics. A failed planner connection must not
disable Manual ERP or replay a previously committed turn.

## Receiving gateway/service — required, not implemented by C57

Coordinate the following versioned boundary with the Mini owner before wiring
the outbound candidate. It may be implemented by a reviewed gateway plus private
Mini service; naming a gateway does not establish that its defaults are secure.

1. Require TLS and validate Entra access-token signature with trusted tenant
   issuer/JWKS, the exact configured audience, expiry/not-before and controlled
   skew. Restrict the allowed Academy workload principal/application and a
   dedicated application permission such as the proposed `Penta.Mini.Plan`.
   Provision that role and assignment explicitly; it does not already exist.
2. Reject wrong tenant/audience/principal, absent role, delegated-user tokens,
   expired tokens, bad signatures and missing/duplicate authentication before
   parsing a model prompt or calling inference. Define key rollover and auth
   dependency outage behavior; fail closed without a development-key fallback.
3. Require the exact transport-version header on both protected routes. It is
   negotiation metadata, not proof of identity. Never trust caller-supplied
   X-MS-CLIENT-PRINCIPAL, actor or tenant headers. Strip impersonation headers at
   the ingress boundary and prove there is no direct unauthenticated bypass.
4. Keep the Mini/Core/inference listeners private and accessible only along the
   approved service path. Deny anonymous/public access and unapproved workloads;
   network privacy alone does not replace service authentication. Browser/user
   traffic must reach AcademyDesk, not the model endpoint.
5. Preserve protocol0.1, request limits, bounded response/runtime identity and
   declared planner/model pins. Authentication must not introduce SQL access,
   new tools, role authority, automatic writes or prompt/credential logging.
6. Define sanitized401/403/version-mismatch and unavailable responses. Token,
   prompt, private result and signing material must not appear in logs, error
   bodies, redirect locations or published evidence. GET/readiness is protected;
   report infrastructure health separately from qualified model readiness.

No receiver code or package dependency is changed in this packet. Sending a
token to the existing unauthenticated Mini route would not satisfy this contract.
The Mini owner must implement/review the receiving boundary against the genuine
source, then return source pins and independent negative/positive results.

## Promotion prerequisites and next owners

- Cloud/Mini owner: claim receiver and provider files; implement genuine shared
  authentication/version handling, then test exact issuer/audience/workload/role,
  malformed/duplicate/missing token and untrusted-header failures with no planner
  dispatch. Do not weaken the private Core CI prerequisite or change frozen data.
- Cloud: claim registration/provider and shared parser paths before connecting
  this transport. Keep new flags default-off and deployment environments honest;
  implement a reviewed production policy only after identity/tool qualification.
- Desktop/infrastructure: verify actual managed identity, audience/role, private
  DNS and TLS certificate, public/unauthorized denial, network interruption and
  timeout. Verify no token reuse across hosts or unintended proxy/redirect path.
- Desktop: bind the integrated provider to real Identity/disposable SQL and
  demonstrate current actor/tenant/module/role, approval, mandatory-audit rollback,
  unchanged ledgers and GET-only receipt recovery during AI outage. Preserve the
  active demo and avoid concurrent heavy suites on the laptop.
- Release owner: both exact-SHA CI jobs, genuine Mini archive prerequisites,
  six critical closure chains, device/staging/restore/rollback and independent
  model/eligible-outcome qualification remain mandatory. Mini/Plus/Pro remain
  unqualified; Ultra deferred;90%unmeasured.

Changing only environment/URL checks or using a stronger model would not satisfy
these prerequisites. No main merge, Azure provisioning/deployment or model
loading/activation follows from the existence of the outbound factory.
