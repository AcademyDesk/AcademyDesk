# C25 reminder consent, Finance lookup and queue recovery — 9 November 2026

Cloud owner. IMPLEMENTED / CLOUD VALIDATED; combined real-host acceptance pending.
Branch `codex/cloud-c25-reminder-queue-safety-20261109`.
Base `8479833` retains complete C24 at2114c8c and latest Desktop C23 SQL return
6f77a94c528b6e6df0301b98c5ed75dcc76c6581 with cherry-pick provenance. The only
conflicts were derived inventory and two issue appends: verified Desktop issue
text was an exact prefix of the retained C24 text, then regenerated the inventory.
All C23 product paths already matched. Five new Desktop QA/handoff/SQL-harness
paths were retained. No competing reminder page/channel-guard claim found.

MODEL: Sol High for notification policy/authority/recovery; Medium for docs.
TASK: prevent unsupported reminder channels from bypassing communication policy;
use the existing Finance-authorized student-options lookup; finish honest read
and queue recovery while retaining tenant/currency/cancellation/audit contracts.

Exact mutation paths:

- `apps/api/Controllers/FeeRemindersController.cs` — channel guard/canonical InApp and minimal Finance-scoped history GET; money/notification/audit rules preserved
- `tests/AcademyDesk.Api.Tests/FeeReminderChannelTests.cs`
- `apps/web/src/app/fee-reminders/page.tsx`
- `QA/tools/reminder-queue.test.cjs`
- `QA/tools/reminder-queue-browser.cjs`
- `QA/tools/fee-reminder-eligibility.test.cjs` — real lookup/confirmation fixture contract and normalized legacy-currency label; retain eligibility assertions
- `QA/tools/fee-reminder-eligibility-browser.cjs` — update only lookup/history/receipt fixture fields; retain all six native assertions
- `QA/ISSUES/BUG-SEC-0008.md`
- `QA/ISSUES/BUG-FUNC-0006.md`
- `QA/ISSUES/BUG-FUNC-0003.md`
- `QA/ISSUES/BUG-DATA-0019.md`
- `QA/ISSUES/BUG-DATA-0016.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/AI_QUALIFICATION_ENTRY_GATE_2026-11-09.md`
- `QA/HANDOFF/C25_REMINDER_QUEUE_SAFETY_2026-11-09.md`

The UI already sends InApp. Preserve null→InApp compatibility and normalize
trimmed case-insensitive InApp; explicitly reject blank/unknown/external channels
before any notification/audit mutation. This follows SEC-0008's allowed remedy
of rejecting unsupported channels rather than inventing provider integration.
Do not change Notifications.Create, provider consent semantics or external sends.

Required initial reads/denials/invalid roots must not become successful zero
balances/history or enabled intake. Use invoices/student-options rather than
student management. Keep actual currency/cents and cancellation eligibility.
Prevent double/stale queue callbacks synchronously. Confirmed queue stays queued
if readback fails; transport/5xx/malformed confirmation is explicitly unconfirmed
and locked against replay. Recovery reads never POST; a new queue after an
unknown result needs successful reload plus explicit new-intent action. Preserve
the existing bulk null-ID/targeted ID/InApp payload and server eligibility rules.

Validate original/final HTTP/Identity/InMemory channel/role/tenant/audit cases,
actual TSX/retained eligibility, sandboxed Chromium with synthetic persistence/
faults, exact-lock build/types/lint/full suites and unchanged production guard.
No fabricated SQL receipts or skipped assertions. Real SQL/provider/device/
critical acceptance remains Desktop-owned. Publish exact feature source/results.
Record a concrete AI entry checklist because the owner asks to be notified when
AI is next: do not imply non-AI acceptance is complete, and keep the explicit
stop before candidate loading/training/inference/activation. Mini/Plus/Pro remain
unqualified; Ultra deferred. No protected financial writers/migrations, shared
governance overwrites, environment/public-access changes, main merge or Azure.

## Minimal history claim expansion before source edits

