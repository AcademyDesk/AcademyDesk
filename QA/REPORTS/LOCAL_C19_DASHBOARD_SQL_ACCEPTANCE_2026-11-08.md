# C19 academy Dashboard SQL acceptance — 8 November 2026

Integrated Cloud `33e47b2` at `81b1c65`. The ten scoped source, test and handoff
files match Cloud. The issue record also retains Desktop's C17/C18 SQL evidence.
The complete web tree matches the Cloud-tested C19 source. All 27 targeted
Dashboard and session-recovery frontend checks passed locally. An exact-lock
Next.js 16.4.3 webpack export and its static postbuild passed for the local
browser window. [Cloud C19 evidence](../HANDOFF/C19_DASHBOARD_BALANCES_2026-11-08.md)

Disposable SQL run `ab28139bd68140d19e915ebe7a01baaa` passed **34/34** real HTTP
and SQL checks, including the 28 retained currency/support checks and six new
Dashboard checks. The combined API suite passed **2,161/2,161** with zero skips.

- Empty, single USD and mixed INR/USD Dashboard amounts match the authoritative
  Finance grouping and flat-field rules. The mixed case includes exact cents,
  applied and pending adjustments, an overpaid invoice, a cancelled invoice
  and a legacy payment in a different currency.
- A real INR payroll payout did not change the outstanding fee receivable.
- The academy-local day included the scheduled midnight boundary, excluded the
  cancelled session and excluded the next day's boundary. The empty-money case
  preserved the existing active student, teacher, course and batch counts.
- Foreign Academy Admin and Teacher access were denied. Fresh snapshots around
  reads and refusals verified the captured ledger, payroll, support, schedule,
  Identity and audit rows stayed unchanged.

The window verified 85 application and seven Identity migrations, removed its
owned database, login and container, and restored both original PENTA services
and the Docker inventory. The guarded runner corrected a lease label before
creating any fixture. Logs, hashes and the API TRX remain local.

C19's real browser modal comparison, broader currency and critical acceptance,
and five P0 records remain open. Cloud C20 owns invoice-list and Reports
currency follow-up. Keep BUG-DATA-0016 open.
