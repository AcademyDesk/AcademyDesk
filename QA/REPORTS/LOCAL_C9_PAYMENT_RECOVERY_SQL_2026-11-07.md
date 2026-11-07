# C9 live payment recovery — 7 November 2026

Verified C9 application source `d3ec9df` with claim `ee16e37`. The archived
frontend was rebuilt for the owned loopback API from the unchanged pinned lock
and copied dependencies. The browser driver verified the 658 exported file
hashes. Next.js build and static export passed. The SQL harness built with zero
warnings or errors.

Disposable SQL run `eadf1fa593f047fa914c638b71c897f7` passed all four live
browser and fresh SQL phases with real Identity login. It recorded 32 browser
API responses, zero rate limits and zero runtime errors. The driver interrupted
only the two targeted requests.

| Case | Observed browser and persisted outcome |
| --- | --- |
| Response lost after real HTTP 201 | The retained draft showed an unconfirmed save and locked resubmission. The new Completed payment of 400 plus the original Reconciled 600 stayed at 1,000 across two rows, with one attributed payment audit. |
| Reload after a commit | The browser performed fresh GETs, showed balance zero and removed the settled invoice from the selector. SQL remained unchanged. |
| Request stopped before reaching the API | The draft stayed unconfirmed and resubmission was locked. The original Reconciled row and 400 balance remained, with no new payment or payment audit. |
| Reload after an unsent request | The browser performed fresh GETs and displayed the original 400 balance. SQL remained unchanged and the invoice remained selectable. |

Each context attempted one POST. A second form submit and the recovery reload
triggered no replay. Fresh snapshots also checked the other invoice, existing
reconciled rows, and unrelated finance, teaching and Identity state. The harness
verified real role and tenant isolation, applied 85 application and seven
Identity migrations, and removed only the owned SQL database, login and container.
Both original PENTA services, the Docker inventory, API health and validation
lease were restored.

The test setup retained three diagnostic failures. The archive proof initially
compared Git LF bytes to exported CRLF bytes. The runner initially filtered out
the new host readiness line. The browser's first login matcher omitted the
query string. The corrected run reused the successful frontend and harness
builds. No application code changed. The existing 1,991 API tests and previous
collection race results remain source-bound and were reused.

To reproduce, serve the C9 export on `127.0.0.1:54998` with its compiled API
origin `http://127.0.0.1:54999`. Build the existing harness and use
`Run-ReconciledPayment.ps1 -Module PaymentRecoveryBrowser -HarnessPath <dll>`
under the bounded validation lease. Feed the owned run, fixture path, export
manifest and Playwright/Chromium paths to `payment-recovery-live-browser.cjs`
using its `QA_PAYMENT_*` and `QA_BROWSER_EXECUTABLE` variables. Signal only the
run's own stop marker and verify SQL and service cleanup.

BUG-DATA-0001 remains OPEN for broader critical and physical-device acceptance.
Live C9 invoice, payroll profile, payout and reconciliation recovery are still
separate checks.
