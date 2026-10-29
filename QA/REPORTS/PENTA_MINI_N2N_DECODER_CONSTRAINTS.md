# PENTA Mini N2n — decoder constraints and bounded disposition

Date: 2026-10-08. Engineering route: Codex / Sol High.

## Decision and stop point

**Both generation constraints are feasible on the installed native decoder and are implemented in one bounded Mini candidate. The language candidate remains NOT ACCEPTED.** Finite, self-contained schema alternatives enforce `replace -> remove=[]` and disjoint `set`/`remove`, without depending on unsupported conditional JSON Schema features. Strict runtime rejection remains necessary and unchanged.

The user approved this implementation after the read-only feasibility review and requested completion/handoff before returning to Academy Desk. This packet changes only the Mini generation schema, version identifiers, an independent test/suite, and continuation documentation. It does not authorize another tuning pass, model replacement, training, hosted fallback, Academy application edits or deployment.

Local serving candidate: package/service **0.1.13 / planner-0.12**, external protocol **0.1**. The planner identifier accounts for the generation-contract change; `SYSTEM_PROMPT` itself is unchanged, SHA-256 `4bb0ee4bc8dcda4a29babcc501b0bb4196a07efe8d5a20cc7d5be7df2c1c03d2`.

## Repository continuity

| Checkout | Branch | Unchanged HEAD |
| --- | --- | --- |
| `D:\AcademyDesk-codex-p0` | `codex/penta-search` | `d3c4ccfd6ea8344a73e390e866b43f512cb51e17` |
| `D:\PENTA AI Models` | `codex/academy-language-contract` | `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe` |

Applicable AGENTS/governance/routing/vision/enterprise roadmap, both handoffs and N2m report were read. Prior dirty work and evidence were preserved. Academy main was not edited. No stage, commit, push, merge or deploy. Older N2 checkpoints remain historical; their audits are not restarted.

## Actual schema-to-grammar path and feasibility

The actual installed inference image is `sha256:559ac229adefe0f7e2d4e32f5222f26927b6bb8ba44b9db08e2544afebf41984`, native binary **b11382 / 11fe02151f79c41d0d4af7da708755d73b9c0da6**, with the existing Qwen3.5-2B Q4_0 artifact. Inference container `c06a0392290ce1b9198ff49e2bf6d1e9d4c2891b88e115b40e0329dda580aca2` was not restarted/replaced. Existing context 2048, four slots, four threads and reasoning off remain unchanged; Mini's existing request temperature 0 and max_tokens 256 remain unchanged.

`src/penta_mini/inference.py` sends the intent schema through `response_format.type=json_schema`, not native tool decoding. The pinned Qwen specialization puts `inputs.json_schema` into `p.schema`; its PEG builder calls the native JSON schema converter, constructs a non-lazy grammar for this no-native-tools path, and the installed grammar sampler masks invalid continuations. llguidance is not enabled in this installed binary.

The pinned converter's `oneOf`/`anyOf` are grammar alternatives, not general exclusive JSON Schema validation. `uniqueItems`, `if`/`then`, `not`, `contains` and dependency relationships do not provide these decoding constraints. The branches therefore carry complete typed closed objects with literal modes and literal removal arrays. A schema validator accepting a keyword is not proof that this decoder enforces it.

Exact-revision converter/parser sources were inspected and six source hashes matched the revision in `proof-decoder-source-identity.json`: `common/json-schema.cpp`, `common/json-schema-to-grammar.cpp`, `common/peg-parser.cpp`, `common/parsers/qwen3-coder.cpp`, `src/llama-grammar.cpp`, `src/llama-grammar.h`. Local saved source/build and the installed ELF parser exercise support this path, without installing another inference backend.

## Smallest implementation

- Mini `orchestrator._intent_schema`: one replace branch with empty removals; sixteen refine branches, one per removal subset of the four existing fields. Each refine branch excludes removed names from the closed typed `set` object.
- Preserve all **65 unique removal sequences**, including permutations, through literal array enums. No duplicate or foreign removal field is legal. No user message, record value, benchmark answer or tenant data enters the schema.
- Preserve the existing snapshot compiler, execution schemas, runtime rejection of contradictory/malformed plans, request allowlists, tenant/role permissions, approvals, executable tools, operation policy, transport and external protocol. No stripping, repair, fallback or retry was added.
- New `tests/test_decoder_constraints.py` contains seven tests, including exhaustive sparse combinations; new `benchmarks/academy-decoder-fresh-v0.1.json` contains eight independently frozen language controls. Version identifiers incremented; pyproject dependency declarations did not change in this packet.

This finite approach is appropriate for four fields; alternatives and removal permutations grow rapidly with additional fields. Future expansion requires a fresh review. Grammar grows from **6,432 to 29,555 bytes**. The native grammar emits schema property order; it does not accept arbitrary JSON property permutations. Tests serialize in the decoder's fixed order while preserving every valid removal order. This existing ordering restriction is not an external host protocol change.