Dependency review found the page also reads generic Notifications, which needs
Communication access. Replacing only student management would still leave a
Finance-only user blocked. Add GET fee-reminders/history inside the already-owned
Finance controller: own-academy Fee payment reminder metadata only (id, title,
channel, status, sentAtUtc), no message/recipient/template/variables/other notices.
Keep generic notification restrictions unchanged; no Communication privilege
grant, provider send or financial write. Verify actual FinanceUser/Owner/Admin,
foreign/unprivileged/anonymous, exact JSON fields and unchanged read snapshots.

## Source-bound verification

| Check | Actual result / boundary |
| --- | --- |
| Original channel HTTP/Identity/InMemory |31 cases:9 PASS/22 FAIL; original controller/TRX retained |
| Missing-history HTTP reproduction |Six cases:0 PASS/6 FAIL on the intermediate channel-only repair, before new GET; separate baseline |
| Final targeted API |133/133 PASS:37 new channel/history +96 retained currency/reminder/family/readback |
| Final complete API |2,272/2,272 PASS;0 fail/skip;fresh final binaries |
| Original actual reminder TSX |23 cases:0 PASS/23 FAIL;original source/TAP retained |
| Final new/retained actual TSX |31/31 PASS:23 new +8 retained eligibility |
| Final complete frontend |2,264 PASS/3 FAIL/0 skip/cancel;2,267 tests in84 files |
| Three frontend failures |Genuine QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log absent in Cloud;not fabricated or skipped |
| Types/build/lint |tsc PASS;exact Next16.4.3/React19.2.8 webpack export PASS;0 errors/61 existing lint warnings |
| Static artifacts |87 aliases unchanged;570 export identities unchanged across final browser tests |
| New native Chromium |28/28 PASS at320/light and1440/dark;14 intercepted InApp POST attempts/10 synthetic commits;270 intercepted requests |
| Retained native eligibility |6/6 PASS;two additional synthetic InApp attempts, original cancellation/net/currency assertions preserved |
| Guard |15/15 PASS;actual production guard remains exit1 for five P0 records plus the broader session record |
| All-priority inventory |120 records:5 P0/102 P1/13 P2;zero input errors/inferred closures;26 old metadata entries still need reconciliation |

The first page lint check found ref reads/updates during render. Those updates
now happen only when validated server data is applied; a state value supplies
the visible intent version while refs synchronously protect handlers. No hook
rule was suppressed. Final cases reran on the repaired source. The earlier
channel-only full2,266 API passes are an intermediate result, not added to the
final2,272. Existing eight-case fixtures now use the real minimal lookup and
queuedCount receipt shape; only the invalid-currency label changed to the shared
formatter's normalized uppercase. No financial or cancellation assertion weakened.
The old six-case browser fixture changed only the two GET URLs and receipt count.

API tests use real test-host HTTP/Identity/middleware with isolated EF InMemory,
not real SQL transaction/concurrency/provider proof. Browser state/commits/faults
are synthetic. Normal Chromium sandbox and owned-profile cleanup passed; no
live customer/provider notification was sent. Existing notification rows are
not rewritten; the new guard prevents future unsupported queue requests. No
cross-client or server-wide deduplication/idempotency acceptance is claimed.

## Final behavior

Only null/default or normalized InApp can enter the queue. External Email/
WhatsApp and blank/unknown/malformed channels receive400 before any mutation,
even if no invoice matches. Supported notices store canonical InApp. Existing
academy/module/role gates, currency-normalized paid sums, adjustment/cancellation/
bulk date/targeted invoice rules and mandatory actor/summary audits are unchanged.

The Finance page uses invoice student-options and GET fee-reminders/history.
The latter returns only id/title/channel/status/sentAtUtc from owned reminder
notices, without messages, recipients, templates, variables or unrelated notices.
Actual FinanceUser/Owner/Admin cases pass without Communication enabled; generic
Notifications stays denied. Foreign, anonymous and unprivileged reads are refused,
and read-side snapshots stay unchanged.

No required read or missing balance/unit metadata is guessed as zero ready debt.
Initial failure is retryable without a POST. Versioned pending guards prevent
captured/overlapping target and bulk handlers from resending. A proper queuedCount/
message receipt survives failed refresh as confirmed; unknown transport/408/5xx/
malformed success stays unconfirmed and blocked. Reload reads history and current
records without inferring success from matching counts. Unknown results need
explicit Start a new reminder after successful review before another user intent.
All original targeted/bulk payload and actual eligibility policies are retained.

