# Visual, screen-state and overlay strategy

Every route has a proposed `VISUAL-PAGE-*` case in the master matrix. Authoritative images: **0**. All candidates are **UNREVIEWED**. Historical screenshots show known problems; they are defect evidence, never automatic goldens.

| Source / consumers | Required visual behavior | Special states |
| --- | --- | --- |
| `standard-select-field.tsx`; student-fees, teacher-payments, student-profile, teacher-profile | Trigger and portal menu share horizontal anchor; menu fits available height; selected value updates; selectable rows remain above content | 0/1/few/many options; very long labels; inactive labels; keyboard and outside click |
| `standard-date-field.tsx`; onboarding and scheduling forms | Label/field distinction; month AND year selection; leap day; no clipped inline popover | Empty/valid/invalid date; required versus optional clear; year change |
| `standard-time-field.tsx` and native time inputs in teacher pages | Legible hour/minute/AM-PM; consistent field sizing; local-time/UTC conversion correct | 00:00/12:00/23:59; portrait/landscape; keyboard |
| `design-system/interactive.tsx` StandardDetailModal | Focus stays inside; Escape/outside/close button works; focus returns; content scroll accessible | Long content, empty table, modal + select; nested scroll |
| `enterprise-shell.tsx`, `workspace-nav.tsx`, `workspace-frame.tsx` | Main mobile menu collapses after navigation; current label matches route; section-to-section navigation works without Overview detour | Direct deep link, browser back/forward, query tabs, session expiry |
| `platform/control/page.tsx` | Single tenant intake has distinct field surfaces; long read-only details labelled; modal intake and success preserved | Missing optional values, long addresses; forbidden/loading states |
| `batch-setup/page.tsx`, `makeup/page.tsx`, `calendar/page.tsx` | Action buttons wrap; clock usable; agenda collapse/expand reachable | Short viewport, empty list, many sessions, long batch names |
| `/teacher` and `/portal` | Tabs/drawers/cards, assignments/uploads, schedules and bills remain readable | Student/guardian variants, no linked record, recording permissions denied |

## UI-DROPDOWN-001 / UI-SCROLL-001 protocol

For every shared overlay consumer listed in `INVENTORY/UI_COMPONENTS.md`, open at page scroll 0%, 25%, 50%, 75%, near bottom and bottom. Independently place the trigger near viewport top/middle/bottom; repeat inside any scrollable modal/container. For a short page use explicit synthetic tall fixtures; do not claim a non-scrollable page exercised bottom scrolling.

Record `{viewport, devicePixelRatio, zoom, visualViewport, scrollX, scrollY, containerScrollTop, triggerRect, overlayRect, placement, elementFromPoint}` and screenshot before/after scrolling. For fixed portals, rectangles use viewport coordinates; for inline absolute popovers, account for containing-block geometry. Assert the menu is adjacent below the trigger when it fits; adjacent above otherwise; never detached at page top. Tolerance may allow pixel rounding but must be documented. Max-height is not actual rendered height. Verify menu does not escape horizontal edges, overlay hit tests reach its options, no ancestor clips it, and scroll/resize updates or intentionally closes it. Test nested scrolling and stale overlays after navigation. Include touch selection and outside click.

Apply equivalent geometry checks to date/time popovers, mobile menus, tooltips where implemented and dialogs. Native browser select/time popups are OS surfaces: DOM screenshots do not certify them. No dedicated autocomplete or bottom-sheet library was found; if a dynamic component acts as one, classify it during browser inspection.

## UI data/state fixtures

Use 0, 1, 5, 100 and a measured large dataset; 1-character and 120-character person names where the server permits; maximum field lengths from DATABASE.md; long unbroken labels; Unicode; one/many related subjects; missing optionals; long notes. Exercise initial/loading/loaded/empty, validation/API error, offline/timeout, unauthorized/forbidden/session expired, saving/success/failure, partial save, refresh and no search results where applicable. Each visual case records state and fixture version.

Approve a baseline only after functional assertions pass, known overlapping/clipping defects are resolved and a human signs the visual review. Store review owner/date/commit/viewport/theme/state with the image in BASELINES/golden-images. A changed screenshot cannot be approved just to make a test green.
