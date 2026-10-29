# Teacher conversational host gateway — 27 October 2026 batch

Status: HOST GATEWAY BOUNDARY QUALIFIED with synthetic interpretation only.
The current API suite passes 3,089/3,089 with zero skipped tests; all43 new
conversation SQL cases and retained111 teacher SQL cases pass. This report
does not qualify a model connection, visible chat, production or 90% AI coverage.

## Scope

Continue the existing teacher Core/review slice, not a new enterprise plan.
The branch remains `codex/penta-search` in `D:\AcademyDesk-codex-p0` at
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Preserve unrelated dirty work,
accepted Core/native review evidence and the separate Mini/main checkouts.

The teacher-specific gateway reuses existing PentaMiniSessions/PentaMiniTurns,
encrypted teacher drafts, current SQL/Identity authorization, serializable academy
locking, required audit and the shared Core preparation adapter. No schema change,
new domain service, model tool registration or direct creation from a prompt.

## Implemented boundary

- Strict prompt input: requestId, exact draftRevision/payloadHash, bounded text.
  Unknown/duplicate fields and malformed bindings fail before interpretation.
- Testing plus independent teacher/Core/conversation flags are mandatory. No live
  interpreter is registered; only an explicitly injected disposable synthetic
  interpreter can exercise the gateway. Flags cannot activate a real provider.
- POST claims and audits the request atomically before calling interpretation.
  The SQL transaction ends before planning. Same-request POST replay never
  redispatches; GET recovery returns pending or the protected recorded result.
- After planning, resolve current database identity/role/tenant again, verify the
  session/draft/revision/hash and current branch/tool/evidence snapshot. A revoked
  actor or changed context/catalog cannot apply a late plan.
- Only strict teacher partial/preparation decisions are accepted. Authority,
  arbitrary tools/code/SQL, raw model prose and approval/execution claims are not
  adopted. Existing duplicate review holds and separate native approval remain.
- Draft changes, completion audit, protected receipt and pending-clear commit
  together. A failure after partial mutation aborts completion; uncertain outcomes
  retain the original pending request rather than automatically redispatching it.
- Protected receipts bind actor, academy, conversation, draft, request, authority,
  claim digest, base/completed revisions, payload hash and required audit identity.
  Raw prompt text is not persisted to the turn ledger or audit.
- Two-worker capacity, 30-second interpretation deadline, bounded input/output and
  existing daily quotas remain host-owned. A non-cooperative late worker retains
  its capacity until it exits; late results are discarded.

HTTP ports under the existing independently audited teacher draft controller:
`POST .../drafts/{draftId}/conversation/turns` and
`GET .../drafts/{draftId}/conversation/turns/{requestId}/result`.
There is no public client-supplied completion endpoint.

## Validation and retained failures

- 24 new API tests; full suite 3,089 passed, zero skipped.
- 34 CI artifact/workflow guards passed; minimum API acceptance floor is 3,089.
- Final guarded window: `5044b22fd3814493b029fc23f18d6565`, SQL
  `2a26978d044843fcb9e9253da87607be`, rebuilt harness zero warnings/errors.
  43conversation +21Core +30workflow +15service +45availability PASS.
  Conversation planning creates zero teachers, accounts or execution audits.
  Real102domain/7Identity migrations and owned database/login teardown pass.
- Native SQL checks cover missing/non-synthetic interpreter gates; anonymous,
  Teacher, other-actor and cross-tenant refusal; strict client/model inputs;
  partial fields and preparation without approval; exact replay/GET recovery;
  concurrent same request with one planner invocation; native/new-prompt locks;
  role/approval/SQL/prose injection; provider outage; duplicate Manual hold;
  revoked actor and changed context/catalog after planning; protected receipt
  tampering; claim/completion mandatory audit rollback; client cancellation;
  two-worker capacity and no teacher/account/execution side effects.
- Retained actual PENTA schema/ConnectorSDK replay11checks PASS against this
  run's saved Core HTTP outputs. This verifies shared Core compatibility, not
  model reasoning or a shared implementation of the new host summary envelope.
- Final read-only qualifier:
  `QA/EVIDENCE/penta-teacher-conversation-qualified-f26d38e4a9c644d8b21c4269ae1b5dc3/result.json`.
  Reconciles current API/test/harness pins, TRX/native/shared receipts,34CI guards,
  exact original service IDs/images/running/health, API health200, full Docker
  inventory, owned SQL removal, released lease and unchanged main. Model loaded,
  model inference, tool registered and production approved all false.
- Earlier `ee1b515474bc445ca434c3fcb5eb8f9d` passed API and retained
  45availability/15service/30workflow/21Core SQL checks, then failed new fixture
  seeding because anonymous-object JSON casing changed FirstName to firstName.
  The fixture now uses dictionary keys. Only its labelled SQL container
  `ce3e86c951e343e6bfe309a2fa814766` was removed after ownership/loopback/no-mount/
  prior-inventory checks. Exact originals and full Docker inventory restored.
  Its source pins no longer match the corrected harness/CI revision; it is failed
  history, never acceptance evidence. All QA evidence retained.
- `9261f719aac74b8ab2eac3a6da442e73` passed API, then the unchanged 3GiB
  admission guard refused the harness build. No SQL created; exact services and
  Docker inventory restored. The current runner uses a smaller owned Node heap,
  explicit collection of its own garbage and a lighter parent shell; no lowered
  guard, unrelated process termination, Docker restart or global memory change.

## Remaining acceptance and next route

Native teacher review UI and its six browser/SQL journeys remain the previously
accepted checkpoint, not a new frontend proof for this gateway. Next connect the
teacher chat transport/composer and the qualified shared interpretation adapter,
including truthful availability, required-field prompts and same-request recovery.
Teacher session/draft bootstrap and global Executor routing also remain open:
this run seeds its own owned synthetic session/draft; the new gateway does not
claim that a customer can start teacher onboarding from the production chat.
The new host summary reports canExecute=false/domainEffect=false even when
kind=prepared; consumers must not treat HTTP200 as teacher creation success.
Recorded pending requests must use their GET result port, never blind redispatch.
Use `PENTA_HANDOFF_TO_MINI.md` for an actual shared engine qualification/contract
dependency. No speculative model activation or parallel replacement backend.

Model qualification remains OPEN: fresh6/12, exposed7/12 NOT ACCEPTED.
The owner's stop before loading weights remains binding; completed local setup
is preserved. No load/training/inference/activation/download, commit/push/merge,
main edit, production DB, Azure action or release approval. Broader workflow
cohorts, Manual/all-role/customer-critical/physical-device acceptance and exact
GitHub/runtime/security/migration/backup-restore/staging/release gates remain OPEN.
BUG-API-0002 and other broader enterprise issues remain OPEN.

Route: Sol High for conversational authority/Core/SQL integration. Sol Medium for
settled bounded UI work. This is guidance, not an automatic model change.
