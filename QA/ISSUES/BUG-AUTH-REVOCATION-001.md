# BUG-AUTH-REVOCATION-001 — inactive credential issuance and incomplete native token revocation

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
