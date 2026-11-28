# D76 — bounded 4B repair window

28 November 2026. Owner explicitly approved at most three distinct, validated
Mini sources within 30 minutes; each attempt at most 15 minutes and stopped at
its first failed case. Same cached weights, images and 14 exact exposed cases.
No assertion/score change, automatic retry, SQL, host domain dispatch, production
activation, engine deletion, main merge, downloads, paid compute or Azure.

## Actual results

| Attempt / Mini source | Real outcome | Remaining cases |
| --- | --- | --- |
| ba050ad5d0334d1b97d8d9a7b465b4a8 / d144e0a |0/1: teacher-list 503; normal completed scope chose teacher/search/refine with no prior context |13 UNRUN |
| d87186695a0c44e5b9b35592fd8fb6d2 / ba35b97 |3/4: list, teacher correction, specialty pass; teacher-open supplied position2 AND name='Result 2', correctly rejected |10 UNRUN |
| 684f824dc78b4c25b47c6520b7d3705b /9f09e48 |7/8: all4teacher cases, student, batch, outstanding fees pass; current-student fee follow-up became an unfiltered financial search |6 UNRUN |

Every run: owned cleanup errors0, existing service inventory preserved, bounded
private model/Core/probe logs and raw synthetic completion bytes/hash retained.
Source/weights/image hashes, budgets, token usage, latency, selected validation
XML and approval accompany each run. All three allowed attempts are consumed;
unused elapsed time does not authorize a fourth. No interactive AI demo was started.

Evidence is local only: `.build-check/d76-core-window/<run>/`. No raw replies,
private approvals or credentials are committed. The test adapter forwards actual
model responses unchanged; no synthetic answers, external fallback or retries.
It enforces model:8080 only, closed schemas, temperature0, nonstreaming, scope
96/192/256 and argument768 caps, at most28generations, bounded64KiB captures.

## Established causes and fixes

1. First reply finished normally with15completion/443prompt tokens. Core logged
   scope/cross_domain_refinement. It was not a timeout or truncated reply.
2. Constrain cold-start scope mode to replace and argument mode to selection.
   A new domain gets clean memory; argument instructions contain common rules
   plus only the selected domain. Existing validators remain authoritative.
3. Second failure: correct open position but illegal search filters. Generate
   closed operation-specific arguments: no open filters/remove, valid displayed
   ordinal range, null search position, allowed domain fields/statuses, pinned
   kind/entity/mode. No-reference opening clarifies without a second call.
4. Third failure: model chose student-fees/search/refine. Our cross-domain reset
   discarded a compatible student reference and broadened the read. Current
   repair supplies typed current-reference position/permitted views (no old name,
   filter values or record labels). Compatible-view search/refine now clarifies
   without arguments instead of widening. It does not manufacture a golden-case
   open. This last repair is not yet live-qualified.

370 selected Linux model-free checks PASS at current45d7a5a source; previous
stages363/366 passed their exact-source runs. No skipped/failed selected checks;
one existing deprecation warning. Private XML/logs remain in d75-diagnostics and
source-specific d76-validation copies; old source-bound361PASS was reused only
for unchanged d144e0a, not falsely relabeled as a new run.

Actual latest CPU latencies:38.599s first teacher list;17.321s correction;19.309s
specialty;17.879s opening;26.614s student;25.449s batch;30.482s outstanding fees;
18.828s failed follow-up. These are exposed routing diagnostics, not end-to-end
business latency or throughput qualification. CPU4/GPU0,8192actual context, one
slot. Local QuadroT1000 has4GiBVRAM; read-only inventory is not a4BGPU acceptance.

## RAM and environment

Before Docker about7.1GiB available. Initial pre-load guard stopped without a
model attempt when free RAM was under5GiB. Optional named apps were absent;
Windows denied attempts to stop Dell background diagnostics. No privilege bypass
or security/dev-service stop. Renderer working sets were trimmed (no documents
closed), guest filesystem cache released/compacted, then the unchanged guard
passed. During generation observed free RAM stayed above the768MiB safety limit.
Only new labelled containers/internal networks were used and removed. Models,
existing containers/volumes, SQL, keys, QA evidence and application work preserved.

## Continuation and production limits

Mini45d7a5a80378dd0b49f9a577dfe65b4f9c65aaa1 contains the unqualified reference
repair. D77 is a separate prepared continuation, same weights/cases/limits with
new source/approval/validation bindings and distinct state. Approval requested;
not yet received at this report entry. Never reuse the consumed D76 approval.

Keep BUG-AI-0001 OPEN. Latest real result7/8 is not14/14 or held-out intelligence,
host/SQL/browser/role/device qualification, all CRUD/compound execution or90%AI
outcomes. Keep Mini/Core and model artifacts;4Bdefault suitability is undecided,
Mini/Plus/Pro unqualified and Ultra deferred. Sol High remains appropriate.

Previously published Academy31415b0 ordinary exact CI passed; paired source access
remains blocked. Original Mini full-suite archive/evidence failures, original
enterprise/device/staging/release gates remain. New publication requires its own
exact CI; do not replace it with older2,799API/39SQL results.
