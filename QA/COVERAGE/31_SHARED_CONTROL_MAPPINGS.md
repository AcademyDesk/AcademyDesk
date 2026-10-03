# Batch 31 — Shared-control mapping, re-audited

This review supersedes the earlier Batch 31 completion claim. Runtime: NOT RUN.

## Corrections established by re-audit

Before correction, progress.json mapped only 123 of 168 non-form declarations. The other 45 were source anchors in prose with no machine-readable entries or explicit scenario links. The previous 24-check validator could pass incomplete coverage because it checked validity of entries without checking missing declarations. Its PASS result did not support the claim of complete mapping.

The corrected manifest contains 168 distinct non-form declarations, plus 481 lexical fields in 82 native forms, for 649 total. The previous 485 + 126 = 611 aggregate was arithmetic/reporting drift. The 70 controller mappings remain source-document associations. Neither total certifies semantic completeness, runtime behavior, rendered field counts or a release.

The structural identity for the CSV file input is retained. It is derived from tag/type/accept and qualified by file and line; it is not universally unique. The validator now recomputes it from actual inventory props, rejects zero/multiple matches and rejects two aliases of one declaration.

## Final 46 declaration-to-scenario links

All entries below are registered in progress.json. IDs refer to existing proposed scenarios, extended by the source-specific obligations below. No test has been executed merely by linking it here.

