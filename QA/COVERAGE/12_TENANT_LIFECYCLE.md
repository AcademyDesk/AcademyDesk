# Batch 12 — Tenant creation, discovery and subscription configuration

2026-09-28. Source review; runtime NOT RUN. Scope: PlatformAcademiesController (all four actions), platform/page.tsx academy popup/status/portfolio, and platform/control/page.tsx tenant creation/discovery/information/configuration. PlatformControlController is PARTIAL: GetTenantOnboarding/SaveTenantOnboarding plus GetSettings/SaveSettings default-trial dependency only. Other platform admin/billing/support/announcement/settings/audit workflows and AcademyPlatformServicesController remain pending. Reading their source during discovery does not count them complete.

Stable cases PLATFORM-API-006/007/008/009, PLATFORM-API-013/014 and PLATFORM-FORM-001/002/003/010; VISUAL-PAGE-052/053 for the specified states. Four native forms add68 lexical controls (32+28+3+5); three standalone controls add portfolio query and two academy selectors. Cumulative36/82 forms,299/649 controls,25/70 complete controllers. Existing role-assignment and selection-integrity findings are extended rather than duplicated.

## G02 — academy and tenant intake fields

| Field(s) | Mapping, guard and boundary |
| --- | --- |
| academyName (both create forms) | Required text → trimmed Academy.Name; EF max200. Empty/whitespace rejected server-side. Test same-name tenants, Unicode and max-1/max/max+1 |
| legalName | Optional → UI null when empty; API trims, EF max250. No requirement to invent a legal name |
| adminUserName | Required email input; API additionally permits non-email username, converting it to username@academydesk.local. Checks FindByEmailAsync before create; test case/whitespace, global duplicate, normalized username collision, concurrent create and invalid address |
| adminDisplayName | Optional; fallback to requested login. Identity display-name limits need retained account mapping; verify escaped display and no blank/unusable login |
| password / temporaryPassword | Required password, UI min8; backend checks nonblank then Identity policy. Test policy boundaries, rejected password, duplicated clicks and lost response. Never log fixture passwords or show them in audit metadata |
| primaryContactName / primaryContactRole | Optional trimmed strings; EF max200/120 respectively |
| primaryContactEmail / primaryContactPhone | Optional; browser email type only for email. EF max320/40; preserve phone plus and leading zeroes. Server does not explicitly validate email format |
| country / state / city / postalCode | Discovery text independent from Academy.CountryCode; EF max120/120/120/24. Test international text and alphanumeric postal codes |
| addressLine1 / addressLine2 | Optional trimmed strings, EF max300 each |
| businessType / operatingSince / website | Optional text; EF max120/20/500. OperatingSince is not a date field; website accepts free text/social references, not an automatically fetched URL |
| branchSummary | Optional trimmed description, no explicit max length in this mapping; does not create Branch rows |
| financeModel / billingFrequency / teacherPaymentModels | Optional descriptive text; EF max160/120/300. Does not configure operational fee or compensation rules |
| paymentCollectionMethods | Optional descriptive text; no explicit max length in this mapping; does not connect a payment provider |
| deliveryModes / classRatios / batchAndClassSetup | Optional descriptive strings; no explicit max lengths here. Ratios are not parsed; these inputs do not create classes/batches |
| teacherCount / studentCount / subjectCount | Optional number inputs min0, no decimal step (integer default). Blank becomes null;0 preserved; API nullable int has no nonnegative guard. Test -1,0,1,fraction,Int32 bounds and malformed type; these are declared discovery counts, not actual roster totals |
| subjectTypes | Optional descriptive text, no explicit max length here; independent of Course records |
| documentsJson | Optional textarea, UI permits document descriptions/secure reference text; default [] when blank. API stores string without JSON parse, EF max8000. Display is text; no upload/download occurs in this form. Do not impose a JSON requirement on users without a clarified contract |
| operationalNotes | Optional text, no explicit max length here; multiline/Unicode/HTML-looking data must render as text |
| academy (edit form) | SelectedAcademy GUID routes discovery PUT; not a profile field. Compare selector/header/data identity under delay/error |

