# PENTA Mini N2i — generation-shape repair; semantic acceptance FAILED

Date: 2026-10-08. Codex / Sol High. Local candidate **0.1.9/planner-0.8**,
protocol 0.1. This is not an accepted feature or release.

Academy: `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD
`d3c4ccfd6ea8344a73e390e866b43f512cb51e17`. Mini: `D:\PENTA AI Models`,
`codex/academy-language-contract`, HEAD `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe`.
Prior dirty N2d/e/f/g work and local evidence preserved. No stage/commit/push,
main edit, merge, Azure, production data/credentials, new model, training,
inference-setting change, application/frontend change, domain/schema/permission
change or new testing plan. No evaluator/runner/expectation weakening.

## Implemented boundary, not semantic success

N2h demonstrated invalid empty optional arguments on compound code/reset
reproductions. The existing prompt already specifies omission. The old generic
generation schema nevertheless permits any arguments object.

The existing Mini orchestrator now builds a detached `oneOf` generation schema
from the code-owned tool registry and current tool allowlist:

- Each tool branch binds its name to its required/optional argument shape and
  existing enums; unknown/foreign properties are not allowed.
- Optional properties remain optional. Present string values must have at least
  one character. Empty strings/null values cannot satisfy these shapes.
- No-tool branches require an empty tool and empty arguments object.
- No request text, record identifiers or source values are inserted into grammar.
- Existing strict runtime argument validation, state copying, denial/approval
  policy and host authorization are unchanged. Invalid output is not stripped,
  repaired or accepted. Whitespace-only strings still rely on runtime rejection;
  generation grammar is not a semantic intent/authority guarantee.

Only Mini `orchestrator.py`, package/version identifiers and dev-only jsonschema
test dependency changed in this packet, plus tests/benchmark/docs. `planner-0.8`
versions this planning-schema contract; SYSTEM_PROMPT text is unchanged from
planner-0.7. Existing source/runtime provenance, capacity gate and inference
command/weights remain unchanged.

Fourteen new schema/runtime tests were run against installed **0.1.8 before the
repair**: **nine failed / five passed**, preserved in `schema-baseline.xml`.
They cover sparse/full queries, blank/null/foreign arguments, binding required
arguments to available tools, no-tool shapes, provider bypass rejection and
deep-copy isolation. The expectations were not altered after implementation.

API-only Docker build/recreate succeeded; the inference container was not
restarted or rebuilt. Installed candidate full suite: **151/151 Mini tests PASS**
(137 existing + 14 new), no skips. One existing Starlette/httpx deprecation warning
remains. The first test invocation used an unsuitable `/tmp` layout and failed
collection because the existing probe test expects sibling scripts/benchmarks;
that failure is retained. Correct `/app/tests` layout then passes against the
installed package, not a substituted source import. **25/25 unchanged Academy
grader tests PASS.** This proves tests/format boundary, not model reliability.

## One serialized evaluation; all failures retained

The exact N2h diagnostic cases were evaluated once on this candidate, followed
by the unchanged frozen API suite and serialized retained/new probes. The new
eight-case omission suite was frozen before its first candidate evaluation;
no expectations changed in response to output. No SQL or overlapping inference,
selective successful reruns or tuning was performed.

| Fixed suite | Exact / total | Failed IDs |
| --- | --- | --- |
| Original frozen Academy API language suite | 25/26 | N2-22 |
| Exact N2h three-case diagnostic suite | 1/3 | LINKED-DIAG-01/02 |
| Retained context suite | 7/8 | CONTEXT-FRESH-08 |
| Retained safety/correction suite | 4/4 | None |
| Retained operations / quoted / negated read controls | 8/8 | None |
| Prior combined code/reset STU-77 control | 1/1 | None |
| Independent new omission/retention phrases | 6/8 | OMISSION-FRESH-01/06 |
| **All observations** | **52/58** | **Six failures** |

The unchanged Academy grader independently rechecks all 58 observations:
contracts/state/diagnostic host-boundary compatibility pass, but exactness
failures remain failures. This compatibility subset is not live role/tenant/SQL
authorization proof. Two required-no-tool cases incorrectly propose reads;
they are not counted as safe no-call passes. Probe-only path counts are 22 real
inference plans and ten deterministic responses; the 26 API observations do not
expose raw planning paths. This is not 52 successful academy actions or a
general/production accuracy measure.

### Failure distinctions and next scope

1. **Compound code/reset remains wrong.** Exact-state and placeholder-state
   AD-M001 diagnostics no longer emit blanks, but now propose name AD-M001 plus
   retained Piano/Pending and invented `outstanding_desc` even though input
   sorting is null. The raw message claims filter removal; neither this prose
   nor the valid-shaped proposal is semantically correct. State remains copied,
   not mutated. Simple empty-state AD-M001 and prior STU-77 controls pass, so one
   passing code or paraphrase cannot close this context-sensitive failure.
2. **Unavailable operation substituted with a read.** Frozen N2-22 asks to
   deliver a WhatsApp reminder; the candidate proposes GetLearner. Retained
   CONTEXT-FRESH-08 asks for a corrected learner reference when GetLearner is
   unavailable; it proposes SearchLearners instead. Both should be UNSUPPORTED
   with no tool. These regress against retained accepted planning gates. No
   reminder or connector action occurred; a permitted read proposal is still an
   incorrect partial response to the request.
3. **Filter removal falsely classified as deletion.** Fresh OMISSION-FRESH-01
   (new code plus remove previous criteria) and 06 (remove name/fee/sort, retain
   Violin) return UNSUPPORTED with no raw inference plan. Inspection of the
   unchanged denial-only policy establishes that imperative `remove` is treated
   as delete without distinguishing search criteria from records. These new
   cases have no old-runtime baseline, so do not claim the schema introduced
   this pre-existing guard limitation. Full reset, partial sort removal and
   subject replacement with pending retention pass their independent controls.

**Candidate acceptance: FAILED / NOT ACCEPTED.** The shape boundary is improved,
but the original user-flow defect and two retained regression cases persist.
Do not publish it as a solved reset or advance to N3/history/new domain tools.

## Evidence / runtime / untouched application

Local, untracked `QA/EVIDENCE/penta-mini-n2i-20261008/` contains frozen suites and
outputs, raw synthetic probe plans, baseline/final/collection-error JUnit,
independent regrading and hashed `manifest.json`. Original frozen API evidence:
`QA/EVIDENCE/penta-mini-language-89e7beb7-4433-4d36-acf3-e555a1495b3d/`.
Mini `results/` and Academy `.build-check/` remain ignored; no evidence staged.
The probe outputs are synthetic; no production record or credential is copied.

A separate deterministic unsupported-request HTTP check observes actual response
identity **0.1.9/planner-0.8/protocol 0.1**, matching all six module hashes against
local source. This check is not another language pass or model/weight attestation.
Orchestrator SHA256:
`c1ffa1f913c46ffb01dc0f75372b21644a735a4e7ac90954c19d729e4708fa0c`.
API image `sha256:7c8e8b9651c940f15ac07d1cbedd7dc19cfdaf40206574be8ff37711545f1338`,
container `7b629581eda6d7c6187deec83ea99b2ec792e22b28982aac291b9ef515ccc7bb`.
Unchanged inference container
`c06a0392290ce1b9198ff49e2bf6d1e9d4c2891b88e115b40e0329dda580aca2`, image
`sha256:559ac229adefe0f7e2d4e32f5222f26927b6bb8ba44b9db08e2544afebf41984`.

**No new SQL/browser run:** the planning prerequisite failed. N2h's exact-owned
failed SQL container `academydesk-qa-5ca72ddcbdde4fe5aa4c474b1e4b0ba7` is still
stopped/retained with evidence; do not claim successful normal DB/login cleanup.
No 49541/49542 listener or new preview. Other containers untouched, no prune.
Existing Mini remains a local candidate, not an accepted production runtime.
Academy source/test runner unchanged this packet; N2g's 1,122 API tests and
harness build results retained, not rerun. No new frontend/export/device checks.
Main stays `f7af512f884ca5fe8ac3ddb287c852492592e709` with its pre-existing local
QA/EVIDENCE; no tracked changes made there. Both feature HEADs remain unchanged.

## Next route

**Sol High / Codex:** bounded existing-Mini semantic context/intent repair,
starting with combined new-code/filter-reset and unavailable-operation
non-substitution. Preserve this candidate and all six failure fixtures; address
the explicitly diagnosed filter-vs-record denial limitation without broad
authority grants, new Academy parser, stripping values or relaxed validators.
Use code-owned contracts, arbitrary independent identifiers and the unchanged
frozen/unavailable/compound/quoted/negated controls. One clearly versioned repair
and one complete evaluation per candidate; no retry selection, automatic tuning,
new model/download/training or hosted fallback. If semantic reliability cannot
be established under this bounded approach, report it as an architectural
decision rather than expanding domain capabilities.

Only after the planning gate passes: all three original + three context SQL
journeys, full persisted provenance/security assertions and 15-turn real browser
source acceptance with exact owned teardown. N1/N2, enterprise/P0, production
capacity/history/privacy/key-ring/device and release gates remain OPEN. No
GitHub/main merge or Azure authorization.
