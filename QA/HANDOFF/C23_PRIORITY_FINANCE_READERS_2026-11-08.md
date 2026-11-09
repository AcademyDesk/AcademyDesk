# C23 priority reconciliation and remaining financial readers — 8 November 2026

Cloud owner. IMPLEMENTED / TARGETED CHECKPOINT; full verification pending.
Branch `codex/cloud-c23-priority-finance-readers-20261108`.
Fresh combined base `b6793d45788b74d8cd5ab82e246c483da8df4aec`, clean before
changes. The old Cloud C22 branch and a pre-switch Git bundle are preserved.
Desktop has integrated C22, passed2,196 combined API and17 expense HTTP/SQL
checks, and six live recovery phases. Its protected writer, payroll, private
recording, audit/SQL and service-restoration work is retained byte-for-byte.
No competing FeeReminders/Portal source claim found in current ownership notes.

Owner now requests all priority issues. Full register has120 non-closed records:
five P0,102 P1 (including the P1 with an explicit P0 session gate),13 P2. This is
not120 unimplemented bugs. Existing scoped fixes and SQL/browser evidence must
be reconciled with the formal original/regression/module/critical/documented
acceptance chain, not discarded or auto-closed.

Exact paths:

- `apps/api/Controllers/FeeRemindersController.cs` — receipt calculation only
- `apps/api/Controllers/PortalController.cs` — invoice projection/download only
- `apps/api/ReadModels/InvoiceReceiptReadModel.cs`
- `apps/web/src/app/portal/page.tsx` — finance totals/history and permitted view only
- `tests/AcademyDesk.Api.Tests/FamilyReminderCurrencyTests.cs`
- `QA/tools/family-finance.test.cjs`
- `QA/tools/family-finance-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — expose actual private View/filter for QA
- `QA/tools/priority-issue-status.cjs`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/ISSUES/INDEX.md` — restore two existing omitted authentication entries; no priority/status changes
- `QA/ISSUES/BUG-DATA-0001.md` — append this source-bound scope, retain local evidence
- `QA/ISSUES/BUG-DATA-0016.md` — append, do not replace newer local records
- `QA/HANDOFF/C23_PRIORITY_FINANCE_READERS_2026-11-08.md`

P0-related readers still group all units/academies by invoice ID. Only accepted,
same-tenant, same-invoice, normalized matching-currency receipts may reduce the
invoice. Reuse one bounded read helper for reminder/portal/download agreement.
Preserve raw rows, adjustments, per-invoice clamp, invoice history and existing
reminder target/due/cancellation/channel/audit/transaction rules. No provider
send, financial writer, role/tenant/parent/finance/document authority change.
Portal fee summary must retain units/cents, exclude cancelled receivables and
not call cancelled invoices Paid; missing metadata is unavailable, no FX/INR
guess. Respect the existing parent finance permission; expose its current server-derived
canViewFinance capability as an additive response flag so a fresh denial cannot
be mistaken for a zero balance after an older parent-profile snapshot. Downloaded HTML must
encode raw currency and label cancellation history versus collectible amount.

Add a deterministic read-only inventory of every issue's actual recorded status,
priority, source/evidence links and quoted remaining acceptance. Historical
metadata conflicts must remain visible; no parsed mention of PASS implies full
verification. No automatic status/priority change, skip, exception or closure.

Validate before/after actual HTTP/Identity/InMemory and current linked student/
guardian/permission/download/queue cases, actual TSX and real Chromium with
synthetic API data, combined build/types/lint/full suites. Actual SQL/provider/
critical/physical-device/release CI/images/staging evidence remains explicitly
required and Desktop-owned. Keep five P0s and applicable issue records OPEN.
Stop before model loading/training/inference/activation; Mini/Plus/Pro unqualified,
Ultra deferred. No main/Azure/environment/private/public-access changes, customer
writes, raw evidence publication, new dependencies/images or external audit.
Sol High for money/privacy/authority; Medium for settled inventory/docs.

## Recovery checkpoint — 9 November 2026

Recovered the same workspace after executor startup/capability failures. Before
interruption, original38 API cases had13 PASS/25 FAIL; final target95/95 and
18 actual-TSX checks passed. These receipts and all working files survived.
Native browser, complete suites and final publication were still pending.

Latest Desktop9c9658132446124b70c53467e940d9769cf97d44 adds production runtime
evidence and a stricter broad-session release gate. It was imported exactly
with cherry-pick provenance, preserving all nine files without new edits. This
retains the five P0 records plus required session acceptance; it does not close
them. C23 still owns only the explicit scoped changes listed above.

Inventory found existing BUG-AUTH-REFRESH-001 and BUG-AUTH-REVOCATION-001 files
missing from INDEX. Before editing INDEX, its claim is added here. Restore their
visibility with recorded bounded evidence and preserve the revocation record's
P1 priority plus its separate P0 session gate. No issue acceptance is inferred.
