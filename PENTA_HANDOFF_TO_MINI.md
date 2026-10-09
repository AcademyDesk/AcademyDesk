# SWITCH PROJECT CHECKPOINT: ACADEMY DESK → PENTA MINI

Date: 2026-10-09. Packet **N2u: read-language qualification dependency**.
Development route: **GPT-6.1 Sol High / Codex**, in the existing Mini project.
Status: **BLOCKED real-read acceptance; precise engine handoff**.

## 1. Ownership, branch and current integration

Academy Desk is the lead product and owns the complete integrated frontend,
UX, connector, domain/API/SQL logic, current authorization, receipts, audit,
end-to-end tests and enterprise release gates. This owner directive supersedes
the earlier request to skip the AI frontend. Standalone frontend is a later
PENTA commercial track. Mini owns shared reusable Core, inference, interpretation,
Tool Protocol, model-level context, learning interfaces and engine evaluation.
Do not edit another project's application files to resolve this packet.

Application worktree: `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`.
Inspected application HEAD before this documentation commit:
`d3c4ccfd6ea8344a73e390e866b43f512cb51e17`.
The eventual handoff commit is a documentation successor, not an application
runtime/model update. Recheck branch/HEAD/status before continuing.
`D:\AcademyDesk` main remains untouched; existing uncommitted work/evidence stays.

Mini worktree: `D:\PENTA AI Models`, branch `codex/academy-language-contract`,
observed HEAD `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe` plus prior dirty work.
No automatic synchronization of these chats or uncommitted files is assumed.

The existing first read feature already has a prompt-first PENTA AI/Workspace
switch, five capability helpers/context rail, authenticated scoped sessions,
provider, only SearchLearners/GetLearner, protected state, deterministic
Completed+Reconciled balance calculation, source artifacts and required audit.
Earlier narrow real Mini/SQL/browser passes remain historical evidence.
Current broad reliability and source-linked correction/reset acceptance is open;
no READY TO VIEW, production, all-five-capability or 90% completion claim.

## 2. Exact engine dependency and why the host cannot repair it

N2r source candidate 0.1.14/planner-0.12 has **86/94 exact** (NOT ACCEPTED).
243 installed Mini unit tests passed; shape correctness did not repair semantics.
All eight failed IDs below remain from N2n. The two old query-removal policy
false denials now reach inference, but the generated arguments are still wrong.
Current normal serving was last reported 0.1.13/planner-0.12; that observation
is from the supplied Mini checkpoint, not a fresh service probe in N2u.

Wrong valid filters can select the wrong authorized cohort. A record code
used as learner_id must fail closed, but denying it does not fulfill the request.
Academy cannot safely infer missing intent, remove model-generated criteria,
parse prompts into substitute tool calls or fabricate the requested result.
Those changes would duplicate shared intelligence and hide the model defect.
Host authority, finance calculations and strict validation must stay intact.

## 3. Exact saved reproductions

Each active protocol 0.1 request has `user_message`, `conversation_id`, supplied
`state` and `available_tools=["SearchLearners","GetLearner"]`. State shorthand
below lists filters, sort and displayed IDs relevant to the reproduction.
Expected arguments are the **complete flat execution arguments**, not sparse
internal planner edits. Tool = SearchLearners for every row below.

| Failed ID / exact message | Entry state | Expected arguments | Observed wrong proposal |
| --- | --- | --- | --- |
| N2-04: `plese find studnts with pendng fees` | Empty filters/results, null sort/selection | `{balance_status:Pending}` | Adds `subject:Piano` |
| FRESH-04: `New lookup: student code STU-77. Drop the old piano, unpaid and ordering filters.` | Piano/Pending, outstanding_desc, displayed `11111111-1111-4111-8111-111111111111`, selection null | `{name:STU-77}` | GetLearner for the old displayed UUID |
| OMISSION-FRESH-05: `Switch the subject to Cello, keep pending balances, and discard the ordering.` | Violin/Pending, outstanding_desc, two displayed IDs | `{subject:Cello,balance_status:Pending}` | Retains outstanding_desc |
| OMISSION-FRESH-06: `Remove the name, fee restriction and sorting. Keep only Violin and list those students.` | Mira/Violin/Pending, outstanding_desc, one displayed ID | `{subject:Violin}` | Retains outstanding_desc |
| TRANSITION-FRESH-01: `Leave just the Flute subject filter; no name, fee condition or order.` | Leela/Flute/Pending, outstanding_desc, three displayed IDs | `{subject:Flute}` | Retains name, Pending and sort |
| SPARSE-FRESH-06: `Forget the previous search; look up student record REC-617.` | Nila/Drums/Pending, outstanding_desc, empty displayed IDs | `{name:REC-617}` | GetLearner with learner_id=REC-617 |
| DECODER-FRESH-02: `Remove the name and the ordering; retain the course and unpaid condition. List students.` | Anika/Harp/Pending, outstanding_desc, no displayed IDs | `{subject:Harp,balance_status:Pending}` | Drops Harp and retains outstanding_desc |
| DECODER-FRESH-04: `Keep only the Harp course. Remove every other criterion and ordering, then list students.` | Anika/Harp/Pending, outstanding_desc, no displayed IDs | `{subject:Harp}` | Retains Anika, Pending and sort |

