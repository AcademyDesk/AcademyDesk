# C11 access/status/navigation batch — 7 November 2026

Status: CLAIMED before edits. Base/latest Desktop
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
