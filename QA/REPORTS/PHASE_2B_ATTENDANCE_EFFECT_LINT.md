# Attendance effect-lint cleanup — 2026-10-09

Existing enterprise continuation; worktree `D:\AcademyDesk-codex-p0`, branch
`codex/penta-search`, starting HEAD `f57696a606394a2dd92772de9910121db7e9f83d`.

## Bounded repair

Independently linted HEAD: one `react-hooks/set-state-in-effect` error and two
`react-hooks/exhaustive-deps` warnings. Mount-only workspace loader now lives in
its effect; initial session uses a functional setter. Attendance fetching is a
pure async read helper; session effect awaits it before updating the session-keyed
cache and depends only on academy ID/session ID. Successful-save readback reuses
the same helper. No lint suppression, artificial scheduling workaround, new
callback dependency, automatic retry or business/API/permission/visual change.

## Verification

- HEAD lint receipt: `QA/EVIDENCE/attendance-effect-lint-head-20261009.log`.
- Existing32 actual-TSX checks retained unchanged; added2 lifecycle guards for
  request counts across drafts/notices/saves/switches and uncertain write outcomes.
  Both HEAD and final34/34 PASS (zero skipped/cancelled). These are positive
  regression controls, not a claim that HEAD failed behavioural tests.
  `QA/EVIDENCE/attendance-effect-lint-baseline-20261009.log` and
  `QA/EVIDENCE/attendance-effect-lint-final-20261009.log`.
- Exactly five initial workspace reads and one initial attendance read; draft
  edits and repeated renders add no requests; successful save adds only one POST
  and one readback; each session switch adds one attendance read.503 uncertainty
  adds only its POST and retains notes without automatic retry/refetch.
- Target page ESLint PASS, zero errors/zero warnings. TypeScript PASS.
- Production webpack build/83-page export PASS.
- Existing browser runner16/16 PASS:320/1440 light/dark, normal/failed-refresh/
  rejected400/committed500. Original exact POST/notes, retained or saved display,
  restored controls, no horizontal overflow/unexpected JS/hydration errors.
  `QA/EVIDENCE/attendance-feedback-browser-1791566447253`.
  Synthetic API transport and emulated viewports, NOT live SQL/Identity/device.

Previous SQL receipts retained, not rerun; no backend/engine/container change.
BUG-FUNC-0003/BUG-DATA-0035 remain OPEN for their broader linked/device/critical
gates. Main/Mini/Azure, unrelated local work and QA/EVIDENCE preserved. No release
or enterprise completion claim. Mini saved86/94 CONTRACT READY / ENGINE BLOCKED
and existing qualification handoff unchanged.

NEXT: Sol Medium for the existing Make-up success-feedback gap check; reuse
accepted repairs and test only remaining gaps. Sol High if authority, persistence
or domain contracts require changes. No new testing plan or engine expansion.
