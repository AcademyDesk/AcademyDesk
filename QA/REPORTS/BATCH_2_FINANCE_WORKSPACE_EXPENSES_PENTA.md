# Batch2 — finance workspace, summary and expenses

2026-10-14. `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Complete this named presentation slice through final tests; this is not completion of every finance screen, enterprise testing or production readiness.

## Implemented

- `/finance`: semantic H1; scoped PENTA cards/work-area links, wrapping responsive grids and44px actions. The collections table is a named keyboard-focusable horizontal-scroll region, preserving all columns/values instead of squeezing or hiding them on phones. Past-due text uses readable theme tokens. A failed/unavailable load no longer claims that no overdue invoices exist; valid empty data still does.
- `/finance-summary`: responsive PENTA card spacing/radius/type and live status. Existing nine projections, formatting, net billable/rate/reconciliation arithmetic and underlying requests unchanged.
- `/expenses`: same scoped PENTA visual family, phone intake/register layout, clear persistent category/branch/filter labels without invalid label-wrapped button controls, explicitly optional date and pending/review-disabled date trigger. Separate currency totals/large exact-money readout wrap legibly; Refresh has proper separation. Existing decimal lexeme serialization, draft capture, same-tick pending, branch403 fallback and uncertain-save explicit-review requirement unchanged.
- New host CSS module only; shared vendor source and global styles untouched. Existing server authorization/approval/transaction/finance policies are not replaced by UI checks.

## Verification

- Final webpack production build, TypeScript and86 outputs PASS; no app-source edits afterward.86 outputs do not mean86 accepted screens.
- Scoped three-page ESLint0 errors/0 warnings. Whole-app inherited lint debt remains an open release gate.
-85 focused checks PASS:24 exact-money helpers,23 expense handler checks (including optional date/reset and explicit review-disable),18 AST preservation checks and20 existing workspace/routing/census checks. AST checks compare accepted HEAD vs current monetary projections/export/create/refresh/loading/filter/blocked declarations: no calculation or handler changes. This proves source preservation, not previously unproved financial correctness.
- Final finance workspace/summary browser36/36 PASS:320/768/1440 × light/dark × populated/empty/denied. Exact summary projections/0.01 collection, complete table keyboard scroll,44px targets, actual overdue contrast>=4.5, unchanged read-only API paths and twelve synthetic invoice/payment CSV downloads verified by filename and bytes. Denied responses do not become empty/zero finance. Receipt `QA/EVIDENCE/finance-workspace-visual-1791999383080/result.json` includes final page/CSS source hashes.
- Final expense regression54/54 PASS: `QA/EVIDENCE/expenses-feedback-browser-1791999383075/result.json`. Existing scenarios cover same viewports/themes × success/failed refresh/rejection/500-committed uncertainty/network/initial failure/pending/branch denial/maximum exact money. Assertions include original request/null contract, exact9999999999999999.99 JSON lexeme/display, separate currencies, no duplicate retries, retained uncertain drafts and explicit read recovery. Added persistent native labels/settled44px actions; no previous assertion removed. **90 final-export synthetic browser cases total**, separate from85 focused checks; all available receipt source hashes rechecked against current files.

Two final phone images inspected: light finance after real synthetic CSV download and dark expense maximum-money case. Collections screenshot is intentionally at the far-right after the horizontal scroll test, not a missing-column default. Huge recorded money/cents and separate currency totals remain visible. Six vendor hashes and scoped whitespace/script parsing checks PASS.

## Diagnosis/finality

The first new finance touch-target check failed: two primary links41px and two supporting links34px due to legacy CSS. Repaired locally with stronger44px rule, preserved assertion and added failing-element diagnostics. Screenshot review then exposed pale overdue text and awkward large-total wrapping; scoped semantic color/phone numeric sizing fixed and contrast assertion added. Rebuilt and reran both suites on the final unchanged export; earlier failed/intermediate receipts retained and not counted as final. No assertions weakened or old financial suites discarded.

This is synthetic browser/download evidence, not real SQL/Identity/RBAC/device, financial reconciliation, tax/accounting compliance or enterprise release acceptance. Existing exact-money and uncertainty rules are retained, not new accounting-policy approval. Other finance pages (invoices/payments/reconciliation/policy/adjustments/governance/fee plans/reminders/payroll) and wider portal states remain pending at their respective evidence level. Production gates and PENTA UI2/Mini qualification remain open.

No API/backend/schema/permissions/SQL/production data/container/Mini/main edits this slice; prior dirty finance/payroll/media work preserved. No stage/commit/push/Azure. Main unchanged `f7af512f884ca5fe8ac3ddb287c852492592e709`, index empty.

Next coherent E3 work: invoice/payment/fee-plan/reminder visual controls and remaining finance screens, then academics/communications and full Platform/Teacher/Student parity. Medium for settled layouts; High for any discovered authority/finance/transaction/security or release integration. Existing enterprise program continues; do not start a new audit or silently enable PENTA mutations to reach the90% target.
