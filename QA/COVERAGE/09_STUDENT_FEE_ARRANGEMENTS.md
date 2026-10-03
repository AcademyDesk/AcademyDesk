# Batch 09 — Student fee arrangements and admission fees

2026-09-28. **Source specification only; runtime NOT RUN.** Baseline unchanged, including the pre-existing student-profile edit. No application changes, financial writes, browser tests or deployment. This covers admission and subject-fee configuration, not payment collection or automatic billing.

## Scope

Read student-fees/page.tsx, shared components/student-fee-arrangements.tsx, StudentFeeArrangementsController (four actions), StudentFeeArrangement entity and EF mappings. Inspected shared component use in student-profile/page.tsx without modifying that file. StudentsController.Overview/List are partial dependencies, not a completed Students controller review. Reuse batch02 invoice/payment rules and batch07 selection-race finding.

## G02 — exact fields and conditional rules

| Stable case / source | Field→request→guard/storage |
| --- | --- |
| FEES-FORM-004 / student-fees form-1 | admissionAmount optional controlled number min0, step0.01 → amount Number(value) or null; admissionDueDateDisplay custom date → dueDate string or null. PUT students/{studentId}/fee-arrangements/admission-fee. API decimal? Amount, DateOnly? DueDate; amount<0 rejected, date with null amount rejected, bothnull allowed. Student scoped to academy or404. Amount decimal(18,2) on Student; date nullable. Amount0 plus date permitted. No explicit past/future/date-window rule |
| ACADEMY-FORM-003 / shared arrangement form-1 | subjectName required text; amount required number min1 with **no step**; fee-frequency StandardSelect default Monthly, options Monthly/Quarterly/HalfYearly/Annual. POST JSON subjectName,Number(amount),frequency,effectiveFrom computed as current UTC ISO date. CourseId omitted. API nonblank trimmed subject, positive decimal amount, exact frequency allowlist. Subject max200, amount decimal(18,2), frequency max30. Missing/null EffectiveFrom defaults UTCtoday |
| fee-student standalone selector | Student GUID selected from loaded student rows, inactive suffix preserved. Default first active else first row else empty. ID goes into API route and child props, not trusted as authorization. Empty selection hides workspace. This selector counts once; shared form counts once even though rendered in two routes |

Admission matrix: absent/null amount+absent/null date accepted; date-only rejected without amount;0/positive amount with/without date accepted;negative rejected. Test blank UI normalizes to null (no empty-date serialization defect here), zero preserved, invalid/malformed dates, leapday/year navigation, local/UTC boundaries, two vs three decimals, amount overflow/JS precision and server-rounded readback. saveAdmissionFee ignores successful response summary, so verify fresh GET/UI reflects stored decimal exactly, not just typed text. Failed PUT must retain values and distinguish authorization/network/validation problems.

Subject matrix: null/empty/whitespace name,199/200/201chars, Unicode/HTML-looking/unbroken names; amount0/negative/0.01/0.99/1/1.01/1250.50 and precision18,2 boundaries; missing/null/blank/case/unknown frequency. UI min1 differs from API>0, and no step defaults to integer increments (BUG-FUNC-0008). Exact numeric business minimum needs agreement separately from allowing fractional amounts. No amount/currency model switch exists: only four billing frequencies, all requiring subject+amount; do not invent monthly/hourly/cycle-specific rate fields from Teacher Compensation.

API-only CourseId nullable: supplied course must exist in same academy, but active status/student enrollment and subject-name-to-course consistency are not checked. EffectiveFrom nullable date allows past/future; UI always sends current UTC date, no editable effective-date field. EffectiveTo/IsActive are response/entity fields, not request fields; new records active with null end. No update/deactivate/delete action in this controller. There is no unique subject/course/date index: repeated creates can persist duplicate/overlapping arrangements. Define legitimate revisions/multiple-course policy; test concurrent identical submissions and ambiguous timeout retries rather than claiming every matching subject must be unique.

No currency property exists on admission/arrangement DTO/entities; UI subject amount prefixes₹. Do not infer multi-currency support just because invoice FeePlan supports it. StudentFeeArrangements stores configuration; this path creates no invoice/payment/notification and no billing scheduler was established here. Include end-to-end linkage policy later before claiming configured fees are billed or paid.

## G03 — action and dependency gates

Global AcademyAccessFilter handles these academyId routes. StudentFeeArrangements maps **Core** by default (not Finance module), permission student-fees.manage. Ordinary caller must match academy, active academy and module gates; Owner/AcademyAdmin pass. FinanceUser and FrontDesk have student-fees.manage; custom valid grants can authorize others. IsPlatformOwner flag bypasses global tenant/module/permission and generic audit; role name alone cannot. Inactive Identity blocked upstream. Test all12 roles, revoked/expired grants, foreign tenant/student/course, disabled module and active/inactive records. Module placement is implemented behavior; desired subscription policy is not inferred.

