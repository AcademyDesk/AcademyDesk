# N2l — focused Mini planner-contract review

Date: 2026-10-08. Disposition: **design review complete; implementation and acceptance NOT complete**.

## Scope and evidence

This is the single focused architecture review requested after N2k, not a new enterprise audit. It uses the existing N2j/N2k evidence and current Mini/Academy contracts. No application, planner, tests, fixtures, runtime, database or browser changes were made; no inference or unchanged tests were rerun. Local feature branches remain `codex/penta-search` at `d3c4ccfd6ea8344a73e390e866b43f512cb51e17` and `codex/academy-language-contract` at `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe`. Existing dirty work is preserved. No stage, commit, push, main or Azure action.

Controlling measured candidate remains **0.1.11/planner-0.10**, protocol 0.1, **NOT ACCEPTED**: 180 Mini units / 25 unchanged evaluator tests pass, but actual-model language gate is **67/74 exact**. Original 66-case subset is 61/66, versus N2j 63/66. Unit or schema success is not language acceptance. See [N2k results](PENTA_MINI_N2K_INTENT_CONTRACT.md) and [N2j results](PENTA_MINI_N2J_QUERY_INTENT.md).

Read-only verification of the saved packets matched N2j's 21 and N2k's 22 copied-evidence hashes and compared the seven failures. It made no new language observations. Six failures concern query slot assignment or retaining a removed condition. One is a deterministic query-only change denial before inference. The original three N2j failures now pass. N2j → N2k changed prompt, catalog and schema together: the output structure is a repair hypothesis, not an isolated proven cause or guaranteed improvement.

## Decision: sparse query edits, intent independent of authority

Retain the known requested-intent vocabulary separately from executable tool availability. Do not return to allowlist-conditioned intent generation: an unavailable GetLearner must be denied, not replaced by an available SearchLearners. No new executable tools, model, hosted fallback, model settings, retries, multi-model verifier or training.

Replace SearchLearners' mandatory four nested keep/remove/set decisions with this internal model-only arguments contract:

```json
{"mode":"replace","set":{"balance_status":"Pending"},"remove":[]}
```

```json
{"mode":"refine","set":{"subject":"Cello"},"remove":["sort_by"]}
```

The outer internal plan retains `kind`, `message`, `tool`, `arguments`. Search arguments require **exactly** `mode`, `set`, `remove`:

- `mode`: only `replace` or `refine`; selected by the model, not a keyword/context-presence router.
- `set`: sparse object using only existing `name`, `subject`, `balance_status`, `sort_by` parameters and their existing string/enum constraints. No extra keys, blanks, nulls, invalid enums or value repair.
- `remove`: actual array of zero to four existing parameter names, unique. Runtime validation must enforce uniqueness and reject unknown keys even if decoder constraints do not.
- `replace`: removal list must be empty. Start from an empty query and apply `set`; prior fields have no effect. A new search, “keep only X” or full reset can use this mode. Empty `set` is valid internally but final broad-search safeguards still apply.
- `refine`: start from a frozen supplied query snapshot, remove listed fields, then apply `set`. Omitted fields mean unchanged, not model-generated copies. Reject overlap between `set` and `remove`; do not invent precedence or silently strip a contradictory instruction.
- Reject old N2k field objects and legacy flat query plans. No production compatibility/sanitizing adapter or fallback mode/retry.

Capture the detached state snapshot at chat entry, before awaiting inference, and compile against that same snapshot; do not read a potentially mutated request state after the await. Missing context contributes no invented fields. Final state remains unchanged. Serialize only that existing snapshot as canonical JSON in the user context, not Python repr; do not promote user/state text into trusted system instructions or add hidden history/data.

Both modes remain in the same detached generation grammar regardless of tool availability. Keep the compiler in the existing provenance-covered orchestrator module (and policy changes in its already-covered policy module), or explicitly extend provenance before adding a new module; do not introduce unhashed planner logic. Keep the existing single model call and strict validation.

The compiled external SearchLearners arguments remain plain complete string fields, not `mode/set/remove`. The six-field protocol 0.1 and Academy provider/DTO/evaluator stay unchanged. All existing role, tenant, allowlist, risk, ID scope, compound-operation, broad-search, ordinal/pronoun, source, approval, audit and replay controls remain in force. Query context is not authorization; Academy reauthorizes and executes approved existing tools.

## Required semantics and retained failures

| Saved failure | Candidate mechanism | Exact outcome still required |
| --- | --- | --- |
| N2-01 fee query invents subject All | Sparse fresh `replace` | Pending only |
| N2-04 fee query invents subject Pending | Sparse fresh `replace` | Pending only |
| N2-05 Piano subject also becomes name | Sparse typed `set` | Subject Piano only |
| N2-08 Guitar correction also becomes name | `refine` changes subject, inherits other fields | Guitar/Pending/outstanding_desc, no name |
| OMISSION-FRESH-05 Cello becomes name | `refine`, subject set and sort removal | Cello/Pending, no name/sort |
| TRANSITION-FRESH-02 removed Pending retained | Explicit removal list | Flute/outstanding_desc, no name/balance |
| TRANSITION-FRESH-03 “change fee criterion” denied | Narrow query-only policy exception below | Leela/Flute/Clear/outstanding_desc |

