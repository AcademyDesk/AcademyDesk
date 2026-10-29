# Native Reports, ledger and actual CSV download — 23 October 2026

**PASS 47 real SQL/API setup/security checks and four real-login browser
journeys**, 390/1440 px × light/dark. Current worktree `D:\AcademyDesk-codex-p0`,
`codex/penta-search`, unchanged HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.
No application/finance/permission/package/model changes or rebuild.

SQL `a78ec590dfd946648b7f0384ff1c2ec5`, approved window
`2b0615b22b824f3ab614eab4f818722f`, both **PASS**. Same exact API image
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`,
frontend candidate `293e5beba439453794e6c1e68adc2b71`, 627 verified export files.
All 102 application + seven Identity migrations retained.

## Actual linked assertions

- Visible real login/session; each journey performs its own nine authoritative
  report-source GETs, not previously cached/mock data. No token injection or
  API response mocks. Relay permits only reads and login/refresh, never a
  browser financial write. Setup uses real existing domain API operations.
- Invoice1000, approved reduction100, Reconciled600 + Completed100 + Voided50:
  SQL and UI collected700, balance200. Detail modal has exactly two collected
  rows (600/100), never the voided50. Outstanding modal shows200.
- Actual NoCashDue payroll row net0 excluded from unsettled payroll. Two
  expenses INR12.50/USD10.25 remain separate currencies, no conversion.
- Foreign-tenant9000 payment/student sentinel never appears in received API
  bodies or downloaded CSV. Existing role/foreign-tenant/inactive-token denials
  remain part of the retained native setup controls.
- Four actual Blob/anchor browser downloads of `academydesk-invoices.csv`:
  exact gross1000.00/reduction100.00/collected700.00/balance200.00, exactly one
  own invoice row, formula-like name preserved with an intentional leading tab
  inside the quoted cell, no foreign sentinel. Files hash-identical. This proves
  emitted bytes, **not** Excel/LibreOffice import/save/reopen or universal CSV safety.
- Exact nine-table SQL hash snapshots unchanged after each browser journey:
  Students, Teachers, Invoices, Payments, FinanceAdjustments, PayrollProfiles,
  PayrollPayouts, Expenses, AuditLogs. Four screenshots and four CSVs retained;
  mobile light screenshot visually inspected. No horizontal overflow/page errors,
  HTTP429 or 5xx. Original limiter unchanged; 61-second separation keeps setup
  and browser traffic in different existing rate windows.
- Final two academies/five identities/four payment rows; owned SQL/API/network
  removed. Exact two original PENTA containers/images restored healthy; all
  29 containers/41 volumes/four networks/ten images preserved. Admission3.499GiB,
  unchanged3GiB guard. No Docker restart/pruning or personal-process termination.

## Failure retained

First SQL `172e6032867943a3b4f6d76cc115cc5c`, window
`5e30ae2f0f464988b9bd4af2ca43e574`: retained deactivation test had already
disabled foreign fixture owner; its later student setup correctly returned403.
No browser cases ran. Fixture creation moved before that same deactivation
assertion; no authority restoration/bypass or application change. First run
overallFAIL retained; owned cleanup/restoration/inventories PASS.

## Qualified provenance and release boundary

SQL receipt SHA256 `9a1333202b1155af0e22bde7b49bcd95a6dee6a47947065baa8ae00bcaee9a7a`.
Window receipt SHA256 `e066b9b33442d79f7bb65d9046cc93cccc681b127da732cfe170821966941e1c`.
Four actual CSVs SHA256 `54c8a78b7fbc495d0e083ae65a14dfb6a0ee0756a48e46a8ac5235dc76ebcbdc`.

Read-only qualification now verifies **15 current native receipts**: 12 API-only,
current Payments, four-role entry and this Reports successor, including 40 PNG
hashes/four actual CSV hashes. Existing failed frontend envelope remains failed,
no waiver. Exact command:

```powershell
node QA/tools/web-dependency-qualification.cjs --native-report-successor 5cf31ac559164848b493d9f69330067c 9a06d7e7c0464cb78458a630ca7c84e3 cd76de157ec744dfa81d72f045bfffdb 2e0dc820a98e4fab800ca3f08790044c a78ec590dfd946648b7f0384ff1c2ec5 2b0615b22b824f3ab614eab4f818722f
```

BUG-DATA-0001/0002/0010 and BUG-SEC-0002 remain OPEN at their wider gates.
Not qualified accounting/GST, production least-privilege, physical devices,
whole-page/whole-portal visuals, live AI or production approval. All11 existing
packages remain OPEN. Next Sol High: native expense-form exact-money/optional
fields/feedback/reset to SQL, then outstanding critical host/domain acceptance.
No main/Mini/Git/Azure action; use Medium for settled visual refinement.
