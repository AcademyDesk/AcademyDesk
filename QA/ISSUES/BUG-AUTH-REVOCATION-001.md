# BUG-AUTH-REVOCATION-001 — inactive credential issuance and incomplete native token revocation

22October2026 [PENTA ownership](../REPORTS/PENTA_SESSION_OWNERSHIP_2026-10-22.md):
729focused assertions/39native SQL-backed browser contexts PASS,including6new
replacement/late-response cases with17GETs/11scoped read audits/unchanged state.
Run`afce2af7fd7c4adabe164e61b5929095` exit0/395.6s/exact owned cleanup. Client
ownership repair does not revoke native tokens or close broader auth/release gates.

21October2026 [cross-tab refresh successor](../REPORTS/SESSION_REFRESH_LOCK_2026-10-21.md):
219focused/29native-lock probe/104exported React/47native HTTP PASS;actual Identity/
SQL/native Edge locks overlap/late401/expired-refresh/replacement evidence,not server
revocation or all-session closure. Native run`cba591c5e6784d8aab88db30386f9c16`,exit0/
53.3s/owned cleanup. Login/logout atomicity/device/critical/release remain OPEN.

21October2026 [explicit-login UI epoch](../REPORTS/SESSION_LOGIN_EPOCH_2026-10-21.md):
client account replacement now unmounts stale views; normal renewal retains marker.
188focused/104browser/native25HTTP overlap and one Student/link/audit postconditions
PASS. No server stamp/role/revocation/lifetime policy change or universal revocation
claim; synchronization/device/native breadth/critical/release acceptance remains OPEN.

21October2026 successor:[native-shaped login challenge UI](../REPORTS/LOGIN_CHALLENGE_2026-10-21.md) adds authenticator/recovery fields,exactRequiresTwoFactor handling,duplicate/token guard and accessible errors.22newchecks/172combined/28synthetic exported-browser cases PASS. Final additive nativeMFA/lockout **61newassertions/34HTTP+36retainedrevocation+tenant/privatefile controls PASS**,run1b8e69db5e7a4795928b710f0a83b65d/exit0/116.7s/102+7migrations/exactownedcleanup. Threeincompletefixtures stopped/retained;normal61-secondwindowpacing,notlimiterrelaxation. No authpolicychange/broadclosure;lockout-only existingaccess200 observed,not approveduniversalrevocation. [Resume recovery](../REPORTS/SESSION_RESUME_2026-10-21.md) repairs missed-eventUI scope only.

| Field | Value |
| --- | --- |
| Status | FIXED LOCALLY / BOUNDED RETEST PASS — broader session acceptance OPEN |
| Confirmation | Original failures reproduced twice; four gaps now rejected in a fresh strict 36-case native SQL run |
| Priority | P1 — broad session security scenario remains a P0 release gate |
| Scenario | SECURITY-SESSION-001; related AUTH-001 |
| Source | apps/api/Program.cs; Identity login/refresh integration; AuthSessionController / AcademiesController |
| Evidence | Historical two diagnostic runs retained; successor 36/36 native cases, 1032/1032 backend, 42/42 handler checks, types/build PASS |

The following numbered observations are the historical reproduction. Current repair and scope: [targeted revocation repair](../REPORTS/PHASE_2B_SESSION_REVOCATION_REPAIR.md). Active-aware SignInManager checks reject disabled login/refresh; authenticated middleware rejects missing accounts and stale stamps. Password-change UI now requests a fresh sign-in after success; its actual handler is tested, browser recovery remains pending. Reactivation semantics are preserved, not accepted as permanent revocation.

2026-10-02 successor: [real browser follow-up](../REPORTS/PHASE_2B_SESSION_BROWSER_ACCEPTANCE.md) accepts visible native-backed password failure/success/session-clear/new-password return and actual Platform disable notice/reloaded state with native/SQL enforcement. Nine scoped observations across partial+resume,not an uninterrupted nine-case run. Existing Admin browser reload hit429/timeout and remains unaccepted;old-access403 native control is separately proven. Product/accepted repair unchanged,fixture cleaned,prior suites retained;broader session/release OPEN. This supersedes only password browser recovery/platform-disable-action pending statements within the documented scope.

2026-10-02 dashboard successor: [session recovery](../REPORTS/PHASE_2B_SESSION_RECOVERY.md) now accepts native existing-admin reload403/access guidance after actual deactivation and native reset-revoked reload401/refresh401/sign-in guidance with Admin-only token clear. Only dashboard UI changed; accepted server repair/helper preserved. Final browser6 scoped checks,13/13 controlled TSX/type pass,four native disable controls/attributed SQL audit/count preservation,owned cleanup.429 desktop/mobile tests are explicitly controlled,not native limiter acceptance. Earlier failed Admin reload and two new QA navigation failures retained;broader cross-tab/logout/roles/2FA/lockout/portal/phones/release OPEN.

1. Persist IsActive=false on a synthetic Admin; anonymous native login with correct credentials returns200 and tokens; anonymous refresh also returns200. Their issued access requests are blocked403 by the existing middleware, so this is token issuance/renewal failure, not demonstrated disabled-user data access.
2. Native Teacher change-password returns200 and fresh SQL confirms stamp rotation. Old access still returns session200; old refresh and old-password login correctly401. Password-change UI recovery policy still requires explicit treatment.
3. Delete a synthetic Admin via scoped UserManager. Old session/refresh401, but GET /api/academies with old token200. The controller's missing-user fallback is empty; no data exposure is asserted. Authentication did not globally revoke the missing identity.

Application inactive middleware does not reject missing users/stale security stamps and does not cover anonymous framework token issuance. Existing framework refresh stamp checks work; access tickets are not automatically database-revalidated. Detailed primary-source explanation, controls, scope and cleanup: [diagnostic report](../REPORTS/PHASE_2B_SESSION_REVOCATION_DIAGNOSTIC.md).

Repair narrowly: active-aware Identity login/refresh and fail-closed missing/stale authenticated identity checks, keeping framework expiry/stamp/2FA/cookie behavior and existing tenant/module restrictions. Retest original cases plus active/expiry/tenant/role/private-file controls. Do not mark broad session/browser/cross-tab/role-revocation/logout/release gates closed from these30 cases. No production changes or deployment in reproduction slice.
