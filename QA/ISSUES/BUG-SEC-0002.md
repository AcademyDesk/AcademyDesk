# BUG-SEC-0002 — CSV quoting does not neutralize spreadsheet formula cells

2026-10-23 [Native Reports actual CSV](../REPORTS/API_IMAGE_REPORT_BROWSER_2026-10-23.md):
four real-login mobile/desktop light/dark downloads preserve exact SQL amounts,
one own row/formula-like student name with leading tab inside quoted field and
no foreign sentinel. Exact bytes/hashes retained, not spreadsheet import/save/
reopen or physical-device acceptance. Issue remains OPEN; no universal safety claim.

## Bounded local export repair — 21 October 2026

Academy register, audit-log and browser Reports downloads now share the same
spreadsheet-view text policy: quote/escape each cell and prefix formula-like or
leading-control text with a tab **inside** the quoted cell. Whitespace, format
marks, apostrophe prefixes and full-width formula markers are covered. Typed
numeric values are not treated as untrusted strings. Stored records, financial
calculations, authorization, filters and source query limits are unchanged.

Final SQL successor: run7a2076155e0744069a61a9d18e9c16b6 passed32actual export
Identity/HTTP/SQL checks plus retained Access regression, exit0/160.8s/102+7
migrations/no429/exact owned cleanup. The first below-3GiB attempt created no
resources; RAM later recovered naturally and the unchanged guard passed. No restart
or weakened assertions. SQL gate now PASS, not issue/client/device/release closure.

27 .NET checks (including scoped read-only InMemory controller output), 27 actual
TypeScript checks, four native browser Blob/anchor downloads, full API 2861/2861,
TypeScript and targeted lint passed. Native SQL/Identity/HTTP gate is prepared
and builds, but a below-3GiB host-memory preflight stopped it before container
creation; that first attempt is historical and superseded by the final SQL run above.
Excel/LibreOffice import/save/reopen and physical
devices remain unaccepted. No universal or lossless CSV-safety claim.

The export UI explains the intentional leading-tab data and programmatic-import
trade-off. [Report](../REPORTS/CSV_EXPORT_GUARD_2026-10-21.md). Issue remains **OPEN**;
the original static baseline below is retained, not the latest implementation state.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major export security risk |
| Priority | P1 |
| Category | SEC |
| Module | OPERATIONS |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /data-operations; /reports; /activity |
| API | GET exports/{resource}; audit export |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-EXPORT-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/AcademyExportsController.cs:26 |
| Class/function | Csv; AuditLogs.ExportCsv; reports downloadCsv |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed a synthetic text value beginning with =, +, -, @ or a tab, export CSV, inspect bytes; open only in a sandbox without active external links. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Seed a synthetic text value beginning with =, +, -, @ or a tab, export CSV, inspect bytes; open only in a sandbox without active external links.

## Expected

Untrusted exported text is represented as inert text for supported spreadsheet consumers.

## Actual / evidence

CSV helper doubles quotes but leaves formula-leading characters intact. Client report and audit export need the same policy review.

Source snapshot:

```text
25:     }
26:     private static string Csv(params object?[] values) => string.Join(',', values.Select(value => $"\"{(value?.ToString() ?? string.Empty).Replace("\"", "\"\"")}\""));
27: }
28:
```

## Suspected root cause

CSV syntax escaping is not spreadsheet formula neutralization.

## Business impact and blast radius

Downloaded customer-entered names/notes/references.

## Related / required regression

SECURITY-EXPORT-001: Export content tests for leading formula/control characters plus sandbox spreadsheet review.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
