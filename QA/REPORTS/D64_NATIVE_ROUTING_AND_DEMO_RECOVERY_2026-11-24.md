# D64 — native routing and local demo recovery

Date:24November2026. Academy base195e8d4e813c35239e52676ff82a1358c96fd06b.
Published Mini diagnostic source:d9c3d7550194685d4e3572befbc092d25541ff04 on
codex/academy-conversation-core-20261123. This continues the existing program.
No main merge, Azure, production data, training or new model loading occurred.

## Results and limits

- [x]207current model-free Linux checks, zero failures/skips, one inherited
  Starlette/httpx deprecation warning. Includes40new native-router cases and167
  retained task/API/capacity/operational checks. Intermediate runs overlap.
- [x] Native-template adapter independently checks context fit, response size,
  finish reason, known function, arguments, duplicate JSON, serial call bounds,
  shared inference lease and cancellation/quarantine. It is not registered by
  the serving app/task endpoint and performs no domain operation.
- [ ] Actual cached2Bsemantic acceptance FAILED in every profile below.
- [x] Local demo login/readback recovered;13students,4teachers,4batches and legacy
  Mini Available. New task catalog remains404/default-off. No new passwords or
  credentials were published; current private ACTIVE_DEMO/logins remain valid.
- [x] Future interactive SQL binding now explicitly pins its loopback port.
  Eight actual Docker/socket restart checks pass; real protected SQL/API
  Reconciliation module also passes using the fixed-port runner.
- [ ]4Bcomparison proposed and artifact verified only. Owner approval pending;
  no4Bload, inference, tier selection, automatic fallback or promotion.

## Native model findings

Used only the already-authorized cached Qwen3.5-2B Q4_0 development runtime,
context2048, CPU4, one slot, existing pinned image. Text was invented/synthetic;
no teacher/student/financial records were retrieved or changed by these probes.

| Variant | Actual verdict |
| --- | --- |
| V1 native parallel per-operation calls |3/4executed pass;5of9planned unrun. Compound selected student+batch+enrollment correctly; teacher read with no read tool selected nothing; student-only correct. Pause repeated update eight times and reached token limit; rejected. |
| V2 single bounded workflow/list |1/2executed pass;7unrun. Compound correct; teacher read selected Teacher.Create. Valid shape, wrong intent: failed. |
| V3 serial calls with planning acknowledgments |0/1executed pass;8unrun. Compound selected extra teacher/class creation and enrollment update, exceeding the bound. Rejected at24.542seconds; no partial success accepted. |

The gates ended healthy; no inference marker was automatically cleared. Prospective
read-vocabulary cases were never reached and are not implemented host tools.
No cutoff/assertion was relaxed. V1/V2 retain raw replies, packets and source hashes
but not complete intermediate source bodies; they are debugging observations,
not sealed release evaluations. V3 archives the complete executed source and raw
responses privately, with SHA256
d1964268b2bbb497115bdd53c5b2e56d1fb15cae89e5b9a80d4375c6b19de4dc.
Final private unit/native window:2fc3bd6e65de419585f2fe1b7a72f99a;
V3packet780d422b600740b1964feed9de830e6c. Overall native window verdict is FAILED,
even though the unit stage and resource restoration passed.

These results establish that native API support alone does not produce reliable
intent selection in this tested model/profile. They do not establish model size
as the sole cause: selection-only acknowledgments, prompt/catalog semantics and
orchestration remain part of the profile. Missing host reads, context switching,
field/relationship resolution, grounded responses and domain/approval integration
remain separate required work. Do not narrow the product to teacher lookup or
claim that buying a larger model solves those gaps. BUG-AI-0001 remains OPEN.

## Local restoration defect and correction

Pausing the exact owned synthetic SQL container released RAM without removing its
data. However, Docker assigned a new host port when restarting its empty HostPort
binding. The API retained the old SQL address and real login returned500. Container
IDs/images/inventories being unchanged was insufficient restoration evidence.

