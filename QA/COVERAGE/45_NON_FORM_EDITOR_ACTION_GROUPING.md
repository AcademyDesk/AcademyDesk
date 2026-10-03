# Batch 45 — gap-only inline-editor and action-state grouping

2026-09-30. Static evidence consolidation on the pinned baseline; no application/browser/HTTP/SQL test, product fix or deployment. This pass preserves the Astra High module reviews and does not re-adjudicate individual form or controller rows.

## Inline editor state groups

Targeted source search for `editingId` / `editingDocuments` finds seven current page implementations. Every one already has an owning source review. The shared applicable states are row selection/open, draft initialization, Cancel without write, Save pending, rejected/ambiguous save, refresh/readback and focus/notice; the differences below remain with the existing specifications rather than a generic assertion.

| Page/editor | Existing source-backed specification | Distinct branch that must remain attached to its owning scenario |
| --- | --- | --- |
| `branches/page.tsx` row editor | [Batch 15](15_ACADEMY_CONFIGURATION_BRANCHES.md), `VISUAL-PAGE-015` | Edit versus Deactivate/Reactivate; full-row stale overwrite, Cancel during save and row switch |
| `courses/page.tsx` row editor | [Batch 17](17_COURSES_CURRICULUM.md) | Edit versus active toggle; omitted extended fields can be reset (existing BUG-DATA-0028); Cancel preserves persisted row |
| `batches/page.tsx` row editor | [Batch 19](19_BATCHES_ENROLLMENT_PROMOTIONS.md) | Edit/toggle replacement payload can reset extended scheduling fields (existing BUG-DATA-0031); Cancel/no-write and notice behavior already specified |
| `guardians/page.tsx` row editor | [Batch 07](07_GUARDIAN_ADMINISTRATION.md) | Edit/toggle touches linked Identity accounts after domain save; partial failure and cleared success notice are already covered by existing issues |
| `fee-plans/page.tsx` `FeePlanEditor` | [Batch 08](08_FEE_PLANS_REMINDERS.md) | Create/edit/deactivate differ: success is set after reload, but failed reload can close the editor or mislabel a committed save; Cancel/row-switch race already specified |
| `teachers/page.tsx` row editor and assignment | [Batch 35](35_TEACHER_MANAGEMENT_SEMANTICS.md) | Eight non-form controls have individual UI→request→DTO→persistence and conditional-state traces; no native form or modal is invented |
| `finance-governance/page.tsx` document setup | [Batch 06](06_FINANCE_GOVERNANCE.md) and [Batch 31](31_SHARED_CONTROL_MAPPINGS.md) | `editingDocuments` only shows/hides an inventoried native form; the preview selector and hidden theme mirrors are not extra editor fields. Existing save/close/stale-state findings remain in force |

The source search is a bounded finder, not proof that no other custom state variable can open an editor. It establishes that **these seven named editor families are not uncovered Astra work**. Do not create seven new verdict batches or duplicate their registered defects.

## Non-form action families

The UI inventory has 292 button declarations, 205 with explicit `onClick`. Those are source call sites, not 205 unreviewed workflows. Route and action declarations already have permanent scenarios; the applicable non-form branching is grouped as follows:

| Behavior | Existing evidence and state expectation |
| --- | --- |
| Row mutation and status toggle | The seven editor reviews above specify target-row identity, pending/disabled behavior, rejection/no-write or partial-write, refreshed persisted state and durable notice. Other batch/course/guardian/teacher mutations remain in their module reviews; this grouping does not overrule their distinct contracts |
| Approve/reject/review/withdraw and confirmation | [Batch 19](19_BATCHES_ENROLLMENT_PROMOTIONS.md) specifies promotion races and Cancel/reason behavior; [Batch 27](27_DOCUMENTS_COMPLIANCE_AUDIT.md) and [Batch 34](34_COMPLIANCE_SEMANTICS.md) specify compliance review/withdraw; [Batch 06](06_FINANCE_GOVERNANCE.md) specifies finance decisions. Keep positive and negative outcomes separate; do not equate a clicked button with persisted success |
| Queue/send/import/download/print | [Batch 08](08_FEE_PLANS_REMINDERS.md) specifies reminder queue and delivery-label limits; [Batch 24](24_MESSAGES_TEMPLATES_CONSENT.md) communication states; [Batch 29](29_IMPORTS_DASHBOARD_INTELLIGENCE.md) import/export outcomes; [Batch 40](40_CERTIFICATE_FORM_SEMANTICS.md) preview/print states. These are distinct endpoint/file or client-only outcomes, not one generic Save case |
| Navigation, filter, expand/collapse and details | [Batch 31](31_SHARED_CONTROL_MAPPINGS.md), [Batch 43](43_NON_FORM_MODAL_GROUPING.md), [Batch 44](44_NON_FORM_SELECTOR_DISCLOSURE_GROUPING.md) and `UI-SCROLL-001` already cover context, active label, value readback, close/cancel and scrolling. No write DTO/EF assertion applies to a pure filter/navigation action (N/A) |

The two `partialInteractions` retained in `progress.json` are fee-reminder queue and submission review. Their recorded exclusions are runtime delivery/browser proof and a native-prompt inventory limitation; [Batch 08](08_FEE_PLANS_REMINDERS.md) and [Batch 25](25_ASSIGNMENTS_SUBMISSIONS_LESSONS.md) already specify their source states. They are not evidence that Astra omitted those workflows.

## Handoff boundary

No new specifically demonstrated static G01/G02/G03 omission was established by this bounded grouping. This is **not** a Phase 1 PASS: the evidence locator and grouped source reviews still need a formal acceptance judgment against the original requirement, and runtime/browser/SQL/visual proof remains NOT RUN. Astra High can now make that judgment using Batches 01–45, recording any *specific* residual if acceptance is withheld. Do not start another 511-row Sol re-review or relabel unexecuted tests PASS.
