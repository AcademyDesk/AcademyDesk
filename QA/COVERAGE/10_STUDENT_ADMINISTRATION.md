# Batch 10 — Student administration and administrative profile

2026-09-28. **Source specification only; runtime NOT RUN.** No application edits, database writes, browser verification or deployment. Preserve the existing student-profile/onboarding changes. This is not a claim that the reported Azure null/save errors are reproduced or repaired.

## Scope and reuse

StudentsController complete (Overview/List reuse batch09; Create/Update below), student register at /students, Student 360 at /student-profile and its /student-management alias, and components/student-admin-profile.tsx. ProfilesController remains PARTIAL: Guardian/UpdateGuardianProfile were reviewed in batch07; Student/UpdateStudentProfile are reviewed here; teacher actions remain pending. EnrollmentsController.Create and BatchesController.List are assignment dependencies only, not complete module reviews. Shared subject-fee editor was counted in batch09 and is not counted again.

No new native forms: these controls use click handlers. Add 17 standalone controls: three register controls, one profile selector and thirteen administrative fields. Stable source cases: STUDENT-API-004/005/006/007, PROFILE-API-002/004; visual requirements extend VISUAL-PAGE-069/070. New targeted cases STUDENT-BRANCH-001/002; wrong-record selection reuses GUARDIAN-PROFILE-001 / BUG-DATA-0018.

## G02 — fields and request contracts

| Control / source identity | Mapping and boundary requirements |
| --- | --- |
| query (/students, unnamed value) | Client-only case-insensitive first/last name, email and phone search. Test empty, whitespace, Unicode, long string, no match, active/inactive filter combinations; search must not change persisted records |
| assignment-student | Active roster student GUID; required by click handler. Empty, stale/deactivated, nonexistent and foreign-academy values must be tested server-side |
| assignment-batch | Active batch with UI enrollmentStatus exactly Open; required by click handler. API accepts case-insensitive Open. Test no eligible options, closed/full/waitlisted batch, prerequisites and concurrent last-seat enrollment |
| student-record | Selected student GUID drives profile GET/PUT and child fee editor; requested query parameter or first roster item initially. Test empty, malformed, foreign, nonexistent and inactive student plus failed/out-of-order fetches |
| studentNumber | Optional string → Clean → null for blank/whitespace; EF max50; same-academy duplicate excluding current student →409. Filtered unique academy/number index. Test retain same number, clear, other tenant reuse, case/collation and concurrent duplicate writes |
| preferredName | Optional string → Clean; EF max120 |
| student-gender | Optional form.gender string; options Female/Male/Non-binary/Prefer not to say. API Clean with EF max50, no enum restriction. Test clear and direct unknown value; do not claim UI list is API validation |
| dateOfBirthDisplay | Optional form.dateOfBirth → JSON dateOfBirth or null. Existing value sliced to YYYY-MM-DD. Test blank, leap day, invalid date, future date and year selection |
| admissionDateDisplay | Optional form.admissionDate → JSON admissionDate or null; test blank, earlier/later than DOB and local/UTC boundary. Controller establishes no date ordering or future-date policy |
| addressLine1 | Optional Clean string; EF max240 |
| city / state | Optional Clean strings; EF max100 each |
| postalCode | Optional Clean string; EF max30. Preserve leading zeroes; no country-specific numeric assumption |
| emergencyContactName | Optional Clean string; EF max160 |
| emergencyContactPhone | Optional Clean string; EF max30; preserve +, spaces and leading zeroes; not numeric conversion |
| medicalOrAccessibilityNotes / adminNotes | Optional Clean strings; EF max4000 each. Sensitive administrative fields; verify authorized reads only, no HTML execution and no leakage to family/teacher self-service, logs or unrelated exports |

All thirteen administrative fields are optional in this editor. It has no native form validation and no displayed maxlength limits; database lengths are not friendly HTTP validation. For every string test omitted/null/empty/whitespace, max-1/max/max+1, Unicode and HTML-looking input. Test invalid JSON/types, date parsing, oversized payload, database failures and clear-and-reload persistence. Do not require fictitious optional values merely to pass a save. Duplicate validation occurs before profile mutation; concurrent uniqueness failures require real SQL and safe error/readback assertions. API-only profile request mirrors these fields; no name/email/account-status editing through this metadata endpoint.

Students.Create is a distinct minimal API, not the rich onboarding workflow: required nonblank FirstName/LastName trimmed (EF max120 each); optional DateOfBirth, Email (max320), Phone (max30), BranchId. Academy must exist; nonnull branch must belong to academy. New domain student defaults active. It creates no portal account or guardian and enforces no minor-onboarding relationship rule. Test omitted optional fields, malformed GUID/date, whitespace, length/email boundaries, duplicate names, foreign branch and inactive branch. No single-student GET is defined here despite the Created Location path; do not assert dereference success.

Students.Update is a full replacement DTO for first/last/email/phone/BranchId/IsActive, not PATCH. Missing nonnullable IsActive binding and omitted BranchId must be exercised explicitly. Names required; student must belong to route academy. BranchId is assigned without Create's tenant validation (BUG-DATA-0021). Register toggle always sends branchId:null because its client Student type omits branch; deactivation/reactivation therefore clears an existing assignment (BUG-DATA-0020). Separate UI preservation from API reference validation: fixing one does not fix the other.

## G03 — action, permission and persistence paths

