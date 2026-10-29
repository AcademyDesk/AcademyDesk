# Events timezone consistency — focused continuation

2026-10-14. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD remains `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Continues the explicit finding in [visual checkpoint](BATCH_2_MAKEUP_HOLIDAYS_EVENTS_PENTA.md), not a new enterprise audit or full module acceptance.

## Decision and evidence

Events previously parsed offset-free wall-clock inputs through the device timezone while always rendering saved events in `Asia/Kolkata`. Prior non-IST browser proof recorded10:00 Los Angeles→17:00 UTC→22:30 IST. Source review confirmed explicit IST in class scheduling, make-ups and calendar display. The Events API accepts UTC instants and independently rejects end<=start; its list marks stored UTC values as UTC without civil-time reconstruction.

Use an **explicit IST Events input/display contract**, matching the existing calendar/scheduling experience, independent of the viewing device. Labels and form guidance now state IST/UTC+05:30. This is not a silent change to the academy's configurable timezone: `Academy.TimeZone` exists and Admin can configure it, but a general academy-IANA-timezone scheduling migration is not implemented by this patch. International academy-timezone adoption remains separate and must cover all affected scheduling surfaces/DST ambiguity, not only Events. No locale inference from browser, country or model.

## Implementation

- `event-time.ts` converts complete date+HH:mm through explicit+05:30. A round trip rejects impossible civil dates instead of allowing JavaScript normalization. Exact minutes, midnight/year rollover and leap days are retained. Device DST gaps/folds cannot alter explicit IST.
- Before writing, the Events handler validates complete/real dates and end strictly after start, with understandable messages and draft retention. Server interval validation remains authoritative; no bypass/replacement of server auth or validation. Existing synchronous pending guards and saved/failed-refresh/uncertain outcome messages remain.
- API request field names/optional nulls/default type/time and status semantics unchanged; only future form wall-clock interpretation is corrected. Saved rows retain their original UTC instants. No database migration, timestamp rewrite, history inference, API/schema/permission or other module timezone change.
- Readback explicitly appends IST and uses the same named timezone as the input contract. Earlier visual/required-control/layout improvements preserved.

## Verification checkpoint

- Final webpack build/TypeScript/86 exports PASS; changed page/helper ESLint0 errors/0 warnings.
-189 focused checks PASS:19 Events handler cases (15 retained plus invalid equal/reversed/impossible/incomplete cases),15 new pure conversion checks and155 retained make-up/schedule/calendar checks. Pure conversion executes in four isolated process timezones (Kolkata/Los Angeles/London/UTC), with exact10:37→05:07UTC, Jan1 midnight crossing previous UTC year, leap day, London gap and Los Angeles fold dates. Malformed/offset-injected dates/times rejected.
- Existing browser feedback matrix expanded to72:three device timezones×320/768/1440×both themes×success/failed-refresh/400/500-committed uncertainty. **72/72 PASS**, `QA/EVIDENCE/events-feedback-browser-1791998784397/result.json`. Exact minute payload assertions intentionally change from browser-local17:00/18:00UTC to explicit IST05:07/06:07UTC for10:37/11:37 input; no relaxed status/save/draft/unknown-outcome assertions. With the10 retained calendar cases,82 final-export synthetic browser cases PASS, separate from189 focused checks.
- Each normal browser journey verifies fresh list and read-only reload: the new event displays10:37 IST and an existing00:17 IST event keeps its original prior-day18:47 UTC bytes. Existing UTC fixtures are never rewritten. Browser transport is synthetic, not database persistence proof.
- Calendar readability regression10/10 PASS (`QA/EVIDENCE/calendar-readability-1791998807457/result.json`), including non-IST civil-day projection/long-content/phone scroll. Vendor6 hashes unchanged, scope whitespace/script parsing checked. No app-source changes after final export.

Phone light screenshot inspected: explicit timezone guidance/labels and original/new event times agree after reload. Backend source `EventsController` inspected but unchanged; real HTTP/SQL, migration-history, physical devices and enterprise gates not rerun/closed by this frontend fix. Prior failures and old browser-local evidence retained with their source-point meaning, not relabelled as corrected behaviour.

Final raw source SHA256: Events page `87fbb86eb81e84d18785cc09213eb2b42ec6856bc0e400720744fb5bc6908a3b`; helper `e15df038f1ae4f93eaf5e4fdda538f9f6daf7ad2e2f2794c1ba9b78162d1b733`. Unmodified API controller `46efda17e34b337c5dcc56047af1889ed560a798359913d3f94a13d4640a5bec` and empty controller diff confirmed. No schema/data changes required by this boundary correction.

## Remaining / handoff

Existing old records may already reflect a mistaken device-time interpretation. They cannot be automatically corrected without evidence of user intent. Any correction requires an authorized supported domain workflow, reviewed record/time and audit; do not mass shift timestamps. General configurable academy timezone/DST support, UTC wire/SQL source proof, full Events conditional/role/tenant/device acceptance and existing lint/security/runtime/release gates remain open.

Next settled work: existing E3 finance/academics/communications and remaining portal visual states (Sol Medium). Consequential timezone generalization, backend/security/finance, real AI tool/host integration and release gates use Sol High. No new Mini contract dependency/project switch for this frontend conversion. Continue existing PENTA UI2/Mini qualification handoffs and90% eligible conversational target without claiming them complete.

No staging/commit/push/main/Azure/production DB/container action. Existing dirty work/evidence preserved, index empty, main remains `f7af512f884ca5fe8ac3ddb287c852492592e709`.
