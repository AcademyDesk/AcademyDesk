# AcademyDesk — Product Blueprint

## 1. Product definition

AcademyDesk is a subscription-based, multi-tenant management platform for Indian academies. A **tenant** is one academy organization; it can have multiple branches. The product begins with Music Academies as a first-class experience, while providing a configurable Tuition and Coaching experience from the same shared platform.

It is one product and one codebase. Academy type, enabled modules, terminology, fields, workflows, reports, and branding are configuration—not separate applications.

## 2. Product principles

1. Every business record belongs to an academy and, where relevant, a branch.
2. Tenant isolation is enforced by the backend and database queries; the browser must never be trusted to select a tenant.
3. Permission checks happen on every protected action, including file access and exports.
4. Financial records are immutable after issuance. Corrections use explicit adjustments, credit notes, refunds, or reversals.
5. Payments are confirmed server-side using a verified provider webhook or API check, never just a browser success message.
6. Sensitive files are private. The application issues short-lived, authorized access rather than public file links.
7. The system is configurable before it is customized. Per-academy custom code is not part of Version 1.
8. Every material change has an audit event: who, when, from where, and before/after values where appropriate.

## 3. User roles

| Role | Scope | Main authority |
|---|---|---|
| Platform Super Admin | All tenants | Plans, tenant lifecycle, support, platform operations |
| Platform Support | Approved tenant support scope | Support cases; no unrestricted financial or file access |
| Academy Owner | One academy | Full academy administration, billing, exports |
| Branch Admin | Assigned branches | Branch operations and staff management |
| Finance Manager | Assigned academy/branches | Fees, invoices, payments, refunds, finance reports |
| Academic Manager | Assigned academy/branches | Courses, batches, schedules, assessments, reports |
| Teacher | Assigned classes/students | Class delivery, attendance, homework, assessment feedback |
| Front Desk | Assigned branches | Enquiries, admissions, student records, payment collection |
| Parent/Guardian | Linked dependants | Timetable, attendance, payments, messages, progress |
| Student | Own permitted records | Timetable, learning work, practice/homework, results |

Roles are permission bundles. A tenant can create limited custom roles without bypassing the underlying permission model.

## 4. Shared Version 1 modules

### 4.1 Tenant, branch, and academic setup

- Academy profile, logo, address, tax and invoice details, time zone, currency, working days
- Multiple branches, classrooms/rooms, staff access by branch
- Academic years, terms, holidays, closure dates, operating hours
- Academy type: Music, Tuition/Coaching, or both
- Feature settings, notification preferences, branding, document requirements, numbering rules

### 4.2 Identity, authorization, and onboarding

- Secure sign-in, invitation, account recovery, session management, MFA-ready authentication
- User invitation, activation/deactivation, role assignment, branch restrictions
- Student and guardian account linking, consent and communication preferences
- Onboarding checklist for a new academy: profile, branches, staff, courses, fee plans, payment setup, notification setup

### 4.3 CRM, admissions, and people

- Enquiry capture from front desk, web form, referral, campaign, walk-in, and import
- Lead stages, follow-ups, counselling notes, conversion/loss reasons
- Applications, admission checklist, document collection, approval/rejection, enrolment
- Student, guardian, emergency contact, address, medical/learning notes, communication preference, document records
- Teacher and staff profiles, availability, qualifications, employment status, assigned branches
- Transfers, pauses, withdrawals, re-admissions, alumni records, duplicate detection

### 4.4 Academic catalogue and enrolment

- Courses/programs, subjects or instruments, levels, terms, curriculum units, prerequisites
- Fee plans, duration, delivery mode, capacity, eligibility, instructor requirements
- Batches, one-to-one enrolment, group enrolment, waitlists, trial classes
- Student enrolment history, course transfer, progress status, completion and certification eligibility

### 4.5 Scheduling and class operations

- Recurring schedules and one-off sessions, rooms, online/offline/hybrid delivery
- Teacher, room, and student conflict detection
- Cancellations, rescheduling, make-up classes, substitutions, closures, capacity and waitlist management
- Daily agenda for admin, teacher, student, and parent
- Class session record: planned versus delivered lesson, attendance, notes, follow-up work

