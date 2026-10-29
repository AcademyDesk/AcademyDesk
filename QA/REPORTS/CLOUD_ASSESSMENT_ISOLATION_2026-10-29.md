# Assessment selection/save isolation — 29 October 2026 IST

BUG-DATA-0018 receives another partial repair on `codex/release-export-security`. Existing guardian/student/teacher/fee and communication slices remain intact.

Assessment results now belong to one academy/assessment load generation. Changing selection immediately retires prior callbacks and hides old results. Pending, failed, malformed, null-row, missing-score and duplicate-learner responses cannot unlock a blank or stale editor. Failed reads offer an explicit retry. Saves require a current successful load and an active roster member; same-tick duplicates are suppressed. A late save or readback cannot update the selected results, notices or pending indicators after a selection change, A→B→A cycle or unmount. All rows lock while a result saves, with progress text limited to the actual saved learner. Existing manual/automatic grading, zero scores, remarks and server validation remain.

## Observed validation

- Original9 isolation cases:8 FAIL/1 PASS. The already passing reversed-read case remains covered.
- Final44 assessment checks PASS:14 isolation/shape/generation cases, all29 existing grading/options cases, and1 additional other-row lock control.
- The original page-level grading tests now execute through the effect-aware controlled TSX harness; their payload, confirmation, rejection and readback expectations remain. No earlier cases were discarded.
- Full frontend:50 files/1,138 tests;1,135 PASS/3 FAIL/0 skip/cancel. The same3 historical communication SQL-evidence cases lack their ignored real SQL log locally. No synthetic replacement log was fabricated.
- TypeScript and production webpack build PASS. Changed-page lint0 errors/1 inherited workspace-load effect warning. Diff check PASS. Prior API1,179 PASS retained, not rerun for this UI-only change.

This is controlled TSX/transport proof, not real browser/HTTP/SQL acceptance. Server-side concurrent edits, authenticated browser/device acceptance, attendance/remaining broad BUG-DATA-0018 slices and critical/release gates remain OPEN. No API/schema/policy/customer-data change, main merge, deployment, environment publication or PentaMini change. Next route:Codex/Sol High inspect expanded security/recovery candidate CI, then continue remaining critical form and workflow acceptance.
