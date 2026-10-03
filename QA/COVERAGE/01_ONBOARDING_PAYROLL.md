# Coverage batch 01 — teacher/student intake and payroll

Payroll individual-verdict continuation: [Batch 42](42_PAYROLL_COHORT_VERDICTS.md) reconciles five Payroll actions and two native forms. It retains BUG-DATA-0003/0009 and records distinct BUG-DATA-0056 (historical payout identity changes with profile) and BUG-FUNC-0033 (payroll print has no visible selected payslip). All seven rows remain NOT ACCEPTED and runtime NOT RUN; the historical summary below is not a PASS.

Review date: 2026-09-28. Baseline and source fingerprint remain those in [acceptance](../REPORTS/PHASE_1_ACCEPTANCE.md). This is **STATIC REVIEW / TEST DESIGN**, not executed functional verification. Scope: three routes, four native forms, 59 source field/control occurrences (17 teacher + 28 student + 7 profile + 7 payout). Repeated subject rows and seven weekdays are runtime variants, not new source-field counts. This does not certify Teacher/Student/Finance modules as a whole.

## Source anchors and existing Test IDs

| Form | UI/handler | API/DTO | Storage | Test IDs |
| --- | --- | --- | --- | --- |
| Teacher intake | apps/web/src/app/teacher-onboarding/page.tsx:32 submit; fields :89–131 | apps/api/Controllers/TeachersController.cs:26 Create; CreateTeacherRequest at file end | apps/api/Data/AcademyDeskDbContext.cs:198 Teacher mapping; Domain/Entities/Teacher.cs | TEACHER-FORM-003 (complete form), TEACHER-FORM-001 (async reset), FORM-OPTIONAL-001 |
| Student intake | apps/web/src/app/student-onboarding/page.tsx:30 submit; fields :91–204 | apps/api/Controllers/StudentOnboardingController.cs:17 Create; StudentOnboardingRequest at file end; CreateAccount helper | AcademyDeskDbContext.cs:152 Students, :175 Guardians, :190 StudentGuardians; separate IdentityDbContext | STUDENT-FORM-001, STUDENT-RULE-002, SECURITY-ROLE-002 |
| Payroll profile | apps/web/src/app/payroll/page.tsx:21 createProfile; first form :24 | apps/api/Controllers/PayrollController.cs:18 CreateProfile; :54 Valid; SavePayrollProfileRequest | AcademyDeskDbContext.cs:286 PayrollProfiles | PAYROLL-FORM-001, PAYROLL-RULE-002 |
| Payroll payout | apps/web/src/app/payroll/page.tsx:22 pay; second form :24 | apps/api/Controllers/PayrollController.cs:42 Pay; CreatePayrollPayoutRequest | AcademyDeskDbContext.cs:297 PayrollPayouts | PAYROLL-FORM-002, PAYROLL-RULE-001 |

Other common sources: components/standard-date-field.tsx:43 and standard-select-field.tsx:73 submit hidden string inputs; Security/AcademyAccessFilter.cs; Security/PermissionCatalog.cs; Security/SubscriptionPlanCatalog.cs; Program.cs. All paths relative to repository root except where abbreviated after first mention. These are navigation anchors, not proof of runtime response.

## Boundary conventions used below

- L(N): actual EF string limit N. Cases N−1/N/N+1 after controller trimming; empty, null, omitted, whitespace, padded valid value and Unicode. Required strings reject null/empty/whitespace; nullable strings distinguish null from empty. No HTML maxlength or matching controller length guard was found in these four forms, so overlong rejection must be checked through SQL/HTTP and must not become an unexplained 500.
- D: nullable DateOnly wire contract. Test omitted/null/empty string/valid ISO/malformed ISO/impossible date/leap day. Hidden inputs currently submit empty string; it is not a nullable JSON date. Required date behavior must be tested: StandardDateField's required prop only disables Clear; the hidden input is not native required validation.
- M: decimal(18,2). Test negative/zero/0.01/1.00, 1.005, largest representable 9999999999999999.99 and overflow 10000000000000000.00 through raw JSON, plus browser Number precision separately. Capture database rounding and response consistency; financial rounding policy is not approved by this review.
- I: nullable Int32 wire type. Test null/omitted/0/−1/1/1.5/2147483647/2147483648; do not apply decimal limits to counts.
- All direct-API cases include wrong JSON types, no unauthorized writes, malformed/unknown IDs where meaningful, response schema and fresh-context persistence. Never infer browser behavior from a DTO annotation alone.

