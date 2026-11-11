# Protected PENTA invoice issuance — 11 November 2026

Desktop base `f708879fc09a40a540c5d6f0fd5b3e814507bed4`. Cloud C36
`1e15133` covers scheduling lookups and read readiness.

The Testing-only invoice API uses the Manual invoice calculation for an
active fee plan or a one-time amount. The host supplies the currency.
The PENTA draft requires an active same-academy student and an explicit
due date. Both invoice paths refuse amounts that require rounding beyond
two decimal places or exceed SQL's monetary range.

The shared finance workflow verifies the current tenant, role, Finance
module and security stamp. Approval binds the student, fee plan terms,
currency, amount and dates. The host rechecks those terms under the
transaction before issuance. The invoice, audit and recoverable receipt
commit together. The receipt read checks the invoice and execution audit.

PENTA blocks an exact match on student, fee plan, due date, amount and
currency against existing non-cancelled invoices. The check also runs at
execution, so a Manual invoice already committed after review prevents
the PENTA duplicate. Independently approved PENTA drafts serialize under
the academy lock. Intentional repeat charges can be reviewed in Manual.

## Local verification

| Check | Result |
| --- | --- |
| API and SQL harness build | Passed |
| Focused invoice and payment API tests | 207 passed |
| Consumers | 24 new invoice checks, 105 retained protected finance checks and six retained finance-consumer stages passed. Run `989341b00ba64f32a4b6f4f582074dbe`. |
| Duplicate races | Five independently approved PENTA pairs each committed one invoice and refused the matching draft. The simultaneous retry of a single draft returned one committed receipt. |
| Browser regression | All 12 retained Finance chat and payment browser checks passed against the new API. SQL confirmed the payment recovery scenario recorded exactly one payment and one audit. |
| Release guard | All 15 unit checks passed. The actual guard blocks release on five P0 issues and the session-security gate. |

The invoice tests covered fee-plan and one-time terms, foreign and inactive
sources, stale approval, purpose and actor isolation, approval and execution
audit rollback, expiry and receipt recovery. The first SQL window
`1936ead913b14c9f932f5478a811554f` hit the per-admin daily draft limit
because the payment and invoice suites shared a synthetic actor. A dedicated
invoice fixture admin let both suites exercise the existing limit independently.

Window `0e7ecb7c396e49f3a1f3517b9c499f7b` started with 4.24 GiB available.
The final run removed its disposable SQL resources, restored both original
PENTA services, preserved Docker inventory and verified HTTP 200 from the
restored API.

Admin browser review, confirmation and lost-response recovery for invoice
issuance are the next checkpoint. Physical-device testing and production
acceptance remain open.
