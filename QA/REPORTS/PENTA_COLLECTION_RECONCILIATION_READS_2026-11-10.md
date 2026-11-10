# PENTA collections and reconciliation reads — 10 November 2026

Base `4f458f9011fdfa32426a733fd1b4055bcfabe2e8`. Cloud C31 at
`cc1bae0970f9197e08f8bbd5aa46390e120b5282` covers a separate maintenance
notice and client recovery packet.

The Testing-only finance host adds two GET routes under
`/api/academies/{academyId}/penta/finance`. Both require the existing PENTA
finance flags and current same-tenant Owner or AcademyAdmin authority.

| Route | Verified read boundary |
| --- | --- |
| `collections` | Shares Manual's overdue queue and requires Finance and FinanceControls. It returns the first 20 eligible invoices ordered by due date and ID. Paid, cancelled, due-today and zero-balance invoices are excluded. Amounts remain per invoice and currency. |
| `invoices/{invoiceId}/reconciliation` | Requires Finance and an owned invoice. It returns the first 20 payments ordered by payment time and ID, with status, amount, currency, reconciliation time and whether the payment counts toward the invoice balance. Free-form method and payment references are omitted. |

The SQL queries request one extra record to determine `HasMore`. The
invoice totals still include all eligible Completed and Reconciled payments
in the invoice currency. A shared projection applies that rule to Manual
invoice, collections and protected readback. The collections predicate
compares the paid amount with the adjusted total before rounding could
discard the one-cent difference. No cross-currency scalar is returned.

Both responses are no-store and require the actor audit to commit in the
same linked SQL transaction as the read. The host rechecks the current
security stamp, tenant, role and modules. The reads leave invoices, payments,
collection follow-ups and notices unchanged.

## Verified locally

| Check | Result |
| --- | --- |
| API and SQL harness | Built with zero warnings or errors |
| Focused API tests | 82 passed |
| Consumers | 17 new collections and reconciliation checks, 21 retained protected reads and six retained finance-consumer stages passed, run `5e88c8ded10e406ca517814db93f5a21` |
| Collections | 30 retained Manual queue, follow-up and access cases passed, run `bf07172521e14f2782513ce4f25c3c1b` |
| LegacyCurrencyWrites | 11 retained HTTP and SQL checks passed, run `60fa0c8e82c247828f0f1db377e1b461` |

Native checks used real bearer identities and disposable SQL. They verified
Manual parity, the one-cent overdue case, the two row caps, earlier
eligible payments outside the preview, empty history, different-currency
and voided exclusions, reference redaction, tenant and module denial,
audit failure, mid-request role revocation and a committed Manual payment
and reconciliation.

Window `62847ba2713e4e90b536cb3662f965cc` started with 4.20 GiB available
against the 3 GiB guard. It removed the disposable SQL resources and
restored both original PENTA services. Docker inventory was preserved and
the restored API returned HTTP 200.

This checkpoint covers backend reads in Testing. Planner and browser wiring,
model qualification, finance write approvals and release gates remain open.
Next is protected expense and payroll read detail.