| Source | Mapping label | Match property | Exact source expression | Existing Test IDs |
| --- | --- | --- | --- | --- |
| apps/web/src/app/activity/page.tsx:90 | Searchactivity | value | <code>{query}</code> | VISUAL-PAGE-005 |
| apps/web/src/app/activity/page.tsx:96 | activityrecordtype | name | <code>"activity-record-type"</code> | VISUAL-PAGE-005 |
| apps/web/src/app/activity/page.tsx:103 | activityperiod | name | <code>"activity-period"</code> | VISUAL-PAGE-005 |
| apps/web/src/app/activity/page.tsx:110 | activitydate | name | <code>"activity-date"</code> | VISUAL-PAGE-005 |
| apps/web/src/app/compliance/page.tsx:81 | personType | name | <code>"personType"</code> | VISUAL-PAGE-021, COMPLIANCE-API-010, COMPLIANCE-API-013 |
| apps/web/src/app/compliance/page.tsx:81 | personId | name | <code>"personId"</code> | VISUAL-PAGE-021, COMPLIANCE-API-010, COMPLIANCE-API-013 |
| apps/web/src/app/compliance/page.tsx:82 | expiryDate | name | <code>"expiryDate"</code> | VISUAL-PAGE-021, COMPLIANCE-API-010 |
| apps/web/src/app/compliance/page.tsx:82 | visibility | name | <code>"visibility"</code> | VISUAL-PAGE-021, COMPLIANCE-API-010 |
| apps/web/src/app/expenses/page.tsx:18 | expenseFilter | name | <code>"expenseFilter"</code> | VISUAL-PAGE-028 |
| apps/web/src/app/finance-governance/page.tsx:70 | documentKind | value | <code>{documentKind}</code> | VISUAL-PAGE-033, FINANCE-API-008 |
| apps/web/src/app/finance-governance/page.tsx:70 | documentKindInvoicedocumentSettingsinvoiceTemplateKeydocumentSettingspayslipTemplateKey | value | <code>{documentKind === "Invoice" ? documentSettings.invoiceTemplateKey : documentSettings.payslipTemplateKey}</code> | VISUAL-PAGE-033, FINANCE-API-008 |
| apps/web/src/app/music/page.tsx:295 | progressitemid | name | <code>{`progress-${item.id}`}</code> | VISUAL-PAGE-048, LEARNING-API-019 |
| apps/web/src/app/platform/control/page.tsx:881 | name | name | <code>{name}</code> | VISUAL-PAGE-052 |
| apps/web/src/app/platform/control/page.tsx:903 | name | name | <code>{name}</code> | VISUAL-PAGE-052 |
| apps/web/src/app/portal/page.tsx:199 | id | value | <code>{id}</code> | VISUAL-PAGE-055, FAMILY-API-008 |
| apps/web/src/app/portal/page.tsx:270 | unnamed | ref | <code>{input}</code> | VISUAL-PAGE-055, AUTH-API-010 |
| apps/web/src/app/portal/page.tsx:530 | Historyfromdate | value | <code>{from}</code> | VISUAL-PAGE-055, FAMILY-API-008 |
| apps/web/src/app/portal/page.tsx:530 | Historytodate | value | <code>{to}</code> | VISUAL-PAGE-055, FAMILY-API-008 |
| apps/web/src/app/portal/page.tsx:549 | Paymentcycle | value | <code>{cycle}</code> | VISUAL-PAGE-055, FAMILY-API-008 |
| apps/web/src/app/portal/page.tsx:549 | Paymentstatus | value | <code>{status}</code> | VISUAL-PAGE-055, FAMILY-API-008 |
| apps/web/src/app/practice-logs/page.tsx:2 | Teacherfeedback | value | <code>{v}</code> | VISUAL-PAGE-057, LEARNING-API-022 |
| apps/web/src/app/teacher/page.tsx:261 | unnamed | ref | <code>{inputRef}</code> | VISUAL-PAGE-072, AUTH-API-010 |
| apps/web/src/app/teacher/page.tsx:374 | name | name | <code>{name}</code> | VISUAL-PAGE-072 |
| apps/web/src/app/teacher/page.tsx:466 | fromDate | value | <code>{fromDate}</code> | VISUAL-PAGE-072, TEACHERPORTAL-API-024 |
| apps/web/src/app/teacher/page.tsx:466 | toDate | value | <code>{toDate}</code> | VISUAL-PAGE-072, TEACHERPORTAL-API-024 |
| apps/web/src/app/teacher/page.tsx:481 | payslipMonth | value | <code>{payslipMonth}</code> | VISUAL-PAGE-072, TEACHERPORTAL-API-028 |
| apps/web/src/app/teacher/page.tsx:618 | Feedbackforstudent | value | <code>{feedback[log.id] ?? log.teacherFeedback ?? ""}</code> | VISUAL-PAGE-072, TEACHERPORTAL-API-009 |
| apps/web/src/app/teacher-payments/page.tsx:37 | paymentteacher | name | <code>"payment-teacher"</code> | VISUAL-PAGE-075, TEACHER-API-001, TEACHER-API-002 |
| apps/web/src/app/teachers/page.tsx:217 | assignmentteacher | name | <code>"assignment-teacher"</code> | VISUAL-PAGE-077, BATCH-API-003 |
| apps/web/src/app/teachers/page.tsx:226 | assignmentbatch | name | <code>"assignment-batch"</code> | VISUAL-PAGE-077, BATCH-API-003 |
| apps/web/src/app/teachers/page.tsx:252 | editFirstName | value | <code>{editFirstName}</code> | VISUAL-PAGE-077, TEACHER-API-005 |
| apps/web/src/app/teachers/page.tsx:257 | editLastName | value | <code>{editLastName}</code> | VISUAL-PAGE-077, TEACHER-API-005 |
| apps/web/src/app/teachers/page.tsx:262 | Email | value | <code>{editEmail}</code> | VISUAL-PAGE-077, TEACHER-API-005 |
| apps/web/src/app/teachers/page.tsx:268 | Phone | value | <code>{editPhone}</code> | VISUAL-PAGE-077, TEACHER-API-005 |
| apps/web/src/app/teachers/page.tsx:274 | Specialties | value | <code>{editSpecialties}</code> | VISUAL-PAGE-077, TEACHER-API-005 |
| apps/web/src/app/teachers/page.tsx:280 | teacherbranch | name | <code>"teacher-branch"</code> | VISUAL-PAGE-077, TEACHER-API-005 |
| apps/web/src/app/work-queue/page.tsx:16 | workQueueFilter | name | <code>"workQueueFilter"</code> | VISUAL-PAGE-079, OPERATIONS-API-005 |
| apps/web/src/app/work-queue/page.tsx:16 | statusitemid | name | <code>{`status-${item.id}`}</code> | VISUAL-PAGE-079, OPERATIONS-API-007 |
| apps/web/src/components/enterprise-shell.tsx:509 | unnamed | ref | <code>{profileImageInputRef}</code> | UI-A11Y-001, AUTH-API-010 |
| apps/web/src/components/enterprise-shell.tsx:520 | SearchAcademyDeskmodules | value | <code>{query}</code> | UI-A11Y-001, UI-SCROLL-001 |
| apps/web/src/components/standard-date-field.tsx:43 | name | name | <code>{name}</code> | UI-A11Y-001, UI-TIME-001 |
| apps/web/src/components/standard-date-field.tsx:48 | Selectmonth | value | <code>{month.getMonth()}</code> | UI-A11Y-001, UI-TIME-001 |
| apps/web/src/components/standard-date-field.tsx:48 | Selectyear | value | <code>{month.getFullYear()}</code> | UI-A11Y-001, UI-TIME-001 |
| apps/web/src/components/standard-select-field.tsx:73 | name | name | <code>{name}</code> | UI-A11Y-001, UI-DROPDOWN-001 |
| apps/web/src/components/standard-time-field.tsx:100 | name | name | <code>{name}</code> | UI-A11Y-001, UI-TIME-001 |
| apps/web/src/app/data-operations/page.tsx:17 | StudentCsvFileInput | identity | <code>input:"file":".csv"</code> | STUDENT-API-008, STUDENT-API-002 |