The current database, original keys and credentials were preserved. A private,
loopback-only opaque TCP recovery process now restores the original SQL address
to the exact same owned container, checking its ID/image/run label. It logs no
credentials, SQL or traffic, follows later port changes and exits with the parent
demo. This is temporary local demo recovery, not a production networking feature.
Fresh real Identity login, counts13/4/4, legacy Mini readiness and task-default-off
checks pass. Startup briefly reported Mini unavailable while the same cached2B
container warmed; final readiness passed. No model/tier was changed.

Permanent future-run fix:

- QA/tools/SqlHarness/DemoSqlPort.ps1 chooses an explicit free loopback port only
  when QA_INTERACTIVE_DEMO=1. Ordinary short QA retains127.0.0.1::1433.
- Run-ReconciledPayment.ps1 uses that binding in both existing Docker branches
  and verifies Docker retained the explicit port and loopback address.
- A port-selection race fails Docker startup; it never kills another listener
  or falls back to a public interface. Existing owned-target and cleanup guards
  remain unchanged.
- Verify-DemoSqlPort.cjs proves eight real Docker/socket checks against the cached
  Python image: explicit binding, reachability and same container/image/address
  after restart, plus exact existing-resource preservation.64MiB socket container,
  no SQL engine/model/download. This is not represented as SQL acceptance.
- Separately, the actual existing Reconciliation SQL/API module passes under the
  new fixed-port runner: real Identity/tenant controls, invoice1000/payment600,
  reconciled balance400 and excess500rejection, mixed statuses, exact settlement,
  cent rejection,86+7migrations and owned DB/login/container cleanup.

Private socket run7d1df8c17b5b425f9f4974f2cc23c206. Protected SQL window
07dbd7768d594c57bda4955bffa10261, SQLc5dd5919908b47eea867673c18bfffb9.
RAM admission remained3GiBfor SQL/native work, with bounded cache reclamation and
exact owned stop/restoration. No Docker restart, pruning or other container
deletion. The existing interactive launcher was also privately adjusted to pin
its Core port for future demos; current Core was preserved, not replaced.

## Concrete next model decision — not execution authorization

Mini operations/evaluation/native-routing-comparison-2026-11-24.json contains a
source/runtime-pinned nine-case, at-most15-minute local4Bcomparison proposal.
The existing Qwen3.5-4B Q4_K_M artifact is2740937888bytes; freshly verified SHA256
00fe7986ff5f6b463e62455821146049db6f9313603938a70800d1fb69ef11a4.
Only metadata/hash were read. Packetmay_execute=false; no4Bweights were loaded.

Approval is required because the owner's prior candidate-loading stop is retained
in Mini AGENTS.md. Proposal preserves the4619986080-byte free-memory start guard,
uses a stricter3.5GiBmodel container cap and768MiBfree-memory safety threshold,
no downloads/training/paid compute/Azure/activation, and restores the exact three
owned demo containers/data. Model and quantization both differ, so comparison
cannot isolate parameter count as a causal explanation. Even9/9would require
fresh held-out and real model/host/SQL/browser/domain acceptance before release.

While that decision is pending, safe host tool/read/context/approval integration
can continue. No owner response is inferred from elapsed time or this report.
Mini/Plus/Pro remain production-unqualified; Ultra deferred. Sol High remains
appropriate for the next security/orchestration/comparative-validation work.

## Exact CI at the preceding source

Academy195e8d4:
https://github.com/AcademyDesk/AcademyDesk/actions/runs/45239939649
ordinary API/SQL/private-media/frontend verification SUCCESS; required paired Core
job FAILED private source access. Dedicated read-only PENTA_SOURCE_READ_TOKEN is
still an owner/admin configuration prerequisite. It was not printed, substituted
with a broad Git token or bypassed.

Mini4a0ad0b:
https://github.com/AcademyDesk/PentaMini/actions/runs/45238649148
1801passed,13failed,63errors,1skip,5subtests passed. All listed failures/errors
are missing original private input/archive paths; no new task-module failure was
listed. No originals/hashes/approvals were fabricated or silently substituted.
These are not the new commits' CI results. Recheck their exact-SHA runs after push.
All existing enterprise, six critical-chain, physical-device, staging/restore/
rollback and Azure release gates remain open.
