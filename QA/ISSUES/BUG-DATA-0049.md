# BUG-DATA-0049 — Student import does not reject duplicate emails within the submitted CSV

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major identity integrity |
| Priority | P1 |
| Category | DATA |
| Module | STUDENT |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /data-operations |
| API | POST /api/academies/{academyId}/imports/students/validate; POST /imports/students |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | OPERATIONS-IMPORT-DUPLICATE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentImportsController.cs:18 |
| Class/function | Validate / Import |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Submit two syntactically valid rows whose normalized email differs only by case/whitespace, with no pre-existing student. Validate then import in isolated SQL fixtures; inspect result and persisted rows. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Submit two syntactically valid rows whose normalized email differs only by case/whitespace, with no pre-existing student. Validate then import in isolated SQL fixtures; inspect result and persisted rows.

## Expected

Validation and import reject duplicate normalized identities within the same file before any write, with row-specific feedback.

## Actual / evidence

Validation checks each email only for an @. Import queries existing database emails but never compares rows against one another before AddRange. Runtime NOT RUN; database uniqueness behavior is not assumed.

Source snapshot:

```text
17:         var rows = request.Rows ?? []; var emails = rows.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email!.Trim().ToLower()).ToList();
18:         var existing = await db.Students.Where(x => x.AcademyId == academyId && x.Email != null && emails.Contains(x.Email.ToLower())).Select(x => x.Email!.ToLower()).ToListAsync(token);
19:         if (existing.Count > 0) return Conflict(new { message = "Some email addresses already exist in this academy.", emails = existing });
20:         db.Students.AddRange(rows.Select(row => new Student { AcademyId = academyId, FirstName = row.FirstName.Trim(), LastName = row.LastName.Trim(), StudentNumber = row.StudentNumber?.Trim(), Email = row.Email?.Trim(), Phone = row.Phone?.Trim(), AdmissionDate = row.AdmissionDate ?? DateOnly.FromDateTime(DateTime.UtcNow) })); await db.SaveChangesAsync(token);
```

## Suspected root cause

Duplicate detection considers persisted rows only, not the proposed import set.

## Business impact and blast radius

Bulk student identity creation, later login/contact matching and import retry resolution.

## Related / required regression

OPERATIONS-IMPORT-DUPLICATE-001: Validate/import duplicate-case/whitespace/blank/null/new-versus-existing matrices, exact row errors, all-or-nothing writes and concurrent import behavior in real SQL.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
