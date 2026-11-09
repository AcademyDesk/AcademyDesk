# C23 priority reconciliation and remaining financial readers — 8 November 2026

Cloud owner. IMPLEMENTED / CLOUD VALIDATED; combined real-host acceptance pending.
Branch `codex/cloud-c23-priority-finance-readers-20261108`.
Fresh combined base `b6793d45788b74d8cd5ab82e246c483da8df4aec`, clean before
changes. The old Cloud C22 branch and a pre-switch Git bundle are preserved.
Desktop has integrated C22, passed2,196 combined API and17 expense HTTP/SQL
checks, and six live recovery phases. Its protected writer, payroll, private
recording, audit/SQL and service-restoration work is retained byte-for-byte.
No competing FeeReminders/Portal source claim found in current ownership notes.

Owner now requests all priority issues. Full register has120 non-closed records:
five P0,102 P1 (including the P1 with an explicit P0 session gate),13 P2. This is
not120 unimplemented bugs. Existing scoped fixes and SQL/browser evidence must
be reconciled with the formal original/regression/module/critical/documented
acceptance chain, not discarded or auto-closed.

Exact paths:

- `apps/api/Controllers/FeeRemindersController.cs` — receipt calculation only
- `apps/api/Controllers/PortalController.cs` — invoice projection/download only
- `apps/api/ReadModels/InvoiceReceiptReadModel.cs`
- `apps/web/src/app/portal/page.tsx` — finance totals/history and permitted view only
- `tests/AcademyDesk.Api.Tests/FamilyReminderCurrencyTests.cs`
- `QA/tools/family-finance.test.cjs`
- `QA/tools/family-finance-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — expose actual private View/filter for QA
- `QA/tools/priority-issue-status.cjs`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/ISSUES/INDEX.md` — restore two existing omitted authentication entries; no priority/status changes
- `QA/ISSUES/BUG-DATA-0001.md` — append this source-bound scope, retain local evidence
- `QA/ISSUES/BUG-DATA-0016.md` — append, do not replace newer local records
- `QA/HANDOFF/C23_PRIORITY_FINANCE_READERS_2026-11-08.md`

P0-related readers still group all units/academies by invoice ID. Only accepted,
same-tenant, same-invoice, normalized matching-currency receipts may reduce the
invoice. Reuse one bounded read helper for reminder/portal/download agreement.
Preserve raw rows, adjustments, per-invoice clamp, invoice history and existing
reminder target/due/cancellation/channel/audit/transaction rules. No provider
send, financial writer, role/tenant/parent/finance/document authority change.
Portal fee summary must retain units/cents, exclude cancelled receivables and
not call cancelled invoices Paid; missing metadata is unavailable, no FX/INR
guess. Respect the existing parent finance permission; expose its current server-derived
canViewFinance capability as an additive response flag so a fresh denial cannot
be mistaken for a zero balance after an older parent-profile snapshot. Downloaded HTML must
encode raw currency and label cancellation history versus collectible amount.

Add a deterministic read-only inventory of every issue's actual recorded status,
priority, source/evidence links and quoted remaining acceptance. Historical
metadata conflicts must remain visible; no parsed mention of PASS implies full
verification. No automatic status/priority change, skip, exception or closure.

Validate before/after actual HTTP/Identity/InMemory and current linked student/
guardian/permission/download/queue cases, actual TSX and real Chromium with
synthetic API data, combined build/types/lint/full suites. Actual SQL/provider/
critical/physical-device/release CI/images/staging evidence remains explicitly
required and Desktop-owned. Keep five P0s and applicable issue records OPEN.
Stop before model loading/training/inference/activation; Mini/Plus/Pro unqualified,
Ultra deferred. No main/Azure/environment/private/public-access changes, customer
writes, raw evidence publication, new dependencies/images or external audit.
Sol High for money/privacy/authority; Medium for settled inventory/docs.

## Recovery checkpoint — 9 November 2026

Recovered the same workspace after executor startup/capability failures. Before
interruption, original38 API cases had13 PASS/25 FAIL; final target95/95 and
18 actual-TSX checks passed. These receipts and all working files survived.
Native browser, complete suites and final publication were still pending.

Latest Desktop9c9658132446124b70c53467e940d9769cf97d44 adds production runtime
evidence and a stricter broad-session release gate. It was imported exactly
with cherry-pick provenance, preserving all nine files without new edits. This
retains the five P0 records plus required session acceptance; it does not close
them. C23 still owns only the explicit scoped changes listed above.

