# C3.1 — role entry and Manual fallback — 6 November 2026 IST

Owner: Cloud Codex. Status: CLAIMED. Branch: codex/cloud-c3-role-entry-20261106. Base: 30d0fd5cca8c58dff9b2af5262b8a86e9d53b3de from the current local-owned host branch. Desktop's L1 module/runtime results are inherited within their recorded limits, not rerun or superseded. Application frontend is unchanged from the earlier C2 checkpoint.

## Exact path ownership before implementation

- `QA/HANDOFF/C3_ROLE_ENTRY_2026-11-06.md`
- `QA/tools/penta-entry.test.cjs`
- `QA/tools/penta-entry-browser.cjs`
- `apps/web/src/app/login/page.tsx`
- `apps/web/src/components/workspace-frame.tsx`
- `apps/web/src/lib/workspace-entry.ts`
- `apps/web/src/components/penta/role-penta-entry.tsx`
- `apps/web/src/components/penta/role-penta-entry.module.css`
- `apps/web/src/components/penta-manual-theme.ts`
- `apps/web/src/components/penta-manual-theme.module.css`
- `apps/web/src/vendor/penta-shared-ui/tokens.ts`
- `apps/web/src/vendor/penta-shared-ui/foundation.css`
- `apps/web/src/app/penta/page.tsx`
- `apps/web/src/app/teacher/ai/page.tsx`
- `apps/web/src/app/portal/ai/page.tsx`
- `apps/web/src/app/platform/ai/page.tsx`

Reuse preserved frontend source from 6887e6eb26e88f8fc99eee7a00c267a14c12883f selectively. Restore role-aware unavailable entry, scoped theme tokens, explicit Manual navigation with unsaved-form protection and workspace-scoped login routing. Extend entry presentation to the AcademyAdmin namespace without enabling actions. Login must reject malformed/mismatched server profile and unsafe/cross-workspace return routes before persisting tokens. PENTA entry requests only authenticated session metadata and never a model/tool endpoint. No synthetic provider, model selector, chat, domain mutation or backend flag is activated.

No backend API/controller/service/SQL/migration edits; local L1 ownership remains. No changes to existing auth refresh implementation, Manual business forms, shared trackers, dependency declarations or packages. Mini source is read-only. This first C3 increment restores entry and fallback, not complete conversation/draft/approval/receipt UI. Source provenance and exact scoped validation will be recorded here.

Validation: existing and new workspace-routing/session tests; current locked TypeScript/lint/build; headless Chromium tests against synthetic session-only fixture where installed tooling supports it. Preserve current full-suite SQL-evidence failures, skipped/unrun results and all safety gates. No package download, model acquisition/load/inference/training/activation, main merge, Azure deployment, customer data or environment publication. Next: remaining protected conversation UI in a separately claimed packet after L1/context/qualification review.
