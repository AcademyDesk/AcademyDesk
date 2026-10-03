# Phase 2A — PAYROLL-RULE-001 / BUG-DATA-0003

Date: 2026-09-30. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c` plus recorded working-tree changes. Local synthetic HTTP/SQL evidence, not a product repair, production observation or actual bank disbursement.

## Fresh runs and isolation

| Run | UTC start | Loopback SQL port | Harness duration including readiness | Result |
| --- | --- | --- | --- | --- |
| `9bd872ac17394b73be8ae8c87031376f` | 2026-09-30 11:46:52 | 53028 | 72.4 seconds | PAYROLL-RULE-001 FAIL; infrastructure exit 0 |
| `8c9dc8f6ce4045d6953dd769c69cd352` | 2026-09-30 11:48:16 | 51645 | 77.3 seconds | PAYROLL-RULE-001 FAIL; infrastructure exit 0 |

Both runs used new exact-labelled Docker SQL Server 2022 containers, marked run-owned databases and restricted runtime credentials. Each applied 79 application and 7 Identity migrations. The real TestServer retained authentication, authorization, subscription and audit filters. Health/login/anonymous-denial and tenant A/B controls passed; the three previously confirmed P0 reproductions still emitted FAIL. Blob access was disabled. No customer/dev/Azure database or real payroll/payment service was contacted.

Synthetic AcademyAdmin A created an active Monthly Teacher payroll profile through `POST /api/academies/{academyId}/payroll/profiles` (201), linked to the same-tenant synthetic teacher. A fresh SQL context confirmed monthly gross 1000, effective date 2026-09-01, no session-block values and no pre-existing payouts. Optional payment method/reference/session/gross-override fields were omitted from payout requests. Stored defaults were BankTransfer, null reference, null sessions and INR; this is not exhaustive optional-field testing.

The same profile is used for eight uniquely labelled synthetic periods. Before and after every request, a fresh no-tracking context captures the profile and all its payout rows. Rejected requests must leave that complete captured financial snapshot unchanged. Accepted requests must add exactly one row, preserve all earlier rows and the profile, and agree in POST response, SQL and the payout-list GET. Creation requests are spaced by 1.1 seconds to avoid unrelated collisions in seconds-plus-random payslip numbering.

Source hashes, identical across both runs: `Program.cs` SHA-256 `17ABB35AB9D8A5D1B4D92F0D135A09773160BDDEDCD8B28A9114F736BDA01FFA`; `PayrollReproduction.cs` SHA-256 `FB8C9F06A6A32DE535009B7687A5201EAE77455D2D30D2A23280C04D703A9B80`.

## Results — identical in both runs

Gross is independently fixed at 1000; cent edges use the model's decimal(18,2) scale. No higher-precision rounding policy is invented.

| Deductions | Expected | Observed HTTP / fresh SQL / API list | Verdict |
| --- | --- | --- | --- |
| 0 | Net 1000 | 201, Paid payout net 1000; rows 0→1 | PASS control |
| 999 | Net 1 | 201, Paid payout net 1; rows 1→2 | PASS control |
| 999.99 | Net 0.01 | 201, Paid payout net 0.01; rows 2→3 | PASS cent-edge control |
| 1000 | Zero-net acceptance policy must be confirmed | 201, Paid payout net 0; rows 3→4 | **POLICY-PENDING**, not business approval |
| 1000.01 | Reject; financial snapshot unchanged | **201, Paid payout net −0.01 persisted; rows 4→5; negative net also returned by GET** | **FAIL** |
| 1001 | Reject; financial snapshot unchanged | **201, Paid payout net −1 persisted; rows 5→6; negative net also returned by GET** | **FAIL** |
| −0.01 | Reject; no payout | 400, rows 6→6, captured profile/ledger unchanged | PASS negative-input control |
| −1 | Reject; no payout | 400, rows 6→6, captured profile/ledger unchanged | PASS negative-input control |

Each run therefore has five passing controls, two failing upper-bound cases and one policy-pending zero-net observation. The test verifies actual accepted persistence rather than treating a defect-triggering 201 as a fixture error. Exit 0 means reproduction and safe cleanup completed, not a financial PASS. No-change assertions cover profile/payout data, not every possible audit table.

## Original sanitized failing output

Both runs emitted:

```text
PAYROLL-RULE-001 FAIL: gross=1000; negative Paid payouts persisted=2; rejected invalid controls=2; zero-net POLICY-PENDING observed=HTTP 201/one Paid payout/net 0; deductions above gross must be rejected without a payout; no debt workflow assumed.
```

Fresh SQL payout identifiers for the two failing boundaries:

| Run | Deductions 1000.01 / net −0.01 / Paid | Deductions 1001 / net −1 / Paid |
| --- | --- | --- |
| `9bd872ac17394b73be8ae8c87031376f` | `634919ff-1a18-43e6-b355-685fa4a7694a` | `8b84c5f4-c571-4520-ba12-d06352fbc97b` |
| `8c9dc8f6ce4045d6953dd769c69cd352` | `c82712b3-5fd7-4ac1-8ee6-12b54554c9f1` | `a0f1e89a-1866-46fd-add5-49dedf4b858f` |

Both runs refused a deliberately mismatched cleanup marker before the callback. Matching marker cleanup removed their owned database, runtime login and host folders. Exact run-labelled Docker containers were stopped and removed; only cached images remain. Their synthetic payroll data cannot be recovered from the removed containers.

## Decision, policy and limits

**BUG-DATA-0003: RUNTIME-REPRODUCED / OPEN P0.** The payout handler rejects negative deductions but has no deductions-above-gross guard and stores gross minus deductions as NetAmount. The original static finding is confirmed at both one-rupee and one-cent upper boundaries.

Zero-net payout policy remains **PENDING**: the existing app accepts it, but that is not a requirement decision. Phase 2B must confirm whether a zero-net entry should be allowed and how it should be labelled; no debt/recovery workflow is assumed or implemented here. This policy uncertainty does not invalidate the demonstrated negative-net payouts.

No product payroll controller, existing student UI change, schema, Azure settings/data, commit or deployment was modified. Harness build: zero warnings/errors. After container cleanup, the API unit suite passed **45/45**, zero failures/skips. Payroll JSON/matrix/issue/checkpoint consistency and source whitespace checks passed; `git diff --check` found no whitespace errors. No run-labelled QA SQL containers remain. The historical Phase 1 fingerprint validator was not weakened or claimed to pass on this expanded Phase 2 tree. Staff/SessionBlock calculations, excess decimal scale, extreme magnitude, duplicate/concurrent payouts, inactive profiles, payroll permissions and browser behaviour remain broader regression coverage, not implicitly passed by this Monthly Teacher baseline.

Phase 2A is now **4/5 P0 baseline reproductions completed twice**, all failing and still OPEN. Next: FINANCE-RULE-003 payment status transitions, then consolidated handoff evidence. Phase 2B repairs and regressions follow that handoff, one confirmed issue at a time.
