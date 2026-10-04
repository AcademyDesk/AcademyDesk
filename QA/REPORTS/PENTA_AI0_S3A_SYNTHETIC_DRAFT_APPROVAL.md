# PENTA AI0-S3a — synthetic draft approval boundary

Date: 2026-10-04. Local worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. This is an authority rehearsal, not an enabled academy action or live AI feature.

## Scope and design

- Added default-off, Development/Testing-only `draft-previews` prepare, read and confirm routes under the existing PENTA Academy Admin/Owner policy. Platform owner, teachers, foreign academy and unrelated same-academy actors cannot use another actor's preview. The ordinary `turns` route and fake diagnostic remain unchanged.
- The only accepted titles are the two code-owned synthetic fixtures `Review schedule` and `Review attendance`. The preview stores no arbitrary user text, student information, model output, recipient, URL or domain record ID. This is not a general-purpose draft editor.
- Preparation persists one actor/academy/tool-version-scoped task, execution and approval with an exact canonical payload, server-computed digest, policy version and ten-minute expiry. A key reused with different arguments conflicts. A bounded daily preview count (20/actor, 100/academy) is checked under the academy SQL lock.
- Confirmation rechecks current actor/academy eligibility, exact preview digest, stored payload integrity, policy version, expiry and pending status under the same SQL lock. Concurrent matching confirmation records one Approved decision and one audit transition; replay returns the saved state. Changed or expired previews cannot be approved.
- **Approved means the preview was approved, not executed.** No `AdminWorkItem`, message, student change, PENTA attempt, model call or external effect is created. The API response explicitly states this. An actual R1 domain action needs its own authorized adapter, transaction, retention and revocation-race tests in a later slice.
- The PENTA controller retains its existing explicit audit ownership, avoiding the generic action filter's late post-write audit. Preview/confirmation and their audit entries commit in the same domain SQL transaction. The new payload column is nullable for existing synthetic executions.

## Validation

| Check | Result |
| --- | --- |
| Full API suite | PASS 1,057/1,057, 0 skipped. New unit/API cases cover role/tenant/input denial, exact replay/changed key, digest confirmation, actor revocation, expiry and no domain effect. |
| SQL harness build | PASS, 0 warnings/errors. |
| Real Identity/HTTP/disposable SQL | Final guarded `PentaFoundation` run `866f9e1fd36c4858ac99b9ce0d41705e` PASS. 86 application and 7 Identity migrations. Concurrent prepare created once/replayed once; concurrent same-actor confirms returned the approved state with one transition/audit; another academy admin could not read or confirm. Wrong digest, stored-payload tampering and expiry denied. Audit-write fault rolled preparation back completely. No work item created. Existing S2b/S2c SQL checks stayed green. |
| Cleanup | Run-owned SQL database, login and container removed. Mismatched-owner negative cleanup guard passed. Local `QA/EVIDENCE` not staged or changed. |

## Open gates

This is not a customer-facing AI assistant, not a personal free-text draft, and not a real work-queue action. No provider choice, privacy/data-processing decision, protected free-text retention policy, host-lease recovery or production pricing has been approved. The first real action still needs a scoped domain adapter, current-permission and revocation-race boundary, outcome/audit transaction, browser/mobile proof, and business-owner acceptance. Enterprise P0 issues and the pre-Azure gate remain open. No merge, GitHub push or Azure deployment is authorized by this result.

Next route: Codex / GPT-6.1 Sol / High / Default mode for an authorization-sensitive first R0 read projection or an approved R1 domain adapter. Use Sol Medium/Cursor for bounded docs/UI; Astra only if a consequential authority decision remains unresolved.
