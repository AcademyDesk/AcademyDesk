# Batch2 — invoice/payment presentation checkpoint

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. This completes the named presentation slice, not all finance workflows or enterprise release acceptance.

## Implemented

- `/invoices` and `/payments` consume the scoped PENTA finance layout: semantic H1, responsive KPI/form/register grids, persistent field labels, themed shared selectors, and44px actions. Payments disables visible editing controls during its existing saving state. The optional payment-method placeholder cannot erase the existing method.
- Invoice student/plan selectors use the existing anchored top-layer control. Optional due date uses the shared year/month picker and Clear. Invoice register retains every column in a named keyboard-focusable horizontal scroll region.
- Screen-only invoice-preview rules contain Classic/Modern/Minimal/Formal templates on phones/tablets. Existing white document branding, tax/authority/contact, print handler, print stylesheet and four server-owned ledger figures remain intact. Screen tests are not PDF/physical printer acceptance.
- No changes to create/load handlers, money functions, canonical balance projections, payload serialization, API paths, backend transactions, roles or tenant checks.34 AST checks compare financial declarations against HEAD (including16 invoice/payment declarations). Preservation is not proof that inherited handlers are release-ready.

## Verification

- Final webpack build and TypeScript PASS,86 static outputs. No application-source edits after this build.
-58 node:test entries PASS, including the existing money-display script with16 internal assertions. Retained reconciled/completed versus voided collection, server adjustment/balance, fully adjusted exclusion, seven-field document settings, billing contacts, four templates and denied/network lookup assertions. Updated mocked imports and selector assertions for actual shared controls; no monetary or authorization assertion removed. Initial pre-change invoice fixture failures were missing the already-present ledger import; fixtures now model that dependency.
-54 final-export synthetic browser cases PASS:320/768/1440 × light/dark × six invoice states (four templates, empty, denied) plus three payment states (populated, empty, denied). Actual year/leap-date selection/Clear, menu top-layer/hit testing/anchor/viewport/focus/Escape/collapse, persistent labels,44px buttons, keyboard register scroll, invoice preview/close, authoritative four ledger values, canonical400 balance, BankTransfer payload, optional nulls, confirmed success/reset/readback and no protected student/governance lookup verified. Receipt `QA/EVIDENCE/invoice-payment-visual-1792035739120/result.json` includes final source hashes.
- Shared stylesheet regressions:36 workspace/summary final-export cases PASS, receipt `QA/EVIDENCE/finance-workspace-visual-1792035750799/result.json`;54 expense cases PASS, receipt `QA/EVIDENCE/expenses-feedback-browser-1792035760632/result.json`.144 final browser cases total. Synthetic APIs only, never production payments or Identity/SQL acceptance.
- Scoped invoice/payment ESLint:0 errors,4 warnings. Same four diagnostics reproduced from HEAD via stdin:two existing effect dependencies and two invoice branding images. No warning suppression; whole-app lint gate remains OPEN.
- Six pinned vendor hashes unchanged; script parse and scoped whitespace checks PASS. Final Modern/light phone preview and dark payment-success screenshots visually inspected.

## Findings and remaining work

The new phone test first exposed a global600px table minimum, then tablet tests exposed Modern/Minimal table width plus fixed margins. Scoped screen-only minimum/width/margin rules fixed these; failing assertions/evidence were retained, not weakened. Preview table cells may wrap on narrow screens; ledger monetary totals retain their full cents. Register screenshots after scroll intentionally show right-hand columns, not missing default columns.

**Next financial-safety task, before accepting these workflows for release:** invoice create lacks a pending/ref guard and catch/recovery; payment pending uses state only, and uncertain outcomes warn but do not require explicit read/review before another attempt. Audit and implement bounded same-tick duplicate prevention, honest confirmed-write/readback failure handling and explicit uncertain-outcome recovery against the existing server transaction contract. Preserve excess-payment400 wording, reconciled behavior, decimal contracts and real SQL race tests. These findings were deliberately not hidden in a styling change. Do not close existing financial issues from synthetic visual evidence.

Then resume fee-plan/reminder and remaining finance visuals, academics/communications and Platform/Teacher/Student full-state parity. Settled visual work routes to Sol Medium; consequential financial safeguards route to Sol High per repository policy. Existing E4–E10, Mini/UI2 qualification, live SQL/security/physical-device and production gates remain OPEN.90% AI remains the product target, not measured completion.

No staging/commit/push/main/Azure/productionDB/container/schema/Mini/vendor changes. Prior unrelated dirty work and local evidence preserved; main remains `f7af512f884ca5fe8ac3ddb287c852492592e709`.
