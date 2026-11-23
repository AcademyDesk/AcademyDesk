# D62 — whole-product conversational task foundation

23 November 2026. Desktop-only owner. Base source:
`29f908a494f2c0919cd1b5a29175c72735eee895` plus the explicitly listed new source
below. Owner approved implementation after correcting teacher-only scope.

## Implemented

1. Internal Manual capability index derived from actual controller/action/HTTP
   declarations, with role surface, existing subscription-module metadata,
   route overrides/token expansion and honest draft-integration classification.
   This is a source inventory, not automatic registration or authorization.
2. Nine typed task descriptors: student onboarding, teacher creation, batch
   creation, enrollment create/update/transfer, class create/reschedule and
   payment recording. Reuses existing onboarding/teacher descriptors and shared
   onboarding rules. Batch creation is explicitly draft-only; protected adapters
   are historical implementation references, not qualified real-model dispatch.
3. Closed `academy-task-plan.v1` proposals, bounded to twelve dependent tasks.
   Partial fields, conditional requirements, explicit dependencies and typed
   result references are retained as a deterministic host draft.
4. Multi-turn field edits preserve other supplied details, reject stale plan
   revisions/contradictions and advance affected downstream draft revisions.
   This is in-memory draft reduction, not yet a durable SQL compare-and-swap.
5. Dependency materialization accepts only internally supplied host observations
   matching scope, task/tool/revision, declared output identity and receipt
   reference. It rejects wrong entity types, incomplete source drafts, foreign/
   stale observations, duplicate observations and already completed target tasks.
   **Reference format is not receipt authenticity.** The eventual protected host
   adapter must first verify committed operation/audit rows and current authority;
   model/browser JSON must never construct a trusted host observation.

The compound regression retains student + batch + enrollment, requests only
missing minor guardian details and the online meeting link, retains corrections,
and materializes enrollment with exact host-issued student/batch IDs through the
existing enrollment parser. No student/batch/enrollment was actually created by
these pure tests. Missing data, typed drafts and a payload are not domain success.

## Validation

- API and test assembly build: PASS; final build has no emitted warnings.
- Final isolated affected regression: **896 passed, zero failed, zero skipped**,
  across 39 test classes in eight fresh processes. Includes 56 new cases, all
  837 existing `Penta`-matching cases and three additional existing methods in
  selected onboarding service classes. Exact class membership and each TRX's
  executed/passed/failed/aborted/not-executed counters were checked.
- `git diff --check`: PASS. No package/configuration/migration changes.
- Existing demo login and API health: HTTP200 after validation. Existing SQL,
  Core and cached inference containers remained running; none was stopped,
  restarted, replaced or pruned in this slice. No inference request was made.
- New SQL/Identity/browser/model-inclusive acceptance: **NOT RUN**. These classes
  are not exposed by HTTP/DI or wired to the current Mini composer.

Private evidence: `.build-check/d62-task-foundation/`, including TRX, per-cohort
logs, discovery list, class groups and `isolated-results.json`. Do not publish
raw private evidence or credentials.

Retained failures, not hidden:

- First focused run: 38 passed/1 failed. Future DOB incorrectly triggered minor
  guardian questions. Corrected to request DOB correction before deriving age;
  final affected regression passes this case.
- One-process broad run: 876 passed/2 failed, then test-host OOM under the bounded
  managed-heap cap; aborted and NOT accepted. Reran the full intended selection
  in fresh bounded processes, without changing assertions or application rules.
- First cohort wrapper stopped despite 111 passing tests because it compared an
  all-class run against the 108 method-filter discovery count. Three additional
  existing methods explain the difference. Kept the original result; validated
  exact class coverage and zero failure/skip/abort counters, then resumed cohorts.

## Tested source SHA-256

| Path | SHA-256 |
| --- | --- |
| apps/api/Intelligence/Penta/PentaTaskCatalog.cs | 3bbec3a17b5732c0c1b00c6e3a85fc3aad1786940a28d0f3ddcf0add20504024 |
| apps/api/Intelligence/Penta/PentaTaskPlan.cs | 9cead2edbe731270d47d0f517aec7348229d35c9df50b548742972a071b78257 |
| apps/api/Intelligence/Penta/PentaManualCapabilityIndex.cs | afe6ff810a6062a572452cc86cb85824b3b8e370e9eaed094424b9b13a0cadf6 |
| tests/AcademyDesk.Api.Tests/PentaTaskPlanningTests.cs | ed2790bc38001a00e67b33b0bd231f13cf6f4a52a7c7952ff06602ed4c5fa599 |
| tests/AcademyDesk.Api.Tests/PentaManualCapabilityIndexTests.cs | f707167fdccd50c52c4bb773d6dbd82de8bfb4e3fe1d3f762ac914d382e603e8 |

## Next and limits

- [x] Whole-product operating contract and source-derived Manual operation index.
- [x] First nine typed task descriptors and dependent draft reducer; focused
  compound/correction/negative regressions, with affected legacy checks preserved.
- [ ] Current-Identity/tenant/record-authorized runtime catalog and actual Mini
  planning against that catalog; unsupported requests must not fall into learners.
- [ ] Durable multi-task dialogue/history, partial clarification, explicit task
  replacement/cancellation, in-flight/unknown operation protection and GET recovery.
- [ ] Protected batch creation binding and compound coordinator, using supported
  domain/Identity/audit transaction boundaries. No transaction over inference or
  human clarification; accurately recover partial effects where atomicity ends.
- [ ] Bind remaining read/write domains and role-specific journeys; grounded
  natural answer composition, fresh real-model/SQL/browser/device acceptance.

BUG-AI-0001 remains OPEN; live Mini still has the legacy two learner tools. The
earlier untracked two-domain AcademyRead and separate Mini draft remain preserved
and inactive. No production AI tier, 90% target, six critical closure chains,
staging/release gate or Azure deployment is accepted by this foundation result.
Continue with Sol High; no separate Cloud/design-project handoff is needed.
