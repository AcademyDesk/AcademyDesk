# PENTA invoice adjustment review and recovery — 12 November 2026

Desktop base `def038296da5c6b7d54f9d551e090c57daf535b0`. Cloud C38
at `e8385f58b23de757928f99481c33ae3dd582b851` covers the separate
enrollment browser host.

The Testing-only Admin Finance chat offers the adjustment action on a
verified invoice result when the server reports Finance and FinanceControls
availability. The review displays the exact amount, currency, current
balance, projected balance and reason. Authorization and submission are
separate explicit steps. The receipt confirms a pending proposal for
Manual approval. The current invoice balance remains unchanged.

After a submission interruption, Check proposal status reads the same
draft. An authorized draft can be retried explicitly. Reload checks the
same draft with GET and recovers the original receipt. The tab recovery
pointer stores only the academy, login epoch, conversation and request
identifiers. A duplicate refusal links to the Manual Finance Adjustments
register.

## Local verification

| Check | Result |
| --- | --- |
| Locked Next.js build, TypeScript, static export and targeted lint | Passed |
| API and SQL harness build | Passed with zero warnings and errors |
| Role entry | 93 passed |
| Consumers | 150 protected HTTP/SQL checks passed. Run `4979f4c8cf7640d8857b12e68b682490`. |
| Live browser | 24 passed: six each for Finance chat, payment, invoice issuance and adjustment proposals. |
| Production guard | 15 guard tests passed. The current register still blocks on five P0 issues and the separate session gate. |

The browser run used a 390 px viewport. The initial submission was
interrupted before dispatch, then the same authorized request was retried.
The SQL transaction committed and the retry response was interrupted.
Reload sent GET and recovered the same pending proposal receipt.
A second request with a different reason still matched the pending
proposal. The Manual register showed the original row.

The owned SQL fixture confirmed one USD 125.01 pending scholarship
proposal and one submission audit. The invoice remained PartiallyPaid with
USD 200.00 in applied reductions and one reconciled payment. The runner
removed its owned SQL resources, restored both original PENTA services,
verified matching Docker inventory and checked HTTP 200 from the restored
API.

The existing Manual register rounds adjustment display to whole units.
The Manual approval queue also falls back to INR for adjustment display.
Correct both displays to use the exact amount and stored currency before
finance acceptance. Production still has six hard gates.
