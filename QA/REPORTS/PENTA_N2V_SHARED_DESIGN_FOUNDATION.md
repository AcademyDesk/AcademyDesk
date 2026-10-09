# N2v — integrated PENTA shared design foundation

Date: 2026-10-09. Branch `codex/penta-search`, worktree
`D:\AcademyDesk-codex-p0`, starting HEAD `16db7ea78fb9958936ad763b45b30f2d3f856974`.
Scope: independent Academy UI/design continuation while Mini qualification is blocked.

## Requirement and actual change

The owner's final directive makes Academy Desk the initial PENTA design authority,
with one assistant/five capability contexts and later standalone reuse. Existing
read integration, manual ERP, dirty source, evidence and enterprise queue retained.
No new engine, protocol, domain API, SQL, provider, model or tool implementation.

- Added `PENTA_DESIGN_SYSTEM.md`: versioned tokens/identity, conversation/composer,
  cards/tables/clarification/action/approval/feedback/task patterns, responsive,
  accessibility, evidence and commercial reuse rules. Future patterns are labelled
  pending. No package extraction, second frontend or standalone SaaS screens.
- Added scoped `penta-design.module.css`; current chat imports it and identifies
  applied `data-penta-ui="0.1"`. Design tokens contain no Academy domain/routes.
- Existing chat CSS consumes PENTA colors/control/card tokens. Dark primary action
  uses navy text on light blue rather than white. Financial numerals are tabular.
- Capability help anchors to the entire wrapped bar; border-box width stays inside
  the page. Local stacking verified by hit-testing; Escape returns trigger focus.
- PENTA-only sizing/focus overrides isolate controls from legacy ERP compact and
  outline-reset rules. No global theme or other portal controls were modified.
- Added `QA/tools/penta-design-browser.cjs`, actual production-export runner with
  explicit synthetic intercepted host responses and blocked external networking.

## Validation actually performed

| Check | Result / limit |
| --- | --- |
| `npm exec -- tsc --noEmit` in `apps/web` | PASS |
| `npm exec -- eslint src/components/penta/penta-chat-workspace.tsx` | PASS, no output/errors |
| `npm run build -- --webpack` | PASS, 83 static pages; final build includes the focus/sizing fixes |
| `node QA/tools/penta-chat-contract.test.cjs` | 57/57 PASS, existing synthetic response boundary unchanged |
| `node QA/tools/penta-chat-contract-browser.cjs` | 16/16 PASS on final export, existing null/malformed/stale/replay/logout/network/stop recovery assertions unchanged |
| `node QA/tools/penta-design-browser.cjs` | 6/6 viewport-theme combinations: 320/768/1440px × light/dark |
| Design browser subchecks | 30 capability-help bounds/layer/Escape checks; 18 result/empty/error presentations; prompt keyboard outline, touch target, manual switch and no horizontal overflow |
| Computed primary-action text contrast | 5.38:1 light; 8.38:1 dark; enabled normal text, not whole-app WCAG certification |
| Browser errors | Zero unexpected rendering/console errors in final runs; recovery suite retains its three deliberate transport logs |
| Script syntax / `git diff --check` | PASS; existing LF/CRLF warnings only |

Final local evidence:

- `QA/EVIDENCE/penta-design-1791538863611/result.json` and 18 screenshots.
- `QA/EVIDENCE/penta-chat-contract-1791538863611/result.json`.
- Actual screenshots reviewed: desktop light result and mobile dark long-name
  result. This is visual inspection of the exported UI, not source-only approval.

The export is built from the preserved dirty working tree. Previous N2o/N2p
response/recovery implementation remains local; this scoped commit does **not**
publish those earlier source changes. Build/browser passes are explicitly local
snapshot evidence, not an assertion that all that work is included in this commit.
The new design runner itself uses valid typed responses and can be retained
independently of those uncommitted response-boundary tests.

## Retained failures and repair sequence

1. `penta-design-1791538666450`: export before help-position fix failed Executor
   help viewport bounds at mobile width. Retained screenshot shows clipping.
2. `penta-design-1791538702752`: updated help passed; Send touch target failed
   because legacy important button sizing won. Repaired only scoped PENTA CSS.
3. `penta-design-1791538790097`: no focus outline after mouse/programmatic focus.
   Corrected the fixture to actual Tab/Shift+Tab keyboard modality, preserving
   the visible-outline requirement, rather than assuming `focus()` was keyboard.
4. `penta-design-1791538803429`: actual keyboard focus still had no outline.
   Repaired scoped focus CSS priority; final strict 3px outline checks pass.

All failed JSON/screenshots remain local. No weakening of contrast, 44px,
geometry, layering, focus, receipt or recovery expectations. Final actual
source changes, not another unchanged retry, resolve the observed UI failures.

## Still open / exact next step

- Mini saved N2r remains **86/94 NOT ACCEPTED**; no new inference/benchmark,
  service rebuild or engine change. Mini HEAD remains `a50bbd3`.
- Active Tool Protocol 0.1 unchanged; new draft/Core contracts not enabled.
- No fresh SQL/auth/tenant/linked source test, physical Android/iOS, screen-reader,
  full visual-regression baseline, full five-capability or production acceptance.
- Owner Command Centre, tables, real action/approval inbox, collected feedback,
  private history and future standalone frontend remain pending their vertical gates.
- Existing finance, privacy, capacity, P0/enterprise and pre-Azure gates stay open.

Continue Mini's existing [N2u packet](../../PENTA_HANDOFF_TO_MINI.md) in its project
on the specified Sol High route; do not recreate its intelligence here. Independent
Academy work may consume the documented UI rules without enabling draft APIs.
After a qualified Mini return, verify artifact/contracts and run the prepared real
SQL/security + 15-turn browser journey, then refine integrated UI and issue the
human READY TO VIEW checkpoint. No major domain expansion before that review.

Publish only these validated logical design/QA/continuity changes on the feature
branch, preserving earlier dirty work and `QA/EVIDENCE`. No main merge, Azure,
production data, paid infrastructure, model training/download or external tools.
