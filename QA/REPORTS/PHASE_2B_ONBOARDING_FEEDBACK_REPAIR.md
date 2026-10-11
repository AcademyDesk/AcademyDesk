# Student and Teacher onboarding — pending and result feedback

2026-10-11 (Asia/Calcutta). Worktree D:\AcademyDesk-codex-p0, branch codex/penta-search, starting14238527f5db874281abd5cafb9e9099db326a40. Continuation of BUG-FUNC-0003 and the existing intake queue, not a new audit.

## Bounded implementation

Both pages check uncached academy loading before enabling intake. Synchronous pending refs reject same-tick duplicate submissions; native fieldsets disable all fields while saving. Status regions are polite/atomic and forms expose busy state. Rejected4xx responses retain details with a safe message; network/5xx responses explicitly say creation is unconfirmed and direct users to check the register before retrying. No automatic uncertain-write retry is introduced. Existing academyApi authenticated401 refresh behavior is unchanged.

Teacher success retains its original reset and payment-details guidance. Student success retains the Student Management redirect/notice; record IDs are encoded into the fixed route. A confirmed student response without a usable record link preserves details, provides a manual management link, and blocks another create. Navigation exceptions likewise do not suggest repeating a confirmed write. Late unmounted responses are ignored; teacher subject callbacks cannot mutate the submitted draft while pending.

Payloads, optional dates/fields, teacher subjects/certifications/availability, parent flags, backend authorization/transactions and uniqueness remain unchanged. No API/schema/Identity/Mini/security policy changes. Native fieldset uses display contents to retain existing layout. This is not broad frontend redesign or backend idempotency.

## Verification

-48 new actual-TSX controlled cases PASS; frozen1423852 source under the same assertions1 PASS/47 FAIL. Failures count assertions, not47 distinct bugs. Includes success/payload/reset/navigation,400/401/403/409/429/500/503, network ambiguity, same-tick duplicates, invalid/empty initial academy, unmount, malformed responses, missing student links, route encoding and subject protection.
-9 retained date-handler cases PASS; hook mock extended for useRef only, existing assertions preserved.
-48 exported UI synthetic browser cases PASS:320/1440px × light/dark × both routes × success/rejection/5xx/network/malformed-success/pending. Native required DOB calendar and submitted payloads verified; duplicate submit while pending and malformed student success send only one POST; no page exceptions. Two mobile uncertainty screenshots inspected, readable light/dark feedback and draft retention confirmed.
-32 retained date-browser cases PASS after updating the exact rejection text to include the new retained-details suffix. First run stopped after its first success because the previous exact text no longer matched; this was an assertion-text update, not removed date/payload/reset coverage. Final receipt records all32 cases.
- Target eslint0 errors/0 warnings, installed TypeScript PASS, production webpack export83/83 pages PASS. JS syntax and scoped diff checks PASS.
- Local browser receipts: QA/EVIDENCE/onboarding-feedback-browser-1791655825050/result.json and QA/EVIDENCE/onboarding-dates-browser-1791655879555/result.json. Evidence remains untracked; .build-check ignored; unrelated Mini work and32/28 shared continuity additions preserved.

The previous29 optional-number,33 account-flag,21 atomic and23 optional-date native SQL checks are retained, NOT rerun in this UI-only batch. Browser transport is synthetic, not live browser-to-SQL, real authentication/role/tenant or physical Android/iOS proof. Full enterprise/critical/release acceptance remains OPEN. Existing global QA validator missing local observed-checks.json remains blocked, not fabricated or claimed PASS.

## Continuation

BUG-FUNC-0003 stays OPEN. Original28 feedback-route checkpoints unchanged; this is additional intake coverage. Next bounded work: independent Manual/shared visual foundation, verify current FW1 return before claiming shared PENTA frontend delivery. Sol Medium for settled visual/UI work; Sol High for consequential host/domain/security/finance/Mini integration/release decisions. No new Mini contract dependency or project switch required for this slice. Existing E1–E10 and90% eligible AI/10% manual target preserved, not measured readiness.

Standing directive allows scoped commit/normal feature push only; no main changes/merge, production data, credentials, Azure deployment or history rewrite.