## Teacher intake: all 17 source controls

Submit serializes Object.fromEntries(FormData), then overwrites specialties/availabilityJson/certificationsJson. Ordinary named input strings are not normalized client-side. Subject/certification fields have no name and are taken from React state. The following tests belong to TEACHER-FORM-003, with optional-date variants under FORM-OPTIONAL-001.

| UI control(s) | Request → storage and actual rule | Specific test obligations |
| --- | --- | --- |
| firstName, lastName | Required text → non-null string FirstName/LastName → Teacher, Trim; IsNullOrWhiteSpace rejects; L(120) each | Browser blank/whitespace versus API rejection; names with Unicode/apostrophes; both boundaries |
| email | Required HTML email → nullable string Email → Trim, L(320); no email-format/required server guard in Create | Browser missing/malformed rejects; direct API null/omitted/malformed behavior recorded; duplicate email not a unique Teacher key. Required UI vs optional API is a policy question, not assume same rule |
| phone | Optional string Phone → Trim, L(30) | Formatting/leading +/Unicode retained; no numeric conversion; L(30) |
| addressLine1 | Optional string AddressLine1 → Trim, L(240) | L(240), newline/Unicode; no invented street-format rule |
| city, state | Optional strings City/State → Trim, L(100) each | L(100), whitespace-only persists empty currently |
| postalCode | Optional string PostalCode → Trim, L(30) | Leading zeros preserved; international letters/spaces; no numeric restriction |
| qualifications | Optional string Qualifications → Trim, L(1000) | L(1000), long multi-line value |
| subject row | Required unnamed text; state trimmed; blank rows removed; at least one nonblank subject required client-side. Joined with comma+space → nullable Specialties, L(500); also subject in CertificationsJson | One/many/remove-last prevention; blank added row; duplicate/case variants; joined string at 499/500/501, not just individual subject length. API does not enforce subject requiredness |
| certification row | Optional unnamed text; Trim or null within CertificationsJson string; no explicit EF limit for this JSON column | Subject→certificate pairing after add/remove/reorder; quotes/backslash/Unicode survive JSON; direct API malformed/literal-null/array/object JSON string preservation and consuming reader behavior |
| employmentType | Controlled hidden string, default Full-time; options Full-time/Part-time/Contract → nullable string EmploymentType → Trim, L(50); no enum guard server-side | Each option, empty, tampered unknown and casing; record current acceptance vs approved employment policy |
| dateOfBirth, joiningDate | Optional hidden strings → DateOnly? → nullable date columns, no relative-date guards in Create | D for each; future DOB/joining-before-birth explicit policy review; empty-date wire failure BUG-API-0001 |
| available-${day} | Seven checkbox variants; form.get==='on'; only checked weekdays retained in availabilityJson (nullable string column without explicit EF cap) | None/one/all seven; toggle off removes payload entry; unknown extra fields must not become teacher properties |
| from-${day}, to-${day} | Native time strings; each checked weekday gets from/to strings, including empty; unchecked days excluded from availabilityJson | 00:00/23:59, missing pair, equal/reversed interval, overnight policy, browser locale AM/PM vs HH:mm wire. Server stores JSON without interval validation; do not invent a rejection expectation until policy approved |

