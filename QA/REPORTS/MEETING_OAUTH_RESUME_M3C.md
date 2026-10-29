# M3c — quarantined callback capture and authenticated resume

2026-10-15. Recommended route: Sol High / Codex. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work.

## Delivered scope

Internal shared capture/resume lifecycle for GoogleWorkspace, Microsoft365 and Zoom. This extends M3b rather than starting another integration plan. **No public route/DI activation, provider redirect, real login/reauthentication UI, token exchange, verified connected account or meeting execution is enabled.** Existing readiness and connection controls remain unavailable.

- Anonymous provider-return input can only quarantine a protected code/denial after exact callback/query/secure browser-correlation/registration/expiry checks. No principal is fabricated from state, cookie or model claims. The capture audit has no authenticated actor.
- New `MeetingOAuthCallback` stores only encrypted material; unique attempt and composite academy/provider/actor/attempt FK prevent duplicate or foreign linkage. Captured→Consumed version checks and the attempt's new CallbackCaptured/version1 state fence competing capture, direct claim and authenticated resume.
- Capture does not extend the original five-minute intent, approve provider access or change AuthorizationPending to Connected. A protected five-minute provider-specific `__Host-` resume cookie contains no plaintext code. It is tied to the original correlation and approved registration; cookie flags remain Secure/HttpOnly/Lax/Path=/, no Domain.
- Resume requires authenticated POST, approved Workspace Origin/TLS API host, both protected cookies and current server Identity/academy/role/module/security stamp. Original attempt integrity, expiry, registration and connection generation are rechecked. Wrong actor/provider/tenant, altered context, replay and direct-claim bypass fail closed.
- Successful resume atomically consumes callback, claims attempt, changes binding to VerificationRequired and records both required audits. It releases code/verifier only in internal JsonIgnored material. The protected callback is rewritten with AuthorizationCode=null; provider denial is consumed without returning code/verifier or raw provider descriptions. Cookie deletion follows committed success/denial only.
- New migration `20261015150306_MeetingOAuthCallbackCapture` preserves existing rows and adds CallbackCaptured/version2 claim semantics. Downgrade refuses populated callback records or attempts requiring the new state contract. No production migration applied.
- Meeting admission's unchanged-SQL snapshot now also includes bindings, attempts and callback rows; prior assertions are expanded, not reduced.

## Verification and qualification limits

- **322/322 focused tests PASS**, 0 failed/skipped: 28 new capture/resume tests plus 294 relevant retained tests. Receipt `QA/EVIDENCE/meeting-oauth-resume-20261015/resume-final.trx`.
- API and isolated harness build:0 warnings/errors. EF reports no pending model changes; existing PassingPercent/AdjustedAmount precision warnings remain outside this slice.
- Two original InMemory tamper fixtures initially failed because provider/actor are now alternate-key fields and EF refuses tracked key mutation. Tests now replace only their disposable adversarial fixture row while retaining its original protected envelope and original denial assertions. No production-data deletion or assertion weakening.
- **203/203 named SQL-run cases PASS**:90 actual HTTP/SQL cases plus113 direct SQL/internal-service cases,including40 new capture/resume cases. The new40 verify all3 encrypted quarantine/authenticated release/erasure/replay,exact202/409 capture race and200/409 resume race withone code release/fouraudits,post-SQL commit-fault rollback/recovery,currentactor/tenant/browser/registration/revocation andcompositeFK/unique/statusconstraints. Existing163cases rerun without weakening their assertions.
- Final run`e6678ab8d79f4bde9abf60a9afc7f159`,loopbackSQL55264,exit0,108.7seconds.94application/7Identitymigrations;newcallback upgrade/populateddowngrade refusal/version2downgrade refusal/emptydown-up PASS alongside earlier3 migrationguards.333routes/322controller method-routes/10Identity,routeSHA`3CA8D1FABA64A043749774D75B9EE01ABC90DA1428443AE2C2C7517BE1477728`.No429 orprovider calls.
- All8 final result receipts' sourcehashes match final application/harness sources:`QA/EVIDENCE/meeting-{readiness,preview,receipt,operation,oauth,oauth-browser,oauth-resume,admission}-sql-e6678ab8d79f4bde9abf60a9afc7f159/result.json`. APIbuild andharnesscopiedDLL SHA both`4BFB1F203F2B1064AB627EFDEE7E63C7A8651B86EFD4BB5217E363B51B352DB2`. Tracked diff whitespacecheck PASS.
- Runner proved exactruntime target/login,mismatchedmarker cleanup refusal andowned database/login removal;exactownedcontainer absent afterexit0. BothPENTAcontainers remain running,unrelatedcontainers untouched.
- Earlier run`4198b32e70c04d0b9c79e3d4c22c02fd` retained a40-case resume receipt but lost observable terminal completion across an interrupted tool session and had noadmissionreceipt. It is explicitly NOT counted as full-suite PASS. Its ownershiplabel was verified before removing only thatrun's disposablecontainer (andits disposableSQLvolume);localQA evidence retained. Newrun independently proves fullcompletion/cleanup.
- No frontend changes;prior frontendchecks retained at their original evidence level,notrerun. No physicaldevice/publicOAuth-browser/realprovider/fullenterprise acceptance claimed. No focused PASS closes full enterprise/security/device release gates.

## Remaining boundaries before activation

This is authenticated **resume**, not proof of fresh credential entry or a deployed browser login flow. Public adapter, real-browser same-site/cross-origin cookie/CORS/ingress/rate-limit and no-code query logging/history/referrer acceptance remain required. Cross-site Workspace/API deployments must not silently relax cookie security. A provider-specific resume cookie supports one currently selected pending return per provider; multi-tab selection/recovery needs explicit UI acceptance, not automatic adoption of the newest result.

Unresumed/expired encrypted callback retention and cleanup, audit retention, key-ring durability/rotation and crash-after-claim recovery remain activation gates. Claimed code is never blindly reissued/retried after uncertain external exchange. A consumed callback does not claim provider success, and this slice does not implement token exchange or error recovery for it.

Next: qualified provider-specific client/flow/scopes/issuer/endpoints profiles (version-bound registration), secure code exchange/account verification and token lifecycle; refresh/revoke/confirmed disconnect; scoped real execution approval/admission/dispatch/reconciliation/history; real Manual/PENTA integration. Public capture/resume and actual browser login are integrated only behind reviewed provider/runtime gates. All three providers stay in scope. No credentials/account setup or external sends authorized here.

Security design checked against [OAuth Security Best Current Practice, RFC9700](https://www.rfc-editor.org/rfc/rfc9700.html): reviewed fixed redirect boundaries, state/browser correlation and independent authorization. This report is not a claim of complete RFC conformance or provider qualification.

Owner full app→accepted/authorized go-live→Hayansh→product website order retained. Existing [8 remaining workstreams /3 unfinished groups](PRODUCTION_REMAINING_20261015.md) remain open; this is progress within provider integration, not closure of a delivery group. No main/stage/commit/push/Azure/Mini/production-data changes; prior WIP and QA evidence retained.
