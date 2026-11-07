# C16 critical support triage — 7 November 2026

Cloud owner. Status IMPLEMENTED / CLOUD VALIDATED; local acceptance pending.
Branch `codex/cloud-c16-support-triage-20261107`.
Clean base `cfa5832ae25c53c8d5bcba859ccba2b21b89060b`.
Latest Desktop inspected `e9a0946` includes C15, with C13/C14 SQL results at
`9631175`. No competing support-page claim found in those current handoffs.

Exact paths:

- `apps/web/src/app/platform/control/page.tsx` — support triage predicate only
- `QA/tools/support-triage.test.cjs`
- `QA/tools/support-triage-browser.cjs`
- `QA/ISSUES/BUG-FUNC-0011.md`
- `QA/HANDOFF/C16_SUPPORT_TRIAGE_2026-11-07.md`

Reproduce the academy Critical payload and owner high-priority count/modal.
Include canonical Critical alongside High/Urgent, handling priority casing from
the existing permissive API; retain resolved/closed exclusions. Preserve C11
status reconciliation, permissions, backend contracts and all unrelated tabs.
Test actual TSX, actual Chromium on the existing/configured exact-lock export,
all affected priority/status combinations and retained C11 controls. Build,
types/lint and full frontend once after final source. No backend source change:
retain C15's2,084 API passes, not a newly executed backend result.

Synthetic API responses are UI evidence only. Desktop must perform real
authenticated academy submission → owner triage → resolve/close acceptance,
audit/tenant checks and device/critical closure. Keep the issue OPEN until then.
No shared handoff overwrite, new packages/images, model loading, configured SQL
connection, main/Azure/environment publication or public-network change.
Sol High for authority/runtime acceptance; Medium for settled display/import.

## Implementation and reproduction

Implementation `fe0d87af3ba9694ddc3a5c86f9bca9cf54283b47`; claim `07da936`.
Final native driver `8ee9e2618d679024e7da87b61dda5136602cd055`.
The only application change includes Critical in the urgent-case predicate,
normalizing whitespace/casing for known priorities. Resolved/Closed exclusions,
the intake payload, existing priority labels and all status-write handlers remain.
No API/controller/schema/permission or model change.

Qualified unchanged-source baseline:19/26 controlled tests PASS,7 FAIL;
native6/8 PASS,2 FAIL. Both high-priority native views omit Critical requests.
Other views show all23 fixture records,13 open and10 resolved; the intended
urgent group contains8 open High/Urgent/Critical records, including existing
case/whitespace variants. Unknown/Low/Normal remain out of that group.

First harness attempts are retained separately: the unit fixture lacked the
academy's existing subscription fields; the browser checked the shell before
data loaded. Both were corrected before the qualified baseline. The baseline
and repaired functional assertions are the same. No test weakening or skipped
case. Native uses existing installed Chromium with its normal sandbox and
synthetic APIs; actual device/host authority is not inferred.

## Executed and retained evidence

- New26 controlled checks and retained28 C11 status/navigation checks:54/54
  PASS, zero skips. Every canonical priority/status combination, priority
  variants, unchanged All/Open/Resolved views and close actions, and an actual
  academy TSX Critical submission feeding the owner TSX are covered. The latter
  uses a clearly synthetic API response; it is not real Identity/SQL acceptance.
- Exact-lock Next16.4.3 webpack export PASS;89 static generation entries,
  570 files and87 unchanged RSC aliases. The compile uses isolated
  `NEXT_PUBLIC_API_URL=http://127.0.0.1:49142`, not the default development API.
- TypeScript PASS; lint PASS with zero errors /62 existing warnings.
- C15's API2,084/2,084 PASS is reused, not rerun. Its recorded Profiles/test
  source and both compiled assembly hashes still match. No backend source
  changed. This packet does not convert that result into real SQL acceptance.
- Full frontend:2,042 PASS /3 FAIL /zero skips or cancellations;75 files,
  2,045 tests. The failures are the same genuine communication-SQL receipt
  checks absent in Cloud; they remain failures, not relabelled or manufactured.
- Final native:8/8 PASS at320/light and1440/dark, zero runtime/interceptor
  errors or API mutations; normal Chromium sandbox and owned cleanup verified.
  All570 exported file hashes remain unchanged after browser verification.
- Two intermediate native attempts each passed7/8: one timed out loading a
  desktop All-cases fixture while the full suite ran; a sequential rerun hit
  a Runtime.evaluate timeout on desktop High priority. Both failures remain
  retained. The final driver brings each page to the foreground and checks
  all four views on one page per viewport, matching a normal user journey.
  The eight functional assertions and timeout thresholds are unchanged.

```sh
node --test QA/tools/support-triage.test.cjs
node --test QA/tools/support-triage.test.cjs QA/tools/status-navigation.test.cjs
NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack
npm run lint
./node_modules/.bin/tsc --noEmit
node QA/tools/run-frontend-tests.cjs
QA_BROWSER_EXECUTABLE=/usr/bin/chromium node QA/tools/support-triage-browser.cjs
git diff --check
node QA/tools/check-production-gates.cjs
```

Build/lint/tsc run in `apps/web`; other commands at repository root. Existing
dependencies only, no install or image download. Production guard intentionally
exits1 for the five still-open/unverified P0 records. Exact-source remote CI is
UNKNOWN: no GitHub workflow/status connector is available here, the previous
API restriction remains, and feature pushes do not match `verify.yml`'s push
filter. No workflow dispatch occurred.

Raw artifacts excluded from Git: `/workspace/c16-support-triage-20261107`,
including original source, qualified and preliminary logs/receipts, full-suite
summary, before/after export fingerprints and source/artifact SHA256 manifest.

## Desktop return and blockers

Import/review only these five paths. Preserve newer Desktop shared handoffs
and C15/finance/restore raw evidence. Authenticate as an academy administrator,
submit Critical support through the real UI/API and verify exact SQL priority,
one attributed audit, and owner All/Open/High-priority count and details. Check
High/Urgent/Normal controls, status transitions to Resolved/Closed removing only
the affected case, real tenant/role denials and any audit-failure boundary.
Complete linked critical and physical-device cases before closing
BUG-FUNC-0011; bounded cloud evidence alone does not close it.

The five P0s, missing genuine cloud SQL receipts, remaining all-role/critical/
device coverage, exact CI/images/security and staging/production recovery stay
OPEN. Mini/Plus/Pro are unqualified; Ultra deferred and model loading stopped.
No main merge, Azure/environment publication or public-network change.
