# C24 invoice document and required-read recovery — 9 November 2026

Cloud owner. CLAIMED / source reproduction next.
Branch `codex/cloud-c24-invoice-document-readback-20261109`.
Clean base `5fbc75f93c0dc9f0e18bb59a9da4a68811708e9c` (completed C23).
Latest fetched Desktop `9c9658132446124b70c53467e940d9769cf97d44` is inherited;
its protected writes, production guard, SQL and release-container work stay intact.
No newer remote claim or overlapping invoice-page claim found in current notes.

MODEL: Sol High for money/readback/recovery; Medium for settled docs.
TOOL: Codex / managed Cloud.
TASK: fix the administrator invoice preview/print consumer of canonical API
balance, retain billed/approved-adjustment/accepted-collection history and
cancellation meaning, and prevent failed/incomplete initial reads from presenting
ready zero financial totals or an editable invoice intake. Preserve invoice
creation payload, permissions, module/tenant gates, tax/theme/branding/contact,
and existing financial pending/uncertain-save controls.

Exact owned mutation paths:

- `apps/web/src/app/invoices/page.tsx`
- `QA/tools/invoice-document.test.cjs`
- `QA/tools/invoice-document-browser.cjs`
- `QA/ISSUES/BUG-DATA-0001.md` — append scoped consumer result, retain local evidence
- `QA/ISSUES/BUG-DATA-0002.md` — append, no closure or guard change
- `QA/ISSUES/BUG-DATA-0016.md` — append missing/legacy money metadata result
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json` — regenerate derived snapshot
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md` — regenerate derived snapshot
- `QA/HANDOFF/C24_INVOICE_DOCUMENT_READBACK_2026-11-09.md`

The current page ignores InvoiceSummary.balance and prints totalAmount as Total
payable. Canonical balance must be used directly; missing/nonfinite balance must
be unavailable, never guessed from stale status or arithmetic in the browser.
Cancelled documents retain history and clearly state zero collectible amount.
Existing total billed scope remains face-value history. Currency metadata must
retain valid units/cents; empty/missing/invalid input must not become guessed INR,
crash the page or label a failed read as a genuine empty register.

Validate before/after actual TSX, retained invoice settings/Finance lookups and
financial transitions, exact-lock build/types/lint/full frontend and actual
sandboxed Chromium at mobile/desktop with synthetic APIs. Include print media/
PDF bytes and all four configured themes, no live financial writes. Reuse C23's
full API result only if every relevant application/test/assembly hash is unchanged;
otherwise rebuild and retest. Actual Identity/SQL/provider/device/critical/release
acceptance stays Desktop-owned. Keep all five P0s and broad-session gate OPEN.
Do not alter API/SQL/migrations, shared CSS/vendor files, Desktop claims, model
loading stop, environment privacy/networking or main/Azure. Mini/Plus/Pro remain
unqualified; Ultra deferred. No new dependencies, raw evidence publication or
external audit. Publish source-bound feature commits and an exact Desktop return.
