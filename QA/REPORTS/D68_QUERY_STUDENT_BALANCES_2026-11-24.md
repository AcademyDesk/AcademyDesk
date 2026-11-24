# D68 — permission-bound student balances in the connected conversation

24 November 2026. Academy base `c0817b0ed0cc9dcfb47f2cb1f2ef1fbb371d417e`;
Mini base `30b88db356d3e22653dca706ffb4212c7bf37cb6`. Continues D67 and the
existing enterprise program. Mini published `f8ff21ec996cbf659c0062ce5356f006b3f46f39`
on `codex/academy-conversation-core-20261123`; paired CI pins that exact source.
No main merge, Azure action, production data change or model experiment.

## Completed scope

- [x] Added `student-fees` search/open to the SAME `academy-query.v1` catalog,
  context, encrypted history and recovery path. Current stored Finance/module
  permission controls availability: eight tools with Finance, six without it.
- [x] Reuses `OutstandingFeesService` and existing `PentaMiniMoney` projection;
  no new ledger calculation, financial policy, currency conversion or model math.
  Canonical decimal strings remain strings through host receipt, history and UI.
- [x] Explicit fee follow-ups can open the same displayed student through a saved
  host reference. Teacher/batch references cannot become students. Financial
  questions cannot silently return directory data; ordinary directory requests
  cannot disclose money or silently acquire a financial context.
- [x] Independent current authority applies before retrieval and saved receipt
  recovery. Finance revocation denies an earlier financial receipt while allowing
  a fresh authorized teacher read. Model/context/claims cannot supply permission.
- [x] Financial receipts validate currency uniqueness, canonical representable
  precision, typed rows and scope. Ordinary directory wire shape omits balances
  and scope, and rejects unexpected financial fields on the client.
- [x] UI shows exact amounts separately by currency, source and scope. Subject
  filters select students; balances cover all their outstanding fees, not just
  course-specific invoices. First-ten results explicitly disclose more matches.
- [x] Mobile screenshot review found auto-scroll hiding the amount rows. The
  latest answer now starts at the top of its transcript; actual browser visibility
  assertions cover the money rows and retained composer separation.

## Final source validation

| Layer | Observed final result | Limit |
| --- | --- | --- |
| Affected .NET | 280 passed, zero failed/skipped | 16 added cases; query class now 60 |
| API/test/harness build | Zero warnings/errors | Current local source |
| Mini Linux | 244 passed, zero failed/skipped; one inherited Starlette/httpx warning | 37 query cases plus retained task/API/native/operational selection; model-free |
| C# -> Python schema | Actual eight-tool host request accepted | No model execution |
| Client/parser | 116 passed | 42 query/workspace/portal + 74 retained legacy/exact-money checks |
| Native SQL/Identity/API | 24 query checks passed; no HTTP429 | Real owned SQL/authentication/HTTP; explicit synthetic planner |
| Existing Foundation | PASS, including 5/5 stale-tab races | Retained budgets/approval/recovery/audit checks |
| Migrations and cleanup | 86 application + 7 Identity migrations PASS | Owned database/login/container removed only |
| Exported query browser | 16 passed | Finance eight + directory eight; Owner/Admin, 320/1440, light/dark; synthetic API |
| Existing shell browser | 36 passed | Six roles, three widths, both themes; synthetic API |
| Next 16.4.5 compatibility build/typecheck | PASS, 92 exported pages | Existing D: cache, not fresh locked install |
| Changed frontend ESLint | PASS, zero warnings | Same isolated cache |

Final native run `c99d01f7a022409d89f1ca7cb4d3d976`, 43.3 seconds. Invoice1000
minus Reconciled600 yields INR400.00; Pending500 is excluded. USD0.01 and
EUR90071992547409.91 stay exact and separate; foreign-tenant GBP999 is excluded.
Twelve matching owned students verify first-ten/HasMore disclosure. GET recovery
does not dispatch another planner call. Audit failure/rollback, current actor
revocation and unchanged ledger rows/totals remain checked. Earlier 23-check
run overlaps this one and must not be added to its count.

Private evidence `.build-check/d68-finance/`: final TRX
`d68-affected-final-source.trx`, `mini-finance.xml`, `host-query-vector.json`,
`native-result-1795500796742.json` and matching log. Final screenshots:
`browser/2026-11-24T06-12-10.712Z-14568/`,
`directory-browser/2026-11-24T06-12-29.070Z-14304/`; shell screenshots remain
`.build-check/penta-workspace-shell/2026-11-24T06-12-42.503Z-12556/`.
No raw evidence, passwords, customer prompts or weights are committed.

## Failures retained and corrected

1. Initial C# local names clashed with enclosing declarations; renamed them.
2. An EF InMemory ledger test could not translate the real SQL collation. Kept
   production collation/query unchanged. Moved exact ledger assertions to real
   disposable SQL; retained meaningful pre-ledger permission denial and directory
   privacy unit tests. Failed TRX `d68-affected-final.trx` remains private; final
   tests have no skips or weakened money/tenant assertions.
3. Initial mobile view scrolled past amounts to the bottom of the answer. Fixed
   transcript focus and identity wrapping, added actual row-visibility predicates,
   then reran both query variants, shell checks, lint and full compatibility build.

## CI: current versus historical

Prior Academy D67 `c0817b0` ordinary job succeeded:
https://github.com/AcademyDesk/AcademyDesk/actions/runs/45334213901
with 2,738 API tests, locked npm-ci/audit (zero vulnerabilities), typecheck/lint,
guarded SQL/private-media checks and full export. This resolves the D67 locked
dependency gap, not the new D68 exact-source gate. Current local compatibility
cache differs from the lock in development/optional dependencies; fresh D68 CI
is required after publication. The native CI completion guard now requires 24.

Required paired Core job still fails missing dedicated read-only
`PENTA_SOURCE_READ_TOKEN`. Keep it required; owner must securely configure the
Actions secret with private PentaMini Contents read access. Connector access is
not an Actions credential; never substitute a broad credential or print it.

Prior Mini D67 run https://github.com/AcademyDesk/PentaMini/actions/runs/45333834037
reported 13 failed, 1,871 passed, one skipped, 63 errors and five passed subtests;
original missing archive/evidence files remain genuine blockers. New query cases
passing do not qualify that full suite. Preserve original bytes/hash requirements;
do not fabricate files, alter pins or skip assertions. D68 exact CI is pending
publication and must be checked independently.

## Configuration and next work

Default-off Dev/Testing setup remains D67's: integrated Mini workspace entrypoint
and query flag, host query flag/explicit loopback endpoint, current real Identity
and SQL, isolated frontend evaluation flag. No new model call/load/activation;
the completed single 4B comparison is not continuing approval for another packet.
No live interactive model/demo qualification or credentials are claimed here.

- [ ] Qualify the connected pipeline against an explicitly approved real model:
  teacher correction, student/batch/fee follow-ups, context and natural answer
  quality/latency. Synthetic planner/API results cannot establish these outcomes.
- [ ] Attendance/calendar and remaining D66 families in the same catalog; broader
  finance aggregates/invoice/expense/payroll operations are not supplied by D68.
- [ ] Bind protected dependent task approval/execution and compound student +
  batch + enrollment; no mutations were enabled by this read-only change.
- [ ] Teacher/Student/Guardian/Platform parity, real SQL/browser combined outcomes,
  physical devices, original critical issue records and staging/restore/security
  release gates. Keep BUG-AI-0001 OPEN and the 90% eligible outcome target unclaimed.
- [ ] Exact-source CI, paired private source credential and genuine original
  archives remain release blockers. Stay Sol High for next integration work.
