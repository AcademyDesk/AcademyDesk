# PENTA expense and payroll reads — 10 November 2026

Desktop base `241810e488fcd7a1951dd0b341ae1c63a75b77fa`. Cloud C32
`28be47a` covers enrollment lifecycle and C33 `f365ad2` claims enrollment
options. Both are separate from this finance packet.

Both GET routes under `/api/academies/{academyId}/penta/finance` use the
existing Testing-only PENTA finance flags. They require a current,
same-academy Owner or AcademyAdmin and the Finance module. The linked
transaction rechecks the actor's security stamp and role. Each result is
no-store and is released after its audit commits.

| Route | Preview |
| --- | --- |
| `expenses` | Expense ID, recorded amount and currency, branch ID, date and status. The description and free-form category stay in Manual. |
| `payroll` | Payout ID, payslip number, recorded gross, deductions and net, currency, status, sessions, nullable paid time and snapshot provenance. Names, worker and profile identifiers, free-form period labels, payment methods and references stay in Manual. |

Both use the same tenant scope and ordering as their Manual registers. They
return up to 20 records, ordered by date descending and then ID. The
21st row determines `hasMore`. Amounts include decimal strings for exact
display, including values beyond JavaScript's safe integer range in cents.
Each record keeps its own currency. Historical no-cash statements retain
their status and nullable paid time. Partial or missing identity snapshots
remain `LegacyUnverified`.

## Verified locally

| Check | Result |
| --- | --- |
| API and SQL harness | Built with zero warnings and errors |
| Focused API tests | 141 passed |
| Consumers | 17 new expense and payroll checks, 38 retained protected finance checks and six retained finance-consumer stages passed. Run `7b1b9d1cb44c4b1b82c2fb604a445077`. |
| ExpenseBranchAcceptance | 17 Manual HTTP and SQL checks passed. Run `9f61e33f628c4377aecc31472b29806d`. |
| Payroll | Monthly and session-block payout regressions passed. Run `3bacb02579ab42dbb4099fa12b5daeb9`. |
| Production gate unit tests | 15 passed. The actual guard still blocks release on five P0 records and the separate session security gate. |

The new checks covered real bearer identities and disposable SQL, including
owner and admin reads, default-off behavior, role and tenant denial, module
requirements, bounded Manual parity, exact large amounts and cents, empty
registers, redaction, audit rollback and mid-request role revocation.

Initial window `c85631029b004d9c95d3b5aedd17f318` stopped on a synthetic
payment method exceeding the 30-character column limit. Shortening the
fixture fixed the setup. The final window
`3decc10118654cfca0cdeb04dee3b686` started with 3.61 GiB available against
the 3 GiB guard. The wrapper checked owned SQL cleanup and restored the
two original PENTA services with HTTP 200 and preserved Docker inventory.

The scope is Testing-only backend reads. The next packet is protected
conversation read wiring and host-rendered results. Browser acceptance,
finance writes, model qualification and release acceptance remain open.
