# D69 — connected academy calendar reads

24 November 2026. Academy base `1bacb1e3d8baafcfc11ae378744f5475844a5115`;
Mini base `f8ff21ec996cbf659c0062ce5356f006b3f46f39`. Mini published
`a36b4636e5afd8cf2e877d7fa2b59b41b2cbcafe` on the existing conversation feature
branch. Host paired workflow pins it and now requires31native query checks.
Existing worktrees, original evidence/model artifacts and unrelated scaffolds
preserved. No main merge, production database, Azure or model activation.

## Completed integration

- [x] Session search/open in the SAME typed query conversation, composer,
  encrypted state/history and GET-only receipt recovery. Maximum ten descriptors;
  current stored Owner/Admin and existing Core module policy independently apply.
- [x] Real bounded ClassSessions/Batches/Teachers projection, with tenant checks
  on all related rows. Filter batch name/code, assigned teacher, dates and domain
  status; stable start/ID ordering, first10records plus explicit more-results notice.
- [x] Verified class start/end, delivery mode, status, teacher and batch name.
  No room, meeting links, contact details, attendance, financial or security data.
  Manual link opens Schedule, not a fictitious selected-class detail route.
- [x] Host resolves academy time zone, local day boundaries and DST. Inclusive
  YYYY-MM-DD range capped31days; Today/Tomorrow/Yesterday/Next7Days supported.
  Missing date shows an explicitly stated next7localday window. Unknown zones,
  impossible/reversed/overlong ranges and ambiguous/invalid midnight refuse reads.
  Model does not provide authoritative zone, UTC offset or arithmetic.
- [x] Resolved exact dates enter saved context. Later ordinal reads cannot drift
  at midnight. Only same-class IDs may be opened; teacher/batch references are
  not class IDs. Student-class requests remain unsupported until enrollment joins
  are implemented, rather than returning all classes as the student's calendar.
- [x] Requested calendar cannot silently become teacher/batch directory data.
  Read conversation rejects known scheduling mutations and attendance requests.
  Existing guided scheduling, task drafts and Manual remain separate/preserved.
- [x] Strict host/client financial-only/calendar-only fields; old directory and
  finance wire shapes omit new null fields. UI shows actual UTC instants in the
  verified academy zone, with correct status instead of generic Active labels.
  Existing premium shell, themes, scroll containment and recovery are retained.

## Final checks — distinct evidence layers

| Check | Observed final result | Boundary |
| --- | --- | --- |
| Affected .NET | 304 passed, zero failed/skipped | Same prior280-case selection plus24new calendar cases; query class84 |
| API/test/harness builds | Zero warnings/errors | Final local source |
| Mini Linux | 260 passed, zero failed/skipped; one inherited Starlette/httpx warning | 53query cases plus retained task/API/native/operational checks; model-free |
| C# -> Python schema | Actual ten-tool request accepted | No inference |
| Client | 163 passed | 38query cases, retained guided/portal/legacy/exact-money cases including35portal-boundary assertions |
| Native SQL/Identity/API | 31 query checks passed, no HTTP429 | Explicit synthetic planner, real HTTP/authentication/owned SQL |
| Existing Foundation/migrations | PASS;5/5stale-tab races,86application+7Identity migrations | Audit rollback/current authority/recovery retained; owned database/login/container removed |
| Query browser | 16 passed | Owner/Admin,320/1440,both themes, Finance+calendar and no-Finance+calendar catalogs; synthetic APIs |
| Shell browser | 36 passed | Six roles,three widths,both themes; synthetic APIs |
| Next16.4.5 cached build/typecheck/export | PASS,92pages | Isolated D: source copy/cache; not exact locked install |
| Changed frontend ESLint | PASS,zero warnings | Same isolated cache |

Final native run `11c7218473e64671bd19d89c983fb02f`,32.5seconds. Earlier30-check
run `8349738ada954939b94bd6d7056758b1` overlaps and is not an extra31outcomes.
Real SQL verifies local00:00in Asia/Kolkata starts at previousUTC18:30; inclusive
start and exclusive next-midnight, teacher-name predicate and cross-tenant
class/batch/teacher exclusion. Actual saved/recovered receipt retains exact
scope/class IDs without redispatch. Thirteen next-day classes produce ten rows
with HasMore and explicit truncation. Invalid range fails; class rows/times/
assignments/status and ledger counts/totals remain unchanged. Existing audit
fault and current actor revocation tests still roll back/deny unaudited results.

