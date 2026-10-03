# Coverage batch 04 — staff and access governance

2026-09-28. **STATIC REVIEW / TEST DESIGN. Runtime NOT RUN.** Baseline remains pinned in [acceptance](../REPORTS/PHASE_1_ACCEPTANCE.md). Three native forms / 11 controls, one repeated standalone role selector, and all 14 actions in StaffController, AcademyRolesController, AccessGrantsController and AccessReviewsController. Repeated permission checkboxes are one source declaration with 12 runtime variants. Visual requirements below are source-derived; screenshots and browser interactions have not been verified.

## Traceability

UI sources: `apps/web/src/app/staff/page.tsx`, `access-review/page.tsx`, `access-review/sign-off/page.tsx`; relevant styles in `apps/web/src/app/globals.css:5101,5108`. APIs: corresponding four files under `apps/api/Controllers`. Shared guards: `Security/AcademyAccessFilter.cs`, `PermissionCatalog.cs`, `SubscriptionPlanCatalog.cs`, Program active-user middleware. Storage: `Domain/Identity/{ApplicationUser,AccessGrant}.cs`, `Data/IdentityDbContext.cs`, Identity migration snapshot; `Domain/Entities/{AccessReview,AdminWorkItem}.cs` and application mapping/snapshot.

| Scope | Stable Test IDs |
| --- | --- |
| Staff creation form | AUTH-FORM-006 |
| Grant form | AUTH-FORM-001 |
| Access-review sign-off form | AUTH-FORM-002 |
| Custom role list/create/assign | AUTH-API-001 / 002 / 003 |
| Grant list/create/revoke | AUTH-API-004 / 005 / 006 |
| Staff list/create/status/role/password/offboard | AUTH-API-012 through 017 |
| Review list/sign-off | OPERATIONS-API-002 / 003 |
| New distinct regressions | SECURITY-ROLE-004, AUTH-ROLE-001, AUTH-GRANT-001, AUTH-REVIEW-001 |

No custom-role UI caller was found by searching web source for role API/roleId/permissionsJson. These endpoints still need direct contract/access tests. This absence does not create an invented form or prove the feature is intentionally API-only.

## G02 — all form controls

Boundary convention: test omitted/null/empty/whitespace, valid value, wrong JSON type and N−1/N/N+1 for actual limits. All GUID tests include malformed, zero, unknown, wrong academy and wrong kind. Every rejected mutation must leave fresh database state unchanged. Identity/SQL failure variants require isolated fixtures. Credentials must be excluded from logs.

| Control | Payload, rules and storage | Specific cases |
| --- | --- | --- |
| Staff displayName | Required input → DisplayName Trim; nonblank guard; Identity max 200 | Whitespace, Unicode, overlong after trim; no controller length guard |
| Staff email | Required HTML email → Email and UserName Trim; nonblank then Identity validation; max 256; global normalized username uniqueness | Case/whitespace and duplicate across same/different academy; distinguish duplicate username from nonunique academy/email index |
| Staff password | Required minLength 6 → unchanged Password; nonblank then configured Identity validators | Composition and length; do not assume six arbitrary characters accepted. No busy guard in create |
| Staff staff-role | StandardSelectField → Role; six roles accepted case-insensitively then canonicalized: Manager, Operations, Sales, Marketing, FinanceUser, FrontDesk | Unknown/null/Owner/custom rejected in this endpoint; default Operations retained after success |
| Grant userId | StandardSelectField → Guid UserId; must be active same-academy Identity user | UI options from staff endpoint exclude Teacher/Student/Guardian; API only checks active user/academy and can accept those roles. Intended recipient policy needs decision |
| Grant permissions checkbox declaration | FormData on-values → array of 12 visible permission keys; server Distinct then exact catalog allowlist | Empty/all-invalid reject; mixed invalid discarded; duplicates collapse. API also permits communications.manage/settings.manage, absent from this UI; test casing and future permissions |
| Grant permanent checkbox | Controlled Boolean → IsPermanent | False requires future expiry; true forces stored expiry null even if supplied; changing selection hides date but does not immediately clear date state |
| Grant expiresAtUtc | Conditional StandardDateField → local end-of-day 23:59:59 converted to ISO → nullable DateTimeOffset | Missing/past/now/future; invalid JSON datetime; browser timezone and DST. Display uses IST while construction uses browser timezone, so policy must specify intended timezone |
| Grant reason | Optional textarea → nullable Reason Trim; max 500 | Blank remains empty rather than null; 499/500/501, multiline/Unicode; no HTML/controller length bound |
| Sign-off notes | Required textarea → nullable Notes Trim; API accepts blank/null; Notes nvarchar(max) | UI vs API requiredness; long notes alone versus follow-up: copied AdminWorkItem.Description has max 4000; 3999/4000/4001 with followUp on/off |
| Sign-off followUp | Checkbox → CreateFollowUp Boolean | False writes review only; true also creates High-priority Open AccessReview work item in same application SaveChanges. No assigned user, EntityId or reviewer set |

