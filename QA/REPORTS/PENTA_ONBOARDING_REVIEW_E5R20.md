# E5-R20 — mounted shared PENTA onboarding review

18 October2026. Existing enterprise/PENTA continuation, not a backend restart.
Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Primary main and PENTA repository
are untouched. Existing unrelated dirty work preserved; zero staged files.

## Delivered

- [x] Mounted `/penta/onboarding` with the actual version-pinned PENTA operational
  UI and its sibling shared foundation. Twelve immutable source files checked
  against the existing PENTA release manifest; no gallery/demo/bootstrap/model
  registry/dependency installation. Academy vendor UPSTREAM.json pins original and
  LF-normalized hashes. Existing Manual foundation remains unchanged.
- [x] Authenticated passive review bootstrap: GET account → current conversation →
  owned draft → current again. Tenant comes from the current server account, not
  URL/model/JWT interpretation. URL conversation/draft IDs are untrusted lookup
  keys; backend independently checks current actor/tenant/ownership/authority.
- [x] Account role/workspace, exact conversation/draft/revision, current context,
  catalogue/tool/status, canonical payload digest and fields are checked before
  mounting. Snapshot actor is returned by the authenticated Owned/current API;
  the public session summary has no actor ID. Both current reads must agree in
  scope/catalogue/draft/evidence/availability. Medical content is withheld, not
  silently removed. Pending/expired/denied/changed/unavailable state shows no draft.
- [x] New loading/error/pending/recheck/Manual/read-chat controls, in-memory only.
  Logout and token replacement clear details; focus checks session replacement.
  Refresh hides the old projection before GETs. No retry/polling or POST on mount.
  Unknown or pending operations are not resent or labelled cancelled/successful.
- [x] Prism light / Orbit dark / Aura choice use the existing ThemeProvider.
  Height-bounded shell, independent scrollable drawer and pinned visible Close
  button after long-field scrolling. Host-only CSS hides inactive Clear/Submit
  controls in passive review without modifying vendor source.
- [x] Existing read chat has an honest review-entry link. Missing conversation IDs
  explain that no draft is created by visiting. Existing read chat/Manual remain
  independent. WorkspaceFrame excludes only this self-authenticating review shell;
  shell exclusion is not authorization. The complete page ledger now lists84
  declarations exactly once; no full-page/physical-device checkbox was closed.

## Explicitly NOT delivered

This is **passive review**, not an operational chat release. Every capability and
send/edit/prepare/confirm/pause/resume/cancel/history/feedback/media control is
disabled. No qualified model options, operation, approval or receipt is fabricated.
The existing E5-R19 POST/status channel remains **unmounted**; its bounded JSON
reader was exported/reused without changing transport behavior. Shared UI New chat
navigates to the existing read chat, not a new persisted operational session.

The host onboarding endpoints are still Testing-only and flags OFF in the developer
runtime. This route does not promote those contracts, apply missing migrations,
create/select a draft, issue a challenge, call inference or execute a domain write.
Opening the unbound local route shows guidance, not synthetic customer data.

## Verification / retained failures

- `node QA/tools/penta-onboarding-review.test.cjs`: **48/48 PASS**. Authenticated
  boundary/payload/freshness failures, medical denial, all12 vendor source pins,
  no grants or writes. Synthetic responses, not real SQL/identity acceptance.
- E5-R19 channel **72/72 PASS** after exporting its same bounded reader. Existing
  read contract **57/57**, workspace-entry/route ledger **20/20** and original
  Manual foundation six-file hash check PASS. No assertions weakened.
- TypeScript and focused ESLint PASS. Production `next build --webpack` succeeds
  and exports87 outputs including the new route. Default `npm run build`
  Turbopack FAILED with the worktree's external node_modules symlink outside its
  filesystem root; no dependencies/configuration changed to conceal it. This
  remains a build-environment concern, not a clean default-build claim.
- Browser acceptance in this report is **synthetic intercepted API only**: actual
  product static export in Edge, no real sign-in, SQL, model or physical device.
  Light/dark320/390/768/1440 layouts, full29-field descriptors, disabled input and
  confirmation, drawer scroll/visible close/help/Escape, pending/denied/unavailable/
  bad hash/stale/role revocation/logout/token replacement and explicit recheck.
  No domain writes or non-GET API requests. Final browser receipt recorded below.
- Original chat browser regression initially completed22 scenarios but failed its
  unchanged zero-console-error assertion: the new nested review link triggered
  static-server prefetch404s. Disabled background prefetch for that optional link;
  no error filtering/assertion/fixture weakened. Failed receipt retained at
  `QA/EVIDENCE/penta-chat-contract-1792333329204/failed.json`; final retest below.
- Final unchanged original chat browser **22/22 PASS**, no unexpected console or
  rendering errors, `QA/EVIDENCE/penta-chat-contract-1792333389801/result.json`.
  Its deliberate410/409/network error logs are retained by the existing assertions.
