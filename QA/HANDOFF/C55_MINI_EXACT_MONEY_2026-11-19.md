# C55 — exact money in private Mini read receipts

MODEL: Sol High
TOOL: Cloud Codex
BRANCH: codex/cloud-c55-mini-exact-money-20261119
DESKTOP: 219bba2971b30321f57a5f509e19e9704d6ee1b7
BASE: f9975dd2 (reviewed C54 return applied on current Desktop source)
STATUS: IMPLEMENTED / bounded validation passed; combined Desktop acceptance pending
COMPLETED: 23 November 2026, Asia/Calcutta

This report completes the 19 November ownership claim. Its original Desktop
checkpoint was 219bba2971b30321f57a5f509e19e9704d6ee1b7. A fresh 23 November fetch
found Desktop 69a5794243788905387f34f638c3062225416c0f. Its newer evidence below
supersedes the claim's old statements that native C54 and all local model
evaluation were pending. No new model loading is authorized or performed here.

C54 was preserved during the prior GitHub503 outage and is now published at
2fa56f86ddc947957d87646c22d06237030c8939. Its two task commits apply cleanly on
219bba29. C53 provider/test bytes match Desktop exactly; no duplicate C53 import.
Cloud changed only:

- apps/api/Intelligence/Penta/PentaMiniMoney.cs (new invariant decimal-string projection).
- apps/api/Intelligence/Penta/PentaMiniOrchestrator.cs (computed read-only receipt property).
- tests/AcademyDesk.Api.Tests/PentaMiniMoneyTests.cs (new precision/compatibility regressions).
- apps/web/src/lib/penta-mini-conversation.ts (strict optional versioned money validation).
- apps/web/src/components/penta/mini-conversation.tsx (exact amount text or explicit unavailable state).
- QA/tools/penta-mini-conversation.test.cjs (new version/precision/binding tests).
- QA/tools/penta-mini-exact-money-browser.cjs (new actual Chromium precision/refusal/recovery cases).
- QA/HANDOFF/C55_MINI_EXACT_MONEY_2026-11-19.md (this packet/results).

The host projection is derived only from existing authorized .NET decimal
receipt results. Keep the numeric0.1 payload for existing consumers and expose a
separate versioned exact-money presentation. Do not alter shared ledger DTOs,
accounting/calculation/currency rules, stored originals, SQL/migrations, model
protocol/tools, tier routing, approvals or server/feature activation gates.
Reject unsupported precision/invalid values instead of rounding. Client must
never derive exact strings from JS Number; validate strict version/shape and
learner/currency alignment. Older receipts retain the safe Manual fallback.

## Implemented behavior

`MiniReceipt.ExactMoney` is an additive, computed `mini-exact-money.v1` view
derived from authorized typed .NET decimal results. It retains protocol `0.1`
and the original numeric ledger payload. Invariant decimal strings preserve
one cent, large values and decimal.MaxValue without JavaScript number rounding.
Currencies stay separate. Invalid currencies, duplicate identities/currencies,
negative values and unsupported precision produce no exact-money projection;
there is no rounding, conversion, aggregation or new financial authority.

Legacy protected snapshots deserialize their original decimal values and gain
the computed presentation without rewriting storage or repeating a model or
ledger operation. Incoming projection metadata cannot override those decimals.
The existing 40,000-character receipt limit is preserved: omit the optional view
if its addition would exceed the bound, rather than break a previously valid read.

The client checks the version, strict fields, decimal representability and exact
learner/currency alignment. It displays the host strings verbatim. Malformed
receipts stay unconfirmed until GET-only recovery succeeds; no financial data
enters the six-field browser recovery pointer. Missing/null projections show
an explicit Manual Finance fallback. The evaluation flag stays off by default.
No domain ledger DTO, accounting rule, SQL migration, model tool/protocol or
server activation gate changed.

## Actual validation and evidence boundaries

Raw receipts remain local at `/workspace/c55-mini-exact-money-20261119` and the
run-specific `.build-check` paths below. They are not uploaded as QA/EVIDENCE.
Retained tests were executed on 19 November against the same unchanged C55
implementation; they are not represented as new 23 November runs.

