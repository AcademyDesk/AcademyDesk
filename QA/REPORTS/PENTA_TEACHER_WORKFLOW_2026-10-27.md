# Protected teacher creation workflow — 27 October 2026

## Current checkpoint

The native host workflow and its frontend transport boundary are qualified
locally behind Testing-only gates. Final backend window
`a7c3cb2731b340479997de7e18512a8a`, SQL `40c1ba4b99fd44a69daf251d2f6db593`,
and corrected frontend window `2e44acaa30cb40349ffcbe8dde96433d` passed.
Independent linked receipt/source/live-restoration reconciliation:
`QA/EVIDENCE/penta-teacher-qualified-a686184fde8b440ba279a87eaa9fa258/result.json`.
This is not a registered model tool, visible chat integration or production approval.

## Changes and scope

- Reused the existing PENTA session and encrypted draft stores, domain teacher
  creation service, SQL/Identity transaction helper, data protection and audit.
  No schema migration or replacement backend. Teacher drafts have an independent
  protection purpose and policy; student and teacher drafts cannot adopt one
  another's protected state.
- Added save/read/review/approve/execute/cancel host methods. Every call checks
  the current authenticated academy administrator, active academy, owned live
  conversation/context version and authority fingerprint. Platform Owner is not
  implicitly treated as an academy conversation actor. Current database roles
  and identities, never model roles, authorize operations.
- Academy-first SQL locking and one shared serializable SQL/Identity transaction
  protect authorization, draft state, duplicate checks, teacher creation,
  mandatory audit and the encrypted persisted receipt. A failed required audit
  cannot leave a teacher or success receipt committed.
- Draft edits invalidate approval. Review issues a host approval reference scoped
  to the exact draft revision/hash and limited to two minutes or the draft's
  earlier expiry. Approval requires the explicit `CreateTeacherWithoutAccount`
  intent. Review/model "yes" alone cannot authorize creation. The actual create
  call remains a separate explicit action.
- Conversational duplicate screening conservatively holds matching academy
  first/last names or nonblank email for Manual review, including inactive
  records. It never merges automatically, leaks foreign academy matches, or
  imposes a new uniqueness rule on existing Manual saves. Checks repeat inside
  execution, protecting separately prepared duplicate drafts.
- Repeat execution of the same completed operation returns its original receipt
  after verifying teacher/audit persistence, without executing or auditing the
  creation again. GET recovery uses that same protected draft; current authority
  still applies. Required read audit metadata is permitted; recovery never
  creates another teacher.
- HTTP ports are added to the existing independently audited PENTA controller,
  with strict keys/types/request bounds, no-store responses and sanitized errors.
  Three switches remain necessary: existing OperationalDraftContractsEnabled,
  OperationalOnboardingHttpContractsEnabled and new TeacherCreationContractsEnabled,
  all under `Penta:` and in the Testing environment. A flag grants no permissions.
- Added a host-only TypeScript transport with current account/tenant, token,
  login-epoch, route and response-schema/hash checks. Passive load only reads.
  It requires explicit review/approval/execute calls, rejects duplicate execution
  clicks and refuses to re-arm writes after an uncertain response. Recovery is
  GET-only. Returned copies cannot mutate its private operation binding.

## Final current-source evidence

- Unfiltered API build/test: **3,049/3,049**, zero skipped/failed. Rebuilt SQL
  harness: zero warnings/errors. Native final run: **30 workflow checks**, plus
  retained **15 shared-service** and **45 availability** cases PASS.
- Both actual races passed: same-draft concurrent execution gives one201/one200
  with identical receipt and exactly one teacher/execution audit; distinct
  simultaneously executed approved duplicate drafts give one201/one409 and only
  one matching teacher. Final workflow totals: three teachers/three execution
  audits, no accounts or inference.
- Final client **52/52** checks, TypeScript and zero-warning targeted lint PASS.
  The two added denial checks require a server401/403 to close the channel and
  prevent access to its old private view or any further requests. Passive load,
  current-account/login-epoch/route fences, malformed responses, exact approval,
  one execution, GET-only unknown-outcome recovery and no automatic re-arming
  remain covered. This is controlled transport validation, not a visible browser.
- All **32** CI artifact/workflow guards PASS with the current **3,049** floor.
  No GitHub execution occurred. Final qualifier retains actual TRX/native/CI TAP
  hashes and current API/harness/client pins; verifies exact live original
  services/images/health200, full Docker inventory, owned SQL removal, released
  validation lease and unchanged main HEAD/status fingerprint.
