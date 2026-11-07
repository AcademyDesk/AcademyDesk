# C14 fee-reminder eligibility and balance — 7 November 2026

Cloud owner. Status: CLAIMED; implementation/tests pending.
Branch `codex/cloud-c14-fee-reminders-20261107`.
Clean base `508c9bb34c68364c85be2f8ac23f163505a120d9` includes C12/C13.
Latest inspected Desktop `b474042` imports C12; P0 packet at `ad54028` complete.
No active competing fee-reminder source claim found in these handoffs.

Exact paths:

- `apps/api/Controllers/FeeRemindersController.cs`
- `apps/web/src/app/fee-reminders/page.tsx`
- `tests/AcademyDesk.Api.Tests/FeeReminderEligibilityTests.cs`
- `QA/tools/fee-reminder-eligibility.test.cjs`
- `QA/tools/fee-reminder-eligibility-browser.cjs`
- `QA/ISSUES/BUG-DATA-0019.md`
- `QA/HANDOFF/C14_FEE_REMINDERS_2026-11-07.md`

Reproduce cancelled reminders at bulk/targeted entry points. Exclude cancelled
invoices in the API; preserve existing positive net, adjustments, Completed/
Reconciled, due-date and tenant rules. Make the screen use the existing invoice
API's authoritative Balance, exclude cancellations and display each currency
separately. This advances the linked BUG-DATA-0001/0002 consumer paths, not their
full P0 closure. Preserve current queue channel/retry/audit policy; no provider send.

Validate baseline/final controlled TSX, API contract tests and retained finance
regressions, actual Chromium with clearly synthetic replies, exact-lock build/
types/lint and full API/frontend suites once after final source. Reuse C13 backend
evidence until the API source changes. No dependency/image downloads, SQL host,
models, main/Azure/environment publication. Return owned SQL/HTTP acceptance to
Desktop; preserve its shared handoff and P0 record ownership. High for financial
rules/SQL; Medium for settled read-display/import work.
