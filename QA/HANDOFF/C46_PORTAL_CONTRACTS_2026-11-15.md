# C46 — Desktop portal response contracts

Cloud / Sol High,15 November2026, Asia/Calcutta.
Base `48f735f9f7189879796b3dca9d866831a3d067aa` on Desktop's published
`codex/penta-host-integration-20261030`. Branch:
`codex/cloud-c46-portal-contracts-20261115`.

## Published ownership before implementation

Only these paths are claimed:

- `apps/web/src/lib/penta-portal-reads.ts`: strict response validation only.
- `QA/tools/penta-portal-contract-boundaries.test.cjs`: new failure regressions.
- this task-specific handoff.

Desktop has completed Student/Guardian guided reads and owns the next separately
scoped Platform Owner flow. No portal API, SQL, migrations, role entry, composer,
Platform Owner or shared root tracker edits are claimed. C45's duplicate portal
implementation was preserved locally at33f5c61c and in a hashed comparison ZIP;
only its claim release was pushed. Do not integrate that private duplicate.

Scope: reject extra scope/data in malformed portal response envelopes,
contradictory upcoming dates and noncanonical/zero balances; preserve valid
Desktop responses, fractional timestamps and exact currency strings. Add actual
runtime regressions before fixing the validator; no new host or model behavior.
Run the focused and full frontend suites, types, lint and exact-lock build.
Existing native Desktop SQL/browser evidence stays source-bound and is not
rerun or relabeled by this client-only packet.

Mini/Plus/Pro remain unqualified; Ultra deferred. The90% target is unmeasured.
Five P0 records and the broader session gate, exact release CI/images,
staging restore/rollback and physical devices remain open. No model loading,
inference, activation, main merge, Azure deployment or environment publication.

## Completed —17 November2026

The Cloud checkout resumed at claim commit
`e1c9b1698eb41466e358fcdb4b9726ac32a5ec81`, with only the saved validator edit
and new test file. The claim was published before these edits. C45's duplicate
implementation was not reapplied. Its comparison remains backed up at local
commit `33f5c61c9284e7c817199c940d7a8e2110103bd6` and in the private comparison
ZIP. Its remote claim release is `bbc9964c`; no duplicate portal host was pushed.

### Runtime failures reproduced and fixed

The new tests initially ran against Desktop's unmodified validator:35 cases,
17 PASS /18 FAIL. The failing cases demonstrated acceptance of extra response
properties, invalid caller IDs that matched an invalid response, unsupported
runtime read kinds, inconsistent truncation, dates earlier than the response's
own `asOfUtc`, and noncanonical balances such as `00.00` or `01.00`.
These are client contract defenses; no new server authorization exploit or
new production-qualified model behavior is claimed.

The validator now requires the exact existing envelope fields and valid caller
scope, admits only the three fixed read kinds, and requires a full ten-row
window when `hasMore` is true. Upcoming classes/work cannot precede their
response's check time. UTC ordering preserves all seven fractional timestamp
digits, rather than letting `Date.parse` discard four SQL ticks. Equivalent
fractional representations still compare equal. Outstanding fee balances must
be canonical positive decimal strings; large exact amounts are never converted
to JavaScript numbers. Valid current Desktop payloads remain unchanged.

The API, Identity/guardian policy, ledger, SQL transaction, composer, route entry,
model protocol, dependencies and all shared root trackers were left unchanged.

### Verification

Private raw results: `/workspace/c46-portal-contracts-20261115`.

| Check | Result / exact boundary |
| --- | --- |
| New failure regressions |35/35 PASS, zero fail/skip; original17/18 failing baseline preserved |
| Existing Desktop portal tests |5/5 PASS |
| Existing shared composer prompt tests |7/7 PASS |
| Full backend on the C46 base |2,493 PASS, zero fail/skip; freshly rebuilt after switching to Desktop48f735f9. This does not qualify SQL or the later Platform source |
| TypeScript |PASS |
| ESLint |Zero errors,61 inherited warnings |
| Exact-lock Next webpack production export |PASS, Next16.4.3 / React19.2.8;90 static segment aliases; no font or source mocks |
| Full frontend on C46 source |90 files /2,497 tests:2,494 PASS,3 FAIL, zero skip/cancel |
| Retained frontend evidence failures |Email, WhatsApp and Meeting historical HTTP receipts still require missing `QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`; existing48-case file remains45PASS/3FAIL. No receipts, expected results or skips were fabricated |
| Production guard tests |15 PASS; actual checker still exits1 for the six open critical acceptance records |
| Desktop native evidence |Retained, not rerun by Cloud: Portal36 focused API /61 native HTTP-SQL /3 live browser groups at48f735f9; see its source-bound report |

