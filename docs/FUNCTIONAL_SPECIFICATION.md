# AcademyDesk — Functional Specification

This document defines the Version 1 application surface. Each item must have acceptance criteria and a permission check before implementation.

## 1. Navigation model

After sign-in, a user is taken to the portal and branch context permitted by their role. The app has a global academy/branch switcher only for users allowed to use it; the API independently validates every request.

Primary navigation is modular: Dashboard, Leads & Admissions, People, Academics, Schedule, Attendance, Learning, Finance, Communication, Reports, Settings. Music and Coaching menus appear only where the academy has enabled those modules.

## 2. Platform administration portal

| Screen | Required functions |
|---|---|
| Platform dashboard | Tenant count/status, trials, subscriptions, revenue indicators, operational alerts |
| Academy directory | Create, search, view, suspend/reactivate academy, assign plan and feature entitlements |
| Subscription management | Plans, pricing, trials, invoices, payment status, cancellation/grace workflows |
| Feature management | Global feature catalog; tenant-specific enabled modules and limits |
| Support cases | Case intake, owner, status, internal notes, audit-safe tenant access workflow |
| Operational health | Failed jobs, payment webhooks, notification delivery failures, API errors |
| Platform users | Platform roles, MFA/privileged-access status, audit trail |

## 3. Academy owner and admin portal

### Dashboard

- Today’s classes, attendance, admissions funnel, fee collections, overdue amounts, unread alerts, teacher absence/conflicts.
- Date/branch filters, drill-down only to data the current user can access.

### Academy settings

- Academy profile, logo, addresses, invoice/tax details, preferred language, operating hours, academic year, terms, holidays.
- Branch, room, classroom, and room-capacity setup.
- Numbering formats for admissions, invoices, receipts, certificates.
- Academy type and enabled vertical modules; terminology configuration (for example, Instrument vs Subject).
- Staff roles, permission assignments, branch access, user invitations/deactivation.

## 4. Leads and admissions

| Screen | Required functions |
|---|---|
| Lead list | Search/filter by source, stage, branch, course, owner, follow-up date; bulk-safe assignment and export |
| Lead detail | Contact details, guardian/student candidates, enquiries, notes, activities, follow-up timeline, conversion/loss reason |
| New lead | Web/manual/referral/walk-in source, consent, desired program, branch, preferred schedule |
| Application | Required documents, eligibility, review, approval/rejection, audit history |
| Admission wizard | Create or match student/guardian, choose course/batch, fee plan, document checklist, enrolment confirmation |
| Transfer/withdrawal | Effective date, reason, refund/credit effect, approvals, student history |

## 5. People

### Students and guardians

- Student list with search, status, branch, course, batch, attendance and dues indicators.
- Student profile: personal details, guardian links, contacts, emergency details, notes, documents, enrolment timeline, timetable, attendance, learning, invoices/payments, communication history.
- Guardian profile: dependants, contact preferences, communication consent, invoices and receipts for permitted dependants.
- Import flow: mapping preview, validation errors, duplicate review, dry-run, import audit record.

### Teachers and staff

- Profile, qualifications, specialties/subjects/instruments, availability, documents, branch access, employment status.
- Assigned courses, batches, class sessions, timetable, leave, attendance, performance/operational notes.
- No teacher can view unassigned student records except where explicitly permitted.

## 6. Academics and enrolment

| Screen | Required functions |
|---|---|
| Program/course catalog | Course/instrument/subject, level, duration, capacity, delivery mode, prerequisites, curriculum, fee-plan association |
| Batch list/detail | Branch, schedule, room, teacher, capacity, roster, waitlist, fee plan, lesson progress |
| Enrolment | Student, program, batch/individual lesson, start/end dates, status, agreed fee plan, discounts, notes |
| Curriculum | Units, lessons, objectives, resources, completion status; adapt labels by vertical |
| Academic year/term | Date ranges, holidays, grading and report publication settings |

## 7. Schedule and class sessions

- Calendar views: academy, branch, room, teacher, batch, and student.
- Create recurring or one-off sessions, with capacity and teacher/room conflict detection.
- Cancel, reschedule, substitute teacher, make-up class, and closure flows, each with notification preview and audit event.
- Class-session detail: planned lesson, actual lesson, roster, attendance, notes, resources, homework, assessment links.
- Student/parent/teacher timetable is read-only except for entitled leave/make-up requests.

## 8. Attendance and leave

- Daily class roster: present, absent, late, excused, online attended, cancelled/not applicable; bulk entry supported.
- Student attendance summary by course/batch/date; low-attendance rules and alerts.
- Teacher/staff attendance and leave requests, approval, substitution impact.
- Corrections require reason and retain old/new values in audit history.

## 9. Learning and assessment

