# C51 — cross-role conversation resilience

MODEL: Sol High
TOOL: Cloud Codex
REPOSITORY: AcademyDesk/AcademyDesk
BRANCH: codex/cloud-c51-conversation-resilience-20261118
ENVIRONMENT: managed Linux cloud, installed Chromium and locked web dependencies
BASE: 4cbe56a76119ad257924d975c1bb2ecb97196022
STATUS: IMPLEMENTED / CLOUD VALIDATED — 18 November 2026 UTC

## Ownership and deliverable

Cloud claims only:

- `QA/tools/penta-conversation-resilience-browser.cjs` (new actual Chromium runner).
- `QA/HANDOFF/C51_CONVERSATION_RESILIENCE_2026-11-18.md` (this claim and results).

The checkout was clean before this branch was created from freshly fetched
Desktop source. Published cloud claims were checked; no C51 competitor exists.
Desktop retains all API/SQL/migration/model and shared tracker ownership.
No product implementation is claimed. If a product defect is demonstrated,
preserve its evidence and publish a precise scope amendment before editing.

Expand the existing browser program with current Admin/Owner, Teacher,
Student/Guardian and Platform conversation resilience. Check verified responses,
service failures, authority denial, malformed contracts, unsupported prompts,
late responses across session changes, safe recovery and Manual availability,
on narrow/light and desktop/dark viewports. Assert actual requested endpoints,
no unexpected mutations, no stale result display and owned process cleanup.
Reuse existing exact application export only if its web source matches this
base; otherwise build with the installed exact lock. API responses are explicitly
synthetic intercepted loopback fixtures. This is browser/host-contract evidence,
not native SQL, actual devices, model intelligence or enterprise acceptance.

## Fresh coordination facts

Desktop has already fixed the payslip-reference collision at this base.
Exact hosted run44065049153 has ordinary API/SQL/media/frontend verification
SUCCESS, with the separate paired Core job FAILURE. Its annotation still
requires `PENTA_SOURCE_READ_TOKEN` with read-only Contents access to private
AcademyDesk/PentaMini. Do not claim the payroll failure remains current.
C50's fixture correction remains published separately at4156da7e.

No main merge, Azure deployment, environment publication, access-policy change,
production database operation, new package/model download, model loading,
training, inference experiment or activation. Six critical acceptance records
remain open. Mini/Plus/Pro unqualified; Ultra deferred;90% target unmeasured.
Keep all previous source, branches, raw failures and frozen/vendor files intact.

## Validation and return

Execute the new actual-browser matrix, relevant existing deterministic client
checks, syntax/diff and source-preservation checks. Record every pass/failure,
fixture correction or product defect, retained raw evidence and next owner.
Publish bounded feature commits and verify remote SHA and exact CI status.
Do not rerun C50's unchanged whole suites or replace private evidence with
synthetic acceptance. Next route: Sol High for any authority/recovery defect;
Desktop for protected native SQL, private model prerequisites and release gates.

## Completed batch

**240/240 browser cases verified**, plus **946/946 deterministic PENTA client
checks** and **81/81 payroll API regressions**, with zero skipped cases in those
client/API runs. No product implementation change was needed or made. This is a
bounded extension of the existing browser program, not production qualification.

The new runner executes20 scenarios for each of six actor roles on both1280px
Orbit/dark and320px Prism/light pages. Every case gets a fresh isolated browser
context. It exercises the actual exported React application in installed,
sandbox-enabled Chromium; responses are intercepted synthetic loopback fixtures.

| Actor | Desktop cases | Narrow cases | Actual guided read exercised |
| --- | ---: | ---: | --- |
| Academy Admin | 20 | 20 | Authorized academy student lookup |
| Academy Owner | 20 | 20 | Authorized academy student lookup |
| Teacher | 20 | 20 | Assigned attendance class search, without preparing/confirming a mark |
| Student | 20 | 20 | Current linked learner's next class |
| Guardian | 20 | 20 | Current linked learner's next class |
| Platform Owner | 20 | 20 | Scoped support aggregate |

Each actor/viewport checks: verified and empty reads;403 denial;503 outage and
explicit recovery; foreign scope; malformed fields or invalid aggregate bounds;
wrong content type; invalid JSON; cacheable replies; an otherwise valid response
larger than64KiB; late replies after login-epoch/token replacement; local sign-out;
actual second-tab sign-out via a real browser StorageEvent; an unrelated workspace
session event; unsupported commands; unavailable or cacheable readiness; and
repeated form submissions while one response is held. Assertions cover expected
bearer namespace, fixed method/endpoint/query, absence of every mutation, no
unverified or stale result display, authorized Manual links or the global fresh-
sign-in fence, responsive overflow, expected theme, and uncaught runtime errors.

