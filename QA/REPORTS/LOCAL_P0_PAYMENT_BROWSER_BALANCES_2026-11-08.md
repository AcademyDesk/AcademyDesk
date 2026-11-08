# Live standard and adjusted payment balances — 8 November 2026

The exact-lock Next.js 16.4.3 webpack export and static postbuild passed for
`81b1c65`. The browser proof verified 658 files. Edge used its native sandbox,
the real login and Payments pages, and a loopback API bridge to disposable SQL.
The new balance-limit cases forwarded all requests to the real API.

| Case | Browser and persisted result |
| --- | --- |
| Lost response after commit | The real 201 was interrupted. Fresh readback showed two rows and zero balance with no replay. |
| Unsent recovery | The request was interrupted before forwarding. Fresh readback showed one reconciled row and INR 400 outstanding. |
| Standard invoice at 1440px | INR 1,000 less INR 600 reconciled gave INR 400. A one-cent excess triggered the browser and form guards and sent no POST. Exactly INR 400 returned 201 and settled the invoice. |
| Adjusted invoice at 320px | INR 1,000 less INR 200 applied adjustment and INR 600 reconciled gave INR 200. A one-cent excess sent no POST. Exactly INR 200 returned 201 and settled the invoice. |

SQL confirmed no captured changes after either overage, one correctly
attributed audit per successful payment, two rows per settled invoice and
zero balance in fresh API and UI readback. The existing reconciled payments,
applied adjustment and unrelated captured records were preserved.

SQL run `1852aa64ec6942008372ac955235a97d` passed all eight browser/SQL phases.
The browser recorded 58 API responses, zero 429s and zero page errors.
The first wrapper attempt pointed at an incorrect script path and stopped
before creating SQL. The corrected retry verified all 85 application and
seven Identity migrations, removed the owned database, login and container,
and restored the original Docker inventory and both PENTA services.

This adds desktop Edge and narrow viewport evidence for the two payment-balance
issues. Physical devices, linked critical cases, other financial and private
media workflow acceptance, release CI/images and staging recovery remain open.
[Release gates](../09_RELEASE_GATES.md)
