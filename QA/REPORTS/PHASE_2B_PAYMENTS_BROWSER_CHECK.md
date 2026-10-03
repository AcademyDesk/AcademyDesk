# Phase 2B — Payments browser and SQL check (2026-10-03)

## Scope and isolation

Isolated worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. No main, production database, Azure, or GitHub write. The new `BrowserPayments` harness creates one run-labelled SQL Server container, database and runtime login, exposes a loopback-only authenticated API, and uses a source-only Next.js copy at `127.0.0.1:49301`. Both runs removed their owned database/login/container; the frontend was stopped. The second run, `19cb3a215ef248ca9a8c84a6604f03a6`, exited 0 in 200 seconds after the SQL ledger assertions. Initial fixture run `3a432b02f628489ab2a4312005c552bf` also exited 0; its final SQL line was filtered by the PowerShell runner, so the second run adds an enforcing assertion.

The synthetic Academy Admin signed in through `/login`, then visited `/payments` directly. No tokens or real credentials were recorded. Two invoices were seeded through real HTTP: gross 1000 with Reconciled 600; and gross 1000 with an approved 200 discount plus Reconciled 600. Tenant-isolation preflight passed (same-tenant access and cross-tenant GET/POST 403 with no foreign write).

## Observed browser result

| Check | Result |
| --- | --- |
| Initial `/payments` and reload | PASS: 2 Reconciled payment rows, collected ₹1,200, balances ₹400 and ₹200, outstanding ₹600. Direct URL and reload retained the values. |
| Approved adjustment | PASS: second invoice displayed `Balance ₹200.00 of ₹800.00 due after ₹200.00 adjustment`, not the misleading gross ₹1,000 due. |
| Record exact 400 | PASS: form prefilled 400; the page showed success, 3 rows, collected ₹1,600, plain invoice Paid/balance 0, outstanding ₹200. |
| Stale selection/duplicate prevention | PASS: while the browser held the adjusted 200 selection, a second synthetic client paid that 200. Browser submit received `Payment exceeds the invoice balance.`; form selection/amount cleared, totals refreshed to 4 rows/₹1,800 collected/outstanding 0. No fifth row. |
| Excess direct API control | PASS in the initial run: 500 after settlement rejected 400 with the existing balance message. |
| Final disposable SQL invariant | PASS in the second run: exactly two invoices; plain status Paid, adjustment 0, exactly two rows, collected 1000; adjusted status Paid, adjustment 200, exactly two rows, collected 800. The harness throws if any invariant differs. |
| Mobile emulator | PASS at 390×844: KPIs and form stack vertically; body/document scrollWidth 379 versus innerWidth 390; no horizontal overflow. Browser warning/error log was empty. Physical Android/iOS was not tested. |

Product UI changes: submit is guarded while saving; network/unknown result warns the operator to refresh before retrying; server rejection is shown through `role=alert` and refreshed balances; success is shown through `role=status`; approved adjustment is described in the invoice directory. TypeScript validation passed. Targeted ESLint had 0 errors and one pre-existing `useEffect` dependency warning.

Evidence kept local/untracked: `QA/EVIDENCE/payments-browser-20261003/mobile-payments.png`, `desktop-payments.png`, and `rejected-stale-payment.png`. The screenshots contain only synthetic fixtures. The final SQL line was filtered from this run's shell output by the runner version already executing; the exit-0 assertion is the exact SQL evidence. The runner now includes `PAYPORTAL` for subsequent runs.

## Limits and next gate

BUG-DATA-0001 and BUG-DATA-0002 remain OPEN. This is one signed-in desktop browser and one emulated mobile viewport, not physical device, complete cross-role/browser matrix, all fault injection, or full release regression. Approval-versus-collection concurrency remains open for BUG-DATA-0002; reconcile-versus-void and Voided restoration policy remain open for BUG-DATA-0010. No merge, push, Azure deployment, or production conclusion.
