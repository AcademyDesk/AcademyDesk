# BUG-UI-0001 — Select opening above trigger uses maximum rather than actual menu height

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major interaction |
| Priority | P1 |
| Category | UI |
| Module | SHARED |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /student-fees; /teacher-payments; shared select consumers |
| API | N/A |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | UI-DROPDOWN-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/components/standard-select-field.tsx:45 |
| Class/function | updatePosition |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Render one/few options near bottom of viewport. Open at scroll top and mid-page, then inside nested scroll. Measure trigger and menu rectangles. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Render one/few options near bottom of viewport. Open at scroll top and mid-page, then inside nested scroll. Measure trigger and menu rectangles.

## Expected

Menu remains adjacent to trigger and within visible viewport, including short menus and keyboard-reduced height.

## Actual / evidence

Top is calculated by subtracting maxHeight, but actual content can be shorter. Forced minimum 150 can exceed available space. Historical screenshots show detached menus; current baseline browser reproduction pending.

Source snapshot:

```text
44:         left: Math.round(rect.left),
45:         top: Math.round(openAbove ? rect.top - maxHeight - 6 : rect.bottom + 6),
46:         width: Math.round(rect.width),
47:         maxHeight,
```

## Suspected root cause

Placement estimates menu height without measuring actual dimensions; no horizontal clamp/visualViewport handling.

## Business impact and blast radius

All StandardSelectField consumers; distinct from earlier clipping fixes.

## Related / required regression

UI-DROPDOWN-001: Geometry/hit-testing at six page scroll positions, three trigger positions, few/many options and resize.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
