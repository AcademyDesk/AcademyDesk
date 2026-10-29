# E5-R27 — mounted pending202 → same-claim completion recovery

19 October 2026. Worktree `D:\AcademyDesk-codex-p0`, branch
`codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.
**Named recovery gap: PASS against real disposable SQL and browser.**
This is additive QA, not new application functionality or Mini qualification.

## Scope and proof

- Added `PentaOnboardingPendingBrowserRegression.cs` and
  `penta-onboarding-pending-live.cjs`, invoked after the existing review,
  operation, Start and dialogue browser journeys. Existing assertions, runner,
  quotas and 30-second gateway deadline are unchanged.
- Two fresh browser contexts: Prism/light320 and Orbit/dark1440. Each mounts an
  actually admitted, durable pending claim. Mount performs two authenticated
  GETs and withholds draft details/canvas. First explicit **Check pending
  request** receives actual202 from the exact claim result; pending is not success.
- Only after that observed202, a separate Node QA controller completes the
  **same already-claimed fixture** through the existing protected completion API.
  Its loopback-only relay requires a random, in-memory controller secret, exact
  known fixture, prior202 observation and a single completion. It rechecks the
  actual pending request/draft/absence of completion audit before dispatch.
  No new claim, interpretation, domain operation or student is dispatched.
- This completion endpoint exists only in the disposable harness relay, not the
  application or page. Controller secret/bearer are passed over process stdin,
  never written into evidence, command-line arguments or browser controller code.
  The bearer is installed into the isolated browser's existing synthetic-account
  session storage; the controller secret is not exposed to that page/storage.
- Second explicit check receives actual200 from the identical result URL, then
  freshly verifies account/current/draft/digest/current and final current cursor.
  The canvas displays the actual revision2 partial draft, read-only. Composer,
  sending and creation remain unavailable; no approval/control becomes usable.
- **11 browser GETs each /22 total**: mount2, pending check2, completion recovery7.
  No browser POST, request replay, automatic polling/retry, new request ID,
  student creation, interpreter configuration, model inference or external traffic
  in these two journeys. The test controller's two completions are explicitly
  separate POSTs, not falsely counted as browser GET-only activity.
- Independent fresh SQL checks require two cleared pending cursors, two revision2
  AwaitingReview drafts, two completed claim receipts and **exactly22 new audits**:
  current reads12, result reads4, draft reads2, completions2, partial saves2.
  Every audit has the exact synthetic actor/academy. Unrelated students,
  guardians/links, invoices/payments, Identity users/roles, academies/branches,
  sessions/drafts/turns remain byte-identical to the pre-browser snapshots.
- Separate synthetic academy preserves the existing daily quotas. No limits
  raised, existing fixtures deleted, raw API bodies mocked or assertions relaxed.

## Validation observed this slice

- [x] Isolated SqlHarness build: zero warnings/errors.
- [x] Node syntax and diff/whitespace checks PASS.
- [x] Existing recovery42, dialogue36 and terminal33 client contracts PASS:
  **111 checks**, no additional client declarations introduced.
- [x] Existing runner with `QA_ONBOARDING_REVIEW_BROWSER=1`:
  `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.
  Run **`bc11789e2ffc4145a2661fce1744e1af`**, observed **exit0 /172.6s**.
  Runtime inventory360 (349controller method/routes +10Identity) and both
  resolved DbContexts match exact owned target/runtime login.102app/7Identity
  migrations apply only to that disposable target.
- [x] **461 retained SQL/API checks** (451 existing +10 R26 guards) and **29 actual
  browser contexts** (15review +4operation +5Start +3dialogue +2new pending).
  R25's3 nested terminal reload journeys also retained; not3 additional contexts.
  New recovery journeys have22exact audits/no429/no unexpected console errors.
- [x] Retained operation journeys still create3 synthetic students/3execution
  audits; retained dialogue creates1 only after separate explicit approval.
  New pending journeys create **zero**. These counts are distinct fixtures,
  not claims that no writes occur anywhere in the full regression.