Inventory found existing BUG-AUTH-REFRESH-001 and BUG-AUTH-REVOCATION-001 files
missing from INDEX. Before editing INDEX, its claim is added here. Restore their
visibility with recorded bounded evidence and preserve the revocation record's
P1 priority plus its separate P0 session gate. No issue acceptance is inferred.

## Implemented source and original reproduction

Checkpoint `8767c3a5354708db7a2c423c36048bb6dccc34ed` is verified on the
feature branch. It includes the production repair and first38 API/18 TSX cases;
one additional current-capability contract case is included in the final suite.
No original assertion is removed. Original controller/page bytes and original
38-case input are retained separately in the raw evidence directory.

The common receipt reader scopes every payment by academy, authorized invoice
IDs, Completed/Reconciled status and trimmed case-insensitive currency. Different
units and foreign-academy receipts cannot reduce family balances, invoice HTML
or reminder debt. Adjustments and per-invoice zero clamps stay intact. Reminder
eligibility/channel/notification/audit/write policy is unchanged; test sends are
synthetic InApp records only. No external provider was contacted.

Family UI groups actual units with cents, omits cancelled receivables and labels
cancelled history without calling it Paid. Paid/Outstanding and cycle/reset paths
retain their meaning. Missing money metadata is unavailable, not a guessed INR
or zero debt. Current server-derived canViewFinance overrides an older parent
snapshot; denied finance also skips the invoice query. Existing stored guardian,
student, academy, link and document authority still decides access. Download HTML
encodes raw currency and separates historical cancellation balance from Amount
due. No financial writer, schema, model, release workflow or Desktop gate was
changed by this packet.

## Current verification — 9 November 2026

| Check | Actual result / limit |
| --- | --- |
| Original HTTP/Identity/InMemory reproduction | 38 cases:13 PASS /25 FAIL; retained c23-original.trx and original source |
| Initial repaired targeted API | 95/95 PASS; new38 plus57 retained finance/reminder/readback cases |
| Final targeted API | 96/96 PASS; one additional fresh denial→grant capability case; no skips |
| Full current API | 2,235/2,235 PASS; no failures/skips; fresh final binaries |
| Original actual TSX View/filter | 18 cases:1 PASS /17 FAIL; retained original TAP |
| Repaired actual TSX View/filter | 18/18 PASS, also included in full frontend |
| Full current frontend | 2,216 PASS /3 FAIL /0 skip/cancel;2,219 tests in82 files |
| Three frontend failures | Genuine QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log absent in Cloud; original failure assertions retained |
| TypeScript | tsc --noEmit PASS |
| ESLint | 0 errors /61 existing warnings |
| Exact-lock static web build | Next16.4.3 /React19.2.8; webpack export PASS;87 static aliases unchanged |
| Real sandboxed Chromium | 24/24 PASS at320/light and1440/dark; four invoice downloads; strictly intercepted GETs, zero API mutations; owned profile removed |
| Inherited production guard tests | 15/15 PASS |
| Actual production guard | Expected exit1: five P0 records plus BUG-AUTH-REVOCATION-001 remain open |

Commands ran from the repository except web build/type/lint from apps/web:

- `/workspace/.tools/dotnet/dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~FamilyReminderCurrencyTests|FullyQualifiedName~FeeReminderEligibilityTests|FullyQualifiedName~InvoiceCurrencyReadbackTests|FullyQualifiedName~FinanceCurrencySummaryTests'` with named TRX output.
- Same project `--no-restore --no-build` for the full suite after final rebuild.
- `node QA/tools/run-frontend-tests.cjs` (expected three missing-receipt failures).
- `node --test QA/tools/production-gates.test.cjs`; `node QA/tools/check-production-gates.cjs` (expected BLOCKED).
- `./node_modules/.bin/tsc --noEmit`; `npm run lint`;
  `NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack`.

Raw logs/TRX, preserved source inputs, frontend per-file logs and export hashes
remain outside Git at `/workspace/c23-priority-finance-readers-20261108`.
No raw QA/EVIDENCE is published. HTTP cases use actual authenticated test-host
Identity/middleware with EF InMemory; they establish no SQL transaction, migration,
concurrency or provider acceptance. The newer Desktop production container report
is inherited evidence for its exact prior source, not a current C23 container run.

## Complete issue inventory and next acceptance owners

