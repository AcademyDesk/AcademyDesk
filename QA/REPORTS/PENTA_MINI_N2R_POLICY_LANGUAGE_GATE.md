# PENTA Mini N2r — installed policy candidate and frozen language gate

Date: 2026-10-08. Route: Codex / Sol High.

## Decision

**Mini 0.1.14/planner-0.12 remains NOT ACCEPTED: 86/94 exact.** N2q repaired two pre-inference query-removal false denials, but both requests now reach the model and still produce incorrect search arguments. The same eight IDs fail as in N2n. No linked SQL, browser, device or Azure acceptance follows.

The two repaired-denial cases are now semantic misses, not policy denials:

- DECODER-FRESH-02: expected `subject=Harp` and `balance_status=Pending`; the generated replace plan omitted Harp and retained outstanding sorting.
- DECODER-FRESH-04: expected only `subject=Harp`; the generated refine plan removed no old criteria, so name, Pending and sorting remained.

The other six failures are N2-04, FRESH-04, OMISSION-FRESH-05, OMISSION-FRESH-06, TRANSITION-FRESH-01 and SPARSE-FRESH-06. Candidate policy repair did not fix these independent model/plan semantics. Exact total and failed set are unchanged from N2n; this is **not** a language improvement claim.

## Evaluation boundary and evidence

Both repositories remained on their existing branches/HEADs: Academy `codex/penta-search` at `d3c4ccfd6ea8344a73e390e866b43f512cb51e17`, Mini `codex/academy-language-contract` at `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe`. Earlier dirty application/model work and QA evidence were preserved.

The source candidate was built **offline** into an isolated `/tmp/n2r/site` package inside the existing local Linux API container. A separate loopback-only port-8001 process served 0.1.14; the normal port-8000 process continued to serve 0.1.13. Both used the existing private Qwen3.5-2B inference server and shared the same local inference gate. No model/settings/dependency declaration/host authority/allowlist/protocol change, connector execution, deployment, or external service was involved. The isolated process and exact `/tmp/n2r` test tree were removed after evidence copy; the serving process was not restarted. Final normal API response still reported 0.1.13 and all four inference slots were idle.

All **11 suite files** matched their N2n pre-inference SHA-256 records before the pass. The 26 frozen cases ran through the isolated HTTP API; the remaining 68 used the existing direct planning probe. The pass was serialized, one observation per case, with no answer edits, retry, overlapping inference benchmark, or candidate edit once inference began. A nonzero probe exit on an exact miss paused the batch wrapper after FRESH-04; its complete result was retained and the next suite resumed without rerunning that case. No transport failure or HTTP 429 was observed.

The unchanged Academy `grade()` independently regraded all 94 outputs. All **94 contracts valid**, **94 states unchanged**, **93 diagnostic host-boundary compatible** (SPARSE-FRESH-06 remains the one miss), all required no-call assertions passed, and no contradictory Search raw plan was observed. The 68 direct-probe paths changed from N2n's 49 inference/19 deterministic to **51 inference/17 deterministic**; the two newly reached model paths are precisely the repaired denial cases. Raw plans for the 26 frozen API cases remain unobserved. Six response-source SHA-256 values from the isolated API header and four direct-probe source hashes matched local candidate files; this is response-code identity, not signed deployment or weight attestation.

The isolated installed package passed **243/243 Mini tests**, with one pre-existing Starlette/httpx deprecation warning. This includes N2q's 13 policy controls and strict shape/runtime rejection tests. There was no new Academy API/TypeScript/SQL/browser/device run in this packet.

Local evidence is `QA/EVIDENCE/penta-mini-n2r-20261008/` (14 files: the 11 case results, manifest, transport probe and unchanged-grader wrapper). Manifest records all 94 grades, failed IDs, suite counts, source hashes and hashes of every result file; copied result hashes were verified. The Mini-side original is ignored `D:\PENTA AI Models\results\n2r-eval\`. The N2n evidence and failed outputs remain intact.

## Next route

**Stop Mini candidate work here.** Do not repeat the same 94-case run, weaken expected answers, add special-case prompt/answer fixes, or proceed to linked SQL/browser as if the language prerequisite passed. Return to independent Academy Desk application work on Sol High where useful, while keeping Mini-dependent gates blocked.

The six underlying semantic failures (and the now-visible two query-edit semantic failures) warrant a separately scoped **private-model capability decision** using the saved raw plans and measured hardware/latency budget. That decision should compare one private candidate against the unchanged baseline and pre-frozen holdouts; it does not authorize model download/replacement, training, hosted fallback, new tools, N3 scope, or Azure deployment automatically. All enterprise, security, physical-device, privacy, release and pre-Azure gates remain open. No stage, commit, push, merge or main edit in this packet.
