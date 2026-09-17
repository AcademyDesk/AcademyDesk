# AcademyDesk workspace and tenancy model

## Product boundary

AcademyDesk has two management levels. They must never share the same navigation or data scope.

1. **Platform Owner** manages the AcademyDesk SaaS product. This is the owner of AcademyDesk, not the owner of a customer academy.
2. **Academy tenant** is a customer academy. Its Academy Admin manages only that academy.

## Workspaces

| Workspace | Who uses it | Scope | Primary work |
| --- | --- | --- | --- |
| Platform Owner | AcademyDesk team | All customer academies | Onboard academy, approve/disable academy, academy-level configuration, platform support, subscription/integration oversight |
| Academy Admin | One academy's administrator | One academy | Students, guardians, teachers, staff, courses, batches, schedules, finance, communications, reports, academy settings |
| Front Desk | Academy staff | One academy, limited operational access | Leads, students, enrolments, attendance support, schedules, communication |
| Teacher | Assigned teacher | Only linked teacher and assigned batches | Today, classes, attendance, lesson plans, assignments, assessments, practice feedback |
| Student | Linked student | Only linked student | Schedule, attendance, assignments, practice, results, certificates, fees, notifications |
| Guardian | Linked guardian | Only linked children | Child schedule, progress, attendance, fees, notifications |

## Role names

- `PlatformOwner`: SaaS-level permission. No academy-wide operational data is exposed unless the owner deliberately enters a support context.
- `AcademyAdmin`: academy-level administrator. Replaces the ambiguous product wording `Owner` for all new accounts.
- `Manager`, `FrontDesk`, `Teacher`, `Student`, `Guardian`: scoped roles.
- Existing `Owner` accounts are treated as legacy Academy Admin accounts until they are migrated safely.

## Onboarding flow

1. Platform Owner signs in to `/platform`.
2. Platform Owner creates an academy and its first Academy Admin account in one transaction.
3. Academy Admin receives credentials and signs in to `/dashboard`.
4. Academy Admin completes academy profile, branches, courses, staff, communication setup, then operations.
5. Academy Admin creates staff and portal accounts. These accounts always land in their own role workspace.

## Navigation rules

- Platform Owner never sees student, teacher, finance, or academy-operation menus in its default workspace.
- Academy Admin sees grouped People, Academics, Operations, Finance, Engagement, and Settings menus.
- Teacher navigation contains only teaching workflows.
- Student and Guardian navigation contains only their portal workflows.
- UI filtering improves usability; API authorization remains the enforcement boundary.

## Required technical changes

1. Add `IsPlatformOwner` to the identity user and provision the first platform owner from deployment configuration.
2. Add `/api/platform/academies` for platform-owner academy listing and onboarding.
3. Add an academy membership model before allowing a user to administer more than one academy.
4. Add `AcademyAdmin` role and retain legacy `Owner` compatibility during migration.
5. Return workspace scope and allowed navigation from `/api/auth/session`.
6. Route sign-in to `/platform`, `/dashboard`, `/teacher`, or `/portal` based on authenticated workspace scope.
7. Replace the current shared menu with workspace-specific menu definitions.
