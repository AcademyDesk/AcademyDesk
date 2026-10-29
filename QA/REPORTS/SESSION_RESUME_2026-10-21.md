# Session resume recovery —21 October2026

Local continuation on `D:\AcademyDesk-codex-p0`, `codex/penta-search`,
HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`, unchanged. Sol High.
Extends the accepted delivered-event logout repair; does not redo its audit.

## Delivered

- `WorkspaceFrame` checks credential presence on focus, page-show and visible-tab
  lifecycle events after observing a session. A missed logout event no longer
  leaves that resumed view showing old content. Same-document sign-out also gates
  the frame. Listeners clean up on unmount; public login remains usable.
- Normal renewal, legacy Admin access keys and independent workspace sessions are
  retained. No credential decoding, persistent epoch, token restoration, server
  logout/revocation change, new authority or automatic action/retry was added.
- The Admin shell's existing announcement focus/poll callback now skips reads
  without an access token. Signed-in focus refresh remains unchanged.
- Recovery notice truthfully covers local or peer sign-out and offers a full
  document sign-in link. Resumption does not automatically resend work.

## Executed validation

- Before product changes: **14/33 PASS,19 FAIL** in the new actual-component
  controlled lifecycle regression. Saved `.build-check/session-resume/baseline.log`
  and recoverable `frame-before.tsx`.
- Final related tests: **130/130 PASS**, zero failures/skips/cancellations:33 new
  resume cases,22 retained logout,37 refresh,20 concurrency,13 dashboard recovery
  and5 password recovery. `.build-check/session-resume/final-tests.log`.
- TypeScript and webpack production export PASS,87/87 static outputs. Targeted
  lint:zero errors;one existing shell `<img>` performance warning, not suppressed.
- **80/80 actual exported React-page browser cases PASS**, four workspaces ×
  320/1440px × light/dark × focus/page-show/visibility/local-logout/normal-renewal.
  All other credential pairs preserved; signed-out resumes make no new API reads;
  normal signed-in Admin focus may make its retained announcement read. No page
  errors or recovery-screen horizontal overflow. Phone-width dark Teacher and
  light Platform recovery screenshots inspected; not whole-portal visual approval.
- Browser APIs and credentials are synthetic and lifecycle events deliberately
  dispatched. This proves actual hydrated UI reaction, **not physical suspension,
  real BFCache, native Identity/SQL or device acceptance**.

Final browser receipt:
`QA/EVIDENCE/session-resume-1792558612776/result.json`.
Two earlier incomplete browser receipts are retained. The first counted unfinished
initial page requests; readiness now waits for initial reads to settle. The second
exposed the retained shell focus read after sign-out, fixed in the callback above.
Assertions still reject signed-out retries/reads; legitimate signed-in announcement
refresh is explicitly distinguished. No failed attempt is reported as acceptance.

## Still open

Same-credential logout/re-login before any delivered event, account replacement,
cross-tab refresh locking, real BFCache/frozen-device behavior and role/session/
enterprise/release breadth remain OPEN. Presence is a UI guard, not server
authorization or proof of current roles. The server must still authorize every
operation. No issue closed, API policy/schema/model/Mini/default change, SQL
customer access, main edit, stage/commit/push or Azure operation occurred here.

Next: native disposable lockout/two-factor acceptance within the existing session
gate (High); settled UI/docs stay Medium. Model qualification remains separately
blocked, not repaired by session checks.