The companion JSON/Markdown reconciles all120 issue files against INDEX with
zero missing/duplicate/unrecognized inputs:5 P0,102 P1,13 P2; zero literal
CLOSED+PASS records. Twenty-six old header metadata entries need reconciliation
with their appended scoped evidence. These counts are acceptance records, not
120 untouched defects. Do not discard accepted source-bound work or auto-close.
Each entry retains exact issue hash, recorded verification, latest section,
report links and quoted historical pending statements. File presence alone is
not a test verdict. Rerun the generator after issue edits.

Prioritized remaining tasks:

1. **Desktop / C23 family-reader acceptance:** integrate the feature return;
   run real SQL/USD100.55-adjustment0.05 with matching20.10, wrong-unit70.15,
   foreign-academy500 and all six receipt statuses. Student/linked guardian JSON,
   invoice HTML and authorized reminder must agree on80.40 for accepted receipts
   or100.50 for unaccepted ones. Verify no read-side ledger/audit changes and
   exactly one correctly scoped notification/audit for an eligible queue. Preserve
   overpayment clamp, original C13/C14 and newer protected writer/race acceptance.
2. **Desktop / permission and browser:** fresh finance denial and later grant,
   revoked/unlinked/foreign students, anonymous/unprivileged users and invoice
   downloads against real Identity/SQL. Actual family screens must display mixed
   units/cents, cancelled/history filters and honest denial with no false zero.
3. **Desktop / critical launch blockers:** finish DATA-0001/0002 linked finance
   critical/device cases; DATA-0003 payroll identity/net/history/print; DATA-0010
   audited void/evidence restore; SEC-0001 private-media recovery and physical
   microphone/mobile/tablet upload/view/download. Preserve existing passed SQL,
   live-browser and fake-permission results rather than repeat accepted work.
4. **Desktop / session:** complete broad SECURITY-SESSION-001 acceptance and
   document BUG-AUTH-REVOCATION-001 independently. It remains P1 in the register
   and a P0 release scenario. All six guard records must satisfy final acceptance.
5. **Release integrator:** obtain exact-release hosted CI/unmodified API+web
   images/security and staging backup/restore/rollback evidence, then reconcile
   the120 records through their original→cause→fix→regression→module→critical→
   documented/device chain. Current remote CI is UNKNOWN; a feature push is not
   a verify.yml run (push only main or codex/release-export-security, PR main or
   explicit dispatch). No workflow dispatch or PR was made here.
6. **AI qualification:** Mini/Plus/Pro remain unqualified; Ultra deferred. The
   90% eligible conversational outcome target is not measured acceptance. Real
   model work stays blocked by the existing stop before loading; no model was
   downloaded, loaded, inferred, trained or activated.

Main, Azure, protected SQL/migrations, laptop D:, privacy and environment settings
are unchanged. The same Cloud executor is running with current enforced restricted
network policy and no observed failure. That is not proof of laptop TCP/service
reachability or an explanation for earlier infrastructure startup failures. No
new public access, environment publication/replacement or unrelated reset/cleanup.

## Native browser result and retained harness failures

`QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/family-finance-browser.cjs`
passed24/24 with no runtime/interception/console errors and normal Chromium
sandbox. At both320px light and1440px dark it exercised student/guardian mixed
units, USD cents, empty history, cancelled positive/zero history, missing/legacy
metadata, finance denial, stale allowed parent→current denial, stale denied
parent→current grant, All/Paid/Outstanding/cycle/reset and actual Blob downloads.
Four synthetic HTML fixtures were fetched with the Portal token and downloaded;
this validates UI wiring and bytes, not the real server HTML content. The separate
HTTP tests validate the actual encoded server document and authorization.

First native attempt failed24 scenarios because the harness incorrectly read
case-sensitive Authorization instead of Chromium's lowercase header name. The
second passed21/24; three repeated filenames overwrote the first downloaded file,
so a new-file assertion failed. Final harness treats HTTP header names case-
insensitively and gives each download scenario its own owned directory. Expected
bearer, exact download count and content assertions remain. Both failed receipts
are retained with successful owned-profile cleanup; no product change or weaker
assertion was needed. Final24/24 is the only native acceptance receipt.

All570 export file hashes match before/after testing. Desktop9c's nine imported
files remain byte-identical. No vendor or protected financial writer/migration
path changed. Mini and the old C5 worktree remain clean. The full120-record
inventory regenerates byte-identically; all referenced local report files exist,
which establishes reference presence only. Raw original/final TRX/TAP, full suites,
browser screenshots/downloads/failure receipts and hashes stay outside Git.