These mechanisms do **not** prove the Mini will choose the correct mode or fields. The same exact frozen/fresh cases must measure that after implementation. “Keep only Violin” should compile via replace to subject only; a complete reset via empty replace remains subject to the existing broad-list guard. An unavailable corrected GetLearner remains UNSUPPORTED/no call, even with SearchLearners available. A Pending set plus balance removal is contradictory and must reject, not be repaired.

## Query-only change exception, not record-mutation authority

Current `change` mutating prefix rejects TRANSITION-FRESH-03 before Mini. Do not remove the prefix guard globally or trust the model's chosen mode as permission. Add a narrow **whole-request** boolean exception for explicit fee/balance query filter/criterion/condition/restriction changes to known Pending/Clear meanings, with at most a bounded instruction to keep the other search/query settings.

The exception determines only that this is a query edit; it must not extract or set tool arguments. Match the complete request, not a substring or first clause. Extra or ambiguous record edits, financial writes, sends or deletion clauses must fail the exception. Continue the independent full-text requested-operation scan and existing compound denials. The existing narrow remove-filter exception stays. Broader arbitrary-field change exceptions are outside this packet.

Freeze independent positive paraphrases and negative controls before evaluating the candidate: changing an invoice balance or fees; a query change followed by marking invoices paid, sending or deleting; mixed query/record removal; quoted verbs, negated requests and names containing verbs. Assert exact retained no-call outcomes, not just valid JSON. This bounded classification is not an exhaustive language/security proof.

## N2m implementation packet — Sol High

1. Add contract/transition/policy tests first and preserve their installed-current baseline failures. Freeze a small independent held-out mode/transition/denial suite before the candidate's first inference evaluation. Keep all existing 74 case identities and expected outcomes unchanged.
2. Implement the sparse grammar, strict compiler, snapshot and query-only policy exception in Mini only; shorten/align the prompt with the new semantics. Do not embed regression case codes/names or a table of expected answers into the prompt. Proposed candidate identifiers **0.1.12/planner-0.11** are reserved suggestions, not currently serving versions.
3. Migrate test-only plan encodings deliberately, preserving every existing final-output, availability, mutation-denial, state and malformed-value assertion. Add duplicate-removal, set/remove overlap, unknown keys/modes, obsolete shape, invalid strings/enums, missing context, snapshot immutability and non-call hidden-argument rejection. Execution-schema tests remain separate and strict. Do not drop tests or use total counts as equivalence proof.
4. Run affected/full Mini source and installed-package suites plus the unchanged 25-case Academy evaluator. Verify imported six-module hashes and package/service/planner identity against actual responses; reported source identity is not signed/model-weight attestation. No Academy app, frontend, provider, DTO, runner, evaluator or database modification is needed for this internal contract.
5. Make **one** serialized evaluation of all unchanged 74 cases plus the frozen new controls for this bounded candidate. Use the existing model, context, temperature, token budget, admission limits and deadlines; no overlapping inference/probes/SQL, silent retries or selecting the best rerun. Require every prescribed exact outcome, valid contract, unchanged state and boundary/no-call assertion, with no HTTP 429 or transport failures. Preserve all failures and raw evidence; an average is not acceptance.
6. If any semantic miss, new regression, unexpected tool/read substitution or transport error remains, mark N2m NOT ACCEPTED and stop the packet. Report the specific limitation and decision options; do not continue a blind prompt-tuning loop, replace/train the model automatically, restart the Astra audit or proceed to N3.
7. **Only if every language gate passes**, run the existing prepared fresh disposable-SQL acceptance: three original conversations plus three context conversations, then the full 15-turn linked browser flow. Verify persisted source/runtime/audit evidence, unchanged finance counts, normal owned DB/login teardown and remove only that successful run's owned container. Do not reuse/remove the stopped failed N2h container or prune others. A linked failure stops acceptance and preserves evidence.

No unchanged enterprise suites are repeated during this design review. N2m is implementation plus measured acceptance, not another inventory/audit. No more Astra review is a prerequisite for this packet. Frontend redesign, history, broader PENTA agents and conversational write capabilities remain downstream of the existing linked and authority gates, not abandoned or silently marked complete.

## Program and release status

The product goal remains one premium conversational PENTA AI workspace with Pulse, Executor, Navigator, Twin and Autopilot and an accessible manual workspace. The 90% AI / 10% manual split is a target for eligible, authorized operations, not today's measured coverage. This narrow read planner does not establish frontier-model intelligence, all CRUD readiness or the final frontend.

N1/N2, linked SQL/browser, physical Android/iOS, privacy, production-capacity, enterprise security/regression and pre-Azure release gates remain OPEN. Existing enterprise testing/remediation continues; this report does not replace its strategy. Runtime remains the last recorded failed N2k candidate until a later implementation changes it. No credentials or production data were accessed, no deployment performed, and no evidence deleted.

**Next task: N2m, Sol High — implement this bounded contract and policy repair, then run the unchanged and independently frozen acceptance gates.**
