# C6 live finance and Teacher controls — local acceptance

Scope recorded on 6 November 2026. Live acceptance passed on 7 November 2026. Base
`fd958a3b847b9869ed1dc8ef2a3fc70b90344fad`; continue the existing C6 handoff.

Scope: one bounded existing-harness module for real finance decision and Teacher
assigned-batch HTTP/SQL acceptance, followed by the current built UI against a
fresh loopback host using real Identity and native responses. Preserve existing
accepted tests, source-bound receipts, finance policies and original dirty trees.

Allowed QA paths: new ManualControls regression/browser host and browser client,
SqlHarness Program/runner registration, this report, related issue records and
shared continuation pointers. Reproduce any actual application defect before a
minimal repair; run relevant API regression after an application change. Do not
rewrite finance, Teacher, auth, AI or frontend systems speculatively.

Verify exact saved values/rows, invoice balance, publication/nullable dates,
assigned/foreign batch authority, canceled decisions sending no mutation, required
rejection notes, success/reset versus denial/unconfirmed retention, audit and
rollback boundaries. Browser interception may deliberately drop an actual server
response for the unconfirmed-outcome case; it must never fabricate a successful
API response. No injected browser session tokens or synthetic backend acceptance.

Keep both original PENTA service identities/images restored, 3 GiB admission guard,
owned SQL cleanup and raw local evidence. No new package/image/model download,
candidate model loading/training/activation, production DB, main or Azure change.
Both C6 issues stay OPEN until their remaining critical/device/release protocols
pass. Route: Sol High for this real authorization/financial integration.

## Reproduction and repair on 7 November 2026

The unchanged application passed 36 native controls in SQL run
`6a0a2ff2b855463a8c8a2a2d9dba7178` at `746c69f`. Three blank rejection notes
returned 200 and changed persisted state. A Platform Owner decision returned
200, changed persisted state and bypassed the audit interceptor. The Admin
fault control returned 500 and preserved SQL state. The retained native receipt
records all four failures.

The decision action now rejects null, empty and whitespace rejection reasons.
Approvals can carry an empty optional note. The action opts Platform Owner into
the existing SQL save and audit transaction. The established invoice lock applies
to both actors. Three API regression cases exercise the validation.
[Finance decision issue](../ISSUES/BUG-FINANCE-DECISION-GUARDS-001.md)

## Accepted run

Window `faa53a1b4fad4a5d98e7d7d89b4bf857`, SQL run
`ac6860b7f7734f26beee7e37ae216824`.

- **41 native HTTP and SQL controls passed.** Three captured finance decisions
  returned the correct status and balance with one attributed audit each. An
  invoice of 1,000 with a reconciled payment of 600 leaves 300 collectible after
  approval of 100. Rejection leaves 400 collectible. Repeat, anonymous, Teacher
  and foreign-academy decisions were denied. Both actors rolled back after an
  injected audit failure. A successful Platform Owner decision recorded one audit.
- Five captured Teacher submissions persisted one record each. Checks covered both
  publication booleans, assignment and lesson values, fractional score 12.5,
  null and filled assessment dates, and fresh readback. Published homework queued
  one in-app notification for the assigned learner. Wrong-batch, foreign-batch,
  anonymous, string-boolean and empty-date requests left the captured SQL state
  intact. Filled date readback compares UTC clock ticks across SQL and HTTP.
- **13 live browser and SQL phases passed.** Real Identity login and API responses
  drove seven finance phases at 1280 px and six Teacher phases at 390 px. Prompt
  dismissal and blank rejections sent zero decision PATCH requests. Deliberate
  decisions saved and cleared their pending entries. Teacher saves reset the form.
  A real 400 retained the denied draft. Dropping one actual committed response
  retained the assessment draft and showed the unconfirmed outcome. SQL found one
  row and the driver saw one submission. The run recorded 37 browser API responses,
  zero 429 responses and zero uncaught page errors.
- The harness built with zero warnings or errors. The browser retry reused the
  verified DLL and backend source hashes. All 658 pinned frontend export files
  matched their recorded hashes. **All 1,989 API tests passed** with zero failures
  or skips.
- The fixture applied 85 application and seven Identity migrations. The runtime
  login, database and exact SQL container were removed. Docker inventory matched
  preflight. Both original PENTA services returned healthy and API health returned
  200. The exclusive lease was released.

The captured C6 input receipt SHA256 is
`d4b8555459d1fae0eb565cf8b38b4709d3b7c1a6b7f5cc40bd0e903c67bd5697`.
The loopback bridge forwarded actual browser requests and exposed a fixture-only
route for read-only SQL assertions. The response-loss case forwarded one POST
and discarded the real response. Raw logs, requests, hashes and proof stay local.

## Retained attempts

- `f6c36cc94c5c45ca866b11aa45ba8109` stopped on a generated test invoice number
  longer than the SQL column allows. The fixture prefix was shortened.
- `60ccb64d8aca4ea3954b9c306ae36226` stopped at the low-memory cutoff during setup.
  Disposable C6 SQL now targets 1,024 MiB inside its 2 GiB container. The coordinator
  retained the 3 GiB admission guard and 512 MiB cutoff.
- `ba82751a9bcd42939c17c7f6f5f9d703` reproduced the four application failures.
- `ee3e9bd56f504e1e9210e2905bd20954` passed 41 native controls and seven finance
  browser phases. The driver targeted the desktop sidebar at phone width. The
  corrected driver uses the visible mobile bottom navigation.

Each failed window kept its receipt, cleaned up its owned SQL container and
restored both original PENTA services.

## Reproduction and release gates

Generate the genuine C6 receipt with `QA/tools/manual-controls-browser.cjs`
against the pinned export. Build `QA/tools/SqlHarness/SqlHarness.csproj` with
`--no-restore`. Set `QA_MANUAL_BROWSER_RECEIPT` to the receipt and
`QA_CLOUD_FINANCE_RECONCILIATION=1`. Run `QA/tools/SqlHarness/Run-ReconciledPayment.ps1`
with `-Module ManualControls` and `-HarnessPath` pointing at the fresh DLL. Use the
established exclusive lease, D: temporary root and owned cleanup.

For live browser testing, serve the verified export at the paired loopback web
origin. Start the same module with `QA_MANUAL_LIVE=1`, `QA_BROWSER_API_PORT` and
`QA_BROWSER_ORIGIN`. The READY line provides the run and owned stop marker.
Supply `QA_MANUAL_LIVE_ORIGIN`, `QA_MANUAL_LIVE_API`, `QA_MANUAL_LIVE_RUN`,
`QA_MANUAL_LIVE_FIXTURE_ROOT`, `QA_MANUAL_LIVE_PROOF`, `QA_MANUAL_LIVE_OUTPUT`,
`QA_MANUAL_LIVE_PLAYWRIGHT` and `QA_BROWSER_EXECUTABLE` to
`QA/tools/manual-controls-live-browser.cjs`. The proof JSON binds `run`,
`sourceSha`, `webSource`, `artifactRoot`, `sourceHashes` and an `artifacts` array of
relative `path` and `sha256` pairs. Signal the exact owned stop marker after the
driver finishes and verify native exit and cleanup. Restore the original services
in the coordinator finally block.

The three issue records remain open for critical and physical-device acceptance.
Current production image and CI verification, remaining release workflows and
model qualification are pending. The next runtime packet uses Sol High.