### 4.6 Attendance and leave

- Student, teacher, and staff attendance with status, late arrival, and reason
- Teacher or admin entry; student/parent leave request and approval workflow
- Attendance correction request, approval, and audit trail
- Per-course/batch/branch reports; low-attendance alerts configurable by academy

### 4.7 Learning, homework, and assessment

- Curriculum/lesson-plan structure and learning resources
- Homework/assignment creation, due dates, attachments, submissions, feedback, and rubric scoring
- Assessments, grades/marks, comments, result publication controls, report cards
- Certificates with templates, serial numbers, verification status, and issue/revocation record
- Student and parent progress views

### 4.8 Finance

- Fee structures by program, branch, student, term, instalment, or custom agreement
- Discounts, scholarships, waivers, tax settings, late fees, invoices, credit notes, refunds, receipts
- Offline payment recording with approver controls and proof attachment
- Online payment orders, webhook processing, reconciliation, failed/duplicate payment handling
- Outstanding balances, due schedules, configurable reminders, collections dashboard
- Academy expenses, categories, approvals, attachments, and income/expense reporting
- Exportable cash collection, receivables, payment-method, tax, and branch reports
- AcademyDesk subscription billing is a separate platform-finance domain from an academy's student-fee collection.

### 4.9 Communication and notifications

- In-app notification centre and notification preferences
- Email and WhatsApp provider adapters; templates, approval state, variables, scheduling, delivery/failure history
- Triggers for admissions, invoices, receipts, due reminders, attendance, cancelled classes, homework, results, and announcements
- Broadcasts limited by role, branch, course/batch, student/guardian audience, and communication consent
- No message may be sent without an auditable recipient selection and delivery record.

### 4.10 Documents, media, and files

- Private profile and admission documents; version/status/expiry tracking
- Course resource library organized by academy, course, lesson, and permissions
- Virus/malware scan integration point, content-type/size limits, retention and deletion workflows
- Permission-based, time-limited download access; no public blob URLs

### 4.11 Search, dashboards, reports, and exports

- Global search limited to the current tenant and user permissions
- Role-specific dashboards: admissions, today's classes, attendance, collections, overdue fees, progress, subscriptions
- Filterable reports with branch/date/course/batch controls
- CSV/XLSX/PDF exports with audit events, background generation, expiration, and permission checks

### 4.12 Platform administration and SaaS billing

- Tenant creation, suspension, trial, plan assignment, feature entitlements, usage limits
- Platform subscription invoices/payments, plan changes, cancellation, grace-period rules
- Support case tracking, tenant impersonation only with explicit approval, prominent banner, scoped access, and audit logs
- Platform health, job failures, webhook failures, tenant-level usage, operational alerts

## 5. Music Academy extension

- Instrument and vocal disciplines; genre/style; grade/exam board; skill level
- Individual lesson, group lesson, ensemble, workshop, masterclass, and recital programs
- Practice plan, practice log, teacher feedback, goals, repertoire/piece list, performance readiness
- Instrument and practice-room inventory, booking, maintenance, and conflict control
- Accompanist/ensemble participation, recital/event scheduling, performer roster, rehearsal plan, ticket/guest list optional module
- Music assessments: technical skill, theory, rhythm, repertoire, performance rubric, exam readiness
- Certificates for course completion, grade, recital, and competition participation

## 6. Tuition and Coaching extension

- Board, school class, subject, syllabus, competitive exam, and test-series configuration
- Batch timetable, doubt session, revision plan, study-material distribution
- Chapter/topic coverage, test scheduling, marks, rank/percentile, question-bank reference, result analysis
- Parent progress report covering attendance, homework, tests, weak topics, and teacher remarks
- Separate treatment of coaching packages, mock exams, and test series where needed

## 7. Required application surfaces

1. Public marketing website and authenticated sign-in
2. Academy-owner/admin web portal
3. Teacher web-first portal, responsive for phone use
4. Parent/student responsive portal
5. Platform administration portal
6. Optional native mobile applications are not required for Version 1; the portals must be mobile-responsive and installable as a PWA if desired.

## 8. Core data model groups

Every tenant-owned table includes `AcademyId`; branch-scoped tables also include `BranchId`. All records include identifier, created/updated metadata, status, and audit correlation.