Duplicate submission cases observed exactly one pending GET. Late old-session
responses never repopulated a composer or result. Unsupported model/export text
sent no primary request. Browser sandbox remained enabled and each owned browser
profile/context/server was cleaned up. Twelve baseline screenshots are retained
privately as synthetic UI evidence. No physical mobile device was exercised.

### Preserved failures and exact retest boundaries

The first36-case pilot passed24 and failed12 because its Manual-link expectation
was inappropriate after an ended login epoch. The existing global
`WorkspaceSessionBoundary` correctly displays **Session ended / Sign in again**
and removes the private workspace. The runner was corrected to require that
fence, a login link and no remaining composer/results after sign-out/epoch change;
a still-authorized session must retain Manual. The next36-case session pilot
passed, including actual second-tab and held-response cases. No application guard
was removed or modified.

The complete240-case run then passed236 and failed four Platform readiness cases:
the test dereferenced `document.body.innerText` before the initial body existed.
Guarding that initial DOM wait with optional chaining fixed the runner. Exactly
those four cases were rerun and all passed with unchanged acceptance assertions.
The final240-case matrix combines the236 passes with the four successful retests;
it is not described as a single uninterrupted240-case exit0 run. The original
failure result and all corrected receipts remain intact. Application source did
not change between any of these runs.

An initial seven-file `node --test` wrapper probe also had one failed file without
detailed case output. Its log is preserved. The project-supported direct per-file
entry points were subsequently run in the outer execution runtime, including the
owned loopback channel suite, yielding23 complete suites /946 PASS /0 FAIL /
0 SKIP /0 CANCEL. The wrapper failure is not used to infer an application defect.

### Commands and source binding

```sh
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/penta-conversation-resilience-browser.cjs
QA_BROWSER_EXECUTABLE=/usr/bin/chromium QA_C51_CASE_FILTER='^PlatformOwner/(1280|320)/readiness-(offline|cacheable)$' node QA/tools/penta-conversation-resilience-browser.cjs
/workspace/.tools/dotnet/dotnet test AcademyDesk.slnx --no-restore --nologo -m:2 -p:UseSharedCompilation=false --filter FullyQualifiedName~Payroll --logger 'trx;LogFileName=c51-payroll.trx' --results-directory /workspace/c51-conversation-resilience-20261118/payroll-results
node --check QA/tools/penta-conversation-resilience-browser.cjs
git diff --check
```

The focused client batch discovers all23 existing `QA/tools/penta-*.test.cjs`
files and executes `node <file>` in three bounded lanes, preserving individual
stdout, exit codes, case counts, skips and cancellations. Its private runner and
summary are retained. Existing regression source was not edited.

The static export is reused from C50: `apps/web/` is byte-identical between its
build/test source `fbc1d0d5167cb7fdac78e60c2ab2c15ee1f1854c` and this Desktop
base `4cbe56a76119ad257924d975c1bb2ecb97196022`. The exact installed dependencies
remain Next16.4.5 / React19.2.8, Node24.19.0. No new frontend build/type/audit
run or full backend/frontend/Mini run is claimed here. The current-source payroll
API assembly was freshly compiled using SDK10.0.403 and its81 selected tests
passed; it does not replace native SQL contention evidence.

The prior C50 report's missing private communication receipts and original Mini
archives remain prerequisites in this cloud checkout. This batch does not close
those failures, synthesize originals or alter any frozen provenance.

## Current hosted result and remaining acceptance

