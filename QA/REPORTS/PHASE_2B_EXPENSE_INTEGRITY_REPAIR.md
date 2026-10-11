# Expense integrity and recording workflow — BUG-DATA-0047

Date: 2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `b864ef0a540d804bab070e2a85f9c5bdd0bfd40f`. Model routing: Sol High for finance/authorization verification. This continues the existing E0/E1 queue, not a restarted audit or testing strategy.

## Scope and baseline

- The unchanged expense controller fails 23 of 32 new tests (9 pass). These are assertions/cases, not 23 independent defects. In particular Update accepts foreign/missing/empty branch references and fabricated statuses; creation/update also lack database field/decimal guards.
- Frozen starting expense UI: 1 of 18 original feedback checks passes, 17 fail. Missing new UI controls also fail structural assertions; this is not 17 distinct proven production failures. Early test-driver failures were corrected to preserve React callback identity and modern iterator semantics; assertions were not weakened.
- The first linked browser attempt discovered a genuine additional flow gap: `/branches` is gated independently by MultiBranch/permissions, so an authorized expense user with a 403 branch lookup could not record even a no-branch expense. Native expense checks passed, but that browser attempt timed out and was NOT accepted.
- A subsequent run added valid/valid concurrent replacements and failed its whole-row/currency/audit invariant on attempt 2. EF changed-column replacement writes can mix fields read from an older snapshot; the repair now serializes the expense read through the commit/audit boundary rather than relaxing this check.
- Linked screenshot review also reproduced binary-number rounding of the valid maximum decimal. The expense summary now includes additive `amountExact` text while retaining numeric `amount`; the expense UI uses integer cents, exact grouped totals/formatting and a validated decimal JSON-number lexeme for submission. No currency conversion or financial-policy change is introduced.

## Repair

1. Shared create/update validation: required trimmed description, maximum 300 characters; category maximum 80 with optional blank/null normalized to General; positive amount within existing decimal(18,2), with overprecision/overflow rejected rather than silently rounded/stored.
2. Update validates supplied branch ownership/existence before mutating the tracked expense. Owned inactive branches remain valid for historical expenses, matching creation's existing policy; null remains permitted.
3. Explicit status is limited to the existing canonical Recorded value. Null/blank omission preserves existing historical status. No Paid/Voided workflow, accounting policy, migration or legacy data rewrite is introduced. Currency is immutable in Update; Create defaults blank currency to INR or accepts a normalized three-ASCII-letter code, not an asserted ISO currency whitelist.
4. Existing authenticated role/tenant/module enforcement and finance transaction/audit boundary are retained. Update acquires a parameterized tenant-scoped SQL UPDLOCK/HOLDLOCK before reading the expense; the host transaction retains the lock through audit commit. Direct SQL callers without that transaction own a local boundary, matching the existing payment pattern; other test providers retain their existing query path. Rejected requests change neither captured domain rows nor success audits. Audit insert failure rolls back creation/update.
5. Expense form has checked readiness, synchronous duplicate-submit/readback guard, disabled controls while pending, accessible durable status, failed-draft retention, captured-field reset after a confirmed save, explicit confirmed-save/failed-refresh and uncertain-response warnings, and no automatic write retry. An uncertain result requires explicit register refresh/review before recording again.
6. Optional branch lookup 403 disables branch selection with an explanation, but permits an already-authorized no-branch expense. Expense denial, branch 401/5xx and invalid initial lists still block the form; no host permission is bypassed.
7. Existing style/token/control system remains. Exact integer cents are displayed and totals are grouped by currency without implying FX conversion; invalid historical currency labels are not silently relabelled INR. Numeric-only legacy responses are accepted only within the safe amount bound; unsafe values display unavailable rather than inventing a rounded value. Narrow module CSS prevents long descriptions/large currency totals overflowing. This is not the all-portal Orbit/Aura/Prism redesign.

## Verification

| Layer | Result |
| --- | --- |
| API build / SqlHarness build | PASS, 0 warnings / 0 errors |
| Expense tests | Original 32/32 plus additive exact-summary case: 33/33 PASS |
| Expense + unchanged AuditOutcome suite | 69/69 PASS |
| Actual expense TSX controlled-handler / exact-money tests | 21 + 24 = 45/45 PASS |
| TypeScript / targeted expense page ESLint | PASS; lint 0 errors / 0 warnings |
| Production webpack build/export | 83/83 pages PASS |
| Final native SQL/API | 66/66 PASS, including ten concurrent replacement controls and audit rollback/recovery |
| Linked real SQL/API browser | Four phone/desktop light/dark journeys PASS; five real saves including maximum decimal, four foreign-branch no-writes; no 429 |
| Responsive exported browser, controlled transport | 54/54 PASS at 320/768/1440px, light/dark, nine scenarios |

Native module exercises create/update/GET tenant matrices; anonymous, Teacher and Student denials; foreign actors/routes/records; missing records; suspended/module-disabled academy; exact decimal boundaries; local/null/inactive branches; optional/legacy status; unchanged currency and foreign sentinels; success audits; SQL DENY INSERT audit rollback/recovery. Five concurrent valid/invalid pairs persist only the valid replacement; five valid/valid pairs preserve a complete last-writer replacement and both audits. There is no new optimistic concurrency/merge guarantee.