PLATFORM-FORM-010 is Overview's five-field popup. PLATFORM-FORM-001 combines five setup fields and27 discovery fields. PLATFORM-FORM-002 has academy selector plus27 discovery fields. All discovery fields are optional; for each string test null/empty/whitespace, cleared value readback, explicit EF length boundaries where present and safely bounded large payloads elsewhere. Create text helper normalizes blanks to null; update helper preserves existing values only when a control is absent, and normalizes present blank to null. SaveTenantOnboarding API merely trims optional strings. Counts must not convert blank into zero. No runtime null/save success is inferred.

API-only Status defaults InProgress and CurrentSection defaults Personal when blank; max30/40. Create UI sends InProgress/Complete, existing editor sends InProgress/current onboardingSection (initial Personal). This is metadata, not proof all sections are complete. Unknown status/section and overlength values require tests. Unique AcademyId index permits one onboarding profile per academy; concurrent first PUT needs conflict/SQL error handling tests.

API-only countryCode defaults IN, otherwise uppercases trimmed text; EF max2 without ISO allowlist. TimeZone exists on creation DTO but Onboard hardcodes Asia/Kolkata. UI currently sends that same value. Test direct non-India requests and document the discrepancy before international rollout. New academy defaults IsActive=true and Trial with catalog limits/modules. Trial expiration hardcodes UTCnow+30 despite persisted DefaultTrialDays (BUG-FUNC-0009).

## G02 — configuration and standalone fields

PLATFORM-FORM-003: plan → subscriptionPlan, UI Trial/Launch/Growth/Professional/Enterprise; status → subscriptionStatus, UI Trial/Active/Past due/Cancelled; endsAt → subscriptionEndsAtUtc nullable date. API requires nonblank plan/status. Catalog lookup is case-insensitive but does not trim and defaults unknown values to Launch (BUG-DATA-0022). Status is trimmed free text max50; no allowlist. Test missing/blank/case/whitespace/unknown plan, status and date, leap day, clearing end date, past expiry, date-only serialization and UTC/local interpretation.

Configure overwrites limits/modules from the chosen plan: Trial75/15; Launch75/15; Growth250/40; Professional750/120; Enterprise5000/1000 (student/staff). Verify exact module sets from SubscriptionPlanCatalog and permitted routes after upgrade/downgrade. No deletion of excess students/staff is performed. Source search finds limits/expiry/status stored and reported, while the global academy gate checks IsActive and enabled modules; no automatic expiry/status/capacity enforcement was established. Define grace/cancellation/quota policy before writing expected denial tests. Explicit suspension is a different flag and should be tested independently.

Standalone query on /platform filters academy name/legalName client-side. Academy selectors at control source lines660 (Tenant Information) and677 (Tenant Management) share selectedAcademy; source-line identities distinguish them. Test no data, unknown ID, long names, duplicate names, search case/whitespace, clearing, switching between tabs and unsaved changes. Tenant Active tile counts SubscriptionStatus==Active, whereas Overview Active counts IsActive; test suspended-but-Active and active Trial and clearly identify the differing definitions.

## G03 — access, identity and persistence

PlatformAcademiesController uses Authorize and checks ApplicationUser.IsPlatformOwner in every action. Role name alone is insufficient. List/Onboard have no academyId argument and rely on this explicit owner check. SetStatus/Configure also pass through the global academy filter; true platform flag bypasses tenant/module gate, then controller checks flag again. Test anonymous401, authenticated nonowner403, AcademyAdmin/Owner/custom-role denial, flag-only owner, inactive Identity, invalid GUID404 and foreign/suspended tenant targets. No UI warning should substitute for server enforcement.

List returns all tenants sorted by name, branch count (all), active student count and Staff count of active Teachers only. Staff Identity accounts are not included in that last count. Test empty/large lists, inactive rows and counts with staff-only accounts; clarify quota semantics separately. Onboard validates name/login/password, prechecks global email, creates role if absent, saves Academy, creates Identity user, assigns role, audits and returns201. EmailConfirmed=true is set by provisioning; password-change enforcement is not established here. IdentityResult failures from role creation/assignment are unchecked: extend BUG-DATA-0011 to AcademyAdmin provisioning. Inject rejected result and thrown exception separately and inspect academy/user/role/audit rows.

