# Phase 2B — Admin dashboard session recovery

2026-10-02. FIXED LOCALLY / BOUNDED RETEST PASS; AUTH-001, SECURITY-SESSION-001, Phase 2B and release acceptance remain OPEN. This follows the [accepted revocation repair](PHASE_2B_SESSION_REVOCATION_REPAIR.md) and [browser follow-up](PHASE_2B_SESSION_BROWSER_ACCEPTANCE.md), without repeating their audit or large suites. Agreed implementation allocation: Sol High.

## Product change

Only `apps/web/src/app/dashboard/page.tsx` changed in this slice. Previously primary 403/429 failures showed the generic localhost-port message and default zero totals. The dashboard now gates loading/error/empty states, distinguishes expired/revoked session401, access denial403, temporary rate-limit429 and network/server failure, and supplies an explicit recovery action. Only terminal401 clears AcademyAdmin tokens;403 does not pretend every denial means deactivation, and neither403 nor429 deletes valid credentials. Retry is a user-triggered read, not a mutation or automatic loop. Effect teardown ignores late initial responses. Optional module403 continues to use the existing permitted-dashboard behavior.

Backend, Identity policy, refresh coordination, lifetimes, schema, shared controls and CSS are unchanged. Accepted predecessor files/binaries/evidence are hash-pinned; source-only QA web copy matched the changed dashboard. The original dashboard and trackers have recoverable snapshots.

## Executed evidence

| Layer | Actual result / scope |
| --- | --- |
| Actual TSX effect/render with controlled responses | Retained old source:2/13 PASS,11 failures; changed source:13/13 PASS. Includes primary401/403/429/500, essential dashboard/session denial, network retry, active/optional403 controls and unmount guard. This is controlled handler evidence, not native transport |
| TypeScript | `tsc --noEmit --incremental false` exit0; empty diagnostics log |
| Final real-browser run |6/6 scoped checks in owned headless Edge154.0.4258.48. Four native-backed checks and two clearly controlled429 observations; no page errors or unexpected native429 in the final run |
| Owned native HTTP/SQL host | Existing accepted host/binary, real Identity and production pipeline. Four disable controls:old AdminA access403,login401,refresh401,unrelated Owner200. Fresh SQL verifies inactive AdminA and exactly one attributed deactivation audit; final user/role counts unchanged |
| Resource cleanup | Wrapper exit0,723 seconds including setup/browser pacing. Both migration histories82/7 verified; ownership-negative cleanup refused; exact owned database/login/root/container removed and SQL/API/web listeners0. QA web parent/child stopped after identity checks; recoverable generated source/cache retained |

Final browser sequence:

1. Real TeacherA login then AdminB login; active Admin dashboard is visible and both workspace token pairs exist.
2. Real Platform Owner resets AdminB password through the actual UI/API200. Existing AdminB reload returns native401; refresh401. Dashboard shows sign-in guidance, hides misleading operating totals and clears only the AcademyAdmin pair; Teacher pair remains present. This is token-pair preservation, not a subsequent Teacher data-read acceptance.
3. After normal quota pacing (60 seconds, limiter unchanged), real AdminA login then Owner deactivation PATCH200. Existing AdminA reload returns403 and shows contact/access guidance plus another-account link, not empty totals; its pair remains retained for a permission-denied state. SQL/native controls above independently verify enforcement.
4. AdminB uses the newly reset password through normal login200. Controlled `/api/academies`429 shows retry guidance, hides misleading totals and preserves token pairs.
5. Remove the controlled response and click the actual Try again button: native dashboard reads succeed and the dashboard returns, without re-login or account mutation.
6.390×844 viewport: controlled429 message and retry button visible. Desktop403 and mobile429 screenshots inspected; no phone hardware, mobile keyboard, Android/iOS or all-portal visual acceptance claimed.

The final run did not change Owner's own password, so the host truthfully records `passwordNativeVerified=False`; previous accepted own-password evidence is retained, not re-executed. AdminB reset is supported by actual reset200/stale-access401/refresh401/new-password-login200, not a separate AdminB SQL-stamp assertion.

## Retained failures / limits

Two earlier browser attempts stopped before any reset/deactivation: first a login-to-next-navigation race (`ERR_ABORTED`), then an exact accessible-link name expectation that omitted its icon. Corrected only the QA runner:wait for rendered Overview and use the actual navigation link href. Their logs/source snapshots and the second incomplete observation remain retained. Only the final six-check run is accepted. A Platform health500 appeared in that earlier incomplete attempt; no Platform health/full-portal acceptance is claimed by this dashboard slice.

Initial controlled baseline produced13 failures because two valid old-source controls incorrectly required a newly added state slot and the old effect lacked a cleanup function. Retained initial script/log; corrected comparison to observable ready indicators and guarded optional cleanup, then reran both sources with the same final suite. Only the final2-pass/11-fail baseline is treated as the bounded behavioral comparison.

The earlier accepted browser report's Admin reload429/timeout remains historical failed evidence; this new native existing-session403 observation supersedes its pending acceptance only for the documented dashboard journey. Controlled429 is not native rate-limit threshold, performance or abuse acceptance. Cross-tab renewal/logout, 2FA/lockout, role revocation, other portal error recovery, physical devices and staging headers remain OPEN. Prior1032 backend/42 handler/36 native repair suites are preserved, not rerun. No general issue census or phase closure is asserted.

## Artifacts

- [Final browser log](../EVIDENCE/logs/phase-2b-session-recovery-browser.log), [observations](../EVIDENCE/session-recovery-observations.json), [native SQL log](../EVIDENCE/logs/phase-2b-session-recovery-sql.log).
- [Final controlled tests](../EVIDENCE/logs/phase-2b-session-recovery-handlers.log), [final retained-source baseline](../EVIDENCE/logs/phase-2b-session-recovery-baseline.log), [types](../EVIDENCE/logs/phase-2b-session-recovery-types.log).
- [Initial navigation failure](../EVIDENCE/logs/phase-2b-session-recovery-browser-initial.log), [second attempt](../EVIDENCE/logs/phase-2b-session-recovery-browser-navigation-attempt.log), [incomplete observations](../EVIDENCE/session-recovery-navigation-incomplete.json), [initial controlled-baseline log](../EVIDENCE/logs/phase-2b-session-recovery-baseline-initial.log).
- [Desktop403](../EVIDENCE/session-recovery-disabled-403.png), [revoked401](../EVIDENCE/session-recovery-revoked-401.png), [desktop controlled429](../EVIDENCE/session-recovery-controlled-429.png), [mobile controlled429](../EVIDENCE/session-recovery-controlled-429-mobile.png).
- [Before manifest](../EVIDENCE/session-recovery-before.json), [source receipt](../EVIDENCE/session-recovery-source-receipt.json), [cleanup](../EVIDENCE/session-recovery-cleanup.json), [consistency validator](../tools/validate-session-recovery.cjs).

Normal development database/services, previous retained QA fixtures, HEAD `20bb6047f9edf733ac8e2a226621cc582ec54b3c` and Azure unchanged. No commit, push or deploy. Only synthetic account data was removed by owned cleanup; test evidence and source copies remain recoverable.

Next bounded task: existing-session logout/cross-tab recovery gaps in SECURITY-SESSION-001, reusing accepted audit and session fixtures. Continue agreed Sol High implementation allocation; security-policy ambiguity remains for Astra review, not a reason to redo accepted work.
