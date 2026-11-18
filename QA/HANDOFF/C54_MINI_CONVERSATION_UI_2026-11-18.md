# C54 — gated Mini conversation and read-only receipt recovery

MODEL: Sol High
TOOL: Cloud Codex
BASE: 400d0c6bdca302332df3b046dc93c67bd8410eb8 (C53 over Desktop4cbe56a7)
BRANCH: codex/cloud-c54-mini-conversation-ui-20261118
STATUS: IMPLEMENTED / CLOUD VALIDATED / NATIVE SQL AND MODEL QUALIFICATION PENDING — 18 November 2026 UTC

The owner assigned cloud the application/model integration. Latest Desktop4cbe56a7
and all published C50–53 claims are checked; no concurrent path overlap observed.
Cloud claims only:

- apps/api/Controllers/PentaChatController.cs — GET receipt route.
- apps/api/Intelligence/Penta/PentaMiniOrchestrator.cs — own/current receipt read and mandatory audit.
- apps/api/Intelligence/Penta/PentaMiniProtocol.cs — recovery receipt binding validator.
- tests/AcademyDesk.Api.Tests/PentaMiniRecoveryTests.cs — new recovery boundary regressions.
- apps/web/src/lib/penta-mini-conversation.ts — new scoped client/recovery state.
- apps/web/src/components/penta/mini-conversation.tsx — new private evaluation composer.
- apps/web/src/components/penta/role-penta-entry.tsx — default-off build gate only.
- QA/tools/penta-mini-conversation.test.cjs — new client tests.
- QA/tools/penta-mini-conversation-browser.cjs — actual Chromium fixture runner.
- QA/tools/penta-browser-harness.cjs — exact unmodified C52 helper return, reused.
- QA/HANDOFF/C54_MINI_CONVERSATION_UI_2026-11-18.md — this ownership/result/handoff.

No SQL schema/migration/domain/finance policy, production activation, model routing,
model loading or packages are claimed. Desktop retains protected native SQL/device/
model qualification. The cloud Docker daemon is usable but has no cached SQL image;
no image was downloaded or existing container/resource changed. Native SQL receipt
acceptance remains a stated prerequisite unless existing safe infrastructure appears.

Implement the existing read-only Mini host conversation contract. Recovery must be
GET-only, same authenticated actor/academy/session/request, current version and
unexpired read-only purpose, with mandatory audit and no planner/connector call.
Store only scoped opaque recovery IDs/version/expiry in sessionStorage, never raw
prompts, tokens or receipts. Unknown outcomes block another send until recovery or
an explicit separate conversation. No automatic POST replay or retries.

A NEXT_PUBLIC_PENTA_MINI_EVALUATION=true build enables the evaluation component;
it is absent by default and does not open backend gates. Existing Owner/Admin,
Development/Testing and Mini/pilot flags remain mandatory on the server. Existing
guided/Manual workflows remain available. No new tier selection, unqualified live
model claim or model-generated HTML. Do not represent numeric finance payloads as
new exact financial authority; use authorized Manual sources where exact terms
are not in the retained protocol.

Test client failure/foreign/malformed/session/storage/recovery boundaries, actual
browser start/clarification/selection/unavailability/lost response/reload, types,
lint/build and affected API tests. Build the default-off variant as well. Retain
failed runs and exact source bounds. No main/Azure/environment publication/public
access change, model/package download/loading/training/inference/activation.
Mini/Plus/Pro unqualified, Ultra deferred; six critical records and90% remain open.

## Implemented behavior

The existing read-only Mini application contract now has an optional real host-
conversation client/composer. It uses the Academy API, not direct browser-to-model
access. It sends the existing minimal text/request/version/executor payload;
model proposals still pass through current server authorization and shared reads.
No model, tier or provider is activated by this code.

