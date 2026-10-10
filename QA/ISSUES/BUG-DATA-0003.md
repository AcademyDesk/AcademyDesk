# BUG-DATA-0003 — Payroll permits deductions greater than gross

| Field | Value |
| --- | --- |
| Status | OPEN / FIX-IN-PROGRESS (local negative-net guard) |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Local negative-net original/module PASS twice; zero-net POLICY-PENDING and broader/critical gates pending |
| Severity | Critical financial integrity |
| Priority | P0 |
| Category | DATA |
| Module | PAYROLL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payroll |
| API | POST /api/academies/{academyId}/payroll/payouts |
| Environment | Two fresh local run-owned Docker SQL/TestServer fixtures; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PAYROLL-RULE-001 |
| Evidence classification | Static trace confirmed by authenticated HTTP, fresh SQL and payout-list reads |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | 2/2 fresh runs, 2026-09-30; eight deduction boundaries per run |
| Source | apps/api/Controllers/PayrollController.cs:49 |
| Class/function | Pay |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/REPORTS/PHASE_2A_PAYROLL_BOUNDARY_REPRO.md; retained Phase 1 source excerpt below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Original Phase 2A FAIL retained; local repair/test/SQL logs linked below |
| API request | Seed active Monthly payroll profile with gross 1000. Request payout with deductions 1001 and valid period. |
| API response | Deductions 1000.01 and 1001 accepted 201 with net −0.01 and −1; payout-list GET returned the same values |
| Database before/after | Two negative-net Paid rows persisted per run; negative deductions rejected 400 with captured profile/ledger unchanged; zero net accepted but policy pending |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted PayrollController upper-deduction guard, 2026-10-01; no deployment |
| Retest result | Two fresh guarded Identity/HTTP/SQL runs reject negative net without captured payroll writes |
| Regression result | 118/118 API tests; each SQL run 14 rule cases PASS + zero-net POLICY-PENDING observation; full critical NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local repair checkpoint — 2026-10-01

2026-10-10 [financial policy bundle FP2](../REPORTS/PHASE_2B_FINANCIAL_POLICY_BUNDLE.md): proposed valid zero-net earnings/deductions statement with NoCashDue rather than Paid/BankTransfer; owner approval, lawful deduction review and compatible statement/settlement schema/DTO/legacy treatment required. Current negative-net guard preserved; current zero-net behavior not changed or accepted. Remain OPEN; no new runtime evidence in this packet.

[Repair and evidence](../REPORTS/PHASE_2B_PAYROLL_NET_REPAIR.md): deductions above effective gross rejected before payout save. Original Monthly boundaries and SessionBlock default/override/cent/error controls pass twice on fresh SQL; 118 API tests pass. Zero-net currently still produces a Paid/net-0 row, observed POLICY-PENDING rather than approved behavior. Remain OPEN pending policy/broader/critical acceptance. Original failure evidence/source excerpt below retained historically; no historical customer payroll rows corrected or real bank payout performed.

## Original reproduction (before repair)

Seed active Monthly payroll profile with gross 1000. Request payout with deductions 1001 and valid period.

## Expected

Reject deductions above gross unless an explicitly approved debt workflow exists; no negative disbursement.

## Actual / evidence

Checks only deductions < 0 and gross > 0, then persists gross minus deductions. No upper deduction guard is present.

Two fresh synthetic real-HTTP/SQL runs confirmed the finding: on Monthly gross 1000, deductions 1000.01 and 1001 persisted Paid payout rows with NetAmount −0.01 and −1, also visible in payout-list GET. Deductions 0/999/999.99 produced exact positive amounts; −0.01/−1 were rejected without changing the captured payroll snapshot. Deductions 1000 produced a zero-net Paid entry, recorded POLICY-PENDING rather than approved behaviour. No actual bank disbursement or production write was performed.

Source snapshot:

```text
48:         if (gross <= 0 || (profile.PaymentModel == "SessionBlock" && (!sessions.HasValue || sessions <= 0))) return BadRequest(new { message = "Enter the completed sessions and a positive payout amount." });
49:         var payout = new PayrollPayout { AcademyId = academyId, PayrollProfileId = profile.Id, PayslipNumber = $"PS-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}", PeriodLabel = request.PeriodLabel.Trim(), SessionsCovered = sessions, GrossAmount = gross, Deductions = request.Deductions, NetAmount = gross - request.Deductions, PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "BankTransfer" : request.PaymentMethod.Trim(), Reference = Clean(request.Reference), PaidAtUtc = request.PaidAtUtc ?? DateTime.UtcNow };
50:         db.PayrollPayouts.Add(payout); await db.SaveChangesAsync(token);
51:         return Created($"/api/academies/{academyId}/payroll/payouts/{payout.Id}", new PayrollPayoutSummary(payout.Id, payout.PayrollProfileId, profile.WorkerName, profile.WorkerType, payout.PayslipNumber, payout.PeriodLabel, payout.SessionsCovered, payout.GrossAmount, payout.Deductions, payout.NetAmount, payout.Currency, payout.Status, payout.PaymentMethod, payout.Reference, payout.PaidAtUtc));
```

## Suspected root cause

Missing net-pay invariant.

## Business impact and blast radius

Payslips and payout ledger can contain negative NetAmount.

## Related / required regression

PAYROLL-RULE-001: SQL and rule tests for deductions 0, gross-1, gross, gross+1, negative; verify body and ledger.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
