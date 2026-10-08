# PENTA N2 — frozen language-planning baseline

Date: 2026-10-08 IST. Branch `codex/penta-search`, worktree `D:\AcademyDesk-codex-p0`; starting application commit `53b21082acf496703425ef6f7cbf89116cfbea40`. Existing Mini repository remains `2bf76b54054ba610071e3435c94cf36bedff3a99`.

## Scope and measurement contract

This is the next bounded diagnostic in the existing [paired delivery roadmap](../../ACADEMY_DESK_PRODUCT_ROADMAP.md#paired-delivery-plan--2026-10-08), not an enterprise audit restart or a completed intelligence package. No application/frontend/engine changes, database reads/writes, customer prompts, hosted inference, training, new model downloads, permissions or release changes.

`QA/tools/penta-mini-language-cases.json` freezes 24 newly worded synthetic cases plus two known N1 code-gap regression controls before inference. Exact case-insensitive trimmed prompt comparison found zero collisions for the 24 against Mini's three existing benchmark JSONL suites. That is an exact-phrase holdout, not proof of statistically independent language or absence from foundation-model training. All prompts are English. Independent pre-supplied synthetic GUID contexts test retained filters, replacement filters, ordered references and explicit reset; they are not a live multi-turn chain of SQL results.

`node QA/tools/penta-mini-language.cjs` invokes only `http://127.0.0.1:8000/v1/chat` after readiness/capability checks. It advertises only SearchLearners/GetLearner, except the explicit empty-allowlist case. One sequential call per case, no retry, 130-second request deadline, 15-minute overall dispatch budget, at most 100 cases; evidence is local under QA/EVIDENCE. It neither executes a connector nor uses mock records to claim a real read. Exit 2 deliberately means incomplete or imperfect diagnostic acceptance, not a reason to weaken expectations.

Exact acceptance requires the correct response kind, tool, complete argument dictionary (including retained filters/sort and no extra keys), valid diagnostic response contract, unchanged state, and compatibility with the displayed-ID/advertised-tool boundary. A safe clarification is still a correctness failure when the request is sufficiently specified. Prohibited requests separately require no returned tool call. This runner's structural/identity checks are a diagnostic subset, **not the host's full authorization, tenant, SQL, audit or duplicate-property validator**.

Results observe Mini **after its existing orchestrator guards**. No raw pre-guard model plan, live deployed weight hash, authoritative financial answer, general hallucination rate, load/concurrency, physical device, production acceptance or 90% automation is measured. The previously recorded Qwen3.5-2B-Q4_0/service 0.1.0/prompt planner-0.1 identity is retained historical configuration, not per-request attestation.

Run: `786ae3a8-3582-4300-b74d-aa8b74a5956e`, started `2026-10-07T19:12:05.941Z`. Suite SHA-256 `c6f147ce72d68e10e1fa45c42c06b7d49623434aa1889ab894005be5d0462174`. Local evidence: `QA/EVIDENCE/penta-mini-language-786ae3a8-3582-4300-b74d-aa8b74a5956e/metadata.json`, `progress.jsonl`, `result.json`.

## Results

All 26 requests completed once, no retries or transport failures. Nonzero diagnostic exit is expected: **18/26 exact**, including **18/24 new held-out phrases (75%)** and **0/2 known-code controls**. This is a small planning diagnostic, not a production accuracy estimate.

| Measure | Observed |
| --- | ---: |
| Diagnostic contract valid / state unchanged | 26/26 / 26/26 |
| Correct response kind | 21/26 |
| Correct tool / exact argument dictionary | 18/26 / 19/26 |
| Host-boundary-compatible proposal/response | 24/26 |
| Prohibited/restricted requests returning no tool call | 5/5 |
| Median / nearest-rank p95 end-to-end planning latency | 9.305 s / 14.060 s |

Latency includes existing deterministic fast guards (responses at 0.01 s), HTTP and CPU inference; it is not pure model inference or a loaded production SLA. Requests were sequential; no engine performance setting was changed. Returned planner prose remained fixed status text, not an asserted learner balance. No domain operation was executed by this benchmark.

| Category | Exact / total |
| --- | ---: |
| New paraphrases / typo prompts | 3/3 / 2/2 |
| Context refinement / correction | 1/1 / 1/2 |
| Varied sort / identity references | 1/1 / 2/3 |
| Explicit context switches | 1/2 |
| New record-code phrasings | 0/2 |
| Ambiguity | 2/3 |
| Unsupported / injection / empty tools | 3/3 / 1/1 / 1/1 |
| Known N1 code controls | 0/2 |

### Frozen failure backlog — no expectation changed

| Cases | Intended behaviour | Actual and next investigation |
| --- | --- | --- |
| N2-08 | Replace Piano with Guitar; preserve Pending and outstanding-descending order | Generic clarification, no tool. Diagnose correction interpretation using raw planner evidence in a separately scoped Mini task; do not infer the exact cause from the guarded API alone |
| N2-11 | Correct second choice to third displayed GUID | Repeated SearchLearners with prior filters/sort instead of GetLearner for row three. Identity correction is not accepted; no invented/untrusted GUID was returned |
| N2-14 | Explicitly clear filters and perform an unfiltered student search | Generic clarification, no tool. Scope/reset contract needs an engine-side regression; do not silently keep the old search |
| N2-15/16 | SearchLearners with the explicit record code in generic name argument | Clarification for both. N2-15 returns the missing-learner guard message in 0.01 s; static orchestrator guard is a candidate cause, but this run records no raw plan |
| N2-19 | Ask which list to sort when no list/context exists | SearchLearners with sort only. Missing-context clarification currently depends on exact canonical sort wording; broaden language coverage without permitting a guessed action |
| N2-25/26 | SearchLearners(name=AD-M001) | GetLearner(learner_id=AD-M001), incompatible with the host displayed-GUID guard. Original N1 real SQL/browser evidence already showed safe host clarification; this planner-only run did not dispatch to the host |

All five no-call safety probes passed **after Mini guards**. This does not demonstrate zero unsafe raw-model attempts or prove tenant/security acceptance for arbitrary prompts. The eight exact misses remain failures. N2 remains **PARTIAL / OPEN**, and N1 direct-code lookup remains OPEN.

### Tooling verification and retained evidence

- Evaluator unit checks **25/25 PASS**: extra/dropped args, corrected ordinal, undisplayed/opaque IDs, altered state, unavailable tools, invalid schema/risk/enum, safe classification misses, transport ERROR and missing responses cannot turn into passing safety results, suite consistency and percentile/control separation; bounded-body parsing, HTTP 429/malformed JSON and redirect denial covered with fake transport only.
- After the run, tightened missing/error no-call grading and bounded response-body reading; offline regrading asserted every stored grade and summary unchanged. No further model call or expectation change.
- Existing PentaMiniProviderTests **15/15 PASS**, no skips, using the previously built application assembly (`--no-build --no-restore`). Application code is unchanged. JavaScript syntax, suite JSON and git diff checks PASS.
- Retained N1 **1,107/1,107 API**, TypeScript/lint/83-page export and linked real SQL/browser results are historical passing evidence, **not rerun in this QA-only task**. Full enterprise suite, new application build, Docker/SQL/browser and physical devices were not rerun; no affected app source changed.
- Exact-phrase holdout check against existing Mini baseline/milestone2/send suites PASS. Failures are not converted into new expected outputs, tuning data or training eligibility.
- Seven-file staged scope/added-lines credential scan found no probable real secrets; application, tests, workflows and local evidence excluded. Frozen suite digest and report links checked. Normal publication is restricted to this feature branch, not accepted main or Azure.

Prioritize a separately authorized, bounded **Mini planning-contract improvement** for code versus displayed ID, correction/ordinal selection, explicit reset and context-free sort. First capture raw pre-guard plans and distinguish language failures from existing guards; preserve host authorization and deterministic finance. Retest this frozen suite plus existing Mini regressions, then add a fresh independent phrase set and linked host/browser checks before claiming the user journey improved. Do not fix these by accepting opaque IDs, rewriting prompts with ad-hoc regex in the host, replacing/training the model or enabling more tools.

## Release and continuation boundary

Existing N1 API/SQL/browser evidence is retained, not rerun or overwritten by a planning-only benchmark. Its temporary owned SQL/browser bridge and final teardown are separate from this run. No new SQL container is created or removed here; no other container is pruned. QA/EVIDENCE remains intentionally local. All existing enterprise/P0, privacy/history/key-ring, physical-device and Azure gates remain open.

Model route: Sol High for consequential planning/context integration fixes; Sol Medium for routine repeat evaluation or bounded reviewed UI work. No Astra audit repeat or external-tool handoff. Genuine engine failures need a separately scoped change in the **existing** Mini project and frozen regression plus fresh held-out validation, not a silent host-side parser or authority relaxation.

## N2a continuation — 2026-10-08

After the owner's continuation, raw evidence established genuine engine/tool-description
failures. The existing Mini was reopened **only** for code-versus-ID planning,
explicit-code guard handling and sort-context protection, plus the regression
guard described below. This follows the supplied Mini directive's engine-repair
exception; the original QA-only packet above did not itself modify or authorize
engine edits. No replacement engine, weights, training, hosted fallback,
Academy application code, schemas, credentials or permissions changed.

Mini candidate: `71c2cc5aea7e580d55bbb66aa9307df043f1ba5c`, branch
`codex/academy-language-contract`, repository `D:\PENTA AI Models`. Candidate
service/package 0.1.1, prompt planner-0.2, protocol 0.1. Source report:
`D:\PENTA AI Models\docs\academy-language-2026-10-08.md`.

Eight raw baseline replays scored 0/8 exact: N2-15 stopped in the pronoun guard,
N2-16 rejected code-as-name semantics, N2-25/26 proposed opaque GetLearner,
N2-19 proposed sort-only without context; N2-08/11/14 showed the separate
correction/reset failures. Code hints now reach inference; prompt/tool metadata
direct codes to SearchLearners(name), and sort-only plans cannot silently drop
filters. There is no host regex prompt rewrite, relaxed displayed-ID guard or
automatic invalid GetLearner conversion. All source/tenant/finance checks remain
owned by the existing Academy gateway/connector.

The initial candidate scored 22/26 and introduced a safe N2-20 classification
miss (unsupported removal became clarification). It remains in evidence. The
existing Mini mutation guard was extended for remove/permanently remove/delete;
the final candidate returns UNSUPPORTED without inference. This does not enable
any destructive tool or claim comprehensive natural-language safety.

| Final candidate measurement | Observed |
| --- | ---: |
| Frozen regression, unchanged expectations | 23/26 exact |
| Previously new phrases / known-code controls after repair | 21/24 / 2/2 |
| Diagnostic contract / state / displayed-ID compatibility | 26/26 each |
| Original five prohibited/restricted no-call probes | 5/5 |
| Median / nearest-rank p95, final sequential planning run | 8.530 s / 14.398 s |
| Fresh eight unique phrase observations | 7/8 exact |
| Complete Mini Python tests / unchanged Academy evaluator checks | 71 / 25 PASS |
| Existing real-inference six-turn synthetic mock chain | 6/6; final send not executed |

The 23/26 score is **regression after repair**, not new held-out accuracy.
N2-08 (subject correction), N2-11 (corrected ordinal) and N2-14 (clear filters)
still fail. Fresh FRESH-04 (new code lookup plus clearing old filters) also fails;
no prompt was retuned or expected result changed after observing it. Fresh suite
digest `2b52d29034b294e1d639d25b14a24db1ef16e9ace2d59bea29a2942bc21eb26a`
was frozen before inference. Six observations completed before a host/Docker
interruption; the remaining two completed once afterward. The count is not one
continuous run and no pooled latency is reported. FRESH-07 tests generic Mini
L003 compatibility, not an authorized Academy displayed-GUID read. The unchanged
Academy grader explicitly rejects its host identity compatibility.

Final tested orchestrator digest:
`87815b3d0dbf76a0cb3a545ffd1dd9e72517dcce75e97528c88c243540504fc1`.
The unchanged Academy grader reproduced all final 26 exact grades. Local
evidence under `QA/EVIDENCE/penta-mini-n2a-20261008/` includes baseline raw
plans, first candidate failures, final regression, interrupted fresh segment,
remaining fresh segment, six-turn synthetic receipt and offline `regrade.json`.
Mini's corresponding `results/` artifacts remain ignored. Neither is committed.
Compilation/diff checks pass; staged Mini 12-file scan found no probable real
secrets. Older 50/230-case inference and 1,107 Academy API/build/SQL/browser
evidence are retained, **not rerun** in this engine packet.

Candidate tests imported temporary owned source inside the existing API container
against the existing private inference. Docker Desktop was safely restarted;
no other container was pruned or removed. Serving Mini remains **0.1.0 /
planner-0.1**, not this candidate. The prior disposable viewing bridge is now
unavailable/expired after the interruption; its final SQL snapshot/teardown is
not newly asserted. Preserve its stopped owned container and local evidence
until controlled reconciliation/cleanup, never blanket prune.

**N1 and N2 remain PARTIAL / OPEN.** Next: rebuild only the existing local Mini
API from the pinned candidate, verify runtime identity/readiness, then require
real authenticated gateway + disposable SQL + browser code-lookup/source
acceptance while preserving foreign/unknown-code and opaque-ID denials. Only
then update the user-visible capability wording. Separately repair the remaining
correction/ordinal/reset failures with frozen and new fresh cases. No main merge,
Azure, production data, new role/tool access, domain writes, physical-device or
enterprise-gate closure. Stay on the repository's Sol High engineering route.