- Platform: Plan, Feature, TenantSubscription, PlatformInvoice, SupportCase
- Organization: Academy, Branch, Room, AcademicYear, Term, Holiday, Settings
- Identity: User, Role, Permission, UserRole, UserBranchAccess, Session, Consent
- People: Student, Guardian, StudentGuardian, Teacher, Staff, Lead, Application, Document
- Academics: Program, Course, SubjectOrInstrument, Level, CurriculumUnit, Batch, Enrollment
- Operations: Schedule, ClassSession, AttendanceRecord, LeaveRequest, SubstituteAssignment
- Learning: LessonPlan, Resource, Assignment, Submission, Assessment, Result, Certificate
- Finance: FeePlan, FeeSchedule, Invoice, InvoiceLine, Payment, Receipt, Refund, CreditNote, Expense
- Communication: Template, Notification, NotificationDelivery, Announcement, CommunicationPreference
- Music: Repertoire, PracticeLog, InstrumentAsset, RoomBooking, Event, Rehearsal
- Coaching: Board, Syllabus, Topic, Test, TestResult, PerformanceAnalysis
- Governance: AuditLog, ExportJob, FileAccessLog, DataRetentionJob

## 9. Non-functional requirements and release gates

### Security and privacy

- Threat model before build; secure defaults, HTTPS, rate limiting, input validation, output encoding, and dependency scanning
- Secrets only through secure environment/secret storage, never source code or client bundles
- Encryption in transit and at rest; backups; disaster-recovery runbook; restore testing
- Granular authorization test suite proves tenant and branch isolation for every API category
- Consent, data-access, correction, retention, and deletion workflows designed with applicable Indian privacy requirements; obtain qualified legal advice before production policy decisions.

### Reliability and operations

- Centralized structured logs, metrics, traces, health checks, alerting, incident runbooks
- Idempotent background jobs and payment/webhook handlers; retry and dead-letter strategy
- Database migrations, seed data, backup/restore procedure, retention policy
- Separate Local, Development, Staging, and Production environments; no production data in lower environments

### Quality

- Unit tests for business rules; integration tests for APIs/database; end-to-end tests for critical journeys
- Accessibility baseline: keyboard operation, labelled controls, color contrast, responsive layouts
- Performance budgets for dashboards/search; pagination and background jobs for large reports/exports
- CI requires formatting, static analysis, test execution, security/dependency checks, and migration validation before deployment

## 10. Recommended build order without sacrificing scope

1. Product design: complete screen map, business rules, permission matrix, report catalogue, notification catalogue, and acceptance criteria.
2. Foundation: repository, environments, identity, tenant/branch enforcement, audit logging, shared UI, CI/CD, observability.
3. Organization and people: academy setup, roles, branches, CRM/admissions, students, guardians, teachers.
4. Academic operations: catalogue, enrolment, schedules, attendance, leave, dashboards.
5. Learning: curriculum, resources, homework, assessments, reports, certificates.
6. Finance: fee plans, invoices, offline collection, online payments, verified webhooks, reconciliation, finance reports.
7. Communication and file services: templates, notifications, email/WhatsApp adapters, secure storage.
8. Vertical modules: Music workflows and Tuition/Coaching workflows.
9. Platform operations: subscription billing, entitlement controls, support tools, final reports/exports.
10. Production readiness: accessibility, security review, load tests, backup-restore drill, pilot-data migration, user acceptance testing, launch runbook.

## 11. Explicit exclusions from initial build

- Separate custom codebase per academy
- Microservices or Kubernetes before scale demonstrates a need
- Publicly accessible student files
- AI features that make academic or financial decisions without human review
- Building native iOS/Android apps before the responsive web product is validated

## 12. Decisions still required before screen-by-screen design

1. AcademyDesk brand, legal entity, and domain name
2. Initial plan tiers, price model, trials, and billing cadence
3. Exact payment provider(s), WhatsApp provider, email provider, and identity approach
4. Whether academy staff can collect student payments online only, offline only, or both
5. Whether the first release includes events/recitals and expense management as mandatory modules
6. Data residency, privacy policy, retention period, and legal/compliance review
