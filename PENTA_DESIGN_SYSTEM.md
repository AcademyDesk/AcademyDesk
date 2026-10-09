# PENTA shared design language

Version: **0.1 integrated pilot** · 2026-10-09 · Initial design authority: Academy Desk.

This is the common visual and interaction specification for Academy Desk PENTA
and the future standalone PENTA AI product. It is not a second application,
published UI package, enabled protocol draft or model-readiness claim.
The current feature branch is `codex/penta-search` in `D:\AcademyDesk-codex-p0`;
the starting revision for this slice is `16db7ea`.

## Ownership and compatibility

Academy Desk owns the integrated shell, Owner Command Centre, conversation,
context panels, artifacts, approvals, feedback, connector and real host services.
Mini owns the canonical Core/inference/Tool Protocol and reusable intelligence.
Standalone PENTA must not depend on Academy Desk SQL, routes or domain classes.
The two products share one visual identity, with different authenticated shells.

Consume Mini's canonical `docs/PENTA_PLATFORM_CONTRACT.md` and latest handoff
in `D:\PENTA AI Models`; those currently local documents are not promised as
published artifacts. Tool Protocol **0.1** is active. `conversation-draft-0.2.0`
and `core-boundary-draft-0.1` remain isolated proposals, not live host endpoints.
Render host-validated typed receipts, never arbitrary model HTML, markdown
interpreted as authority, executable scripts or model-provided navigation URLs.

## Token source and identity

`apps/web/src/components/penta/penta-design.module.css` is the scoped initial
token source. The existing conversation imports its `system` class; no global
ERP theme is changed. `data-penta-ui="0.1"` identifies the applied design revision.
The standalone shell will eventually supply its own theme selector and font
adapter while preserving token semantics. Do not extract a package yet.

| Semantic token | Light | Dark | Meaning |
| --- | --- | --- | --- |
| `--penta-canvas` | `#f4f7fb` | `#07111f` | Quiet application background |
| `--penta-surface` | `#ffffff` | `#0d1a2b` | Conversation / context panel |
| `--penta-surface-muted` | `#edf2f8` | `#111f32` | Human messages / record cards |
| `--penta-input` | `#edf2f8` | `#1a2d43` | Distinct input region |
| `--penta-text` | `#172033` | `#e7edf7` | Primary readable content |
| `--penta-text-muted` | `#5f7088` | `#aebed1` | Secondary information, not disabled text |
| `--penta-border` | `#b9c8da` | `#405774` | Defined control and surface edge |
| `--penta-accent` | `#0f6cbd` | `#48b7ef` | PENTA identity, focus and navigation |
| `--penta-on-accent` | `#ffffff` | `#07111f` | Text on primary action; never white on pale blue |
| `--penta-accent-soft` | `#e5f1fb` | `#10354f` | Selected capability / secondary action |
| `--penta-danger` | `#b42318` | `#fb8e83` | Failure / destructive meaning |
| `--penta-success` | `#16835d` | `#57c99b` | Verified completion, not model health |
| `--penta-warning` | `#ad6a00` | `#f2b84c` | Attention / uncertainty |

Typography inherits the existing locally served application sans-serif font;
no remote font or new dependency. Body 14px/1.6, metadata 12px, record heading
16px, conversation heading 24px. Page title scales 25–36px. Labels remain
readable and never rely only on placeholders. Financial values use tabular
numerals and explicit currency; never total different currencies together.

Spacing scale: 4 / 8 / 12 / 16 / 20 / 24px. Control radius 10px, card 14px,
panel 20px. Controls target at least 44px. Focus is a visible 3px accent outline
with 3px offset. Overlay layer 5 is local to PENTA; global portal menus/dialogs
retain their existing stacking policy. No decorative gradients, shimmer,
glassmorphism or simulated progress is required.

## Conversation and composer

One PENTA assistant, five capability contexts, one coherent conversation.
The global mode switch is **PENTA AI | Workspace**. Selecting Executor focuses
the composer; selecting a capability is not a permission or new model session.
Each capability has a named `!` help button, explicit current-versus-planned
description, close control and Escape focus return. Help anchors to the whole
capability bar so wrapped mobile chips cannot push it beyond the viewport.

Human message: right-aligned muted surface, plain text, preserved line breaks,
long-word wrapping, at most 90% of the canvas. Assistant: left-aligned readable
content, clear PENTA byline, verified outcome or clarification status. Never
call a proposed tool an executed action. Keep context, result, error and next
step distinct. Use existing polite additions/busy announcements without
re-announcing the entire conversation after every keystroke.

