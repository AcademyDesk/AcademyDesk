# Phase 1 acceptance-gap review — batch 32, re-audited

Continuation: [Batch 33](../COVERAGE/33_TEACHER_COMPENSATION_SEMANTICS.md) completes the compensation specification slice below (not runtime verification). Remaining work is tracked in [the semantic checklist](../COVERAGE/SEMANTIC_CHECKLIST.md); next is compliance. Counts and next-work instructions below describe this Batch 32 snapshot.

2026-09-29. Supersedes the previous Batch 32 recommendation. Documentation and static source review only; no application repair, runtime test, data operation or deployment.

## Decision

**Phase 1 remains NOT CLOSED, but do not restart completed semantic reviews.** The corrected declaration census is 82/82 native forms (481 lexical fields), 168/168 non-form declarations, 649/649 total controls and 70/70 controller classes. [Batch 31](../COVERAGE/31_SHARED_CONTROL_MAPPINGS.md) explains the missing 45 manifest entries and validator blind spot found in this re-audit. Distinct source associations are now mechanically checked; they do not prove that every behavior is specified.

The earlier recommendation to begin again with all finance/onboarding/private-upload flows was too broad. [Batch 01](../COVERAGE/01_ONBOARDING_PAYROLL.md) already has per-field mappings, exact persistence boundaries, conditional-state/N/A tables and effective permission expectations for teacher/student intake and payroll. [Batch 02](../COVERAGE/02_FINANCE_LIFECYCLE.md) already expands finance lifecycle fields and effective controller/role expectations. Preserve these on the unchanged baseline; revise only a demonstrated omission or source change.

## Demonstrated residuals, not a new whole-repository audit

| Gate / bounded scope | Evidence in existing specification and source | Required static completion |
| --- | --- | --- |
| G01/G02 — teacher compensation, TEACHER-FORM-002 | Batch 28 lists seven controls and broad zero/decimal/model-switch cases, but not seven explicit UI→payload→DTO→guard→persistence rows. `teacher-payments/page.tsx:29` uses truthiness before Number conversion: loaded numeric zero and newly entered string zero need distinct expectations. Line 50 conditionally renders Monthly/Hourly fields. TeacherCompensationController.Save accepts nonnegative rates, requires the selected model's primary rate and writes a JSON summary rather than separate rate columns. | Expand seven rows plus the mapped teacher selector. Specify required/optional/null/zero transformations, model transitions, stale selection, transport/parse failures, save/readback and persistence limits or their absence. Do not invent decimal-column constraints for JSON fields. Link existing form/API cases. |
| G01/G02 — compliance record/consent forms, COMPLIANCE-FORM-003/004 | Batch 27 names optional and length tests but does not enumerate each exact member/type/limit boundary. `compliance/page.tsx:73-81` instantiates two PersonPickers, each with its own state; the earlier shared-selection-state claim was wrong and is corrected. | Complete per-input/member constraint rows and per-form state expectations. Verify independent selections, per-instance reset and subject/payload mapping. Include non-form review/task/withdraw actions already described. |
| G01/G02 — teacher inline editing and assignment | Batch 31 maps eight declarations in `teachers/page.tsx:217-280`, not their full save semantics. Edit checks trimmed required names but sends original strings; assignment PUT replaces a batch payload, including retained/defaulted fields. | Join editors to existing teacher-update/batch-update scenarios. Record transformations, DTO/guard/persistence limits, open/cancel/save/failure/refresh states, selected teacher/batch consistency and unchanged-field retention. Reuse creation rules where identical. |
| G03 — later prose-only permission summaries | Batch 28 asks to verify role matrices and notes missing catalog mappings; Batch 27 similarly lists caller categories. Neither alone records expected allow/deny by action. AcademyAccessFilter has a platform-owner-flag bypass, tenant/module checks, admin path and catalog/grant path whose ordering matters. | Expand these named action groups first, reusing the earlier filter decision model. Distinguish anonymous, foreign tenant, owner/admin, delegated permission/grant and platform flag outcomes, plus action-local guards. Separate implemented behavior from desired policy. |
| G01–G03 — finite reconciliation of earlier evidence | Declaration equality does not check editor/modal states, constraint semantics or role outcomes. The examples above prove residual gaps; this re-audit does not certify them as the only gaps. | Maintain a finite checklist keyed to existing forms, editor/action groups and controller actions: complete with exact evidence section, incomplete with named missing detail, or N/A with reason. Reuse adequate existing sections; expand only missing rows. |

This is a prioritized evidence-backed queue, **not a promise of five remaining jobs**. No percentage or fixed number of turns is supported until the finite semantic checklist is reconciled. Shared hidden mirrors and unused helper declarations remain census entries, not extra user workflows.

## Verification and boundaries

- The revised helper rejects missing mappings, aliases targeting one declaration, stale anchors and ambiguous structural identities. Eight QA-tooling regression tests pass.
- QA consistency validation passes 25/25 checks, including distinct declaration completeness. This is tooling evidence, not application testing or semantic approval.
- Application baseline and the two existing student UI edits are unchanged. The register remains 106 OPEN issues: 5 P0 / 88 P1 / 13 P2; none is closed or newly runtime-confirmed.
- Retained application evidence remains eight InMemory tests, TypeScript success and lint failure; not rerun here. No browser, HTTP or SQL application tests were performed.
- Runtime enumeration, authorization testing, database assertions and visual approval belong to later phases, not retroactive Phase 1 closure requirements. Phase 1 requires static G01–G03 evidence reconciliation and explicit acceptance; closure would still not approve release.

## Next bounded work

Complete compensation first (TEACHER-FORM-002, its selector and TeacherCompensationController.Get/Save), using existing scenario IDs and the global authorization model. Update the finite checklist, then take only the next demonstrated gap. Do not restart onboarding/payroll or the detailed finance lifecycle matrices. Phase 2 remains planned, not started.