Desktop was freshly fetched again before publication and remains at the exact
base above; neither claimed file overlaps its changes. GitHub
[run44065049153](https://github.com/AcademyDesk/AcademyDesk/actions/runs/44065049153)
is completed/failure because only the separate Core job is blocked:

- **API, SQL, private media and frontend verification: SUCCESS.** This supersedes
  C50's earlier hosted payroll failure on60036cfb. The payout-identity reference
  correction is Desktop work already present in this base, not a C51 fix.
- **PENTA protected onboarding and private Core transport: FAILURE.** The exact
  GitHub annotation requests `PENTA_SOURCE_READ_TOKEN` with read-only Contents
  access to private `AcademyDesk/PentaMini`. Cloud repository access is separate
  from Actions' missing credential. No secret/settings change or bypass occurred.

The actual production-gate checker still exits1 with all six records open:
`BUG-DATA-0001` collections, `BUG-DATA-0002` adjustment consumers,
`BUG-DATA-0003` payroll, `BUG-DATA-0010` void/restoration,
`BUG-SEC-0001` private media and `BUG-AUTH-REVOCATION-001` broader sessions.
The additional browser evidence does not close any of them. Remaining launch
work includes that required Core job, current-candidate linked critical and
physical-device acceptance, staging configuration/backup/restore/private Blob/
rollback rehearsal, and separately authorized private Mini qualification.
Mini/Plus/Pro remain unqualified; Ultra deferred;90% model-backed completion is
unmeasured. No model was downloaded, loaded, run, trained or activated.

## Integration and preservation handoff

Desktop may review/import this branch's two task commits or its final two-file
delta: the new runner and this report only. It need not replay C49 or the payroll
patch. C50's separate fixture/report return remains at4156da7e and is not silently
reapplied here. Shared `AI_HANDOFF.md`, issue records, API/SQL/migration source,
package manifests/locks and all product files remain untouched.

All1,816 unclaimed tracked base files are byte-exact, including16 vendor files.
All33 frozen Mini files match their original hashes; the Mini checkout is clean.
Previous branches, originals and failure artifacts were preserved. Publication
is on this feature branch only; the final return supplies the verified remote
commit and exact check status. No main merge, Azure deployment, environment
publication or access-policy change occurred.

Next route: Sol High/Desktop for the scoped Actions prerequisite, real protected
host/device/staging and model-qualification acceptance. Use this synthetic runner
for UI regressions alongside the existing native live runners; never substitute
its intercepted replies for real Identity/SQL/provider or model evidence.

## Private evidence receipts

Raw directory: `/workspace/c51-conversation-resilience-20261118`.
Browser runs remain under `.build-check/penta-conversation-resilience-browser/`;
each run has its own immutable directory. Only sanitized source/report files are
published. SHA256 values bind the result summaries and logs below:

| Receipt relative to the private raw directory | SHA256 |
| --- | --- |
| `browser-final-summary.json` | `f8235c3ed47fa67a8bc3e04034af534575d4101d3e295654f81886b46a300364` |
| `browser-full.log` | `8c6811d04768260a42fd9f91235bdfd7f38da3f18301f1580caf8d455e68dc46` |
| `browser-platform-readiness-corrected.log` | `e0612841c5c2b63701aa193d2672e322bfa4a70ce75ea76886e6dead5e8c0e29` |
| `browser-pilot.log` | `eaf57fb58da7a2033d397b0b95aa95c14001c419f2c0b951aa170583578914d8` |
| `browser-pilot-scope-corrected.log` | `7e6e6c297c92c3cd6983f0d232ddd2d972f672244fa5e4d1b23a5514cce06cb8` |
| `focused-client-tests.log` | `4a92a7854b56e7ae1d4f57d945f6a5e19f1b4b1548650f3fc71f7a63738ccdde` |
| `penta-client-results/summary.json` | `4c562250407754b86f55741efc7a978c77d484dbb214462dcdbea79c75f28e8f` |
| `payroll-tests.log` | `a70e9301f30c89eb2cf6a92547a44b660fa3f9dca74c5a0b1890c83aa94a8048` |
| `payroll-results/c51-payroll.trx` | `13e09c41eeb0676e1ae872b86314be2f3d95befa112779c0c76a3e231159ad6b` |
| `source-integrity.json` | `a8ac1c6fa2df24e048e76f547ff93649e555bffcb7d2d6cefabb2df43cf7ab83` |
| `desktop-ci-jobs.json` | `6b04ce8d39a5a83ba8f16921ce2303098e2b85262e41c7132f5e9c1d66a02633` |
| `desktop-ci-annotations.json` | `14971b4f10bc90416f60ff8abc830e23de3163af3ad471bacbaefe4d1fc17650` |
| `production-gates.log` | `d7342475f63187e45a79e0a482851f5fe6345953f2878dc8851c6244c6b959e6` |

Final runner SHA256: `93a7a7555418b07e0d27e5f24aede69beaa483cdd71012a4b55c9ae576fbc8c9`.
