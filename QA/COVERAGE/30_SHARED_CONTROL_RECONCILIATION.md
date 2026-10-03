# Batch 30 — Shared-control reconciliation

2026-09-29. Static source bookkeeping review; runtime NOT RUN. This batch makes no application changes and does not claim browser coverage.

## Reconciliation result

The prior planning estimate of 38 residual shared controls was not a mechanically verified count. Using the validator's exact matching logic (including source-property and optional line semantics), the field inventory entries outside native `<form>` declarations contain **46 mechanically unregistered declarations**. Some are already described in page-flow scenarios from earlier batches, but they were not individually associated with the machine-readable standalone-control manifest. Therefore the 38 estimate must not be used to close G02 or to infer 611/649 is complete individual declaration coverage.

The raw result is a tracker-reconciliation finding, not a product defect: it identifies incomplete traceability in this Phase 1 source specification. It creates no new application issue and does not alter the open issue register.

## Shared primitives reviewed

| Component | Source declarations / behavior to verify later | Existing risk linkage |
| --- | --- | --- |
| `standard-date-field.tsx` | hidden named value, month/year selectors, calendar/day actions and client-local date conversion | BUG-UI-0002, BUG-UI-0006, BUG-UI-0007 |
| `standard-select-field.tsx` | hidden named value, fixed portal menu, trigger-to-menu geometry and click-to-close | BUG-UI-0001, BUG-UI-0002 |
| `standard-time-field.tsx` | hidden named value, portal time selector, focus/keyboard and viewport behavior | BUG-UI-0002 |
| `enterprise-shell.tsx` | global search, navigation, notification controls and profile image file input | BUG-FUNC-0003 and existing responsive/navigation scenarios |

At desktop and 320/375/390/430, 768, 1280/1440 widths and 200% zoom, later runtime testing must verify trigger anchoring, clipping, stacking, focus transfer/return, Escape and keyboard selection, selected-value readback, scroll lock, file rejection and success/error notices. Source review alone cannot establish any of those outcomes.

## Remaining mapping work

The largest raw unmatched groups are `teachers/page.tsx` (8), `portal/page.tsx` (6), `teacher/page.tsx` (6), `activity/page.tsx` (4), `compliance/page.tsx` (4), shared date field primitives (3), and smaller one/two-control groups across finance, platform, operations and shared shell components. They must be either individually linked to a Test ID and source property/line, or consolidated under a reusable component mapping that demonstrates each call site is covered. Do not count a described page flow as a completed source declaration without that link.

## Outcome

No new source issue is registered. The issue register remains **106 OPEN: 5 P0 / 88 P1 / 13 P2** (105 static source findings plus the retained executed lint defect). Native forms remain **82/82** and controller mappings **70/70**. The manifest counter remains **611/649**, but is not a completion claim while the raw outside-form reconciliation reports 46 unregistered declarations.

Historical snapshot above is superseded by the [Batch 31 re-audit](31_SHARED_CONTROL_MAPPINGS.md): all 168 non-form declarations, including the anonymous CSV input, are now individually registered and distinctly resolved. Correct totals are 481 form fields + 168 non-form declarations = 649; the former 611 aggregate was reporting drift.

Current next scope is the compensation semantic slice identified in [Batch 32](../REPORTS/PHASE_1_GAP_REVIEW_BATCH_32.md). No declaration exception remains. G01–G03 remain open; Phase 1 is not closed and Phase 2 has not started.
