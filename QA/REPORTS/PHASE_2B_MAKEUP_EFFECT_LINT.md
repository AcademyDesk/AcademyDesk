# Make-up effect-lint cleanup — 2026-10-09

Existing enterprise continuation in `D:\AcademyDesk-codex-p0`, branch
`codex/penta-search`, starting `6fba640a038d113d749da591c4a31db81ef4d0a6`.

Pure async workspace fetch is now shared by the scoped mount initializer and
successful-save readback. The effect does not capture the component loader or
academy state; no dependency suppression, callback reload loop or artificial
scheduling workaround. API paths/payloads, delivery/location rules, timezone,
permissions, defaults and visuals unchanged. Existing save feedback reused.

Validation:

- All31 prior controlled feedback/location tests retained, plus2 lifecycle positive
  controls:33/33 PASS before and after, zero skipped/cancelled. Initial five reads;
  draft/mode/render changes add none; each successful write adds one mutation and
  four readbacks; uncertain503 adds only its mutation without retry/refetch.
  `QA/EVIDENCE/makeup-effect-lint-baseline-20261009.log` and
  `QA/EVIDENCE/makeup-effect-lint-final-20261009.log`.
- Previous independently verified HEAD lint0 errors/1 dependency warning retained
  in `QA/EVIDENCE/makeup-feedback-lint-20261009.log`. Final0 errors/0 warnings,
  no suppressed messages: `QA/EVIDENCE/makeup-effect-lint-final-eslint-20261009.json`.
- TypeScript and production webpack83-page export PASS.
- Existing exported-browser16/16 PASS across320/1440 light/dark and success,
  failed readback, rejected400 and committed500; exact POST/PATCH payloads, input
  retention/reset, durable messages, fixture rows/status, no horizontal overflow
  or unexpected JS/hydration errors. Expected resource errors counted separately.
  `QA/EVIDENCE/makeup-feedback-browser-1791569045625`.
  Synthetic transport/emulated viewport, not live Identity/SQL/device acceptance.

No fresh backend/SQL/engine/container tests or changes; prior receipts retained.
BUG-FUNC-0003 and linked/device/critical/enterprise/release gates remain OPEN.
Main/Mini/Azure and unrelated dirty work/local evidence preserved; scoped feature
publication only. Mini saved86/94 CONTRACT READY / ENGINE BLOCKED and its existing
qualification handoff unchanged.

NEXT: Sol Medium for existing Leave-request feedback gaps; Sol High only for
domain/access/persistence changes. No new testing plan or Azure deployment.
