# PENTA Mini N2q — query-removal policy repair

Date: 2026-10-08. Route: Codex / Sol High. Scope: source-only Mini policy repair and deterministic regression tests.

## Result

The N2n decoder candidate remains **NOT ACCEPTED**. Its two saved pre-inference false denials (DECODER-FRESH-02 and DECODER-FRESH-04) are fixed in a **source candidate**, not yet in the running Mini API. The other six N2n language failures have not been repaired or regraded. This is not a claim of 94/94 language acceptance, a live Academy integration pass, or production readiness.

The exception for `remove` now recognizes a query-item qualifier on *each* conjoined item (`the name and the ordering`), `every other` on a query criterion, and singular `criterion`. It still requires the whole removal clause to contain only named query controls and a query marker. It derives no search arguments, grants no tool, and does not change the runtime allowlist, host authorization or planner contract. Clauses referring to records, profiles, balances or other data still signal delete; separate send/delete clauses still signal their respective unavailable operations.

Mini source service identifier is **0.1.14** (planner remains **0.12**, protocol remains **0.1**). The installed/running API remains the earlier 0.1.13 candidate; it was not rebuilt or restarted in this packet.

## Evidence and scope

Before changing the policy, a frozen new 13-case test file had SHA-256 `30188D281135A0B8BE7AB55DFC142B6A12D27FC53111266CA78AE5569282D7B3`. Its Linux baseline reproduced **5 false denials / 8 passing safety controls**. After the narrow grammar change, **13/13** pass, including both saved prompts, three independent query-edit phrasings, six real-action denials and two quote/negation controls. The candidate's complete Mini source test suite is **243/243 passed**, with one pre-existing Starlette/httpx deprecation warning.

The Windows Python 3.14 test collector cannot import Mini's POSIX `fcntl` gate; no behavioral result was taken from that attempt. Tests ran under the existing Linux API container with candidate source/tests/scripts/benchmarks copied only to an isolated `/tmp/n2q-baseline` path and selected by `PYTHONPATH`. The installed package and serving process were not replaced. New policy source SHA-256: `A7FAFA624674872B5106A7F5193998A3B40A96A58879B492276061D3071699A9`. No actual model inference, 94-case regrade, connector execution, SQL, browser or device test was run for N2q.

Repositories remain on `codex/penta-search` in `D:\AcademyDesk-codex-p0` and `codex/academy-language-contract` in `D:\PENTA AI Models`. Earlier dirty work/evidence was preserved. No stage, commit, push, merge, main edit, Azure deployment, credential change or model download/replacement.

## Next gate

Under a separately scoped evaluation, build/install the source candidate in an isolated local Mini runtime, verify response/source identity, then run the **unchanged 94-case** frozen gate once with no concurrent inference. Preserve all misses. The two policy cases must reach the model and produce exact expected search plans; bypassing the denial alone is insufficient. The six semantic failures and all contract/no-call/state/host-boundary criteria remain mandatory. If any case misses, stop and leave Mini NOT ACCEPTED. Do not start linked SQL/browser or Azure work until the language prerequisite passes. A stronger private-model evaluation for remaining semantic misses is a separate decision, not an automatic follow-up to this policy fix.
