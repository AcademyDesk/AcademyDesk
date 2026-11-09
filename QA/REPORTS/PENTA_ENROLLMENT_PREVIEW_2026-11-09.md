# PENTA enrollment preview with real domain checks — 9 November 2026

Base `6f77a94c528b6e6df0301b98c5ed75dcc76c6581`. This batch connects the
existing `AcademyDesk.Enrollment.Create` typed payload to a Testing-only host
preview. Both `Penta:Enabled` and `Penta:EnrollmentPreviewEnabled` are required.
The endpoint is `POST /api/academies/{academyId}/penta/enrollments/preview`.

The host validates current actor, academy, role, module and security stamp in
its linked SQL and Identity transaction. The existing admission lock protects
the read while the shared rules check the student, batch, prerequisites, open
status, capacity and duplicates. Manual creation uses the same assessment
before its existing write. A successful preview returns the checked fields
with `PreviewOnly` and requires approval. It writes one scoped audit, creates
no enrollment, reserves no seat and issues no authorization. A future
execution path must independently recheck authority and the domain rules.

## Observed validation

Local window `e500f563de5543efb324305275470fe3`, disposable SQL run
`14f8b6048b714c3e8bbe5728b56d1d00`. The API and harness builds passed with
zero warnings or errors. The new endpoint passed 13 actual HTTP/Identity/SQL
checks. They include default-disabled behavior, anonymous and Teacher denial,
foreign academy refusal, invalid payload status, missing learner, active and
waitlist capacity, duplicate and prerequisite refusal, audited preview with
unchanged domain and Identity rows, audit rollback and revoked-role refusal.

The retained shared enrollment suite passed 57 checks, including 15 mixed
Manual/service last-seat, duplicate and transfer races. The module exited 0.
The owned database, login and container were removed. Both original PENTA
services were restored and the inventory was preserved. Raw evidence remains
local. The full API suite and browser were outside this run.

## Next backend boundary

Enrollment needs a durable conversation-bound draft, exact approval, domain
execution and audited receipt readback. Finish create before adding status
changes and transfers. Model qualification, broader role and UI acceptance,
and production deployment stay gated. Cloud C24/C25 finance and C26 provider
work are separate. [Backend integration sequence](../../PENTA_HOST_IMPLEMENTATION_STATUS.md)
