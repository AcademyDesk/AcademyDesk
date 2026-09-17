# AcademyDesk Academy Admin Enterprise Standard

## Purpose

Academy Admin is the operating authority for one academy. Every record, setting, report, and action must be academy-scoped, auditable where sensitive, and connected to the next operational step.

## Workspace controls

| Area | Admin controls | Connected outcome |
|---|---|---|
| Academy & branches | academy identity, legal details, time zone, branches, holiday calendar | schedules, invoices, communications use local context |
| People & access | learner, guardian, teacher, staff, portal account, active/inactive lifecycle | enrolment, attendance, communication and role access |
| Admissions | lead source, stage, follow-up, conversion, enrolment | learner, fee plan and batch assignment |
| Academics | programs, curriculum, batches, resources, lesson plans, assessments | class schedule, teacher delivery, learner progress |
| Operations | schedule, rooms, delivery mode, attendance, leave, make-up, events | teacher workspace, guardian/student visibility |
| Finance | fee plans, invoice status, payments, expenses, reminders | learner balance, receipts, finance reports |
| Engagement | channel configuration, templates, consent/preferences, delivery status | compliant learner/family communication |
| Governance | branches, activity/audit, reports, exports, retention and controlled account actions | accountable administration |

## Required record standards

### Learner

- Identity: learner number, legal/preferred name, date of birth, gender (optional), admission date, active lifecycle
- Contact: email, mobile, address, city, state, postal code
- Safety: emergency contact name/phone, medical or accessibility alert, consent-sensitive notes
- Operations: branch, guardians, enrolments, attendance, invoices, learning progress, communication history

### Guardian

- Identity/contact: legal/preferred name, email, mobile, address
- Relationship and authority: relationship per learner, primary contact, pickup/collection authorization, active lifecycle
- Engagement: preferred language/channel, communication preference and portal access

### Teacher and staff

- Identity: employee code, legal/preferred name, joining date, employment type, active lifecycle
- Contact/safety: email, mobile, address, emergency contact
- Professional: qualifications, specialties, bio, branch assignment, availability/leave, assigned batches
- Access: staff account, role, password reset/deactivation history, audit event

### Academic delivery

- Course: code, name, level, instrument/subject, duration, capacity, prerequisites, fee plan, active lifecycle
- Batch: code/name, course, branch, teacher, capacity, timetable, delivery mode, start/end dates, active lifecycle
- Session: batch, teacher, room/online link, start/end, delivery mode, status, cancellation/substitution reason

### Finance

- Fee plan: code, amount, currency, frequency, tax/discount policy, effective dates, active lifecycle
- Invoice: number, learner, plan, line items, issue/due dates, status, adjustments, collection history
- Payment: receipt/reference, method, date, amount, allocation, reversal/refund audit
- Expense: vendor, category, branch, bill/reference, tax, approval/status, attachment reference

## Implementation rules

1. New sensitive fields must be visible only to Academy Admin and authorised scoped roles.
2. A deactivated person loses workspace access without deleting historical records.
3. Each list page must support search, lifecycle filters, record navigation, and an empty/loading/error state.
4. Every 360 record links to its related financial, academic, operational, and communication records.
5. Exports, finance corrections, account actions, and bulk communications must create audit events.
