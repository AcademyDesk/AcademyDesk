# C9 live invoice, payroll and reconciliation recovery — 7 November 2026

The local branch started at C10-integrated source `5f4fdc7` and published
claim `637178e`. Cloud subsequently published C11 claim `28ad7b9` for access
display, platform status and sales navigation. Those paths are disjoint from
this local finance and SQL scope. The prior C9 Payments receipt and Cloud's
controlled frontend evidence were reused.

The current Next.js export was built for the owned loopback API using the
unchanged lockfile and copied pinned dependencies. The browser driver checked
658 exported file hashes. Build, TypeScript and static postbuild passed. The
SQL harness built with zero warnings or errors.

Disposable SQL run `131ddadfee374774b3004858650990e8` passed all eight
browser/SQL phases. The signed-in Admin made one real write in each case. The
driver withheld its response after the API accepted it, then checked the
unconfirmed notice, draft retention, resubmission lock, and fresh readback on
reload. Each saved state and its single actor/tenant/route audit were verified
against fresh SQL snapshots. Each reload left SQL unchanged.

| Action | API status | Verified persisted outcome |
| --- | --- | --- |
| Issue invoice | 201 | One INR 123.45 invoice, balance 123.45, correct learner and December 15 due date |
| Create payroll profile | 201 | One active monthly profile at INR 1,000.25 for the selected teacher and effective date |
| Record payout | 201 | One paid statement for the new profile, with INR 100.10 deductions, INR 900.15 net, and the stored teacher and payslip identity |
| Reconcile payment | 200 | The existing INR 600 payment carries the bank evidence and reconciliation timestamp, with no extra payment row or change to its invoice |

The browser recorded 62 API responses, with zero rate limits and zero runtime
errors. Real Identity, anonymous denial and cross-tenant GET/POST denial passed.
The harness applied 85 application and seven Identity migrations. The owned
database, login and container were removed. Both original PENTA services,
Docker inventory, API health and the validation lease were restored.

Two preliminary browser attempts remain in local evidence. The first could
not locate the invoice field by exact label and stopped before a write. The
second passed seven phases and stopped because the reconciliation evidence
element also contained its timestamp. The final selectors target the actual
form control and evidence element. The reruns reused the verified export and
compiled harness. Application source and the existing 1,991 API test baseline
are unchanged.

To reproduce, use the pinned export at `http://127.0.0.1:54998` with API
`http://127.0.0.1:54999`, and the existing bounded SQL validation lease. Build
the harness and run `Run-ReconciledPayment.ps1 -Module FinanceRecoveryBrowser
-HarnessPath <dll>`. Run `finance-recovery-live-browser.cjs` with its
`QA_FINANCE_*` variables and the owned fixture/manifest and browser paths.
Signal the exact run's stop marker and verify cleanup.

This covers committed lost-response recovery for the four actions. Original
critical, device and production gates remain open. Next local packet is C10
linked role, API and SQL acceptance. Recheck Cloud ownership first.
