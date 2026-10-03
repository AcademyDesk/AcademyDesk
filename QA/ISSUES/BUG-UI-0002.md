# BUG-UI-0002 — Shared modal and select omit expected keyboard focus behavior

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Moderate accessibility |
| Priority | P2 |
| Category | UI |
| Module | SHARED |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | Shared detail dialogs and selects |
| API | N/A |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | UI-A11Y-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/components/design-system/interactive.tsx:14 |
| Class/function | StandardDetailModal; StandardSelectField |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Open detail modal with keyboard, press Tab repeatedly, Shift+Tab, Escape, and close. Repeat select Arrow/Home/End selection. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Open detail modal with keyboard, press Tab repeatedly, Shift+Tab, Escape, and close. Repeat select Arrow/Home/End selection.

## Expected

Modal traps/restores focus and closes on Escape; select supports accessible keyboard navigation and meaningful labels.

## Actual / evidence

Modal has role/aria-modal but no focus management or keyboard handlers; select has mouse selection but no listbox keyboard behavior.

Source snapshot:

```text
13: /** Shared overlay for details opened from interactive tiles. */
14: export function StandardDetailModal({ title, eyebrow = "Details", onClose, children }: { title: string; eyebrow?: string; onClose: () => void; children: ReactNode }) {
15:   return <div className="standard-detail-modal" role="dialog" aria-modal="true" aria-label={title} onMouseDown={onClose}><article onMouseDown={(event) => event.stopPropagation()}><header><div><p>{eyebrow}</p><h2>{title}</h2></div><button type="button" onClick={onClose} aria-label="Close details">×</button></header>{children}</article></div>;
16: }
```

## Suspected root cause

ARIA roles added without matching interaction implementation.

## Business impact and blast radius

Shared detail tiles, all portal users relying on keyboard/screen reader.

## Related / required regression

UI-A11Y-001: Keyboard component tests plus browser focus assertions and human screen-reader check.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