- The completed backend source was reused for the client-only401/403 closure
  correction, rather than rerunning unchanged SQL. The exact hash-matched runner
  used by the backend is archived in its local evidence; its optional combined
  client-test count was subsequently advanced from50to52. The final client has
  its own pinned validation window; neither earlier frontend source nor a future
  runner revision is relabeled as current execution proof.
- Final frontend window admitted at3,515,412,480free bytes, above unchanged3GiB;
  all owned children finished within their existing caps. Both exact original
  services/images/health and complete inventory restored. No downloads or model
  work. Raw local evidence remains intentionally uncommitted.

## Intermediate source-bound evidence

- API build/unfiltered suite: 3,049 passed, zero failed/skipped, in both prior
  workflow windows. Nine new unit cases verify all ports' independent environment
  gate, real same-store SQL requirement and malformed revision/context refusal.
- Window `db8f03f2300043438f66b23662d26ae3`, SQL
  `a51640a0ef9f43deb8bffa311970d5a7`: 30 real workflow checks passed, plus the
  retained 15 shared-teacher-service and 45 availability cases. Exactly three
  workflow teachers/three execution audits, no portal accounts or inference.
  Same-draft simultaneous execution returned one 201/one 200 with identical
  receipt and exactly one teacher/audit. Distinct duplicate drafts were checked
  sequentially in that run; the final run strengthens them to simultaneous
  execution requiring one 201/one 409.
- That run covers gate/role/tenant/private-session refusal, encrypted persistence,
  same-save replay, explicit approval, malformed/forged approval, one-time result,
  completed/edit/cancel terminal handling, duplicate holds, injected execution
  audit rollback and explicit retry after read reconciliation, partial-name
  review refusal, cross-workflow protection purposes, corrupt ciphertext, expiry,
  changed conversation and revoked membership. Exact original services/images,
  full inventory, owned SQL cleanup and lease release passed.
- Initial client transport checks: 50/50 passed against controlled responses.
  CI artifact/workflow guards: 32/32 passed; the current full-suite floor is 3,049.
  Older 2,995/3,040 reports remain historical and cannot qualify this candidate.

## Retained failures

- Window `1ee5e6921d6b4776b94b46d79dc6522d`: API 3,049 passed, but SQL fixture's
  anonymous field properties were camel-cased by the test client's serializer.
  The strict host rejected the first draft with 400. The fixture now preserves
  exact field keys with dictionaries; no parser relaxation. The runner retained
  its owned failed SQL container. Separate guarded cleanup verified its exact
  label/name/loopback binding, no mounts and absence from pre-run inventory, then
  removed only that container and its ephemeral SQL store. Full prior Docker
  inventory was restored; original failed result remains unchanged, with an
  additive `owned-cleanup.json` receipt.
- Frontend window `098802dbfa6948b89595d2a0b549b127`: 50 client checks passed,
  but TypeScript found the account validator's narrowed type omits `workspace`.
  The runtime property check is preserved by capturing it before narrowing.
  Lint was not run after that failure. Exact original services/images/inventory
  restored. The final combined run checks the corrected current source.

## Completed and remaining

- [x] Final combined current-source API/SQL/client/type/lint qualification and
  linked live-restoration/source/evidence reconciliation.
- [ ] Publish a current-authority teacher tool descriptor and implement the
  versioned Core/ConnectorSDK snapshot/prepare/operation/receipt bridge using
  the existing shared contracts. No contract revision or model catalog change
  was made by these native host ports.
- [ ] Qualified teacher conversational interpretation, partial-field dialogue,
  visible shared-design review/approval/result UI, actual integrated browser
  and physical-device acceptance. The new transport is not a visible screen.
- [ ] Other connected workflow cohorts, all-role/Manual/customer-critical and
  exact enterprise/GitHub/runtime/migration/security/restore/staging/release gates.

No model loading/training/inference/activation, downloads, production database,
credentials, main checkout changes, commit/push/merge or Azure action. Owner's
explicit stop-before-model-loading remains in force. Existing Mini model quality
6/12 fresh, 7/12 exposed remains NOT ACCEPTED. No 90% coverage, completed AI
frontend, general model parity, broad issue closure or production-ready claim.

Next route: Sol High for the protected host/Core bridge; Sol Medium after the
behavior and shared contracts settle for bounded visual implementation. Existing
accepted enterprise work remains preserved; do not restart the audits.
