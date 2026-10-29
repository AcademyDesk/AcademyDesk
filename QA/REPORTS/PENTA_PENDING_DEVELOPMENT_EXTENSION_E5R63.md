# E5-R63 — separate pending development-data proposal

21 October 2026. Academy `codex/penta-search` / `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`;
Mini `codex/academy-language-contract` / `2f8806967709a1a7ec29e5176e7c31968adbf6aa`.
Continues the existing [six-gap packet](../HANDOFFS/PENTA_MINI_CORPUS_GAPS_2026-10-21.md),
not a new model project, acceptance benchmark or application redesign.

## Delivered

- Added a separate offline development generator, exact-source proposal exporter
  and two test files in the existing Mini repository. No existing generator,
  evaluator, prompt, contract, reducer, provider, runtime, serving import or weight
  changed. No extra environment/dependency was installed.
- **20 new synthetic English train-candidates**, all pending semantic/privacy
  review and training-ineligible. New `DEV63-*` identities and synthetic
  `PrepareRehearsalInventoryDraft63` catalogue; no Academy connector/tool activation.
  The tool requires host preparation/approval and exists only in this offline data.
- G1: three nonempty existing-row contrasts: whole replacement, explicit append
  and count-only correction preserving row order/unrelated fields.
- G2: three required-name cases: initial collection actually missing required name,
  subsequent supplied-name snapshot retaining the prior note, and explicit question.
- G3: actual positive listed reference plus duplicate-label and absent-choice
  clarification, without choosing first or inventing an ID.
- G4: varied negation, note-only correction and field-only removal. English-only:
  **multilingual/language review remains OPEN**, not silently marked delivered.
- G5: explicit ISO-resolved date, ambiguous numeric date and conflicting target
  clarification. Existing contract supports dates, not timezone-bearing meeting
  scheduling; **time/timezone contract/workflow coverage remains OPEN**.
- G6: five retained-state cases: access/audit request unsupported, removal of an
  untrusted quoted note without following it, approval-bypass unsupported, uncertain
  outcome status without replay, and draft-only cancellation without saved deletion.
  These authored samples are not real-model prompt-injection acceptance.

The required-name pair is independently authored as two snapshots, not a live
conversation or proven persisted multi-turn completion. Full candidate decisions
are computed by the unchanged reducer; focused tests independently assert the
important authored deltas, retained values, questions and no-action results.
Reducer conformance is not independent semantic/privacy approval.

## Identity, exclusion and exposure boundaries

The exporter pins E61 receipt
`d64f57ac22612ab68433d1cbd17b9329772e28aa92e1e50115ed125384b7f311`
and all three listed original file byte hashes. It reads only receipt/exclusion JSON;
the original corpus/readiness files are hashed, not deserialized by this exporter.
Duplicate/malformed exclusion layers, changed source files and action arguments
reject. All four frozen exclusion layers pass existing `review_packet`/batch checks.
The old56 records/14-validation split and unsigned packet remain byte-unchanged.

During implementation, the **existing generator source was read, including its14
validation templates**. This is subsequent exposure, distinct from the earlier
42-train-only pre-review. Those14 remain validation/regression material, never
training data or independently unseen acceptance for this developer context.
No prompts, scoring or labels were tuned to them. Retained evaluator tests execute
their fixtures internally; their answers were not printed as tuning targets.
Fresh model acceptance must be independently authored/frozen later. This batch does
not establish absence of semantic paraphrase contamination; independent lineage
review remains necessary.

## Validation

- First development-data run:43 checks PASS.
- Export checks initially hit nine temporary-directory setup errors because the
  requested D: parent did not exist;49 checks passed. Initial JUnit is retained at
  `D:/PENTA-Models/operations/e5r63-checks/new.xml`; not relabeled a pass.
- Corrected run uses a fresh, non-existing GUID-named temp path under the explicit
  D: checks directory, preserving old temp/evidence paths: **58 new checks PASS**.
  `new-final.xml`. No product assertion weakened.
