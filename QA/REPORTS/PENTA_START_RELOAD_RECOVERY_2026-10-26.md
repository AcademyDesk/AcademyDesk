# Protected onboarding start recovery across reload — 26 October 2026

## Final bounded acceptance

- [x] Reproduced the lost-recovery-key gap; repaired start checks58/58, adjacent
  operation59/59 and dialogue47/47 PASS. Full TypeScript/targeted lint PASS.
- [x] Fresh offline export **42fe121177754f509c53106a09b5bbfa**, **87/87 static
  pages /627verified files**, current217source pins unchanged; no package install.
  Frontend window **dca65f4642d24d6db343e7857a126fe5** PASS, reusing only the
  prior unchanged-source start/typecheck results after the retained lint timeout.
- [x] Harness rebuilt from current source. SQL run
  **eb5a3c5cc3bf4a38b84ceb68f8fd8c24**, window
  **d86e4a5aed4a4d56b9056cbd33e3d943**, PASS/exit0 in334.9seconds.
 102application +7Identity migrations; retained module records473PASS markers
  (not473independent xUnit tests). No existing core/atomic/start assertion removed.
- [x] Five actual exported browser/Identity/SQL scenarios, **37HTTP requests**:
 320px light,1440px dark,390px lost-response/reload/recovery, unavailable service
  and Teacher refusal. Lost response after committed201 survives real reload;
  no new POST on mount/reload; same-key GET recovers, wrong actor403. No429/page
  errors/horizontal overflow in these journeys. This fixture bootstraps real
  host-issued synthetic tokens; it is **not login-UI acceptance** or real inference.
- [x] Browser exact deltas: three sessions/empty drafts/start markers/start audits;
  zero student creations/interpreter calls/unrelated state changes. Six screenshots
  inspected, including the390px recovery-only screen. The inherited320px chat area
  is crowded; no claim of all-screen visual or physical-device acceptance.
- [x] Owned database/login/container removed by the harness. Both windows restored
  exact original PENTA IDs/images/running/health and the full Docker inventory;
  exclusive lease released. No cleanup/pruning of any other container or data.
- [x] Separate read-only linked recheck rehashed the complete627-file export,
  current frontend/API source, native receipts/counts/no-retry/denial and cleanup:
  `QA/EVIDENCE/penta-start-recovery-qualified-a60e6cecc7b9416eae4a8955bc40885d/result.json`.
  It pins all nine changed application/test/window inputs. Main status/HEAD unchanged.
- [ ] This closes the bounded start-reload recovery gap only. It does not enable
  real AI/production writes, qualify a model, close broad audit/security issues,
  complete host workflow families or authorize Git/Azure. Model loading remains
  explicitly declined by the owner; no repeated approval request is pending.

No application API/domain source changed, so the full2,995-test suite was not
rerun or relabelled as new evidence. Existing domain services/accepted API source
were reused. Focused SQL additionally reverified actual rollback/duplicate/current
authority controls because this client consumes their protected start/receipt path.

## Problem and implemented behavior

The existing explicit onboarding start creates a protected empty conversation/
draft with a host-owned idempotency receipt. When its response was lost, the
client could recover by GET only while the original page remained mounted. A
refresh discarded the request ID and offered a fresh start, so the existing
server recovery endpoint was no longer reachable from that page state.

The client now writes only the opaque start request ID to its current URL before
the single POST. A reloaded recovery URL starts in read-only reconciliation mode:
the existing current-account/academy checks still run, and explicit “Check
conversation start” calls the same authenticated host GET. It never resubmits
the start or grants creation/approval. Successful recovery replaces the URL with
the existing conversation/draft workspace. No prompt, record, tenant, user,
credential, authority claim or approval is stored in the recovery URL.

Malformed/duplicate/extra recovery parameters reject before transport. Failure to
retain the lookup ID, concurrent route navigation, token or login-epoch changes,
invalid receipts and unknown delivery remain fail closed. The only permitted
self-authored route-fence change is the exact history replacement immediately
before POST. Manual remains available, including during unavailable AI service.

