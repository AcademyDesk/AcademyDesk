# BUG-DATA-0049 — Student import does not reject duplicate emails within the submitted CSV

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED / BOUNDED REPAIR / RETEST |
| Final verification | API/native SQL bounded PASS; full acceptance NOT COMPLETE |
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
| Fix commit | Scoped student-import repair on codex/penta-search; see successor report and Git history |
| Retest result | 17 new API and57 native SQL/Identity/HTTP cases PASS; linked/device/full-critical pending |
| Regression result | 36 work-item +36 audit API checks and27 handler checks PASS; no full enterprise rerun |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

## 2026-10-11 successor — issue remains OPEN

[Repair report](../REPORTS/PHASE_2B_STUDENT_IMPORT_INTEGRITY_REPAIR.md) supersedes the historical NOT RUN source snapshot below for the named cases only. Starting HEAD ee7efc6bf6f6e8f0774524c469d9cee4c9d543e4: controller4/17 PASS; disposable SQL baseline a0c795be9b3040ada73dd7b8074a193c confirmed validation accepted and import persisted two case/trim duplicate emails. Final run134f21301c5f4d9d9d15403cb6efa0ce passed57 native SQL/HTTP cases, five concurrent duplicate pairs exactly200/409, one student/one audit per pair, audit rollback, tenant/role/optional values and bounds, no429. Exact owned DB/login/container cleanup verified.89 targeted API and27 handler checks PASS; browser proof separately recorded in report.

Duplicates within a file now reject400 before any domain write and identify every occurrence. Existing scoped normalized matches reject409. Whitespace optional fields become NULL. SQL imports serialize on the academy row and retain the lock through domain/audit completion. The validation endpoint retains existing successful-POST audit behaviour even when errors are returned; validation makes no student/domain writes. No global email unique index, legacy rewrite, arbitrary-mailbox normalization, new PENTA permissions or other-writer lock protocol is claimed. Linked browser-to-SQL, physical devices, complete CSV grammar, cross-tab/idempotency/other-writer races and full critical/release gates remain pending. Historical template fields below describe discovery, not current acceptance.

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
