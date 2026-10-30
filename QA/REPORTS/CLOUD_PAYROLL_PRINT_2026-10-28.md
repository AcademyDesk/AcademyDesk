# Saved payroll printing — 2026-10-28

BUG-FUNC-0033 remains OPEN with a bounded print repair on `codex/release-export-security`.

Each register-row action now fetches current saved payouts with `no-store`, matches the selected ID, validates the returned document fields, and commits exactly one payslip before opening the browser print dialog. Failed, missing, malformed or uncertain reads cannot print a stale document. Duplicate pending actions are blocked, unmounts retire pending work, and dialog/read failures release retry controls. Printing never creates a payout.

The document contains stored worker name/type, payslip number, period, date, session count/cycle, gross, deductions, net, currency, status, payment method and reference. Currency formatting preserves two decimal places, including positive-cent net payouts. Print-only layout rules hide the register and shell controls and release the shell's scrolling/height constraints without changing other screens.

## Executed evidence

- Initial10 controlled actual-TSX print cases:10 failed. Final11 print cases PASS, including the added empty-register control.
- Existing23 form-target cases PASS alongside the print suite:34/34 total.
- Real Chrome156.0.8078.73 with Playwright Core1.64.1:1280x900 and390x844 viewports; two selected payout rows each;4 generated PDFs PASS.
- First real-PDF run caught clipping after worker/type because the application shell retained its print overflow/height constraints. Original failed PDFs/log retained at `.build-check/payroll-print-browser-before/`. Corrected final PDFs at `.build-check/payroll-print-browser/` contain complete fields, exact INR amounts and the USD0.01 net case, each on one A4 page. Other workers, register and application UI are excluded; horizontal content fits.
- Browser checks exercise failed/missing fresh reads, pending-load lock, retry and row selection. No page errors or API writes; intercepted synthetic transport refuses account credentials and blocks other network destinations.
- Final full frontend:49 files,1,123 tests;1,120 PASS/3 FAIL/0 skip/cancel. The same3 communication cases fail because ignored historical real-SQL evidence is absent. No evidence was fabricated.
- TypeScript PASS; webpack static build PASS; changed-page lint0 errors/1 inherited load-effect warning; actionlint and diff checks PASS. Earlier1,179 API PASS retained, not rerun for this UI-only change.

## CI and reproduction

Verification builds a loopback-configured static export, installs pinned Playwright Core1.64.1 in ignored tooling, uses the runner's Chrome and Poppler utilities, runs the browser/PDF script and preserves generated synthetic artifacts. Deployment separately rebuilds using `PRODUCTION_API_URL`; it does not deploy the verification export. These workflow definitions remain development-branch changes until reviewed merge.

To reproduce on a machine with Chrome, Node, `pdftotext` and `/usr/bin/pdfinfo`:install Playwright Core1.64.1 in isolated tooling; build `apps/web` with `NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack`; run `QA/tools/payroll-print-browser.cjs` with `QA_PLAYWRIGHT_MODULE` pointing to that tool's module and `QA_BROWSER_EXECUTABLE` pointing to Chrome. The script owns an ephemeral loopback static server and closes its browser/server. No normal API or configured database starts.

This is real DOM/Chromium/PDF evidence with synthetic responses, not real Identity/SQL or bank settlement proof. Authenticated end-to-end payroll, other print engines, long/unusual content and wider device coverage, critical P0/P1 acceptance and staging/backup/restore gates remain OPEN. No configured DB migration, main merge, environment publication or Azure deployment occurred. Next route:Codex/Sol High for actual candidate CI and remaining critical release acceptance.
