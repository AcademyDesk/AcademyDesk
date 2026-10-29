# PENTA AI-first role entry and complete-portal execution ledger

Date:2026-10-14. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD unchanged `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Owner requests AI-first role landing, working Manual toggle and complete visuals/AI functionality tracked to acceptance. This implements the first bounded entry/safety slice and documents the remaining work; it does not claim the full request is complete.

## Verified reality / scope

Reviewed host security policy, `PentaPilotPolicy`, auth session workspace projection, existing chat/context/health/turn/receipt frontend and latest PENTA UI2 return/manifests. Conversational pilot is Owner/AcademyAdmin + current academy/Finance + configuration/Development-or-Testing gates. Only SearchLearners/GetLearner; no general CRUD/send/schedule/financial writes or automations. Student/Teacher/Guardian/Platform do not receive those tools. Manual pages and deterministic domain services are retained. `LocalPreview` does not satisfy the pilot's Development/Testing gate; local health200 is not an enabled Mini connection.

UI2 is explicitly a synthetic interaction prototype, not a released authenticated host adapter. PENTA's latest return retains old86/94 language qualification as historical NOT ACCEPTED; frontend310 cases do not resolve it. No engine inference benchmark rerun, engine/Core implementation duplication or model/tool activation here. Prepared [dependency packet](../HANDOFFS/PENTA_ROLE_WORKFLOW_READINESS.md) through established handoff; not automatically sent, no Mini files edited or new protocol adopted.

## Implemented

- Login validates the authoritative auth-session response before saving tokens. Correct workspace namespace only; missing/failed/malformed session no longer silently becomes an Admin login. Unknown/mismatched role/workspace profile rejected. This frontend sanity check does not replace backend authorization.
- Default Owner/Admin → `/penta`; PlatformOwner → `/platform/ai`; Teacher → `/teacher/ai`; Student/Guardian → `/portal/ai`. Other administrative roles such as FinanceUser/Manager remain Manual. Explicit in-workspace return destinations retain their path/query/fragment; cross-role/external/encoded separator/backslash/control-character destinations rejected. No role or tenant ID is taken from query/model/memory.
- Three role-specific PENTA entry routes verify only their current workspace's session and truthfully show no conversation/actions enabled. They request no academy records, Admin finance/tool/model endpoints or mutation. Wrong role/expired/denied/unavailable session has clear non-sensitive guidance. No fabricated model/chat or enabled unavailable action; five capability disclosures are explanatory only. Both themes use pinned PENTA tokens; Manual action contrast repaired after screenshot exposed vendor anchor specificity.
- Platform/Teacher/Student Manual portals have a visible PENTA AI / Manual mode bar; Admin retains its existing in-place switch/directory/conversation lifecycle. Normal role Manual functionality remains independent. A conservative role Manual mode-link safeguard observes interaction with forms (including pointer interaction with custom controls): asks before leaving; Stay/Escape keeps form data. It does not implement durable drafts, save confirmation, universal unsaved detection or guard every external/internal navigation. Conservatively may warn after a previously saved interaction; no silent save or model action occurs.
- [Completion ledger](../../PENTA_PORTAL_COMPLETION_CHECKLIST.md) enumerates83 current source page declarations with light/dark/state checks and explicit in-page section requirements, all full-page acceptance still pending. Links existing E3–E10, host role boundaries, UI2 integration, per-domain conversation/tool backlog, Mini qualification and release. No new enterprise audit or false90% metric.

## Final source validation

Final `npm run build -- --webpack`: production compilation/TypeScript and86 exported outputs PASS (three new page routes; source census83 page declarations is a different count). Scoped lint0errors/0warnings for login/helper/WorkspaceFrame/new role component/three route files.20/20 unit checks PASS:7 default roles, malformed accounts, scoped returns and complete page-ledger census; no full enterprise suite repeated for frontend-only changes. Six pinned vendor normalized hashes unchanged; no new dependency/config/package changes.

**170/170 exported-browser cases PASS on final unchanged app source**, fixtures intercept synthetic API responses, not real login/permissions/SQL/provider/device acceptance:

| Suite | Cases | Local result receipt |
| --- | ---: | --- |
| AI-entry/default login + switch/outage/denial/profile guards |56|`QA/EVIDENCE/penta-entry-1791967893874/result.json`|
| Platform navigation regression |24|`QA/EVIDENCE/platform-navigation-1791967893843/result.json`|
| Teacher Manual foundation regression |36|`QA/EVIDENCE/teacher-manual-1791967893827/result.json`|
| Student/Guardian Manual foundation regression |48|`QA/EVIDENCE/student-manual-1791967960182/result.json`|
| Existing prompt-first read-pilot design/receipt interaction |6|`QA/EVIDENCE/penta-design-1791967997581/result.json`|

Entry56 includes36 login/mode journeys (six roles×320/768/1440×both themes),16 denied/missing/mismatched/unavailable role-session cases and4 failed/malformed login-profile cases. Verifies token namespace, role session auth header, no unauthorized-role academy/tool/model calls, no live domain writes, actual Manual CTA contrast>=4.5, existing Admin composer disabled during503, visible Manual directory/fallback, capability disclosure, Teacher320px two-theme form safeguard/Stay/Escape retention/explicit leave, viewport widths/no unexpected page errors. Negative tests do not prove current real server RBAC or revocation. Actual synthetic login POST is intercepted; no real accounts or sessions created in SQL.

Phone dark Teacher and light Platform screenshots visually inspected after final contrast repair: clear role/unavailable status, legible Manual action and capability information. Intermediate receipts retained. First new browser fixture had an incorrect Student detail shape and detached on runtime error; corrected fixture to the unchanged real contract, not application. Initial fixture-only/schema and wrong-working-directory test command failures retained; no assertion or permission weakening. Initial56 before contrast/safeguard is not the final receipt above.

## Preserved boundaries / next

No backend, domain authorization, finance calculations, tool allowlist, approvals, model/provider, SQL schema/data, credentials or separate Mini source changed. Existing dirty finance/media/payroll/Mini/QA work preserved. No stage/commit/push/main edit/merge/Azure/container cleanup. Prior frontend commit remains intact. Evidence local; whole-app lint, exact release security/runtime/SQL/migrations/device/privacy/operations remain open. No full backend or frontend acceptance from this entry checkpoint.

Next existing E3: Admin directories/onboarding→class/schedule→remaining finance/operations/academics/communications/governance visual states; remaining Platform tabs/Teacher7/Student6 and full-page signoff. In parallel, reviewed UI2 real host adapter E5; real Mini-dependent execution waits for qualified engine/paired contract return. E6–E8 each operation needs authoritative tools/domain validation/duplicates/current authorization/approval as required/transaction-idempotency-audit/real receipts and tests. Never enable all five names or all roles to make UI look complete. High consequential entry/host/security,Medium settled styling. No automatic project switch or deployment.
