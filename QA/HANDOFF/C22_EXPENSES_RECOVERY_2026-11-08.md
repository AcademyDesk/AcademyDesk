# C22 Expenses currency and submission recovery — 8 November 2026

Cloud owner. CLAIMED; implementation/verification pending.
Branch `codex/cloud-c22-expenses-recovery-20261108`.
Clean base `5f4eccba0fee010862289993e5ad5d824d268e18` (published C21).
Latest Desktop `88a3a45218ced1e4995d83fdcc1d4574cffb29c0`: C20 integrated;
2,183 combined API passes and real Reports/payment-transition evidence. Desktop
owns the reproduced legacy currency write repair and protected SQL/P0 work.
No active competing Expenses page claim found. C21 is a separate required return.

Exact paths (expanded lookup claim before backend changes):

- `apps/api/Controllers/ExpensesController.cs` — add read-only branch-options and its id/name DTO; preserve Create/Update
- `tests/AcademyDesk.Api.Tests/ExpenseBranchOptionTests.cs`
- `apps/web/src/app/expenses/page.tsx`
- `QA/tools/expenses-recovery.test.cjs`
- `QA/tools/expenses-recovery-browser.cjs`
- `QA/ISSUES/BUG-DATA-0016.md` — preserve local SQL and previous currency evidence
- `QA/HANDOFF/C22_EXPENSES_RECOVERY_2026-11-08.md`

Group recorded-expense and TeacherPayout totals by actual unit; keep cents and
safe missing/legacy-currency labels using C20's existing pure formatter. Preserve
all-recorded-expense scope, filtering, branch/date/category controls and the
existing explicit INR creation payload. Do not introduce a new expense lifecycle
or payment/approval/worker attribution policy.

Initial required read/loading/denial must not publish ready zero totals or an
editable form without a complete register/branch lookup. Keep read-only retry
and sign-in/account guidance without developer host/port details. Replace the existing branch-management lookup dependency with a minimal Finance-
scoped GET branch-options, inheriting the existing controller permission/module/
tenant gate. Do not grant branch management or change any write authority.

Guard overlapping/stale submits synchronously, preserve definitive-rejection
input, distinguish acknowledged save from failed readback, and lock any unknown
transport/5xx result against replay. Reload financial records performs reads,
not a POST retry. An unconfirmed submission retains its draft and requires a
fresh read plus explicit Start a new expense before clearing that draft and
unlocking another write. Acknowledged saves clear inputs once; failed refresh
stays explicitly saved and blocked until a successful read. Ignore late reads/
saves after unmount and preserve every backend/audit/transaction/SQL path.

Before/after actual TSX handlers, controlled Chromium mixed/empty/legacy/error/
read-only retry, real-browser save/rejection/lost-response/failed-readback/double-
submit cases with clearly synthetic persistence, build/types/lint and full
frontend. The original frontend-only scope initially verified reuse of2,177 API passes.
The new read-only lookup requires targeted actual Identity/HTTP/InMemory and a
fresh full API run; previous source-reuse receipt is historical, not final proof. Actual expense Identity/HTTP/SQL/
audit/race/device/all-role/branch dependency acceptance remains Desktop-owned.
Keep five P0s/BUG-DATA-0016 OPEN, Mini/Plus/Pro unqualified, Ultra deferred and
stop before model loading. No main/Azure/environment/privacy/public access,
new dependencies/images, customer/provider/real-laptop writes or raw evidence
publication. Sol High for financial outcome/recovery; Medium for settled docs.

## Expanded minimal-lookup claim

Source review found the existing GET branches dependency uses MultiBranch and
has no FinanceUser controller mapping. An otherwise permitted Finance expense
user can be denied branch options. Before editing, Cloud adds only the read-
only Expenses branch-options action/test paths above to this packet; no active
competing Expenses-controller source claim exists in the latest Desktop handoff.
Desktop retains all protected finance write repair, SQL and migration paths.
The option JSON must expose only id/name, scope by academy and order stably.
Normal FinanceUsers with Finance (without MultiBranch) must be able to use it;
foreign/anonymous/unprivileged/missing-Finance requests remain denied. Reads
must not change branch, academy or audit data. Create/Update/DTO bytes remain.
