# Phase 2B — Delivered cross-tab logout recovery

2026-10-02. FIXED LOCALLY / BOUNDED RETEST PASS; SECURITY-SESSION-001, AUTH-001, BUG-AUTH-REFRESH-001, Phase 2B and release remain OPEN. Agreed allocation: Sol High implementation, not a repeat of Astra's accepted audit.

## Scope and confirmed gap

The accepted helper protects same-realm logout/re-login. Other tabs have independent generation maps and no visible logout listener. With delivered same-origin token-removal events, a peer page kept showing its old content; a pending peer renewal could restore/replay after logout followed by identical stored credentials. Final retained-source comparison:8/18 controlled checks pass,10 fail.

Only two product files changed:

- `apps/web/src/lib/api.ts`: shared pure pathname-to-workspace mapping and precise localStorage removal-event recognition. Delivered logout/clear events invalidate only matching workspace generations, including the legacy Admin keys; ordinary token updates and unrelated/sessionStorage events are ignored. This prevents pending renewal restoration/replay after those delivered events. Existing refresh receipts, per-waiter cancellation, request contracts and lifetimes are preserved.
- `apps/web/src/components/workspace-frame.tsx`: route-keyed client boundary subscribes to matching workspace removals, unmounts the old page/shell and displays an accessible Session ended/sign-in action. Login/register remain accessible; unrelated workspace logout does not gate the page. The sign-in anchor performs a full document navigation so obsolete page state is discarded. Boundary listeners clean up on unmount.

This is client logout propagation, **not server token revocation**. No API/security/Identity/schema/storage policy, CSS or other page changed. [Previous dashboard recovery](PHASE_2B_SESSION_RECOVERY.md) and native security fixes/evidence are preserved.979 predecessor pins verified;975 immutable pins retained, four declared pinned successors (API helper and three trackers). Workspace frame additionally has a pre-edit snapshot. Aggregate before manifest was assembled after testing from snapshots captured before edits; no fabricated before-timestamp claim.

## Executed checks

| Layer | Observed result |
| --- | --- |
| Final retained-source comparison |8/18 PASS,10 FAIL; shared synthetic storage, separate actual-helper JS realms and delivered removal events |
| Final helper/frame tests |75/75 PASS:18 new cases plus57 existing helper regressions rerun because the helper changed. Four workspace peer gates and same-credential pending renewal guards, unrelated/rotation/sessionStorage events, legacy Admin, clear, public route, listener teardown and other-workspace pending renewal |
| TypeScript / targeted lint | `tsc --noEmit --incremental false` exit0; two changed files lint exit0, no diagnostics |
| Actual browser/DOM |5/5 final checks, owned headless Edge154.0.4258.48, real browser StorageEvents. API responses/login profile issuance are explicitly controlled: **no native Identity/HTTP-SQL acceptance** |
| Cleanup / preservation | Verified owned QA web parent/child stopped;49431/49432 listeners0; recoverable source/cache retained. No new SQL/root/container/API host was created. Previous fixtures, normal dev services/database, HEAD and Azure unchanged |

Final browser sequence: actual login UI with controlled synthetic responses establishes Teacher and Admin workspace pairs; two Admin pages and a Teacher page share one browser context. Actual Admin Sign out clears its pair, emits real native storage-removal events, gates the peer page with no old operating indicators, and preserves the Teacher pair/visible Teacher heading. Peer Sign in again followed by normal UI login returns to a ready dashboard. Repeat peer logout at390×844: message/action visible, no horizontal overflow. Actual Teacher Sign out leaves the re-established Admin pair and ready dashboard untouched. No final page/hydration errors. Desktop/mobile screenshots inspected, not physical Android/iOS certification.

Controlled VM tests demonstrate the pending identical-pair guard after event delivery; the browser run demonstrates visible Admin logout propagation/isolation and sign-in recovery, **not a browser ABA-refresh or all-four-portals test**. Login APIs are intercepted with synthetic responses through normal form actions; no real credentials/customer data, saved user profile or user browser used.

## Retained failures and boundaries

Initial browser fixture incorrectly provided Teacher overview instead of the actual `/api/teacher/me` payload; first Teacher heading timed out. Original runner/log/incomplete evidence retained; corrected only the fixture path and final five-check run accepted. Initial lint invocation from repository root could not find the frontend ESLint config; retained output and reran from `apps/web`, without changing lint config. Test adapter was extended to render the new internal boundary and mount effects once; final old-source comparison remains8/18, not a claim that the initial script mounted a component that did not exist.

Storage event delivery is not atomic cross-tab coordination: simultaneous refresh, a response completed before event delivery, sleeping/frozen tabs/BFCache/focus recovery and account replacement without removal remain OPEN. No Web Locks/BroadcastChannel/new persistent epoch/leader election added. Cross-tab single-flight renewal, expiry/lockout/2FA/role revocation, stolen-token/server-side logout invalidation and staging headers remain separate gates. No full backend/native-suite rerun or phase/release closure.

## Evidence

- [Before manifest/snapshots](../EVIDENCE/session-logout-before.json), [source receipt](../EVIDENCE/session-logout-source-receipt.json), [cleanup](../EVIDENCE/session-logout-cleanup.json), [consistency validator](../tools/validate-session-logout.cjs).
- [Final controlled comparison](../EVIDENCE/logs/phase-2b-session-logout-baseline.log), [75 final checks](../EVIDENCE/logs/phase-2b-session-logout-handlers.log), [types](../EVIDENCE/logs/phase-2b-session-logout-types.log), [final lint](../EVIDENCE/logs/phase-2b-session-logout-lint-final.log), [initial wrong-directory lint](../EVIDENCE/logs/phase-2b-session-logout-lint.log).
- [Final browser log](../EVIDENCE/logs/phase-2b-session-logout-browser.log), [controlled transport/event observations](../EVIDENCE/session-logout-browser-observations.json), [initial browser failure](../EVIDENCE/logs/phase-2b-session-logout-browser-initial.log), [initial incomplete observations](../EVIDENCE/session-logout-browser-incomplete.json).
- [Desktop peer logout](../EVIDENCE/session-logout-desktop-peer.png), [mobile peer logout](../EVIDENCE/session-logout-mobile-peer.png).

No commit/push/deploy. Next bounded accepted gap: effective role-permission revocation with an existing session (SECURITY-ROLE-001 / SECURITY-SESSION-001). Continue agreed Sol High implementation/evidence allocation; unresolved policy/design decisions get a narrow Astra review rather than a repeated audit.
