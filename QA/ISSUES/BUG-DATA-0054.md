# BUG-DATA-0054 — Changing music progress status erases a stored zero score

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major learner-progress data integrity |
| Priority | P1 |
| Category | DATA |
| Module | LEARNING |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /music |
| API | PATCH /api/academies/{academyId}/music-progress/{progressId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | LEARNING-PROGRESS-ZERO-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/music/page.tsx:146 |
| Class/function | updateProgress / MusicProgress.Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated academy create an assigned music-progress row, set Score to decimal zero through the API, and read it freshly. On the music page change only the status to Learning or Mastered. Capture the PATCH payload, then read the row in a fresh SQL context. Repeat with a positive score, null score, rejection and rapid status changes. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In an isolated academy create an assigned music-progress row, set Score to decimal zero through the API, and read it freshly. On the music page change only the status to Learning or Mastered. Capture the PATCH payload, then read the row in a fresh SQL context. Repeat with a positive score, null score, rejection and rapid status changes.

## Expected

Changing only status preserves an existing score of zero exactly; clearing a score requires an explicit edit. Rejected updates leave both status and score unchanged.

## Actual / evidence

The page uses score: item.score || null. Numeric zero is falsy and becomes null in the PATCH body. MusicProgress.Update replaces progress.Score with request.Score, so an ordinary status change can erase the stored zero. Static source trace; runtime NOT RUN.

Source snapshot:

```text
145:           targetDate: item.targetDate || null,
146:           score: item.score || null,
147:           notes: item.notes || null,
148:         }),
```

## Suspected root cause

Truthy fallback conflates numeric zero with missing score and the status action uses a full replacement DTO.

## Business impact and blast radius

Any student repertoire progress record with a legitimate zero score; later status changes silently alter assessment history.

## Related / required regression

LEARNING-PROGRESS-ZERO-001: Real browser + HTTP/SQL zero/positive/null score matrix, status-only change, fresh-context persistence check, stale response and denied no-write variants.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