Standalone control: `staff-role-${person.id}` selector, one source declaration per directory list. It binds only person.roles[0] (fallback Operations), offers six standard roles, and disables for inactive/busy row. Owner/custom/multiple-role records can have no matching option or omit actual memberships. Selection triggers PATCH immediately; no confirmation step exists. Count this once in source coverage, with all runtime role variants.

## API contracts beyond the forms

Staff.Create additionally accepts nullable TeacherId with no existence/tenant/activity check; linking must be tested against teacher-resource authorization from batch 03. Same-tenant link authority is a policy choice; wrong-tenant/missing links need explicit validation expectations. Create returns 201 summary/Location; no GET-by-ID action is defined. Role creation and assignment results are checked; failed assignment attempts DeleteAsync cleanup but ignores deletion result. Test failed cleanup and thrown store errors, rather than incorrectly applying PortalAccounts' unchecked role-add finding to this controller.

Staff.UpdateStatus accepts IsActive, rejects target equal to caller even for activation, scopes target to academy but not staff role. UpdateAsync result is discarded; inject failure and compare returned IsActive with persisted state. Staff.ResetPassword requires at least eight characters plus Identity validators, accepts any same-academy target including caller and privileged/non-staff accounts; management policy must define allowed targets. Staff.Offboard blocks self, sets inactive and indefinite lockout, updates security stamp then user. Final result checked, stamp result unchecked. Reactivation via status does not clear indefinite lockout. Test status/lockout/refresh/access independently; do not treat message “sessions invalidated” as runtime proof.

Staff.UpdateRole removes only six standard roles, retains custom/protected/Teacher roles, adds new role, updates stamp and returns only requested role. Existing custom permissions remain effective: BUG-SEC-0004. Remove/add are separate writes, so failed add can leave old standard roles removed. AcademyRoles.Assign uses a different rule: removes everything except Owner/AcademyAdmin/Student/Guardian, ignores removal result, then adds selected role without stamp update. Failure and concurrent-assignment cases must inspect actual memberships; do not assume atomic replacement.

AcademyRoles.Create: Name required, raw length ≤80 before Trim (test padded 80/81), duplicate precheck within academy. Identity Name/NormalizedName max 256; global unique NormalizedName plus tenant Name index means two academies cannot reuse normalized name (BUG-DATA-0012). Permissions nullable, Distinct, serialized max 4000, no catalog filtering; test unknown/blank/null entries and serialized length including escaping. Unknown strings alone do not grant authority unless a consumer recognizes them. List returns global plus own-academy roles. Assign requires same-academy target and global/own role, nonnull role name; accepts system/protected role IDs, no staff-only or self guard. PlatformOwner role assignment alone does not set IsPlatformOwner flag. Protected-role delegation and self-lockout require an explicit policy before repair.

AccessGrants.List returns historical rows descending creation time, including expired/revoked; missing person label Former user. Create records admin as GrantedByUserId, UTC creation, allowed permission JSON max 4000, Reason max 500. Nonpermanent expiry must be strictly future. No duplicate-grant uniqueness constraint; overlapping grants union. Revoke scopes grant to academy, fills revoked time/actor once, returns 204; repeat revoke is idempotent for grant record. UI correctly avoids parsing its empty body. Revoking one overlapping grant does not remove rights supplied by another grant or role.

AccessReviews.List returns own-academy reviews descending ReviewedAtUtc. SignOff returns empty 200. ReviewerUserId remains null; generated task has no exact review link (BUG-DATA-0013). Generic audit actor/route is not exact record attribution. Test transaction rollback if follow-up length fails, concurrent reviews, optional task, missing/empty notes, UTC/local presentation and audit failure after successful save (existing BUG-API-0002).

## G03 — effective permissions and lifecycle matrix

Program rejects an authenticated Identity user with IsActive=false. Global filter obtains live user/roles, checks academy match/activity and module, and unions system permissions, custom role JSON and unrevoked permanent/unexpired grants. Grants use strict ExpiresAtUtc > UTC now. New requests therefore must stop accepting expired/revoked authority without assuming tokens need renewal; in-flight requests and overlapping authority require separate controls.

