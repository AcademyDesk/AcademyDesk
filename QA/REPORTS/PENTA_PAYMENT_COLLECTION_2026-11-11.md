# Protected PENTA payment collection — 11 November 2026

Desktop base `525f5ad365140a47eff61b40e511df9a66e3ec87`. Cloud C34
`cfee08e` covers batch lookup permissions.

The Testing-only collection API records a payment the academy has already
received. The invoice supplies the currency. The draft accepts the invoice
ID, exact decimal amount and one of the six payment methods used by Manual.
Private payment references can be entered in Manual.

Both paths use the same invoice write lock, adjusted total and
same-currency Completed and Reconciled sum. The shared validation also
rejects amounts that exceed the SQL monetary range or require rounding
beyond two decimal places. Existing overpayment responses are preserved.

The host requires a current same-academy Owner or AcademyAdmin and the
Finance module. A server-created conversation is scoped to the actor and
security stamp for 24 hours. The review lasts at most ten minutes and binds
the invoice, currency, method, amount and reviewed balance. Approval and
execution each resolve the current invoice under the same lock. A changed
balance requires a fresh draft, even if it would still cover the amount.

The payment, invoice status, receipt and required audit commit together.
GET checks the committed payment and audit before returning a receipt.
An exact execution retry returns the same receipt. The remaining balance
in the receipt is the balance at recording. The API returns decimal strings
for exact review and receipt rendering.

## Local verification

| Check | Result |
| --- | --- |
| API and SQL harness | Built with zero warnings or errors |
| Focused API tests | 130 passed |
| Consumers | 29 new protected payment checks, 76 retained protected finance checks and six finance-consumer stages passed. Run `c86550b4dd804b0ca86748715aa4e7f4`. |
| Mixed PENTA and Manual races | 5/5 recorded one payment, kept the adjusted collected total at 800 with two payment rows, and returned no HTTP 429 |
| Manual collection race | 5/5 returned one 201 and one 400 with exactly two payment rows and 1000 collected. Run `2501959cc1f44fcb9e7abb7a41cbd977`. |
| Manual adjusted race | 5/5 returned one 201 and one 400 with exactly two payment rows and 800 collected. Run `77d85bd7461045c393d1aeacbe0ffc92`. |
| Sequential reconciliation | Passed the original 600 reconciled, 400 remaining, 500 refused case and retained consumer checks. Run `77f2ffe2a2a54b30a6d69656981be05f`. |
| Release guard | All 15 unit checks passed. The actual guard blocks release on five P0 issues and the session-security gate. |

The protected checks also verified cross-tenant and cross-actor refusal,
current role and subscription, expired approval, replay and concurrent
retry, same-currency balance, and rollback after an approval or execution
audit failure. The first build needed a namespace import. The first SQL
attempt exposed case changes in the synthetic payload serializer. The
corrected run passed all checks.

Window `cdd83c8a2a4348cb905fc0276dbeac19` started with 3.16 GiB available
against the 3 GiB guard. The runner removed its disposable SQL resources,
restored the two original PENTA services, preserved Docker inventory and
verified HTTP 200 from the restored API.

The next checkpoint is Admin browser review, explicit confirmation and
lost-response recovery. Physical-device testing, model qualification and
production acceptance remain open.
