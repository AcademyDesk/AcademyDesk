# BUG-SEC-0002 — CSV quoting does not neutralize spreadsheet formula cells

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — synthetic controller export bytes |
| Final verification | PARTIAL PASS / OPEN — spreadsheet and HTTP/SQL validation pending |
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
| Fix commit | Feature-branch checkpoint on codex/release-export-security; not merged |
| Retest result | 31 backend cases and 17 frontend encoder cases PASS; full API 1073 PASS |
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

## Cloud development retest — 2026-10-28

Synthetic regression tests reproduced 30 unsafe academy/audit export cases before
repair (30 failed, zero passed). A shared API CSV encoder and a report-download
encoder now prefix formula-like text with an apostrophe before CSV quoting.
Leading controls, whitespace/BOM before formula markers, ASCII and full-width
formula markers are covered. Source values are unchanged; typed backend negative
numbers remain numeric. Frontend report inputs are already strings, so a
formula-like string such as a negative number is conservatively exported as text.

After repair: 31 backend cases passed within the full 1,073-test API run
(zero failures/skips); 17 frontend encoder tests passed; TypeScript passed.
See [cloud export report](../REPORTS/CLOUD_EXPORT_SECURITY_2026-10-28.md).

This is exported-byte/encoder evidence, not actual spreadsheet execution proof.
Issue stays OPEN for real HTTP/SQL export checks, supported Excel/LibreOffice
behavior, browser download behavior, and critical release regression. CSV safety
must be reassessed if a consumer strips the prefix or re-exports the data.

## 29 October 2026 IST — Bounded spreadsheet consumer probe

Unchanged frontend encoder output was opened by real LibreOfficeDev26.8 alpha,
with formula evaluation enabled and forced-text import disabled. Raw =1+1
calibration evaluated to2;18 protected non-NUL rows contained zero formula cells.
An initial multi-file NUL-containing batch timed out. The final exact-byte
probe, using a fresh profile per document, passes both18 non-NUL and19 NUL-
containing rows with zero formulas; raw calibration still evaluates to2. The
timeout remains historical and was not established as a product defect. The
repeatable QA/tools/check-csv-spreadsheet.py records version/options/hashes and
fails on missing/timeout/formula output. This alpha-build check does not establish
supported stable Excel/Calc, backend HTTP/SQL or browser download acceptance. Issue remains OPEN. Full version, import options,
limitations and scratch evidence location are recorded in the
[continuation report](../REPORTS/CLOUD_WORK_ITEM_VALIDATION_2026-10-29.md).
