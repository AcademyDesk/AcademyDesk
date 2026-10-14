# Batch2 — Admin Manual foundation and navigation focus

Date:2026-10-13. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, unchanged HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. Scoped presentation/navigation work; not full Admin/module/enterprise acceptance.

## Delivered

EnterpriseShell explicitly adopts the existing shared Manual stylesheet with an Admin modifier only in Manual mode. Both Manual classes are absent in PENTA AI mode, so the AI canvas keeps its existing styles. Shared semantic tokens and prior Platform-only selectors are preserved; no global theme/package rewrite or versioned FW1 delivery claimed.

Header owner text wraps; role label stays visible on phones; utility links/buttons/theme/profile targets are at least44px. Below360px the five utility groups use a compact grid and icon-only theme presentation while retaining their explicit accessible names and44px buttons. Profile, search and notification menus wrap/bound horizontal content and scroll. Search and notifications anchor below the current header rather than a fixed pixel offset; notifications use the header as their containing block instead of overflowing left from the middle utility icon. Manual fields/mobile text/controls/sidebar targets share44px and16px foundations; surfaces use the existing token-backed canvas/card language.

Repeated mobile destination labels now identify their group, such as **Students · Overview** versus **Teachers · Overview**. Existing click-to-collapse and route/query reset remain; Escape closes the workspace chooser and returns focus; outside pointer closes it. Original URLs, module inclusion and finance-only/PENTA eligibility logic, upgrade prompts, view switching, drafts, API operations and domain authority remain unchanged.

### Confirmed navigation defect repaired

An intermittent keyboard route timeout led to a separate deterministic reproduction: focus a navigation link, dispatch the existing window-focus announcement refresh, await its response and two render frames, then test that the same DOM node remains connected and focused. Before repair this assertion fails. The nested `LockedNavigationItem` definition created a new component type on every shell render, replacing links even when their destinations/permissions did not change. It is now a stable top-level component with the same presentation/eligibility/included/active/upgrade values passed explicitly. This is component identity repair, not a permission grant. React's [component identity guidance](https://react.dev/learn/preserving-and-resetting-state#dont-nest-component-definitions) supports the mechanism; actual failure/pass evidence is in the local browser receipts. The earlier uninstrumented timeout is not claimed retrospectively proven to have this exact cause.

## Verification

- Final Next16.3.5 webpack production build, TypeScript and83/83-page export PASS.
- EnterpriseShell ESLint0 errors/1 warning. Starting HEAD independently linted through stdin:0 errors/1 warning too; no suppression or clean-lint claim. Final post-focus-fix lint also0/1.
- **24/24 final synthetic production-export journeys PASS**, `QA/EVIDENCE/admin-manual-1791907275802/result.json`, source hashes for shell and stylesheet.
-320/390/768/1024/1440×700 plus768×360, light/dark, Core-only/full synthetic module sets. Long owner name35-row fixtures;44px utilities/320px one-row geometry/name wrap/no horizontal overflow; repeated-label distinction/two consecutive keyboard routes/collapse/Escape focus return/outside dismissal; exact current-link connection/focus through announcement refresh.
- Profile/notification/search horizontal bounds and text wrapping;44px search/mobile16px; Core-only Leads remains a button opening Upgrade rather than a usable link; full fixture exposes its link. No upgrade navigation/write executed. These are existing client presentation rules, not live RBAC proof.
- Simulated PENTA health503 produces Unavailable; Manual classes absent in AI mode and restored on switching back; Student Overview and document/mobile vs desktop workspace scrolling remain usable. No chat creation or turn request dispatched; zero mutation requests/unexpected page errors required. AI requests and domain reads are synthetic, not model/SQL acceptance.
- After stable-component repair, existing unchanged **6/6 PENTA design journeys PASS**, `QA/EVIDENCE/penta-design-1791907130196/result.json`; real exported chat/result/help/recovery interactions with synthetic inference, contrast5.38 light/8.38 dark. Existing unchanged **32/32 shared detail-modal journeys PASS**, `QA/EVIDENCE/manual-detail-modal-1791907140786/result.json`; empty/long Student/Teacher lists/four widths/both themes/top-layer/inert focus/close/scroll/no-write retained. These ran before the final320px utility-grid refinement; not falsely claimed fully rerun after that CSS-only change. Final24 Admin journeys cover the final refinement.
- Four final320px screenshots captured; light Core-only and dark full-module images visually inspected: header utilities no longer wrap the avatar, Manual/PENTA switch retained, group-specific current label visible. Physical Android/iOS/Safari and spoken screen readers NOT RUN.
- Runner syntax/scoped diff checks PASS. An initial syntax command used a web-relative QA path; corrected repository-root command passes. Earlier backend/SQL/media/finance/Platform evidence retained, not redundantly rerun.

Preserved receipts: `admin-manual-1791906809335` status locator timeout (nested status text made exact text too narrow; final explicit unavailable substring still required); `1791906872100` intermittent uninstrumented navigation timeout; `1791906956236`24-case pass before focused-refresh assertion,not acceptance of that defect; `1791907029525` deterministic focus-refresh failure; `1791907119822`24 cases PASS after repair before final utility-grid refinement. Initial screenshot avatar-wrap defect repaired with grid spacing,not smaller controls. Final focused-refresh assertion retained unchanged.

## Remaining and next

Batch2 remains IN PROGRESS. Next **Sol Medium: Teacher Manual foundation**, preserve its already-dirty backend/UI/media/payroll work; address recorded narrow header/list/action clipping and bottom-nav/history overlap, then Student. High only for changed security/session/domain/cross-project contracts.

Admin all-form/module visual migration, complete draft continuity/real host/role/outage journeys, other utility focus/scroll combinations, inherited lint, physical devices/accessibility and versioned PENTA theme/component parity remain OPEN. No claim that every Admin action/permission or90% AI coverage is delivered. Existing enterprise strategy/Batch1 release gates preserved; no issue closure, API/schema/Mini/project switch/production credential or container action. No stage/commit/push/main merge/Azure.
