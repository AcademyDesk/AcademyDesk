# BUG-DATA-0054 — Changing music progress status erases a stored zero score

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Actual-handler reproduction/serialization repair and native SQL/linked zero verification; broader acceptance OPEN |
| Final verification |72 handler/96 synthetic +17 native SQL/four linked zero journeys PASS; local pending/stale/recovery repaired; wider concurrency/device/critical pending |
| Severity | Major learner-progress data integrity |
| Priority | P1 |
| Category | DATA |
| Module | LEARNING |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /music |
| API | PATCH /api/academies/{academyId}/music-progress/{progressId} |
| Environment | Disposable local SQL/Identity/TestServer and linked production export; production state not inferred |
| Device/viewport | Desktop browser320/1440px light/dark PASS; physical Android/iOS NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | LEARNING-PROGRESS-ZERO-001 |
| Evidence classification | Actual TSX handler, synthetic export, native SQL/HTTP and linked export-to-disposable-SQL verification |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Frozen actual handler fails four zero cases; final20 cases pass; production frequency not measured |
| Source | apps/web/src/app/music/page.tsx:146 |
| Class/function | updateProgress / MusicProgress.Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Historical source excerpt below; current repair and SQL/linked reports in Exact reproduction |
| Screenshot | Four linked final viewport screenshots local in QA/EVIDENCE/music-progress-live-1791728438464; pinned baseline screenshots not captured |
| Console logs | Linked page/console error checks PASS; local receipts in report |
| API request | In an isolated academy create an assigned music-progress row, set Score to decimal zero through the API, and read it freshly. On the music page change only the status to Learning or Mastered. Capture the PATCH payload, then read the row in a fresh SQL context. Repeat with a positive score, null score, rejection and rapid status changes. |
| API response |17 native expected-status controls and eight linked PATCH200 with authoritative readback PASS |
| Database before/after | Fresh SQL exact score/status/date/notes and foreign sentinel/count verified; rejected requests preserve progress/audit snapshot |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Scoped feature serialization repair2026-10-11; see feature history/report |
| Retest result | Bounded zero/positive/null native and linked zero PASS; page-local pending/stale/recovery PASS; cross-tab/linked positive-null/device/critical OPEN |
| Regression result |20 score handlers retained plus stricter one-request guard and51 feedback cases PASS;96 synthetic browsers and native/linked rerun PASS |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

2026-10-11 [save/status/recovery repair](../REPORTS/PHASE_2B_MUSIC_PROGRESS_FEEDBACK_REPAIR.md): same-tick characterization replaced with strict one-write assertion; shared write/readback guard, lifetime/read-version and stale local-row protection, durable polite outcomes and explicit refresh recovery.72 handlers,96 synthetic browser cases and unchanged17 native/four linked zero journeys PASS. No API/version/idempotency change; issue stays OPEN for cross-tab metadata/linked positive-null/create-assign fault/device/critical/release gates. Older entries below are historical, superseded only for these bounded page-local checks.

2026-10-11 [SQL/linked verification](../REPORTS/PHASE_2B_MUSIC_PROGRESS_SQL_LINKED.md):17 real controls PASS; zero/positive/null fresh SQL persistence and invalid/role/tenant/missing-row no-write, foreign sentinel retained. Linked zero-score journeys/owned cleanup in report. A separate characterization reproduces two same-tick writes; NOT a guard pass. Issue remains OPEN/RETEST for async/stale/recovery, linked positive/null/device/critical/release gates.

2026-10-11 [bounded repair](../REPORTS/PHASE_2B_MUSIC_PROGRESS_ZERO_REPAIR.md): actual frozen handler16/20 → final20/20; one `??` fallback preserves0.48 synthetic browser cases PASS. Historical source finding below retained. No backend/domain/schema change; issue OPEN/RETEST pending native SQL/linked/role/stale/rapid/device/critical release gates.

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
