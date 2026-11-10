# PENTA deterministic finance reads — 10 November 2026

Base `0758493c3bee2f07dd1b49220bed2fe46be3e46d`. Cloud C31 at
`3a33e5627ce847328f47c0091170c073bf65ce2d` claims the maintenance notice and
client recovery, a separate packet.

The Testing-only host adds two GET reads under
`/api/academies/{academyId}/penta/finance` when `Penta:Enabled` and
`Penta:FinanceReadEnabled` are enabled.

| Read | Authoritative source |
| --- | --- |
| `overview` | The same Manual Finance and Dashboard summary projection. Gross, approved adjustments, collected, outstanding and reconciled amounts stay in separate currency groups. An empty academy returns an empty array. |
| `invoices/{invoiceId}` | The same invoice projection as Manual. Only owned Completed and Reconciled payments in the invoice currency reduce its balance. |

No currency conversion or flat mixed-currency total is calculated.
Legacy receipts in another currency keep their own overview group and
do not reduce the referenced invoice balance. Cancelled invoices follow
the existing Manual summary rules. The existing learner fee lookup keeps
its stricter ledger validation.

The host requires a current same-tenant Owner or AcademyAdmin and the
Finance module. The overview also requires FinanceControls. Both reads
recheck the current security stamp and role in the linked SQL transaction,
return no-store responses and withhold results if their actor audit cannot
commit. Audit metadata omits balances and document content. The reads do
not update business records. Delegated FinanceUser retains Manual access.

## Verified locally

| Check | Result |
| --- | --- |
| API and SQL harness | Built with zero warnings or errors |
| Focused API | 74 finance consumer, invoice readback, finance summary and dashboard tests passed |
| Consumers | 21 new protected-read checks and six retained finance-consumer stages passed, run `b57f20a2cc694f5c9180c790eefc76d1` |
| LegacyCurrencyWrites | 11 retained HTTP and SQL cases passed, run `17cf4665cfed4f5cbfe8ca9461c7262b` |

Disposable SQL used real bearer identities. The new checks verified mixed
INR, USD and EUR parity, exact normalized currency matching, no scalar
total, no record mutation, cross-tenant and role denial, current module
gates, audit failure, a newly committed Manual payment and a stamp change
between the initial policy check and the locked read.

The first window `52be72bccb664090a0f61e186e2c72a2` stopped before SQL
on ambiguous test JSON serializer calls. The retest specified the
serializer options and applied the tenant/invoice filter before projection.
Final window `c1ca340a8e3541269bbf995371d6b59b` started with 4.08 GiB
available against the 3 GiB guard. It removed the disposable SQL resources
and restored both original PENTA services. Docker inventory was preserved
and the restored API returned HTTP 200.

The new routes are backend-only and Testing-only. Planner and browser
wiring, finance writes, Mini qualification and release gates remain open.
Next is collection and reconciliation read detail.
