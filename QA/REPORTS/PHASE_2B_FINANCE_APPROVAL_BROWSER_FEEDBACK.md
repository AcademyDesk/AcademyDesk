# Phase 2B — Finance approval browser feedback (2026-10-04)

## Scope

Local `codex/enterprise-p0-continuation` only. This bounded browser check covers the operator-facing response when the API rejects an adjustment approval after collection has consumed the remaining invoice balance. The SQL/HTTP approval-versus-payment transaction result is recorded separately in [the guard report](PHASE_2B_APPROVAL_PAYMENT_GUARD.md); this check does not substitute a mock response for that ledger test.

The page source was copied byte-for-byte into an ignored `.build-check` Next.js shell. A loopback-only synthetic API on `127.0.0.1:49312` supplied one pending 200 discount and returned the backend's exact 400 message, `Adjustment exceeds the remaining invoice balance.` The browser used `127.0.0.1:49311`. No academy/customer data, user credentials, normal development database, Azure, or production API were used. The first probe exposed that this browser does not support native `window.prompt`; the product flow was changed to an inline note form, and the final browser run used that unmodified page without a prompt override.

## Result

| Check | Observation |
| --- | --- |
| Initial load | Pending adjustments showed 1 and the synthetic Discount approval was visible. |
| Open and cancel inline approval form | No PATCH was sent (`approvalAttempts: 0`); item remained pending. |
| Confirm approval | One PATCH received the 400; the page announced the exact balance reason through `role=alert`, refreshed the adjustment list, and kept the pending count at 1. |
| Empty rejection reason | Native form validation blocked submission; PATCH count remained 1. |
| Browser logs | No warning or error after final inline-form run. |
| Frontend validation | `npx tsc --noEmit` passed. Targeted ESLint: 0 errors, three existing warnings (hook dependency and two image elements). |

`node QA/tools/validate.cjs` could not complete because this worktree has no local `QA/EVIDENCE/logs/observed-checks.json`. That evidence is intentionally local and was not recreated or removed for this browser slice.

The page now uses an inline approval/rejection note form instead of a native prompt, prevents a second decision while saving, surfaces the API's bounded message, refreshes the queue after rejection, and distinguishes error alerts from success status. Screenshot evidence with synthetic data is local and untracked at `QA/EVIDENCE/finance-governance-browser-20261004/rejected-approval.png`.

## Limits

This is one desktop browser with a synthetic API response. It does not prove a linked signed-in browser/database flow, physical Android/iOS behavior, over-adjustment/refund policy, the complete role/tenant matrix, or release readiness. BUG-DATA-0002 remains OPEN. No main merge, GitHub push, or Azure deployment.
