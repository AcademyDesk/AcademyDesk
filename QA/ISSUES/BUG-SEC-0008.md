# BUG-SEC-0008 — Fee reminder queue bypasses external-channel consent and connection checks

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major notification policy inconsistency |
| Priority | P1 |
| Category | SEC |
| Module | FEES |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | Direct fee reminder API; UI uses InApp |
| API | POST fee-reminders |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-NOTIFICATION-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/FeeRemindersController.cs:27 |
| Class/function | Queue compared with Notifications.Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated fixture with an unpaid invoice, no communication consent and no connected external channel, POST Channel Email or WhatsApp. Compare resulting notification status to Notifications.Create for same recipient/channel. Repeat consent-only, connected-and-consented, null, blank and unknown channels. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In an isolated fixture with an unpaid invoice, no communication consent and no connected external channel, POST Channel Email or WhatsApp. Compare resulting notification status to Notifications.Create for same recipient/channel. Repeat consent-only, connected-and-consented, null, blank and unknown channels.

## Expected

All notification creation paths enforce the same applicable consent/connection eligibility, or fee-reminders explicitly reject unsupported external channels.

## Actual / evidence

Queue copies request.Channel and Notification defaults Status to Queued, without reading CommunicationPreferences or CommunicationChannels. Normal Notifications.Create produces BlockedConsent/AwaitingConnection under these conditions. Finance permission is enough for reminder queue.

Source snapshot:

```text
26:             if (balance <= 0) continue;
27:             dbContext.Notifications.Add(new Notification { AcademyId = academyId, RecipientId = invoice.StudentId, RecipientType = "Student", Title = "Fee payment reminder", Message = $"Your outstanding balance is {invoice.Currency} {balance:0.00}. Invoice: {invoice.InvoiceNumber}. Due date: {invoice.DueDate:yyyy-MM-dd}.", Channel = request.Channel ?? "InApp" });
28:             queued++;
29:         }
```

## Suspected root cause

Reminder producer writes notifications directly instead of applying shared notification eligibility checks.

## Business impact and blast radius

Persisted external-channel queue records through authorized finance API; no evidence of actual email/WhatsApp delivery or installed dispatcher is claimed.

## Related / required regression

SECURITY-NOTIFICATION-001: Real HTTP/SQL queue-status and no-send assertions with fake outbound adapters, channel allowlist and consent/connection matrix; never use real recipients. Test downstream delivery-time revalidation separately when a dispatcher is identified.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
