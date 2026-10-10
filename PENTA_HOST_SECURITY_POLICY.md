# Academy Desk PENTA host security policy

Policy ID: `academy-host-security/1.0`. Adopted 2026-10-10.
This is a permanent application requirement, not a Tool Protocol version change.
It applies to Mini, Plus, Pro, Ultra and every future provider or connector.

## Authority and execution boundary

Authenticated user → server-resolved tenant → permitted tools → model interpretation
→ validated tool request → current authorization → required approval
→ existing domain/application service → real result → required audit.

The authenticated identity is resolved against current server records. Route,
client, model and memory tenant IDs/roles/approval flags are untrusted inputs.
An academy route must match the current user's server-resolved academy; every
source query and domain mutation must also enforce its own tenant/resource scope.
Revalidate current authority after planning and immediately before execution;
stale tokens, displayed records or conversation state do not preserve revoked rights.

The model may suggest, request and learn. Academy Desk must verify and authorize.
No model upgrade, branding, claimed intelligence, prompt or returned policy can
expand the code-owned tool allowlist or application permissions.

## Mandatory application controls

- Keep teacher financial restrictions, student own-record restrictions, current
  RBAC and cross-tenant isolation in the domain/API layer. PENTA is not a bypass.
- Never expose role/security-configuration mutation, arbitrary SQL, shell or code
  execution as PENTA tools. Model output is data, not executable instructions.
- Strictly validate tool names, argument types, allowed fields, bounds, source
  identity and risk class. Unknown fields/tools fail closed; no silent repair.
- Bind sessions, receipts, protected context and approvals to tenant and actor.
  Stale versions, replay, uncertain outcomes and changed permissions must retain
  the established concurrency/recovery safeguards, not automatically retry writes.
- Compute finance with existing deterministic domain services. Preserve payment
  transaction/locking integrity, lifecycle rules, exact balances and race tests.
  Models cannot calculate authoritative amounts or change accounting/GST rules.
- Sensitive/bulk/destructive mutations require the existing domain safeguards and
  server-validated approvals before execution. Approval must be scoped to the
  authorized actor, tenant, exact operation/target/payload and current policy;
  changes, expiration and replay must invalidate it. A model's approval label or
  conversational "yes" alone is not an application approval.
- Persist required audit with the successful operation/state transition. Audit
  failure must not report success; domain transactions must not partially commit.
  Record bounded metadata, never secrets or unnecessary raw private conversation.
- Conversation memory and feedback cannot grant permissions, override policies,
  alter finance rules, authorize tools or modify system/security configuration.
  Such changes may only occur through separately authorized domain operations.

## Current implemented boundary (not future feature acceptance)

The current local read pilot is Owner/AcademyAdmin only, gated by configuration,
environment, active identity/academy and Finance module. Teacher and Student
PENTA access is denied; no student-specific conversational feature is enabled.
`PentaPilotPolicy`, `PentaFinancePolicy` and `AcademyDeskConnector` independently
check current identity, role, academy/module and resource scope. The orchestrator
reauthorizes after inference. Only `SearchLearners` and `GetLearner` are available;
GetLearner requires a stable identity in trusted current displayed results.
Returned model state/facts are not adopted as authoritative context/results.

Search uses the existing `OutstandingFeesService`: Completed plus Reconciled
payments count; Voided payments do not. The host returns source-backed facts,
not model prose/finance. Protected session/receipt state and required audit are
transactional. An `APPROVAL_REQUIRED` response in this read pilot performs no
domain action and is not an enabled write approval workflow.

No create/update/delete/send/schedule/security tools, durable learning-memory
authority or autonomous actions are enabled by this policy. Future tools need
their own domain authorization, approval/transaction implementation and tests
before any frontend can present them as usable. Do not duplicate the engine or
rewrite working domain services to satisfy a model proposal.

## Frontend and safe learning

Render availability/permissions and action status from authoritative backend
responses, not selected capability names or model claims. Planned capabilities
must remain visibly unavailable. Show understandable denial/outage guidance
without server internals, secrets or unauthorized record facts. Keep manual ERP
navigation and operation available when PENTA fails. Future writes need clear
confirmation/approval interfaces linked to the server-approved operation and
truthful committed/rejected/unknown outcomes.

Authorized corrections/feedback must use versioned, bounded interfaces with
privacy/retention controls. Learning eligibility is not permission. Current
metadata-only learning records do not enable production feedback, training or
durable Academy Brain memory. Those remain separately gated work.

## Shared contract and customer systems

Mini/Core may independently reject unsafe plans; that never replaces host
authorization. Active Tool Protocol 0.1 remains unchanged. Mini owns shared
versioned policy/tool contracts; breaking proposals use `PENTA_HANDOFF_TO_MINI.md`
and paired adapter/security tests before activation. No draft contract, new tier
or standalone connector automatically activates a tool. Third-party customer
systems must independently enforce their authoritative tenant/resource rights,
business rules and approvals; a gateway or Core check is additional defense.

## Verification and release gates

Keep existing RBAC, tenant, finance, concurrency and enterprise tests. Run the
existing disposable SQL harness with `QA_PENTA_HOST_SECURITY=1` and module
`PentaFoundation` for the hostile fake-planner regression. It exercises real
Identity/HTTP/SQL, not model reasoning. Preserve existing assertions and runners.
Run actual-provider security/semantic and linked browser tests when a qualified
engine is returned; hostile fake output is not real prompt-injection acceptance.

Future tools require malformed-call, cross-tenant/resource, unauthorized finance,
escalation, stale/manipulated context, post-plan revocation, approval bypass,
downtime, required-audit failure and domain financial-integrity integration tests.
Only disposable run-owned resources may be cleaned up; retain QA/EVIDENCE.
Physical-device, privacy, regression and pre-Azure gates remain mandatory.
This policy or a passing focused suite never declares production readiness.

Evidence and remaining gaps: [host-security report](QA/REPORTS/PENTA_HOST_SECURITY_ENFORCEMENT.md).
