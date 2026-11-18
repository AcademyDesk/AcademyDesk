# C52 — conversation context and protected attendance recovery

MODEL: Sol High
TOOL: Cloud Codex
REPOSITORY: AcademyDesk/AcademyDesk
BRANCH: codex/cloud-c52-context-attendance-20261118
BASE: 4cbe56a76119ad257924d975c1bb2ecb97196022
ENVIRONMENT: managed Linux, installed sandboxed Chromium, exact locked static web export
STATUS: IMPLEMENTED / CLOUD VALIDATED — 18 November 2026 UTC

## Exact ownership

Cloud claims only these new files:

- `QA/tools/penta-browser-harness.cjs`: isolated synthetic-browser test infrastructure.
- `QA/tools/penta-context-attendance-browser.cjs`: follow-up/learner/attendance matrix.
- `QA/HANDOFF/C52_CONTEXT_ATTENDANCE_2026-11-18.md`: this claim and final results.

The current Desktop tip was fetched and the checkout was clean. C50/C51 are
preserved on their own feature branches; no competing C52 claim was observed.
Desktop retains API/SQL/migration/model/shared-tracker ownership. No product
source change is claimed; demonstrate any defect and publish a bounded amendment
before changing its implementation. Root governance has been read this session.

## Batch

Extend the existing test program with actual browser assertions for academy
lookup follow-ups, corrected/expired context, Guardian linked-learner choice,
Student self-read contracts and exact fee strings, and protected Teacher
attendance review/confirmation/reload/receipt recovery. Planned matrix176 cases:
64 Admin/Owner,64 Student/Guardian and48 Teacher, at1280/dark and320/light.
Do not invent multi-learner Student access. All intercepted replies and effects
are explicitly synthetic, with no native SQL/Identity/provider/model acceptance.
Assert exact allowed requests and review terms, no premature/repeated effects,
no stale/foreign result display, current session binding and owned cleanup.

Reuse the existing static export only after verifying its application source
matches; no need to repeat C50/C51's unaffected broad suites. Run meaningful
focused regressions if implementation changes. Preserve initial failed runs and
any missing original-evidence prerequisites. Never synthesize acceptance receipts.

No main merge, Azure deployment, environment publication, access-policy change,
new package/model download, model loading, training, inference or activation.
All six critical records remain open; Mini/Plus/Pro unqualified, Ultra deferred,
and90% target unmeasured. Desktop's latest ordinary hosted job passed; its
separate Core job still requires read-only `PENTA_SOURCE_READ_TOKEN`.

## Return

Record actual cases and retests, raw evidence hashes, source-preservation checks,
exact hosted status and next Desktop acceptance work. Publish only the claimed
feature files and verify remote SHA. Next route: Sol High for authority/recovery;
Desktop for protected native SQL, private-model prerequisites and launch gates.

## Completed results

The seven-case pilot passed, then a single complete run passed **176/176 cases,
zero failures**. No product defect was demonstrated in these scenarios. Only
the two new QA source files and this report changed; no product fix was needed.
The pilot is not added to the176 unique full-run cases.

| Actor | Desktop1280 / Orbit | Narrow320 / Prism | Total |
| --- | ---: | ---: | ---: |
| Academy Admin | 16 | 16 | 32 |
| Academy Owner | 16 | 16 | 32 |
| Student | 12 | 12 | 24 |
| Guardian | 20 | 20 | 40 |
| Teacher | 24 | 24 | 48 |
| Total | 88 | 88 | 176 |

All cases ran the actual exported React application in installed Chromium with
explicitly synthetic intercepted API replies. The harness gives each case an
isolated browser context and records method/path/query/body, uncaught runtime
and interception errors, responsive overflow and the current Manual link/theme.
The full run has no captured errors, sandbox enabled, owned profile removed,
zero remaining contexts and no cleanup error. Ten selected screenshots are
private synthetic UI evidence. Narrow desktop viewports are not physical devices.

### Academy lookup context —64 cases