| Action | Additional guard / return |
| --- | --- |
| AdmissionFee | Same-academy student lookup,404 if absent;200 amount/date, including inactive student |
| UpdateAdmissionFee | Validates amount/date then scoped student; saves nullable values,200 summary. No invoice update or receipt |
| List | Scoped academy/student arrangement query; missing/foreign student gives empty200, not person404; sorts EffectiveFrom descending, includes inactive/expired/future arrangements |
| Create | Student same tenant exists or404; name/amount/frequency validation; optional course tenant check;200 summary. Inactive student/course not explicitly rejected. No duplicate/overlap check |
| Students.List (partial) | Core + students.manage; same-academy full roster including inactive, name sort. Fee Details loads this before rendering controls; FinanceUser lacks students.manage, extending existing BUG-FUNC-0006 dependency problem |
| Students.Overview (partial) | Same gate; totals active arrangements by IsActive only, no EffectiveFrom/To or active-student filter. Admission totals include all students; invoice outstanding/overdue uses gross unpaid-status amounts. Reuse financial balance/cancelled/currency tests from batches02/06/08; do not call these sums collected cash |

Shared form in Student Profile also depends on stronger ProfilesController access to reach page state (batch07 unmapped-controller admin gate); component permission does not guarantee page eligibility. Preserve least privilege when designing later fixes. Global audit after mutation remains a separate-save failure risk (existing BUG-API-0002); no safe-success inference from HTTP alone.

## G01 — state, feedback and selection integrity

Fee Details initial load parses academy response without checking status; missing first item can misleadingly suggest creating academy. Student load does check status. Active/inactive/empty roster,403/500/network, error JSON and stale data need tests. Student selector stays available during load/save. Admission effect neither clears prior values nor marks pending nor discards outdated responses. Switching A→B while B GET fails/delays leaves A values under B; Save uses B ID. This extends **BUG-DATA-0018 / GUARDIAN-PROFILE-001** with student fee evidence, not a duplicate issue. Test failed/slow/out-of-order loads, empty selection, changes during PUT and exact fresh A/B rows.

saveAdmissionFee sets saving true, awaits fetch, then clears saving and shows success/generic validation error. No try/finally/catch: rejected fetch can leave busy permanently. Success stays visible but is not cleared on selection change, so it can be attributed to the next student. No currentTarget.reset defect here. Save result is not parsed or read back. Optional fields must remain empty when saved as null and not force fabricated values.

Shared arrangement component is not keyed by student in Fee Details/Student Profile; state and draft subject/amount/frequency survive prop changes. load updates items only for response.ok, ignores non-OK and has no error catch or stale-response guard. Failed new-student load can show old rows; late response can overwrite selected student's list. add has no busy guard/catch, generic validation error for every non-OK, resets subject/amount on success (frequency stays), sets success then loads without clearing it. Good ordinary success persistence does not prove reload succeeded. Pending old-student add can reset new-student draft and reload old student's items. Test explicit keep/discard draft policy and correct record attribution before allowing submit after selection.

List heading uses items.length as “active” without filtering isActive or effective dates; server returns all arrangements. Rows hide effectiveFrom/effectiveTo/isActive/course, so inactive/future/expired fixtures need truthful labels/counts and distinction. These are source-state cases under ACADEMY-FORM-003/FEES-API-010, not runtime claims about current data. No edit/disable affordance rendered; do not invent corresponding modal tests.

Visual protocol:360/390/430/768/1440, adjacent breakpoints,200% zoom, portrait/landscape and both themes. Fee student dropdown must match the established Teacher360 interaction standard: anchored to trigger, above adjacent card layers, value changes and collapses, touch/keyboard/Escape/outside click, no page-top jump. Check short-page and scroll0/25/50/75/bottom, long/inactive names and soft keyboard. Admission header Save wraps on mobile, amount/date surfaces clearly separated, accessible date label/year selection; shared subject/amount inputs need persistent labels beyond placeholders. Frequency dropdown and Add button should not cover list/footer. Verify decimals, long subject names, null fees, loading/error/success live announcements and keyboard focus. No physical-device/browser/screenshot PASS and no approved baseline.

## Findings, coverage and next work

One new OPEN / STATIC-FINDING P2: BUG-FUNC-0008 / FEES-AMOUNT-001 (fractional amount blocked by input step). Existing P1 BUG-DATA-0018 expanded to admission fees; other access/balance/feedback families reused. No issue closed or runtime-reproduced.

Added2 native forms/5 controls,1 standalone student selector and1 complete controller/4 actions. StudentsController Overview/List are PARTIAL. Cumulative **32/82 forms,193/649 controls,22/70 complete controller classes**. Source counts are not overall completion percentage or application tests.

Next bounded review: remaining StudentsController and administrative student profile fields/actions, preserving pre-existing user edits. Reuse this shared fee form and prior onboarding/family mappings; no duplicate counts. Continue G01/G02/G03 reconciliation with agreed Astra High audit stage; no Phase2, app repairs or deployment yet.
