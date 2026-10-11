# E3 — Finance Governance document-preview reflow

2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, parent `44bc7d458098e8244c78d1e2b19e0a635616bcf3`.

## Scope and cause

The Governance sample document header used three flex siblings with fixed padding; invoice reference/body total could overflow the paper internally even when page-level overflow passed. Long signatory text and the fixed180px footer worsened narrow geometry. Modern overflow hiding could conceal the problem. This is a preview layout follow-up, not a new finance policy.

Only `.finance-control-documents .document-full-preview` CSS is changed. Desktop uses a shrinkable identity column; at640px and below header/reference/body/total stack and theme spacing contracts. Footer metadata wraps without truncation. Existing invoice/payslip themes and paper colors remain; surrounding Manual Orbit + Aura dark /Prism light tokens are unchanged. Real invoice print layouts, PDF generation, tax/payroll calculations, API requests, permissions, settings-version/audit transactions and database schema are untouched.

## Verification

- New `QA/tools/finance-document-preview-browser.cjs`: real production-export DOM with synthetic read-only transport, not live SQL or physical device proof.
-112 cases:320/390/768/1440px × light/dark × default/long branding × four invoice and three payslip themes. Long unbroken signatory, multiword designation, logo/signature and absent images covered. Every child rectangle and text Range must stay within the paper, sibling header/body regions must not overlap, document/page scroll width must fit. Exact metadata, sample record codes and sample totals retained; no writes or unexpected network/console activity.
- CSS-only counterfactual removes exactly15 repair rules from the identical exported DOM:38 PASS/74 FAIL. This is **not** a frozen full-application build. Original reference/total clipping reproduced with page overflow still false. Same geometry assertions retained.
- First test stopped on an over-exact label locator; changed to source-backed native selects, without relaxing geometry. First counterfactual stopped on incorrect expected rule count14; corrected to actual15, assertions retained. Failed local evidence preserved.
- Screenshot review of initial320px Modern light and Professional dark identified cramped identity wrapping despite geometry PASS; mobile header refined to one column and fresh export/matrix rerun. Final screenshots reviewed separately.
- Production webpack build and integrated TypeScript validation:83/83 static pages PASS.
- Existing actual-handler checks:27/27 PASS. Existing settings conflict browser36/36 and policy theme browser16/16 rerun against final export PASS.
- Final evidence: `QA/EVIDENCE/finance-document-preview-1791727386773/result.json`, `QA/EVIDENCE/finance-settings-conflict-browser-1791727397171/result.json`, `QA/EVIDENCE/finance-policy-theme-browser-1791727407690/result.json`. Counterfactual receipt: `QA/EVIDENCE/finance-document-preview-1791727333882/result.json`. Evidence remains intentionally local.
- Final320px Modern invoice/light and Professional payslip/dark screenshots visually inspected: academy name, reference, total and full long metadata readable without clipping; stress metadata intentionally wraps over many lines. Desktop/tablet geometry automated, not equivalent to exhaustive human/device review.

## Boundaries and continuation

The prior76 API units,37 real SQL controls, five strict settings races and eight linked browser journeys remain evidence in [length repair](PHASE_2B_FINANCE_SETTINGS_LENGTH_REPAIR.md), **not rerun** for CSS-only work. No SQL container created, removed or pruned. No all-portal, physical Android/iOS, accessibility certification, print acceptance, enterprise completion, Mini qualification or90% AI claim.

BUG-DATA-0053 remains OPEN/RETEST for physical/critical/wider permission/release gates. Keep interrupted Mini/Platform work, older32/28 handoff/QA additions and local evidence out of this scoped publication. No main edit/merge, Azure deployment or PENTA Core change.

Next: existing BUG-DATA-0054 zero learning-progress score round-trip reproduction; Sol Medium for settled absence-vs-zero serialization, Sol High if domain/authority/SQL decisions arise. Manual shared rollout/FW1, Mini qualification and existing enterprise gates continue, not a replacement testing plan.