Unit cases also exercise23-hour America/New_York day, invalid Havana midnight,
bad zone/date/ranges, Finance independence, denied mutations and wrong student
calendar inference. Browser exercises teacher -> student correction -> lost
reply -> GET recovery -> reload -> balances where permitted -> calendar -> reload
without anotherPOST. Screenshots inspected at320px; actual class details are
visible and composer does not cover them. Browser sandbox retained; owned
profiles removed and no contexts remain. No physical-device or real-model claim.

Private `.build-check/d69-calendar/`: `d69-affected-source-final.trx`,
`mini-calendar.xml`, `host-query-vector.json`, `client.log`,
`native-result-1795514842451.json` and matching log; browser folders
`2026-11-24T10-03-39.234Z-9240` and `2026-11-24T10-06-13.662Z-12716`.
Shell `.build-check/penta-workspace-shell/2026-11-24T10-06-32.139Z-15304/`.
Raw evidence, runtime credentials and original archives remain private.

## Retained failures and environment limits

1. Initial catalog assertions still expected six/eight tools; updated exact
   expected catalogs to eight/ten for the newly connected Core calendar tools.
   Finance remains separately absent/present. Initial failedTRX retained.
2. Mutation-denial regression caught singular class missing from the regex.
   Corrected plural/singular object handling; retained failedTRX and reran final
   304cases. No mutation tool exists or was enabled by this change.
3. Docker engine was stopped externally; started it with the existing CLI.
   No model service was started. A native attempt then refused below the existing
   3GiBRAM guard before creating SQL. Released .NET build servers/caches, retained
   the guard, and reran successfully. No prune, volume deletion or driver change.
4. Legacy client test required this checkout's absent TypeScript install. Private
   test-only module resolver pointed that exact dependency to the existing D:
   cache; production/test assertions unchanged. Final163checks pass. No downloads.
5. Local Next cache differs from locked optional/development dependencies.
   Its successful build is compatibility evidence; D69exact CI remains required.

## Exact CI, release boundaries and next work

D68 ordinary exact-source CI is now SUCCESS at `1bacb1e`:
https://github.com/AcademyDesk/AcademyDesk/actions/runs/45339778681
with2,754API tests, zero failed/skipped; locked npm-ci/audit0, typecheck/lint,
querySQL24, remaining guarded SQL/private-media checks and92-page export.
This is historical for D69; new exact-source CI must run after publication.

Required paired Core job still fails the missing read-only
`PENTA_SOURCE_READ_TOKEN` secret. Owner must configure it securely in AcademyDesk
Actions with private PentaMini Contents read access; connector access is not that
credential. Job remains required, never skipped. No token is printed or committed.

Latest completed Mini base f8ff21e run:
https://github.com/AcademyDesk/PentaMini/actions/runs/45339505684
13failed,1,878passed,1skipped,63errors,5passed subtests. Genuine original archive/
evidence and original D:-bound files remain missing in CI. No substitute receipts,
modified hashes, skips or weakened assertions. D69Mini exact CI is independently
pending publication;260selected passes cannot qualify its full suite.

- [ ] Explicitly approved connected real-model accuracy/latency/grounding and
  adversarial outcomes; no new weights/inference packet initiated. Prior one-off
  4Bcomparison completed; a continuation is not new packet authorization.
- [ ] Attendance and remaining domain readers; enrollment-aware calendar joins,
  cross-domain multi-step observations and richer natural grounded responses.
- [ ] Protected task approval/execution and student+batch+enrollment journey;
  Teacher/Student/Guardian/Platform parity, remaining P/E/N/T/A capabilities.
- [ ] Original critical security/financial/authentication records, combined
  browser/SQL journeys, physical devices, staging/restore/rollback and Azure gates.

Queries remain default-off Development/Testing. BUG-AI-0001 remains OPEN;
neither90%eligible outcome completion nor production readiness is accepted.
Stay Sol High for the next integration/security batch.