## Source-specific obligations and scope boundaries

| Group | Current source behavior and applicable checks | Existing detailed review |
| --- | --- | --- |
| Activity: four filters | Search/action/entity/date/rolling-age filters run over already-fetched audit rows. Exercise combination/reset, no matches, midnight and future timestamps, empty/error/loading list; filter changes have no write DTO or EF column (N/A). UI label Today currently means elapsed 24 hours. Server list scope/cap remains an API concern. | [Batch 27](27_DOCUMENTS_COMPLIANCE_AUDIT.md) |
| Compliance: person/date/visibility | PersonPicker is instantiated separately in document and consent forms at lines73/74, so each instance owns independent state. Changing type clears its local personId; options use the supplied combined people array. FormData maps type to studentId or guardianId, expiry empty to null and visibility directly. Test both instances and subject-type mismatch; unknown IDs/visibility are API variants. This corrects the earlier phrase implying shared state between forms. | [Batch 27](27_DOCUMENTS_COMPLIANCE_AUDIT.md), BUG-DATA-0045 |
| Expense category filter | Filters the loaded register, with All and category options. Test reset and no matches. No filter write/persistence rule; displayed overall KPI totals need not equal filtered subtotal. | [Batch 28](28_FINANCE_OPERATIONS_COMPENSATION.md) |
| Finance document/theme selectors | Kind switches preview; theme changes documentSettings, then the editing form's hidden fields submit those settings. Test each kind, theme retention, opening/closing editor, failed save and reload. Kind itself is preview-only; do not count hidden theme mirrors as additional business inputs. | [Batch 06](06_FINANCE_GOVERNANCE.md) |
| Music/practice status and review | Progress selector writes per-item status; practice feedback writes teacherFeedback through review PATCH. Test row identity, empty/long feedback, failures, repeat clicks and response/readback; approved feedback policy and server length rules remain semantic obligations. | [Batch 26](26_MUSIC_PRACTICE_RESOURCES.md) |
| Platform Field / FieldSelect helpers | Both are local definitions (lines867/891); no JSX call sites were found in this file. They count as source declarations, not active screens. Live submission, role or persistence assertions for these unused helpers are N/A at this baseline. The page scenario covers absence and future use must be re-reviewed. | [Batch 13](13_PLATFORM_ADMINISTRATION.md) |
| Family selector, picture and filters | Child selector selects the portal student context; picture input uses the authenticated profile upload. History dates and invoice cycle/status filter loaded results. Test switching linked children while loading, rejected upload with retry, from/to and empty/reset filters; filter values have no write DTO/EF mapping. Permissions follow child reads and self-avatar endpoint, not the mere presence of controls. | [Batch 05](05_TEACHER_FAMILY.md), [Batch 03](03_AUTH_ACCOUNTS_FILES.md) |
| Teacher portal controls | Avatar input uploads current user's image. TeacherBatchDropdown hidden input mirrors name/value at its call sites; test FormData and selection without counting duplicate business fields. History dates drive classroom-activity queries; month drives payslip query. Practice feedback PATCH uses the selected log and typed draft. Test empty/range/reset, denied/stale responses and per-row pending/save/error state. | [Batch 05](05_TEACHER_FAMILY.md) |
| Teacher payments selector | Select sets teacherId before async compensation load. Test clear, inactive teacher, failures and reverse-order responses; submission must target visible record. Selector identity is a route parameter, not a separate compensation column. | [Batch 28](28_FINANCE_OPERATIONS_COMPENSATION.md), BUG-DATA-0018 |
| Teacher management: assignment/edit | Assignment sends a full batch PUT but cannot retain fields absent from its Batch snapshot/payload; Online/Hybrid may fail meeting-link validation and InPerson can lose extended settings. Edit sends names, nullable email/phone/specialties/branch and original isActive; linked Identity updates occur after the teacher commit. Exact field limits, state/permission matrix and distinct findings are now traced. Successful load clears the just-set success message. | [Batch 35](35_TEACHER_MANAGEMENT_SEMANTICS.md), [Batch 01](01_ONBOARDING_PAYROLL.md), [Batch 19](19_BATCHES_ENROLLMENT_PROMOTIONS.md), BUG-DATA-0031/0051/0052, BUG-FUNC-0003 |
| Work queue | Filter invokes list request; status invokes per-item mutation then reload. Test query/row identity, reverse-order responses, each listed status, rejection, and durable notice. Filter DTO is read-only; status persistence uses the existing work-item action. | [Batch 28](28_FINANCE_OPERATIONS_COMPENSATION.md) |
| Shared shell | Profile image input submits current-user avatar. Search filters navigation choices; test no matches, role visibility, select/close/focus and long results. Search has no server write/EF contract. Avatar success/failure is AUTH-API-010, not private class-file authorization. | [Batch 03](03_AUTH_ACCOUNTS_FILES.md), [shared controls](30_SHARED_CONTROL_RECONCILIATION.md) |
| Date/select/time implementations | Hidden inputs mirror call-site state. Date month/year selectors navigate the calendar; picking a day emits a local YYYY-MM-DD string. Year options run from1900 to currentYear+25. Test leap days, clear/required behavior, values outside year list, keyboard/focus, nesting, scroll/resize and edge anchoring. Month/year navigation has no DTO/EF column; selected value rules belong to consuming forms. Test select/time emitted values and midnight/AM/PM; browser success remains unverified. | UI-TIME-001, UI-DROPDOWN-001, UI-A11Y-001; underlying field obligations in individual form batches |
| CSV file input | loadCsv reads file text, splits lines/commas into rows; filename is local state. Validate and Import send JSON rows, not multipart file. Test cancel/reselect, quoted CSV, malformed headers, duplicate rows and false validation-success handling. No file storage/EF file column belongs to this input. | [Batch 29](29_IMPORTS_DASHBOARD_INTELLIGENCE.md) |

## Validation and next boundary

coverage.cjs resolves each source declaration and enforces full, distinct coverage of forms, fields and controllers. coverage.test.cjs exercises omitted controls, aliases, same-line collisions, stale identity/line and missing form/controller cases. These are QA tooling checks.

The final 46 rows are mechanically complete. Semantic acceptance is assessed in [the revised Batch 32 review](../REPORTS/PHASE_1_GAP_REVIEW_BATCH_32.md); no existing application issue is closed.
