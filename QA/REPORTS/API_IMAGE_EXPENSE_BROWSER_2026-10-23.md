# Native expense form, optional fields and exact decimal — 23 October 2026

**Accepted successor: PASS34 SQL/API checks +four native browser journeys.**
SQL `db412a4d58424333a01f73fc0d5e3575`, window
`7520f723e5204994806c9b8903a1893a`. Current `codex/penta-search` worktree,
unchanged HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`, exact API image
`sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`
and 627-file frontend candidate `293e5beba439453794e6c1e68adc2b71`.
No application/finance/permission/package/model changes or rebuild.

## Real end-to-end scope

- Actual visible Identity login, page fields and existing form-submit handler;
  no API response mocks or token injection. Four journeys:390/1440 px ×light/dark.
- Native exact amounts `0.01`, `12.50`, `9999999999999999.99`, `1.00` are submitted
  without JavaScript Number conversion, retained exactly in SQL and displayed
  through the additive exact-decimal readback field. Maximum text comparison is
  exact Indian grouping, not a rounded `10000000000000000` value.
- Optional branch/date empty in UI become explicit nulls in the request. SQL
  BranchId stays null; ExpenseDate follows the existing server default. Required
  description/category/currency/status are correct. No optional-null exception.
- Each pair of immediately dispatched form-submit events produces exactly one
  real201, one matching persisted row and one `POST Expenses` audit. Success
  `Expense recorded.` visible; description/amount reset and empty submit disabled.
  This proves pending duplicate-submit protection, not a new semantic duplicate-
  expense/idempotency policy or protection against a deliberate later new entry.
- Exactly four final expense rows and four audits. Seven other domain table
  hashes unchanged; no payments, identities or model actions created.
- Native sub-cent, overflow, zero and random unowned/nonexistent branch requests
  all400/exact validation guidance; expense/audit and other financial snapshots
  unchanged. The guard is labelled `foreign branch` in the fixture but uses a
  fresh nonexistent GUID: **not** a separately populated foreign-branch case.
- No429/5xx, page exceptions or document horizontal overflow. Four PNGs retained.
  Selected desktop maximum screenshot inspected: the extreme aggregate KPI
  wraps mid-number. Data and exact row assertions pass; this remains a visual
  polish item in E3, not a claim of perfect all-screen styling/device acceptance.
- All109 histories retained. Owned SQL/API/network removed; exact original two
  PENTA containers/images restored healthy; inventories match29containers/
  41volumes/four networks/ten images. Admission3.722GiB, unchanged3GiB guard.

## Retained prior attempt and orchestration repair

SQL `69c8d1e45f6b4f5f947ea764b56e77bb` completed the same34checks/four journeys,
but its receipt predates the explicit final `expenseRows` field required by the
new qualifier. It is not rewritten or accepted by a weakened qualifier.
Window `7dfd13bccfb34ad1850cde8b06c497ec` remains **FAIL**: a retry was started
before that window finished its restoration verification. Its final health/
inventory snapshot overlapped the next window. This is an orchestration defect,
not a finance failure or accepted restoration; the failed receipt is retained.
The next window ultimately restored exact originals and inventories successfully.

`local-validation-lease.cjs` now coordinates future API and frontend stop/restore
wrappers with an exclusive owned lock, released only after their final cleanup.
Existing/foreign/changed/linked locks fail closed, with no automatic stale-lock
deletion or process kill. Four unit tests PASS. Actual CLI exclusion smoke held
one lease and invoked both wrappers: each exited1 before service mutation; exact
service status unchanged and owned lease released. A file-symlink unit fixture
first hit Windows privilege denial; a real directory-junction fixture now tests
the same escape boundary without enabling privileges. No failed test hidden.

## Qualified evidence

- SQL result SHA256 `a339137a9839a80282f90033416d4f396770f709403ac1366de977621480512b`.
- Window result SHA256 `1acc83f8210effec83e136320082178d293e9057521cbd7c5014f93b3c3ce07d`.
- **16 current native receipts qualified**, including44PNG/four actual CSV hashes.
  Earlier failed frontend envelope remains failed and advisory remains nonzero.

```powershell
node QA/tools/web-dependency-qualification.cjs --native-finance-consumer-successor 5cf31ac559164848b493d9f69330067c 9a06d7e7c0464cb78458a630ca7c84e3 cd76de157ec744dfa81d72f045bfffdb 2e0dc820a98e4fab800ca3f08790044c a78ec590dfd946648b7f0384ff1c2ec5 2b0615b22b824f3ab614eab4f818722f db412a4d58424333a01f73fc0d5e3575 7520f723e5204994806c9b8903a1893a
```

Not qualified accounting/tax, entire expense lifecycle/races, all-page visuals,
physical devices, full conversational AI or production approval. All11 existing
packages remain OPEN. No main/Mini/Git/Azure/model action. Next Sol High: native
Teacher onboarding optional dates/certification/success/reset, preserving real
authorization and independent Manual fallback.
