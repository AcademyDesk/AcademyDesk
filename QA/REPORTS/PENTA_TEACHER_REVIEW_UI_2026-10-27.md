# Protected teacher review UI — 27 October 2026

## Scope and current checkpoint

Added `/penta/teachers?conversationId=...&draftId=...` as the existing teacher
workflow's protected review surface. It uses actual shared PENTA foundation
components/tokens: Prism light, Orbit dark, responsive cards, exact details,
separate review/approval/create controls, confirmation dialog, cancellation,
verified receipt/profile link and independent Manual navigation. No demo records
or automatic writes are embedded. Untrusted query values are lookup keys only.

The page is not a full teacher conversational experience: qualified interpretation,
dialogue/routing and model availability remain separate unfinished work. It is
not linked as an enabled production AI tool. The backend remains Testing-only.

- [x]52existing client transport checks PASS, TypeScript PASS, zero-warning
  targeted lint PASS (including the changed workspace shell boundary).
- [x]Fresh export `a8dad96441d04369b862eb0488039dcb`:88static pages/636files,
  source-verified. Existing physical dependencies copied into an isolated D:
  build directory; no install/download/main changes. This is a local export,
  not a clean GitHub/Linux/release-image acceptance claim.
- [x]Frontend window `1ee8f74f752d476a8db8ae964318b2d5` restored the exact two
  original PENTA service IDs/images/health, preserved the entire Docker inventory
  and released its exclusive validation lease.
- [x]Native browser window `92c0925d43ca429f8df85004dd09738a`, SQL
  `0f29de38a9b94e7084d3c79dbfdfeb0d`:six real browser/SQL journeys/35HTTP PASS.
  Retained21Core/30workflow/15service/45availability cases also PASS.
- [x]Final linked qualifier
  `QA/EVIDENCE/penta-teacher-ui-qualified-098af81a2b4d488b88f8f7cd29ac9abf/result.json`
  verifies actual native/SQL results, current API/web/harness/export pins,
  accepted3,065API source,11SDK/33CI proof, exact original services/images/API200,
  entire Docker inventory, owned SQL cleanup, released lease and unchanged main.
- [x]All eight actual PNGs inspected; additive `visual-review.json` in that
  qualifier records exact image hashes/observations. Its original result retained
  screenshotReview=PENDING until inspection; the separate review completes that
  evidence without rewriting the original result. Physical Android/iOS and full
  keyboard/screen-reader acceptance were NOT RUN.

## Behavior

- Passive loading only checks the authenticated account, protected saved draft,
  then current account again. Invalid/extra/duplicate query parameters, unavailable
  host or denied current authority withhold private details and usable actions.
- Draft details come from the verified canonical payload and hash. Changes require
  a fresh review. Approval opens a real dialog and explicitly records only the
  exact draft's authorization; creation is a separate button afterward.
- The component uses the existing client transport's single-flight and one-use
  execution controls. Lost/failed outcomes clear displayed details and offer
  same-draft GET recovery; no blind POST retry, automatic approval or write re-arm.
  A completed receipt links to the actual teacher profile route with its source ID.
- Current account/token/login-epoch changes and sign-out hide the private view;
  server401/403 closes the transport. Expiring approval disables actions locally,
  while the backend independently checks it within the transaction.
- A route-level shell exclusion mirrors protected onboarding, preventing duplicate
  enterprise navigation without changing endpoint authorization. The page owns
  its scrollable canvas and the shared modal has bounded independently scrollable
  content. Mobile/tablet/desktop visual and actual scroll acceptance still need
  the native browser results below; a CSS declaration is not proof.

## Native test design

The browser runner uses the installed Edge/Playwright runtime and a run-owned
loopback bridge into the actual QaApiFactory/Identity/SQL pipeline. It serves only
the source-verified static export and forwards the browser's bearer, never an
inherited seeder identity. Seeded drafts and identities are invented QA data;
bearers stay in process/stdin, not evidence files. No interpreter/model is called.

Six actual passing journeys:

1.Prism320px: review → approval dialog → explicit approve → separate create →
  authoritative success → reload completed receipt without redispatch.
2.Orbit1440px: the same real host flow.
3.Orbit390px: drop the actual201execute response after commit, show uncertainty,
  then GET-only recover and reload; exactly one execute request.
4.Orbit390px: cancel, terminal view, no teacher creation.
5.Teacher bearer: actual account denial, no private details or usable create.
6.Unavailable host768px: gates off, no private details/actions and Manual available.

Actual postconditions:three created teachers, three execution audits, three
succeeded drafts and one cancelled draft, unchanged accounts/unrelated state,
zero429/page errors/horizontal document overflow. Eight PNGs and actual HTTP
status/method metadata retained without request bodies/tokens. Only the owned
SQL database/login/container were removed; exact services/images/health and all
Docker inventory restored. The existing102application/sevenIdentity migration
histories were verified by the runner. No model call.

The mobile cancellation screenshot preserves the existing scroll position; its
status is browser-asserted and aria-live announced, while scroll-to-status/focus
polish remains a bounded follow-up. This does not claim perfect all-state visuals,
physical devices or all-portal parity.

## Retained failure

Initial UI window `fbbac70042854b889af132d581e807c2` passed52client checks/types
but lint rejected ref access while rendering the recovery button. The component
now relies on render state for the button and checks its channel ref inside the
handler. Lint was not weakened; the successor passed. Initial failure and exact
service restoration evidence remain. No build was falsely claimed from that run.

## Wider limits and next route

Current backend is qualified separately by3,065API,21CoreSQL,30workflowSQL,
15serviceSQL,45availability cases,11actual shared-schema/SDK checks and33CI
guards. See `PENTA_TEACHER_CORE_2026-10-27.md` and
`PENTA_TEACHER_WORKFLOW_2026-10-27.md`; their previous source receipts are history.

Owner's stop-before-model-loading remains binding. No model loading/train/inference/
activation, downloads, live customer data, commit/push/main/Azure or issue closure.
Production remains NOT READY: qualified Mini, genuine teacher conversation routing,
other workflow cohorts, all-role/Manual/customer-critical/device and exact existing
enterprise/release gates remain open. These local proofs do not establish90%AI.

Next Sol High: qualified shared conversation routing/integration; this native
review slice is complete and should not be repeatedly expanded. Sol Medium for settled bounded UI adjustments; no need to repeat
accepted enterprise audits or expand a completed control indefinitely.