Verified single-result pronouns and first/second result references; ambiguous
pronouns and out-of-range references request clarification. Selection before any
search cannot read a record. Refinement stays in the prior domain, switching to
batch lookup replaces the student result, and an empty, denied or malformed
lookup cannot supply a later selection. Unsupported operations and expired
five-minute context require a fresh lookup. Duplicate IDs and unsafe source
paths are refused. HTML-shaped labels render as text without executing markup.
Expected record links, fixed search endpoint/query and zero mutations are checked.

### Student/Guardian learner binding —64 cases

Student scenarios use only one self learner; no multi-learner Student access is
invented. Guardian scenarios verify explicit and ambiguous learner choice,
unknown/duplicate names, follow-up under the selected learner and correction to
a different currently linked learner. Every turn rechecks current linkage.
A revoked selection cannot silently fall back to its sibling. A choice revoked
between displaying and clicking it is rechecked before any record read.
Academic and finance flags independently block unauthorized information reads.

Class, due-work and exact outstanding-fee display are checked. The positive
22-digit balance remains the exact string `USD 9999999999999999999999.99`.
Foreign-student and wrong-kind envelopes, numeric/zero balances, invalid currency
and an impossible due date are refused without a result section. HTML-shaped
class text does not execute. No portal or lookup case sends a mutation.

### Protected Teacher attendance —48 cases

Selection and roster lookup produce no preparation, confirmation or effect.
Preparation requires the exact typed learner/status/trimmed optional note;
it saves a session/academy/request-bound recovery pointer and displays a review.
It applies zero marks. Present, Absent, note and whitespace-trimming paths pass.
Preparation-only and prepared-page reload never confirm attendance automatically.

Confirmation sends the exact reviewed hash and explicit intent. Successful
synthetic host state has one mark and one queued notice, with one receipt.
The UI describes the notice as queued, not delivered. Repeated clicks while
confirmation is pending produce one confirmation. Lost preparation/confirmation
responses retain the same request; explicit recovery and page reload use GETs
without another PUT/POST. Receipt reload also preserves the one synthetic effect.

Blocked recovery storage sends no preparation. Wrong learner/class/status in a
preparation response and altered confirmation hash/receipt/notice count are not
shown as verified success; recovering the original request returns the verified
review or receipt. Expired review sends no confirmation. Recovery404/409/410
requires the user to check Manual Attendance before dismissing the pointer.
Foreign login-epoch/academy and corrupt stored pointers are discarded without
request recovery or a write.

These are **synthetic fixture counters and browser-contract assertions**.
They do not prove database commit, real notification delivery, mandatory audit
rollback, native concurrency, actual Identity/RBAC, provider readiness or model
quality. Existing protected-host SQL and live-browser evidence retains its own
source and scope; none is replaced by this runner.

## Reproduction and source binding

```sh
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/penta-context-attendance-browser.cjs
# Optional diagnostic subset; the result explicitly records the selected filter:
QA_BROWSER_EXECUTABLE=/usr/bin/chromium QA_C52_CASE_FILTER='^Teacher/1280/lost-confirm$' node QA/tools/penta-context-attendance-browser.cjs
node --check QA/tools/penta-browser-harness.cjs
node --check QA/tools/penta-context-attendance-browser.cjs
git diff --cached --check
```

The helper requires the existing exact application export and installed Chromium;
it installs no dependencies. Current `apps/web/` matches C50's build source
`fbc1d0d5167cb7fdac78e60c2ab2c15ee1f1854c` byte-for-byte, so that unchanged export
was reused. Node24.19.0 / Next16.4.5 / React19.2.8 remain installed. C51's946
client checks and81 payroll API tests are matching-source retained evidence,
not newly run C52 totals. No new full suite, typecheck, lint, build, audit, native
SQL or Mini run is claimed. There were no implementation changes requiring them.
Original communication SQL receipts and private Mini archives are still absent
from this cloud checkout; no original evidence was synthesized or overwritten.

