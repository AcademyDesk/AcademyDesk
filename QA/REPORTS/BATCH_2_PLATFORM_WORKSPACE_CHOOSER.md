# Batch2 — Platform Owner compact Manual navigation

Date:2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. First bounded implementation in active Batch2; not all-portal visual or enterprise acceptance.

## Delivered slice

The existing phone navigation displayed eleven destination rows at320px, consuming substantial vertical space before the content. Both Platform entry points now use one `PlatformMobileNavigation` component below900px. A visible Workspace caption and current section label expose a native disclosure, with all eleven existing destination URLs retained. Selection closes it immediately; Escape closes it and returns focus; clicking outside closes it. The control-page component is keyed by the current tab so route/history changes discard obsolete open state. No new session, authority or tenant state is introduced.

The destination list expands in normal document flow rather than floating over page layers. Its own vertical scroll is capped at55dvh/420px, including short landscape screens.44px links, visible focus, wrapping labels, host semantic tokens and safe-area horizontal padding support both existing themes. The desktop sidebar and existing document/mobile vs bounded desktop content scrolling remain unchanged.

Preserved inherited Platform overlay/header/utility work, selected tenant/drafts, exact routing and backend permissions. Modified only the two mobile-nav render blocks/imports in the already-dirty Platform pages; existing page work was not reset. New component/scoped stylesheet plus an expanded existing navigation runner. No API/domain/schema/Mini/model changes, PENTA frontend package extraction or final Orbit+Aura/Prism parity claim. Mini's actual delivery plan still states reference approval/customer implementation not started; the Manual foundation uses current shared semantic tokens independently.

## Verification

- Installed TypeScript `--noEmit`:PASS.
- Next16.3.5 production webpack build/export:83/83 pages PASS.
- New component ESLint:0 errors/0 warnings PASS.
- Platform pages lint:NOT PASS,8 errors/10 warnings; counts match the inherited result recorded in [earlier foundation](E3_PLATFORM_NAVIGATION_FOUNDATION.md). No lint suppression or fresh frozen-baseline multiset comparison in this slice.
- Navigation runner syntax and scoped whitespace checks:PASS. Initial syntax command used the wrong working directory; rerun from the repository root passed.
- **24/24 exported-browser journeys PASS**, receipt `QA/EVIDENCE/platform-navigation-1791904903625/result.json`, with final source hashes.320/390/768/1024/1440px at700px high plus768×360px landscape, light/dark, Overview/direct Admins entry. Original11-destination sequence/title/URL/one-current-target/history/44px/focus/header geometry/no horizontal overflow/document-vs-content scroll expectations retained:264 consecutive transitions plus history checks, not264 independent domain tests.
- New mobile checks:collapsed nav height≤100px, keyboard open/Escape-focus return/outside dismiss, bounded independently scrollable list with full source destinations, post-selection collapse and updated visible label, Back current label/source-link parity. Desktop original interaction retained.
- Two failed runner receipts retained: `platform-navigation-1791904794625` and `platform-navigation-1791904848592`. Both reached collapsed Announcements then timed out inspecting it by accessible name: hidden native-disclosure descendants have no exposed name even with a role query's hidden option. Final test verifies the visible summary label and exact selected source span/one-current-link separately, while each activated link still receives the original accessible-name/keyboard/44px checks when open. No application fix or removed navigation expectation resulted from these QA selector corrections.
- Eight mobile collapsed/expanded screenshots captured; light collapsed Overview and dark expanded Admins at320px visually inspected. Page content is visible below the compact control; selected destination and keyboard focus are distinct in dark mode. This is browser emulation, not physical Android/iOS/Safari or spoken screen-reader acceptance.

Existing Platform popup regression: **112/112 PASS** on this same production export, receipt `QA/EVIDENCE/platform-overlays-1791904915601/result.json`.320/390/768/1440px × light/dark × empty/40-row fixtures × seven overview/control summaries. Native top-layer/inert focus/44px close/viewport bounds/long-list scroll/sticky close/Escape-close-backdrop/focus return/body-scroll restoration/above-high-z-index/no unexpected page errors/no mutation requests retained. Unsupported notification utility remains disabled; portfolio search route retained. No weakening of the inherited popup runner. Backend/SQL/media/payroll suites retained, not rerun for presentation-only changes.

## Next route and boundaries

Continue **Sol Medium**, existing Academy worktree: settled shared Manual header/surface/control styling, finish Platform Owner bounded checks, then Admin → Teacher → Student. Keep observed Teacher clipping and bottom-nav/history overlap in Batch2. High only for consequential authorization/session/cross-project contracts. Actual versioned PENTA frontend/token delivery is required before final shared-parity acceptance, not every independent Manual layout improvement.

Four delivery groups remain: core-integrity checkpoint → active Manual visuals → qualified conversational workflows → integrated release. Open Batch1 issue/security/device/operational gates stay tracked, not waived. No stage/commit/push/main merge/Azure/production-data action. Existing enterprise strategy and all unrelated dirty work/local evidence preserved.
