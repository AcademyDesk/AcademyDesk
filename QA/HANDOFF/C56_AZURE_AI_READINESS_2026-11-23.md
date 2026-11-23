# C56 — Azure preparation and conversational launch gaps

MODEL: Sol High
TOOL: Cloud Codex
REPOSITORY: AcademyDesk/AcademyDesk
BRANCH: codex/cloud-c56-azure-ai-readiness-20261123
ENVIRONMENT: managed cloud, local/read-only checks
BASE: c3617f28d458287189ca7173c8fe26ddea8fa271
STATUS: IMPLEMENTED / local validation passed; production BLOCKED
DATE: 23 November 2026, Asia/Calcutta

Cloud owns exactly these new paths:

- `QA/tools/azure-readiness-preflight.cjs`
- `QA/tools/azure-readiness-preflight.test.cjs`
- `QA/HANDOFF/C56_AZURE_AI_READINESS_2026-11-23.md`

TASK: Prepare a dependency-free local preflight that reports required deployment
inputs without exposing their values, retains the existing critical acceptance
guard and never calls Azure. Review current deployment/runtime wiring and map
current conversational limitations against the owner's 90% launch goal.

MUST NOT MODIFY: Desktop-owned native SQL/model/demo paths, shared root handoffs,
runtime/provider gates, workflows, application behavior, packages/locks, secrets,
cloud environment settings, original archives or frozen inputs. No main merge,
deployment/provisioning, public-access change or model loading/activation.

VALIDATE: Pure validation and subprocess tests for invalid/missing configuration,
credential redaction and blocked release behavior; existing production-guard
tests; current real preflight must retain open critical failures. Compare all
unclaimed base paths byte-for-byte. Verify remote publication SHA and exact CI.

STOP WHEN: bounded preparation is tested and published with explicit remaining
native, model, device, staging and release requirements. This packet cannot
qualify all AI workflows or authorize a deployment.

NEXT ROUTE: Desktop Sol High for native/model and combined acceptance; Cloud
continues only independent claimed source. Preserve the current synthetic demo;
do not run heavy local suites while Desktop's demo is active.

## Outcome

C55 is already integrated and natively verified by Desktop on this base. This
packet adds an offline Azure input preflight and a source-bound launch gap map.
It does not activate production AI, change a workflow, connect to Azure or
declare the application production-qualified. Only the three claimed paths change.

The owner's target is conversation-driven completion of 90% of eligible school
operations, with Manual available. This source does not establish that target.
Deploying it now would not supply full conversational AI: the actual provider
accepts only Development/Testing and its Mini tool registry contains precisely
`SearchLearners` and `GetLearner`. The same production/staging limitations apply
to existing guarded assistants through their own policies. Do not set a public
host to Testing or remove these restrictions as a deployment workaround.

## Exact inherited progress and blockers

Desktop c3617f28d458287189ca7173c8fe26ddea8fa271 records combined C55 acceptance:
837 PENTA API, 1,020 client, 214 browser assertions and native SQL exact-money,
legacy recovery, audit rollback, unchanged-ledger and single-dispatch checks.
Its restored synthetic demo also passed 13 model/Identity/SQL checks including
six role logins, nine pending learners, a two-Piano-learner follow-up, exact
INR2250.00 and one Manual save. These are inherited Desktop results, not tests
rerun by C56. The demo is laptop-loopback only and expires per its local handoff.

[Combined source-bound report](../REPORTS/PENTA_C55_COMBINED_ACCEPTANCE_2026-11-23.md)
and [current demo instructions](INTERACTIVE_DEMO_2026-11-23.md) preserve the evidence.
One earlier built-in Docker bridge identity drift remains a resource qualification
failure despite a later stable observation; C56 does not repair or erase it.