The linked browser uses actual export, real Identity logins and authorized ASP.NET endpoints against run-owned SQL. A loopback URL relay points the export's API requests at that owned host; it never mocks response data or supplies authoritative roles/tenants. Four phone/desktop light/dark saves plus one maximum-decimal save/display, denied optional branch lookup, four foreign-branch rejections, exact fresh SQL/audit deltas and unchanged historical/sentinel/other ledgers are asserted. Production throttles remain unchanged; the browser stage waits one existing 60-second limiter window after the API matrix, and 429 is not accepted.

The responsive controlled-transport matrix covers 320/768/1440px, light/dark, confirmed save, failed readback, 400 rejection, committed-but-500 uncertainty, response loss, initial load failure, pending duplicate actions, optional branch 403 and exact maximum money submission/readback. This UI matrix is not real SQL or physical-device evidence; the separate linked browser is.

## Execution and cleanup

- Initial native-only run `5d584f38216a406a94c880db110a4fd4`: 61/61 PASS, migrations 88 application + 7 Identity, owned database/login/container removed.
- Intermediate linked run `e9cf3dc7a39848df97a96cf4b49fe2c9`: 61 native checks PASS, linked browser NOT PASS due the optional branch 403 readiness bug. Failed evidence retained. Its exact labelled loopback container was inspected (no mounts) and removed; no other container was pruned or removed.
- Intermediate concurrency run `9166a87fa7fd42b7963ec3466b28239e`: 61 original native cases plus the first valid/valid pair PASS; second valid/valid pair failed the unchanged invariant. Linked browser was not reached. Its exact labelled loopback container was inspected (no mounts) and removed; failed evidence retained.
- Intermediate locked run `7b9126dbfaf44e359651c707d45a4bf2`: all 66 native cases PASS; two linked phone cases passed, third linked case timed out awaiting save feedback, so the linked matrix was NOT accepted. This led to explicit active-failure diagnostics/limiter-window pacing and exact-money hardening. No unsupported definitive cause is assigned to that timeout. Its exact labelled loopback container was inspected (no mounts) and removed; failed evidence retained.
- Final run `9b58a8ddbad64e4bb5c690262c8c5785`: 66 native cases and all four linked journeys PASS; five real authenticated form saves (including maximum exact decimal) and four rejected foreign-branch writes verified against fresh SQL and exact audit deltas. No 429. Application/Identity migrations 88/7 and scoped login verified; negative mismatched-marker cleanup refused, then owned database/login/container removed. Exit 0, 139.2 seconds. No other container was removed.
- Final controlled browser evidence: `QA/EVIDENCE/expenses-feedback-browser-1791693970183/result.json`, 54 cases. Linked evidence: `QA/EVIDENCE/expenses-live-1791694077039/result.json`, four journeys; phone light maximum-money screenshot inspected for exact display, successful feedback and no horizontal overflow. Evidence and TRX/log files under `QA/EVIDENCE/expense-integrity-20261011/` remain intentionally local. `.build-check` remains ignored.
- The historical QA inventory validator binds to its recorded commit/source inventory; this packet does not regenerate that accepted audit or claim the frozen inventory is current. Targeted report/issue/checklist consistency and diff checks are used for this successor.

## Compatibility and limits

- POST remains 201 with the summary and additive `amountExact` field; PUT remains 200 with an empty body; GET has the same additive exact field. Numeric `amount`, numeric request contract, routes/date defaults and replacement semantics are retained. No new update/delete editor, refund/credit/void lifecycle, expense idempotency protocol or FX/tax engine is claimed.
- Exact expense rendering/aggregation/submission is covered here, not an application-wide migration of monetary contracts or an accounting/tax certification. Other finance modules retain their own existing gates.
- Server idempotency, cross-tab duplicate intent, physical Android/iOS, full critical regressions and the final accounting/production release gate are not completed by this bounded packet.
- BUG-DATA-0047 remains OPEN with RETEST disposition, pending the established issue closure gates. No Azure deployment, main merge, production database/credential change, PENTA Core change or Mini handoff. Existing interrupted Mini and Platform work is preserved and excluded from the scoped publication.
- NEXT: existing BUG-DATA-0048 work-item reference/assignee ownership reproduction and repair, Sol High. Manual shared visual foundation and Platform → Admin → Teacher → Student rollout remain queued; AI frontend remains owned by the PENTA AI project.

## Reproducible commands

```powershell
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/expense-integrity --filter "FullyQualifiedName~ExpenseIntegrityTests|FullyQualifiedName~AuditOutcomeTests"
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/expense-integrity-sql
# First build/export apps/web using its installed toolchain:
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module ExpenseIntegrity
node --test QA/tools/expenses-feedback.test.cjs
node --test QA/tools/expense-money.test.cjs
node QA/tools/expenses-feedback-browser.cjs
```

The ExpenseIntegrity runner now requires the built web export for its final real-browser gate. It is not a synthetic fallback when the export/browser is unavailable. Owned SQL safety/cleanup guards remain enforced.
