# Phase 2A — FINANCE-RULE-001 / BUG-DATA-0001

Date: 2026-09-30. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c`, with the recorded working-tree changes. This is a synthetic local HTTP/SQL reproduction, not a financial product repair, browser test or Azure finding.

## Execution and isolation

| Fresh run | UTC start | Loopback SQL port | Harness duration, including SQL readiness | Result |
| --- | --- | --- | --- | --- |
| `fe7b35c85d7a40bd8d7a80c2dbf7fbe0` | 2026-09-30 10:57:35 | 54323 | 56.7 seconds | FINANCE-RULE-001 FAIL; infrastructure exit 0 |
| `8f9135e88f744f29b7b687c5b6141c81` | 2026-09-30 10:58:59 | 54035 | 50.2 seconds | FINANCE-RULE-001 FAIL; infrastructure exit 0 |

Each exact run-labelled Docker SQL Server 2022 container used a new run-owned database and restricted runtime login. Image digest: `sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090`. Both runs applied 79 application and 7 Identity migrations, retained real authentication/authorization/audit middleware, and passed health, login, anonymous denial and tenant A/B controls. Blob access was disabled by the isolated host. No development or Azure database, customer account or real payment was used.

The finance fixture extended synthetic academy A's enabled modules with Finance. A real AcademyAdmin login creates the invoice/payment through their actual endpoints. A separately created Student Identity, linked to the active same-tenant student, authenticates and reads `/api/portal/students/{studentId}`. This exercises the shared family invoice calculation through a student login; it does not certify Guardian-specific access flags.

Harness source SHA-256: `Program.cs` = `7145D24F39A604F3F19DCB5731B9C1A8AE2882E824F4E60C89398979F694E782`; `FinanceReproduction.cs` = `770D7842B1EA32CD35B0DD332CFAE0F9C6D6244C97E2321536A546F5C2F56DF7`. Neither source changed between the two runs. Phase 1's pinned source fingerprint remains historical; it is not claimed to match the expanded Phase 2 working tree.

## Observed sequence — same result in both runs

| Actual request / transition | HTTP | Fresh SQL / independent read evidence |
| --- | --- | --- |
| POST invoices, studentId + amount 1000 | 201 | Invoice 1000, adjustment 0, Issued, no payments |
| POST payments, invoiceId + amount 600 | 201 | Exactly one Completed payment 600, invoice PartiallyPaid |
| Admin GET invoices; Student GET portal detail | 200 / 200 | Admin paid 600 / balance 400; Student balance 400 |
| Attempt extra payment 500 before reconciliation | 400 | Invoice/payment snapshot unchanged; no new ledger row |
| PATCH payment/reconcile with blank reference | 400 | Snapshot unchanged, status Completed, no reconciliation timestamp/reference |
| PATCH payment/reconcile with `QA-RECONCILE` | 200 | Same payment ID and amount 600; Reconciled, timestamp set, exact reference persisted; invoice PartiallyPaid |
| Admin and Student invoice reads after reconciliation | 200 / 200 | Admin paid 600 / balance 400; **Student balance 1000** |
| Attempt extra payment 500 after reconciliation | **201** | **Second Completed payment 500 persisted; total ledger 1100 against invoice 1000**, original 600 remains Reconciled |

Every transition above has a fresh, no-tracking SQL snapshot. Rejected controls compare the complete captured invoice/payment snapshot, not just row counts. The expected arithmetic is independently fixed: 1000 minus collected 600 = 400; adding 500 exceeds 400 and must leave the ledger unchanged. Defect observations are reported as FAIL rather than converted into passing assertions. Exit 0 means the reproduction and safe cleanup completed, not that finance passed.

## Original sanitized failing output

The following line was emitted in both fresh runs:

```text
FINANCE-RULE-001 FAIL: invoice create=201/1000; payment=201/600; pre-reconcile admin paid=600/balance=400/student balance=400; excess 500=400/no SQL change; blank reference=400/no SQL change; reconcile=200; post-reconcile admin paid=600.00/balance=400.00/student balance=1000.00; extra 500 HTTP=201/SQL rows=2/ledger=1100.00; expected paid=600/balance=400/reject 500 with unchanged ledger.
```

Persisted rows at the failing final transition:

| Run | Original payment | Reconciled UTC timestamp | Excess payment | Invoice state |
| --- | --- | --- | --- | --- |
| `fe7b35c85d7a40bd8d7a80c2dbf7fbe0` | `f624c34d-c590-4c31-b3b4-b00ed4cd405d`, 600, Reconciled, `QA-RECONCILE` | 2026-09-30 10:58:30.6802531 | `59aa7cf3-0a01-47ce-b6d5-7ab6462a9bfa`, 500, Completed | 1000 / adjustment 0 / PartiallyPaid |
| `8f9135e88f744f29b7b687c5b6141c81` | `a0edfac9-11ad-43ac-afe1-cd709d071c32`, 600, Reconciled, `QA-RECONCILE` | 2026-09-30 10:59:48.6239203 | `584a6130-8c67-4d1f-bc3b-4bf3ce769010`, 500, Completed | 1000 / adjustment 0 / PartiallyPaid |

Both runs also retained SECURITY-FILE-001's failing regression observation. A mismatched cleanup marker was refused; matching markers then authorized removal of the run-owned database, login and host folders. The exact labelled SQL containers were stopped and removed. Only cached images remain; no synthetic financial data was retained in those removed databases.

## Decision and next phase task

**BUG-DATA-0001: RUNTIME-REPRODUCED / OPEN P0.** The source explanation is confirmed: Payment creation and Portal invoice aggregation count only Completed, while reconciliation changes the payment to Reconciled and the admin invoice list counts non-Voided payments.

No product business rule, existing student UI edit, production data, commit or deployment was changed in this slice. Harness build: zero warnings/errors. The API unit suite was rerun after both containers were removed: **45 passed, 0 failed, 0 skipped**. Finance tracking consistency passed across JSON, matrix, issue and phase checkpoint; `git diff --check` reported no whitespace errors. The historical Phase 1 fingerprint validator was not weakened or claimed as passing on this changed working tree. Void/status variants are reserved for FINANCE-RULE-003; approved adjustments are next in FINANCE-RULE-002. Guardian-specific permissions, concurrent collection, browser notices and wider financial regression remain separate required coverage, not implicitly passed here.

Phase 2A now has **2 of 5 P0 baseline reproductions completed twice**, both failing as predicted. Three remain: invoice adjustments, payroll boundaries and payment status transitions. Phase 2B starts after the remaining reproduction evidence and handoff review; it will repair one confirmed issue at a time and rerun its original case plus regression.
