# D71/D72 — recorded attendance and Aurora AI workbench

Follow-up verification: ordinary exact-source CI for41470488c183817672673e1f092bf2a342e20d88
PASSED, including2,799API tests and all39native query SQL/Identity/API checks.
The earlier NOT LOCALLY RUN entries below retain their original scope; CI supplies
the current native result, not a real-model pass. Required paired Core job still
fails missing private-source credential. See D73_REPLY_BOUNDARY_AND_MODEL_PACKET_2026-11-24.md
for exact job links, Mini failures and the new unexecuted4Bpacket.

24 November2026. Academy base66bb16a1e88d0e9c6fc98677061e434a19615914;
Mini basea36b4636e5afd8cf2e877d7fa2b59b41b2cbcafe. Mini published
9dafedb28e3f8c3250659ee60be9cc2abd4b7c47, paired workflow pinned. Continues the
existing program and owner-requested frontend direction; no main/Azure/model load.

## Recorded attendance connection

- Adds attendance search/open to SAME query catalog, encrypted history/recovery
  and mandatory audit. Current stored Owner/Admin/tenant/Core permission governs
  it. Twelve tools with Finance, ten without; no expanded role/model authority.
- Joins stored marks with own-academy students/classes/batches/assigned teachers.
  Bounds/order10rows + HasMore; filters student name/code,batch,teacher,canonical
  status and31-day max class-start date range. Attendance defaults Today; calendar
  still defaults next7days. Host pins academy-local dates and UTC class instants.
- Present/Absent/Late/Excused/Online marks are distinct from class/directory
  status. Existing case variations are read canonically without modifying rows.
  Mark-update timestamp is not arrival; it is not the date filter. No notes,
  medical/contact data, money or inferred missing marks. Historical marks do not
  require a currently active enrollment. Staff attendance/aggregates/marking and
  unmarked rosters remain unsupported. Only same-mark ordinal references allowed.
- Date refinement can change only through while retaining validated from; both
  Mini and host still enforce the final range. No model SQL/clock or permissions.
- Current native harness adds eight checks: private/cross-tenant/status/date
  projection, semantics, GET-only recovery, ordinal, staff-denial, paging, mutation
  denial and no inferred absence for an enrolled learner. CI requires39checks.

## Owner's Aurora workbench

- AI only: left navigation/current response history/connections/protected flows;
  centre chat/composer; right receipt-backed artifacts/source/status/action review.
  Graphite/violet Aura dark and matching Prism light; Geist and responsive drawers.
  All role AI entries use same shell. No Manual page/layout/service was redesigned.
- Context is an in-memory projection of existing validated receipts, cleared on
  account/view changes; no extra data authority, browser persistence or tools.
  Publication is tied to the original account epoch and blocked after a session
  event, including late effects; browser cases assert old artifacts disappear.
  No fake previous conversations, CRM connection, approval counts or model replies.
- Mini/Plus/Pro/Ultra picker shows Mini evaluation only, Plus/Pro not connected,
  Ultra deferred. Choosing UI cannot load weights or route to another model.
  Unavailable P/E/N/T/A engines remain disabled with !help, rather than fake tools.
- Connections reflect actual workspace/provider status. Review links preserve
  existing protected guided workflows; no approval queue is claimed loaded.
- New conversation explicitly confirms replacing the current recovery pointer;
  it creates only a conversation, not a domain/tool dispatch. Exact source facts
  remain available beside chat; money strings are not recalculated in the UI.

## Observed checks and exact limits