API-only CreateTeacherRequest fields not shown here: BranchId (Guid?, if supplied must belong to academy), CompensationJson (nullable string, stored raw). Both need omitted/null/unknown/malformed/other-tenant variants at API layer. Creating a Teacher record does not create a portal Identity user. The 201 response TeacherSummary contains id, firstName, lastName, email, phone, specialties, branchId, isActive; it does not return all intake fields. Verify other values from a fresh SQL context, not missing response fields.

## Student intake: all 28 source controls

Body uses Object.fromEntries(FormData), then explicitly replaces five checkbox values with booleans. StudentOnboardingRequest string inputs are nullable except names; guards add conditional requiredness. Tests belong to STUDENT-FORM-001 unless specified.

| UI control(s) | Request → storage and actual rule | Specific test obligations |
| --- | --- | --- |
| studentFirstName, studentLastName | Required HTML text → StudentFirstName/StudentLastName → Student.FirstName/LastName, Trim + required guard, L(120) | Required and L(120) cases independently |
| studentNumber | Optional string StudentNumber → Trim, Student.StudentNumber L(50); unique (AcademyId,StudentNumber) where number IS NOT NULL | L(50), same/cross-tenant explicit duplicates; two successive blank and whitespace values vs null/omitted controls. BUG-DATA-0008 / STUDENT-RULE-002 |
| preferredName | Optional string PreferredName → Trim, L(120) | L(120) |
| gender | Optional hidden string → Gender → Trim, L(50); listed options Female/Male/Non-binary/Prefer not to say; no server enumeration guard | All listed options plus empty/unknown/case variants; L(50) |
| dateOfBirth | Required custom date → DateOnly? DateOfBirth → Student.DateOfBirth; server requires non-null and <= UTC today | D plus tomorrow rejected, today, exactly 18th birthday ±1 day, Feb29 birthday and UTC/local midnight; frontend local Date calculation may disagree with server AddYears(18) |
| admissionDate | Optional hidden string → DateOnly? AdmissionDate → supplied date or UTC today | D; server default on null/omitted; blank currently not null. Relation to DOB has no guard here; record policy question |
| studentEmail | Optional HTML email → string? StudentEmail → Student.Email Trim L(320) | Blank allowed browser; malformed direct API contract; no duplicate-email uniqueness on student record |
| studentPhone | Optional string StudentPhone → Student.Phone Trim L(30) | L(30), retain leading zero/+ |
| studentAddressLine1 | Optional string → Student.AddressLine1 Trim L(240) | L(240) |
| studentCity, studentState | Optional strings → Student.City/State Trim L(100) each | L(100) |
| studentPostalCode | Optional string → Student.PostalCode Trim L(30) | L(30), alphanumeric international codes |
| emergencyContactName | Optional string → Student.EmergencyContactName Trim L(160) | L(160); no server pair-required rule |
| emergencyContactPhone | Optional string → Student.EmergencyContactPhone Trim L(30) | L(30); name-only/phone-only controls |
| medicalOrAccessibilityNotes | Optional textarea → nullable string → Trim, L(4000) | L(4000), multiline/Unicode; sensitive field must remain within intended read scopes, not just creation |
| parentFirstName, parentLastName | HTML required when minor. Nullable DTO strings → Guardian names Trim, L(120). Minor requires both; adult with ANY of six parent identity/contact inputs filled also requires both | Minor neither/one/both; adult all empty vs phone/email/address/city only; whitespace-only does not trigger hasParentDetails; L(120) |
| parentEmail | HTML email, required for minor → nullable string → Guardian.Email Trim L(320) | Minor null/empty rejected; adult full names with no email permitted unless account helper invoked; browser malformed vs direct API; L(320) |
| parentPhone | Optional string → Guardian.Phone Trim L(30); nonblank participates in hasParentDetails | L(30); phone-only adult rejects missing parent names |
| parentAddressLine1 | Optional string → Guardian.AddressLine1 Trim L(240); participates in hasParentDetails | L(240); address-only adult guard |
| parentCity | Optional string → Guardian.City Trim L(100); participates in hasParentDetails | L(100); city-only adult guard |
| relationship | Default Parent string → StudentGuardian.Relationship Trim or Parent fallback, L(50) | Null/empty/whitespace fallback; L(50); relationship alone does NOT trigger guardian creation |
| allowParentPortalAccess | Checkbox defaultChecked minor, disabled for minor. Client sends false when disabled/omitted. DTO bool default false. Server parentAccess = isMinor OR request value | Minor always access when guardian exists; adult checked/unchecked; switch DOB minor→adult and back: defaultChecked is not controlled state. Inspect UI and actual boolean, no guessed consent policy |
| allowAcademicProgress | Checkbox default true → bool AllowAcademicProgress default true → CanViewAcademicProgress = parentAccess AND flag | Both values; true flags cannot override parentAccess false |
| allowFinance | Checkbox default true → bool AllowFinance default true → CanViewFinance = parentAccess AND flag | Same truth table; linked family financial read must honor flag in later portal suite |
| allowDocuments | Checkbox default true → bool AllowDocuments default true → CanViewDocuments = parentAccess AND flag | Same truth table; direct download authorization tested separately |
| allowLeave | Checkbox default true → bool AllowLeave default true → CanManageLeave = parentAccess AND flag | Same truth table; no guardian row means no access row, regardless of checked flags |

