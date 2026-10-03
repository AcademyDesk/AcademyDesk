# Roles and permissions

Observed from ProductionIdentityBootstrapper.SystemRoles, StaffController.AllowedRoles, PermissionCatalog, AuthSessionController, AcademyAccessFilter and individual portal controllers. Twelve named roles: PlatformOwner, Owner, AcademyAdmin, Manager, Operations, Sales, Marketing, FrontDesk, FinanceUser, Teacher, Student, Guardian. Custom academy roles and temporary/permanent AccessGrants add permissions; they are not a fixed thirteenth role. UI calls Guardian “Parent” in some places; no separate Parent role is established.

## Actual gates

1. Inactive authenticated users are rejected by middleware. A missing identity and an inactive identity are different cases and need separate tests.
2. Actions with `Guid academyId` invoke AcademyAccessFilter. Anonymous/missing user → 401; IsPlatformOwner → bypass the tenant/module/catalog checks; another tenant → 403; inactive/missing academy or disabled required module → 403.
3. Owner/AcademyAdmin pass the catalog gate for their tenant. Other users need ALL mapped permissions, combining system roles, custom role JSON and unrevoked/unexpired grants. An unmapped controller defaults to 403 for non-admin users.
4. Action-specific checks still execute after the global gate. A PlatformOwner flag bypass does NOT automatically override a controller requiring own AcademyId or Owner/AcademyAdmin membership.
5. Teacher and family endpoints do not take `academyId` route parameters and implement their own linked-person scope checks. They must be tested directly over HTTP; hiding navigation has no security effect.

## Role matrix (current implementation, not approved future policy)

| Role | View/create/edit/delete/approve scope at shared gate | Direct API denial and exceptions |
| --- | --- | --- |
| PlatformOwner | Tenant-independent academy gate bypass using IsPlatformOwner flag; platform actions explicitly require flag | Role name alone does not establish flag; strict tenant-admin controllers can still deny; test self-service links separately |
| Owner | All implemented operations in own active/enabled academy, subject to action rules | Other academies denied; platform-only APIs denied |
| AcademyAdmin | Same tenant administrative scope as Owner | Other academies and platform management denied |
| Manager / Operations | batches.manage, scheduling.manage, attendance.manage, makeup.manage across mapped controllers | No implicit finance, students.manage, settings or unmapped-controller access |
| Sales / Marketing | sales.manage, students.onboard | StudentOnboarding action additionally requires admin: current conflict BUG-FUNC-0002 |
| FrontDesk | sales.manage, students.onboard, students.manage, student-fees.manage | Onboarding conflict; no finance/payroll authority implied |
| FinanceUser | finance.manage, student-fees.manage, reports.export | Payroll/TeacherCompensation lack a catalog mapping; reports export also gated by subscription module |
| Teacher | No default academy-controller permissions; TeacherPortal self-service by linked teacher, academy and batch/session ownership | Other teachers, unassigned student/batch/session, academy admin APIs denied absent grants |
| Student | No default academy-controller permissions; Portal self-service by linked active student | Other students, guardian identities, teacher/admin/platform APIs denied |
| Guardian | Own linked children with CanAccessPortal and not revoked, then feature-specific academic/finance/documents/leave flags | Unrelated/revoked child and forbidden feature denied; parent cannot use student's profile update endpoint |
| Custom role / grant | Exact JSON permissions for mapped controllers, plus plan/module gating and action checks | Unknown permissions confer no mapped scope; expired/revoked grants must cease authorizing immediately |

## Feature/operation matrix

Legend: A = own-tenant Owner/AcademyAdmin through normal checks; P = PlatformOwner where action permits; C = permission-qualified role/custom grant; T = linked assigned Teacher; S/G = linked Student/authorized Guardian; — = no matching operation or not granted by this feature. Methods and action-specific overrides are exhaustively listed in [endpoint authorization appendix](INVENTORY/AUTHORIZATION.md).

| Feature | View | Create | Edit | Delete/deactivate | Approve/decide | Who must be denied |
| --- | --- | --- | --- | --- | --- | --- |
| Platform tenants/config/admins/billing/support | P | P | P | P where implemented | P | All non-platform-owner users |
| Own academy profile/branches | A/P (own check may deny P) | A/P | A/P | A/P where implemented | — | Other tenants, ordinary self-service users |
| Leads/trials/campaigns | A/P/C sales.manage | A/P/C | A/P/C | Same gate on implemented lifecycle actions | Conversion same gate | Roles without sales.manage |
| Student intake | A/P may load prerequisites | A only in current action | — | — | — | Sales/Marketing/FrontDesk currently denied despite catalog grant |
| Students/guardians/enrollments | A/P/C students.manage | A/P/C | A/P/C | Same gate on implemented actions | Transfer same gate | Unrelated tenant; self-service users using admin endpoints |
| Profile summaries / teacher directory | A/P | A/P (teachers) | A/P | A/P where implemented | — | Non-admin without mapping, even if workforce.manage granted |
| Student fee arrangements | A/P/C student-fees.manage | A/P/C | A/P/C | Same gate where implemented | — | Roles without mapped permission |
| Batches / sessions / attendance / makeup | A/P/C respective manage permission | Same | Same | Same where implemented | Leave decisions require separate controller review | Unassigned self-service users; wrong tenant |
| Courses / academic periods / assessments / promotions | A/P/C academics.manage on mapped controllers | Same | Same | Same where implemented | Same gate for decisions | Permission absent; action/controller may be unmapped |
| Invoices / payments / expenses / adjustments | A/P/C finance.manage plus module | Same | Same | Lifecycle/status same gate | Adjustment approval uses same finance permission | Permission absent, cross-tenant, disabled plan module |
| Payroll / compensation | A/P (unmapped for staff) | A/P | A/P | Implemented actions only | — | FinanceUser currently not granted through catalog alone |
| Roles / staff / grants / portal accounts | A; P may hit own-admin guard | A | A | A | A | Non-admin grants do not override strict IsAdmin helpers |
| Communication / compliance / work queue | A/P (unmapped catalog in these controllers) | A/P | A/P | Implemented actions only | A/P | Permission names such as communications.manage do not help if controller unmapped |
| Teacher portal assignments/attendance/resources/leave | T within links | T within links | T within links | Only implemented lifecycle actions | Assigned submission/result review | Other teacher/student/batch/tenant |
| Student/Guardian portal | S/G per link/flags | Assignment/practice/leave per action | Student own profile; Guardian own contact profile | — | — | Unrelated child, revoked flag, inactive student |
| Private file URLs | Intended authorized recipient | Authorized upload | — | — | — | Anonymous/cross-tenant retrieval must deny; current static hosting risk BUG-SEC-0001 |

All runtime authorization cases are NOT RUN. Next phase must approve intended policy where source conflicts, then assert every method's allow/deny outcome, response and absence of unauthorized DB changes. Controller-level permission is not separate read/write/approve permission: do not invent finer separation than source implements.
