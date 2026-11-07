# C11 access/status/navigation batch — 7 November 2026

Status: SOURCE REPAIR / CONTROLLED BATCH COMPLETE; original live closure OPEN.
Claim `28ad7b9` was published before edits. Base/latest Desktop
`5f4fdc74724391c7ed2def8577eb2d07d4b64513`; branch
`codex/cloud-c11-status-navigation-20261107`. Frontend matches the verified C10
source; Desktop integrated C9/C10 and completed live C9 Payments. Remaining live
invoice/payroll/reconciliation and academic/role/SQL acceptance stay with Desktop.

Exact cloud-owned paths:

- `apps/web/src/app/access-review/page.tsx`
- `apps/web/src/app/platform/control/page.tsx`
- `apps/web/src/app/sales-marketing/page.tsx`
- `QA/tools/status-navigation.test.cjs`
- `QA/tools/status-navigation-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` (test-only extraction of the real inner sales component)
- `QA/ISSUES/BUG-UI-0003.md`
- `QA/ISSUES/BUG-UI-0004.md`
- `QA/ISSUES/BUG-UI-0005.md`
- `QA/HANDOFF/C11_STATUS_NAVIGATION_2026-11-07.md`

Reproduce expired-grant display, rejected/stale platform status selections and
Follow-ups tile navigation against current source. Fix only their display,
status mutation/readback recovery and route mapping. Keep server permissions,
tenant/finance/audit rules, role authority and live acceptance separate. Preserve
existing platform create/profile/announcement behavior and all retained tests.

Use fixed-clock and delayed synthetic API controls, then one exact-lock build,
full frontend and native Chromium batch. Record unchanged-source failures before
repair. A new test adapter may expose the original inner sales component without
rewriting or exporting extra application page APIs. No weakened assertions/skips.

No backend/SQL/migration, Desktop-owned files, shared session/model/vendor source,
package/lock/workflow or integrator pointer edits. No new downloads, private-graph
audit retry, environment/public-access/VPN/TCP/privacy change, main/Azure/customer
DB, or model loading/training/inference/activation. Mini/Plus/Pro unqualified;
Ultra deferred. Original issue histories and complete closure gates remain.

## Returned implementation

`88a49a989d8957c5d299acecae2aaaf710eb5986` on
`codex/cloud-c11-status-navigation-20261107`: three pages, two new QA runners and
one test-only adapter line. No backend, SQL harness, authorization or dependency
change. Base includes Desktop's C6/C8 repairs and C9/C10 returns; none is reapplied.

- Access review uses the existing authorization predicate for its display clock:
  not revoked and permanent or strictly future expiry. Initial timestamp travels
  with the asynchronous client read. Owned timers update at expiry, including a
  long-open page, without API polling. Malformed temporary expiry is nonactive
  and renders safely. Active/Expired/Revoked labels retain unrevoked revoke actions.
- Platform invoice/case status controls use accepted records directly. Duplicate,
  placeholder, stale removed-row and unmounted callbacks cannot write. A status
  epoch prevents older general reads from replacing newer accepted status rows.
  Known rejection keeps the prior value. Uncertain outcomes and failed readback
  stay locked with an explicit last-loaded label; **Refresh platform status lists**
  performs only fresh GETs. Case priority comes from the latest loaded row.
  Existing general create/profile/announcement paths retain their behavior; their
  shared helper cannot submit while status reconciliation is outstanding.
- Sales details links reuse existing tile metadata, adding the omitted Follow-ups
  route while preserving all five other tested labels. The unused mapping binding
  is removed. The adapter exposes the original inner sales component only inside
  test compilation, without adding application page exports or changing assertions.

These are UI/data-read safeguards. Client clock/cached records never authorize
anything. Server permissions, time, scope, current rows, transactions and audit
remain authoritative. Grant creation/revocation and unrelated platform mutations
are not newly qualified by this batch.

## Actual source-bound verification

| Check | Actual result | Boundary |
| --- | --- | --- |
| Corrected initial TSX baseline |5 PASS /23 FAIL|28 controls on preserved old page snapshots; notice/panel selectors corrected before this recorded run |
| Unchanged-export native baseline |10 PASS /39 FAIL|49 groups on all570 byte-exact prior C10 files; invalid expiry produced one captured runtime error |
| Final controlled TSX |28/28 PASS, zero skips|Expiry count/status/timer, malformed expiry,18 invoice/case recovery cases and six sales route mappings |
| Final native Chromium |49/49 PASS|Nine grant contexts,28 platform contexts and12 sales tile contexts |
| Runtime / requests |Zero JS/runtime/interceptor errors;24 synthetic PATCHes|No live status/permission write; grant and sales cases read-only |
| Full final frontend |2,008 PASS /three FAIL /zero skips|2,011 tests in73 files; missing genuine communication SQL receipts remain strict failures |
| Build / TypeScript / lint |PASS|Installed Next16.4.3webpack,89 static generation entries; zero lint errors/62warnings, one fewer unused-binding warning |
| Export / postbuild |PASS|570 files,87 unchanged aliases, all final file hashes unchanged during native verification |
| Browser / cleanup / parse / diff |PASS|Normal Chromium sandbox, exact owned profile removed, both new scripts parse and whitespace clean |