| Check | Result | Source/time boundary |
| --- | --- | --- |
| PENTA API regressions | 837 passed, zero failed/skipped | Retained final 19 November TRX; includes 29 new money cases |
| Mini client tests | 74 passed | Retained 19 November; included again in the fresh combined run |
| 24 PENTA client test files | 1,020 passed, zero failed/skipped/cancelled | Fresh 23 November; independent files, bounded three-worker run |
| New exact-money Chromium matrix | 92/92 passed | Fresh 23 November; 23 scenarios x Owner/Admin x 1280 dark/320 light |
| Existing enabled composer browser matrix | 118/118 passed | Fresh 23 November; unchanged enabled export |
| Default-off browser matrix | 4/4 passed | Fresh 23 November; no Mini requests |
| Enabled webpack export, TypeScript | Passed | Retained matching-source 19 November; Next 16.4.5 / React 19.2.8 |
| Lint | Zero errors, 61 inherited warnings | Retained 19 November; not a warning-free whole-app claim |
| Default-off webpack export | Passed, 90 static aliases | Fresh 23 November; default export left in place |
| Full local npm audit | Zero vulnerabilities | Fresh 23 November; does not erase a hosted failure |
| Production gate checker | BLOCKED, exit 1 | All six required critical records still lack final acceptance |
| Integrity | Passed | 1,822 unclaimed tracked paths byte-exact vs f9975dd2889daf9b91ecea2ee2f45e4239eb89ed, including 16 vendor files |
| Mini preservation | Passed | Clean 14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8 checkout, all 33 frozen hashes unchanged |

Browser runs retain sandboxing, remove only owned profiles and finish with zero
contexts, no cleanup error and no recorded browser errors. They use the actual
exported application with synthetic intercepted APIs, not native SQL or live AI.
Their 214 distinct cases exclude the separately retained six-case pilot.

- Exact-money result: `.build-check/penta-mini-exact-money-browser/2026-11-23T04-36-09.229Z-1153/result.json` (eight large/mixed-currency screenshots alongside it).
- Composer regression: `.build-check/penta-mini-conversation-browser/2026-11-23T04-36-10.843Z-1359/result.json`.
- Default-off result: `.build-check/penta-mini-conversation-browser/2026-11-23T04-42-29.620Z-9915/result.json`.

The initial API compilation failed on target-typed `new` in a params test fixture;
the fixture now specifies `new LearnerBalance(...)`. Initial failure, corrected
836-pass run and final 837-pass run remain separate. The 19 November browser
interruption was an automatic approval usage-limit rejection. Approved 23 November
runs completed after access returned; no execution restriction was bypassed.

## New Desktop work and exact CI

