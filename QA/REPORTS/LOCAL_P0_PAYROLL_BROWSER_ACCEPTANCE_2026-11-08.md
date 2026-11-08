# Live payroll boundary and payslip acceptance — 8 November 2026

The real Academy Admin browser used two active monthly profiles for one
synthetic teacher. Both runs exercised the same four phases against disposable
SQL. Baseline `51d01aa4cd4d4ce5bc57d963f9758776` passed the SQL checks but failed
the duplicate-message assertion. The API returned 409 and correctly explained
the duplicate. The page instead asked the operator to complete pay-cycle details.

| Phase | Verified outcome |
| --- | --- |
| Zero net | At 390px, deductions equal to gross triggered the form guard with no POST. A separately submitted authenticated API request returned 400. SQL showed no payout or audit. |
| One-cent net | Gross INR 1,000.25 less INR 1,000.24 produced exactly one Paid INR 0.01 payout, one attributed audit and one saved worker identity snapshot. The page refreshed and displayed the amount. |
| Saved payslip | At 1440px, the print action fetched the selected persisted payout. At the print boundary its DOM contained the saved payslip number, worker, pay period, reference, gross, deductions and net. SQL stayed unchanged. |
| Duplicate | The alternate active profile for the same worker submitted a differently cased and spaced version of the same pay period. The API rejected it with 409. The page displayed the duplicate explanation and retained the input. SQL still contained one payout and one audit. |

The payout form now has a specific message for the duplicate conflict. The
existing handling of definitive validation failures and uncertain write
outcomes is retained. The backend payroll rule and stored data are unchanged.

Final SQL run `88da2157c94644b18d7ed1ad527c7626` passed all four live phases.
Edge recorded 18 API responses, no rate limits and no runtime errors.
The exact-lock Next.js 16.4.3 build and static postbuild passed. The browser
verified 658 artifact files and 42 targeted finance and payslip tests passed.
The window removed its owned SQL resources and restored the original Docker
inventory and both PENTA services.

The browser used a print probe to inspect the rendered document at the moment
of the print call. Native print dialog/PDF output and physical-device and
linked critical acceptance stay open. Keep BUG-DATA-0003 OPEN.
