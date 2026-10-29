# Reconciliation and adjustment save recovery

2026-10-15. `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. Successor to the explicit functional findings in the four-screen finance-controls checkpoint. No new enterprise audit or issue closure.

## Implemented

- Replaced swallowed register-loading failures with checked HTTP/array parsing and atomic list hydration. The initial workspace must load successfully before any write;failed loads no longer masquerade as empty ledgers/history. Both screens have an explicit read-only refresh action.
- Stable ref ready/pending/review checks block same-tick old callbacks,parallel write/refresh and further attempts during uncertain-outcome review. Pending covers both the write and register readback. Native inputs/shared selectors and reconcile selection/cancel disable consistently. A failed manual refresh also blocks stale writes until a later successful read.
- Reconciliation captures the selected eligible payment and trimmed reference once. Reconciled/Voided/missing payments or blank evidence cannot initiate a request. The server still independently checks authority/current status/transactions. Refresh dismisses an obsolete editor if its payment is now Reconciled/Voided. Existing PATCH payload `{reference}` and routes preserved.
- Adjustment captures invoice/type/native amount/reason before disabling fields;valid invoice,positive finite amount,allowed reduction type and nonblank reason required. Existing POST field names/numeric serialization/raw reason (server trims it),allowed Discount/Scholarship/Concession and approval semantics preserved. Successful submission says **submitted for approval**,never approved or applied. Reset only matching captured native values/selectors;later draft edits are retained.
- Confirmed response keeps durable success even when readback fails and explicitly says not to repeat.500/408/network failures are unknown outcomes,retain draft and require successful explicit read/review before retry. Other4xx retain safe backend/fallback guidance and draft. No automatic write retry or new financial approval.
- Scoped top-level recovery buttons no longer translate on hover,avoiding repeated-refresh click instability. Previous monetary display/date/eligibility filters/versioned policy/governance behavior remain intact.

## Verification

- Final webpack production build/TypeScript PASS,86 outputs. Changed-page ESLint0errors/0warnings. No application-source edits after final build.
-111 focused tests PASS:49 new recovery/validation/transport cases plus62 retained tests. The four deliberately changed load/save declarations are now checked through explicit request/state/outcome tests rather than falsely compared as unchanged.18 pre-edit declaration hashes still verify unchanged eligibility/projection/policy/governance handlers;12 currency formatter cases,existing policy/governance and adapter tests retained.
-28 fresh targeted backend InMemory/controller tests PASS (ReconciledBalanceTests,AdjustmentTests matching filter and BatchOneFinancialPolicyTests). No backend/schema/persistence change;these tests are not live SQL/race/HTTP/device release acceptance.
- Final-export controlled browsers PASS168 cases:96 recovery (2pages×3widths×2themes×8scenarios:normal,initial failure,rejection,500 committed uncertainty,network loss,confirmed save/readback failure,delayed write,delayed read) plus72 retained four-screen visual cases. Exact request bodies/success/error/draft/reset/read recovery/no automatic retry/captured evidence/readback verified. Receipts:`QA/EVIDENCE/finance-recovery-1792039789592/result.json`, `QA/EVIDENCE/finance-controls-visual-1792039713856/result.json`;all recorded source hashes match final files. Final phone light reconciliation/dark adjustment recovery screenshots inspected. Six vendor hashes/script parse/scoped whitespace PASS. This is not live SQL/auth/device/approval acceptance.
- Existing settings conflict36 and document-preview112 cases passed during this batch on unchanged policy/governance/shared-adapter source. Their earlier receipts remain retained regression evidence,not claimed freshly rerun after the last adjustment-only reset repair: `QA/EVIDENCE/finance-settings-conflict-browser-1792039618547/result.json`, `QA/EVIDENCE/finance-document-preview-1792039618577/result.json`.

## Browser-discovered corrections

The first synthetic readback test used an exact text locator for a reference span that also contains its timestamp;it now locates the evidence container and asserts the reference,not a product change or weakened financial expectation. Recovery clicks then exposed inherited hover translation;scoped fixed-position interaction repaired with normal clicks retained.

Actual browser confirmed adjustment fields did not reset because FormData omits disabled fields. The conditional reset now reads the current native named controls' values even while disabled,still preserving later drafts. Unit fixture models named native controls;success/reset assertions retained and final browser rerun required. Earlier failed/intermediate receipts remain local,not counted as final PASS.

The held-readback browser assertion originally expected a reconciliation editor to remain visible after the confirmed PATCH. Its existing/intended lifecycle closes immediately after confirmation;the test now explicitly verifies that closure while ledger actions and refresh remain disabled until readback settles. Held-write scenarios still dispatch two extra native submit events;held-read stale-handler protection is additionally checked in the handler suite. No approval,amount,no-duplicate or readback assertion was removed.

## Limits and next

This is page-lifetime pending/outcome protection,not durable server/cross-tab/reload idempotency,duplicate adjustment reconciliation or verified bank evidence. Successful read asks a human to inspect the previous action;it does not prove an unknown submission was absent. Existing SQL transaction/audit/concurrency,authorization/tenant,physical-device and exact-release gates remain OPEN. Do not close financial P0 issues from this checkpoint.

Next High:bounded governance collection/approval action pending/outcome safeguards where existing handler limitations remain;document-settings version/concurrency contracts must stay intact. Then Medium remaining payroll/academics/communications and full portal visual/state parity. Existing PENTA/Mini/UI2 qualification remains OPEN. Owner order stays app completion→authorized go-live→Hayansh onboarding→Academy Desk product website after first customer is live.

No backend/schema/Mini/vendor/main/stage/commit/push/Azure/production data/container change. Earlier unrelated dirty work and QA evidence preserved;main unchanged `f7af512f884ca5fe8ac3ddb287c852492592e709`.