Next.js's installed Native History API documentation was read before changing
the page: `window.history.replaceState` integrates with `useSearchParams`, while
the same start component remains mounted. The real exported browser check is
required to verify this interaction; transpiled transport tests alone are not
acceptance of React/router behavior.

No application API/domain/schema/authorization/tool/runtime activation changes.
The existing actor/tenant-bound GET, protected start marker, domain/Identity
transaction, current-authority checks and mandatory audit are reused. A guessed
or copied URL request ID is never a bearer capability.

## Baseline and focused validation

- Actual new-cases baseline on the old client: **47 PASS / 11 FAIL**; retained at
  `QA/EVIDENCE/penta-start-reload-baseline-20261026/result.json`.
- Repaired start transport: **58/58 PASS**, including recording the ID before
  POST, losing the response then reloading, recovery while interpreter unavailable,
  duplicate/forged query refusal, failed History API persistence, invalid receipts,
  one-POST locks and route/token/login-epoch fences. Retained at
  `QA/EVIDENCE/penta-start-reload-fixed-20261026/result.json`.
- Adjacent existing operation **59/59 PASS**, dialogue **47/47 PASS**. These are
  synthetic transport/coordinator checks, not actual inference/SQL qualification.
- Full worktree TypeScript PASS and targeted lint PASS. First lint attempt hit its
  120-second timeout with an empty log: no false PASS or assertion removal. Exact
  original services/inventory restored. The successor reuses unchanged-source
  start/typecheck evidence and gives lint a bounded300-second startup allowance;
  lint then passed. First failed window:
  `QA/EVIDENCE/penta-start-recovery-window-e9549bd83403404bbecd757279d4ce6e/result.json`.

## Export and SQL/browser acceptance

**PASS, within the scope above.** Fresh export window
`dca65f4642d24d6db343e7857a126fe5`, build
`42fe121177754f509c53106a09b5bbfa`. Native window
`d86e4a5aed4a4d56b9056cbd33e3d943`, SQL/browser run
`eb5a3c5cc3bf4a38b84ceb68f8fd8c24`. Exact services and inventory restored.

The first SQL launcher attempt was blocked **before creating SQL** by Windows'
script execution policy. Harness build had passed; failure and exact restoration
are retained in window `0abccd63dc5e44b0959469bb744dd1f8`. The corrected launch
scopes `-ExecutionPolicy Bypass` to the approved child process only: no machine/
user execution-policy or persistent setting was changed. Its incremental harness
build and full retained module then passed. The focused SQL container was bounded
to2 GiB/2CPUs/1536MiB SQL memory and `--pull=never`; only cached image used.

The additive native check retains every existing OnboardingAtomic/Core/start
regression. Its five actual browser/Identity/SQL scenarios include the same
lost-response scenario with a real page reload, no second POST, explicit GET-only
recovery and a wrong actor403. Dedicated run-owned academy avoids reusing
exhausted draft quotas without deleting fixtures, relaxing limits or changing
dates. Exact postconditions remain three sessions/drafts/start markers, three
start audits, zero students/account/domain mutations and zero interpreter calls.
Do not call these actual model conversations or physical-device acceptance.

The live runner uses a source-verified isolated static export, not a replacement
dev stack or modification of main. Disposable SQL is owned by the established
harness; only its own container/database/login may be removed. Existing containers,
images, volumes, networks and original PENTA service IDs/images must be restored
and compared after each bounded validation window. All resources stay local/D:.

## Remaining scope

This repairs an interruption/reload control in the existing protected onboarding
foundation. It does not expand tools/roles, enable the Testing-only gateway in
production or qualify a model. Owner's explicit stop-before-model-loading stays
in force. Existing model full-plan6/12fresh7/12exposed NOT ACCEPTED, connected
workflow families, actual all-role/device/customer-critical acceptance and exact
GitHub/staging/release/rollback/Azure gates remain open. No commit/push/deploy,
new package/model download, model load/train/inference or customer data.

Engineering route: Sol High for current host integration, authority and real SQL/
runtime validation. Sol Medium is suitable for settled visual refinements later.