- Visual inspection found long drawers scrolling their header/Close out of view;
  fixed via scoped host CSS, not vendor alteration. An intermediate browser test
  then timed out because the test used a visible-role selector for an intentionally
  hidden revised-preview button. Corrected the selector to assert hidden AND
  disabled; no weakened product assertion. Both earlier receipts remain local.
- Route-ledger test initially failed because the new page was absent from the
  checklist. Added its unchecked row/correct84-page census; existing test untouched.
- No .NET/SQL/model suite rerun: unchanged backend/harness retains prior400focused
  .NET/422SQL; PENTA347selected Python retained, not new client acceptance.

## Receipts / reproduction

Local evidence: `QA/EVIDENCE/penta-onboarding-review-20261018/`:

- `review-final2.json`:48 cases,
  SHA256 `4e19e015a1c05654985b16f605c11b9ca57da647a2ed4ecde592cc03006927e9`.
- `channel-retained.json`:72 cases,
  SHA256 `141a43242ce1a3936979cf85d01677692161a31852d32d4c39bccc327f7a0fd0`.
- Earlier47-case review receipt retained, superseded by vendor integrity addition.
- Final new review browser: **18/18 PASS**,0 rendering/unexpected console errors,
  `QA/EVIDENCE/penta-onboarding-review-browser-1792333319757/result.json`, including
  token replacement after display, role change/recheck, and source-derived29-field
  layout. Eight theme/viewport combinations plus ten scoped state cases.
  Browser receipt SHA256:
  `61f2bd5b929471900c2fe276f55c38da07426a8e0173d39e172c66e2b3305758`.
- Current host loader SHA256:
  `2e3d2c511527f9e8e187c54596b2146a5a09e1e344852bb29cac29c93b94b444`;
  mounted component:
  `388044cdd1ab713030cd33d3d7afcdc138bcdc0be870d3cb954048c4a966efe5`;
  host drawer stylesheet:
  `4aa25b831bc1d576172d5261c946b189b0d21c6ed15950c69b47d9eeb35cae7a`.
  Vendor UPSTREAM.json pins all12 source files. Package/lock/dependencies and
  backend gateway harness source remain unchanged.

Run from root:
`node QA/tools/penta-onboarding-review.test.cjs`,
`node QA/tools/penta-onboarding-channel.test.cjs`,
`node QA/tools/penta-chat-contract.test.cjs`,
`node QA/tools/workspace-entry.test.cjs`,
`node QA/tools/penta-foundation-contract.cjs`.
Build from apps/web: `npm run build -- --webpack`.
Then root: `node QA/tools/penta-onboarding-review-browser.cjs`.
Each browser run owns/closes only its own ephemeral server/headless Edge process.
No user browser, running API or unrelated container is stopped.

## Remaining / next route

- [ ] Pair this exact browser/bootstrap with authenticated real disposable-SQL
  responses, including cross-actor/tenant denial and post-read changes. Synthetic
  success must not be counted as that integration. Developer DB remains untouched.
- [ ] Current operational session/draft creation/selection, real validated chat
  turn and same-request reconciliation, exact current full Projection updates.
  Do not wire the model straight to domain controllers or infer readiness from UI.
- [ ] Host preview/challenge/confirmation/execution/status/real receipt adapters
  and once-only SQL/HTTP/browser proof. Preserve explicit approval/replay/audit.
- [ ] CORE-01 actual model correctness/latency qualification, approved exact
  profile/resources and fresh frozen cases; E5-R18 prompt is inactive/unqualified.
- [ ] All other families/roles/languages, Manual complete light/dark/device parity,
  providers/privacy/security/operations/enterprise/exact release/Azure gates.
  BUG-DATA-0001 remains OPEN;90% remains target, not measured completion.

Continue Sol High for paired SQL/authenticated projection/action integration.
Medium only for settled bounded visual work. No PENTA contract change/project
switch needed now. No model/resource/credential/config/schema/dev DB/main/stage/
commit/push/Azure changes. QA/EVIDENCE local/untracked; .build-check ignored.
Production NOT READY; no all-portal visual or actual operational AI completion.

## Local runtime restoration

Earlier retained local processes stopped during the long task; final smoke found
neither3000 nor5092 listening. Cause is not established (do not attribute it to
the frontend changes). Restored only the existing services, not another project:

- API session99698, same unchanged normal net10 DLL hash
  `049bba230504354ee8245f32a1ffaa01dc0218429b1f30d7f53bc2f21deaa504`.
  Process-only Testing configuration, existing Development connection accepted
  only after a fail-closed local-server check; no connection/secret values printed.
  Bootstrap credentials empty, migrations/media/PENTA/Mini/operational gates OFF.
  No seed, migrations, password reset or configuration-file edit.
- Web session67371, `npm run dev -- --webpack --hostname localhost --port3000`
  (CLI uses separate `--port 3000` arguments), existing source/dependencies.
- Final web `/penta/onboarding`200, API `/health`200, anonymous
  `/api/auth/session`401. Actual Admin login/owned real draft remains unverified.
  The unbound route displays guidance, not a customer preview or active AI.

This process restart is not a SQL integration acceptance or production activation.
