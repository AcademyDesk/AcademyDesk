# Existing test audit

All eight tests were read and executed. Evidence: `EVIDENCE/logs/phase1-existing.trx`. They construct controllers directly with random-name EF InMemory contexts; no HTTP authentication/filter/model-binding/serialization occurs. They use synthetic data. No duplicate, obsolete or destructive-production test was identified among these eight; none was deleted.

| Test | Useful assertion | Weakness / required extension |
| --- | --- | --- |
| AcademicGovernanceWorkflowTests.Term_outside_its_academic_year_is_rejected | BadRequest result type for out-of-year term | No reason/body assertion, no explicit unchanged-row check, only one boundary |
| AcademicGovernanceWorkflowTests.Assessment_cannot_use_an_inactive_grading_scheme | BadRequest result type for inactive scheme | No response content or absence-of-insert assertion; InMemory permits missing real relationships |
| AcademicGovernanceWorkflowTests.Approved_promotion_completes_source_and_creates_target_enrolment | Source Completed, target Active and promotion Approved | Same tracked context, no fresh SQL commit/read, auth or duplicate/concurrent approval tests |
| ComplianceWorkflowTests.Withdrawing_consent_retains_the_record_and_timestamp | Granted false and withdrawal timestamp retained | No serialized content, timestamp range or repeated-withdraw assertion |
| ComplianceWorkflowTests.Document_review_rejects_invalid_state | BadRequest for unknown status | Only result type; no unchanged document or message assertion |
| FinanceWorkflowTests.Reconcile_records_evidence_and_marks_payment_reconciled | Status/reference/timestamp stored | Does not recalculate collection/family balances; misses BUG-DATA-0001 |
| FinanceWorkflowTests.Approving_full_adjustment_marks_invoice_paid_and_records_decision | Adjustment amount/status/notes/time and invoice Paid | Does not exercise later collection against adjusted balance; misses BUG-DATA-0002 |
| FinanceWorkflowTests.Rejecting_adjustment_preserves_invoice_balance | Invoice amount/status unchanged and rejection recorded | Does not check HTTP/result content; same tracked context; no SQL transaction/concurrency |

Three tests check only the IActionResult type, effectively status-shape checks rather than the full contract. Five assert stored state, but InMemory and tracking mean none proves SQL constraints, fresh-connection durability or actual HTTP response serialization. Coverage collection package is installed; no measured line/branch coverage report was generated. No existing automated success-message, optional-date, teacher-create, identity-transaction, role-isolation, upload privacy, responsive or E2E suite was found.

Recommended retention: keep all eight as fast regression checks; strengthen rejected-result tests to assert message + no mutation and add separate SQL/HTTP integration cases for persistence/policy rather than duplicating each business assertion at every layer. Existing test PASS must not be presented as an end-to-end feature PASS.
