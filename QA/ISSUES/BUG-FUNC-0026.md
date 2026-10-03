# BUG-FUNC-0026 — Retained template overrides the selected message or banner channel

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED —10 controlled UI/12 controller baseline failures; local repair verified |
| Final verification | BOUNDED PASS; live browser/device/lifecycle/critical NOT RUN |
| Severity | Major message routing mismatch |
| Priority | P1 |
| Category | FUNC |
| Module | COMMUNICATION |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /communications |
| API | POST notifications |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMMUNICATION-CHANNEL-001 |
| Evidence classification | Controlled actual TSX/controller product failures;50 TSX/557 backend/56 real HTTP-SQL repaired checks |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/communications/page.tsx:127 |
| Class/function | recipientType switch / create / Notifications.Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | [Repair report](../REPORTS/PHASE_2B_COMMUNICATION_CHANNEL_REPAIR.md), baseline/final logs/TRX and pinned source snapshot; original excerpt retained below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Select an Email or WhatsApp template, then change message type to Portal banner and publish valid content. Inspect payload/result and compare a banner composed without first selecting a template. Also change Channel to InApp while retaining the template in direct compose. |
| API response |22 actual editor POST payloads; full response and scoped GET equal fresh SQL, intended channel/status/message/banner metadata |
| Database before/after |56 real HTTP-SQL cases, rejected no notice/audit/source writes, preserved unrelated/foreign rows and existing two-log success audit |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Two-source local repair, uncommitted; no deployment |
| Retest result |50 actual controlled TSX/41 new backend tests PASS |
| Regression result |557 full backend/56 real Identity-HTTP-SQL PASS; TypeScript/build PASS; lint0 errors/1 inherited warning |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Select an Email or WhatsApp template, then change message type to Portal banner and publish valid content. Inspect payload/result and compare a banner composed without first selecting a template. Also change Channel to InApp while retaining the template in direct compose.

## Expected

Visible banner mode or selected channel determines delivery, or incompatible template is explicitly cleared/rejected before submit.

## Accepted baseline / historical evidence

Changing recipient type clears only recipientId. Hidden templateId is posted for banners even though channel is forced InApp; backend then overwrites channel from template. Academy recipient has no consent preference so resulting external-channel notification is BlockedConsent, while UI announces a running banner and portal announcement reader does not filter channel. Direct compose channel edits are similarly overridden. Runtime NOT RUN.

Source snapshot:

```text
126:           scheduledAtUtc,
127:           templateId: templateId || null,
128:           variables: isAnnouncement
129:             ? { ...variables, audiences: announcementAudience }
```

## Suspected root cause

Hidden template state survives mode switch and backend channel precedence conflicts with visible selection.

## Business impact and blast radius

Banner channel/status consistency and ordinary template/manual channel changes; no external send claimed.

## Related / required regression

COMMUNICATION-CHANNEL-001: Browser/HTTP all recipient mode switches with/without templates, explicit channel changes, template removal and fresh readback; assert intended channel, status, message, audience and no stale hidden IDs.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local bounded repair — 2026-10-01

[Verification report](../REPORTS/PHASE_2B_COMMUNICATION_CHANNEL_REPAIR.md): incompatible template links are detached on mode/channel change while visible text is retained; banner payload always InApp/null template. API rejects conflicting requests before writes, preserves matching and non-banner unspecified-channel fallback.10 UI/12 controller baseline product failures retained;50 controlled TSX/557 backend (41 new)/56 real HTTP-SQL PASS. Consent/connection/access/schema/prior repairs/normal binaries unchanged. No customer/dev database/Azure/outbound messages/commit/deployment. Browser/mobile/inbox/dispatch lifecycle/critical acceptance pending; Status OPEN.