## Current CI and release boundaries

Desktop was fetched again before publication and remains
`4cbe56a76119ad257924d975c1bb2ecb97196022`; none of the claimed paths overlap.
Exact GitHub [run44065049153](https://github.com/AcademyDesk/AcademyDesk/actions/runs/44065049153):

- API, SQL, private media and frontend verification: **completed/success**.
- PENTA protected onboarding and private Core transport: **completed/failure**.
  Its exact annotation requires `PENTA_SOURCE_READ_TOKEN` with read-only Contents
  access to private `AcademyDesk/PentaMini`. Chat repository access does not
  configure the separate Actions secret. No credential/settings change occurred.

The actual production checker remains blocked, exit1. `BUG-DATA-0001`,
`BUG-DATA-0002`, `BUG-DATA-0003`, `BUG-DATA-0010`, `BUG-SEC-0001` and
`BUG-AUTH-REVOCATION-001` all remain open. Broader linked critical and physical-
device acceptance, staging configuration/backup/restore/private Blob/rollback,
the required Core job and separately authorized private Mini qualification remain.
Mini/Plus/Pro are unqualified, Ultra deferred and90% model-backed completion is
unmeasured. No model load, inference, training or activation occurred.

## Preservation and import

All1,816 tracked files from the Desktop base remain byte-exact, including all16
vendor files. All33 frozen Mini source files match their original hashes; its
checkout is clean. Product/API/SQL/migration source, root trackers, manifests and
lockfiles remain untouched. All earlier branches and failure evidence survive.

Desktop may review/import this branch's two task commits or its final three-file
delta. C50/C51 remain separate source returns; do not reapply them wholesale or
replay the already integrated C49/payroll changes. The final user return supplies
the verified remote C52 SHA and exact check status. Next route remains Sol High/
Desktop for real protected-host/device/staging acceptance and model prerequisites.
No main merge, Azure deployment, environment publication or access-policy change.

## Private evidence integrity

Raw directory: `/workspace/c52-context-attendance-20261118`. Full browser evidence
also remains in the unique `.build-check/penta-context-attendance-browser/`
run directories; the helper preserves every prior run. Only sanitized QA source
and this report are committed.

| Receipt relative to the private raw directory | SHA256 |
| --- | --- |
| `pilot.log` | `1fa2fdb5f3843652a6fe3ea7d20f29c6b690c9e4c40b228d9d3b282d2b40efcb` |
| `browser-full.log` | `1bf01cf4f26f336cd9970433e8d95788962586810d42c158c8a8338fe6738cf8` |
| `browser-full-result.json` | `b4a3eb829e304c0632b042547bdca5aa0db33fb1f7499b93729e88252024c00b` |
| `batch-summary.json` | `abd6ec0a7f8b5a290896e78ce02c78222e326d717664fef0507d92ac817e8e1d` |
| `source-integrity.json` | `fd7d7613b2296e0f6e4d27786bf4d1c8d019023b6d250f07c34195e070c217ca` |
| `production-gates.log` | `d7342475f63187e45a79e0a482851f5fe6345953f2878dc8851c6244c6b959e6` |
| `desktop-ci-runs.json` | `a170802c81a5366ecd468f9f3bdeff8fba12750f97de01ca585a3d2ce2fa46f4` |
| `desktop-ci-jobs.json` | `55169fc0a1d6680e3b80361ccaaea606045064148814910793c8683b4e501431` |
| `desktop-ci-annotations.json` | `1fa9be48487f47a4effabcae3c3fb32da42e887a873e6145b88545cf7871af13` |

Executed source hashes:

- `QA/tools/penta-browser-harness.cjs`: `5e9bc1b36c2adce39ffb446857c0cee9b4a5a37571ce03a7624cebdb4f6f00ef`.
- `QA/tools/penta-context-attendance-browser.cjs`: `ef231e5cbbefe3b2c633e13746e152cf8e7b9a443098e2db01d0174dd91b69bc`.
