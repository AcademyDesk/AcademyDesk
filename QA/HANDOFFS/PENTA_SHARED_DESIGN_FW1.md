# Academy Desk → PENTA AI: shared design foundation and frontend handoff

Packet: **FW1-R1** · Prepared2026-10-13 · Status: **REQUEST PREPARED / NOT DELIVERED OR ACCEPTED**.

This is a frontend delivery request continuing FW1, not a new AI architecture, engine qualification, testing strategy or release approval. It has not been automatically sent to the PENTA project.

## Copy-ready task for the existing PENTA AI project

### Routing / work boundary

PROJECT: Existing PENTA AI / Mini project, using Codex only.

REFERENCE DIRECTORY: `D:\PENTA AI Models`. Verify the actual repository root, worktree, current branch, HEAD and dirty status before editing; do not invent a branch or assume this reference directory is the Git root. Preserve current frontend/Core/model/policy/benchmark work. Read the project's applicable governance and current handoff first. Locate governance if absent at the displayed root; report genuine conflicts rather than fabricate instructions.

MODEL: Start on GPT-6.1 Sol High for the bounded ownership, host-adapter and design-foundation decisions. Once interfaces/design are settled, recommend Sol Medium for component/style/responsive implementation. High remains appropriate for consequential permission/context/cross-project changes. This is coding-agent routing, NOT selection or replacement of PENTA's runtime Mini/Plus/Pro/Ultra models. Do not claim to have changed the user's model setting.

MAY MODIFY: Shared PENTA frontend/design-system source, synthetic component gallery/tests, frontend delivery documentation and its return handoff in the verified PENTA project.

MUST NOT MODIFY: Academy Desk source/database/credentials, existing engine/model qualification, tool permissions, finance policies or unrelated dirty work. No training/weights/model upgrade, third-party runtime AI fallback, broad backend rewrite, production calls, Azure, external design tools, staging/commit/push/merge/history rewrite or evidence deletion without separate explicit authority. This request does not authorize running another project's pending database migrations.

### Mission

Deliver one premium, coherent interface family for PENTA AI and Academy Desk Manual. Academy's target is90% of eligible work through conversational AI and10% manual control/fallback; this is a goal, not current measured coverage. The UI must support eventual operational workflows beyond CRUD without presenting unsupported features as working.

Do NOT wait to finish the entire AI frontend before returning the shared foundation. Deliver two bounded milestones; **start Milestone A now and stop at its return checkpoint**. Milestone B follows visual/interface acceptance or a new continuation instruction.

### Read existing sources; do not restart accepted audits

In PENTA, read current handoff and applicable governance, `docs/PENTA_FRONTEND_DELIVERY_PLAN.md`, current synchronization/platform/tool/security contracts and approved visual references. Locate actual files if names changed; record supersession explicitly. Older ownership wording is subordinate to the latest owner direction.

Academy read-only references are in `D:\AcademyDesk-codex-p0`:

- `AI_PRODUCT_VISION.md`, `PENTA_DESIGN_SYSTEM.md`, `PENTA_HANDOFF_TO_MINI.md` (FW1 section), `PENTA_HOST_SECURITY_POLICY.md`.
- `ACADEMY_PROGRESS_CHECKLIST.md`, current `AI_HANDOFF.md` and Batch2 Platform/Admin/Teacher reports.
- `apps/web/src/components/penta/`, `enterprise-shell.tsx`, `workspace-frame.tsx`, `workspace-nav.tsx`, `manual-workspace.module.css` and shared navigation components.
- Installed consumer: Next16.3.5, React19.2.8, TypeScript, CSS Modules; production uses static export. Package integration must remain browser/server-render safe and respect installed tooling. Do not mandate a framework/dependency replacement or new repository.

