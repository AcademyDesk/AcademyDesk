# Protected PENTA invoice adjustment proposals — 11 November 2026

Desktop base `1d23c77b5ea5e344320041d6c7897bac45bdc0e5`. Cloud C37
at `4c4382ff1cd5fc08e7ccd1c53af7146f29291fb2` covers the separate
enrollment assistant and preview.

The Testing-only API accepts a typed invoice reference, reduction type,
exact amount and reason. The host requires a current Academy Owner or
Admin with Finance and FinanceControls. It resolves the invoice within
the current academy and calculates the projected balance from applied
reductions and same-currency Completed and Reconciled receipts.

Review, authorization to submit and submission are explicit steps.
Submission rechecks the exact draft under the invoice transaction.
A change to the reviewed ledger requires a fresh draft. Equivalent
pending proposals are rejected even when the reason differs. One
successful submission saves a `PendingApproval` record, an audit and a
recoverable receipt together. Manual Finance Adjustments retains the
separate approval that applies the amount. New refunds and credit notes
still require their dedicated workflows.

PENTA accepts reasons up to 500 characters. Manual proposal creation
now reports the existing 2,000-character storage limit as a validation
error.

## Local verification

| Check | Result |
| --- | --- |
| API and SQL harness builds | Passed with zero warnings and errors |
| Focused API regression | 154 passed |
| Consumers | 150 protected HTTP/SQL checks including 19 proposal checks and five duplicate races. Run `eb429efee35943aa806b2ae647b0a89a`. |
| Manual Adjustment | Retained adjusted-balance, cent precision and pending/rejected proposal checks passed. Run `81f954eb12374ce6ba8009446e49e071`. |
| FinanceIntegrity | 12 integrity cases and 28 additional reconciliation cases passed. Run `156be1ab3326495a83c92212fa3ed8c6`. |
| Release guard | 15 unit checks passed. The actual guard reports six open hard gates. |

The guarded window began with 4.11 GiB available. All three modules
removed their owned SQL resources. Both original PENTA services were
restored, Docker inventory matched and the restored API returned HTTP 200.

Next is Admin adjustment review, submission and receipt recovery.
The five P0 records and the separate session-revocation gate remain open.
Production also needs linked critical browser/device acceptance,
exact-release CI and image/security evidence, and staging recovery.
Private model qualification and the 90% eligible conversational outcome
target remain open.