Hosted Academy run [45119449295](https://github.com/AcademyDesk/AcademyDesk/actions/runs/45119449295)
is complete on exact c3617f28:

- Ordinary API/SQL/private-media/frontend verification: **SUCCESS**.
- Protected onboarding/private Core transport: **FAILURE**, step
  `Require access to pinned private Core source`.

The paired workflow now pins Mini `2686d90e3d3fb57e58889e09969a336312e6d9a5`.
Do not replace it with the superseded c3c61b9 pin. The read-only
`PENTA_SOURCE_READ_TOKEN` remains an owner-provisioned Actions prerequisite.
This packet neither obtains nor configures credentials. A cloud clone's read
access does not establish that a GitHub Actions job has cross-repo permission.

Desktop's Mini report for run45118369025 retains 1,748 passes, 13 failures,
63 errors and one skip; reported failures/errors require genuine private archives
absent from that runner. Reviewed portability/original-input reconciliation and
289 selected checks are progress, not a whole-suite pass. Use a reviewed secure
original-archive provisioning route; never commit private archives, change their
expected hashes, alias invented replacements or skip the tests to make CI green.

## What the new preflight checks

```text
node QA/tools/azure-readiness-preflight.cjs --help
node QA/tools/azure-readiness-preflight.cjs
```

Run with already securely provisioned environment inputs. Help lists required
names. Never paste credentials into command arguments, chat, Git or a committed
env file. No setting values, supplied arguments, private paths or loader errors
are included in the output; fixed setting names and failure codes are emitted.

The tool validates the current workflow's required deployment/source-access
inputs; compatible registry/app/resource names; separate exact HTTPS web/API
origins and optional build/CORS parity; Production environment; explicitly
configured private Blob and encrypted persistent key-ring inputs; certificate
separation from key storage; optional bootstrap consistency; and exclusion of
evaluation/development credentials and unqualified activation flags.

It also executes the existing critical-record guard without rewriting records.
An unreadable record set fails closed. Exit1 means local input/critical-record
checks failed; exit2 rejects unsupported arguments. Exit0, if achieved, means
only those local checks passed. Every report explicitly retains:

```json
{
  "productionQualified": false,
  "deploymentAuthorized": false,
  "azureResourcesVerified": false,
  "aiCoveragePercent": null
}
```

Required runtime inputs are a preparation contract, not proof that the current
workflow wires them. The tool is opt-in and is not inserted into CI/deployment.
It validates syntax/presence, not credential validity, RBAC, filesystem mounts,
actual Azure settings, certificate contents, connectivity or evidence authenticity.
Secret values belong in the appropriate secret store even when supplied as
environment inputs during a local check. There is no Azure login/network call,
resource creation, SQL access, model operation or deployment.

## Azure gaps found in the current repository

| Finding | Required acceptance / next implementation |
| --- | --- |
| Production/staging provider path is unavailable by current policy | A separately reviewed private production-serving design and qualified typed tool/approval path; preserve tenant/actor authority and existing closed gates until accepted. A larger model or a flag change alone does not implement it. |
| Deployment config sets SQL, CORS, registry and startup migrations, but does not provision the new private-media settings/RBAC | Inventory actual Azure first. Verify system/user managed identity, private Blob container/access, Blob data permissions, network/DNS and both authorized downloads and revoked-ticket refusal. Preserve existing private media. |
| Workflow does not provision persistent encrypted Data Protection storage/certificates | Provision protected shared key storage outside the image/web root with correct mounts, certificate secret delivery, old decryption certificates and rotation/restore. Prove token/receipt continuity across replica/revision changes; never fall back to ephemeral or unencrypted keys. |
| Source has a DB-aware `/health`, but deploy YAML has no explicit readiness/smoke/rollback stage | Verify actual Container App probes and target revision; add reviewed DB/Identity/authenticated read-only smoke and rollback criteria in a separately claimed workflow packet. `/health` alone does not cover AI, Blob or every connection. |
| Current workflow mutates production app settings and enables startup migrations | Rehearse the exact image/schema/config sequence in isolated staging. Record migration owner/lock behavior, backup/restore, failed rollout and compatibility with the prior image. Do not assume an image rollback reverses a schema migration. |
| No staging promotion path is specified in deploy YAML | Prepare the resource/revision/traffic plan and source-bound approval. Verify SQL PITR/restore, private Blob recovery, config/secret rollback and production smoke before proposing promotion. No paid resource is created by this packet. |
| Deployment uses `AZURE_CREDENTIALS` and an existing production environment | Verify least-privilege identity, environment review restrictions, registry pull and secret lifecycle. Federation is a possible later reviewed change, not configured by C56. Never print current credentials. |
| Deployment web build has no explicit Node setup, while verification pins24.19.0 | Pin the agreed locked build runtime in a separately claimed deployment change and verify identical frontend configuration/artifacts. Local verification does not establish the deploy runner's version. |
| Existing production-deployment.md says deploy follows a main push | Actual YAML is manual `workflow_dispatch`, reusable verification, main-only deploy, production environment and critical guard before Azure login. Update stale documentation under a separate claim; do not treat an ordinary push as deployment authorization. |

These are repository wiring findings. Actual Azure resource configuration was
not queried, so omitted YAML is not proof that a resource is absent or exposed.
Environment access/privacy and public-internet policy were not changed.

## Conversational outcome map and next packets

This extends the existing operation inventory and acceptance program. It is a
launch gap map, not a new endpoint audit, coverage percentage or universal model
test plan. An endpoint, a guided form and an accepted free-prompt outcome are
different evidence levels.

| User outcome | Current evidence / limitation | Next acceptance |
| --- | --- | --- |
| Find learner, pending fees, follow-up/correction/ranking | Mini SearchLearners/GetLearner, bounded read-only context; Desktop local demo and narrow held-out evidence | Wider fresh cases, natural phrasing/languages as required, ambiguity/corrections, tenant/role refusals, exact amounts, deployed private-provider latency/outage |
| Create/update learner or teacher | Shared deterministic services and guarded onboarding/intake paths exist; not tools in this Mini free-prompt registry | Bind allowed natural-language intent to existing typed validation/review/approval; verify actual authorized write, audit and recovery on current combined source |
| Enrollment and lifecycle changes | Existing protected guided enrollment and lifecycle UI/service evidence; Testing-only | Model-driven clarification and approved execution through the same services; cover every supported lifecycle outcome and conflict/refusal |
| Schedule/reschedule/make-up lessons | Existing protected scheduling services and guided browser paths | Conversational field/context binding, availability/clash handling, explicit approval, no duplicate execution and durable receipt |
| Teacher attendance | Existing assigned-teacher guided roster/confirmation flow; Testing-only | Role/assignment-aware prompt path, clear confirmation and one committed attendance/notice outcome; handle absence of authority |
| Issue invoices, collect fees, propose adjustments | Existing guarded approval/execute/recovery workflows | Bind prompts to current ledger/approval rules, exact money, separate approval where required, concurrency/audit rollback and verified receipt |
| Expenses, payroll and reconciliation | Deterministic Manual workflows and bounded Testing finance reads; no universal Mini write capability | Implement and accept each permitted conversational outcome separately; retain zero-net/duplicate/payroll and finance controls |
| Leads, communications and delivery | Existing CRM/communication services and provider-readiness distinctions | Approved recipient/consent/scope, draft/review/send, recoverable delivery status; a queued notice is not delivered email/SMS |
| Private resources/recordings | Existing Manual/private-media native evidence | Authorized conversational metadata/actions where eligible, consent and private storage; accept actual recording/upload/download and live revocation on target devices |
| Student/Guardian self-service | Separate guarded portal reads, current learner links and permissions; not all-role Mini conversation | Qualified role-specific dialogue and outcome evidence under live permission changes; never broaden access from model context |
| Platform Owner operations | Separate guarded aggregate health/onboarding/support reads | Accept permitted platform outcomes independently; preserve secure Manual credential/role administration and required approvals |
| Pulse/Navigator/Twin/Autopilot and multi-step work | Product direction; broad end-to-end coverage not established by the Mini lookup demo | Define bounded eligible tasks, scheduling/approval/stop/resume semantics, then verify the actual outcome and audit; do not label a roadmap as implemented |

For every eligible outcome, record role, permitted input, original model/runtime
pin, clarification/correction behavior, required approval, authoritative result,
audit and recovery, Manual fallback, security-negative cases and an independent
held-out verdict. Keep exposed regressions separate from fresh evaluation.

Before scoring 90%, approve the eligible business-outcome denominator and record
every exclusion explicitly. Secure Manual administration and required human
approvals remain governed, not silently counted as automated success. Calculate
accepted eligible outcomes / agreed eligible outcomes; report critical-workflow
failures and per-role results independently so an average cannot hide them.
Also measure end-to-end p50/p95 latency, correctness, clarification burden,
duplicate prevention and outage behavior on the intended hardware/load. The
demo's single measured20.9-second first turn is not production latency evidence.

Preserve locked Mini/Plus/Pro tier selections and original artifacts. A small
lookup candidate's narrow score does not demonstrate the intelligence needed
for wider tasks. Evaluate the agreed tiers against the outcome set before any
promotion; do not choose/download/load replacements here. Mini/Plus/Pro remain
production-unqualified; Ultra remains deferred.

## Parallel ownership and release order

1. Desktop retains native SQL/model/demo/resource-drift diagnosis and physical
   device evidence. Keep current synthetic demo usable; no heavy concurrent suite.
2. Cloud can take separately claimed configuration/docs/workflow and portable
   client/contract packets from the current published source. Before extending
   domain/model adapters, agree the exact paths to avoid overlapping Desktop.
3. Release owner securely supplies private Core Actions access and genuine
   archive prerequisites, then obtains both exact-candidate hosted passes.
4. Close the six existing records only with their required final evidence:
   BUG-DATA-0001/0002/0003/0010, BUG-SEC-0001 and BUG-AUTH-REVOCATION-001.
5. Qualify the production-serving/tool path, accepted eligible conversation set,
   device/security and staging restore/private-Blob/rollback behavior. Prepare a
   concrete release candidate with its configuration/artifacts and smoke plan.
6. Seek separate release authorization only after the proposal is reviewable and
   the gates pass. C56 does not merge main, provision Azure or deploy.

## Validation and limits

- New preflight:69/69 passed, zero failed/skipped. Tests include invalid origins/DNS,
  registry/resource-name mismatch, private-key path rules, flag/credential refusal,
  fail-closed critical evidence and child-process credential/argument redaction.
- Existing production guard:15/15 passed, zero failed/skipped.
- Syntax checks and git diff whitespace:passed.
- Integrity:all1,837 unclaimed base paths, including16 vendor files, are byte-exact.
  The separate Mini checkout is clean at14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8;
  all33 frozen original paths retain their recorded hashes. No Desktop work is
  replaced and no newer Mini source is checked out over the preserved baseline.
- Current real local preflight:exit1,21 input failures and six open critical
  acceptance records. This cloud lacks a complete deployment input snapshot;
  these failures are not an assertion about actual Azure resources.
- Initial restricted test run preserved61/63 passes with two CLI capture errors.
  A diagnostic found child-process pipe EPERM. Explicit child-process error
  assertions were added; approved outer66-case and final69-case runs passed. No test
  was skipped or assertion relaxed; initial logs remain separate.
- No application, SQL, model or browser suite was rerun: their implementation is
  unchanged and inherited source-bound Desktop results are recorded above.

Raw local task evidence: `/workspace/c56-azure-ai-readiness-20261123/` contains
initial/final test logs, current preflight JSON, exact Desktop run/jobs responses,
source hashes/integrity and the post-publication remote/check receipt. Raw
credentials, original archives and Desktop evidence are not copied or uploaded.
The enclosing implementation commit is the return source; select it once onto
the current Desktop feature branch. No source change is inferred from a preview
status, and no feature-branch Actions pass is claimed without an exact run.

| Private local receipt | SHA256 |
| --- | --- |
| `preflight-tests-dns-final.log` | `701ab70007bfd8c7cf1f44aeb5812661164c2b329d8f61b150d6d058f2f65c61` |
| `production-gates-tests-direct.log` | `1d4103c192a6dbd70b18aa63ce207218d0913385c0f52697705ace3271cd63f8` |
| `current-preflight-dns-final.json` | `5a646b5d66762a92077ba88bbfd314dd26211729475acca1d6da88f3a7d765c9` |
| `source-integrity-final.json` | `0f1c58a4b8632c3606be396e499918e023380c9f97b039b4f3068ec7fe798ee6` |
| `desktop-ci-jobs.json` | `20696e6a54153606100e99d4969ef522d95dd2aafd895bba372f483cb0b9aeb6` |