Composer: persistent in the conversation flow, visible label, multiline text,
2,000-character current host limit, IME-aware Enter to send / Shift+Enter newline.
Disable duplicate submission while busy. Stop waiting and new-conversation
recovery do not automatically retry; stopping wait does not prove server cancellation.
Retain an unsent prompt after errors; no invisible submission or speculative result.
Private history/recent conversations/streaming are **pending** their server-side
retention and contract gates, not decorative enabled controls.

## Typed result and component boundaries

| UI pattern | Rendering rule | Delivery state |
| --- | --- | --- |
| Conversation / composer | Validated host receipt and local state, not a second planner | Existing integrated pilot, tokens applied |
| Result card | Verified display name, code, status, subject, each currency balance, source, as-of and source link | Existing student read pilot |
| Clarification | Show verified candidate records and explicit choice; no automatic selection | Existing student ambiguity pilot |
| Status / recovery | Text and semantic state; provider readiness is not accuracy or authorization | Existing pilot |
| Result table | Typed columns/cells and stable source keys; sortable only where API supports it | Specified; not implemented in this slice |
| Action preview | Exact operation, targets, before/after, risk, source revision, expiry and approval requirement | Future real action gate |
| Approval panel | Trusted pending action; confirm/reject with fresh host authority, idempotency and audit | Future real action gate |
| Feedback | Minimal tenant-scoped correction categories matching Mini learning-event schema | Existing metadata foundation; customer collection UI pending |
| Task card | Real task status, evidence, next action and unknown-outcome recovery | Future task/inbox gate |

Future component props should accept presentation DTOs and host-supplied link/
action callbacks; they must not import EF models, Academy routes or raw provider
clients. Reuse stable patterns first in this app. Extract a versioned shared
PENTA UI package only after integrated visual/function acceptance and an actual
second consumer justify it; no third repository or uncontrolled component copy.

## Context, manual access and command centre

Desktop: conversation is primary; a 270px collapsible context/manual rail
shows verified academy, current filters/order, tool scope and source authority.
At 1050px the rail moves below chat; at 600px it becomes one column with 16px
page padding. Supported test widths include 320, 390, 768 and 1440px.
No hidden navigation links, clipped prompt, overflowing names or covered controls.
Local tables may scroll horizontally; the whole page must not.

Manual ERP remains independently usable when Mini is offline or disabled.
Invoices, Payments, Students and Classes & batches are discoverable now.
Do not fake a 90% AI ratio by removing manual operations. Owner Command Centre
will use authorized source metrics and attention items, not invented forecasts.
Future contextual pages supply hints, never tenant/role authorization shortcuts.

## Actions, learning and reliable states

Read → verified result. Draft → preview, not send. Write → bound preview plus
required confirmation/current policy. High risk → stricter domain approval or
prohibition. A model approval flag or bare yes is never authority.
Unknown outcomes require reconciliation before another attempt; never report
failure as proof that nothing executed. These future UI patterns do not enable
new tools in the current read-only pilot.

Loading reflects actual pending work; no fabricated percentages or token stream.
Empty means the authorized query returned zero results, not missing permission.
Unavailable/denied/expired/stale/malformed responses have safe explicit recovery
and manual access; never show unvalidated source facts or raw exception details.
Feedback remains separate from private memory and reviewed offline training;
no live weight updates, tenant-crossing learning or automatic customer export.

## Accessibility and evidence gates

Keyboard-operable actions, visible focus, labels, named help/close controls,
Escape return and appropriate live/error announcements are mandatory. Target
4.5:1 normal-text contrast and 3:1 relevant graphical/focus contrast; do not
conflate measured token pairs with a full WCAG conformance audit. Never encode
status by color alone. New animation must honor reduced motion; future dialogs
need focus containment/return and viewport-safe scroll/stacking tests.

Test the actual production export at desktop/tablet/mobile in both themes:
contrast, prompt labels/focus, all capability help bounds/layering/close,
source-backed card identity, safe empty/error, manual switch and no overflow.
Synthetic interception is **UI evidence only**, never Mini/SQL acceptance.
Keep screenshots/results in local `QA/EVIDENCE`, with source/fixture identity.
Physical Android/iOS, screen-reader, full visual regression and accepted visual
baselines remain open; do not claim them from desktop emulation.

The full first feature still needs the unchanged real Mini language gate,
prepared real SQL/security/source journeys and 15-turn browser acceptance.
Saved Mini 86/94 is NOT ACCEPTED. See [outgoing dependency packet](PENTA_HANDOFF_TO_MINI.md).
Only then issue READY TO VIEW with URL, role, steps, exact versions/evidence,
limitations, reusable components and next feature; stop expansion for owner review.
Feature-branch GitHub publication is not main acceptance or Azure deployment.