| Action | Source behavior and test oracle |
| --- | --- |
| Students.Overview / List | Batch09 covers Core + students.manage and tenant-scoped aggregates/roster. Overview gross non-Paid amounts are not net collectible balances; roster includes inactive records. Retain prior finance/currency/cancelled-invoice tests |
| Students.Create | Same-academy branch guard and trimmed names, save then201 summary. Exercise real model binding, tenant gate, audit-after-save failure and fresh persisted read |
| Students.Update | Scoped domain row saved before linked Identity accounts. For each same-academy StudentId-linked account: update active/display name/nonblank email/phone; check UpdateAsync result. Failure returns400 explicitly saying student was saved, with earlier writes retained. UserName unchanged; blank domain email does not clear Identity email. Test zero/one/multiple identities, duplicate Identity email, partial second-account failure and reauthentication after deactivation; no transactional all-or-nothing claim |
| Profiles.Student | Scoped student or404; includes inactive student, linked guardians, all enrollment statuses, grouped attendance, ten invoices by due-date descending, ten practice logs, eight communications and music progress. Administrative notes included; authorization is essential |
| Profiles.UpdateStudentProfile | Scoped student or404; duplicate cleaned number →409; assign nullable metadata, save then re-query full profile. A post-save query/audit failure can produce a failed response after persistence. No Identity updates here |
| Enrollments.Create (dependency only) | Same-academy student exists (no explicit student-active gate); batch tenant/active/open guards, prerequisite completion, capacity/waitlist logic and duplicate active pairing409. Register sends status Active/startDate:null; server defaults UTCtoday. Success clears both selectors; does not prove profile/list has refreshed |
| Batches.List (dependency only) | Same-academy batches plus Active enrollment counts. Register loads this and students with Promise.all, requiring both HTTP responses to succeed; lookup denial blocks student register |

Global AcademyAccessFilter applies. Students uses students.manage, default Core; Owner/AcademyAdmin and granted roles subject to academy/module rules, FrontDesk has student permission. Batches requires batches.manage, Enrollments students.manage. Exercise each endpoint with all12 roles, foreign tenant, revoked/expired grant, disabled module and inactive Identity. FrontDesk's batch lookup dependency must not be solved by broadly granting administrative access. ProfilesController is unmapped and follows the stricter administrative fallback, not students.manage alone; flagged Platform Owner bypass differs from a role-name string. Reuse batch07 fallback analysis and batch01/03 identity/audit findings; no authorization fix during source review.

## G01 — state, save feedback and visual specifications

Register: loading/empty/error/no-search-result, active and inactive tabs, row Open record and status actions, assignment selectors and disabled-save behavior. Status handler reloads before setting success, but a failed reload after successful PUT currently becomes a generic status failure; fresh DB read must determine outcome. No duplicate mutation on retries after ambiguous network failure. Assignment rejects missing IDs, displays server message, clears both IDs on success. Test durable success notice and return/reload behavior without creating duplicate enrollment.

Profile selection risk extends BUG-DATA-0018: select sets current studentId immediately without clearing profile or locking Save. StudentAdminProfile copies old profile on studentId change. Delayed/failed B load can therefore leave A metadata editable under B, and reverse-order responses/save callbacks can overwrite current display. Use distinct synthetic A/B names, student numbers and notes; assert exact PUT target/body, delayed/failed GET, switching during save, empty selection and final fresh A/B reads. Also check shared fee editor after selection; no duplicate issue for the same state-identity defect.

Profile initial academy/student fetches parse JSON without consistent response checks; failed profile GET can leave stale/empty state. Test401/403/404/500, non-JSON/null, session expiration, retry and browser Back/Forward/deep links. Query-derived initial state and notice need hydration/navigation checks; a query notice is not persistence evidence. Save normalizes optional dates to null, parses error JSON defensively, calls onSaved then displays Administrative profile saved. Test malformed success body, callback failure, disabled double submit, focus preservation, dirty navigation warning and whether assistive technology announces result. No runtime success-message PASS is claimed.

Student360 summaries: current-month attendance filters MarkedAtUtc >= UTC month start, not class date and without month-end upper bound. Test late/backdated marking, future marks and local-month boundaries. Invoice projection is limited to10 by due date; UI totals cannot be assumed lifetime balances. It subtracts non-Voided payments but omits adjustments; cancelled rows and mixed currencies need the previously defined finance oracle. Test zero/inactive/missing joins and high-volume data for guardians, enrollment, attendance, fees, practice and communications. Confirm four detail popups match summary labels and underlying rows. No actual PDF/document-output review in this batch.

Desktop/mobile specifications: 320/360/390/768/1280 widths, long names/email/notes,200% zoom, portrait/landscape and keyboard-visible layouts. Register min720 table should scroll locally without trapping page scroll. Profile fields need persistent accessible labels, adequate input contrast, error association and readable success contrast (current amber class needs both themes). Student/gender selectors and date year picker must remain anchored, unclipped and keyboard-operable at top/mid/bottom scroll and inside popups; choose closes selector, Escape restores focus. Detail dialogs need focus trapping/restoration, scroll containment and no overlap with main navigation. Android/iOS physical-device and browser evidence remains NOT RUN.

## Outcome / next boundary

Two new P1 STATIC-FINDING issues (branch erasure and unvalidated branch reference); existing selection-race issue expanded. Counts:32/82 native forms unchanged;156 lexical +54 standalone =210/649 controls;23/70 complete controllers. Profiles, Notifications, Enrollments and Batches remain partial, not counted complete. Phase1 G01/G02/G03 remain open. Next bounded review: teacher administrative profile and remaining ProfilesController actions, reusing payroll/teacher-onboarding mappings; other uncovered modules and document output remain pending. Continue the agreed Astra High audit stage, without app repairs or Phase2 execution.
