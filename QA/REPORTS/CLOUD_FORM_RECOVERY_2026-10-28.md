# Remaining form targets and confirmed-write recovery

Date: 2026-10-28. Branch: `codex/release-export-security`.
Base: `f9a0cd7d7569aaab2846c0afeacb308546dd2067`, pushed and independently verified.

MODEL: Current Codex session
TOOL: Codex
TASK: Finish the remaining direct event-target reset sites and preserve confirmed-write outcomes
MAY MODIFY: Affected frontend handlers, controlled tests, QA records and handoff
MUST NOT MODIFY: Payroll arithmetic, backend authorization/business rules, migrations or deployment
NEXT ROUTE: Codex for guarded SQL/HTTP and critical security gates; browser/device acceptance separately

## Reproduction and changes

The nine remaining `event.currentTarget.reset()`/`e.currentTarget.reset()` sites
were in teacher assignment/leave, learner action, grading scheme/prerequisite,
platform support/invoice/announcement and payroll profile/payout handlers.
Each now captures its form element during dispatch and resets that element after
a confirmed successful response. No such direct reset calls remain under web/src.
This source check is not a claim that all asynchronous form behavior is correct.

Twenty-one controlled actual-handler cases initially returned seven passed and
14 failed. They reproduced nine accepted-submit failures, two platform rejection
paths that attempted a reset anyway, and three confirmed-write/readback failures.
After repair all 21 passed. Two additional payroll readback cases then reproduced
two further failures; those are repaired too, for 23 passing targeted cases.

Platform request helpers now return whether the write was confirmed. Support and
invoice forms retain their input on rejection; successful writes remain successful
if the separate list refresh fails. Platform announcements and payroll profile/
payout forms likewise report the confirmed save and a refresh warning rather than
an ambiguous failure that could encourage a duplicate submission.

The tests assert one synthetic write and its endpoint, reset only on acceptance,
and explicit recovery guidance after failed readback. Payroll tests retain exact
monthly payout payload assertions, linked worker identity and null/session handling.
No backend financial rule or actual disbursement/notification/customer action was
changed or executed. Idempotency, transaction and policy gates remain open.

The existing controlled page runtime was extracted to
`QA/tools/frontend-page-runtime.cjs`; all 37 original initial-load/form assertions
remain in place and pass. The new regression file adds 23 checks. No existing
assertion was removed or disabled.

## Validation

| Check | Result |
| --- | --- |
| Remaining form regression suite | 23 passed, zero failed/skipped/cancelled |
| `npm test` | 966 tests: 963 passed, three failed, zero skipped/cancelled; 44 current files |
| `npx tsc --noEmit` | PASS |
| `npm run lint` | PASS: zero errors, 65 inherited warnings |
| `npm run build -- --webpack` | PASS on Next.js 16.4.3 |
| `git diff --check` | PASS |

The same three communication-drafts cases still fail because the historical
synthetic SQL log is absent. They were not skipped or supplied fabricated data.
The prior 1,073 API passes and zero-known-vulnerability npm/NuGet audits are
retained, not rerun for this frontend-only increment; dependencies are unchanged.

## Acceptance and continuation

BUG-FUNC-0001 and BUG-FUNC-0003 remain OPEN for actual browser/SQL acceptance,
other notice/reload behavior and critical regression. Controlled hooks simulate
event release and responses; they do not prove real browser dispatch, persistence,
financial integrity or delivery. Other broad record-selection races also remain.

The pending CDN rules still prevent the SQL image/model downloads, so no SQL or
inference readiness claim is made. No environment was published and no Azure
deployment or main merge ran. This increment is checkpointed on the development
branch under the user's explicit authorization for continuing GitHub pushes.
