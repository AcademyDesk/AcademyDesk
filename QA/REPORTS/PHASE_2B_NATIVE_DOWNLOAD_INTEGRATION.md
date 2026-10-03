# Phase 2B — native private attachment downloads

2026-09-30. Bounded local repair for SECURITY-FILE-001 / BUG-SEC-0001; **OPEN / FIX-IN-PROGRESS / PARTIAL PASS**, not release approval. Base commit `20bb6047f9edf733ac8e2a226621cc582ec54b3c` plus [45 final source/preservation hashes](PHASE_2B_NATIVE_DOWNLOAD_SOURCE_SNAPSHOT.json). Accepted Astra audits and previous failing evidence are retained, not repeated. No commit, push, Azure configuration/deployment or development/Azure database migration.

## Changed behavior

The Teacher/family private Download action no longer needs a giant JavaScript Blob or the browser-specific file-save picker. It obtains a short-lived read credential through the existing authenticated API transport, then posts that credential to the exact attachment URL using a native form and isolated hidden target. The server streams an attachment with the original filename. This removes the application's former 64 MiB buffered-download path and the custom-title filename/extension problem; it does **not** prove every device can save 2 GB. Previews remain bounded at 32 MiB and unsupported formats use Download.

`POST /api/class-material-downloads/tickets` requires normal account authentication, safe HTTPS transport outside explicitly local Development/Testing, an exact approved web Origin, one current authorized resource binding, and completed Blob session or an existing legacy file. It returns a no-store credential valid for **60 seconds**. A separate Data Protection purpose protects the encrypted/authenticated payload containing identity ID, academy, security stamp, exact path and origin. Expired payloads are rejected by the framework's time-limited protector. [Microsoft time-limited Data Protection documentation](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/limited-lifetime-payloads?view=aspnetcore-10.0).

A named authentication scheme is selected only by the `PrivateMaterialRead` policy on the three attachment read actions. It accepts only a small URL-encoded POST body with one `ticket` field, known positive Content-Length up to 4096, no query, the bound path/origin, and safe transport. It rejects tampering/expiry, oversized/extra fields and changed/inactive identity or security stamp. The content action then repeats current role, tenant, active academy/batch, recipient/enrollment/guardian and publication checks before streaming. Ordinary bearer-only POST cannot substitute for this credential; normal bearer GET/HEAD remains unchanged. The credential cannot authenticate other APIs or issue additional credentials. [Microsoft named-scheme authorization documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/authorize-with-a-specific-scheme?view=aspnetcore-10.0).

No account bearer/refresh token is put in a form, query string, attachment URL or cookie. No signed/public Blob URL, credentialed CORS widening, GET-ticket route or anonymous file bypass was added. Ticket issuance and body values must remain redacted from deployment logs. Origin binding is an additional browser-context check, not a substitute for the protected credential; non-browser callers can forge Origin but still need that secret and current file access. The credential is a narrowly scoped bearer capability, reusable within its 60-second window, not a device-bound or single-use guarantee. Expiry/revocation is checked when a request begins, not continuously during an already-running response. Native GET-based download resumption without a new credential is not promised.

