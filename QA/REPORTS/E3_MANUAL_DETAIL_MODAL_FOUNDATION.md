# E3 — Manual detail-modal foundation

Date:2026-10-11. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, starting `ac7956a3b7d0945ef10af718a948c45cb5fc4236`. Bounded UI checkpoint, not E3/enterprise acceptance.

## Ownership and scope

Latest local Mini `docs/PENTA_FRONTEND_DELIVERY_PLAN.md` (2026-10-10) supersedes the older sync/design ownership wording: Orbit integrated, Prism light, Aurora standalone. The inspected plan says customer frontend implementation has not started. FW1 remains prepared, not an automatically delivered/returned versioned UI release. No Mini/pilot/protocol/authority/runtime/domain change, project switch or external message.

Existing Manual/PENTA semantic colors already align. No duplicate token system or global theme replacement needed. Existing `StandardDetailModal` consumers retain their props, records, permissions, calculations and close callback.

## Repair

- Native `dialog.showModal()` top layer replaces the fixed div; inert background controls and no ancestor clipping/page-local z-index workaround.
- Explicit initial close focus, Escape/outside/close dismissal, opener focus restoration after React removal and exact previous body-overflow restoration.
- Scoped CSS module using host `--ad-*` tokens:44px close target, visible focus,14px panel radius, dynamic-viewport bounded scrolling, sticky header and long-word wrapping. No unreleased PENTA package copied.
- Existing registry documentation updated; no API writes added.

## Evidence

`QA/tools/manual-detail-modal-browser.cjs`: actual production export, synthetic reads, non-local traffic blocked.320/390/768/1440px × light/dark × Admin Student Overview/Teacher Overview × empty/40-record long-content = **32/32 PASS**. These are two Admin pages, not Teacher/Student portal acceptance.

Checks: native top-layer/named dialog, initial-close focus,44px target, close-text contrast >=4.5:1, panel/page horizontal bounds, scroll lock/restore, background focus exclusion, Tab/Shift+Tab not reaching background app controls, close/Escape/opener return, reopen/backdrop close. Long content scroll and sticky close verified; synthetic high-z-index sibling cannot cover modal. Browser chrome may legitimately receive Tab focus. Zero mutation requests and page errors required.

Final `QA/EVIDENCE/manual-detail-modal-1791687714360/result.json` includes source SHA256 identities. Light student/dark teacher320px screenshots inspected: readable wrapped names, clear header/close/surfaces and bounded scroll. All evidence remains local.

Retained failures (assertions not weakened):

- `manual-detail-modal-1791687555835`: previous export immediately fails native-modal check; not a fresh full baseline build/matrix.
- `manual-detail-modal-1791687614530`: first implementation fails focus return; explicit cleanup restoration added.
- `manual-detail-modal-1791687653332`: first empty case passes, long content fails initial focus. Removed React imperative autoFocus; focused close explicitly after showModal.

An initial build session ID was not retained. Two additional attempts correctly refused the active build lock; no process killed/lock deleted/config changed. Waited, then captured successful builds; final after focus correction passed. Missing session output is not acceptance evidence.

- ESLint0 errors/0 warnings; installed TypeScript `--noEmit`:PASS.
- Next16.3.5 webpack build/export83/83:PASS.
- Runner syntax and scoped diff checks:PASS.
- Previous onboarding/date/SQL/Identity/RBAC/finance proofs retained **NOT rerun**; no domain change.

## Remaining / next

E3 remains OPEN: Platform Owner -> Admin -> Teacher -> Student rollout, all actual consumers, physical Android/iOS/Safari, screen readers, visual regression, AI outage and live host journeys. No issue closed/full WCAG/legal/production claim. Original28 feedback count unchanged; critical/Mini86/94/FW1/release OPEN. Legacy consistency remains blocked by missing local `observed-checks.json`, no fabricated full-QA pass.

Preserve11 unrelated tracked modifications,32/28 unrelated handoff/readme additions, untracked Mini work and evidence. Scope-only feature publication is not main/Azure acceptance.

NEXT **Sol Medium**: Platform Owner Manual shell/navigation/responsive/keyboard check and demonstrated bounded repairs; retain accepted navigation/dropdown fixes. **Sol High** for consequential authority/context/domain/shared-package/release integration. No project switch; genuine Mini dependency uses existing FW1 and explicit return gate.
