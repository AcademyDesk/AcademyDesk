# C10 calendar/date/input batch — 7 November 2026

Status: SOURCE REPAIR / CONTROLLED BATCH COMPLETE; original live closure OPEN.
Claim `de01c06` was published before edits.
Base C9 `ff662d006fabfeedbdceb06c510ca2190b0488cd`;
latest Desktop `f5cad5c4f314fd55410c1456f9e190e5f3a5d190` completed C6 source/live
acceptance and claims C8 music SQL next. Calendar/date/input work is disjoint.

Exact cloud-owned paths:

- `apps/web/src/app/calendar/page.tsx`
- `apps/web/src/app/academic-periods/page.tsx`
- `apps/web/src/app/holidays/page.tsx`
- `apps/web/src/app/curriculum/page.tsx`
- `apps/web/src/components/student-fee-arrangements.tsx`
- `QA/tools/calendar-details.test.cjs`
- `QA/tools/academic-inputs.test.cjs`
- `QA/tools/academic-inputs-browser.cjs`
- `QA/ISSUES/BUG-UI-0006.md`
- `QA/ISSUES/BUG-UI-0007.md`
- `QA/ISSUES/BUG-FUNC-0021.md`
- `QA/ISSUES/BUG-FUNC-0014.md`
- `QA/ISSUES/BUG-FUNC-0008.md`
- `QA/HANDOFF/C10_ACADEMIC_BATCH_2026-11-07.md`

Continue five records from the existing 118-record C9 batch queue. Reproduce
timezone-dependent calendar day/month grouping and civil-date shifts, cancelled
session join actions, blank sequence submission and fractional fee constraints.
Preserve session teacher/location, safe URL and original calendar assertions;
apply minimal presentation/input repairs after reproduction. One combined
exact-lock build/types/lint/full frontend/native browser run for final source.
Synthetic browser API fixtures are not real permission/SQL/critical acceptance.

No Desktop-active music/finance/Teacher, backend/API/SQL/session/PENTA source,
dependency, workflow, original vendor or shared integrator pointer edits. Preserve
issue histories and OPEN status until original complete closure requirements pass.
No new downloads, npm audit retry, environment/privacy/VPN/TCP change, models,
main merge, Azure or customer DB. Mini/Plus/Pro unqualified; Ultra deferred; the
candidate loading/training/inference/activation stop remains.

## Returned changes

Implementation `acaab4c10113036502858cbca3e08ca1ee64a98b`; final native fixture
`efec7bd2651dbd5009e9be002de2cbef54bc96f7` on
`codex/cloud-c10-academic-batch-20261107`. Eight implementation paths: five
application files, retained calendar assertions with seven new cases, and two
new QA files. No backend, shared component redesign or dependency change.

| Existing issue | Reproduced cause and resulting behavior |
| --- | --- |
| BUG-UI-0007 | Calendar used browser-local dates/months while displaying India time. Grid/navigation use UTC civil dates; instants group/filter by their India date. Both month boundaries agree across zones. |
| BUG-FUNC-0021 | Cancelled class rows retained a join URL. They stay visible with a Cancelled label and no meeting action; other session teacher/location/URL behavior remains. |
| BUG-UI-0006 | Local-noon date parsing shifted civil dates when formatted in India from Honolulu. Explicit UTC date-only parsing/formatting preserves the original API date. |
| BUG-FUNC-0014 | Blank sequence became numeric zero. Native required/min/step and direct handler guards reject blank/zero/fractional sequence; valid whole-number behavior remains. |
| BUG-FUNC-0008 | Default number step/min blocked legitimate fractional fees. Shared input min/step0.01 accepts positive paisa increments; the API contract is unchanged. |

Calendar's initial static-export state is stable and shows loading until its
current client snapshot initializes the India month/today. Initialization happens
with the asynchronous accepted data read; a cancelled read cannot update state
after unmount. No lint rule was disabled to initialize the clock. Calendar grid,
agenda and previous/next/Today navigation share the same civil-month coordinate.
Date-only changes do not alter event instants, server dates or scheduling policy.

## Actual verification

| Check | Observed result | Exact boundary |
| --- | --- | --- |
| Calendar TSX baseline | 39 PASS / seven FAIL | All39 prior teacher/location/safe-URL/filter/make-up assertions retained; three cancelled variants/four timezone boundaries failed |
| Date/input TSX baseline | Six PASS / six FAIL | Honolulu civil dates, invalid sequence and number constraints; positive controls retained |
| Corrected native baseline | 12 PASS /18 FAIL |30 cases on byte-exact archived C9 export, with corrected QA route prerequisite |
| Final targeted TSX |58/58 PASS, zero skips |46 calendar plus12 date/input;39 retained plus19 new, not58 new cases |
| Final native Chromium |30/30 PASS |Eight calendar, eight civil-date, eight sequence and six fractional-fee groups |
| Native requests/errors |Eight synthetic POSTs; zero JS/runtime/interceptor errors |Two valid curriculum and six subject-fee writes; all other contexts read-only/refused |
| Full frontend |1,980 PASS / three FAIL / zero skips |1,983 tests in72files; unchanged missing genuine communication SQL receipts |
| Exact-lock build/type/lint |PASS |Next16.4.3webpack,89 static generation entries, typecheck/noEmit; zero errors/63 existing warnings |
| Export/postbuild/source |PASS |570 files,87 unchanged segment aliases; all final export hashes unchanged during native checks |
| Browser/cleanup |PASS |Normal Chromium sandbox; exact owned temporary profile removed; no PENTA/model/inference requests |