| Layer | Result | Limits |
| --- | --- | --- |
| Affected .NET |325passed, zero failed/skipped |21new attendance cases, query class105; retained prior304 |
| API/test/harness builds |Zero warnings/errors |Final local source; separate output from running demo |
| Mini interpreter |26new pure cases PASS on existing Windows Python |No Linux serving/model acceptance inferred |
| C# -> Python |Actual twelve-tool host vector PASS |Six attendance fields and existing descriptors |
| Client |174passed, zero failed/skipped |11new attendance cases + retained163 |
| Query browser |16passed |Owner/Admin,320/1440,both themes; with/without Finance, calendar+attendance; synthetic APIs |
| Shell browser |36passed |Six roles,three widths,both themes; model picker disabled tiers, help/dialog focus and three-column layout |
| Cached Next16.4.5 build/typecheck/export |PASS,92pages |D: copy/cache; single build worker,1536MiBNode cap; exact lock CI still required |
| Changed frontend lint |PASS,zero warnings |Same isolated dependency cache |
| Actual preview API |Six real Identity logins/sessions200; anonymous401; own students retained |No model or fake planner; current preserved D70backend66bb16a |
| Actual preview browser |Owner login/AI landing, light/dark, model dialog, Manual navigation PASS |Actual SQL/Identity/API; no intercepted network, physical device or live model |
| D71native39packet |COMPILED, NOT LOCALLY RUN |Owner's active exclusive demo preserved; required CI must execute it |
| Current Mini Linux/full suite |NOT LOCALLY RERUN |Use exact CI; original archive blockers remain |

Private `.build-check/d71-attendance/`: `d71-affected.trx`,
`mini-attendance-windows.xml`, `host-query-vector.json`, `client-final.log`.
Query screenshots/receipts remain under `browser/`; the final privacy-regression
reruns are recorded there separately from preceding passes. Final isolated export
is `.build-check/d72-final/web/out/`; earlier screenshot/export evidence retained.
Shell screenshots remain under `.build-check/penta-workspace-shell/`.
Actual preview evidence `.build-check/d72-aurora/live-preview-check.json` and
`live-browser-e1d81f55-d536-406b-a784-a7cb2b052778/result.json`/screenshots.
Browser contexts/profile removed, sandbox retained. No raw evidence/credentials
or local server scripts committed. Initial duplicate/narrow test passes overlap.

## Retained failures

1. DOM wait returned an element (React reference graph too deep); changed the
   predicate to Boolean while retaining the same required DOM condition.
2. Reloading before recovery/history finished cancelled an intercepted request;
   test now waits for actual aria-busy=false before controlled reload. No network
   errors suppressed and all recovery/no-extra-dispatch assertions retained.
3. New-conversation QA helper searched raw text without the decorative plus;
   used the actual navigation button containing its label. App action unchanged;
   explicit confirmation/new-session/private-context reset assertions pass.
4. Live login QA initially targeted input[type=email]; real existing field is
   #username. Corrected test selector, preserved failure receipt; actual login
   succeeds. No password values printed or recorded in screenshots.

## Preview and CI/release status

Owner preview http://127.0.0.1:3001/login uses SAME accounts/passwords/private
LOGIN.txt as ACTIVE_DEMO. Actual data/backend66bb16a preserved on3000; no owner
work was reset to show visuals. New same-origin local preview proxy forwards
actual authentication/domain calls, preserves tenant authorization and avoids
changing production CORS. It stops with original demo expiry25November03:46IST.
Live query provider is Unavailable; new D71reader is not enabled in old backend.

Historical D69ordinary exact CI succeeded:
https://github.com/AcademyDesk/AcademyDesk/actions/runs/45372326746
2,778API tests, zero failed/skipped, locked npm-ci/audit0, SQL31, private-media and
full92-page export. This is NOT current D71/D72combined-source acceptance.
Required paired Core job still fails missing read-only PENTA_SOURCE_READ_TOKEN.
Keep it required; owner must configure it securely in Actions, not paste a token.

Mini a36b463 full CI: https://github.com/AcademyDesk/PentaMini/actions/runs/45371962084
13failed,1,894passed,1skipped,63errors,5passed subtests; genuine original archive/
source files absent. Never fabricate/archive-alias/change hashes/skip assertions.
Current9dafedb full CI and current Academy CI must be checked after publication.

- [ ] Current39-check native/locked/Linux-serving/full exact CI and paired source.
- [ ] Connected real-model semantics, context fit, latency and grounded answer
  quality; Mini/Plus/Pro remain production-unqualified, Ultra deferred.
- [ ] Remaining domain/role tools, protected compound approval/execution and
  student+batch+enrollment outcome; real connector/brain/memory qualification.
- [ ] Original critical security/financial/auth records, device/staging/restore/
  rollback/Azure quality gates. BUG-AI-0001 OPEN;90%outcomes not accepted.

No model loading/inference/downloads, paid resources, production data, main merge
or Azure. Stay Sol High for integration; visual refinement alone may use Sol Medium.