The component is absent unless built with
`NEXT_PUBLIC_PENTA_MINI_EVALUATION=true`. By default it renders nothing and sends
zero Mini requests. The enabled build mounts it only for an already verified
Owner/Admin academy account; Finance User, Teacher, Student, Guardian and Platform
pages do not mount it. The build flag is visibility only, not permission. Existing
Development/Testing, PENTA/Mini flags, current identity/academy/Finance rights and
native SQL requirements remain mandatory on the server. Production/Staging gates
were not opened, and guided/Manual workflows remain available.

The operator explicitly starts a private conversation and explicitly sends each
turn. The UI displays the host's verified result/clarification/refusal/error,
learner labels/codes/subjects and fixed authorized record links. Choosing a result
sends an ordinary ordinal follow-up through the model/host contract, not a browser-
authorized tool or SQL call. Host-provided HTML-shaped labels are escaped text.
Unknown envelopes, fields, result IDs, versions, providers, capabilities or source
paths never render as verified data.

### GET-only recovery

New route:
`GET /api/academies/{academyId}/penta/chat/conversations/{conversationId}/turns/{requestId}`.

The service requires current authenticated actor/academy/Finance authority, own
unexpired read-only session, the same stored request, no pending/newer turn and
a valid purpose-decrypted receipt. Onboarding-purpose sessions, changed versions,
foreign requests, invalid protected receipts or inactive referenced learners are
refused. Recovery and its mandatory replay audit run in a serializable transaction
under the existing academy lock; audit commits before the response is returned.
The recovery method never calls the planner or connector again. Private Mini
health/create/turn/recovery responses now declare no-store.

The client keeps exactly six opaque fields in sessionStorage: epoch, academyId,
conversationId, requestId, expectedVersion and expiresAtUtc. It stores no prompt,
access/refresh token, private result or receipt. Foreign/corrupt pointers are
removed. Storage failure prevents a turn POST and retains the unsent text for the
operator. A changed token/epoch clears the UI and late replies are not adopted.

An unknown turn blocks another send. Checking its saved response and reload
recovery use GETs only; there is no automatic POST replay/retry. A stored receipt
can recover while inference is unavailable, with follow-up/start buttons disabled
until availability returns. The operator may explicitly start a separate
conversation rather than redispatching an unknown prompt. A lost creation reply
has no known conversation ID to recover; no prompt was sent, and a separate start
remains explicit. Empty created sessions may consume the existing daily quota.

### Exact financial display remains bounded

The retained0.1 host receipt represents balances as JSON decimal numbers. This
new UI does not round or present those as newly exact financial terms. Learner
results link to Manual Finance/record sources for exact amounts. An exact-string
financial receipt/presentation extension remains separate cloud work; current
lookup/clarification does not complete every finance outcome or establish90%.

## Fresh final validation

| Check | Final result | Scope |
| --- | --- | --- |
| PENTA API/host selection | **808 PASS,0 FAIL/SKIP** | Includes23 new recovery binding/cache-route tests and C53 guards; native SQL path is not qualified by these |
| Current PENTA client files | **24 files,984 PASS,0 FAIL/SKIP/CANCEL** | Includes38 new actual client-state tests; no model |
| Enabled evaluation Chromium matrix | **118/118 PASS in one final run** |108 Owner/Admin cases plus10 role-exclusion cases,1280/dark and320/light |
| Default build Chromium checks | **4/4 PASS** | Composer absent and zero Mini requests, guided lookup still mounted |
| TypeScript | PASS | Direct noEmit and both exact-lock webpack builds |
| Lint |0 errors,61 inherited warnings | Final component lint clean |
| Final enabled/default webpack exports | PASS /PASS | Next16.4.5,React19.2.8,90 static segment aliases; default-off export left in place |
| Actual production gate | BLOCKED /exit1 | Six critical acceptance records remain open |
| Native SQL recovery / real Mini / devices | NOT RUN | Explicit separate prerequisites below |

The final118-case run covers explicit start/no auto-POST, results, clarification
and second selection, empty/refusal/error/unsupported outcomes, degraded/denied/
malformed health, malformed/foreign/stale/private-extra replies, lost response,
GET recovery, reload recovery, inference-down recovery,404/409/410, blocked storage,
session clearing, duplicate submission, creation failure, escaped labels and role
exclusions. All final runs retain the Chromium sandbox, no captured runtime/
interception errors, owned profile removal and zero remaining contexts.
These are intercepted synthetic host fixtures, not model intelligence or native
SQL/domain/audit acceptance. Counters in them are not real committed business effects.

