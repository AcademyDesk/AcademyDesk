# Batch 37 — Evidence reconciliation and corrections

Continuation: [Batch 38](38_OPERATIONS_FORM_SEMANTICS.md) completes the three operations-form field/state traces and registers BUG-DATA-0053. The nine-form gap count and 109 issue count below describe the state at Batch 37; current counts are in [counts.json](../REPORTS/counts.json). The next bounded slice is four learning forms, then two certificate forms.

2026-09-30. Audit-only; application runtime NOT RUN. Phase 1 decision: **NOT CLOSED**. This is an evidence-location reconciliation, not a declaration that every located paragraph satisfies every semantic requirement. Preserve the existing detailed reviews; do not restart the full application audit.

## Corrected action permissions

Source: `apps/api/Security/SubscriptionPlanCatalog.cs:28–45`, `PermissionCatalog.cs`, `AcademyAccessFilter.cs`, `Program.cs:88–102`, and the eight named controller files below. Reuse the common gate in [Batch 36](36_LATER_ACTION_PERMISSIONS.md#common-effective-gate--implemented-behavior-not-approved-policy): anonymous/unresolved users are denied; inactive authenticated users are rejected by Program; non-platform callers need a local active academy before module/admin/permission evaluation. The **flag**, not a role named PlatformOwner, bypasses the academy filter. Ordinary callers of unmapped academy controllers are denied even with all custom grants. This includes teacher/family users; linked learner/teacher ownership does not grant access here.

For each admitted mutation, specify valid control, cross-tenant target, nonexistent target and invalid request variants; assert fresh domain/file state and no false success on denial. HTTP, binding, audit failure and storage behavior still require Phase 2. No unauthorized request has been executed.

| Action | Module / effective admission | Local outcome and boundary |
| --- | --- | --- |
| MusicPieces.List | Core, no catalog mapping; local Owner/Admin or platform flag | Lists active route-academy pieces only; no learner ownership filter |
| MusicPieces.Create | Same | Blank title 400; creates in route academy; no additional local role check |
| MusicPieces.SetActive | Same | Scoped piece lookup or 404; cannot mutate a foreign-tenant piece through another academy route |
| MusicProgress.List | Core, no mapping; local Owner/Admin or flag | Route academy plus optional student query filter; not a linked-student access gate |
| MusicProgress.Assign | Same | Student and piece must exist in route academy (400 otherwise); duplicate assignment 409; active status not checked |
| MusicProgress.Update | Same | Invalid status 400, scoped progress missing 404; replaces status/score/notes/target date without a separate review role |
| PracticeLogs.List | Core, no mapping; local Owner/Admin or flag | Route-academy list; not an individual student's private reader |
| PracticeLogs.Create | Same | Minutes below one 400; entity binding does not check StudentId ownership or protect review fields (BUG-DATA-0043) |
| PracticeLogs.Review | Same | Scoped log or 404; writes feedback, Reviewed status and server time; no extra teacher/reviewer check |
| LearningResources.List | Certificates, no mapping; local Owner/Admin needs module, or flag | Lists all route-academy resources including unpublished; no learner scope filter |
| LearningResources.Create | Same | Title/absolute URL plus local batch/course checks; no cross-check of batch's course (BUG-DATA-0044) |
| LearningResources.Upload | Same | Nonempty permitted extension plus local scope; writes file before row save. Static file retrieval remains separate P0 BUG-SEC-0001 |
| LearningResources.Publish | Same | Scoped resource or 404; updates publication with no separate publish role |
| StudentImports.Validate | Core, no mapping; local Owner/Admin or flag | Returns validation errors in an OK response; does not import. No extra student-onboarding role check |
| StudentImports.Import | Same | Basic row errors 400; existing route-academy email conflict 409; intra-request duplicates remain BUG-DATA-0049. No extra local role check |
| AdminIntelligence.Get | Core, no mapping; local Owner/Admin or flag | Route-scoped datasets; orphan teacher projection remains BUG-FUNC-0031; no extra local role check |
| Dashboard.Summary | Core, no mapping; local Owner/Admin; flag is NOT sufficient | Explicit authentication plus local user.AcademyId equality: foreign or unassigned flagged owner still 403. Same-tenant flagged owner bypasses active/module filter; local academy missing returns 404 |
| WeatherForecast.Get | N/A academy/module/permission gate: no academy argument or authorization attribute | Anonymous random sample is allowed by source; authenticated inactive caller is rejected by Program. No persistence/tenant data; production allow/remove policy unresolved |

Existing permanent API IDs for these 18 actions are resolved by method + route + controller + action in [the generated index](semantic-index.json). They remain NOT RUN. Rows above correct Batch 26's nonexistent Learning module and Batch 29's incomplete dashboard/global-gate explanation. They do not add or close application issues.

## Evidence index and limits

Run `node QA/tools/semantic-index.cjs` to regenerate [semantic-index.json](semantic-index.json). Each of the 82 native forms, 168 standalone controls and 282 controller actions has an inventory identity, source location, existing evidence sections and available permanent scenario IDs. Form rows explicitly contain their 481 field inventory indices. Profiles action references are split across guardian/student/teacher evidence; PlatformControl references across onboarding/administration/billing evidence. Later compensation/compliance/teacher-edit/action reviews take precedence without erasing earlier evidence.

**EVIDENCE-LOCATED is not STATIC-SPECIFIED.** Section references are candidate evidence for a reviewer; heading presence or ID occurrence cannot prove a full rule trace. The generator never upgrades a row to accepted. Missing individual scenario mappings are exposed, not invented from a nearby page ID. Framework Identity's ten routes stay in their separate manifest. The 843 UI declarations are not 843 unique editor/modal states; grouping those states remains a separate G01 obligation.

## Demonstrated remaining details

| Bounded follow-up | Existing evidence to preserve | Exact missing reconciliation |
| --- | --- | --- |
| Four music/practice/resource forms: LEARNING-FORM-003–006 | Batches 03/26 plus corrected permissions above | Expand prose's unspecified optional EF lengths and property transforms into exact per-field boundaries; distinguish link versus multipart upload; bind conditional progress/review state to its source control |
| Two certificate forms: COMPLIANCE-FORM-001/002 | Batch 27 and Batch 36 action permissions | Explicit UI-to-branding/upload/issue request mapping, conditional theme values, exact persisted lengths/null rules and per-form reset/error/success states |
| Three operations forms: FINANCE-FORM-001/006, OPERATIONS-FORM-001 | Batches 06/28/36 | Expense category/branch/date payload names versus UI names; policy's missing payslip field; queue date/time to one UTC property and reset failure ordering; exact limits instead of generic "EF limits" |
| Remaining index rows | All earlier detailed batches; 33–36 remain valid within stated scope | Compare located evidence to each row's applicable G01/G02/G03 obligations; supply an exact section verdict or named omission. A document association alone cannot close this row |
| Non-form UI state groups | UI inventory, shared-control Batches 30/31 and source-specific earlier sections | Account for editor/modal/action branches not represented by input declarations; explain N/A for navigation/presentation rather than equating controls to all interactions |

Finance-policy concrete trace requiring follow-up: `apps/web/src/app/finance-policy/page.tsx:14–15` sends `form.get("payslipTemplateKey")` but renders no control with that name; therefore a normal submit sends null. `FinanceGovernanceController.Save` assigns null to the Standard default, replacing a saved Compact/Professional selection. Extend FINANCE-FORM-006 with a preservation regression, deduplicate/register the issue in the next field-trace slice, and do not label this runtime-reproduced. This observation is not yet an additional counted issue.

Expense and work-queue feedback corrections are applied directly in Batch 28: expense notice follows successful reload; queue's asynchronous currentTarget access can fail before the notice. Preserve those differences instead of applying one generic success-message assertion to every form.

## Acceptance decision and next bounded task

Phase 1 remains **NOT CLOSED**, but the reason is specific evidence/semantic work, not missing declaration coverage and not the absence of Phase 2 execution. Do not approve a release, execute a production save, or introduce mass tests/fixes. Existing registered totals remain 109 open issues / 589 proposed tests / 599 scenarios. Audit consistency and index-helper PASS only establish artifact integrity.

Next bounded task: finish the three operations-form G01/G02 traces above and register the confirmed static payslip overwrite without duplicating existing reset/message findings. Then review the four learning forms and two certificate forms, followed by the remaining located-row and non-form-state verdicts. These are work scopes, not a promise of a fixed number of turns. Use the agreed Sol High allocation for bounded source-to-field work; reserve Astra High for the final acceptance decision. This task-specific judgment follows [official model-selection guidance](https://developers.openai.com/api/docs/guides/model-selection), checked 2026-09-30; it is not a performance guarantee. No model switch is performed by this document.

Verification: 17/17 QA-helper tests and 26/26 consistency checks pass, including baseline hashes and index freshness. The index's 122 standalone entries without individual scenario IDs are unresolved links, not 122 new bugs or proof of absent historical review. Application runtime tests were not run.
