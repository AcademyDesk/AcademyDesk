# Calendar live browser/API/SQL continuation — 2026-10-09

Issues: BUG-FUNC-0021 / BUG-UI-0007 remain **OPEN**.
Starting HEAD: `09be2ed1b1b5467d15caab03c2998b63a1cc4988`.
Worktree: `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`.
Route: Sol High / Codex. Existing enterprise program, not a new audit.

## Bounded repair

Events and make-up GET list projections now explicitly mark known UTC column
values as UTC, preserving ticks, tenant selection, lifecycle, location and rows.
SQL datetime2 loses DateTimeKind; without Z the browser treats these instants
as local time. This is the same contract repair already accepted for class and
Teacher calendar projections, not a conversion, migration or input-policy change.
Create/next-scheduled make-up response normalization is outside this GET slice.

Pre-fix: two Unspecified-kind unit cases failed on absent Z; two Utc-kind cases
passed. Fresh disposable SQL run `0d2976bb987f4613bbb003589f3203c8` also stopped
at `events/startUtc` without Z. The earlier `340919239bdf46d7b3a917e8464b053b`
attempt stopped at Events 403: synthetic fixture lacked Certificates entitlement.
Only owned fixture entitlements were corrected; product access filters unchanged.

Four new combined event/make-up tests cover Unspecified/Utc, ordinary/Cancelled
history, foreign academy exclusion, exact serialized start/end, ticks and full
read-only row preservation. **Full API 1,130/1,130 PASS**, zero skipped, on the
preserved working snapshot. Unrelated local Mini source is not published here.
Harness build: zero warnings/errors. JavaScript/PowerShell parsing PASS.
Five invalid origin variants reject before SQL creation (non-loopback, wrong
scheme, same port, path and query). Existing runner cleanup guards retained.

## Actual local integration, not recorded-response replay

Opt-in `QA_CALENDAR_BROWSER=1` extends the existing CalendarDetails module after
its unchanged 19 cases. Original module selection, SQL guard, runtime inventory,
Identity/tenant/no-write tests, migrations and exact-owned cleanup stay intact.
The loopback bridge forwards to the real ASP.NET application TestServer pipeline
and disposable Docker SQL, not a deployed API container. Browser port forwarding
copies actual HTTP responses; no mock JSON, bearer injection or disabled authority.
Login uses the real UI and synthetic Identity actors. Exported Calendar/Schedule
source is unchanged; prior TypeScript/lint/83-page build retained, not rerun.

Only synthetic academy modules needed by this fixture are enabled: Core,
TeacherClassroom, Certificates, MultiBranch, Finance. Lower-plan workflows are
not accepted by this fixture. Bridge binds exact loopback, permits reads/login
and PUT for only its sixteen seeded sessions, blocks unrelated mutations and
requires the exact run-owner marker for its stop file. Logs omit tokens/passwords.
Each successful PUT is checked in fresh SQL; final domain snapshots must retain
all original fields except the sixteen expected statuses, history counts, other
sessions, batches, events, make-ups and notifications.

## Browser test setup failures retained

- `863e6eef39764cad9e33390153e08ae4`: class/make-up shared the batch name;
  strict locator was ambiguous. Narrowed to the real class grid selector.
- `b9183a05a9d547e2a1e75b54069bf913`: one full live case passed; rapid subsequent
  reads hit unchanged HTTP 429. Separate actors do not isolate the current limiter
  because it executes before authentication. No limiter/middleware change made.
- `debaf153ce404b729b5453e019e7f8cc`: one full case passed, 95 HTTP 200 responses;
  conservative global pacing paused a request longer than the default 30s locator
  timeout. Increased QA wait bounds and made route teardown errors controlled.

Final test conservatively paces at most 95 requests per 61 seconds, lets fixture
setup's limiter window expire, retains no-429 acceptance and never retries PUTs.
Failed logs/screenshots remain local. Only the five exact failed-run SQL containers
above were removed after matching run labels, absent host folders and inactive
harness checks. This removes disposable test databases, not academy data;
failed DB contents are not retained after container removal, evidence is retained.
Mini and unrelated containers were not pruned or changed.

## Final scoped validation

Run `f618f3ed232540179cef0c7f4d0c14fe`, loopback SQL port 52815:
**16/16 live combinations PASS** (UTC/Kolkata/Los Angeles/Sydney × 320/1440px
× light/dark). Real UI login and Schedule status selection perform exactly
sixteen HTTP PUT 200 cancellations. Fresh SQL verifies each, then Calendar
navigation and hard reload retain Cancelled status, original IST time/day and
history, without visible/hidden meeting access. Filters/collapse and October/
January month/year boundaries retain correct class/event/make-up placement.
October agenda count remains 21 (19 classes + event + make-up); January has 2.
Theme state and document-width guard pass; the latter does not prove inner-card
readability (see the separately registered visual issue below).

**840 forwarded real HTTP responses, all 200**, including 16 logins, 16 PUTs,
80 session GETs and 48 each event/make-up GETs. Zero console/page errors or HTTP
429; no mock bodies or request retries. Final fresh SQL snapshots preserve all
non-status session fields, unrelated sessions and the listed other domain rows.
Both event/make-up sources assert UTC Z, exact fresh SQL ticks/status/counts and
read-only snapshots. Original 19 CalendarDetails cases, runtime tenant/login
controls, 88 application + 7 Identity migrations, negative marker cleanup,
database/login teardown and exact container stop/removal **PASS**, exit 0.
API bridge, frontend server and SQL port no longer listen; host folder absent.

Final log: `QA/EVIDENCE/calendar-live-20261009/sql-live-final.log`.
SHA256: `228587efed4f8041a594a457674b61a0127c64c2262c69a7460ea2003b1a2809`.
Live result/screenshots: `QA/EVIDENCE/calendar-live-browser-1791563279438/`.
Mobile light UTC and dark Sydney screenshots inspected, leading to BUG-UI-0008.

Original synthetic exported-DOM lifecycle/timezone matrix **32/32 PASS**.
Controlled default TSX **324/324 PASS** (81 per timezone), original assertions
preserved; completed current SQL capture adds Scheduled and cancellation cases
for supplementary **332/332 PASS** (83 per timezone).
Capture-loader guards **18/18 PASS**. No physical Android/iOS or spoken assistive
technology acceptance, no all-critical-regression/enterprise/release closure.

Evidence stays untracked under `QA/EVIDENCE/calendar-live-20261009/`,
`calendar-live-browser-*`, `calendar-timezone-1791563027626`,
`calendar-timezone-1791563974314` and
`calendar-timezone-browser-1791563146378`. API TRX is in the first folder's
`test-results/calendar-live-api.trx`; baseline/setup failures are not overwritten.

## Continuity

No main merge, Azure deployment, product authorization rewrite, database
migration, Mini benchmark/training/runtime/tool expansion or new testing plan.
Mini remains saved **86/94, CONTRACT READY / ENGINE BLOCKED**; existing N2u
qualification and linked/security/15-turn/human gates remain controlling.

Mobile screenshot review additionally found useful subject/date/make-up text
cut short at 320px; [BUG-UI-0008](../ISSUES/BUG-UI-0008.md) is OPEN. The functional
DOM assertions do not establish full visual readability or mobile acceptance.

Next independent packet: Sol Medium for this bounded calendar readability repair,
then Schedule success-feedback/browser regression (BUG-FUNC-0003); Sol High if
domain/access changes become necessary. Physical-device and original critical suites remain open for
the two calendar issues; do not close them on this scoped browser run alone.
