# BUG-API-0004 — Valid non-object JSON metadata can crash object-only readers

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — starting-source controller/private-reader tests; bounded repair implemented |
| Final verification | 35 new +45 retained tests,72 new native metadata +47 retained audience cases PASS |
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
| Evidence classification | Original static trace; successor actual controller/EF InMemory and pure-reader reproduction |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/TeacherCompensationController.cs:54 |
| Class/function | Read / Property; portal announcement readers |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In disposable SQL seed CompensationJson as literal null, [], or a scalar. Read compensation. Repeat VariablesJson on announcement consumers. |
| API response | Successor72-case native matrix: safe200 metadata summaries/lists,401/403/404 access controls; no500/429 |
| Database before/after | Fresh captured teacher/notification/receipt/domain-audit/platform-audit snapshots unchanged after each native GET |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Scoped JSON-kind repair; publication SHA supplied in final Git handoff |
| Retest result | 35/35 metadata cases +72/72 native SQL/Identity/HTTP PASS; run6c6bd1fba98048f1b764393272e621f6 |
| Regression result | 41 retained NotificationChannel +4 HolidayRoute tests and47 native AnnouncementAudience cases PASS; broader critical/device gates pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

2026-10-11 successor: [metadata shape repair](../REPORTS/PHASE_2B_METADATA_SHAPE_REPAIR.md). Frozen new35-case baseline10 PASS/25 FAIL; final80/80 includes45 unchanged channel/holiday cases. Narrow kind checks preserve valid metadata and avoid storage rewrites.72 native metadata and47 retained audience cases PASS, no429,88+7 migrations and exact owned cleanup. Issue remains OPEN for browser/device/full-critical gates. Original static trace below is historical.

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