Failed CreateAsync attempts to remove the already saved academy; cleanup failure or cancellation can leave partial state. Thrown failures after successful identity create have no enclosing cross-context transaction. Audit failure can return error after provisioning. Test fresh rows, credentials and controlled retry at each boundary; UI success must mean usable intended role. Never repeat creation blindly after ambiguous failure. SetStatus modifies IsActive and audit in one domain Save; no user deletion or Identity deactivation. Verify existing academy requests deny after suspension and reactivation restores according to policy, reusing session/global-gate tests.

Discovery GET checks owner and academy then returns existing profile or null. Depending on ASP.NET null response formatting, client tolerates empty response text. Discovery PUT checks owner and academy, creates/updates profile, adds platform audit and saves once. No explicit validation of negative counts or discovery completeness. PlatformControlController remains partial; other endpoints' detailed field/state specifications are upcoming.

## G01 — two-step onboarding, selection and feedback

Overview popup creates academy/admin only, closes and clears inputs, reloads portfolio, then displays success directing user to Tenant Onboarding. Test blocked double submit, cancel/backdrop handling, failed validation preserving inputs, success popup closure and reload error after successful creation. It does not navigate automatically to the newly created tenant; default selected tenant in control is first returned academy. Require an unambiguous way to choose the intended tenant during browser testing.

Combined create performs POST academy/admin then PUT discovery. If second call fails, academy already exists;401/403 clears tokens and reports partial creation, other failures display partial-save error. startingTenant and selectedAcademy change only after successful details response, so test recovery to the created tenant, duplicate-login retry conflict and preservation of all entered details. A null/malformed successful create payload must not dispatch undefined tenant ID. No rollback across these calls is claimed.

Existing discovery selection clears tenantProfile and remounts uncontrolled form, but fetch has no cancellation/identity check and Save is not disabled during load. Failed GET becomes undefined just like an absent profile, permitting blank replacement of existing data. Out-of-order A response can populate B's keyed form and be saved to B; late response also remounts away unsaved typing. Extend BUG-DATA-0018 to this tenant risk. Test delayed/failed/reversed loads, switching during save, typing while loading, unique index conflict and exact target/body/fresh A/B rows. Existing person-level regression IDs are retained, with tenant variants explicitly added.

saveTenant configuration uses controlled values synchronized from selected academy, then shared request helper reloads all controls and sets success. Test switching during PUT, failed reload after commit, kept edits/error messages, duplicate actions and no misleading generic failure. Discovery saves parse error body defensively and display explicit429 text;401/403 currently described as expired session even if actual cause is forbidden access. Test genuine expired session versus permission revocation without weakening access. Message placement/announcement must remain visible at end of long form.

## Visual and navigation requirements

Test Overview popup, one-sheet discovery, Tenant Information cards and configuration at320/360/390/768/1280 widths and200% zoom. Verify visible labels/input boundaries, section spacing, long text wrapping, numeric0 versus missing data, title capitalization, no nested onboarding sidebar and a single clear submit per form. Browser/physical Android/iOS tests remain NOT RUN.

Main-section navigation must work directly between query tabs, update active label, support Back/Forward/deep link/start=true and avoid resetting selected tenant unexpectedly. Dropdowns/date picker must anchor above panels at all scroll positions, close on selection, support year selection and keyboard Escape/focus return. Modals need focus trap, accessible name, body/inner scroll and restore trigger focus; long discovery page needs full scrolling with keyboard visible. Cancel must preserve or clearly discard dirty inputs. Stale financial/health fallback cards from failed dependencies need separate overview tests; billing/health workflows are not closed by this tenant batch.

## Outcome

Two new static findings: unknown plan silently resets configuration to Launch; configured trial duration ignored. Existing provisioning and record-selection findings extended. No application changes, customer writes or runtime test execution. Phase1 remains NOT CLOSED. Next bounded review: remaining platform administration (admin accounts, announcements, settings/audit), followed by platform billing/support and academy configuration; these subdivisions are part of the broad platform work group and are not a fixed remaining-turn estimate.
