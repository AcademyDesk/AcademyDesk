# BUG-DATA-0033 — Assessment grades depend on the save path and can remain stale after score changes

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Accepted static finding / local repair verified |
| Final verification | Backend396, controlled frontend15 and real HTTP/SQL39 PASS; browser/device pending |
| Severity | Major grading inconsistency |
| Priority | P1 |
| Category | DATA |
| Module | ACADEMIC |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /assessments; /teacher |
| API | POST academy assessments/{id}/results; POST teacher assessments/{id}/results |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMIC-GRADE-001 |
| Evidence classification | Accepted source finding reused; repaired grade/mode behavior verified locally |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/assessments/page.tsx:377 |
| Class/function | ResultRow / AssessmentResults.Upsert / TeacherPortal.RecordAssessmentResult |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures create a scheme with passingPercent50 and assessment max100. Save score80 with empty grade through admin UI; after readback change only score to20 and save. Separately submit score80 with grade:null through the assigned teacher endpoint for the same scheme-linked assessment. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local working tree only; migration staged; no commit/push/deployment |
| Retest result | 20 new backend tests plus full396/396 and controlled frontend15/15 PASS |
| Regression result | Fresh Identity/HTTP/SQL39 cases + legacy migration preservation PASS; see report |
| Closure notes | Remain OPEN for browser/device, wider lifecycle, critical and release gates; original runtime baseline not replayed in this slice |

## Exact reproduction

In isolated fixtures create a scheme with passingPercent50 and assessment max100. Save score80 with empty grade through admin UI; after readback change only score to20 and save. Separately submit score80 with grade:null through the assigned teacher endpoint for the same scheme-linked assessment.

## Expected

Automatic grading remains linked to the score unless the user deliberately selects a documented manual override; equivalent result operations use the same grading policy.

## Actual / evidence

Admin first save generates Pass and loads it into editable grade state. Score-only edits send that existing Pass as an explicit grade, so API preserves it even for score20. Teacher endpoint never consults GradingSchemeId and assigns a null grade for the equivalent null-grade request. Runtime NOT RUN.

## Local repair — 2026-10-01

The accepted source evidence above describes the original behavior. This slice reuses it rather than repeating the original audit. [Repair report](../REPORTS/PHASE_2B_ASSESSMENT_GRADE_REPAIR.md): Admin and Teacher result writers now share automatic/manual grading policy; explicit automatic mode recalculates on every score save. Manual grades retain their value. Nullable stored mode preserves unknown legacy provenance, and the Admin form offers an explicit mode selector. Null/empty/whitespace grades from older clients use automatic calculation; older explicit nonblank grades remain manual overrides.

Verified: backend396/396 including20 new tests,15 controlled form tests, TypeScript,39 real Identity/HTTP/SQL cases and actual old-schema→new-schema legacy-result preservation. Student and Guardian published-result API projections match SQL. Page lint improves from2 errors/2 warnings to1 existing error/2 warnings; gate remains failing. Issue stays OPEN; no actual browser/device, full critical suite or deployment performed.

Source snapshot:

```text
376:     setScore(result?.score.toString() ?? "");
377:     setGrade(result?.grade ?? "");
378:     setRemarks(result?.remarks ?? "");
379:   }, [result]);
```

## Suspected root cause

Automatically derived and manual grades share one undifferentiated field; teacher and administrator result writers implement different grading logic.

## Business impact and blast radius

Student and guardian academic results can show a grade inconsistent with current score or differ by submitting portal.

## Related / required regression

ACADEMIC-GRADE-001: Browser score edits above/below/exact threshold, deliberate override versus auto mode, null/empty/whitespace grade, administrator/teacher parity and fresh result/portal reads. Preserve intentionally approved manual overrides.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
