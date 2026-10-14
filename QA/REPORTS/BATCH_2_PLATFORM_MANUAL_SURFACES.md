# Batch2 — Platform Manual header, surfaces and controls

Date:2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. Presentation-only continuation, not full portal or enterprise closure.

## Implemented

New scoped `apps/web/src/components/manual-workspace.module.css` is applied only to the two Platform pages. Existing shared semantic tokens remain authoritative; no global theme rewrite or PENTA canvas styling. Future portal consumers must be explicitly adopted and verified rather than receiving a blanket global override.

- Quiet token-derived canvas tint,18px card surfaces and consistent token-backed primary action fills/text across light and dark.
- Header grows with content instead of forcing a fixed height; full owner names wrap. Phone identity and utilities remain separate, with utilities fitting one row at320px after a4px-gap refinement.
- Search/disabled notification/theme/profile targets are at least44px. Profile menu wraps long text, is width-bounded and internally scrollable; Sign out remains reachable and above content. On short screens the document can scroll the anchored menu into view; no all-popover top-layer claim.
- Content fields/buttons at least44px high; mobile editable field text16px. Existing native values, validation, routing, drafts, API calls and action behavior unchanged. Checkboxes/radios/hidden inputs excluded from text-field sizing.
- Shared visible focus outline for interactive controls. Existing navigation, current labels, Escape/collapse and body/content scrolling retained.

Read actual saved Orbit/Prism image references and latest Mini frontend plan. These are raster references, not a delivered versioned component/token package. The owner’s Orbit+Aura dark/Prism light requirement remains the target; this foundation does not certify full parity or enable any AI capability.

## Evidence

- Final production Next16.3.5 webpack/export:83/83 pages PASS, including TypeScript. Initial build also passed; final rebuilt after phone spacing refinement.
- **24/24 final synthetic exported-browser journeys PASS**, `QA/EVIDENCE/platform-navigation-1791905944051/result.json` with seven source hashes, including the new stylesheet.
-320/390/768/1024/1440×700 plus768×360, light/dark, Overview/direct Admins. Original11-destination sequence,264 transitions, history/title/current-page/header geometry/keyboard focus/44px destinations/document-vs-content scroll/no horizontal overflow/no page errors/no mutation requests retained.
- Added long synthetic owner name/email fixtures; all visible header controls44px, phone utility centres in one row, full name wrapping, profile horizontal bounds/long-text wrap/reachable foreground Sign out. Final Overview search field44px/mobile16px and actual normal-text primary-action contrast≥4.5:1 checked in all24 journeys.
- Existing unchanged popup runner **112/112 PASS**, `QA/EVIDENCE/platform-overlays-1791905750251/result.json`, during the initial styling build, before final phone-only utility gap refinement. Retained top-layer/inert focus/close/scroll/focus-return/no-write cases. This is intermediate popup evidence, not a claim that the whole112-case matrix was rerun after the final gap change; final24-case navigation/header checks cover that change.
- Eight final collapsed/expanded320px screenshots captured; light/dark collapsed Overview visually inspected. Utility row no longer wraps the avatar, long name retained, field/control/card separation clear.
- Runner syntax/scoped whitespace checks PASS. New CSS compiles; no TS logic changed beyond imports/classes. Previous component lint0/0 retained, not rerun; inherited Platform8 errors/10 warnings remain OPEN, not claimed clean.

Preserved failed QA observations: `platform-navigation-1791905747953` (pointer modality before keyboard-focus assertion), `platform-navigation-1791905770307` (same issue at first desktop link), `platform-navigation-1791905920352` (row probe unintentionally included hidden profile-menu Sign out). Final setup explicitly presses Tab before keyboard-focus checks, probes direct utilities/theme buttons/profile summary, and independently checks menu action reachability. Original focus/target/navigation assertions retained. Screenshot-observed avatar wrapping was an actual spacing defect, repaired without shrinking controls.

No real API/SQL/auth/RBAC/device/Safari/screen-reader or whole-portal acceptance in these synthetic UI checks. Backend/media/finance regression evidence retained, not rerun for scoped styling. No credentials/production databases/Mini project/container/deployment changes. No stage/commit/push/main merge/Azure.

## Next

**Sol Medium: Admin shared Manual foundation**, explicitly adopt the scoped styles after inspecting its shell/context/controls. Preserve AI/Manual switching, module locks, routes/drafts, authenticated context and independent fallback. Then Teacher → Student, including known list/action clipping and bottom-navigation/history overlap. Keep wider Platform form/loading/overlay/lint/device and actual versioned PENTA parity gates tracked. High only for consequential security/session/domain/cross-project changes.

Batch2 remains IN PROGRESS. Batch1 repair checkpoint and open enterprise gates retained; Batch3 conversational expansion and Batch4 release remain pending. No new testing strategy or re-audit of accepted work.