The client validates the ticket reply and fixed path before handoff; clears/removes the temporary credential form immediately and retains the hidden response target until component cleanup. It says **Download requested**, not that a file was durably saved. Cancellation after handoff belongs to the browser download manager. Original filenames are controlled by the server attachment header. Native forms and attachment response headers are standard browser mechanisms, but compatibility still needs actual device evidence. [MDN form submission](https://developer.mozilla.org/en-US/docs/Web/API/HTMLFormElement/submit), [attachment headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Disposition).

The target uses `strict-origin-when-cross-origin`, not a no-referrer policy that can suppress Origin on basic form posts. Origin/referrer-policy behavior is a device-test gate. [MDN Origin header behavior](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Origin).

An additional client path defect was corrected: existing uploader filenames use 32-character GUID-N names, whereas the previous private-path helper expected 36 characters. Those private legacy file actions now match both teacher-materials and learning-resources correctly. Blob paths also use exact GUID syntax, not a loose 36-character hyphen wildcard.

## Executed verification and boundaries

| Layer | Actual result |
| --- | --- |
| API unit suite, final-source repeat | **85/85 PASS**, zero failures/skips. Fourteen new ticket/purpose/expiry/path/origin/transport cases added to previous 71 |
| Client/protocol/SSR tests, final-source repeat | **22/22 PASS**; four new ticket-reply, cancellation, form-handoff and GUID-N path tests. HTTP/storage/DOM doubles are explicit, not browser integration |
| SqlHarness build and frontend typecheck | PASS; build zero errors/warnings; `npx tsc --noEmit --incremental false` exit 0 |
| Changed helper/component ESLint | Zero errors / one existing dynamic-preview image warning; no suppression. Full workspace lint/build NOT RUN; earlier page lint failures remain open |
| Real Identity/HTTP/SQL/Azurite | Initial **69 assertions PASS**, expanded legacy-folder run **75 assertions PASS**. See source timing distinction below |
| Desktop browser native transport probe | **PASS: actual 68 MiB download, exact saved length and SHA-256**, actual handoff helper with a synthetic API, not application/SQL/Identity end-to-end |
| Final-source real HTTP repeat | Initial repeat blocked by host disk exhaustion; recovered fresh run **75 assertions PASS**, exit 0. Failed attempt retained separately |
| Full Teacher/family portal, actual Android/iOS, 2 GB transfer, full critical suite | **NOT RUN** |

[Final check outcomes](../EVIDENCE/logs/phase-2b-native-download-checks.log), [22 client tests](../EVIDENCE/logs/phase-2b-native-download-client.log), [real HTTP runs](../EVIDENCE/logs/phase-2b-native-download-http-runs.log), [disk-failed attempt](../EVIDENCE/logs/phase-2b-native-download-repeat-disk-failure.log) and [recovered final-source repeat](../EVIDENCE/logs/phase-2b-native-download-recovered-repeat.log) are retained separately. One intermediate compile error (nonconstant Identity scheme in an attribute) was corrected by using the registered explicit policy; no failing HTTP authorization assertion was hidden.

| Real backend run | UTC start | SQL port | Duration | Outcome |
| --- | --- | --- | --- | --- |
| `461e36f824b84c74933592d6425daaf2` | 2026-09-30T16:32:09.9744895Z | 65357 | 62.5 s | 69 assertions PASS; exit 0 |
| `5a9979b1606e4880b663b6879fa81892` | 2026-09-30T16:38:27.2581006Z | 57760 | 51.5 s | Expanded 75 assertions PASS; exit 0 |
| `0466893ccb2246f0af09b4a3b9a79dcd` | 2026-09-30T16:40:58.4136807Z | 54469 | 45.2 s | DB setup followed by host-folder IOException: insufficient C: space; HTTP suite NOT RUN |
| `886f22dde626448e8a4c31a1bba362f3` | 2026-09-30T16:50:13.7007317Z | 52334 | 49.6 s | Recovered final-source 75 assertions PASS; exit 0 |
| `ed2a6264385545b38a23edb8a9bbffcc` | 2026-09-30T16:51:38.5155929Z | 57543 | 42.1 s | Original leak reproduction and 29 legacy HTTP checks PASS; exit 0 |

The first 75-assertion pass occurred before final defensive malformed-Base64 handling and case-insensitive Content-Type parsing were added. After an independently retained disk-failed attempt, one fresh final-source run passed all 75 assertions. The final 85-unit/22-client passes and typecheck also cover current sources. Do not describe the 69/75 historical runs as two identical final-source repeats. All original 49 Blob assertions are retained, with credential issuance/denial, exact native attachment bytes, tampering, missing/foreign origin, wrong file, extra/oversized fields, URL/query/account-token misuse, stamp/role/activity/publication revocation, expired credentials through real authentication and both legacy-folder retrievals/anonymous denials. Real transferred payloads remain 8,388,645 bytes plus small legacy/recovery fixtures; they are not 2 GB load tests.

Current runtime inventory is **307 method/routes** (296 controller, 10 Identity, one health), digest `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`. Four new method/routes are the issuance action and three attachment POST reads. Migration histories remain 80 application / 7 Identity; no new migration was created in this slice.

The fresh final-source [original private-file gate recheck](../EVIDENCE/logs/phase-2b-native-download-legacy-recheck.log) also passed the original unauthorized-byte leak case and all 29 legacy HTTP checks, including recipient, guardian, role and active-entity revocation, range/HEAD/conditional requests and path aliases. Its exact owned database/login, container and guarded host fixture were removed. This is backend regression evidence, not a full portal/browser test.

## Actual browser probe

The computer-use workflow clicked the isolated probe's Download control in the desktop Codex in-app browser. The actual `nativeMaterialTicket`/`handoffNativeMaterial` helper was compiled into the probe page. A loopback-only **synthetic** API checked the real browser Origin, Content-Length, form encoding, exact path and ephemeral synthetic credential, then streamed 68 MiB without a JavaScript response Blob. It does **not** exercise normal account authentication or the real backend policy. The independent real-backend tests above exercise those policies; combining the two does not manufacture a full UI-to-backend test.

The browser returned a completed file path and the saved bytes independently matched:

- Run: `1dab70c6-b3e3-4cd1-a332-c46426466e29`
- Length: **71,303,168 bytes** (68 MiB, above the former 64 MiB cap)
- SHA-256: `1d7ad4f99bc065fbdc68b55d5cd620e7713e6d733b9de41e73bc496afe2aa8c6`

[Browser/server/hash evidence](../EVIDENCE/logs/phase-2b-native-download-browser.log) and [handoff screenshot](../EVIDENCE/screenshots/native-download-browser-probe.png) are preserved. The browser probe used the valid-ticket path before the later malformed/null-reply guard; the native form handoff implementation is unchanged. No private customer file, account token or browser profile was read. Native API/device Origin behavior, Unicode filenames, CSP/download prompts and loss-of-network handling still require broader browser coverage.

The probe tab was closed and its exact Node helper stopped only after matching the recorded loopback listener PID and script command. After full length/hash verification, the synthetic file was moved from Downloads to `D:\AcademyDesk\.build-check\native-download-1dab70c6-b3e3-4cd1-a332-c46426466e29\academydesk-qa-native-1dab70c6-b3e3-4cd1-a332-c46426466e29.bin`. It is recoverable there, not deleted. Do not commit `.build-check` artifacts.

## Cleanup, recovered infrastructure failure and next task

Successful backend runs removed their exact private Blob container, SQL database/runtime login, Docker containers and guarded host folders. The disk-failed run's guarded host folder is absent; its two exact containers were stopped/removed after matching name/image/run-label checks. No independent SQL-level cleanup is asserted for that failure. Its synthetic container contents are not recoverable. No broader Docker prune or personal/development/Azure data deletion was performed.

The immediate post-failure observation had **less than 1 GB free on C:** and further SQL provisioning was stopped. Space subsequently recovered to about 2.55 GB after owned cleanup, without broad deletion or machine-storage changes. An explicit 2 GiB free-space preflight then permitted the recovered successful final-source run. The disk failure remains evidence, not a current unresolved blocker. Maintain sufficient capacity and use a separately approved relocation plan if needed; do not silently alter machine storage settings.

**Next: stay on the agreed Sol High allocation** for isolated full Teacher/family portal and actual-device verification. Reserve Astra High for a specific unresolved security/release-policy decision, not repeating accepted audits. Shared Data Protection keys across API replicas, HTTPS/origin configuration and body-log redaction must be verified before Azure rollout. `MediaStorage.Enabled=false` remains unchanged in production.

Phase 2B and BUG-SEC-0001 stay open. Four finance/payroll P0 issues remain OPEN / RUNTIME-REPRODUCED; earlier failures/policy decisions are unchanged. Remaining upload pacing, recording/microphone/device formats, shared-device draft retention, orphan-session cleanup, scanning/quarantine and critical regression gates are not waived. This report supersedes only the earlier [client checkpoint's](PHASE_2B_MEDIA_CLIENT_INTEGRATION.md) buffered-download implementation status, not its historical observed checks or pending release gates.

[Final integrity observations](../EVIDENCE/logs/phase-2b-native-download-integrity.log) confirm all 45 source hashes, preserved student changes, unchanged HEAD/configuration, parseable current test cases and no remaining Docker containers. Accepted Phase 1 inventory and validator evidence were not regenerated.
