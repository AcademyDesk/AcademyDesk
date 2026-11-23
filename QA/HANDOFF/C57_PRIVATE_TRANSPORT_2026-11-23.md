# C57 — inactive private Mini transport foundation

MODEL: Sol High
TOOL: Cloud Codex
BRANCH: codex/cloud-c57-private-transport-20261123
BASE: 2e8e12c36c0252d339c78e8a83f794f954bd9c80
MINI REVIEW PIN: 2686d90e3d3fb57e58889e09969a336312e6d9a5
STATUS: IMPLEMENTED / model-free verification; receiving integration NOT ACCEPTED
DATE: 23 November 2026, Asia/Calcutta

Cloud owns exactly:

- apps/api/Intelligence/Penta/PentaPrivateTransport.cs
- tests/AcademyDesk.Api.Tests/PentaPrivateTransportTests.cs
- docs/penta-private-transport-v1.md
- QA/HANDOFF/C57_PRIVATE_TRANSPORT_2026-11-23.md

D56 assigns production transport/security preparation to Cloud. Implement an
unregistered outbound transport foundation: exact HTTPS service origin and
existing Mini paths, workload-token authentication, disabled redirects/proxy/
cookies, private-address-only connection binding and model-free rejection tests.
Document the receiving gateway/identity/shared-contract prerequisites explicitly.
No change to existing provider, PilotPolicy, Program registration, model tools,
tenant/actor authority, approvals, domain/audit, native SQL, Mini serving source,
workflow, dependencies, environment settings or production activation gates.

Validate with fake credential/HTTP handlers and private-address classification;
no live identity, network, model or Azure request. Keep all other base paths and
the frozen Mini checkout unchanged. Publish bounded source and handoff, not an
activation or a claim of end-to-end authenticated serving. Existing D56 native
and synthetic demo evidence remains Desktop-owned.

Next route: Sol High Desktop/Cloud for reviewed receiver auth and qualified
provider integration; retain the model-loading stop, no main merge/provisioning/
deployment and all six open critical records. C56 remains a separate return at
ba4b261e; do not replay C55/C54/C53/C50.

## Completed source and actual boundary

Added the outbound candidate and its tests, plus the
[versioned receiver/integration contract](../../docs/penta-private-transport-v1.md).
The public factory uses managed identity for one configured application audience;
only the internal test factory accepts a fake credential/HTTP handler.
No registration, existing provider/policy, production flag or Mini source changes.

The sender validates an exact HTTPS service origin and existing protocol0.1
POST/v1/chat and GET/readiness paths. Authentication is per request, cleared in
finally, and never stored in default headers or payload fields. Caller identity,
cookie and auth headers fail before token/network use. Credentials expiring
within60seconds or malformed/oversized tokens fail closed. Token/network failure
details are sanitized and there is no model-call retry.

Factory redirects/proxy/cookies/decompression are disabled; TLS verification is
preserved. Each new connection resolves once, refuses mixed/public/loopback/
metadata/CGNAT results and connects the validated RFC1918/IPv6-ULA address itself.
HTTP1.1 is fixed to avoid an alternate QUIC route bypassing the TCP binding.
The default response buffer is16KiB; a future streaming provider must retain the
existing bounded JSON/duplicate/runtime/schema parser and host authority checks.
This transport does not parse, approve or execute model plans.

The genuine reviewed Mini source still has no workload-token authentication on
these handlers. Sending it a token alone would not establish authenticated
serving. The contract requires a separately implemented and verified receiver
boundary: tenant issuer/signature/audience/workload/application-role, expiration,
version negotiation, private ingress and no direct unauthenticated bypass.
No receiver pass, Azure identity/DNS/TLS connectivity or production readiness is
claimed. The current Development/Testing-only provider stays unchanged.

## Desktop return and CI

D56 at2e8e12c36c0252d339c78e8a83f794f954bd9c80 preserved C54/C55, extended actual
native legacy precision/near40k and restoration tests, and returned a refreshed
synthetic demo with13model/Identity/SQL and11Core-outage/denial/Manual checks.
Those are Desktop results, not cloud re-executions. Preserve failed historical
resource/stop-start runs and private SQL/key backup; do not close critical issues
or claim Azure restore from a local backup verification.