## Tests and frozen criteria

New schema-unit baseline against the prior installed package: **two failed / five passed**, zero errors/skips. Final source and rebuilt installed package: **230/230 Mini tests pass**, zero errors/skips. Unchanged Academy evaluator: **25/25 pass**. Existing Starlette/httpx deprecation remains; no dependency upgrade to silence it.

Decoder proof: **2,102/2,102 pass** both in the exact-revision native converter/parser build and in the **actual installed Linux ELF grammar engine**. Cases cover all 2,080 combinations of set subsets, ordered removal subsets and modes, plus duplicate/foreign/malformed shapes and unchanged other-tool controls: 188 valid / 1,914 invalid. Old grammar mismatches 1,900 desired constraints. This proof exercises complete-string grammar acceptance without loading weights; it is not an exhaustive real-vocabulary token-sampling benchmark. Actual candidate language inference supplies a separate transport/integration observation.

Eight new independent controls were frozen before first candidate inference, SHA-256 `ba28ef3a1bb175cef914623fa859fafdd954dfb5f525df1b0a34d3ad431644bc`, plus every identity/message/expected outcome of the prior **86** cases. Required acceptance was **94/94 exact**, valid contracts, unchanged state, diagnostic host compatibility, every required no-call assertion, strict malformed/contradictory runtime rejection, and matched response source identity. Any miss makes the candidate NOT ACCEPTED. Unit/grammar success cannot substitute for language acceptance.

One serialized complete evaluation, one observation per prescribed case, no overlapping inference/API/SQL benchmark and no candidate edits after inference began. Failures were retained; the prescribed full pass completed rather than selecting only passing cases.

| Suite | Exact / prescribed |
| --- | --- |
| frozen-api | 25/26 |
| diagnosis | 3/3 |
| context | 8/8 |
| safety | 4/4 |
| operation | 8/8 |
| prior-reset | 0/1 |
| omission-fresh | 6/8 |
| intent-fresh | 8/8 |
| transition-fresh | 7/8 |
| sparse-fresh | 11/12 |
| decoder-fresh | 6/8 |
| Total | **86/94 — NOT ACCEPTED** |

Identical N2m subset: **80/86 versus 60/86**, 21 fixes, one regression (TRANSITION-FRESH-01), five retained failures. Identical N2k subset: **69/74 versus 67/74**, five fixes, three regressions (FRESH-04, OMISSION-FRESH-06, TRANSITION-FRESH-01), two retained failures. These are bounded observations, not a universal quality claim. New independent controls: **6/8**.

All **94 contracts/state unchanged**; **93/94 diagnostic host-boundary compatible** (SPARSE-FRESH-06 fails). Required no-call assertions all pass. All eight failed IDs are preserved: N2-04, FRESH-04, OMISSION-FRESH-05, OMISSION-FRESH-06, TRANSITION-FRESH-01, SPARSE-FRESH-06, DECODER-FRESH-02, DECODER-FRESH-04. No connector was executed. Probe paths: **49 inference / 19 deterministic**, with **zero contradictory Search raw plans**; 26 frozen API raw plans are unobserved. No HTTP 429, transport error or ERROR response was observed; no inference retry was added or performed.

## Semantic failures and limits

| Class | Saved evidence / current outcome |
| --- | --- |
| Invented subject | N2-04 still adds Piano to a fee-only request; expected only Pending. Frozen API exposes materialized arguments, not raw model output. |
| Dropped retained filters | N2m N2-06 dropped Pending and SPARSE-FRESH-05 dropped retained name/subject. Both pass this candidate; this observation does not prove general retention. |
| Incorrect reset/removal | OMISSION-FRESH-05 emits refine, set Cello, remove=[] and retains unwanted sorting. OMISSION-FRESH-06 removes name/balance but retains sorting. TRANSITION-FRESH-01 emits refine with no removals, retaining old name, fee condition and sorting despite keep-only subject. |
| Display code vs stable ID / wrong old reference | FRESH-04 opens the prior displayed GUID instead of searching STU-77. SPARSE-FRESH-06 puts display code REC-617 in GetLearner learner_id; diagnostic host boundary fails. |
| Separate deterministic policy false denials | DECODER-FRESH-02 and DECODER-FRESH-04 return UNSUPPORTED before inference, raw_plan=null. The unchanged whole-clause query-removal exception does not cover their wording (the repeated article in the first, every other criterion in the second). These are policy failures, not decoder or model reasoning failures. |

Six failures return incorrect tool proposals; two return incorrect deterministic denials. All complete raw plans available for probe failures are retained in the copied result files. No failure was relabelled equivalent or removed. A stronger model alone cannot repair the two pre-inference policy denials.

