# Teacher conversation startup — 28 October 2026

Status: STARTUP HOST AND FRONTEND TRANSPORT QUALIFIED. All 3,106 API tests
passed, zero skipped; 200 SQL checks, 113 client checks, TypeScript, lint,
11 retained shared Core compatibility checks and 35 CI guards passed. This is
not model, visible chat or production acceptance.

## Scope

Continue the accepted teacher Core, review and conversation gateway slice in
`D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Preserve unrelated dirty work,
main, Mini source, existing tests/issues/evidence and the enterprise release plan.

The previous gateway required a fixture-created session/draft. This batch adds
server-authorized startup over the existing session/turn/draft tables, reusing
current SQL/Identity authorization, academy locking, protection and audit. No
schema change, replacement backend, model registration or implicit teacher create.

## Implemented behavior

- Separate Testing-only TeacherStartContractsEnabled plus retained teacher/Core/
  conversation gates. HTTP retains the existing independently audited controller
  gates. A real/unqualified provider cannot adopt the synthetic QA marker by flag.
- GET `/api/academies/{academyId}/penta/teachers/readiness` rechecks current
  authority, bounded teacher context and interpreter availability. canExecute=false;
  synthetic interpreter availability is explicit and is not model qualification.
- POST `.../penta/teachers/conversations` accepts exactly one canonical nonzero
  requestId. A serializable SQL/Identity transaction stores one owned session,
  empty teacher draft, protected startup marker/receipt and required audits.
- Same-key concurrent POST returns201/200 with the same IDs. No startup inference,
  approval, teacher creation or account creation. A new request obeys existing
  academy/actor session, draft and turn quotas; exact recovery precedes quota checks.
- GET `.../penta/teachers/starts/{requestId}` resolves current authority and
  validates actor/tenant/purpose/context/expiry/receipt/draft/audit. Startup IDs
  recover after interpreter disconnection, during a pending prompt and after
  explicit domain completion without allocating again or implying a new create.
- Protected session state and start receipts have independent teacher purposes.
  Foreign workflow markers, altered ciphertext, missing audit, changed authority/
  epoch, expiry and unsupported context fail closed. Audit failure rolls back all
  startup rows and leaves no phantom result.
- Frontend startup transport follows the retained student startup/recovery pattern,
  with teacher routes/protocol/status and independent strict validation. Current
  server-resolved academy/account plus token/login epoch/route/abort fences;
  no automatic POST, double-click redispatch, model approval or raw prompt storage.
  Before the only explicit POST, retain only the opaque teacherStartRequestId in
  the URL. Unknown outcomes/reload use same-key GET-only recovery.

The immutable teacher-start/1 summary uses status=conversation_started,
canExecute=false/domainEffect=false. It returns lookup IDs, not a field snapshot,
approval or committed teacher receipt. Fetch current draft/turn state separately.

## Validation checkpoints

- Backend window `871d5dcb371841a0b2f781d2bf5053e5`: API 3,106 passed;
  rebuilt harness zero warnings/errors, SQL `965cfe09046d444992117d7f8d619a4e`:
  46 startup +43 conversation +21 Core +30 workflow +15 service +45 availability
  checks passed. The runner was extended, not substituted or weakened.
- Startup proof includes actual HTTP ownership/role/tenant/default-gate and
  malformed-input refusals, independent connection availability, simultaneous
  201/200 same-identity startup, current empty Core context, fresh-host/disconnected
  recovery, rollback on required audit failure, no phantom receipt, expiry,
  ciphertext/cursor/context/audit/purpose tampering, session quota with same-key
  recovery, pending-prompt recovery and revoked current authority. Startup and
  recovery invoke no interpreter or domain create. Two explicit synthetic prompts
  lead to partial/prepared state; separate explicit native approval/execute gives
  exactly one teacher/execution audit and no account, with a persistent receipt.
- CI acceptance floor advanced3,089→3,106;35artifact/workflow guards pass,
  including refusal of the previous3,089-test baseline as the current candidate.
- Frontend window `fb24089af50c4511abbb59ee4387abda`: all 61 startup transport
  and retained 52 teacher operation checks passed, full TypeScript validation and
  zero-warning targeted lint passed. Saved individual startup checks cover exact
  protocols/scope/expiry, hostile readiness/results, missing/unqualified provider
  indicators, HTTP failures, double click, lost delivery, abort/navigation, token
  and identical-token epoch replacement, recovery-key history persistence, reload,
  disconnected recovery and malformed/edited query rejection. These are controlled
  transport tests, not native browser/physical device acceptance.
- The 11 retained actual PENTA schema/ConnectorSDK checks passed on this run's
  saved Core HTTP outputs, with Mini source hashes unchanged. This is compatibility,
  not reasoning quality or Mini implementation of teacher-start/1.
- Final linked qualifier:
  `QA/EVIDENCE/penta-teacher-start-qualified-68c078d345284c3a9ecc992e273f7f61/result.json`.
  Reconciles API/frontend/test/harness/source/evidence pins, TRX/native/client
  receipts, exact original services/images/running/health/API200/full Docker
  inventory, owned SQL database/login/container cleanup, released exclusive lease
  and unchanged main. No package acquisition, model inference or release approval.
- No new browser/export/device proof claimed: an unconsumed transport needs no
  new static page export. The accepted native review UI remains historical to its
  own source. Student startup source/implementation was not changed.

## Remaining work and boundaries

Still open: visible teacher conversation composer, global Executor routing and
qualified private interpretation. This transport is not an exposed usable chat
screen. Synthetic host validation is not reasoning quality, enabled production
tools, all-role coverage or90%AI delivery.

Owner's explicit stop before weights loading remains binding; preserve the
completed setup. Mini fresh6/12/exposed7/12 NOT ACCEPTED. No model load/train/
inference/activation/download/paid compute, commit/push/merge/main/production DB/
Azure or enterprise issue closure. Existing model, workflow, all-role/Manual/
customer-critical/device and exact GitHub/runtime/security/migration/restore/
staging/release gates remain OPEN. Use the established Mini handoff for actual
shared-engine dependencies; no project switch is needed for host wiring.

Next route: Sol High for composer/routing/current-authority integration;
Sol Medium for settled bounded UI work. No automatic model change.
