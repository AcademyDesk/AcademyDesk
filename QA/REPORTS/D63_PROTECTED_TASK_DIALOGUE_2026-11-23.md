# D63 — protected multi-task dialogue runtime

Session began23November2026 and continued24November2026 (Asia/Calcutta).
Academy base:5cd5f934ad7b358aaeca9b3edf50788fabec0f4f.
Mini candidate:4a0ad0bc90c60e0882c4374704344bb35ae83c70 on
`codex/academy-conversation-core-20261123`. Both are feature branches; main and
Azure remain unchanged. This continues D62 and the existing enterprise program.

## Delivered

- Current stored Identity user, security stamp, role membership, active academy
  and enabled modules determine the allowed catalog. No client/model scope,
  role, approval or conversational memory grants authority. This first runtime
  is Owner/AcademyAdmin only, Development/Testing only and default-off; it is
  not an all-role production catalog.
- Separate `academy-task-dialogue.v1` provider and protected SQL conversation
  endpoints. Nine existing typed operation descriptors feed the D62 graph;
  required/conditional questions, retained field corrections and typed dependent
  student/batch/enrollment bindings are preserved. All are draft-only here.
- Durable claim, one planner call outside SQL, current authority revalidation,
  encrypted state/receipt and mandatory audit in a serializable transaction.
  Replay conflicts and pending/unknown outcomes do not dispatch again. GET-only
  receipt recovery and actor/tenant-bound chronological history with pagination.
- New Data Protection purpose and protocol marker separate these rows from
  legacy Mini conversations. Legacy routes, receipts, exact-money protocol and
  original model source remain supported. No new migration or domain dispatch.
- New transport requires an explicitly configured127.0.0.1base URL, bounded
  request/response, deadlines, no redirects/retry/public fallback, legacy runtime
  identity plus task contract/source/prompt hashes. Response headers report
  provenance; they are not cryptographic attestation or production transport.
- Model-free native regression is enabled in the existing ordinary CI
  PentaFoundation module. The required private Core job is retained unchanged.

Implementation: `PentaTaskRuntimeAccess`, `PentaTaskDialogue`,
`PentaTaskPlannerProvider`, `PentaTaskConversations`, partial
`PentaChatController.Tasks`, DI registration and the existing owned SQL harness.
Mini's additive entrypoint/provider/decoder are documented in its
`operations/ACADEMY_TASK_DIALOGUE_2026-11-23.md`; no frozen originals were changed.

## Current-source verification

| Check | Actual outcome |
| --- | --- |
| Focused host checks |94passed,0failed/skipped:56D62 +28runtime +10dialogue. Included in the broader result below, not additive. |
| Affected host regression |934passed,0failed/skipped, nine isolated processes; existing896 plus38new. |
| Mini Linux selection |167passed,0failed/skipped; one inherited Starlette/httpx deprecation warning. No inference/model mounts/network. |
| Cross-language contract | Actual C# nine-tool request accepted by current Mini TaskRequest. |
| Native task runtime |19SQL/Identity/API checks passed with explicit synthetic QA planner; encrypted compound draft/edit/history, replay, pending race, actor/tenant/role denial, in-flight actor/module revocation, provider outage, read/completion audit rollback. |
| Combined host security/privacy | Existing40host-security and16read-privacy controls passed with the new runtime packet in the same disposable SQL run. No real-model acceptance. |
| Exact money/recovery | Protected cent/large/separate currency strings, legacy GET, unsupported precision, receipt-size fallback, unavailable provider, audit rollback and one-dispatch recovery passed; ledger unchanged. |
| Migrations/cleanup |86application +7Identity migrations applied to exact owned SQL; runtime-login guard and negative cleanup marker check passed; owned database/login/container removed. |
| Resource preservation | Exact container/image/network/volume inventories restored, original two PENTA services healthy, original API health200. |

Private final model-free/regression run:20997acc05344908b8a238bb8efd1d8d.
Private first native pass:b90f9d21a6714c97a0308577d71ffe0b,
SQLf102bec0252f4bdca3d2ca98c2226066.
Final combined pass:6755407c60fc4ec5b2e8462b64d463b5,
SQL6440f461618744a1950de3172f861981. Raw logs/XML, source hashes and synthetic
model replies stay under `.build-check/d63-task-runtime`, outside publication.

## Preserved failures and corrections

The first native wrapper was blocked by local PowerShell execution policy;
process-scoped Bypass was used, without changing machine policy. The next private
harness output omitted its API assembly under --no-dependencies; a full project
build into the isolated output corrected it. An admission attempt stopped below
the unchanged3GiBguard. Cache reclaim/compaction and bounded waiting permitted
later runs; the guard was not lowered and Docker was not restarted.