API-only fields: BranchId (Guid?, assigned without same-tenant branch check in this action), StudentUserName/StudentTemporaryPassword and ParentUserName/ParentTemporaryPassword (nullable strings, not present on this form). Account helper runs only when both username and password are nonblank, with a parent record for parent account. It checks duplicate username/email, uses Identity password rules and assigns Student/Guardian role; AddToRole result is not checked. Username is trimmed after uniqueness lookup; fallback student email applies only when StudentEmail is null, not empty string. Identity rules/indices belong to API account variants, not invented hidden UI fields. Existing BUG-DATA-0004 and BUG-DATA-0007 track transaction and response-flag risks; role assignment and helper failure must be fault-injected in Phase 2.

Successful intended persistence: one Student; zero/one Guardian; matching StudentGuardian FK pair, primary flag, permissions and granted timestamp; optional accounts only when actually created. Response is 200 JSON {id, firstName, lastName, isMinor, parentId, studentAccountCreated, parentAccountCreated}; verify correct casing/types/non-null id, then notice/redirect/reload. Empty/malformed success response must not be treated as confirmed save. Failed transaction must leave no orphan relational or Identity records.

## Payroll profile: all seven source controls

PAYROLL-FORM-001. Explicit JSON body uses React selection state and Number(form.get(...)); hidden fields are not blindly spread. DTO SavePayrollProfileRequest and Valid helper apply to POST and PUT.

| UI control | Request → storage and actual rule | Specific test obligations |
| --- | --- | --- |
| workerType | Hidden string Teacher/Staff → non-null string WorkerType, exact enum guard; L(20) | Both values, unknown/case/blank/null; switching clears workerId; request uses correct ID slot |
| workerId | State selection; becomes TeacherId Guid? OR StaffUserId string?, other null; workerName from current loaded list → required string L(240) | No selection client-blocked; stale/deleted/unknown/cross-tenant and both/null links API tests. TeacherId create only checks supplied ID; StaffUserId L(450) not existence checked; PUT does neither. BUG-DATA-0009 |
| paymentModel | Monthly/SessionBlock hidden value → string PaymentModel exact enum guard, L(20) | Switch each way after filling amounts; inactive model payload must be null; UI stale values must not leak |
| monthlyAmount | Monthly-only required number min1 step.01 → Number → decimal?; Valid >0 for Monthly → decimal(18,2) | M; 0.01 API accepted by positive guard but UI min1 rejects; null/omitted/blank→0 rejected; non-Monthly stored null |
| sessionsPerCycle | SessionBlock-only required number min1, default integer step → Number → int?; Valid >0 → SQL int | I; hidden for Monthly and stored null; large count limits not invented |
| amountPerCycle | SessionBlock-only required number min1 step.01 → Number → decimal?; Valid >0 → decimal(18,2) | M; 0.01 UI/API distinction; Monthly stored null |
| effectiveFrom | Required custom date and explicit !effectiveFrom client guard → DateOnly?; server null defaults UTC today on create, preserves existing on update | D; future/past policy absent; cleared/missing client-blocked; directly omitted valid per server default |

