# BUG-FUNC-0025 — Provider setup promises a secure connection step that is not implemented

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major integration setup dead end |
| Priority | P1 |
| Category | FUNC |
| Module | COMMUNICATION |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /communication-settings; /meeting-links |
| API | PUT communication-settings/{channel} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMMUNICATION-CONNECTION-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/CommunicationSettingsController.cs:69 |
| Class/function | Save / CommunicationSettingsPage secure connection section |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Save valid metadata for a new synthetic Meeting channel with Configured status, then reload settings and meeting links. Trace any available connect/test callback action; compare Email/WhatsApp setup without contacting providers. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Save valid metadata for a new synthetic Meeting channel with Configured status, then reload settings and meeting links. Trace any available connect/test callback action; compare Email/WhatsApp setup without contacting providers.

## Expected

Saving metadata is clearly distinguished from a verified provider connection, with either an implemented secure next step or an explicit unavailable-feature notice.

## Actual / evidence

Save persists metadata but never sets HasSecureConnection; entity defaults false. Source-wide inspection found only flag readers/declaration, no provider connect/callback/test/disconnect workflow. UI promises a next step but renders no such control, and meeting Create remains disabled on normally created rows. Runtime NOT RUN; this describes audited application source, not unknown external deployments.

Source snapshot:

```text
68:         item.ExternalAccountReference = Clean(request.ExternalAccountReference);
69:         item.MessagesEnabled = request.MessagesEnabled && status == "Configured";
70:         item.UpdatedAtUtc = DateTime.UtcNow;
71:
```

## Suspected root cause

Connection metadata screen was delivered ahead of the secure integration lifecycle it advertises.

## Business impact and blast radius

Fresh Google/Zoom/Microsoft meeting setup and external-channel readiness; Configured is not connection proof.

## Related / required regression

COMMUNICATION-CONNECTION-001: Future isolated integration lifecycle: configure/connect/cancel/denied consent/invalid callback/refresh expiry/revoke/provider switch/test failure, with provider stubs and protected tenant-bound credentials. Assert configuration alone never claims verified connection; do not set a database flag as a workaround.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