Academy branch `codex/penta-search`, HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`, with substantial legitimate uncommitted work. That SHA does not include current local visual/backend changes. Read-only source comparison must account for dirty files; do not replace them with HEAD or edit the main checkout.

### Ownership

PENTA owns reusable AI frontend, shared visual primitives and standalone PENTA shell. Academy owns Manual ERP screens, integrated host shell/mode switch, authenticated gateway/connector and domain authorization/execution. Consume reusable primitives and semantic tokens, not a complete chat shell imposed on Manual pages.

Exactly four Academy portals: Platform Owner, Academy Admin, Teacher, Student. Student/guardian restrictions remain enforced by the host; do not invent a fifth Academy portal. Platform fleet context differs from academy scope. Standalone PENTA is a separate customer product, not another Academy portal.

Academy-only switch: **PENTA AI | Manual Workspace**. Standalone has no Manual switch or Academy route/domain dependency. Optional source navigation comes from trusted host/connector callbacks, never model-generated URLs or tokens in query strings.

### Milestone A — shared design foundation FIRST

1. Reuse existing tokens/components and accepted inventory. Record reusable pieces and missing ones without redoing the enterprise audit. Supply a concise component/consumer map.
2. Use owner-confirmed **Orbit + Aura dark / Prism light**. PENTA documents also use **Aurora** for standalone: explicitly reconcile Aura/Aurora naming; do not silently rename approved assets or create competing theme systems. Keep shared semantics even when standalone shell composition differs.
3. Deliver actual semantic tokens: canvas/surface/elevated/control surfaces, text/muted/on-accent, borders/focus, selected/disabled, success/warning/danger; typography, spacing, sizing/radii, iconography, elevation/overlay layers and motion/reduced-motion. Include JSON or typed token definitions plus usable CSS and a versioned Academy mapping. Never make form fields indistinguishable from backgrounds or labels.
4. Deliver coded, interactive primitives covering buttons/icon actions, fields and labels/help/validation, selects/search/menus, date/month/year and time controls, cards/KPIs, tables/list actions/pagination, tabs/navigation, dialogs/popovers/drawers, notices/toasts/status/confirmation. Preserve existing standard controls where suitable; mark unimplemented primitives as pending rather than fabricate completeness. Date controls need direct month/year selection; menus must close on selection/Escape/outside interaction and remain anchored/bounded while scrolling.
5. Demonstrate the SAME primitives in an AI conversation/composer/result/context example and a Manual form/register example. Include representative portal navigation/header variants for all four roles, using clearly labelled synthetic data. Do not build all Academy screens in PENTA.
6. Provide role/context/capability adapter boundaries. Host supplies authenticated context and permitted/available actions; public presentation props carry no secrets or authoritative model-provided roles/tenant IDs. Prefer callbacks for host navigation/theme/available actions/approved mode changes. Keep Academy auth tokens out of component props/gallery logs, browser persistence and URL parameters. Do not create new production endpoints or alter active protocol versions for a visual deliverable.
7. Export a version-pinned candidate and component registry/props/state contracts, peer/build requirements, source hashes/commit plus dirty-source manifest if uncommitted, CSS/font/icon requirements, import examples, migration map and rollback guidance. Do not create an uncontrolled copy or publish a package to a registry without approval. A proposed next version must be labelled proposed, not an active protocol.

Keep themes premium and futuristic through clear hierarchy, restrained depth and precise accents—not excessive glow, decoration, opaque controls or fake operational dashboards. Manual remains efficient forms/registers, not chat on every screen. Prevent global CSS/theme/storage collisions, including when shared code is not mounted or AI is unavailable.

### Milestone B — AI interaction prototype AFTER A

Build the reusable coded conversation/work-canvas prototype using the accepted foundation:

- One assistant; Pulse, Executor, Navigator, Twin, Autopilot capability contexts. Selecting Executor reveals/focuses the chat composer. Users enter natural-language prompts, not compulsory search-mode forms.
- A small accessible `!` help control beside each capability opens a bounded description distinguishing current versus planned support, with close/Escape/focus return.
- Desktop conversation plus useful right-side source/context/task detail; tablet collapsible panel and mobile drawer/reflow with composer, keyboard and safe-area clearance. Do not expose private model chain-of-thought or label fabricated activity as reasoning.
- Typed results in chat/cards/tables with provenance/as-of/record links from trusted callbacks; clarification/disambiguation; sequential field collection/correction; draft summary; duplicate-warning presentation; before/after preview; approval/confirmation for sensitive or destructive actions.
- Loading, empty, denied, unavailable, stale-context, expired/revoked approval, network failure and UNKNOWN OUTCOME. Unconfirmed writes must not show success or silently retry. Stopping local waiting does not prove server cancellation. Success must come from authoritative completion; prototype outcomes remain explicitly synthetic.
- Academy Brain feedback/corrections as safe proposed UI; no automatic model training, global/private transcript persistence, permissions, policies or finance-rule changes.
- Integrated and standalone shells share components. Unsupported CRUD/send/delete/Autopilot features are disabled/unavailable with truthful guidance. Existing qualified reads remain governed by their independent backend gates.

### Security / migration rules

All real operations remain: authenticated user → server-resolved tenant → permitted tools → interpreted/validated request → current host authorization → required approval → existing domain service → authoritative result → audit. UI hiding is not authorization. No arbitrary SQL/code execution, direct provider credentials, model-supplied permission grants or learning overrides.

Preserve the existing Academy pilot, drafts/failure recovery, native overlay/focus behaviour and Manual fallback. Context switches must use host-approved lifecycle/reset rules; cancel local waiting and clear stale artifacts/approvals without carrying data across tenants. Do not invent cross-context transcript transfer or treat UI role selection as access authority. Proposed lifecycle changes need a versioned paired host handoff before activation.

UI delivery does NOT qualify Mini's language/security benchmark or close prior enterprise defects. The inspected frontend delivery document records historical86/94 NOT ACCEPTED; check the current engine handoff for newer evidence and preserve failures. Do not present that old count as a freshly rerun result or rerun an unchanged failed model just to satisfy this visual packet.

### Validation / acceptance

Use installed tooling and retain affected existing tests. Record commands, exact candidate source, failures and what was not run.

- Typecheck/build/component lint and relevant regression tests.
- Both themes at320/390/768/1024/1440 widths plus short landscape and text zoom; wrapping/no unintended horizontal overflow/no clipped labels or action buttons.
- At least44px interaction targets and legible mobile input text; WCAG2.2 AA-oriented contrast/keyboard/focus checks. Do not claim certified accessibility or real screen-reader/device acceptance from emulation.
- Dropdown/dialog/drawer layering, bounds, scroll, dismissal, focus restoration; clear field/label distinction; no trapped page scrolling or navigation/composer overlays covering final records.
- Long names/titles, many table/list records; loading/empty/denied/error/pending/success/unknown-outcome states.
- Synthetic capability projections for all four roles and AI outage/Manual fallback using host adapter mocks; no unauthorized action portrayed as executable. These mocks are not real RBAC proof.
- Prototype transport isolation: no production/inference/tool calls or mutation side effects. Media/financial/task metrics must be labelled synthetic, not real receipts.

Return a visual-review gallery/local instructions/screenshots plus coded source; screenshots alone are not delivery. Separate implementation, synthetic proof, owner approval, host integration and production acceptance.

### Required return handoff / STOP

At Milestone A, create/update `PENTA_RETURN_TO_ACADEMY_DESK.md` (locate its current authoritative equivalent) and current PENTA handoff. Return:

1. Status **SHARED FOUNDATION READY FOR REVIEW** or **BLOCKED**, not production-ready.
2. Actual repo/branch/HEAD/dirty-source manifest; exact files/artifact hashes and token/component candidate versions. No commit/push implied.
3. Delivered/pending component checklist, theme naming/mapping and per-role/per-state preview inventory.
4. Academy import/adapter examples, Next/React/static-export compatibility, migration steps and rollback.
5. Tests/results, preserved failures, limitations and remaining host/engine/security/device acceptance.
6. Any genuinely required paired-contract changes, explicitly inactive until reviewed.
7. Milestone B next tasks/model route. Recommend Sol Medium for settled UI; High for consequential unresolved interfaces/security.

STOP after Milestone A handoff. Do not automatically modify Academy, qualify engines, deploy or start the entire AI backend again. Academy resumes shared Manual adoption from the reviewed foundation and performs its own integration/regressions. Further AI frontend work proceeds in PENTA under its existing plan, not by replacing or restarting it.

## Academy preparation receipt

This packet only adds documentation. Platform/Admin/Teacher Manual foundations have bounded browser/build evidence; Student/full-screen migration/final parity remain pending. Backend core repair checkpoints do not constitute completed backend/release approval. Existing enterprise and Mini integration work is preserved.

Local preview was verified earlier this session: frontend `http://localhost:3000/login`200, API `http://localhost:5092/health`200, local SQL connected, auth session401 without credentials and localhost CORS. API uses process-local `LocalPreview` settings with automatic migrations/seeding disabled; no Azure credentials or production database used. Local history has87 migration entries and the new PayrollNoCashDueStatements migration is not applied. Backup/migration work was interrupted before any tool execution; it remains pending, not part of this handoff. Preview service liveness has not been rechecked for this documentation-only packet.

Model-route recommendations follow project routing, with current effort guidance checked in [official OpenAI documentation](https://developers.openai.com/api/docs/guides/deployment-checklist#set-up-reasoningeffort). High-first then Medium is an engineering judgement, not an OpenAI-mandated sequence or permission to replace the runtime Mini model.
