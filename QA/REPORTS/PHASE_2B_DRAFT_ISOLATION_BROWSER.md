# Phase 2B — same-browser Teacher account/draft isolation

2026-10-01. **PASS for bounded Teacher A → sign out → same-tenant Teacher B → reload → sign out → Teacher A recovery.** This is application-level draft isolation, NOT an all-workspace sign-out, secure disk erasure or shared-OS-user confidentiality certification. Phase 2B/issues remain open. Continue agreed Sol High allocation; accepted Astra work not repeated. No product/API-test edits, commit, push, Azure access/deployment or development/Azure database writes.

## Environment and fixture

Fresh run `b9e07dcfe02c4df09d39565aca3ab5fb`, UTC start `2026-10-01T04:12:56.0943250Z`; run-owned SQL port 52232, private Azurite, real Program/Identity/SQL behind loopback bridge 49002. Actual source-only Next frontend 49001, copy `.build-check/browser-web-a3fc74609c36431abce3cac34dfecb3c`. Normal development app/database not replaced, no .env copied. Tenant startup controls PASS, inventory 307 routes / 296 controller method/routes / 10 framework Identity entries, migration histories 80 application / 7 Identity.

QA fixture adds Teacher B in the same academy with a separate assigned batch/session and no enrolled students. It does not change Teacher A assignment or authorize B to A's class. UI proves switching accounts/scopes; explicit storage double also proves same-batch different-owner isolation, not an additional real same-batch assignment test.

Existing inert file `synthetic-resume.bin`, 17,825,792 bytes / 17 MiB, SHA-256 `9f4be0e976dfd2921de0b28a22ec87d887100b6d4af3301339ef7a51629ee393`, title “Shared browser A draft”, comment “Only Teacher A should see this saved metadata”. No playable-media/device-recording claim.

## Actual sequence

| Check | Observed result |
| --- | --- |
| Teacher A initial transfer | Create 201; GET 200; chunk 0 PUT 200; same upload ID `300f53a2-021f-4a36-93ef-4f3e2d8b2c6f` |
| Pause during paced chunk 1 | Visible 47%, draft/file/title/comment retained, Resume available |
| A normal sign-out | Sign in page; direct /teacher navigation subsequently returned real auth/teacher/portal 401 and no class/draft data |
| Signed-out /teacher UX | Remained in empty Teacher shell with “This account is not linked to an active teacher profile.” No redirect. Misleading authentication notice recorded below; not a data exposure |
| B real login / own classroom | Synthetic Teacher B / Synthetic B Batch, empty title/comment, no saved draft/filename, Confirm disabled; A's metadata absent |
| B actual reload / reopen class | Same empty form, no A draft |
| B sign-out / A real login / original class | A's original title/comment/filename restored; Resume disabled until original file reselected; browser File itself not persisted |
| A reselect / Resume | Create 200 reuses same session, GET 200, chunks 1 and 2 PUT 200, completion 200. Chunk 0 not resent |
| Success | “Class material uploaded successfully.” Empty unlocked fields/file/draft, one refreshed history item with original metadata |
| Read-only fresh SQL | Exactly one completed session owned by A and original assigned class, one matching learning resource, one Student notification, zero B upload sessions |
| Native download | Exact 17,825,792 bytes/SHA-256; retained without overwrite as `.build-check/browser-media-fixtures/account-switch-downloaded.bin` |
| Final exit | Normal sign-out verified by actual Sign in DOM; agent-owned tab closed |

Paced initial chunk 1 eventually returned HTTP **499** after sign-out/unmount cancellation; this is observed transport outcome, not a claim all in-flight writes are instantly revoked by local sign-out. Successful resumed transfer verifies same ID and original bytes.

Browser interactions used actual fields/file chooser, no account-token/session-store/hidden React inspection or UI auth injection. Semantic DOM and HTTP are functional evidence. [B identity/class screenshot](../EVIDENCE/screenshots/phase-2b-draft-isolation-teacher-b.png) shows the top of B's class, not the offscreen attachment fields; empty-field proof is DOM. [Owner success screenshot](../EVIDENCE/screenshots/phase-2b-draft-isolation-owner-success.png) includes success and history; full-page capture has left-edge clipping, not visual acceptance. No actual Android/iOS or full visual/viewport certification.

