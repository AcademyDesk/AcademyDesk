# Phase 2B — password-change recovery and Platform admin-disable browser checks

2026-10-02. **BOUNDED LOCAL BROWSER/SQL PASS; broader session/release OPEN.** The existing repair was already complete when this continuation resumed. It was preserved, not implemented again or re-audited. The unfinished QA-only BrowserSession host/dispatcher and source-only frontend fixture were reused. No application changes in this slice; normal development and Azure unchanged.

## Accepted observations

Nine bounded browser checks across the partial initial attempt and its resumed remainder, not nine successful checks from an uninterrupted run:

1. Real synthetic AcademyAdmin UI login200 routes to dashboard with an Admin token pair.
2. Owner clicks the actual Platform Admins **Deactivate** action: native PATCH200, visible success notice, Deactivated status and Activate action.
3. A real Owner page reload retains the Deactivated state.
4. Disabled Admin UI login401 stays on the login page and displays its error; no successful portal routing.
5. Password confirmation mismatch displays its error and retains input without a native password-change POST.
6. Incorrect current password produces native400, visible error, retained inputs and retained Platform session.
7. Successful password change produces native200, replaces the route with login, shows an accessible new-password notice, clears the Platform credential pair and leaves the login password field empty.
8. Real new-password UI sign-in200 follows returnTo back to Platform controls and creates a usable Platform credential pair.
9. No uncaught `pageerror` events in the resumed browser journey. This is not blanket console-warning, visual, accessibility or device acceptance.

Actual Edge154.0.4258.48 headless, desktop1440x1000. Normal rendered UI, hydration confirmed by Show/Hide state change, native requests/Identity/SQL. Independent Owner/Admin browser contexts; no auth storage injection, fake responses, forced expiry, disabled rate limiting or reused personal browser profile. Only fixture origins are allowed for browser requests; all credentials/data synthetic. The runner reads storage-presence booleans, not token values. Screenshots and text contain synthetic fixture identifiers only. The desktop login-success and disable-success screenshots were inspected; no mobile/dark/theme-wide/pixel-baseline approval inferred.

Native host controls after the actual Owner disable action confirm inactive SQL row and exactly one attributed PlatformAuditEntry, old Admin access403, login401, refresh401, unrelated Owner session200. After actual Owner password change, a fresh scoped read confirms stamp rotation; pre-change Owner access/refresh/old-password login401. These seven native requests use the host's independently captured credentials, not browser-injected sessions. Final fresh-context original user/role counts preserved. Browser new-password login supplies the positive recovery proof. SQL checks are scoped row/audit/count assertions, not whole-database equivalence or audit-total guarantees.

## Preserved failures and limits

- The prepared QA host's first build had CS0136 (shadowed local `id`); its corrected final build passed0 warnings/errors in23.23s. Both logs retained; the failed build is not accepted. The corrected host was already running before this continuation.
- The predecessor repair validator reports a Program.cs hash mismatch because the declared successor added the QA-only BrowserSession dispatcher/runner. The ancestor validator is intentionally not rewritten; the successor validates those changes against recorded before-snapshots and preserves product/accepted predecessor hashes. This is not a product-regression assertion or excuse to rerun accepted Astra work.
- Initial headless attempt accepted the first three observations, then Admin dashboard reload hit real429 responses and timed out waiting for403. Full initial journey is NOT accepted. Logs/incomplete observations/initial runner retained. Existing-admin browser reload/recovery remains unaccepted; native old-access403 is a different accepted control.
- After the normal rate window elapsed, an explicit resume used fresh disposable browser contexts and continued disabled-login/password checks without repeating the disable action or resetting data. The accepted prior observations are marked as retained; remaining six checks passed. Resumed network observations contain no429. Rate limiter unchanged; no production load/performance conclusion from this local dev burst.
- Owner disable success/failure notices are visible paragraph text, not a new aria-live guarantee. Unattempted disable-error/reenable/role-change/2FA/lockout/cross-tab/logout/phone journeys and staging-header/security breadth remain OPEN. A reactivation policy change was neither made nor accepted.
- Backend1032/handler42/strict-native36/TypeScript from the prior repair retained, not rerun/recounted here. This host executes its own tenant preflight and the selected action-native controls. No new unique bulk-suite total or phase-completion percentage asserted.

## Isolation and cleanup

Run4c94a112b34845099d770bd55b8500b9, owned loopback SQL51893/API49262/web49261. Source-only copy `.build-check/browser-web-458cfacbfafd4a8782ea70b1dc4fb464`, no .env copied. Owner/control/login/api-helper source hashes match current product and its accepted repair receipt. Guarded root/SQL marker verified before exact stop-browser marker. Host final: disableVerified=True, passwordNativeVerified=True; wrapper exit0 in689.7s including the earlier fixture/browser preparation time, not an isolated performance benchmark.313 routes and82 application/7 Identity migrations unchanged.

Negative wrong-marker cleanup refused; owned SQL database/login/container/root removed. Exact QA Next parent4376/child7116 were verified by listener/commandline/parent relationship and stopped; generated source/cache copy retained. Independent check: root/container absent, no listeners51893/49262/49261. Only disposable synthetic QA data removed; evidence/source/binaries retained. Older interrupted provisioning fixture remains stopped/retained with no deletion retry. HEAD20bb6047f9edf733ac8e2a226621cc582ec54b3c unchanged. No commit/push/deploy. Immutable pin totals are calculated in the successor source receipt/validator; no product/schema/factory/guard edits.

## Next bounded task

Close the still-unaccepted existing-admin browser reload/recovery case and verify clear handling of401/403/429 without relaxing normal authorization or rate limiting. Then proceed to the remaining explicit session/role/lockout gates. Continue with agreed Sol High; no repeated static audit or model comparison. BUG-AUTH-REVOCATION-001 remains locally fixed with broader session/release OPEN.

[Initial browser log](../EVIDENCE/logs/phase-2b-session-browser-acceptance.log), [resumed log](../EVIDENCE/logs/phase-2b-session-browser-acceptance-resume.log), [accepted observations](../EVIDENCE/session-browser-observations.json), [initial incomplete observations](../EVIDENCE/session-browser-incomplete-observations.json), [native/SQL](../EVIDENCE/logs/phase-2b-session-browser-sql.log), [first build failure](../EVIDENCE/logs/phase-2b-session-browser-build.log), [final build](../EVIDENCE/logs/phase-2b-session-browser-build-final.log), [ancestor validator difference](../EVIDENCE/logs/phase-2b-session-browser-predecessor-validator.log), [cleanup](../EVIDENCE/session-browser-cleanup.json), [before](../EVIDENCE/session-browser-before.json), [source receipt](../EVIDENCE/session-browser-source-receipt.json), [UI runner](../tools/session-browser-acceptance.cjs), [initial runner](../EVIDENCE/session-browser-acceptance-initial.cjs), [validator](../tools/validate-session-browser.cjs), [password notice screenshot](../EVIDENCE/session-browser-password-success-login.png), [disable screenshot](../EVIDENCE/session-browser-disable-success.png), [accepted repair](PHASE_2B_SESSION_REVOCATION_REPAIR.md).
