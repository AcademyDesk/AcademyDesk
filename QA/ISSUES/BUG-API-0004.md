# BUG-API-0004 — Valid non-object JSON metadata can crash object-only readers

## Local SQL readback — 7 November 2026

Actual Student, Teacher, Admin and foreign Admin reads returned only the
expected announcements, including denial of malformed visibility metadata.
Compensation returned the empty summary for a non-object root and preserved
the legacy zero and date. Fresh SQL confirmed the metadata and audits were
unchanged. Linked critical and device acceptance remains open.
[Local SQL verification](../REPORTS/LOCAL_C13_C14_SQL_ACCEPTANCE_2026-11-07.md)

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED / CLOUD RETEST PASS |
| Final verification | NOT RUN (full issue) |
| Severity | Moderate malformed-data reliability |
| Priority | P2 |
| Category | API |
| Module | TEACHER |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher-payments; /portal |
| API | GET teacher compensation; portal announcements |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | API-JSON-001 |
| Evidence classification | Historical static trace retained; C12 compensation controller and authenticated HTTP reproduction |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | C12 baseline: 14/36 target failures (nine direct non-object cases and five HTTP500 cases); portal not run |
| Source | apps/api/Controllers/TeacherCompensationController.cs:54 |
| Class/function | Read / Property; portal announcement readers |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In disposable SQL seed CompensationJson as literal null, [], or a scalar. Read compensation. Repeat VariablesJson on announcement consumers. |
| API response | C12 five original authenticated HTTP500 cases become HTTP200; canonical zero/malformed-text cases and 401/403/404 controls pass |
| Database before/after | C12 unique InMemory metadata bytes unchanged, no audits; real SQL before/after pending |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Compensation reader: 375418c22be01315a93e48355482657c8a1ae27a; portal reader: 9edbf60a749c798560a1b1c0405a60af26b6d51a |
| Retest result | C12/C13 combined 55/55 PASS; real SQL and linked acceptance pending |
| Regression result | C13 full API 2,046/2,046 PASS, zero skips; linked SQL/critical/device acceptance pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In disposable SQL seed CompensationJson as literal null, [], or a scalar. Read compensation. Repeat VariablesJson on announcement consumers.

## Expected

Invalid-shape metadata is rejected on write or read safely as empty/error without a 500.

## Actual / evidence

EnumerateObject/TryGetProperty assumes Object; handlers catch JsonException, while wrong JsonValueKind can throw InvalidOperationException.

Source snapshot:

```text
53:     {
54:         foreach (var property in root.EnumerateObject())
55:             if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))) return property.Value;
56:         return null;
```

## Suspected root cause

Parsing success confused with shape validation.

## Business impact and blast radius

JSON metadata readers and legacy/partial records.

## Related / required regression

API-JSON-001: Table-driven missing/blank/null/object/array/scalar/malformed metadata through serializer and HTTP.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## C12 compensation partial return — 7 November 2026

Cloud started from `3ffaa9edb409fee3984cd0e3525ee6a18390a3cd` and
published the exact path claim before edits. The unchanged controller failed
14 of 36 new tests: nine direct GETs threw on non-object JSON and five
authenticated HTTP GETs returned 500. The other 22 tests passed. Original
failure logs and TRX remain outside Git under the run-owned cloud artifacts.

The property reader now checks that the JSON root is an object before
enumeration. Non-object records use the existing empty compensation response;
no stored metadata is rewritten. Final compensation tests pass 36/36, including
seven actual MVC/authentication/filter/serialization cases with synthetic
InMemory persistence. Missing/foreign teacher and cross-academy denials,
zero/decimal/null preservation, legacy aliases and case-insensitive keys pass.
These tests do not establish real SQL durability or production qualification.

Portal announcement readers remain unmodified because Desktop owns current
private-media/portal retesting. In particular, root `TryGetProperty` and field
`GetString` calls still need type guards and endpoint tests. This issue stays
OPEN for that reader and the existing linked SQL, browser, critical and device
closure requirements. See the [C12 handoff](../HANDOFF/C12_COMPENSATION_METADATA_2026-11-07.md)
for exact commands, source hashes, regression results and the local change request.

## C13 portal return — 7 November 2026

After Desktop completed its private-media/payment/restore packet, Cloud claimed
the announcement helpers separately. Original Portal baseline: nine test groups
passed and ten failed on malformed root/field reads or invalid start-date
visibility. The repair validates object/string shapes and hides malformed
records without rewriting them; canonical writer and legacy valid metadata pass.
Final combined target55/55 and full API2,046/2,046 pass, zero skips.

The19 portal groups cover160 authenticated HTTP scenarios, including Student,
Teacher, Admin, Owner and Guardian audience behavior, wrong-kind fields, future/
expired windows, real writer compatibility, tenant/status/recipient exclusions,
401/403, and unchanged notification/audit/read-receipt snapshots. Persistence
is explicitly synthetic InMemory; SQL/browser/device acceptance remains OPEN.
See [C13 commands and local handoff](../HANDOFF/C13_PORTAL_METADATA_2026-11-07.md).