The first combined native packet exhausted the existing shared actor's daily
conversation quota after25security controls. The new task packet now creates its
own actual synthetic Identity account. No quota is reset/raised and no assertion
is weakened. The final combined packet passes. Failed run containers were
removed only after exact name/run-label/mount ownership checks; raw logs remain.

The first Linux runner imported pytest before its existing isolated dependency
path was added; corrected import order passed. No package install was needed.

Before stopping the old demo, its exact synthetic database was backed up with
COPY_ONLY/CHECKSUM and RESTORE VERIFYONLY, along with its original encryption key
ring. Backup868352bytes, SHA256
745c9300cb8f721dc602ba029698654da78ea193e55b61e03f2d859e28106c3b.
This is preservation/VERIFYONLY, not an actual restore acceptance. An empty DACL
prevented reading the private backup; a Set-Acl attempt lacked privilege. Exact
owner/SYSTEM-only icacls grants corrected access, and the original hash was
reverified. No raw backup/key bytes were changed or published.

## Real-model verdict: NOT ACCEPTED

The already-authorized cached Qwen3.5-2B Q4_0 development runtime was probed using
synthetic text, no writes, no new weights. Initial compound generation timed out
or omitted requested tasks; later constrained generation added unrequested tools
or truncated JSON. Opaque routing passed2/7exposed cases. A goal-first revision
identified the three explicit compound effects and the teacher read correctly,
but mapping/latency still failed: one pass of three executed, four further cases
unrun. The latest descriptive mapper timed out on its first case: zero passes
among one executed, two further planned cases unrun. Do not call these a completed
held-out evaluation or infer the final candidate passed from model-free tests.

The existing backend advertises native tool-call support; evaluating that path
and actual runtime capacity is the next bounded orchestration investigation.
Default thinking was already off in the observed rendered template, so enabling
that flag again is not an evidenced repair. Memory pressure is possible, not a
proven sole cause. Model size alone is not a solution. No automatic quarantine
clear: after timed-out probes, exact empty owned markers were cleared only under
the exclusive gate lease after two idle slot observations and healthy backend.

The task candidate remains disabled and is not connected to the live composer.
The restored demo retains the legacy learner-only pilot and its known teacher
defect. BUG-AI-0001 remains OPEN. Mini/Plus/Pro remain production-unqualified;
Ultra is deferred. Current934/167tests and synthetic SQL checks do not establish
90%AIusage,100%conversational coverage, grounded answers or model understanding.

## Remaining acceptance and next work

### Restored owner demo

Fresh synthetic demo01a9fd77330040e5a433c80688d1280a is ready at
http://127.0.0.1:3000/login . All six actual Identity logins/sessions, seeded
learner/teacher lists, real legacy Mini pending-fee lookup/contextual follow-up,
exact balance, GET recovery and Manual optional-field save passed13checks.
The Manual test leaves13synthetic students, four teachers and four batches.
Seven restoration/boundary checks also passed, including new task catalog404.
Six unique generated passwords were rotated/verified; previous credentials are
revoked. Current secure paths are resolved through private ACTIVE_DEMO.json;
never commit or paste those password values. The old dataset/key ring remains a
private backup; this fresh database is not a restore of that old database.

### Open work

1. Add authorized factual reads, domain/context switching, references and
   source-backed answer composition across the existing Manual task catalog.
   Teacher correction is one regression, not the product boundary.
2. Complete task additions/replacement, controlled in-flight cancellation and
   retention/purge; current24-hour expiry is an access limit, not physical purge.
3. Bind existing protected domain operations and approvals; batch creation and
   dependent compound execution need actual transaction/idempotency safeguards.
   A draft or pending receipt must never be displayed as successful execution.
4. Qualify the real provider using fresh semantic/latency/context/permission
   packets before UI activation. Preserve the stop before new weight loading.
5. Integrate a single persistent conversation UI and typed result/review rail;
   finish Orbit+Aura/Prism Manual screen and physical-device acceptance.
6. Retain six critical issue chains, full Mini archive/platform gates, current
   paired CI access, staging/security/restore/rollback and Azure release gates.

Baseline CI5cd5f93:
https://github.com/AcademyDesk/AcademyDesk/actions/runs/45224325333 . Ordinary
API/SQL/private-media/frontend job succeeded; required paired Core job failed
at missing private-source access. A securely configured read-only
PENTA_SOURCE_READ_TOKEN is still an owner/admin prerequisite. No token printed,
permission widened or required job skipped. The new checkpoint needs its own CI.

Stay with Sol High for the next security/orchestration/native work. Routine
documentation/visual polish can use Sol Medium. No Azure/main/model activation.
