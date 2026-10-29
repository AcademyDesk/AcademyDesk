# Payroll — bounded action recovery

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. High successor to the payroll presentation checkpoint;not full payroll or enterprise acceptance.

## Implemented

- Synchronous ready/review/snapshot refs complement the existing shared pending ref. A failed manual refresh blocks even an old callback with captured ready state;successful refresh invalidates old callbacks. Existing shared pending protection still covers both forms and write/readback.
- HTTP408 joins500/network failures as uncertain outcomes. No automatic POST retry;draft retained and actions locked until an explicit successful read/review. A successful read asks the operator to check the previous entry,not proof it was absent.
- Profile creation requires a currently loaded worker and allowed worker/payment model. Payout creation requires a currently loaded active supported profile and allowed method. Existing backend independently checks authorization/current state/business rules;client checks grant no new authority and do not solve cross-tab/server races.
- Save captures native amount/session/period/reference fields plus each form's draft revision before awaiting. Confirmed success resets only an unchanged submitted draft. A newer native value or changed selector/date survives instead of being erased. Native values are checked directly through named controls even while disabled;FormData omission of disabled fields cannot falsely trigger reset.
- Confirmed profile/Paid/NoCashDue responses announce success before readback. If readback fails,success stays truthful with a do-not-repeat warning. Accepted payout responses with invalid/null/unknown/unreadable status no longer claim a bank payout or generated payslip:show accepted/status-unavailable,retain draft and lock for read recovery. Known4xx use existing safe guidance without claiming success.
- Existing exact decimal serializer,precision/amounts/currency totals,null payloads,UTC/payslip warnings,NoCashDue rules and PENTA presentation retained. No backend/schema/API/finance-policy change.

## Verification

- Final webpack build/TypeScript/static export86 PASS;payroll ESLint0errors/0warnings. No application source edit after final build. Six pinned PENTA vendor hashes unchanged;script parsing/scoped diff whitespace PASS.
- 102 focused node:test entries PASS:30 additive recovery/current-eligibility/draft/timeout/status tests plus72 retained payroll feedback/money/native-lowerbound/preservation entries. The5 intentionally hardened declarations are explicitly excluded from the historical unchanged claim,not rehashed as preserved;6 unchanged projections/guards and2 whole-file hashes (money/payslip) still checked. Existing exact-payload/reset/no-cash/pending/unmount assertions retained.
- One new invalid-worker-type test initially expected later validation guidance,while the existing type-change callback correctly cleared the worker first. The test now supplies a synthetic staff selection after changing type so the intended invalid-type guard is exercised;the no-POST requirement is retained. No application behavior changed to satisfy that assertion.
- 146 final-export synthetic browser cases PASS:96 new recovery cases (both forms×320/768/1440×light/dark×7 common scenarios,plus malformed accepted status/NoCashDue payout cases),42 existing visual/state/payslip cases and8 existing exact-money journeys/16 raw writes. Recovery covers408,network loss,rejection,confirmed-save/read failure,held write/readback,newer programmatic native draft while controls remain disabled,malformed accepted response and correct earnings statement. Exact payload/nulls,one-write limit,retained/recovered drafts,pending lock and no page errors/overflow checked. This is synthetic transport,not real SQL/Identity/physical devices.
- Receipts:`QA/EVIDENCE/payroll-recovery-1792046273252/result.json`, `QA/EVIDENCE/payroll-visual-1792046283717/result.json`, `QA/EVIDENCE/payroll-precision-browser-1792046294884/receipt.json`. Available recorded source hashes match final files;final phone light profile/dark payout timeout screenshots inspected. No application-source edit after final build. Intermediate/local evidence preserved.

## Limits and next

This is page-lifetime recovery and stale-callback protection,not durable idempotency,proof of bank transfer,or cross-tab/reload/server concurrency acceptance. Runtime array checks are not a complete schema validator. Existing API/SQL/audit/RBAC/tenant/device release cases and payroll issue records remain OPEN;no backend/realSQL suite rerun in this frontend-only slice. Existing linked SQL/Identity evidence is retained at its prior source point,not relabelled fresh.

Next Medium:remaining academic/communications and portal-state visual parity from the existing E3 ledger. High remains required for consequential finance/authority/runtime/AI integration. PENTA UI2/Mini qualification/conversational-domain coverage and E9/E10 release gates remain OPEN. Owner sequence:complete agreed PENTA+Manual app → authorized go-live → Hayansh onboarding → Academy Desk product website after first customer live. No project switch/new engine contract handoff needed here.

No backend/schema/Mini/main/staging/commit/push/Azure/production data/container changes. Earlier unrelated dirty work/local QA evidence preserved.
