# BUG-FUNC-0027 — Recipient inbox exposes future and cancelled messages and overwrites their delivery state on read

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — bounded local repair verified; full closure pending |
| Final verification | Bounded predicate/controlled UI/real HTTP-SQL PASS; browser/critical NOT RUN |
| Severity | Major notification lifecycle failure |
| Priority | P1 |
| Category | FUNC |
| Module | FAMILY |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /portal; /teacher; /communications |
| API | GET /api/portal/notifications; PATCH notifications/{notificationId}/read |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMMUNICATION-INBOX-LIFECYCLE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PortalController.cs:69 |
| Class/function | Notifications / MarkNotificationRead / PortalNotifications |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Queue a valid synthetic recipient InApp message scheduled tomorrow and another cancelled message. Read recipient inbox before due time. Open its notification panel and inspect persisted Status. Include BlockedConsent/AwaitingConnection external-channel fixtures without delivering anything. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted receipt/eligibility/menu repair; no deployment |
| Retest result | 593 backend (36 new),14 controlled TSX,80 real Identity/HTTP/SQL cases PASS |
| Regression result | Migration upgrade/down/reapply; ownership/no-write/separate receipts/concurrent first-read PASS; wider gates NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local repair checkpoint — 2026-10-01

[Repair/evidence/limitations](../REPORTS/PHASE_2B_RECIPIENT_INBOX_REPAIR.md). Old API reproduced nine hidden-row disclosures and Cancelled→Read corruption; actual component baseline1/14 passed. Both endpoints now share due/channel/status eligibility; hidden IDs404 without writes. Separate per-account receipts preserve delivery state and first read timestamp, including concurrent first reads. Controlled menu confirms each response and retains failed/partial acknowledgments. Final593 backend/14 controlled UI/80 HTTP-SQL cases PASS; additive migration tested only in disposable SQL. Normal dev DB/API services NOT migrated/restarted. Legacy delivery state already lost to Read cannot be reconstructed. Browser/device, announcements, sender lifecycle, all-linked/critical/release gates remain OPEN; original static trace below is historical, not current behavior.

## Exact reproduction (historical baseline)

Queue a valid synthetic recipient InApp message scheduled tomorrow and another cancelled message. Read recipient inbox before due time. Open its notification panel and inspect persisted Status. Include BlockedConsent/AwaitingConnection external-channel fixtures without delivering anything.

## Expected

Future/cancelled/blocked delivery states are respected and read acknowledgment cannot turn blocked or cancelled delivery into Read; exposed history must be distinguished from active notifications.

## Actual / evidence

Inbox filters only academy/person/type, not schedule/channel/status, so all are returned immediately. Learner menu treats every non-Read row as unread and displays bodies. MarkNotificationRead unconditionally sets shared Status=Read, erasing cancelled/blocked/queued state. Runtime NOT RUN.

Source snapshot:

```text
68:         var notifications = await db.Notifications.AsNoTracking()
69:             .Where(x => x.AcademyId == user.AcademyId && x.RecipientId == recipientId && recipientTypes.Contains(x.RecipientType))
70:             .OrderByDescending(x => x.CreatedAtUtc).Take(100)
71:             .Select(x => new PortalNotificationSummary(x.Id, x.Title, x.Message, x.Channel, x.Status, x.CreatedAtUtc, x.SentAtUtc))
```

## Suspected root cause

Recipient visibility lacks lifecycle gating and read state shares a field with delivery state.

## Business impact and blast radius

Student/guardian/teacher notification endpoint and learner bell; early information disclosure to intended recipient, not a cross-tenant claim.

## Related / required regression

COMMUNICATION-INBOX-LIFECYCLE-001: Full HTTP/browser scheduled due-boundary, cancelled, blocked, failed, sent/read and recipient/tenant ownership matrix. Separate delivery eligibility from per-user read receipts; preserve failure history and assert no premature body visibility.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
