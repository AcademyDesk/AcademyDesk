# C27 Sales trial access and lead lifecycle integrity — 9 November 2026

Cloud owner. CLAIMED / reproduce next.
Branch `codex/cloud-c27-sales-trial-integrity-20261109`.
Clean base `abb57f0063398dc71715a116f569b1656d88b563` (complete C26).
Latest Desktope460dbc86d5e02c4cdf7ede6c330ab74f089c65d is inherited exactly.
No competing Leads/SalesMarketing/trial-page claim found in current notes;
Desktop retains protected enrollment/host/SQL/migration ownership.

MODEL: Sol High for domain/state/transaction/authority; Medium for settled docs.
TASK: resolve DATA-0026/0027 and FUNC-0013 source defects: direct stage selection
must not manufacture conversion; trials must not regress converted leads;
Sales-authorized booking must not need teacher-management access. Preserve
legitimate unconverted/Won/Lost trials and existing optional teacher/batch policy.

Exact mutation paths:

- `apps/api/Controllers/LeadsController.cs` — stage/conversion lead boundary only
- `apps/api/Controllers/SalesMarketingController.cs` — minimal teacher options, trial creation/status audit boundary
- `apps/api/Infrastructure/LeadMutationBoundary.cs` — scoped SQL lead lock under existing atomic/audit transaction
- `tests/AcademyDesk.Api.Tests/SalesTrialIntegrityTests.cs`
- `apps/web/src/app/trial-bookings/page.tsx`
- `apps/web/src/app/leads/page.tsx` — remove invalid Converted stage choice; preserve actual Convert action
- `apps/web/src/lib/trial-booking-clock.ts` — explicit IST wall time consistent with existing displayed IST
- `QA/tools/sales-trial.test.cjs`
- `QA/tools/sales-trial-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — execute actual clock helper for controlled tests
- `QA/ISSUES/BUG-DATA-0026.md`
- `QA/ISSUES/BUG-DATA-0027.md`
- `QA/ISSUES/BUG-FUNC-0013.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C27_SALES_TRIAL_INTEGRITY_2026-11-09.md`

Reuse the existing AtomicAcademyMutation boundary for stage/convert/trial writes,
including Platform Owner. SQL Server writers lock the same owned lead before
reading conversion state; no new global auth/filter or migration. Actual SQL
race/audit rollback proof remains Desktop-owned. Converted student links are
terminal for trial/stage changes. Legacy Converted-without-link can still use
the existing explicit Convert action to create its actual student; do not invent
or rewrite historical links. New teacher options expose only owned id/name fields,
without granting teacher management or changing existing write authorization.

The trial UI must require complete current reads, preserve history names while
excluding converted leads from new booking, and guard overlapping/stale or
unconfirmed create/status submissions. Read-only recovery must never re-POST;
unknown create needs successful review plus explicit new intent. Retain existing
nullable teacher/notes and status values. Serialize the visible IST date/time
independently of browser zone, validate before toISOString, and label that zone.
Keep Lead Convert distinct from a stage update and preserve unrelated lead UI.

Validate original/final actual HTTP/Identity/InMemory, TSX and real sandboxed
Chromium with synthetic persistence/faults, multiple browser zones, exact-lock
build/types/lint/full suites and unchanged production guard. No assertion skips,
fake SQL receipts, new packages/provider sends or customer writes. Preserve all
older accepted evidence. Publish source-bound feature commits and exact Desktop
SQL/race/device tasks. All five P0s/broad-session/release gates remain open;
Mini/Plus/Pro unqualified, Ultra deferred, stop before model loading/inference/
training/activation. No main/Azure/environment/public-access changes.