- **730 distinct related tests PASS /24.95s**, zero fail/error/skip: retained672
  plus58 new checks in one combined execution. Narrow43/58 runs overlap, not added.
  `D:/PENTA-Models/operations/e5r63-checks/combined.xml`.
- Existing Starlette/httpx deprecation warning remains (one category); no package
  replacement/install to hide it. No model inference, GPU/trainer or training run.
- Tests cover strict pending labels, minimal/retained deltas, actual required gap,
  listed-ID/ambiguity, dates, safety/unknown-state outcomes, all exposure-index
  layers, failed empty review set, malformed labels/eligibility, unchanged-source
  hashing, no old-validation JSON deserialization, invalid action rejection and
  actual new review-only export without source changes.

## New artifact (D: only)

`D:/PENTA-Models/operations/e5r63-development/pending-cd17b6109d42/`
contains `development-proposal.json` and its `receipt.json`.

- Proposal file SHA256:
  `34c5c5fab8ec59f796b32e5b71f90e47d0230b1320cdb269e2f6424b80cf7130`.
- Canonical new corpus SHA256:
  `a4ac5ad9f88dcd358065f2c96bbe297fefbe214c5049d79e28b637cc2fe3ff4a`.
- Frozen exclusion fingerprint:
  `2c26f79777dd055ae97518d53d6436fedfeafddee5a3b03c726894ce246cd2b6`.
-20 pending /zero reviewed /zero training-eligible /`may_train=false`,
  zero attestations, training jobs, inference calls, downloads, installs/activation.
- Original56 pending plus new20 pending =76 candidates across two separate packets,
  not76 approved examples, a merged dataset or training export.

Tested source SHA256 in `D:/PENTA AI Models`:

| Path | SHA256 |
| --- | --- |
| `operations/tools/curriculum_extension_e5r63.py` | `F1343C5649EC06F9C230BD32AE722C4D8FA995C69BAA157A34A368231FAED2EE` |
| `operations/tools/prepare_extension_e5r63.py` | `6C620071BADB967C8D76D4766F62326BE8501294A60B58D12D57CA2DA1001F45` |
| `tests/test_curriculum_extension_e5r63.py` | `074A198EB66E01DFDFC3D43E620944A9FCBA9B5F559237E4F45018B6A9DEB736` |
| `tests/test_prepare_extension_e5r63.py` | `15266435FE98439FB71FD793FA229442D955E12E714561B1DDEF4255174387CB` |

## Next gate — not self-authorized

Actual independent semantic/privacy/provenance review and trusted author/reviewer
designation remain missing. No review identity or attestation was invented. E62
accepted-for-followup receipts still do not authorize training or set eligibility.
After actual review, prepare one scoped checkpoint/trainer/resource experiment
for approval; missing original3B shards, no verified trainer and unmeasured safe
training fit remain in the retained readiness record. No acquisition/training/
paid-resource action may be inferred from this successful proposal export.

Latest actual full-plan result remains **fresh6/12 /exposed7/12 /NOT ACCEPTED**.
No trained/qualified AI or general ChatGPT-like quality demonstrated. Wider
language/workflow/host/domain/approval/history/security/device/enterprise/release
gates remain OPEN, including BUG-DATA-0001 and all11 broad delivery packages.

Academy application source unchanged; prior225 focused/461 SQL/API/33 browser/
lint0/0/87-route evidence retained, not rerun. Both branch HEADs/main/staging
unchanged; legitimate dirty work retained. Docker read-only inspection:29containers,
41volumes, only two PENTA services running. No commit/push/merge/main edit,
production data access, Azure, download/install/training/inference/activation,
driver/global-setting change, memory-guard relaxation, restart or prune.

Next route Sol High for consequential independent-review/resource/model gate;
Medium for settled UI/docs. Direct local access needs no project switch. Human
authority is required for genuine review designation and subsequent experiment
approval; repeated “continue” is not a replacement for those inputs.
