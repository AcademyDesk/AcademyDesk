# PENTA Mini N2c — reproduced positional-read failure

Date: 2026-10-08. Academy `codex/penta-search`, starting `3daa601`;
Mini `codex/academy-language-contract`, starting `e3ef442`.
Validated Mini implementation published normally on that feature branch as
`a50bbd354ec0e8938064ad2d753c1d4beccfa0fe`; fetched remote/local SHA match,
0 ahead / 0 behind, tracked working tree clean. Eight scoped files passed the
staged secret/scope scan; ignored results and model/private files were excluded.
N1/N2 remain PARTIAL/OPEN. This continues the existing program, not a new audit.

## Task and boundaries

Route: repository Sol High / Codex, local private Mini and disposable SQL only.
May modify QA diagnostics, this genuinely diagnosed Mini reference operation,
its tests/version and checkpoint documentation. Do not modify Academy business
services, frontend, credentials, permissions, schemas, weights, inference settings,
main, Azure or the existing testing strategy. Preserve failed/local evidence.

## Reproduction — no retry-until-green

Before inference, fixed three independent four-turn trials: pending fees →
Piano → highest first → second displayed student. Same strict source identities,
balances and tenant checks as the existing harness. A QA-only decorator delegates
once to the registered real provider; it records pre-connector provider plans,
input/returned state and minimum synthetic receipt fields. It excludes bearer
credentials, settings, model prose and contacts. Records survive key-ring teardown
under local/untracked `QA/EVIDENCE/penta-mini-<run>/sequence/`.

Run `453f0f05a7384b12963fd414cbf90add`, SQL loopback 60814: **3/3 FAIL**.
Every fourth turn proposed SearchLearners with Pending/Piano/outstanding_desc,
not GetLearner for the second displayed GUID. The host correctly returned the
three SQL rows (INR 400/300/200), version 4 and no selected learner. This explains
the reproduced sequence-failure class; it is not a SQL authorization defect.
The original fourth-turn `.Single()` could throw before reporting that plan.
The diagnostic now explicitly requires Count=1, one row and the exact source;
no assertion was relaxed or failed trial discarded.

The new failure counter initially counted only academy A's seven invoices but
compared with eight seeded invoices including academy B. That secondary QA error
is corrected by counting the exact seeded invoice IDs. Independent owned SQL
checks found all eight invoices/seven payments unchanged. The terminal error of
this first diagnostic run is not falsely labelled a successful invariant check.

Earlier `20a608...` still has no captured receipt/stack; this reproduction does
not retroactively prove its exact cause. Its failed observation remains history.

## Narrow existing-Mini repair

Mini now serves **0.1.3 / planner-0.4**, protocol 0.1 unchanged. Full anchored
simple positional-read grammar resolves first–fifth against the host-provided
ordered result IDs. It checks tool availability, clarifies missing/short lists,
returns a READ GetLearner proposal and preserves the input state snapshot.
It does not use inference for that exact reference operation, invent an ID,
parse Academy prompts in the host, substitute a code for an ID or execute a read.
The unchanged host still reauthorizes and validates displayed GUIDs before SQL.
Compound requests, negated/corrected ordinals and added actions do not match;
they are not silently truncated into this operation. Complex correction/reset
failures remain separate work. This is reliability routing, not stronger LLM reasoning.

Source SHA-256:
`098fb30df8e7f321212cc465c05ed153bc6cf8c5f068719cde52b8336ce3fc92`.
Serving API image:
`sha256:caed3791e9c77572c6a441bf694e4a12c13a038fa91be4d1ceceffcf8d727034`.
Existing inference container `c06a0392...` / image `sha256:559ac229...` unchanged.
API import/package/hash, version and readiness verified after API-only rebuild.
Unchanged Dockerfile dependency ranges remain a separate reproducibility gate.

## Validation and setup observations

- Mini unit tests **86/86 PASS**, including 14 new positional contract cases.
  Local Python 3.14; one retained Starlette/httpx deprecation warning. Runtime
  probes exercise the Python 3.12 image, not an identical unit-test environment.
