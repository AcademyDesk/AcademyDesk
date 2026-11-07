# BUG-DATA-0054 — Changing music progress status erases a stored zero score

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLED-TSX-REPRODUCED / partial repair; C8 controlled Chromium score/recovery checks PASS |
| Final verification | Controlled TSX/Chromium PASS; real Identity/HTTP/SQL, devices and critical acceptance pending |
| Severity | Major learner-progress data integrity |
| Priority | P1 |
| Category | DATA |
| Module | LEARNING |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /music |
| API | PATCH /api/academies/{academyId}/music-progress/{progressId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | Chromium 320/1440 CSS-pixel light/dark checks PASS; physical devices NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | LEARNING-PROGRESS-ZERO-001 |
| Evidence classification | Historical static/TSX zero-loss reproduction; current controlled native Chromium PASS; live persistence pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/music/page.tsx:146 |
| Class/function | updateProgress / MusicProgress.Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Historical source excerpt below; C8 source-bound report linked below |
| Screenshot | Current 320/light music-page snapshot retained locally; register/full-page visual acceptance pending |
| Console logs | Final controlled run: zero JavaScript console/runtime/interceptor errors; earlier failures retained |
| API request | In an isolated academy create an assigned music-progress row, set Score to decimal zero through the API, and read it freshly. On the music page change only the status to Learning or Mastered. Capture the PATCH payload, then read the row in a fresh SQL context. Repeat with a positive score, null score, rejection and rapid status changes. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Development-branch repair; see latest dated checkpoint/report |
| Retest result | C8: 42 native Chromium groups and 12 recovery tests PASS; existing 51 value-preservation tests PASS |
| Regression result | C8 full frontend 1,930 PASS / three missing-genuine-SQL-receipt FAIL / zero skips; historical counts below retained |
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

## Cloud controlled repair — 2026-10-28

Both zero-score status-change payloads reproduced zero-to-null loss; the positive,
null and absent controls already passed. Replaced truthy fallback with nullish
fallback for score only. All eight Learning/Mastered × zero/positive/null/absent
payload cases PASS, retaining exact target/status/date/notes behavior. API/storage
and other music handlers remain unchanged.

Remain OPEN for actual HTTP/SQL fresh-row persistence, denied no-write and stale
responses, browser/device and critical regression. See
[value-preservation report](../REPORTS/CLOUD_VALUE_PRESERVATION_2026-10-28.md).

## C8 controlled browser and status recovery — 2026-11-07

Implementation `44c613d885fca59266d670f5c70a29133568e103`, started 6 November and
verified after environment recovery on 7 November. The existing nullish score
serialization already passed all 32 score/theme/viewport groups on unchanged
source. C8 additionally reproduced six native-browser failure paths: no recovery
after transport loss (both committed and uncommitted synthetic outcomes), failed
post-save reload, unlocked controls during a pending PATCH or fresh read, and an
empty placeholder PATCH. The original receipt records 36 PASS / six FAIL; the
first six TSX recovery tests recorded one PASS / five FAIL.

The narrow music-page repair serializes status PATCH and its fresh read, blocks
duplicate/stale callbacks and invalid statuses, and binds payloads to the latest
loaded progress row. Existing score/date/notes serialization is preserved. Any
rejected or unconfirmed save requires an explicit read retry before another
status mutation. A successful save with failed refresh is reported separately.
Read retry never replays a PATCH. Pending reads/saves cannot write React state
after unmount. No backend, permissions, score policy, package or schema changes.

Final real Chromium/React/DOM **42/42 PASS**, including 32 zero/positive/null/absent
score contexts and ten status/recovery controls. Exactly 79 intercepted synthetic
PATCH bodies, zero live API writes, no JavaScript runtime/console errors. Normal
Chromium sandbox and owned-profile cleanup PASS. All 12 new TSX controls and 51
existing value-preservation tests PASS; those are included in the full suite,
not additional to its 1,930 passes. Exact-lock Next16.4.3 build, static postbuild,
TypeScript and lint PASS (zero errors / existing 63 warnings).

Remain **OPEN** for real authorized HTTP/SQL and fresh score persistence, actor/
tenant denials, audits/rollback, other-client/server concurrency, physical devices
and required critical/release acceptance. The UI serialization lock does not
prevent an independent client from changing the same row. Assignment and piece
creation acceptance are separate from these status checks. See the
[C8 handoff](../HANDOFF/C8_MUSIC_PROGRESS_2026-11-06.md) for retained failures,
exact source, artifacts and prioritized Desktop tasks.
