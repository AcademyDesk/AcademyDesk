# BUG-AUTH-REFRESH-001 — Renewed bearer overwritten by stale request headers

22October2026 [PENTA channel/provenance successor](../REPORTS/PENTA_SESSION_OWNERSHIP_2026-10-22.md):
detached onboarding and read-chat adoption bind to explicit-login epoch,not token
text alone.729focused assertions/39native SQL-backed browser contexts PASS;
run`afce2af7fd7c4adabe164e61b5929095`,exit0/395.6s,17new GETs/11scoped read audits/
unchanged state/owned cleanup. Original guards retained. Broader auth issue OPEN;
login/logout write atomicity/device/security/release acceptance not claimed.

21October2026 [cross-tab successful renewal](../REPORTS/SESSION_REFRESH_LOCK_2026-10-21.md):
epoch/native-Web-Locks coordination and peer reuse,219focused/29native-lock probes/
104exported React/47native HTTP PASS,no429/exact Student/link/audit/owned cleanup.
Native run`cba591c5e6784d8aab88db30386f9c16` exit0/53.3s. Legacy/unsupported path
unchanged. Login/logout check-write atomicity/native-device/critical/release OPEN;
not every-race/server-revocation proof. Earlier cross-tab-success gap is historical
within this bounded successor. Issue remains OPEN at broader acceptance.

21October2026 [explicit-login epoch successor](../REPORTS/SESSION_LOGIN_EPOCH_2026-10-21.md):
opaque per-workspace login identity retained across automatic renewal. Completed
replacement before old refresh adoption rejects even identical-pair/pre-event
restoration; old views gate on marker/storage/resume.188focused/104exported-browser
cases/types/lint0/0/87routes PASS; native25HTTP/four overlaps/one Student/link/audit
PASS, run5009f7540dbb4e61b650976f8b972d0a/exit0/36.4s/exact cleanup. Atomic cross-tab
check/write synchronization, real suspension/device/security/critical/release breadth
remain OPEN; not server revocation or all-race proof. Earlier marker-gap notices below
are historical within this bounded successor, not a reason to redo accepted repairs.

21October2026 successor:[missed-event resume/local logout](../REPORTS/SESSION_RESUME_2026-10-21.md) repairs the UI presence-check gate at focus/page-show/visible-tab resume and prevents signed-out shell announcement reads.130relatedchecks/80exported-page cases PASS;events/APIs controlled,not actual frozen/BFCache/physical/SQL acceptance. Normal renewal/legacyAdmin/otherworkspace sessions preserved. Same-credential ABA before event delivery,account replacement,cross-tab locking and broader session/release remain OPEN.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation | Reproduced in actual TypeScript helper; bounded local repair verified |
| Final verification | PARTIAL PASS / OPEN —57 controlled helper checks,25 successor native Identity/HTTP requests and fresh SQL assertions; same-realm overlap verified, cross-tab/browser/device/critical pending |
| Priority | P1 |
| Affected source | apps/web/src/lib/api.ts; callers passing apiHeaders() |
| Scope | All four portal workspace keys; real expired-token checks cover Admin/Teacher only |

2026-10-02 successor: [delivered cross-tab logout recovery](../REPORTS/PHASE_2B_SESSION_LOGOUT.md). Retained-source8/18 PASS,10 failures;matching peer-removal generations and visible workspace boundary fix the scoped gaps.75/75 new+relevant helper regressions/types/targeted lint PASS;five actual browser storage-event/UI checks with controlled API responses,not native Identity/SQL/server revocation. Other workspace/public route/normal renewal isolation preserved;975 immutable predecessor pins,normal dev/Azure unchanged,owned QA web stopped. Pending identical-pair renewal rejection is controlled event-delivery evidence,not a browser ABA claim. Cross-tab single-flight/pre-event races/frozen tabs/account replacement/critical/release remain OPEN.

After401 and successful refresh, the previous request spread init.headers after the new Authorization header. Form requests using apiHeaders(true) therefore resent the old token. The original workspace was also read after the first await, allowing navigation to select another workspace's refresh credential; a pending refresh could restore a signed-out/replaced session.

Reproduction: actual helper with old access/valid refresh, copied Authorization and POST body. Controlled old-source run15/37 pass,22 fail; final37/37 pass. The repair uses standard Headers, pins the originating session, applies the renewed bearer last, and checks the token pair before and after refresh. Explicit unrelated bearer credentials are not refreshed using the local session; cancellation and malformed responses do not save/replay; no403/409/500 or transport-loss mutation retry. Existing legacy admin fallback, per-workspace keys, API contracts and server lifetimes8h/30days remain unchanged.

Native signed access/refresh tickets expire only in disposable QA fixtures, preserving original claims and production options. Actual api.ts executes through loopback into the real Identity/SQL host: Admin and Teacher session401→refresh200→session200, Admin create401→refresh200→create200, expired refresh401→401/no replay. Exactly one Student account/membership/typed academy-person link/success audit persisted. No browser/storage-profile inspection or session injection into a browser. Synthetic Map storage is only the client test adapter.

[Evidence and limits](../REPORTS/PHASE_2B_SESSION_REFRESH.md). Broader parallel refresh coordination, sign-out/sign-in with identical stored credentials, revocation/password-change/inactive accounts, cross-tab events, visual login routing and full critical suite remain separate gates. This issue, Phase2B and release remain OPEN.

2026-10-02 successor: [Same-realm concurrency repair](../REPORTS/PHASE_2B_REFRESH_CONCURRENCY.md). One shared refresh per workspace/session with successful late401 receipt, generation protection for public-helper signout/re-login, per-waiter cancellation and identity-safe flight cleanup.20 new controlled checks (old source5/15) plus37 retained controls final57/57 PASS.25 native requests/four groups each exactly one refresh, one Student mutation/SQL membership/link/audit, expired-refresh waiters401/no replay. Initial mock interruption and obsolete signal-identity assertion failure retained/excluded or corrected transparently. Types/build/owned cleanup PASS;833 predecessor entries retained; no browser/backend rerun/deployment. This supersedes only the same-realm concurrency/identical-login gates above; cross-tab ABA/events, broader stress, native cancellation, revocation/disabled identities and visible routing remain OPEN.

Browser follow-up: [real provisioning/login routing](../REPORTS/PHASE_2B_PROVISIONING_BROWSER_RETRY.md) verifies bounded new Student/Teacher/Admin landings and existing Owner/Student/Teacher session preservation across other normal logins. No expired-token browser test or cross-tab/revocation closure. IndependentSQL3 users/roles/typed links/one academy PASS;interrupted host final assertion not accepted;disposable fixture stopped/retained after policy blocked deletion. Product unchanged;issue/release remain OPEN.

2026-10-02 native revocation successor: [two-run diagnostic](../REPORTS/PHASE_2B_SESSION_REVOCATION_DIAGNOSTIC.md) reproduces four server-side gaps in30 cases per fresh SQL run; tracked separately as BUG-AUTH-REVOCATION-001, not a regression/reopening of the accepted helper repair. Inactive token issuance/refresh200; password-change stale access200; deleted identity protected list200. Existing inactive access403/password stamp-refresh401 controls work. No product fix yet; broader security/this issue/Phase2B/release OPEN. Next bounded server repair,Sol High.