The initial108-case browser run had107 PASS /1 test failure because a leak regex
matched the short digits450 inside a random UUID. That raw result is preserved.
The assertion now checks the complete amount literal and exact six-key pointer
shape; the failing case and ten role gates passed. Review also corrected disabled
follow-up controls during inference outage and preserved unsent input on storage
failure. The final source then received the complete118-case and4-case runs above.

The first inner-runtime build failed parsing TypeScript --showConfig; direct
TypeScript and unchanged-source outer-runtime builds passed. The precise lower-
level subprocess failure was not established. The failure log is retained; no
package/lock change was used to hide it. Earlier enabled/default exports are
preserved privately alongside the final evaluation export.

## Reproduce without model activation

```sh
# Existing installed dependencies; do not acquire artifacts or enable a live model.
cd apps/web
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 NEXT_PUBLIC_PENTA_MINI_EVALUATION=true npm run build -- --webpack
cd ../..
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/penta-mini-conversation-browser.cjs
# Build and verify the inactive default separately:
cd apps/web
env -u NEXT_PUBLIC_PENTA_MINI_EVALUATION NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
cd ../..
QA_BROWSER_EXECUTABLE=/usr/bin/chromium QA_MINI_EVALUATION_EXPECTED=false node QA/tools/penta-mini-conversation-browser.cjs
node QA/tools/penta-mini-conversation.test.cjs
/workspace/.tools/dotnet/dotnet test AcademyDesk.slnx --no-restore --nologo -m:2 -p:UseSharedCompilation=false --filter FullyQualifiedName~Penta
```

The984 client run executes each current `penta-*.test.cjs` directly, as the
repository's test runner does, with individual raw logs/counts. Do not add its38
new tests or the API23 again to their enclosing totals. No whole frontend/backend,
new dependency audit/image build or model semantic result is claimed for C54.

## Required native acceptance / Desktop return

Cloud Docker28.4.0 is reachable, but cached SQL image inventory is empty. No SQL
image/package was acquired; no container, volume or existing service was changed.
There is no native database success receipt for this packet. Desktop should use
its existing owned SQL/Identity test infrastructure with a synthetic/counted
IPentaProvider or purpose-protected seeded receipt; this does not require loading
a real model. Verify on the exact integrated source:

1. A completed own receipt returns200 with no-store and exactly one new replay
   audit, while provider/connector invocation counts stay zero.
2. Unauthenticated, wrong actor/academy, revoked role/module/current account,
   inactive learner and onboarding-purpose access fail without private data.
3. Unknown request404, pending/newer request409, expired session410 and tampered
   protected receipt409 never dispatch the model or resurrect stale results.
4. Injected audit/store failure returns503 without a receipt or success claim.
   Check current-version/concurrent-turn behavior under real SQL locks.
5. Provider downtime still permits the current stored receipt GET under valid
   authority. Original stores/resources/keys recover and ownership cleanup passes.
6. After separately authorized Mini qualification, test the actual pinned model's
   clarification, correction, reference selection, failure and timeout paths
   through this composer/host. Synthetic UI tests cannot count toward that score.

Desktop owns artifact/runtime verification and the existing stop before weights
loading remains. This source must not be used to enable a live model, Plus/Pro
routing, reserves, training or production flags without their existing acceptance.

## Source, integration and current release status

C54 is based on C53 `400d0c6bdca302332df3b046dc93c67bd8410eb8`; integrate/review
C53 first, then this task's two commits or final11-path delta. The browser helper
is byte-exact C52 source and can be deduplicated if C52 was already imported.
Do not replay C49/payroll or C50–52 wholesale over newer Desktop work.