| All actions in controller | Module and filter permission | Additional action gate / expected actors |
| --- | --- | --- |
| StaffController (6) | Core, workforce.manage for non-admin | IsOwner requires same academy Owner/AcademyAdmin; workforce grant alone insufficient |
| AcademyRolesController (3) | AccessGovernance, workforce.manage | Same-academy Owner/AcademyAdmin required |
| AccessGrantsController (3) | AccessGovernance, workforce.manage | Same-academy Owner/AcademyAdmin required; recipient eligibility separate |
| AccessReviewsController (2) | AccessGovernance, no permission mapping | Ordinary Owner/AcademyAdmin pass; nonadmins denied even with custom grants. Platform flag bypasses filter and no further action gate |

Platform flag bypasses global restrictions/audit but still encounters same-tenant admin gates in first three controllers. A bare PlatformOwner role is not that flag. Test anonymous, unassigned, active/inactive identity, suspended/missing academy, enabled/disabled module and all 12 roles with and without custom grants. Wrong-tenant targets/roles/grants deny without writes. Lack of Authorize on AccessReviews does not make it anonymous: global filter still checks user. API response denial may come from framework auth or controller/filter, so test documented status/body rather than assuming every rejection is identical.

Custom permission grants cannot elevate a nonadmin through the explicit IsAdmin helpers. This is the same design mismatch family as existing SECURITY-ROLE-002; record policy differences instead of weakening checks. Staff targets are broader than the list UI: direct status/password/offboard/role actions can address same-academy portal or privileged users. Test last-admin/self/other-admin behavior against agreed governance rules. Academy roles with Teacher authority but missing TeacherId cannot be assumed a working teaching account.

## G01 — feedback, confirmation and visual specifications

Staff: initial loading, signed-out, forbidden, no academy, empty directory, many users. Create resets fields/reloads with no success notice. Role update and offboard set notice then load clears it (existing FORM-SUCCESS-001). Offboard native confirm cancellation returns before POST; affirmative action has no busy guard. updateRole clears busy after response but lacks finally for network rejection, leaving a disabled row. Loading failure can coexist with stale list. No native modal baseline is asserted.

Access grants: choose user/permission required in handler; expiry required only when nonpermanent. Success calls event.currentTarget.reset after await (existing BUG-FUNC-0001), then clears states/sets success/reloads (notice clearing). No saving guard; rapid submit/revoke and ambiguous response need tests. Active badge counts all unrevoked including expired (BUG-UI-0003), while backend excludes expired. Expired/revoked/permanent/future rows need distinct understandable labels and the same effective-access arithmetic. Date selection, hidden date input and keyboard behavior inherit shared controls tests.

Sign-off: loading/errors, empty/history; request parses no success body, async reset and reload feedback problems remain. load parses history without checking response/status, so error object can reach v.map. Required textarea differs from nullable API. Follow-up offers no assignee. Success needs visible confirmation and updated review history; repeated submit must not accidentally duplicate attestation/task.

| Visual surface | Required future evidence |
| --- | --- |
| Staff create + directory | Desktop paired panels and mobile single column; name/email distinction, long names and multiple roles, disabled/active states, readable error/success. Staff grid collapses at 860px, person/header layout at 760px in source; inspect adjacent widths as well as standard device matrix |
| Role dropdowns | Correct value after change; all rows clickable above neighboring cards; test first/middle/last row and short page, scroll 0/25/50/75/bottom, keyboard, touch, zoom. CSS z-index rules are not browser proof |
| Grant form/register | Permission labels and checkbox hit areas, conditional expiry reflow, date month/year access, long permission lists/reason, time/status distinction, wrapping action buttons, no horizontal overflow |
| Sign-off/history | Consistent field surfaces and spacing across themes, long notes, readable date/reviewer, checkbox/button alignment, success/error contrast. Existing utility colors need rendered contrast checks |
| Feedback and navigation | Correct mobile section label, menu collapse, direct section-to-section navigation, focus after cancel/save, scroll restored or retained intentionally; status announced and durable after reload |

Apply [viewport matrix](../06_DEVICE_VIEWPORT_MATRIX.md) and [overlay protocol](../07_VISUAL_TEST_MATRIX.md) with synthetic fixtures. No visuals were repaired, screenshots approved or physical iOS/Android results claimed in this batch.

## Findings, totals and next step

Four new source findings: BUG-SEC-0004 and BUG-DATA-0012 (P1), BUG-UI-0003 and BUG-DATA-0013 (P2). All OPEN / STATIC-FINDING / final verification NOT RUN. Cumulative coverage: 16/82 native forms, 98 form controls +4 standalone =102/649; 13/70 complete controller mappings. Remaining work cannot be estimated as a percentage from unequal batch counts.

Next bounded review: remaining Teacher/Student/Guardian permission paths, excluding resource paths already covered in batch 03. Continue the agreed Astra High audit stage. Sol handles the approved isolated test implementation later; Terra expands established patterns. Phase 1 acceptance remains open until field/state/permission gaps are actually resolved.