Client also sends isActive:true. Create uses entity default true, ignoring request.IsActive; Update assigns request flag. This difference is a contract variant to review before implementation, not a UI control. Profile response must return generated id, exact worker/model/amount/date/active values; no assumption that creation Location has an implemented GET-by-id route. Indices on academy/worker IDs are nonunique; repeated POST needs duplicate-submission policy review, not an assumed database uniqueness rule.

## Payroll payout: all seven source controls

PAYROLL-FORM-002; negative-net variants PAYROLL-RULE-001.

| UI control | Request → storage and actual rule | Specific test obligations |
| --- | --- | --- |
| payrollProfileId | Active-profile selection → non-null Guid → lookup same-tenant AND active profile | Empty blocked UI; Guid.Empty/malformed/unknown/other-tenant/inactive rejected; record deactivation between load and submit |
| periodLabel | Required text → non-null string; whitespace rejected; Trim → L(120) | Required, L(120); no parsed date-period rule or uniqueness guard: repeat/overlapping period cases need explicit policy |
| deductions | Optional number min0 step.01 default0 → Number(value or0) → decimal; server rejects <0; stored decimal(18,2) | M plus gross−.01/gross/gross+.01. Current code permits net<0 (BUG-DATA-0003); zero-net policy must be agreed |
| sessionsCovered | SessionBlock-only required integer min1 → Number → int?; server fallback profile.SessionsPerCycle, must >0; Monthly ignores | I; no proof of attendance completion or cycle multiple in this action; test fallback distinct from empty browser Number=0 |
| grossAmount | SessionBlock-only required number min1 step.01 → decimal?; fallback profile.AmountPerCycle; Monthly ignores request and uses MonthlyAmount | M; gross>0; null fallback vs explicit0; computed net persisted and serialized consistently |
| paymentMethod | Hidden selection BankTransfer/UPI/Cash/Cheque → string?; Trim or BankTransfer fallback; L(30), no enum guard | Every option, omitted/null/whitespace fallback, unknown API value and L(30) |
| reference | Optional text → string?; Clean = whitespace→null else Trim, L(150) | L(150); duplicate reference not unique in model; verify null for blank |

API-only paidAtUtc nullable DateTime: client sends null; server UTC now fallback. Test explicit zone offsets and invalid timestamp; never use wall-clock-sensitive equality. Entity defaults currency INR, status Paid; no payment gateway is called. Payout has unique (academy,payslipNumber), not a pay-period idempotency key. Return 201 typed summary, exact gross/deductions/net and DB row. No new payout for invalid profile/negative deductions. Completed-session calculation/rounding/duplicate-period policy needs domain confirmation before declaring a rule regression passing.

## G01: specific UI state and interaction specifications

| Form | Implemented branches and required tests | Current feedback risks / non-applicable states |
| --- | --- | --- |
| Teacher intake | Loading academies → first academy or create-first notice; load failure; missing subject; dynamic add/remove rows; selected availability; employment/date selectors; saving disables submit; non-OK body message; thrown request; intended success resets controlled+native values and displays Teacher onboarded | Async currentTarget.reset after await blocks success (BUG-FUNC-0001). No redirect exists here: return expectation is same cleared form, not an invented navigation. No search/table/empty-directory/modal branch on this page |
| Student intake | Loading/empty/failure; minor/adult helper+required parent fields; parent access disabled for minor; saving guard; server error; intended success redirects with studentId and notice to student-management; reload should show persisted data | BUG-FUNC-0001; nullable result.id dereference; no special timeout/offline/session-expiry recovery UI, exercise generic failure with retained values; no portal-account controls on this form |
| Payroll profile | Initial loading/empty academy/failure; Teacher/Staff choice; Monthly/SessionBlock conditional fields; worker/effective date enablement; non-OK notice; intended reset/load/success | No saving state/catch around fetch/reload; duplicate taps and network rejection must be tested; async reset BUG-FUNC-0001. Failed reload after committed POST can mask outcome; do not retry automatically |
| Payroll payout | Active profile list empty/one/many; selected model changes conditional fields; inactive profile stale state; no profile client guard; non-OK notice; intended reset/load/success; register empty/populated and print action | No saving disablement/catch; async reset. Print is window.print of page, not a distinct server-generated PDF; no new document-export workflow assumed |

