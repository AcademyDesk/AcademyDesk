# C25 reminder consent, Finance lookup and queue recovery — 9 November 2026

Cloud owner. CLAIMED / reproduce next.
Branch `codex/cloud-c25-reminder-queue-safety-20261109`.
Base `8479833` retains complete C24 at2114c8c and latest Desktop C23 SQL return
6f77a94c528b6e6df0301b98c5ed75dcc76c6581 with cherry-pick provenance. The only
conflicts were derived inventory and two issue appends: verified Desktop issue
text was an exact prefix of the retained C24 text, then regenerated the inventory.
All C23 product paths already matched. Five new Desktop QA/handoff/SQL-harness
paths were retained. No competing reminder page/channel-guard claim found.

MODEL: Sol High for notification policy/authority/recovery; Medium for docs.
TASK: prevent unsupported reminder channels from bypassing communication policy;
use the existing Finance-authorized student-options lookup; finish honest read
and queue recovery while retaining tenant/currency/cancellation/audit contracts.

Exact mutation paths:

- `apps/api/Controllers/FeeRemindersController.cs` — channel guard/canonical InApp only; money/notification/audit rules preserved
- `tests/AcademyDesk.Api.Tests/FeeReminderChannelTests.cs`
- `apps/web/src/app/fee-reminders/page.tsx`
- `QA/tools/reminder-queue.test.cjs`
- `QA/tools/reminder-queue-browser.cjs`
- `QA/tools/fee-reminder-eligibility.test.cjs` — real lookup/confirmation fixture contract and normalized legacy-currency label; retain eligibility assertions
- `QA/ISSUES/BUG-SEC-0008.md`
- `QA/ISSUES/BUG-FUNC-0006.md`
- `QA/ISSUES/BUG-FUNC-0003.md`
- `QA/ISSUES/BUG-DATA-0019.md`
- `QA/ISSUES/BUG-DATA-0016.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/AI_QUALIFICATION_ENTRY_GATE_2026-11-09.md`
- `QA/HANDOFF/C25_REMINDER_QUEUE_SAFETY_2026-11-09.md`

The UI already sends InApp. Preserve null→InApp compatibility and normalize
trimmed case-insensitive InApp; explicitly reject blank/unknown/external channels
before any notification/audit mutation. This follows SEC-0008's allowed remedy
of rejecting unsupported channels rather than inventing provider integration.
Do not change Notifications.Create, provider consent semantics or external sends.

Required initial reads/denials/invalid roots must not become successful zero
balances/history or enabled intake. Use invoices/student-options rather than
student management. Keep actual currency/cents and cancellation eligibility.
Prevent double/stale queue callbacks synchronously. Confirmed queue stays queued
if readback fails; transport/5xx/malformed confirmation is explicitly unconfirmed
and locked against replay. Recovery reads never POST; a new queue after an
unknown result needs successful reload plus explicit new-intent action. Preserve
the existing bulk null-ID/targeted ID/InApp payload and server eligibility rules.

Validate original/final HTTP/Identity/InMemory channel/role/tenant/audit cases,
actual TSX/retained eligibility, sandboxed Chromium with synthetic persistence/
faults, exact-lock build/types/lint/full suites and unchanged production guard.
No fabricated SQL receipts or skipped assertions. Real SQL/provider/device/
critical acceptance remains Desktop-owned. Publish exact feature source/results.
Record a concrete AI entry checklist because the owner asks to be notified when
AI is next: do not imply non-AI acceptance is complete, and keep the explicit
stop before candidate loading/training/inference/activation. Mini/Plus/Pro remain
unqualified; Ultra deferred. No protected financial writers/migrations, shared
governance overwrites, environment/public-access changes, main merge or Azure.
