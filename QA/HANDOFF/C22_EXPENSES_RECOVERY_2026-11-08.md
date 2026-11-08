# C22 Expenses currency and submission recovery — 8 November 2026

Cloud owner. CLAIMED; implementation/verification pending.
Branch `codex/cloud-c22-expenses-recovery-20261108`.
Clean base `5f4eccba0fee010862289993e5ad5d824d268e18` (published C21).
Latest Desktop `88a3a45218ced1e4995d83fdcc1d4574cffb29c0`: C20 integrated;
2,183 combined API passes and real Reports/payment-transition evidence. Desktop
owns the reproduced legacy currency write repair and protected SQL/P0 work.
No active competing Expenses page claim found. C21 is a separate required return.

Exact paths:

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
and sign-in/account guidance without developer host/port details. Preserve the
existing branch-read dependency; no new role/module authority or backend API.

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
frontend. Existing2,177 API passes may be reused only after exact source/assembly
comparison; no new backend/SQL pass claim. Actual expense Identity/HTTP/SQL/
audit/race/device/all-role/branch dependency acceptance remains Desktop-owned.
Keep five P0s/BUG-DATA-0016 OPEN, Mini/Plus/Pro unqualified, Ultra deferred and
stop before model loading. No main/Azure/environment/privacy/public access,
new dependencies/images, customer/provider/real-laptop writes or raw evidence
publication. Sol High for financial outcome/recovery; Medium for settled docs.