For every selector/date here: apply UI-DROPDOWN-001/UI-SCROLL-001 protocol (six scroll positions, viewport-edge triggers, nested scrolling, resize, clipping, hit-test, keyboard/focus). Desktop/mobile checks remain NOT RUN. Teacher native time controls need platform-specific behavior tests, not screenshots assumed to represent every OS. No upload UI occurs in these forms. Successful SQL plus failed client rendering is an ambiguous save, not permission to submit again.

## G03: effective permissions for the three reviewed controllers

Applies to valid route/body requests; malformed binding may short-circuit before action filters. Test that separately. All active users go through Program inactive-user checks. Missing resolved identity returns 401 at AcademyAccessFilter; inactive users are 403. Ordinary users must match academy; academy must exist and be active. Platform-owner flag bypasses those filter checks, not middleware or action guards.

| Action(s) | Required module for ordinary admin | Effective allow | Effective deny / override cases |
| --- | --- | --- | --- |
| Teachers.List/Create/Update | Core (Allows always true for Core, even absent module JSON) | Same-tenant Owner/AcademyAdmin; active flagged PlatformOwner through filter | All other roles, including workforce.manage custom/granted users: TeachersController has no catalog entry. PlatformOwner role name without flag gives no bypass. Create also checks academy exists, supplied branch same academy; Update must find same-tenant teacher |
| StudentOnboarding.Create | Core | Same-tenant Owner/AcademyAdmin | Sales/Marketing/FrontDesk or custom students.onboard pass filter then fail action (BUG-FUNC-0002); Teacher/Student/Guardian and other ungranted staff fail. Flagged PlatformOwner still needs matching actor.AcademyId AND Owner/AcademyAdmin role at action. Role name alone insufficient |
| Payroll.Profiles/CreateProfile/UpdateProfile/Payouts/Pay | Finance in EnabledModulesJson (not inferred from plan display name) | Same-tenant Owner/AcademyAdmin with module; active flagged PlatformOwner bypasses module/tenant filter | FinanceUser/custom finance.manage denied because PayrollController is unmapped in PermissionCatalog. Disabled/missing academy or Finance module denies ordinary admin. Target profile lookup/worker guard still applies inside actions |

For each action run actor A/B, all twelve roles, role-only vs flag-only platform owner, custom grant, expiry/revocation, missing/inactive user, inactive/missing academy and module enabled/disabled. Assert records/bodies scoped and no unauthorized mutation. Platform-owner bypass skips ExecuteAndAuditAsync; therefore audit expectations differ for that path and must be separately recorded. UI permissions do not substitute for direct API checks. Intended role-policy changes require confirmation; tests first record current implemented behavior and catalog conflicts.

## Result and next bounded batch

The four forms' source rules, conditional states and the three controllers' effective permission paths are now explicitly specified. These are not runtime passes or closure of G01–G03 across the whole app. Remaining: other 78 native forms, controlled editors (including teacher-management save/toggle/assignment), remaining controller/helper permissions, framework Identity surface and full field mapping. Next risk-first batch: invoice/payment/reconciliation/adjustments and their role gates. Stay on Astra for this review; use Sol when implementation starts after formal acceptance.
