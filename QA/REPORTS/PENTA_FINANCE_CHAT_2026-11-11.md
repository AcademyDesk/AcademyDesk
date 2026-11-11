# Protected PENTA finance chat — 11 November 2026

Desktop base `d5f91d344eb5f9706733907d79cbbe2a52685ff1`. Cloud C33
`6d508ee` covers enrollment lookups. The desktop packet connects the
protected finance reads to the existing Academy Admin PENTA workspace.

The chat recognizes finance overview, overdue invoices, recent expenses
and recent payroll. An overdue row can open the invoice or its
reconciliation. An exact invoice ID can also be entered in the chat.
The host interprets these bounded requests directly and returns the result
only after the protected read audit commits. The browser renders the most
recent request and reply in memory.

The availability check uses the current Academy Owner or AcademyAdmin
and Finance subscription. The host independently enforces FinanceControls
for overview and collections. Each successful read checks tenant, stamp
and role inside the existing read transaction. The chat returns at most
20 rows. Monetary strings preserve cents and carry currency per row.
Payroll names and bank references, expense free text and student IDs stay
out of chat. A new login clears the previous result in other tabs.

The same page exposes the matching Manual register after a successful
read and Manual Finance after a failed read. Unrecognized commands get
examples, missing invoices get a specific message, and a disabled or
unauthorized workspace keeps the existing Manual entry.

## Local verification

| Check | Result |
| --- | --- |
| Locked Next.js frontend build and static export | Passed against the existing lockfile and installed dependencies |
| Targeted lint | Zero errors. One existing ref-cleanup warning in the Manual mode switch. |
| Role entry regression | 93 checks passed |
| API and SQL harness | Built with zero warnings or errors |
| Consumers | 21 new HTTP and SQL checks, 55 retained protected finance checks and six retained finance-consumer stages passed. Run `d905a93162f448e193758d281f263157`. |
| Real browser | Six checks passed against the live API and disposable SQL. The 390 px Admin checks covered read and drill-down, precise payroll, refusal, Manual navigation and cross-tab clearing. A separate Teacher session had no finance composer. |
| Release guard | All 15 unit checks passed. The actual guard blocks release on five P0 issues and the separate session-security gate. |

The first browser attempt matched identical gross and net amounts. The
second expected a table for an invoice with no payments. The final run
checked the exact net cell and the empty reconciliation state. It reused
the verified app artifacts and SQL harness, then removed its disposable
database and container. Both original PENTA services returned healthy and
the restored API responded HTTP 200.

The scope is Testing-only host-interpreted finance reads. Finance writes,
Mini qualification, physical-device verification and production acceptance
remain open. The next desktop packet is protected payment collection using
the Manual transaction, exact approval binding and a recoverable receipt.
