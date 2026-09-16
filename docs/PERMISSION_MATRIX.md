# AcademyDesk — Permission Matrix

Permission names are stable application capabilities. Roles are bundles of capabilities, and each capability is additionally filtered by academy, branch, assigned class, or linked student scope.

## Scope rules

- Platform roles can access platform records; academy roles cannot access another academy.
- Academy Owner and Platform Super Admin are the only roles with unrestricted academy-level administration.
- Branch Admin, Finance Manager, Academic Manager, Front Desk, and Teacher are restricted to assigned branches or records.
- Teachers can access only assigned batches/students unless an academy policy explicitly grants broader academic visibility.
- Parents/guardians can access only linked dependants; students can access only their own permitted records.
- Export, file download, impersonation, financial correction, and permission changes always create an audit event.

## Capability matrix

Legend: **F** full manage, **E** create/edit, **V** view, **A** approve, **—** no access. Scope restrictions still apply.

| Module | Platform Super Admin | Platform Support | Owner | Branch Admin | Finance | Academic | Front Desk | Teacher | Guardian | Student |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Tenant lifecycle/plans | F | V | V | — | — | — | — | — | — | — |
| Support cases | F | E/V | V | V | V | V | V | — | — | — |
| Academy/branch settings | F | V* | F | E | V | V | — | — | — | — |
| Users, roles, permissions | F | V* | F | E (branch) | — | — | — | — | — | — |
| Leads and admissions | V* | V* | F | F (branch) | V | E | E | V* | — | — |
| Student/guardian records | V* | V* | F | F (branch) | V | E | E | V (assigned) | V (linked) | V (self) |
| Teacher/staff records | V* | V* | F | F (branch) | V | E | V | V (self) | — | — |
| Programs/courses | V* | V* | F | E (branch) | V | F | V | V | V | V |
| Batches/enrolments | V* | V* | F | F (branch) | V | F | E | V (assigned) | V (linked) | V (self) |
| Schedule/class sessions | V* | V* | F | F (branch) | V | F | E | E (assigned) | V (linked) | V (self) |
| Attendance | V* | V* | F | F (branch) | V | F | E | E (assigned) | V (linked) | V (self) |
| Leave/substitutions | V* | V* | F | F (branch) | V | F | E | E (self/request) | E (linked request) | E (self request) |
| Resources/homework | V* | V* | F | E (branch) | V | F | V | F (assigned) | V (linked) | E (self submission) |
| Assessments/results | V* | V* | F | E (branch) | V | F/A | V | E (assigned) | V (linked) | V (self) |
| Certificates | V* | V* | F | E (branch) | V | F/A | V | E (assigned) | V (linked) | V (self) |
| Fee plans/invoices | V* | V* | F | V (branch) | F | V | E (collection) | V (assigned) | V/E (pay) | V (own) |
| Payments/refunds/credits | V* | V* | F/A | V (branch) | F/A | V | E (offline collection) | — | E (pay/request) | E (pay) |
| Expenses | V* | V* | F/A | E (branch) | F/A | — | — | — | — | — |
| Notifications/templates | F | V* | F | E (branch) | E (finance) | E (academic) | E (admissions) | E (class) | V | V |
| Broadcasts | V* | V* | F | E (branch) | E (finance audience) | E (academic audience) | E (admissions audience) | E (assigned audience) | — | — |
| Reports/exports | F | V* | F | F (branch) | F (finance) | F (academic) | E (admissions) | V/E (assigned) | V (linked) | V (self) |
| File upload/download | F | V* | F | F (branch) | F (finance files) | F (academic files) | E (admission files) | E (assigned) | E/V (linked) | E/V (self) |
| Audit log | F | V (support scope) | V (academy) | V (branch) | V (finance) | V (academic) | V (admissions) | V (own actions) | — | — |
| Music module | V* | V* | F | F (branch) | V | F | V | F (assigned) | V (linked) | E/V (self) |
| Coaching module | V* | V* | F | F (branch) | V | F | V | F (assigned) | V (linked) | E/V (self) |

`V*` means support or platform access is explicitly scoped and audited; it is never an implicit right to read all customer data.

## Sensitive actions requiring approval or step-up controls

- Change role, permission, branch access, or academy ownership
- Export personal, financial, or whole-tenant data
- Download restricted documents or access another user’s sensitive notes
- Issue/void invoice, refund payment, create credit note, or modify a paid ledger
- Publish assessment results or issue/revoke a certificate
- Send a broadcast or enable an automated communication rule
- Impersonate an academy user for support
- Suspend tenant, change subscription plan, or alter retention/deletion settings

## API enforcement requirements

1. Resolve the authenticated user and current academy from trusted server-side identity claims/session, never from an unchecked request field.
2. Apply academy and branch filters in the data-access layer, not only in UI queries.
3. Check capability plus record scope before read, write, export, download, or background-job execution.
4. Return the same safe not-found response for inaccessible records to avoid leaking their existence.
5. Record authorization failures and all sensitive successful actions in the audit log.
6. Write automated tests for every capability boundary and a cross-tenant access-denial test for every resource family.

