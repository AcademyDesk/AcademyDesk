# PENTA C1a — private synthetic conversation foundation

Date: 2026-10-04. Worktree: `D:\AcademyDesk-codex-p0`. Branch: `codex/penta-search`.
Starting HEAD: `f8389a77ff731ebc5a095f42a1d72d2671f22e18`. Changes remain local/uncommitted.

Status: **C1a implemented and focused API/real-SQL checks passed; C1 and conversational product acceptance remain OPEN.** This is a deliberately narrow storage/ownership/serialization boundary, not natural-language AI or the final chat UI. Read [PENTA-002](../../PENTA_CONVERSATIONAL_ARCHITECTURE.md) and the [31-requirement gap map](PENTA_CONVERSATIONAL_GAP_MAP.md).

## Scope and safeguards

- Added separate conversation, turn and message entities, application migration and scoped composite keys. Existing diagnostic tasks/executions/approvals/usage are not repurposed as transcripts.
- `POST /api/academies/{academyId}/penta/conversations`, `GET .../{conversationId}` and `POST .../{conversationId}/turns` require current own-academy active Owner/Admin authority. Same-academy admins do not share private history. Tenant and actor are never accepted from message payloads.
- Independently default-off `Penta:ConversationContractsEnabled` also requires the existing pilot flag and **Testing** environment. Development, Staging and Production cannot enable this C1a rehearsal. No app configuration enables it; only synthetic test hosts set the flag.
- Only two exact code-owned synthetic input strings are accepted, and replies/labels are explicitly synthetic. Arbitrary prompts, model/tool/actor/academy fields and oversized content are rejected. No real student/financial/contact data or customer transcript enters storage. This restriction is not the future product's prompt interface.
- SQL academy-first serialization rechecks current actor authority after waiting, discarding the scoped read-only Identity cache. Context version, turn, both messages and required audit commit together. No network/model/domain operation is inside the transaction.
- Request IDs are scoped to actor/academy/conversation. Same turn details replay the original receipt, including after host restart; changed details conflict. Another tab using an old context version receives 409 without messages or a version update. A later replay reports the original turn version; read current history before submitting a new turn.
- Ordered history is bounded to 50 turns/100 messages, with a 24-hour **synthetic-test access expiry**, 20 new conversations/user/day and 100/academy/day. These are test limits, not approved production retention. Expired synthetic data is removed by disposable test-database cleanup; no production purge/history policy is implemented.
- Required read/replay audits contain fixed policy/outcome metadata, not prompt/message content. The shared filter recognizes this controller's own atomic audit rather than adding a post-commit duplicate audit.
- No frontend change, live runtime, financial action, external send, role expansion or Azure enablement.

## Validation

| Check | Result |
| --- | --- |
| Initial API build | PASS, 0 warnings/errors |
| SqlHarness rebuild with new tests | PASS, 0 warnings/errors |
| Focused PENTA tests | PASS **48/48**, including 8 new conversation cases |
| Shared audit-outcome tests | PASS **30/30** |
| EF pending-model check | PASS, model matches migration; retained pre-existing Invoice.AdjustedAmount/GradingScheme.PassingPercent precision warnings |
| Real Identity/HTTP/disposable SQL | PASS, run `63bcea7c255340baa84a3626d25ab038`, exit 0, 67.4 seconds |
| Distinct concurrent turns | **5/5** exactly 201 + 409, version 1, one turn/two messages, no 429 |
| Identical concurrent turn / restart | Exactly 201 + 200, same turn ID; restarted host replay 200 without a second turn |
| SQL composite-FK negative cases | Wrong academy and wrong actor cannot attach a turn to private conversation |
| Audit-store failure | Create and append roll back; no unaudited conversation/turn/message/version survives |
| Private access and current revocation | Anonymous 401; teacher/platform/foreign tenant 403; another own-academy admin 404; revoked admin 403 |
| Migrations / cleanup | **87 application + 7 Identity** migrations; negative ownership cleanup refused a mismatched marker; run-owned database/login/container removed |

Commands: `dotnet test ... --filter 'FullyQualifiedName~Penta|FullyQualifiedName~AcademyAccessFilter'`, separate `--filter FullyQualifiedName~AuditOutcomeTests`, `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore`, `dotnet ef migrations has-pending-model-changes --context AcademyDeskDbContext --no-build`, and existing `Run-ReconciledPayment.ps1 -Module PentaFoundation` with browser hold disabled.

The first eight-case test run had one fixture failure: advancing the shared clock expired authentication as well as conversation access, producing the correct 401 instead of the expected 410. The test now signs in again before asserting conversation expiry; final focused tests pass. The first Windows PowerShell invocation was blocked by execution policy before starting any SQL container. The reviewed local runner was then invoked with process-only `-ExecutionPolicy Bypass`; no persistent system-policy change was made.

Existing PENTA SQL regression assertions were retained: student/batch minimal reads, role/tenant/audit-fault checks, priced-budget caps, synthetic probe coordination, unknown-outcome recovery/race and synthetic approval all passed in the same run. No regression was weakened. Full enterprise suite, Docker image rebuilds, migration downgrade/production upgrade acceptance, browser/physical devices and real-model/golden-conversation tests were **not run** here.

The preserved frontend and existing BrowserPentaHost SHA-256 values match their pre-C1a values. `QA/EVIDENCE` remains intentionally local/untracked. This report contains no secret values or production connection details.

## Remaining C1 and next route

Next is **C1b, Sol High**: server-validated page/record references, bounded structured conversation context and code-owned typed artifacts. Add targeted actor/tenant/reference/version/audit tests and disposable SQL proof. Do not treat client-provided context as authority or send customer data to a model. Real free-text message protection, approved production retention/notice/key lifecycle, private list/rename/search/delete policy, asynchronous status/unknown-outcome contracts and final frontend integration are still open.

Then C2 adds shared deterministic Finance dues projections; C3 evaluates/enables only an explicitly approved runtime; C4 implements and accepts the complete prompt-first read journey; C5 adds one separately gated domain operation. No P0 issue is closed. No commit, push, main change, production database operation or Azure deployment occurred in this packet.
