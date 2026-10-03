# BUG-FUNC-0024 — Meeting creation and history are display-only placeholders

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major meeting workflow unavailable |
| Priority | P1 |
| Category | FUNC |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /meeting-links |
| API | No meeting mutation or history request is issued |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-MEETING-CREATE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/meeting-links/page.tsx:166 |
| Class/function | submit / meetings state / edit |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In a future isolated browser fixture provide a Meeting channel with matching provider and hasSecureConnection=true, complete required title/date and press Create meeting link. Observe requests, state and history. Compare disconnected control. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In a future isolated browser fixture provide a Meeting channel with matching provider and hasSecureConnection=true, complete required title/date and press Create meeting link. Observe requests, state and history. Compare disconnected control.

## Expected

An available Create action performs a real authorized operation and displays the saved link/history, or clearly declares the feature unavailable without promising creation.

## Actual / evidence

Connected submit only calls setMessage. meetings is initialized to [] without a setter or fetch; history is permanently empty. Edit has no record ID or persistence path. Recurrence rule input is not captured. Runtime NOT RUN; fixture bypasses connection availability only to isolate this independent missing workflow.

Source snapshot:

```text
165:   }
166:   function submit(e: FormEvent) {
167:     e.preventDefault();
168:     if (!connected)
```

## Suspected root cause

UI scaffold was exposed without meeting persistence/provider/history implementation.

## Business impact and blast radius

Meeting creation, recurring rules, access/chat/recording promises and advertised history/edit experience.

## Related / required regression

SCHEDULE-MEETING-CREATE-001: Isolated component/full HTTP tests with stubbed provider transport only; verify actual payload, authorized stored meeting, provider result and durable history. Until implemented assert truthful unavailable state. Never create real provider meetings or send invites during QA.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