Calendar native cases use a synthetic clock at the India month boundary and
Asia/Kolkata, UTC, Pacific/Honolulu and Pacific/Kiritimati browser zones. Both
320/light and1440/dark contexts check month/grid/agenda, cancelled link removal
and previous/next navigation. These are two viewport/theme pairs, not every
device/theme combination or physical-device acceptance. Civil-date pages run in
the four zones. Native form validity and keyboard submissions prove empty/zero/
fractional sequences send no request and valid sequence2does; positive fees1.50,
0.50and0.01post as exact numbers through the actual shared component.

The native fee route is `/student-fees`. The same component is used by
`/student-profile`, but this packet does not prove that other parent's full
permissions/selection/linked flows. All API responses and commits are intercepted
synthetic fixtures. There is no real Identity, SQL, new permission, cancellation
mutation, teacher assignment or finance policy qualification from these checks.

## Retained prerequisites and source binding

The first native process was interrupted and left RUNNING; its receipt remains
and is not a pass. The resumed initial native fixture used `/academic-governance`
instead of the actual `/academic-periods` read. Its four route failures are QA
prerequisites, not application defects; the receipt remains. The corrected30case
baseline ran in an owned separate workspace extracted from the archived original
570-file export; every byte matches the pre-repair manifest. No original source
or baseline result was rewritten. The accepted native result has no error entries.

An initial direct setState-in-effect calendar initialization failed lint. It was
replaced with initialization from the existing asynchronous data snapshot, with
an active-read guard. Lint, targeted tests and full regression were rerun on final
application source; initial failed lint and earlier overlapping run remain local.
The QA route correction changes only the native fixture. No API permission or
application assertion was relaxed to obtain a pass.

All58targeted checks and final full/browser results are source-bound, not added
together as independent application coverage. Three genuine SQL-receipt cases
remain strict failures in cloud. Older Desktop SQL passes retain their original
source/receipt scope. Raw initial/failure/final logs, baseline sources/archive,
final browser receipt, full summary and source/artifact hashes remain at
`/workspace/c10-academic-batch-20261107`, outside tracked source. No raw laptop/
customer data, installed dependency tree or compiled artifact is published.

## Integrate and finish acceptance

Review the eight implementation paths above and five issue/report updates over
C9. This branch inherits C9; integrate its financial batch separately according
to that handoff, then these disjoint files. Preserve Desktop's newer finance
audit/rejection and music SQL work; do not copy an older backend over it. Rebuild
the exact-lock local artifact because five application files changed.

Prioritized remaining original acceptance:

1. Real role/tenant/HTTP/SQL: calendar cancellation and active links from actual
   sessions, date-only year/term/holiday records, sequence persistence/publication
   and decimal subject fees. Check unauthorized/no-write and related parent flows.
2. Current critical linked workflows and browser timezone/device/accessibility
   matrix, including midnight/month/year transitions and live clock behavior.
   Synthetic clocks and CSS-pixel widths do not close physical-device gates.
3. Complete documented original module/critical/retest closure for each record.
   All five stay OPEN; grouping and passing controlled tests do not close them.

Reproduce with existing packages and installed Chromium:

```bash
node QA/tools/calendar-details.test.cjs
node QA/tools/academic-inputs.test.cjs
node QA/tools/run-frontend-tests.cjs
cd apps/web
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
./node_modules/.bin/tsc --noEmit
npm run lint
cd ../..
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/academic-inputs-browser.cjs
git diff --check
```

`QA_ACADEMIC_API_ORIGIN` accepts only canonical unprivileged HTTP127.0.0.1origins
matching the compiled artifact. Desktop may use54999. Owned static files and
explicit synthetic API routes are the only permitted requests. Unrecognized
requests and runtime errors fail; only an exact session-gone CDP reply after its
own target close is ignored. No borrowed PASS or changed skip.

C9's118-record queue remains the batch basis. C9andC10advance11records with
49and30native cases across their own source-bound artifacts; that is not a fresh
combined release proof or11closed issues. P0/P1, real host/device/privacy/restore/
current CI/images/audits/staging and release gates remain. Mini/Plus/Pro remain
unqualified, Ultra deferred; no candidate model loading/training/inference/
activation, new acquisition, environment/public-access/VPN/TCP/privacy change,
main merge, customer DB or Azure action.

Next: Sol High for actual domain/security/SQL acceptance; Medium for settled QA.
Check current exact-path claims before continuing another independent batch.
