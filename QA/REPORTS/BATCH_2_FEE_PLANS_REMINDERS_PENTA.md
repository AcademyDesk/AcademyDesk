# Batch2 — fee plans and reminders

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. Named two-screen delivery checkpoint; not completion of all finance, enterprise testing or the90% conversational target.

## Implemented

- Both pages consume the shared scoped PENTA finance styles:responsive cards/form/register,44px actions,persistent field labels,wrapping action groups and matching Prism/Orbit themes. Fee-plan create and inline editor selectors use valid labelled shared controls rather than button controls nested in a label. Empty cycle selection cannot clear a required existing frequency. Fee plan original money formatter,frequencies,stats,min1/step0.01 and POST/PUT contracts retained.
- Page-lifetime ready/pending/ref/review guards cover fee-plan creation/update/deactivation and reminder requests. No same-tick repeated write or refresh overlap;visible controls disable while busy/review. Failed initial/list parsing does not authorize entry or display a successfully loaded empty register. Confirmed saves retain success even if readback fails;unconfirmed500/408/network responses retain drafts/block retries until deliberate successful read recovery. No automatic POST/PUT retry. Malformed accepted reminder receipts cannot be called queued success.
- Reminder UI now uses the existing invoice API's canonical `balance`,not local gross minus Completed-only payments. This fixes reconciled-payment/applied-adjustment overstatement and hides settled invoices. Different currency totals remain separate;single invoice amounts use their currency. Removed unnecessary payment-ledger fetch,not the existing Students permission requirement. Existing backend policy,scopes,transactions and requests unchanged.
- Queue labels describe queueing,not delivery. Bulk action accurately says Queue due reminders and explains server due-date rules;`invoiceId:null,channel:InApp` and per-invoice contracts retained. Queue receipt is preserved through refresh;zero queued is not called delivery. History lists actual stored status/channel with separate spacing,not a fabricated sent status.

## Verification

- Final webpack production build/TypeScript PASS,86 static outputs. Scoped changed-page lint0errors/0warnings. No app-source edits after the final build.
-152 node:test entries PASS:52 new fee-plan/reminder cases,retained invoice/payment/lookup/template/money checks and33 unchanged-declaration comparisons (three new plan formatter/frequency/stats comparisons). The retained money script has16 internal assertions. Existing assertion families preserved;mock/fixtures extended,not financial expectations weakened.
- Original frozen HEAD reminder arithmetic reproduced1000 instead of the canonical200 for gross1000/adjusted200/reconciled600. A separate test explicitly retains that counterfactual failure;new actual TSX tests require200,exclude settled records,separate USD0.01,fail closed on missing balance and preserve all queue payloads. Initial characterization attempted to evaluate TS generics as JS;fixture corrected with transpilation,not a product change.
- Fresh targeted backend build/tests PASS13/13:FinanceConsumerTests and ReconciledBalanceTests,including Completed/Reconciled/Voided,adjusted/fully settled reminders and unchanged collection behavior. These are InMemory/controller tests,not real SQL/HTTP/tenant-concurrency acceptance.
- Final production-export browsers PASS228 cases:84 fee-plan/reminder (2screens×3widths×2themes×7states),54 retained invoice/payment,36 finance workspace/summary and54 expense regressions. Receipts:`QA/EVIDENCE/fee-plan-reminder-1792038017120/result.json`, `QA/EVIDENCE/invoice-payment-visual-1792038017135/result.json`, `QA/EVIDENCE/finance-workspace-visual-1792038017137/result.json`, `QA/EVIDENCE/expenses-feedback-browser-1792038017136/result.json`. Available source hashes match final files. Final light fee-plan/dark reminder phone images inspected,including separated history channel/status and padded notice. Synthetic APIs only;never real customer sends,financial writes or Identity/SQL acceptance.

## Findings retained

Browser checks exposed a refresh-button hover movement preventing a second recovery click;scoped top-level recovery buttons now stay fixed rather than translating on hover. Normal click checks retained—no force-click bypass. A queue receipt appears before asynchronous history readback;the browser test now waits for the actual queued-history record instead of asserting before the read finishes. The assertion itself remains. Screenshot review found history channel/status concatenation and cramped notice;scoped flex gap/message padding repaired with additive computed-style assertions. Earlier failed/intermediate evidence remains local.

The UI does not implement durable server/cross-tab/reload idempotency,duplicate-notification reconciliation,new bulk approval policy or delivery guarantees. Existing reminder due-date filter semantics are preserved;general academy-timezone policy remains a separate known gate. No financial/privacy/security issue is closed by these frontend tests. Existing real SQL/race/authorization/audit/device/communications approvals and exact-candidate release gates remain OPEN.

## Next

Continue remaining finance visuals in coherent batches:reconciliation,adjustments,policy/governance/payroll,then academics/communications and full Platform/Teacher/Student state parity. Medium for settled presentation;High for consequential financial/domain/security/integration/release changes. Existing PENTA/Mini/UI2 handoffs and qualification gates remain unchanged.

Owner order preserved:complete agreed PENTA+Manual app and quality gates→authorized go-live→Hayansh Academy onboarding→Academy Desk public product website after the first live customer. No website work or reduced-scope launch substituted for app completion.

No backend/schema/SQL-data/permissions/Mini/vendor/main/stage/commit/push/Azure/container change. Prior unrelated work and QA evidence preserved. Main stays `f7af512f884ca5fe8ac3ddb287c852492592e709`.
