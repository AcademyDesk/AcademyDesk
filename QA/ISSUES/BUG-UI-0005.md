# BUG-UI-0005 — Follow-ups tile opens conversion details instead of follow-ups

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Moderate navigation mismatch |
| Priority | P2 |
| Category | UI |
| Module | SALES |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /sales-marketing |
| API | N/A |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SALES-NAV-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/sales-marketing/page.tsx:299 |
| Class/function | StandardDetailModal Open details link |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Open Follow-ups due tile on Sales Overview, then activate Open details. Compare route/title with directly opening the follow-ups view. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Open Follow-ups due tile on Sales Overview, then activate Open details. Compare route/title with directly opening the follow-ups view.

## Expected

Open details goes to /sales-marketing?view=follow-ups and retains the follow-up context.

## Actual / evidence

Modal href handles Leads and Trial bookings explicitly and routes all remaining tiles to conversion, including Follow-ups due. Runtime NOT RUN.

Source snapshot:

```text
298:               <Link
299:                 href={tileDetail === "Leads" || tileDetail === "Total leads" ? "/leads" : tileDetail === "Trial bookings" ? "/trial-bookings" : "/sales-marketing?view=conversion"}
300:                 className="enterprise-action-button enterprise-action-button-secondary"
301:                 onClick={() => setTileDetail(null)}
```

## Suspected root cause

Follow-up target is omitted from modal navigation branch despite correct target existing in leadTiles metadata.

## Business impact and blast radius

Sales follow-up navigation on mobile and desktop; modal list itself uses the correct due records.

## Related / required regression

SALES-NAV-001: Browser tile-to-modal-to-route table for Leads/Total leads/Follow-ups due/Trial bookings/Converted/Conversion rate, back navigation, keyboard and direct query views.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
