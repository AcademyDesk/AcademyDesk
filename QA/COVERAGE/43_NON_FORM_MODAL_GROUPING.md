# Batch 43 — gap-only non-form modal grouping, first slice

2026-09-30. Static source/evidence routing only; browser, keyboard, Android/iOS and visual checks NOT RUN. This does not reopen native-form, controller or field reviews.

## Reconciled source groups

The UI inventory has 843 declarations, but they are not 843 independent state machines. A targeted check of modal source found **33 `StandardDetailModal` call sites across nine pages**, **six bespoke register/health modal families in `platform/control/page.tsx`**, and **two modal families in `platform/page.tsx`** (overview detail and onboarding). This is 41 source call sites/families, not 41 proven behavioral gaps.

| Group and source | Existing behavioral evidence to retain | Gap-only state disposition |
| --- | --- | --- |
| Shared detail shell, `apps/web/src/components/design-system/interactive.tsx:14–15`; 33 calls in Batches, Dashboard, Reports, Sales & Marketing, Student Overview/Profile, Teacher Overview/Profile and Teachers | Batches [11](11_TEACHER_ADMINISTRATIVE_PROFILE.md), [16](16_SALES_CAMPAIGNS_TRIALS.md), [19](19_BATCHES_ENROLLMENT_PROMOTIONS.md), [29](29_IMPORTS_DASHBOARD_INTELLIGENCE.md) and existing `UI-A11Y-001`/`BUG-UI-0002` already address identity, row membership, empty/loading/partial-data distinctions, focus and close. Existing page visual IDs cover each route | **Grouped, not newly accepted.** Shared shell has labeled dialog and close/backdrop handlers in source, but no Escape/focus trap/return logic; keep the existing issue, do not register 33 duplicates. Content loading/empty/error must follow each owning page's existing data-source scenario, not a fabricated modal API |
| Platform owner six register/health families, `apps/web/src/app/platform/control/page.tsx:688,713,752,782,809,858` | [Batch 13](13_PLATFORM_ADMINISTRATION.md) covers admin/announcement/health and load-failure distinctions; [Batch 14](14_PLATFORM_BILLING_SUPPORT.md) covers invoice/support membership and modal accessibility; [Batch 12](12_TENANT_LIFECYCLE.md) covers tenant scope. `VISUAL-PAGE-052` is the route-level visual scenario | **Grouped, not newly accepted.** Variant filters have source-specific empty messages. Retain the existing high-priority support membership finding `BUG-FUNC-0011`; no new issue from grouping. A page load failure versus a truly empty modal list and keyboard focus/scroll remain execution expectations, not static PASS |
| Platform Overview detail and onboarding dialogs, `apps/web/src/app/platform/page.tsx:346–358` | [Batches 12](12_TENANT_LIFECYCLE.md) and [14](14_PLATFORM_BILLING_SUPPORT.md) cover onboarding/overview save and data boundaries; `VISUAL-PAGE-053` covers route-level visual states | **Grouped.** Overview is a read-only detail branch; onboarding wraps an already inventoried native form, so its fields must not be recounted. Modal open/cancel/busy-close/saved/error and focus/scroll are the applicable shell states. A truly empty academy list versus unavailable overview data still requires explicit browser assertion; do not report either as observed PASS |

## What this slice proves and does not prove

The first non-form modal slice is now classified by reusable shell and owning data source, using the existing Astra High coverage. No source change or new distinct product defect was established. `BUG-UI-0002` already records the shared keyboard-focus issue; `UI-A11Y-001` is NOT RUN. The grouping does not certify all non-form editors, selectors, action menus or other conditional page branches. Those remain the bounded G01 documentation scope, without repeating native form/field/controller matrices.

No application changes, data operations, issue-status updates, scenario execution, commit, push or deployment occurred.