All selected learner IDs above are null. Complete UUID sets, request fixtures,
outputs and raw direct-probe plans are in the existing evidence/benchmarks.
Do not reconstruct shortened table state as a replacement for frozen fixtures.

Local Academy report: `QA/REPORTS/PENTA_MINI_N2R_POLICY_LANGUAGE_GATE.md`.
Evidence: `QA/EVIDENCE/penta-mini-n2r-20261008/manifest.json` and eleven result
files. Mini original results: `D:\PENTA AI Models\results\n2r-eval\`.
N2u read-only preservation verified all **11/11** result hashes against the
manifest; no new model/SQL/browser run. Report records 94 contracts/state valid,
93 diagnostic host compatible, required no-call pass and zero observed raw
Search contradictions. API raw plans for the original 26 cases were not captured;
do not claim those as observed. Source hashes are not weight/deployment attestation.

Reproduction procedure: read the N2r manifest and the existing frozen suite for
each ID; load exact entry state/tools; inspect the corresponding saved output.
Only evaluate again after an explicitly scoped candidate change. Use the
existing unchanged Academy `grade()` and Mini API/direct-probe runners, serialized
and without output edits, answer repair, repeated retries or overlapping runs.
Do not use this table or PC fixtures as model-training material.

## 4. Canonical contracts and compatibility

Consume Mini `docs/PENTA_PLATFORM_CONTRACT.md`,
`schemas/platform-contract/`, `docs/conversation-draft-n2t.md`,
`schemas/conversation-draft-n2t/` and frozen `benchmarks/conversation-draft-v0.2/`.
Mini is the canonical shared contract owner; Academy is its consumer.

Active Tool Protocol **0.1** and `/v1/chat` remain unchanged. Expected read
response: protocol_version=0.1, kind=TOOL_REQUEST, fixed bounded message,
tool_call with code-owned name/validated flat arguments, risk=READ, original
supplied state unchanged. Host owns state advancement from verified outcomes.
Record codes are search values; GetLearner uses an authorized stable identity
from the current displayed set. No model-provided source/permission grants.

`conversation-draft-0.2.0` and `core-boundary-draft-0.1` are **isolated drafts**.
They are not a live shared conversational endpoint or a protocol migration.
ReferenceCore is not imported into the current API. Its mock adapter is the
existing mock, not a second independent ERP. Natural free-form grounded prose,
durable memory, real approvals/actions and authenticated standalone gateway
remain pending. Saved N2t 264 tests and Core 63 tests validate synthetic boundary
behavior, not actual model decisions or source/tenant execution.

The host deliberately uses tighter limits than generic protocol maxima:
2,000-character input, ten displayed UUIDs and at most three nonempty filters.
Keep those safety limits; do not weaken the host to match a looser generic DTO.
Shared future native IDs require an agreed adapter/version, not silently
relaxing Academy's source references. Any breaking wire change requires a
separate proposal and coordinated adapter/tests before runtime activation.

## 5. Specific Mini task: one bounded qualification decision, then candidate

First reconcile this handoff with current source and N2t/platform work. Do not
restart the model project, repeat audits or redo completed fixture construction.

Prepare **one concrete private candidate/decoder/resource packet** using the
eight saved semantic failures and existing N2t evaluation plan. Determine
whether the selected settings/schema can represent the required decisions and
fit the complete tokenized prompt/output without truncation. Check local
available artifacts, exact base/quantization/tokenizer/template/runtime identity,
license, context/KV/OS headroom, and the measured laptop bounds. Separate model
semantics from deterministic policy and host errors. A larger model is a
hypothesis, not a guaranteed fix.

If an already-authorized local candidate change is available, implement/evaluate
only that bounded change with unchanged compatibility/safety controls. Otherwise
return the exact proposed artifact, resource requirement, acquisition/training
or setting change needed and a cost/quality rationale for approval. The owner's
offer of an Azure VM is not permission to provision it. No download, training,
GPU purchase, production install or hosted fallback is granted by this handoff.
Stop on an unmet resource/authorization boundary with a concrete decision,
not a vague "improve Mini" or an unchanged benchmark rerun.

Avoid further case-specific text rewrites or host substitutes. Preserve current
strict shape/compiler rejection, allowlist, no-call controls, bounded admission,
unknown-outcome quarantine, no retries and response-bound provenance.
Maintain independent fresh acceptance families; exposed 94 and PC fixtures
remain regression controls outside training, not unbiased final holdouts.

## 6. Acceptance, regression and exact return condition

Return **SWITCH PROJECT CHECKPOINT: PENTA MINI → ACADEMY DESK** with:

- Exact engine branch/commit and dirty candidate/artifact manifest; identify
  committed versus uncommitted source. No assumption that HEAD includes changes.
- Actual serving versus source package/prompt/protocol versions, artifact and
  tokenizer/config hashes; reported versus verified provenance clearly separated.
- Candidate decision/change, all test counts/failures, original eight dispositions,
  measured resource/latency/capacity observations, and limitations.
- Unchanged 0.1 compatibility or a separately proposed version/adapter request.
- Exact evaluation commands, frozen fixture/evaluation hashes and durable result
  locations accessible on this laptop; no customer credentials/raw private data.
- Application integration instructions, recovery/rollback and approved runtime
  enablement scope, or the exact resource/approval blocker if still unresolved.

**ENGINE READY FOR LINKED RETEST** only after a genuine changed candidate passes
all **94/94 exact existing read regressions**, no-call/state/host-boundary checks
and preserved safety/provenance tests. Record failures rather than replace
expectations. If qualifying the new conversational draft, its 48 applicable
engine turns and strict evidence/schema assertions also need actual candidate
evaluation; that is separate from keeping the read-only 0.1 pilot compatible.
Passing 48 expected-payload validators alone does not qualify model reasoning.

If the candidate does not pass, return **BLOCKED / NOT ACCEPTED** and the
evidence-backed next decision. Do not advertise a resolved integration, broaden
tools/history/writes, automatically try another model or train to the scoreboard.
Do not edit Academy code, deploy Azure, close enterprise issues or delete evidence.
Any Mini feature-branch publication needs its own scoped validation/governance;
this outgoing Academy documentation commit does not publish Mini's dirty work.

## 7. Academy task when Mini returns

Verify contract/artifact/source identity and reproduce the current accepted
candidate through Academy's unchanged provider/strict validation. Then rebuild
the existing SQL harness and run the prepared three original plus three
correction/reset real Mini/Identity/HTTP/disposable-SQL journeys, authority/
replay/audit/fault checks and the existing 15-turn linked browser protocol.
Use real sorted source identity, exact balances and persisted truthful provenance;
tear down only exact run-owned resources and retain all observations.

Complete/refine the existing integrated frontend under Academy ownership;
real receipts and source-linked cards, accessible clarification/recovery,
light/dark/responsive and manual fallback. No new parallel standalone app.
Only after source/security/browser acceptance provide **PENTA FEATURE CHECKPOINT:
READY TO VIEW**, exact local URL/role/conversation/branch/commit/limitations,
then stop major expansion for human review. Physical-device, privacy, enterprise
and pre-Azure gates remain independently explicit.

While this dependency is open, Academy may implement isolated consumer contract
tests/defined presentation work without enabling a draft route, copying Core,
altering engine files or claiming actual-model acceptance. Preserve the blocked
read feature; do not switch repeatedly for cosmetic issues. Sol High owns
consequential integration; Sol Medium fits settled bounded UI changes.

## 8. Publication and validation boundary

N2u changes Academy coordination documentation only. No runtime/model/DB/UI,
fresh inference/SQL/browser, main merge or Azure action. Existing application,
Mini and QA/EVIDENCE changes remain local. The outgoing handoff is self-contained;
local evidence/contract paths are explicitly not promised as remote artifacts.
Normal feature-branch documentation push is allowed by the lead directive;
it does not merge to main or trigger an Azure deployment request.
