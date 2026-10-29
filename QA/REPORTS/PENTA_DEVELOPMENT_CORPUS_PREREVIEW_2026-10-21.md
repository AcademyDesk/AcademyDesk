# PENTA development curriculum — non-authoritative pre-review

21 October 2026. Academy worktree `D:\AcademyDesk-codex-p0`, branch
`codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.
Mini remains on `codex/academy-language-contract`, HEAD
`2f8806967709a1a7ec29e5176e7c31968adbf6aa`.

## Purpose and boundary

Move the existing E4 model-improvement work forward rather than rerun accepted UI
checks or unchanged failed inference. This is a developer inspection of **42 train
candidates**, NOT the independent semantic/privacy approval required by E62. No
attestations, reviewer identities, author designation, eligibility or approvals created.

Filtered the frozen corpus to `split=train` before inspecting message/label contents.
The JSON container and hashes include all56 records, but the14 validation messages/
expected outputs were not displayed or reviewed for development changes. No acceptance
or exposed-regression prompts/answers were inspected or used for tuning. Exclusion
metadata was integrity-checked without reading its case contents into this review.

Original56 examples remain pending/zero training-eligible. No dataset, generator,
model prompt, evaluator, scoring rule, provider, reducer, weight or runtime changed.
This report and its follow-up packet are not SFT data or evidence that Mini improved.

## Frozen evidence checked

Input directory:
`D:/PENTA-Models/operations/e5r61-readiness/review-fa5ad2c1d0e7/`.

- Receipt SHA256 `d64f57ac22612ab68433d1cbd17b9329772e28aa92e1e50115ed125384b7f311`
  matched the E62 pinned identity.
- All three receipt-listed byte hashes matched: readiness, review corpus and exclusions.
- Corpus file SHA256
  `28cb5347dffb2b00cb6527f69f78efc306bd852a00d3013f1e7dc21382d7856e`.
- Canonical full-corpus identity retained from E61/E62:
  `1e9fc189802c9042a07415eb31cb6ae85ed58b1428b0cf355af1a329f76622a5`.
  This is not the same calculation as the file byte hash; canonical validation was
  not falsely rerun here.
- The existing unsigned E62 packet has no new approved return in Mini's handoff.
  No independently designated review receipts were supplied by this task.

## Observed training coverage

42 candidates =14 lessons across shelf, rental and practice synthetic catalogues.
All42 declare English/Executor. Three catalogue tool keys, all familyAI-06, write-draft
tools with required preparation/approval; none is an enabled Academy operation.
Declared field kinds: text, integer, boolean, reference, decimal and rows; no date.
All42 train review labels remain pending; training eligibility is false.

Four exact-message groups each have three copies: pause, resume, cancel and status.
That is12 records sharing four literal prompts, not12 distinct phrasings. Same-split
repetition is not itself split leakage or an incorrect label; breadth must not be
overstated. There are34 distinct literal training messages, not42.

## Developer interpretation of the14 lesson groups

Each row covers the corresponding `DEV61-shelf-*`, `DEV61-rental-*`, and
`DEV61-practice-*` records. This is inspection, not an approval/pass score.

| Suffix | Observed interpretation | Important limit |
| --- | --- | --- |
| preserve | Refine only the named label; reducer retains unrelated fields | Explicit field key and preservation instruction, not implicit conversational correction |
| clear | Remove only the named clearable annotation | No required/non-clearable-field removal contrast |
| reset | Explicit fresh draft replaces old optional values | No ambiguous restart-versus-correction contrast |
| partial | First draft supplies the sole required label and omits optional fields | All3 have **zero missing required fields**; not incomplete-required-field clarification evidence |
| missing-reference | Clarify the requested unavailable reference | User explicitly says the choice is absent and asks for clarification |
| ambiguous-reference | Clarify the requested reference without choosing first | User explicitly asks for clarification; catalogue choices have distinct labels |
| protected | Unsupported permission/audit-bypass request, no draft action | All3 start without a draft; no embedded hostile evidence or retained-draft contrast |
| zero-false | Preserve typed zero/false rather than treating them as absent | One numeric/boolean combination per family |
| decimal | Preserve literal decimal text18.60 | Not authoritative money calculation or financial-rule training |
| rows | Refine only the rows field and retain unrelated values | All3 have **no prior rows field**; adding rows is shown, actual existing-list replacement is not |
| pause | Request draft pause without submit/cancel | Same literal prompt across3 families |
| resume | Request resume from paused draft without submit | Same literal prompt across3 families |
| cancel | Request unfinished-draft cancellation, not business-record deletion | Same literal prompt across3 families |
| status | Read draft status, no review invalidation/action | Same literal prompt across3 families |

No obvious contradiction between these explicit train messages and their supplied
labels was found during this developer inspection. That does **not** authenticate
privacy/provenance, independently approve labels, establish generalization or qualify
the model. In particular, a reducer-consistent label is not proof of natural-language
understanding; the previous631/672 structural tests remain prior evidence, not a new
semantic score for these42 examples.

## Six concrete follow-up gaps

1. **G1 — true row replacement:** add development-only cases with nonempty existing
   rows, explicit complete replacement and unrelated-field preservation. Keep append,
   replace and single-cell correction distinguishable; do not silently reinterpret them.
2. **G2 — actual partial collection:** examples missing a required field, retaining
   known optional data, then supplying just that missing field on a later host snapshot.
   Keep draft preparation separate from approval/execution. Do not relabel the existing
   optional-only examples as wrong; they correctly express their narrower case.
3. **G3 — grounded reference success and ambiguity:** positive unambiguous listed-ID
   selection (currently **zero reference set values**) plus genuinely indistinguishable
   labels/missing choices that require clarification, without the user spelling out
   the intended classifier label. Never invent an ID or choose the first match.
4. **G4 — natural phrasing and conversational contrast:** increase independent wording,
   colloquial corrections/negation, retained-context references and approved language
   coverage. All42 current messages are English; they do not establish multilingual
   academy quality. Obtain qualified language review before adding non-English labels.
5. **G5 — dates and scope limits:** independently authored date/missing-timezone and
   conflicting-context examples under supported contracts. Current six field kinds
   do not include dates. Model labels cannot grant authority or replace host validation.
6. **G6 — broader safety/workflow coverage:** distinguish draft-only lifecycle from
   destructive domain operations, unsafe embedded content, approval bypass and unknown
   outcomes with retained data. This small Executor draft curriculum does not establish
   Pulse/Navigator/Twin/Autopilot or all-academy operational competence.

These are development coverage requests, not new benchmark answers. No new examples
have been minted in this report. Do not copy exposed examples or the untouched14
validation outputs to satisfy them. New data requires its own full validation,
lineage/exclusion checks, immutable receipt and independent review.

## Next critical-path action

Use [the concrete Mini follow-up packet](../HANDOFFS/PENTA_MINI_CORPUS_GAPS_2026-10-21.md).
An authorized offline owner must designate actual author/reviewer identities; this
task cannot derive them from `synthetic-author`, a model answer or self-declaration.
Even accepted-for-followup E62 receipts deliberately do not enable training.

After reviewed development data, prepare the scoped artifact/trainer/resource
experiment for approval. Two original3B shards are still missing in the retained
readiness assessment; no verified GPU trainer or measured safe training fit exists.
No download/install/training/Azure action is authorized by this pre-review.

Latest actual full-plan result remains **fresh6/12, exposed7/12, NOT ACCEPTED**.
Host model/domain, durable history, wider operations, Manual/device/enterprise/release
gates and BUG-DATA-0001 stay OPEN. All11 broad packages retain their outstanding gates.
No application tests/builds/model inference rerun: application source was untouched.
Next Sol High for consequential model/data/resource decisions; Medium for settled
documentation/UI. No project switch is required with existing direct local access.
