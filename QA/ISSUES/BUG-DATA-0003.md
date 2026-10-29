# BUG-DATA-0003 — Payroll permits deductions greater than gross

## Regular statement/period continuation — 25 October 2026

Owner approved one regular payout per worker/period, with separate approved
adjustments for additional amounts. Native baseline
`3d417b89f1ec44a6bdae6266cd2a11ff` reproduced three sequential duplicates and
5/5concurrent201/201pairs, including alternate profiles and NoCashDue. Existing
arithmetic/precision/FP2/identity/history evidence below remains accepted.
Minimal academy transaction lock plus bounded recorded-worker/normalized-period
guard is implemented;2950API tests and74focused UI/QA checks PASS. Current SQL
`8789bc45d614493e82f25df29ca8b02e` PASS84HTTP/three sequential duplicate guards/
5/5simultaneous201-409pairs/authority/month aliases/audit rollback-recovery and
unchanged FP1/FP2/FP3 on fresh image `a930092d4f7a4beca243036d76629be2`.65actual
image files/exact original healthy restoration/full inventory/independent qualifier PASS;
linked SQL `c4cf7a9a88ea468eb6f2ef69a768f30a`44HTTP/four actual Owner browser
journeys390/1440light-dark/four201writes/four409refusals/SQL identity-money-status/
draft retention/reset/four PNG hashes/restoration/independent qualifier PASS.
no issue or broad payroll gate closure. See
[current report](../REPORTS/PAYROLL_REGULAR_PERIOD_GUARD_2026-10-25.md).

Structured payroll calendar/overlap equivalence, unknown legacy identity across
profiles, separate approved adjustment workflow, uncertain-result idempotency,
qualified payroll/device/release remain OPEN. Prior statement identities and
amounts are never rewritten to satisfy the new guard.

## Exact-candidate NoCashDue transport — 22 October 2026

[Native policy report](../REPORTS/API_IMAGE_POLICIES_2026-10-22.md):57checks PASS
with Monthly+SessionBlock gross1000/deductions1000/net0/NoCashDue/nullbankevidence/
recorded worker identity/exactNet0.00; deductions1001and false bank reference/date
rejected without financial/audit writes. Native immutable API/private SQL/current
109histories/cleanup/restoration/source unchanged PASS. Product FP2 retained,
not qualified payroll/accounting acceptance or duplicate-period gate closure.
**OPEN / RETEST**; wider workflow/browser/device/release still required.

## Positive-cent / linked boundary successor — 2026-10-13

[Report](../REPORTS/PHASE_2B_PAYROLL_DOCUMENT_LINKED.md): three native positive-cent minima aligned;64 focused checks PASS. Final run484f34cba03446dea79468d462c2f25b,577.8s/eight Admin journeys16 exact SQL-backed writes/eight one-cent payouts/four Teacher journeys/16 actual PDFs/304 unchanged five-table reads/retained14 arithmetic/precision/policy/history/worker/terminal/migration guards/no429/exact owned cleanup PASS. Prior1484 unit baseline retained, not rerun. **OPEN/RETEST**: periods/duplicates/idempotency/qualified payroll/physical/wider release remain; no whole-finance acceptance. Earlier form-lower-bound/Teacher-PDF pending notes are historical;320px list/action clipping separately queued for Batch2.

## Batch1 precision checkpoint — 2026-10-12

[Exact monetary input/storage report](../REPORTS/PHASE_2B_PAYROLL_INPUT_PRECISION.md): bounded Number-loss/sub-cent/SQL-overflow repair,1484 unit/61 handler-display-serializer checks PASS. Native6d65a21b502245f7853bc1c4d9657c24:12 exact-guidance400/full five-table no-write rejects and6 maximum create/update/payout controls retain0.01 net across Monthly/SessionBlock,14 original arithmetic boundaries and approved NoCashDue/worker/history/audit/tenant guards retained. No silent rounding. OPEN/RETEST: native form lower-bound/linked precision/Teacher browser/device, duplicate/period/idempotency and qualified payroll/release review remain. Earlier policy-pending wording below is historical and superseded only by the explicit approval checkpoint, not release acceptance.

## Batch1 checkpoint — Approved zero-net semantics — 2026-10-12

Owner-approved FP2 now records Monthly and SessionBlock zero-net earnings as NoCashDue statements, with nullable paid-at, no payment reference and no bank-disbursement claim. Legacy Paid/net0 data is retained and explicitly described as legacy. Linked run50823642199a46988c2ea153f2d4b69f passed14 retained arithmetic boundaries, native statement/reference/date/audit controls and actual selected statement PDFs. Migration downgrade refuses null-paid rows with51000 rather than fabricating a payment date; compatible down/up roundtrip passed on owned SQL. Full-range monetary inputs/sub-cent checks, period/duplicate/idempotency and qualified payroll review remain pending.

Current bounded evidence: [Batch1 ledger](../REPORTS/BATCH_1_CORE_INTEGRITY.md). Changes are local/uncommitted; **OPEN / RETEST, not closed**. The original baseline fields and historical findings below are retained, not a claim that the new runtime checks were unexecuted.

| Field | Value |
| --- | --- |
| Status | OPEN / FIX-IN-PROGRESS (local negative-net guard) |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Prior arithmetic/history retained; regular-period2950API/74UI-QA/84native HTTP/5pairs/FP1-FP3 plus44native linked HTTP/four real UI journeys/current payload/qualifiers PASS; structured calendar/adjustment/idempotency/qualified review/device/release OPEN |
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