On15 November, automatic approval review could not run the proposed full build,
frontend runner or CI query because the account hit a usage limit. Those actions
did not execute. They completed successfully on17 November after the environment
resumed; the frontend's three genuine receipt failures remain as recorded.
No workaround or alternate path bypassed that approval failure.

### Newer Desktop source and integration handoff

Read-only fetch on17 November found Desktop at
`a7e0b06e92bb851b7adbc9468353b095fca0c1c4`, completing the separate Platform
Owner conversation. Its changed paths do not overlap any C46 claimed file.
Its report `QA/REPORTS/PENTA_PLATFORM_CONVERSATION_LIVE_2026-11-15.md` records
8 client,55 native,3 live browser and24 controlled role-entry groups.
These are Desktop's results, not Cloud reruns or evidence for the changed C46
validator. Initial guided workflows now have recorded evidence in all four
portals. This is not all eligible operations, model qualification or measured90%.

Integrate only C46's implementation commit into that newer Desktop feature
checkpoint. The three changed paths are this handoff, the validator and the new
test file. Do not merge the private C45 comparison or restore its old Portal
mount. Run the35 new,5 retained portal and7 shared prompt tests, then the built
Student/Guardian browser against disposable SQL to confirm current valid
responses and malformed-response fallback on the integrated candidate. Preserve
the current role/linkage revocation and platform/academy boundaries.

### Exact hosted CI and remaining production blockers

GitHub read and feature push work through the managed outer executor. The
selected environment is running/connected and reports restricted networking;
after resume the enforcement observation is unknown, so it is not represented
as newly verified enforcement. No environment setting, privacy, VPN, public
internet permission, model or deployment setting was changed.

The exact C46 base48f735f9 has0 GitHub Actions runs and a successful Vercel Preview
Comments check. Preview success does not establish backend or release CI.
The existing verify workflow runs on main or its named release branch, PRs to
main, or explicit manual dispatch; a C46 feature push is not a test run. No
workflow dispatch, PR, main merge or Azure deployment was performed.

Earlier exact Mini API checks remain dated15 November: Tests failed at current
Cloud14c11fcf ([run42234314840](https://github.com/AcademyDesk/PentaMini/actions/runs/42234314840)),
Desktop returne55e5b96 ([run42158243325](https://github.com/AcademyDesk/PentaMini/actions/runs/42158243325))
and launch933a7bf4 ([run41959954805](https://github.com/AcademyDesk/PentaMini/actions/runs/41959954805)).
The launch branch's separate no-model protected-host run41959954960 passed.
The Cloud branch's unit job failed at pytest; its detailed log redirect was
policy-blocked403, so no detailed hosted cause was inferred from old local logs.

Prioritized acceptance, Sol High:

1. Apply the bounded C46 source to the current integrated feature checkpoint
   and complete the source-bound browser/SQL acceptance above.
2. Investigate exact failing Mini CI and reconcile all parallel source returns.
   Prepare the first private learner-search qualification packet. The explicit
   model-load/inference stop remains; no prepared model was activated here.
3. Finish current-candidate acceptance for collection `BUG-DATA-0001`, adjustment
   consumers `BUG-DATA-0002`, payroll `BUG-DATA-0003`, void/restoration
   `BUG-DATA-0010`, private media `BUG-SEC-0001`, and broader session revocation
   `BUG-AUTH-REVOCATION-001`. Their bounded retests do not close the records.
4. Run exact integrated-candidate hosted CI, API/web image/security checks,
   physical-device coverage and staging migration/restore/rollback. Only then
   assess production launch and Hayansh onboarding under separate authorization.

SOURCE CONTINUATION / NOT PRODUCTION QUALIFIED. Mini/Plus/Pro are unqualified;
Ultra is deferred. The90% target remains unmeasured. Main, Azure and model
activation remain stopped.