Grant browser contexts use Asia/Kolkata, UTC, Pacific/Honolulu and Pacific/Kiritimati
at320/light and1440/dark. A controlled Date.now moves immediately before/at expiry;
the real browser timer updates the display while grant-read count remains unchanged.
An additional malformed-expiry case remains nonactive without a render exception.
This is not a real authorization-expiry or clock-skew acceptance test.

Each platform kind runs rejection,500, committed transport loss, failed fresh
read, external-authority readback, pending and placeholder cases at both widths.
Recovery GETs neither replay the PATCH nor modify unrelated rows. A repeated failed
refresh stays locked. Lost-response recovery reflects the synthetic saved status;
fresh external status outranks the requested value. The controlled unmount cases
also hold a rejected response body after headers to ensure no late state update.

All six sales tile labels retain exact hrefs. The Follow-ups cases activate the
real Next link with focused Enter, reach the correct heading/query and return via
browser history with no lingering modal. Other target routes are asserted as
hrefs, not newly accepted end-to-end workflows. Shared modal/select focus and
physical devices remain separate original records/gates.

## Retained preparation failures and artifacts

The first panel selector omitted another existing class; it was corrected without
altering expectations. The corrected baseline ran against copied old page bytes
in an owned temporary test workspace using the same installed dependencies and
test-only sales extraction. The earlier log remains. Final unmount cases include
an additional delayed denial-body phase; repeated counts are not additive.

An initial timer handle used the merged Node type; browser timeout handles now
use number. React purity also rejected a component-local load-time Date.now read.
The timestamp is now captured by the asynchronous data reader and updated through
cancellable browser timer callbacks. No lint rule was disabled. A targeted ESLint
attempt from the repository root lacked the web configuration; the final existing
npm lint ran from apps/web and passed. All initial compiler/lint logs remain.

The final body-read lifecycle guard was strengthened before publication, and the
final source was rebuilt and full/targeted checks rerun. Final native verification
uses the exact unchanged final export. The owned-close guard ignores only an exact
session-gone CDP reply after its own target close; all live execution errors fail.

Raw failure/final receipts, original source/export archives, temporary baseline
workspace, build/type/lint/test logs and SHA256 manifest remain outside tracked
source at `/workspace/c11-status-navigation-20261107`. Active browser output is
excluded under `.build-check/status-navigation-browser`. No raw laptop/customer
evidence, dependency graph, compiled artifact or model data is published.

## Integrate and complete original acceptance

Integrate six implementation paths, the three bounded issue records and this report
over `5f4fdc7`, preserving newer Desktop work. The frontend artifact must be rebuilt
because three pages changed. The backend is unchanged here; Desktop's1,991API,
music SQL and live C9 payment results retain their own source/receipt boundaries.

Prioritized remaining work for these records:

1. Real Identity/role/tenant/SQL: compare grant display with actual authorization
   before/at/after expiry, revocation and permanent controls; test clock differences
   and actual actor boundaries. Do not use client labels as proof of permission.
2. Real Platform Owner status operations: rejected/no-write and committed-but-lost
   responses, exact audit/rollback and current invoice/support priority/status;
   independent-client changes and fresh views. Synthetic commits do not certify it.
3. Existing all-role navigation/back/query, accessibility/keyboard focus and device
   matrix, plus module/critical/release closure. All three records remain OPEN.

```bash
node QA/tools/status-navigation.test.cjs
node QA/tools/run-frontend-tests.cjs
cd apps/web
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
./node_modules/.bin/tsc --noEmit
npm run lint
cd ../..
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/status-navigation-browser.cjs
git diff --check
```

`QA_STATUS_API_ORIGIN` accepts canonical unprivileged HTTP127.0.0.1origins only,
matching the compiled export (Desktop may use54999). Requests are intercepted and
allowlisted by exact route/method; unknown traffic fails. No PENTA/model/inference
request occurred. This is a fixture client, not real host acceptance.

The original118-record queue is a snapshot. Current base also contains Desktop's
separately discovered finance-decision and music-audit records; no record is hidden
or closed to reduce the count. P0/P1/role/privacy/critical/device/restore/current
CI/images/audits/staging/release and AI outcome qualification remain open. No
package/model/image acquisition, remote audit retry, environment/public-access/
VPN/TCP/privacy change, models, main merge, Azure or customer DB. Mini/Plus/Pro
unqualified, Ultra deferred; model loading/training/inference/activation stop held.

Next: Sol High for actual host/security/transaction acceptance; Medium for settled
UI QA. Refresh exact-path ownership before another independent packet.