- Existing Academy evaluator **25/25 PASS**, unchanged.
- Four new positional phrases, frozen before observations, **4/4 PASS**.
  Digest `9ae1417164313e50d3ff3b421db7166d2acfe759cdb1f29bc8ac76a1029a0aef`.
  All have raw inference plan=null: this validates bounded Mini routing, not
  held-out LLM reasoning or arbitrary conversational ability.
- Harness build zero warnings/errors; runner AST and missing-browser-configuration
  rejection pass. The runner now rejects invalid preview configuration before SQL.

Intermediate repaired run `eccf84be7e04495a876a3b67d36f6a30` (65079) passed
three trials, duplicate-name choice and own/foreign/unknown code checks, then
hit the unchanged actor's 20-conversation limit during audit-fault setup (429).
It is **NOT a complete harness pass**. Extra diagnostics now use the existing
second same-academy fixture admin, preserving the original actor's quota budget;
no quota, policy or security expectation was changed. Missing browser environment
was also detected in this invocation, but the run stopped at 429 before that
bridge guard; do not claim a browser-configuration runtime failure there.

Final run `711e9d56b2d44ab09a3a5161bbf4c75d` (57751) passes three fixed trials,
the real Mini code/name/source checks and the existing full PENTA SQL/security,
role/tenant/ownership/replay/revocation/audit/currency/ledger guards. Five stale-tab
races are exactly 201/409 without 429. Final browser PASS: nine explicit chat
turns, all 201/no 429; exact ordinal/source, duplicate choice, own-code INR400,
foreign/unknown empty results and Student 360 INR300/400 parity. Manual/help
preserve the conversation; 44px choice targets, no overflow at 320/390/1440px,
light/dark and zero runtime/hydration errors. The mobile code screenshot was
visually inspected. Emulated viewports are not physical Android/iOS acceptance.
Unchanged frozen language suite: **24/26 exact**, N2-11/14 still fail. Digest
`c6f147ce72d68e10e1fa45c42c06b7d49623434aa1889ab894005be5d0462174`.
All grades independently reproduced by the unchanged Academy evaluator; 23 cases
record an inference plan, three use earlier guards/routing. N2-08's planner-only
pass is not linked correction acceptance. Previous combined code/reset remains
open and was not retested. No expectations or failed observations were changed.

Browser uses the previously accepted production export with unchanged frontend
source, real Edge/Identity/private Mini/SQL, no mocks or injected tokens. Frozen
regression and browser correctness probes share inference; their raw timings are
contended and must not be interpreted as an isolated latency baseline or SLA.
Historical 1,107 API tests, TypeScript/lint/83-page build, old 50/230 benchmarks
and physical Android/iOS results are retained, **not rerun** in this QA/engine slice.

## Resource disposition and next gate

Both failed current runs had exact container name/label/hostname/loopback and
SQL marker checks, with eight seeded invoices/seven payments retained before
removing only their owned containers. Their disposable databases are no longer
recoverable; diagnostic JSON/logs are retained locally. No prune or unrelated
removal. Older interrupted `90d24...` stays stopped/preserved; its teardown is
not newly asserted. Final browser snapshot confirms unchanged domain/execution
counts; negative cleanup refused the wrong marker, 88+7 migrations and exact
owned database/login/container removal passed, harness exit 0 (395 seconds).
The temporary API/static previews are stopped; existing Mini stays local.
Final local offline regrading verifies all three failing baseline receipts against
all three exact repaired fourth-turn receipts, unchanged model-returned state and
the original 26 language grades. Raw results, frozen copies and regrade summary
remain local in `QA/EVIDENCE/penta-mini-n2c-20261008/`; none are staged.

Next: complex corrected ordinal/filter reset and combined code/reset, with frozen
expectations, new phrases and linked source checks. Sol High for consequential
context debugging; Sol Medium later for bounded reviewed visual work. No Astra
audit repeat, external specialist, main merge, Azure or new domain capability.
All production privacy/history/key-ring, physical-device, enterprise/P0/security
and release gates remain open; the 90% AI / 10% manual target is not yet achieved.
