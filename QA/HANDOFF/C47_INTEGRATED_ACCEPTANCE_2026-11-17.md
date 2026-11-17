# C47 — current four-portal source and C46 integration

Cloud / Sol High, 17 November 2026, Asia/Calcutta.
Branch: `codex/cloud-c47-integrated-acceptance-20261117`.
Clean base: Desktop `a7e0b06e92bb851b7adbc9468353b095fca0c1c4` from the
current published integration branch, verified by fetch. C46 remains published
at `17e50d878aefadc264447f4419ab0d55707cba51` on its own feature branch.

## Ownership before integration

Only these paths may change:

- `apps/web/src/lib/penta-portal-reads.ts`: exact C46 implementation.
- `QA/tools/penta-portal-contract-boundaries.test.cjs`: exact C46 regressions.
- `QA/HANDOFF/C46_PORTAL_CONTRACTS_2026-11-15.md`: original source-bound handoff.
- this C47 task-specific handoff.

Integrate C46's completed implementation commit onto the newer four-portal
Desktop source. Preserve Desktop's API, SQL, Platform/Teacher/Admin/Portal UI,
root trackers, migrations, dependencies and all other files. Do not substitute
the private C45 comparison. Mini investigation is read-only: inspect exact hosted
CI annotations/jobs and source prerequisites without modifying source/evidence,
loading weights or importing model runtimes. Preserve the clean Mini checkout.

Validation: exact integrated-source full frontend and backend tests, relevant
client regressions, types/lint, locked production export and existing controlled
browser checks where available. Keep real SQL/device/model evidence separate.
Record exact candidate CI and all genuine failures; no skipped or invented
communication receipts, provenance or qualification.

Five P0 records plus the broader session gate, model qualification, measured
90% coverage, release CI/images and staging/device acceptance remain open.
No main merge, Azure deployment, environment publication, public-access change
or model download/loading/training/inference/activation is authorized here.

## Integrated source

Published claim: `d293c953` before source integration. C46 implementation
`17e50d878aefadc264447f4419ab0d55707cba51` was cherry-picked as
`e93d1dd5` onto the current Desktop four-portal checkpoint. Application and test
files applied without conflict. The only conflict was C46's handoff document:
the new base did not contain the earlier claim file. Its complete original
version was retained. All three returned C46 files match their source bytes.

This branch now contains Desktop's Admin/Owner, Teacher, Student/Guardian and
Platform Owner guided workflows together with C46's strict portal response
validation. No private C45 host or UI was imported. This is an isolated feature
checkpoint; main and the Desktop branch were not changed.

## Validation of this combined source

Raw results are private at `/workspace/c47-integrated-acceptance-20261117`.

| Check | Result / scope |
| --- | --- |
| Full backend | 2,493 PASS; zero fail/skip, fresh .NET build and TRX |
| Full frontend | 91 files / 2,500 tests: 2,497 PASS, 3 FAIL; zero skip/cancel |
| Retained three failures | Email/WhatsApp/Meeting historical HTTP receipts still require missing `QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`; no receipt, expectation or skip was changed |
| C46 response-boundary regressions | 35 PASS |
| Platform client regressions | 3 PASS; also included in the complete frontend run |
| Types / lint | TypeScript PASS; zero lint errors, 61 inherited warnings |
| Locked production webpack export | PASS; Next16.4.3 / React19.2.8; 90 static segment aliases |
| Existing Chromium entry/recovery runner | 24 groups PASS, sandbox enabled, zero model requests; six intercepted synthetic Teacher mutations across two isolated workflows, exactly one execution per workflow |
| Browser evidence and cleanup | Prior evidence copied before running; current report/screenshots copied into C47 evidence; owned temporary profile cleanup completed |
| Production guard | 15 tests PASS; actual checker still exits1 for the six critical acceptance records |

The Chromium runner uses intercepted synthetic responses. These results do not
replace Desktop's live SQL/browser reports or qualify real model interpretation,
physical devices, provider delivery, production data or release deployment.
Backend totals are source-bound and must not replace larger historical/local
totals from other branches. Wider parallel-return reconciliation remains needed
before selecting an exact production candidate.

## Mini CI diagnosis — read-only, exact current source