Exact D56 [45155173334](https://github.com/AcademyDesk/AcademyDesk/actions/runs/45155173334):
ordinary verification SUCCESS; required paired Core FAILURE at
`Require access to pinned private Core source`. The owner must securely supply
dedicated read-only PENTA_SOURCE_READ_TOKEN and obtain both exact-candidate passes.
CLI credential rejection during reconnect was separate: API reads and normal Git
fetch/push subsequently worked, without changing credentials or network policy.
No Actions secret or job was modified/dispatched here.

Desktop advanced during this packet to
fef8bdff6120ef018bd40ea09815b2d6519b119c, integrating C56's preflight/launch map
and boundary regressions. All four C57 paths remain absent on that new base;
there is no claimed-path collision and C56 must not be replayed. Desktop's D57
handoff explicitly preserves C57 and claims a separate GuardianChildren500 fix
in PortalController.cs/GuardianFlagsRegression.cs after its live permission probe.
That new application failure is Desktop-owned and not fixed/qualified by C57.
D56 already corrected the deployment runbook. Do not overwrite newer Desktop
source or replay whole branches. Combined fef8+C57 acceptance is still required.
D57 [45174171166](https://github.com/AcademyDesk/AcademyDesk/actions/runs/45174171166)
was in progress at the exact-source check; no final result is inferred.

## Verification and preservation

Tests use fake token credentials and HTTP handlers, real typed protocol0.1
serialization, and pure address-selection policy. There is no DNS lookup,
socket connection, workload-token acquisition, model request or Azure call during
these tests. Actual network/TLS/identity, receiver auth and native provider
integration are explicitly unrun.

Initial targeted run:61passed/onefailed, zero skipped. The failing fixture
constructed a malformed URI inside a delegate expected to throw a configuration
exception; .NET's Uri constructor correctly rejected it first. The final test
separately asserts URI-type rejection and retains the configuration assertions.
No refusal case or assertion was removed. The expanded intermediate suite then
passed903/903; final verification includes the subsequent fixed-HTTP-version and
buffer-bound cases. Initial/intermediate evidence remains separate.

Final verified source:905/905 PENTA API cases passed, zero failed/skipped,
including68 new transport cases and837 retained PENTA regressions. The final
TRX has905 executed/passed, zero errors/timeouts/aborts. This is the affected
selection, not the complete2,600-case application or a model/SQL qualification.
Git diff whitespace passed. Existing production checker still exits1 for all
six open acceptance records. No production or issue gate was relaxed.

All1,840 unclaimed tracked base paths, including16vendor files, are byte-exact.
The Mini working checkout stays clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8,
all33frozen paths unchanged. Only Git objects for the genuine2686d90review pin
were fetched; no worktree/model/archive was replaced. The local integrity receipt
records both reviewed Mini source hashes and executed host/test source hashes.

Raw local evidence: `/workspace/c57-private-transport-20261123/` contains retained
initial/intermediate/final test logs/TRX, exact CI responses, source integrity,
remote pins and publication receipt. No credentials, private archives, raw
Desktop evidence or database/key backup are uploaded. The enclosing implementation
commit is the return SHA; feature publication does not imply hosted backend CI.

| Private local receipt | SHA256 |
| --- | --- |
| `verified-results/c57-penta-verified.trx` | `5943dad7115995e4e82e62cff6c779f1b1915144b26dad9a04e892bcbab4ef95` |
| `penta-tests-verified.log` | `98e3b34d2fb6a0c8de445542b2b38e0890fdcd63571a2b09bf2dc9843d5d113c` |
| `source-integrity.json` | `d0fd240b84b82fc8baa1525216051292a3359974f04380a7ea41f46cd2712ed7` |
| `desktop-ci-jobs.json` | `6f6f565f28817fe224c3d7eb977b12dd68bb55d79e170de0448c2873c45408f4` |

## Next acceptance and ownership

1. Mini/Cloud owner claims the receiver auth/version files and implements the
   genuine boundary; accepts allowed-workload success and wrong issuer/audience/
   principal/role, user-token, missing/malformed/duplicate token/header and auth
   dependency outage failures with zero planner calls. Do not add unapproved
   packages, fabricate archive evidence or change frozen provenance.
2. Cloud claims provider/registration/parser paths, retains strict response/runtime
   validation and prepares a reviewed default-off production policy/transport
   integration. Neither this factory nor a version header grants activation.
3. Desktop/infrastructure accepts actual private DNS/TLS/token/RBAC and native
   Identity/SQL/approval/audit/receipt-outage behavior on combined source. Preserve
   the active demo; do not run heavy suites on the laptop during its demo lease.
4. Release owner completes the required paired CI credential and genuine private
   archives, six critical closure chains, physical devices and staging/private
   Blob/key/SQL restore/rollback. Mini/Plus/Pro remain production-unqualified,
   Ultra deferred and90%unmeasured. Broader conversational writes/roles are
   separate outcome packets; the two-tool lookup registry did not expand here.

No main merge, paid provisioning, Azure/environment publication, public-access
change, model acquisition/loading/inference/training or activation was performed.
Next route remains Sol High for receiver/provider/security/native qualification;
Sol Medium for routine pipeline and handoff maintenance.