- [x] Negative cleanup refuses a mismatched database marker. Existing runner
  reports exact owned database/login teardown and checks container name, run
  label and loopback SQL binding before stop/removal. Exact successful container
  is absent after exit0. Other/failed/PENTA containers were not pruned or removed.
- [x] Pending mobile screenshot and both completed-state screenshots inspected.
  No horizontal overflow at tested widths. This is viewport recovery evidence,
  not full premium visual acceptance or Android/iOS physical-device proof.
- [x] All14 R26 application/test source pins still match. Isolated candidate API
  DLL and normal developer DLL unchanged. Web3000 onboarding200/API5092
  health200/anonymous session401; actual developer Admin login still unverified.

R26's189 focused .NET tests, TypeScript/lint/webpack87 and other synthetic browser
counts are **retained prior evidence, NOT rerun here**: no application source
changed in this slice. Existing accepted tests were not restarted as a new audit.

## Evidence and source pins

Evidence is intentionally local/untracked under
`QA/EVIDENCE/penta-onboarding-pending-live-bc11789e2ffc4145a2661fce1744e1af/`:
`result.json`, independent `sql-postconditions.json`, and four screenshots.
The other same-run `*-live-*` folders retain each original browser ledger and
parent SQL postcondition receipt. No secrets recorded.

SHA256:

| Path | SHA256 |
| --- | --- |
| `QA/tools/SqlHarness/PentaOnboardingPendingBrowserRegression.cs` | `24BCAC54E53B7826FE45D130DA7973FD41B9B01E339C94D39F199CF1DCDBB0A8` |
| `QA/tools/penta-onboarding-pending-live.cjs` | `1951612446B617BB1014FBFAE30BD7A6AB272B49D28B10143DC3099F1EFB2576` |
| `QA/tools/SqlHarness/OnboardingAtomicRegression.cs` | `222184B3B525A31AF21B1C8B954B4FEBDB11CB63390E46FE904A3B140EB4FB48` |
| `QA/EVIDENCE/penta-onboarding-pending-live-bc11789e2ffc4145a2661fce1744e1af/result.json` | `CF410C8EE207DCFC249AD0777DE13117F908DD279A3C806C67C2342F74208425` |
| `QA/EVIDENCE/penta-onboarding-pending-live-bc11789e2ffc4145a2661fce1744e1af/sql-postconditions.json` | `8793320FF8815E423A9E8BD33432E968D34E7D0D47FF4921A1A96D15F614E13B` |

Isolated harness DLL
`7EE3EAB9DCC66F23AC906B7C59698935B7FAD7DEC21F8036AD074A9E75EE1AC6`;
its unchanged API DLL
`DC6DBBE43C601270F783DB22D4E5D5E02C93FEE0F39F1DDA1983FAF1317111FA`.
Unchanged normal developer API DLL
`049BBA230504354EE8245F32A1FFAA01DC0218429B1F30D7F53BC2F21DEAA504`.

## Remaining work / next route

- [x] E5-R25 terminal re-entry, E5-R26 explicitly synthetic host conversation and
  E5-R27 actual mounted pending202→completed pairing now have named evidence.
- [ ] Genuine Mini CORE-01 semantic/latency qualification and reviewed real-runtime
  admission remain BLOCKED. Scripted QA and controller-supplied completion are
  **not** evidence of model reasoning or real gateway pending completion.
- [ ] Durable/expired-session history, broader conversational families/roles,
  complete Manual parity, privacy/provider/security/enterprise/physical-device
  and exact release gates remain OPEN. BUG-DATA-0001 remains OPEN.

Stay **Sol High** for real-runtime qualification/admission and consequential
authority integration; Sol Medium only for settled bounded presentation work.
Use the established Mini handoff when engine qualification needs work; no new
shared contract or automatic project switch was needed for this slice. No paid
resource, download, training or activation authorized by this QA result.

No application/schema/default/vendor/engine/weights/developer-data/password/main/
PENTA-project changes, stage/commit/push/merge/Azure. HEADs unchanged; evidence
local, `.build-check` ignored. This closes one named host-recovery gap, not90% AI,
all backend work, enterprise testing or production readiness. **NOT production-ready.**