Mini remained clean at `14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8`.
The existing hosted run
[42234314840](https://github.com/AcademyDesk/PentaMini/actions/runs/42234314840)
failed at `Run python -m pytest -q`. GitHub check/annotation endpoints now return
200. They report exit code1, an actions Node20 deprecation warning and an Ubuntu
runner-label migration notice; they contain no individual failed-test details.
The earlier log-download redirect remains policy-blocked, and no alternate
download route was used. Hosted case-by-case results remain unverified.

The exact source was independently reproduced locally using its existing Python
3.12 venv, the established Academy node_modules overrides and the unfiltered
pytest suite. No package installation or source/evidence edits were performed.
Test caches were disabled and temporary files stayed in the owned C47 directory.

| Local full pytest result | Count |
| --- | --- |
| Passed parent cases | 1,738 |
| Failed cases | 13 |
| Setup errors | 63 |
| Inherited skip | 1: pinned offline native proof binary absent |
| Passed subtests | 5 |
| Versioned exact-original binding controls within that run | 74 PASS |
| Frozen original files rehashed unchanged | 33/33 |

The CLI parent-case counts above total1,815; JUnit counts the five passing
subtests too and reports1,820. Every one of the76 failures/setup errors has a
`FileNotFoundError` for excluded archived inputs. This local run did not uncover
a new code assertion failure. It is not a claim that all hosted failures have
been individually inspected or that Mini is qualified. The original suite
remains failing, with its inherited skip intact; no gate was weakened.

### Directly missing inputs

These are the seven paths directly reported by this run, not an exhaustive
inventory of the parents/source files referenced inside their receipts.
The first six belong under the original `D:/PENTA-Models` archive root:

| Missing archived input | Purpose |
| --- | --- |
| `operations/e5r61-readiness/review-fa5ad2c1d0e7/receipt.json` | Original curriculum/readiness receipt and referenced source binding |
| `operations/e5r61-readiness/review-fa5ad2c1d0e7/exclusions.json` | Original exposure exclusions |
| `operations/review-20261026-development/pending-20815471c6a8/development-review.json` | Original pending development proposal and authority context |
| `runtimes/llama.cpp/11fe02151f79c41d0d4af7da708755d73b9c0da6/llama-source.zip` | Exact original native source archive |
| `operations/trainer-preflight-20261026/lock-bd602eb5861f/candidate-lock.json` | Frozen candidate metadata lock |
| `operations/trainer-preflight-20261026/proposal-7839647748f2/acquisition-proposal.json` | Frozen setup proposal |

The seventh belongs in the original Mini checkout's excluded evidence folder:
`operations/evidence/e5r57-1fbe9d2228fa/trial.json`.

The source transfer explicitly excluded original local evidence/runtime archives.
The hosted workflow checks out Git, installs Python dependencies and runs all
tests; it has no step that supplies these original private inputs. That is a
concrete test prerequisite gap. Missing files must not be recreated, path-aliased
or replaced with synthetic content. Do not change pinned hashes or normalize
owner/source identifiers to manufacture a pass. The existing portable v1 verifier
already preserves exact original identity; its accepted Desktop genuine-input
return remains valid only for its recorded source and artifacts.

Desktop/native owner next: recover the original byte-pinned archive inventory and
run the existing original/portable checks locally, or prepare a private artifact
handoff with the exact referenced parents and hashes. Do not commit those raw
archives into either repository. Original Windows-only tests retain their own
platform boundary; v1 portable acceptance does not silently rewrite that boundary.
A future hosted qualification path must explicitly supply the real prerequisites
and retain the full historical result. No new weights or model execution is needed
to diagnose these metadata/source-archive failures, and neither is authorized here.

## Prioritized remaining acceptance

1. Use this combined feature checkpoint for the next exact-source protected
   browser/SQL acceptance; retain Desktop's current role/linkage checks and C46's
   malformed-response defenses. The native laptop owns SQL/migration validation.
2. Resolve Mini's real archive prerequisites and inspect exact hosted failures.
   Prepare the first private learner-search qualification packet under the
   existing explicit stop before model loading/inference. Synthetic/guided
   outcomes do not qualify Mini or establish the90% target.
3. Complete critical release acceptance for collection `BUG-DATA-0001`, adjustment
   consumers `BUG-DATA-0002`, payroll `BUG-DATA-0003`, void/restoration
   `BUG-DATA-0010`, private media `BUG-SEC-0001`, and broader session revocation
   `BUG-AUTH-REVOCATION-001`. Preserve their bounded passing retests; no closure
   is claimed from this packet.
4. Run the selected integrated candidate's hosted CI, API/web image/security
   verification, physical-device acceptance and staging migration/restore/rollback.
   Feature pushes alone do not trigger the current Academy verification workflow;
   no workflow dispatch, PR, main merge or Azure deployment was performed here.

Environment observations were current: running/connected, restricted network
policy enforced. No privacy, VPN or public-access setting was changed. GitHub
read/feature push work through the managed outer executor. Laptop/D: access is
not claimed. All Mini/model/vendor and unclaimed application source is preserved.

SOURCE CONTINUATION / NOT PRODUCTION QUALIFIED. Mini/Plus/Pro remain unqualified,
Ultra deferred, 90% unmeasured. No model download/loading/training/inference or
activation, environment publication, main merge or Azure deployment occurred.