Desktop 69a5794243788905387f34f638c3062225416c0f integrated C54 and reports 2,571
API cases, 118 enabled/four default-off browser cases and native Identity/SQL
receipt recovery, authority denial and mandatory-audit rollback acceptance.
Read its [native report](https://github.com/AcademyDesk/AcademyDesk/blob/69a5794243788905387f34f638c3062225416c0f/QA/REPORTS/PENTA_C54_NATIVE_ACCEPTANCE_2026-11-23.md).
These are Desktop source-bound results, not cloud re-executions or C55 native
acceptance. Its browser helper additionally fixes Windows DevToolsActivePort
discovery; do not replace that newer helper with the cloud baseline.

Desktop's Mini planner 0.15 branch `codex/mini-query-grounding-20261118` is remotely
verified at 4732116bc9a8c4eb9904ee7c4fc09100866816f6. Its report records 81 focused
passes and a separate 12/12 narrow learner-finance evaluation after an earlier
10/12 failure. Preserve both results and their exposure boundaries. The full
hosted Mini run [44448836332](https://github.com/AcademyDesk/PentaMini/actions/runs/44448836332)
still failed: reported 1,661 passed, 26 failures, 60 errors, one skip and five
subtests passed. Original archive, platform/process and frozen-hash discrepancies
remain genuine prerequisites/defects; no skip or substitute evidence is accepted.
The cloud Mini checkout was deliberately preserved rather than overwritten.

Latest Academy run [45112955162](https://github.com/AcademyDesk/AcademyDesk/actions/runs/45112955162)
is bound to exact Desktop 69a5794243788905387f34f638c3062225416c0f. At this report's
check, the ordinary API/SQL/media/frontend job was running and the paired job
had failed at `Require access to pinned private Core source`. Configure the
existing read-only `PENTA_SOURCE_READ_TOKEN` prerequisite through the owner's
secure Actions settings; do not expose credentials or weaken the required job.
Re-read the run for its final ordinary result; an in-progress job is not a pass.

Historical run 44274804584 on 219bba29 failed install/audit and paired source
access. New Desktop evidence attributes the audit failure to picomatch
CVE-2026-112366 (build/dev copies). This cloud's fresh full audit reports zero,
and the earlier hosted log redirect was unreadable here. Preserve the differing
observations; do not infer remediation or a green hosted candidate from audit
zero. No workflow, lockfile, secret, setting or job dispatch was changed here.

## Testing this packet without loading a model

Use an isolated clean worktree at this feature branch; retain all existing D:
work and local evidence. The installed locked dependencies and an existing
Chrome/Chromium executable are prerequisites. From the repository root, PowerShell:

```powershell
$env:NEXT_PUBLIC_API_URL = 'http://127.0.0.1:49142'
$env:NEXT_PUBLIC_PENTA_MINI_EVALUATION = 'true'
npm --prefix apps/web run build -- --webpack
# Set this to your existing Chrome/Chromium executable's full path.
$env:QA_BROWSER_EXECUTABLE = '<existing-browser-executable>'
node QA/tools/penta-mini-exact-money-browser.cjs
Remove-Item Env:NEXT_PUBLIC_PENTA_MINI_EVALUATION
npm --prefix apps/web run build -- --webpack
```

Expected: 92 passes, exact amounts including `9999999999999999.99`, separate USD
and INR, safe legacy fallback, malformed-payload refusal and GET-only recovery.
Inspect the generated result JSON and large/mixed screenshots beneath
`.build-check/penta-mini-exact-money-browser/<run>/`. The loopback harness serves
the export and intercepts synthetic APIs; no SQL, credentials, model service or
public access is needed. Stop on a build/test failure and retain its log.
The final rebuild restores the default-off web export.

This is automated component/browser testing. A user-operated real-data demo
needs Desktop's separately prepared synthetic-data environment and native
acceptance. C55 does not make a live model ready or override the loading stop.

## Integration and prioritized next acceptance

Publish this bounded C55 implementation after the existing claim on
`codex/cloud-c55-mini-exact-money-20261119`; its enclosing implementation commit
is the return SHA. Select only that commit onto the current Desktop feature
branch. All eight claimed paths were compared to both the tested f9975dd2 base
and Desktop 69a57942: existing paths match, and new paths are absent on both.
Keep newer Desktop governance, Student 360, grounding and SQL harness work.
C54 is already integrated on 69a57942; do not replay C54, C53 or C50 wholesale.
No combined-source test pass is claimed by this clean path comparison.

1. Desktop: integrate C55 once, run affected combined API/client/build checks,
   then verify exact cents through the real protected POST and GET receipt route
   on disposable SQL, including old encrypted receipts, lost replies, provider
   outage, current permission denial and audit failure. Assert unchanged ledger
   rows/totals and one original dispatch. Preserve C54's native results.
2. Desktop: finish the owner's synthetic interactive demo, including actual
   composer behavior and Manual save. Keep any model experiment within its
   separately authorized local packet; no cloud download/load/inference here.
3. Release owner: resolve paired Actions source access and verify both jobs on
   the exact integrated SHA. Investigate the full hosted dependency result and
   Mini archive/platform/hash failures without weakening gates.
4. Close `BUG-DATA-0001`, `BUG-DATA-0002`, `BUG-DATA-0003`, `BUG-DATA-0010`,
   `BUG-SEC-0001` and `BUG-AUTH-REVOCATION-001` only after their required linked
   acceptance. Complete physical-device and staging/private-Blob/restore/rollback
   qualification. Prioritize Hayansh's end-to-end launch journeys.
5. Independently qualify wider eligible AI outcomes before measuring 90%.
   A narrow 12/12 or the synthetic UI matrix is not full AI acceptance.

Mini/Plus/Pro remain unqualified for production; Ultra is deferred; 90% remains
unmeasured. No main merge, Azure/environment publication, public-access change,
dependency/model acquisition, model loading, inference, training or activation
was performed by C55. Environment privacy remains outside this packet's edits.
Next route: Sol High for combined native/host/model and security acceptance;
Sol Medium for routine demo documentation.

## Private evidence seals

SHA256 values for retained local receipts (raw paths relative to the task folder):

| Receipt | SHA256 |
| --- | --- |
| `api-final-results/c55-penta-final.trx` | `88ed07d673c9d9f8c9d41a1571a1d538a3176cda1c0a734efd05efdb81b8e43c` |
| `client-regressions-20261123/summary.json` | `ffbea387920a16661e77488223d468b8b12cb1866d7675a0002162804d0c2f57` |
| `browser-exact-full-20261123.log` | `67ea85f965136bf1c4ca1d7d9affafbe8bb6be96a5695104e6ed68f80dec5284` |
| `browser-composer-regression-20261123.log` | `906ec1967f96625fb1278cce47da2ddb4ff8766c76d9887ec6045dea3eabf648` |
| `browser-default-20261123.log` | `b4bd19bd3fdd54dc81b4f6ca014dbb50a61bd857338351105c7b8035877d7269` |
| `source-integrity-20261123.json` | `edc613faff783e03827b7c952907940842379524b344e656816e9fe778d6ec36` |
| `desktop-integration-preflight-20261123.json` | `262c810f96506e0aa14cd8f2699801cbb0a8457ebcbb8a0d7bf5bdbd76625d90` |

The integrity receipt also records all seven executed implementation/test source
hashes and browser cleanup flags. Publication remote-SHA and exact check queries
are recorded separately after commit; no backend CI green result is implied by
a preview status or by publishing a feature branch.