| Screen | Required functions |
|---|---|
| Resource library | Private resources by academy/course/lesson; tag, search, audience, version, file access logs |
| Assignment list/detail | Create, assign to batch/student, schedule/due date, rubric, attachments, submission status |
| Student submission | Upload/link/text answer, submitted timestamp, revision rules, teacher feedback |
| Assessment builder | Assessment type, criteria, mark scheme/rubric, class/student assignment, publication settings |
| Results | Marks/grades/comments, moderation/approval, result publication and report card generation |
| Certificates | Eligibility, template, serial number, preview, issuance, revoke/reissue, verification record |

## 10. Music Academy screens

- Music profile: instrument/vocal discipline, style, grade/exam board, skill level, teacher specialty.
- Repertoire: piece/composer/level/status, assigned date, practice goal, teacher feedback, performance readiness.
- Practice log: student entry, duration, self-reflection, attachment/audio option, teacher comment.
- Lesson plan: technique, theory, repertoire, listening, goals, assigned practice.
- Instrument and room inventory: asset identifier, availability, maintenance state, booking conflict checks.
- Events and recitals: event details, rehearsal schedule, performer roster, repertoire, consent/guest list settings, certificate participation record.
- Music rubric: rhythm, technique, theory, musicality, repertoire, performance confidence, custom weighting.

## 11. Tuition and Coaching screens

- Academic configuration: board, class, syllabus, subject, competitive examination, term.
- Topic coverage tracker: subject/chapter/topic, planned vs completed, linked materials and tests.
- Test and test-series builder: schedule, instructions, marks, evaluation, result publication.
- Performance analysis: marks, percentage, rank/percentile where applicable, topic weaknesses, improvement trend.
- Revision/doubt session planner and study-material distribution.
- Parent progress view: attendance, homework, tests, teacher remarks, pending dues.

## 12. Finance screens

| Screen | Required functions |
|---|---|
| Fee plans | Fixed/term/instalment/custom fees, tax, due rules, discounts, late fees, effective dates |
| Student account | Charges, invoices, receipts, payments, credits, refunds, outstanding balance, ledger view |
| Invoice editor | Draft/review/issue, immutable issued invoice, send/print/download, void/credit-note workflow |
| Payment collection | Offline payment entry with proof and permissions; online order/status; receipt generation |
| Reconciliation | Provider event, verified payment, unmatched/duplicate/failed event workflow, idempotency history |
| Expenses | Category, vendor, branch, attachment, approval, paid status, reports |
| Finance dashboard | Collections, receivables, overdue aging, discounts, refunds, revenue/expense by branch/course |

## 13. Communication screens

- Notification centre: personal alerts, read/unread state, destination links.
- Template library: email, WhatsApp, in-app template; variables, approval/status, preview and test route.
- Campaign/announcement composer: approved audience selection, consent filter, schedule, dry run, recipient estimate, delivery reporting.
- Automated rule catalogue: trigger, template, audience, delay/quiet hours, enable/disable, execution history.
- Student/guardian communication history: sent/delivered/failed status, source workflow, no private content exposed beyond authorization.

## 14. Reports and exports

- Report catalogue grouped by Admissions, People, Academics, Attendance, Learning, Finance, Communications, Music, Coaching.
- Each report supports permission-scoped filters and a visible definition of dates/statuses included.
- Exports run in the background, expire, log requester/filters/row count, and use private download access.
- Required baseline reports: student roster, enrolment, class schedule, attendance, teacher workload, fee collection, dues aging, invoice/receipt, assessment, progress, notifications, audit history.

## 15. Parent and student portal

- Home: upcoming class, homework/practice, notifications, dues, recent feedback.
- Timetable and attendance.
- Learning resources, assignments, submissions, feedback, result/certificate access.
- Student practice/repertoire screens only when the Music module is enabled.
- Invoice, payment initiation/status, receipt history for the linked guardian; no ability to modify financial records.
- Profile/contact update request and leave request, subject to academy approval policy.

## 16. Teacher portal

- Today/week agenda; assigned students/batches; attendance entry; lesson/session record.
- Resource, assignment, assessment, feedback, and progress workflows limited to assigned teaching scope.
- Leave and availability request; substitution notification; own attendance.
- Music-specific lesson/practice/repertoire workflows or Coaching-specific lesson/topic/test workflows.

## 17. Cross-cutting acceptance criteria

Every screen must meet all relevant items below:

1. Responsive layout for desktop and phone-sized screens.
2. Loading, empty, error, no-permission, and offline/retry states.
3. Tenant and branch authorization enforced by the API.
4. Search/filter/pagination appropriate to record volume.
5. Validation, confirmation, error messages, and audit events for state-changing actions.
6. Accessible labels, keyboard operation, focus order, and color contrast.
7. Realistic seed data and automated test coverage for critical journeys.

## 18. Detailed-design order

1. Permission matrix and role journeys
2. Tenant/branch setup and authentication
3. Leads, admissions, students, guardians, teachers
4. Programs, enrolment, scheduling, attendance
5. Finance and payment reconciliation
6. Learning/assessment and Music/Coaching modules
7. Communications, reports, files, platform administration