Last successful Desktop fetch at task start was
`4cbe56a76119ad257924d975c1bb2ecb97196022`, with no competing C54 path claim.
The previously confirmed exact run44065049153 had ordinary API/SQL/media/frontend
SUCCESS and separate Core FAILURE requiring read-only `PENTA_SOURCE_READ_TOKEN`
access to private AcademyDesk/PentaMini. During final refresh, Git and GitHub API
requests returned upstream503. This is a recorded refresh failure, not evidence
of revoked access or a confirmed newer source. Publication SHA/check status must
be taken from the final return/verification receipt, not assumed from local tests.
No workflow dispatch, repository secret/settings change or main merge occurred.

All1,814 unclaimed tracked base files remain byte-exact, including16 vendor files.
All33 frozen Mini files match their original hashes; Mini is clean. C53's provider
repair and original evidence remain intact. No SQL schema/migration/domain policy,
package/lock file, shared root tracker, tier registry or model source was changed.
The final source hashes and raw test/build/browser receipts are in
`/workspace/c54-mini-conversation-ui-20261118`. Raw private evidence is not committed.

The release checker still blocks BUG-DATA-0001/0002/0003/0010, BUG-SEC-0001 and
BUG-AUTH-REVOCATION-001. Broader critical/device acceptance, staging restore/private
Blob/rollback, exact-candidate required CI and model qualification remain open.
Mini/Plus/Pro are not production-qualified; Ultra deferred;90% unmeasured.
No Azure/environment publication, public-access change, weight download/loading,
training, inference or model activation occurred. Next route: Sol High for native
receipt acceptance and exact financial/model capability contracts; Desktop for
protected runtime/artifact/private-model evidence.

## Evidence hashes

| Receipt relative to the private raw directory | SHA256 |
| --- | --- |
| `api-tests-final.log` | `d5ff491e939dffd9e69e14fcdbeefcefe5cb51925ee6b9ad2c1c7429ada8c5f4` |
| `api-final-results/c54-penta-final.trx` | `81dd33c360cc7cbe97e6faa7776a24221f207841e165dd815d37c0d756c23e08` |
| `client-tests-final.log` | `246faa22e45e1a7a018796516ef6c071afc61549e70655dd1c17867f29d5fd24` |
| `client-final-regressions/summary.json` | `da9233c5fbab1aa5e3b72bce870e39a596b19ea160cfdba06735489e7229947e` |
| `browser-final-summary.json` | `069a855fc4230e983233fb868c3d62388290a23f0df787671efcda89c6001dbd` |
| `browser-evaluation-final-source.log` | `72e542a8142c6153cfa904098d900818f10c106fa455c10a0113a7547b3d974b` |
| `browser-default-final.log` | `4cce868c2ae2a8c16275690d85eff9b130799bf9bd99a8e01895555797d7585d` |
| `build-evaluation.log` | `38c02527ef41eb112a830a5bd8181ed3929d8f4c03ac181538f83b9af5aba5d4` |
| `build-evaluation-final-source.log` | `acbf8eb5493e50bfe2b91b2247ad6524695c8519144ac4cde556ff7421c91976` |
| `build-default-final.log` | `7fd60791bac8126617f70983fe21f75b5908125f081d518727d2bbb78d73bff4` |
| `types.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `lint.log` | `34086bd13d81b2afbca6a391c284d1354217b29a42530fa881af6de234381362` |
| `lint-mini-final.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `executed-source-pins.json` | `13c951bc69572ebaeaf66108105ad8a2d6179c29deafb8c8b07861ec30b7a597` |
| `source-integrity-final.json` | `96504322af2928e1292e73da689fb785e61bd32fe9d21e6103d302662d774f1f` |
| `cloud-sql-preflight.json` | `c75f07c3159df4c14fd475c7c8950b2f38d1c6000a35cea3ab0407c8b4e119af` |
| `production-gates.log` | `d7342475f63187e45a79e0a482851f5fe6345953f2878dc8851c6244c6b959e6` |
| `desktop-ci-jobs-last-confirmed.json` | `55169fc0a1d6680e3b80361ccaaea606045064148814910793c8683b4e501431` |