## Prioritized Desktop return and AI entry

1. Preserve the current Desktop6f C23 SQL return and Cloud C24 invoice paper/PDF
   source. Integrate only the sixteen C25-owned paths; no older protected writer,
   migration, shared governance or SQL-harness file should replace newer local work.
2. On actual disposable SQL/Identity, test default/normalized InApp and external/
   blank/unknown channels for FinanceUser/Owner/Admin. Refusals must leave invoice,
   notification and audit snapshots unchanged; allowed queue keeps exact actor and
   summary audit behavior and canonical channel. Include required-audit rollback.
3. Verify the complete Finance-only feed chain, exact five-field owned history,
   no unrelated/foreign/private content, denied generic Communications, revocation,
   anonymous/unprivileged and missing-module cases. New endpoint needs no schema
   change; no new Communication permission is granted.
4. Run real browser/SQL queue recovery before commit, after lost committed response
   and after acknowledged queue with failed readback. Reload must never re-POST;
   history alone cannot identify a request receipt. Retain C14/C23 eligibility,
   normalized currency and linked financial critical/device acceptance.
5. Finish remaining five P0 and broad-session critical/device/documented acceptance,
   exact hosted CI/current security/unmodified images and staging recovery. Existing
   successful source-bound checks remain useful; no enterprise audit restart.
6. **AI is not next yet.** Use the linked [AI entry checklist](AI_QUALIFICATION_ENTRY_GATE_2026-11-09.md)
   to notify the owner when the non-AI baseline is accepted, or identify a specific
   earlier AI-only dependency. No current blanket AI acceptance or model-loading
   authorization is implied. Mini/Plus/Pro unqualified;Ultra deferred;existing
   stop before loading/training/inference/activation remains. Final AI-inclusive
   release checks still follow any future authorized qualification packet.

Validation ran from the repository (web commands from apps/web):

```sh
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~FeeReminderChannelTests|FullyQualifiedName~FamilyReminderCurrencyTests|FullyQualifiedName~FeeReminderEligibilityTests|FullyQualifiedName~InvoiceCurrencyReadbackTests|FullyQualifiedName~FinanceCurrencySummaryTests'
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --no-build
node --test QA/tools/reminder-queue.test.cjs QA/tools/fee-reminder-eligibility.test.cjs
node QA/tools/run-frontend-tests.cjs
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/reminder-queue-browser.cjs
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/fee-reminder-eligibility-browser.cjs
node --test QA/tools/production-gates.test.cjs
node QA/tools/check-production-gates.cjs
node QA/tools/priority-issue-status.cjs --write
```

Cloud used /workspace/.tools/dotnet/dotnet and named TRX files in the retained
raw directory. Web: `./node_modules/.bin/tsc --noEmit`, `npm run lint`, and
`NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack`.
Production guard and full frontend have the expected blocked/nonzero outcomes
listed above; all failures are retained. No current hosted run is inferred.

Raw original/final source/TRX/TAP, per-file frontend logs, screenshots, synthetic
receipts and export/source/assembly hashes remain outside Git at
`/workspace/c25-reminder-queue-safety-20261109`. QA/EVIDENCE is not published.
Current exact hosted CI remains UNKNOWN; feature push alone does not run the
inherited verify workflow. No main merge/Azure/environment or public-access
change. Desktop retains protected-host/SQL/device ownership;Sol High for the
combined notification/finance/privacy acceptance and next consequential repairs.

## Immutable source pin

Validated implementation/test/checklist/report commit:
`3622cdf647ca631e6506d6243588caf1d7ddbd88`.
Initial claim63bfa88; inherited Desktop integration8479833 preserves6f77a94's
exact five-path SQL/handoff return. Complete C24 source at2114c8c remains present.
The following documentation commit pins SEC-0008's exact fix, regenerates its
inventory hash and records this source. No tested product/test/browser/export
or API assembly changes follow the validation above. All formal acceptance
states remain open. Retained raw hashes and publication record:
`/workspace/c25-reminder-queue-safety-20261109/manifest.json`.

Owner notification: non-AI work is still pending; do not start model execution.
The AI entry checklist names the current baseline gaps and the information that
must accompany a concrete qualification packet when it becomes next.