The decoder prevents contradictory edits; it cannot infer the correct course, remember the user's intended retained filters, determine whether the user requested a reset, or distinguish a display code from a stable ID merely from schema types. A structurally valid wrong plan remains a failure. No claim of frontier parity, general reasoning improvement, live authorization success or source answer acceptance follows from this packet.

## Runtime and evidence

Only the API was rebuilt/recreated from the existing image with an offline `--network=none`, `--no-deps` package install. The image build excludes development tests by existing dockerignore; tests were copied separately. The previous live API's exact recorded test-tool versions were restored after recreation (pytest/jsonschema and their existing test dependencies); no upgrades or new runtime dependency declarations. Before/restored dependency maps match apart from Mini 0.1.12 -> 0.1.13. The test-tool restoration log is retained.

Failed initial build/copy layout, compiler include/stream serialization setup and initial grammar proof key ordering are retained separately. Proof serialization was corrected to the actual converter's property order; validity expectations and language cases were not weakened. Final successful builds/tests are separate evidence. No model-language retry was performed to address setup failures.

Evidence: local/untracked `QA/EVIDENCE/penta-mini-n2n-20261008/`, manifest with all independent grades, comparisons, **56 copied SHA-256 entries**, runtime identity and images. Original frozen API evidence: `penta-mini-language-a834abde-c8a2-4b27-a9c8-01b2f62e3071`. Mini ignored `results/n2n-decoder/` retains exact source/build/harness details; `results/academy-n2n-*.json` retains original probe results. Collector: ignored `.build-check/n2n-evidence.cjs`, using unchanged Academy `grade()`, not a replacement evaluator.

All six actual response-module hashes match source; the unchanged eleven suite hashes match their pre-inference capture, including the frozen API suite. API container `978357d1b07fc4e52fddd2530f4c485a1bbe34858a199de4e752bda621131278`, image `sha256:c7eb72ae4cc71208006b57e32122165f4f45b4e9c88d709b8ce05d28ceed6bdf`. Response identity is not signed deployment or model-weight attestation.

## Academy Desk return and separately scoped future work

**This bounded Mini packet is finished. Return to Academy Desk application work from `AI_HANDOFF.md`, keeping Mini-dependent acceptance blocked.** No new SQL/browser run was started after the failed language prerequisite. Existing failed N2h owned SQL and prior evidence remain retained; this packet claims no newly accepted DB teardown. Linked three original plus three context SQL journeys, 15-turn browser, physical-device, privacy, production-capacity, security, enterprise and pre-Azure gates remain OPEN.

Stronger private-model evaluation is **warranted as a separately scoped hypothesis test** because semantic misses remain after the generation relations are enforced. The saved failures support evaluation, not proof that weights alone are the cause or that a larger model will pass. Do not start training, download/replace a model, change settings, use hosted fallback or provision Azure automatically. No more blind prompt-only loops or unchanged audits are prescribed.

Recommended engineering route: **Codex / Sol High**, consistent with existing repository routing. No model change for this chat is implied. Optional next Mini task, only if separately requested:

```text
MODEL: Sol High
TOOL: Codex
TASK: PENTA Mini stronger private-model evaluation scoping
SCOPE: READ-ONLY; use N2n evidence, do not implement or start inference.

Verify D:\AcademyDesk-codex-p0 codex/penta-search and D:\PENTA AI Models
codex/academy-language-contract; read current governance/handoffs and N2n report.
Preserve all dirty work, evidence, main, tenant/role permissions, executable
tools, strict runtime rejection, external protocol and the frozen 94 cases.
Use saved raw plans to isolate remaining invented-filter, retention/reset,
slot-assignment and code/stable-ID risks; do not rerun the current model.
Inspect locally available private artifacts and measured hardware capacity.
Propose one private candidate and a bounded resource/latency/privacy budget,
with an unchanged baseline and one serialized 94-case evaluation plan.
Freeze any additional independent holdouts before future inference.
Require every exact/no-call/state/source-identity criterion; no answer insertion,
weakening, contradiction stripping, retries, hosted fallback or automatic tuning.
Do not download, change files/containers/models/settings/dependencies/credentials/
databases, provision Azure, stage/commit/push/merge/deploy, or restart audits.
Return the evidence-backed candidate decision and exact separately authorized
implementation/evaluation packet. STOP; no runtime/model change in this task.
```

Final repository verification: both branches/HEADs preserved, nothing staged, `git diff --check` passes (inherited LF/CRLF warnings only). Pre-task hash comparison finds exactly the seven planned existing-file changes: Mini schema/version/package identifier plus three continuation documents, and Academy AI_HANDOFF. New test/suite/report/evidence only; no missing files or Academy application edits. All 28 prior N2m and 56 new N2n copied-evidence hashes match, as do six response-source and eleven frozen-suite hashes. No candidate edits occurred during inference. Final inference health is 200 and all four slots are idle. No enterprise or release gate is closed by this result.