## Important privacy boundaries

Draft metadata remains in browser localStorage under API host + owner + batch + optional recipient/session. Teacher sign-out removes only Teacher credentials; other role workspaces deliberately remain signed in. Original owner can recover metadata after signing in again. This is not encryption or protection against someone inspecting the shared OS/browser profile, and no stolen-token invalidation or global server-side session revocation is established. Any “clear all sessions/drafts on shared device” policy would require a separate agreed change, not silently discard legitimate recovery work.

No material was published by B. UI absence alone does not replace backend authorization tests; historical API/legacy security gates remain historical, not rerun.

## Tests, changes and evidence

Targeted client/protocol/SSR **28/28 PASS**, QA build **0 warnings / 0 errors**. Two new explicit-double tests cover different owners/API origins and actual role-scoped token-clear behavior with retained owner-only draft. No full API/legacy/critical suite, frontend typecheck or lint rerun. No new opt-in fault mode/guard tests this slice.

QA-only changes: BrowserMediaHost adds synthetic B; client test adds two cases; new read-only `Verify-BrowserDraftIsolation.ps1` validates exact labelled loopback SQL container/image/run/database marker before SELECT/assertions. Existing disposable password stays inside container environment, not arguments/stdout/report. SQL script does not write or access Azure. Snapshot [54 records](PHASE_2B_DRAFT_ISOLATION_BROWSER_SOURCE_SNAPSHOT.json) compares previous 53-record revocation snapshot: only BrowserMediaHost/client-test changed, SQL verification script added. All captured product/API-test/config hashes unchanged; HEAD `20bb6047f9edf733ac8e2a226621cc582ec54b3c`.

[HTTP/cleanup](../EVIDENCE/logs/phase-2b-draft-isolation-http.log), [client/build](../EVIDENCE/logs/phase-2b-draft-isolation-client.log), [SQL assertions/download/preflight](../EVIDENCE/logs/phase-2b-draft-isolation-sql.log), [selected actual DOM excerpts](../EVIDENCE/logs/phase-2b-draft-isolation-dom.log), [frontend lifecycle](../EVIDENCE/logs/phase-2b-draft-isolation-frontend.log), [source/registry/link validation](../EVIDENCE/logs/phase-2b-draft-isolation-validation.log).

Initial cold login navigation timed out while compiling, then same tab recovered; uppercase AX name initially did not match case-sensitive DOM locator, corrected from fresh DOM. First B login attempt immediately after navigation was lost during hydration/no auth POST; a fresh settled-page retry succeeded. These are retained test-sequencing/automation limitations, not silent substitutes or evidence of a new login API failure.

## Cleanup and next task

Owned stop marker disposed host, exit 0 / 356.3 seconds; exact private container, disposable database/runtime login, root folders and labelled SQL/Azurite containers removed. Current root/containers/listeners 49001/49002 absent. Matched QA Next child 11192 / parent 1580 intentionally stopped; launcher exit 1 is intentional termination, not product error. Disposable synthetic data not recoverable; source copy/fixture/download/evidence retained on D:. Customer data untouched. Older interrupted `d6dfd31d64c146789abbf686bccb920a` stopped containers remain; refused cleanup not retried/bypassed.

New bounded UX observation **DRAFT-ISOLATION-UX-001**: with cleared Teacher credentials, opening /teacher returns 401 but tells the visitor their profile is inactive/unlinked instead of asking them to sign in. Source `apps/web/src/app/teacher/page.tsx` load/error handling. Runtime observed once in fresh synthetic run; no product fix this slice. Next bounded task is correcting this signed-out Teacher state and testing signed-out versus linked-profile denial separately, agreed **Sol High**.

BUG-SEC-0001 remains OPEN / FIX-IN-PROGRESS / PARTIAL PASS; BUG-FUNC-0007 OPEN / STATIC-FINDING (original legacy failure not reproduced). Recording/actual devices/2 GB/wider account-role-expiry-concurrency and full critical/release gates still pending. Four financial P0 issues/policy decisions remain open; no phase closure.
