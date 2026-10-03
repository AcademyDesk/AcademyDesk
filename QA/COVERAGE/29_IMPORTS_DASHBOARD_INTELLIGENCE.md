# Batch 29 — Imports, dashboard, intelligence and development endpoint

2026-09-29. Source specification; runtime NOT RUN. Completes StudentImportsController (Validate/Import), DashboardController (Summary), AdminIntelligenceController (Get) and WeatherForecastController (Get):5 actions. These pages have no remaining native form declarations; Data Operations actions are composed file/button interactions already covered by operational visual and API scenarios.

## G02 — Student imports and exports

Data Operations parses CSV client-side by splitting lines and commas. It does not implement CSV quoting, embedded newline, BOM, header duplication or column-order handling. Test synthetic files only: quoted comma/newline, escaped quote, CRLF/LF, blank rows, missing/reordered/duplicate headers, Unicode, oversized file, invalid dates and browser file reset. The UI allows Import after rows exist regardless of a successful Validate result; authoritative Import revalidates basic server rules but user flow must not imply validation approval.

BUG-DATA-0049: server validation/import do not detect duplicate normalized emails within the submitted set. The Import query checks only rows already in the database, then AddRange persists all proposed rows. Test all-or-nothing behavior and actual SQL uniqueness; no database constraint is assumed from static source.

Import accepts first/last name, optional email/phone/student number/admission date. It validates email only by contains @ and does not validate duplicate student numbers, row limits, normalization or field lengths before SaveChanges. Test failure output shape and no-write guarantees for model binding, DB constraint and concurrent imports. Client handlers parse response JSON even on transport/non-JSON failures and use a shared message; test committed-write/readback ambiguity and durable success.

## G02 — Dashboard and intelligence

Dashboard requires an authenticated user whose AcademyId exactly matches route academy, then calculates counts, invoice total, completed-payment total, outstanding amount, attendance and academy-timezone schedule. Test owner/admin/teacher/family/platform/foreign user, invalid/missing timezone fallback, UTC/day/DST boundaries, cancelled sessions, null teachers, zero totals and mixed currencies/adjustments under the existing finance findings. Do not treat total invoiced minus completed payments as a reconciled accounting balance without Phase 2 evidence.

The dashboard page additionally requests students, invoices, payroll, sessions and attendance to populate detail modals. A partial failure silently produces empty details while the headline remains visible. Test summary success with each dependent failure, stale/reordered responses, many sessions/attendance calls, modal focus/close, mobile cards/table overflow, and whether “Outstanding fees” intentionally includes teacher payroll. User-facing success is a readable dashboard state, not proof of all detail data.

AdminIntelligence derives occupancy, overdue invoices, scheduled workload and attendance risk from independent datasets. Its overdue amount is gross total and ignores payment/adjustment state, so it must be reconciled against existing finance policies rather than presented as collection truth. BUG-FUNC-0031: a scheduled session with non-null TeacherId whose local teacher row is unavailable dereferences null in workload projection and can fail the complete endpoint. Test inconsistent/historical references, capacity zero, waitlists, attendance thresholds and unknown data without cross-academy disclosure.

WeatherForecast is the default unauthenticated development sample endpoint. It returns random current-local values and has no academy filtering. Confirm deployment/product policy; it must not be treated as AcademyDesk data or a functional health check. No real endpoint call was made.

## G03 — Access, failure and residual controls

Correction, Batch 37: StudentImports, AdminIntelligence and Dashboard all map Core and have no PermissionCatalog mapping. Same-tenant Owner/AcademyAdmin can pass the active-academy filter; ordinary users cannot, even with all grants. Dashboard additionally requires the resolved user's AcademyId to equal the route academy, including for a flagged platform owner who bypassed the global filter. Weather has no academy argument or authorization attribute, so its module/grant gates are N/A rather than an unmapped-controller denial. Program still rejects an authenticated inactive user. [Batch 37](37_EVIDENCE_RECONCILIATION.md#corrected-action-permissions) provides per-action expectations. Weather's production allow/remove policy and HTTP verification remain pending.

At 320/375/390/430, 768 and 1280/1440 plus 200% zoom, test file input focus, import status/action disabling, dashboard tiles/modals, long values, empty/error/loading states, activity/schedule overflow, date-year usability, scroll and keyboard/focus behavior. Shared-control reconciliation remains open for the 38 uncovered source controls; no visual or runtime PASS is claimed.

## Outcome

Two new OPEN P1 source findings: BUG-DATA-0049 (duplicate identities inside one import) and BUG-FUNC-0031 (orphaned scheduled teacher crashes intelligence). Adds4 controllers/5 actions; all82 native forms remain complete. Cumulative82/82 forms,611/649 controls,70/70 complete controllers. Shared-control reconciliation and G01–G03 acceptance remain open; source counters are not runtime pass or percentage completion.

Next: final shared-control reconciliation and Phase1 acceptance-gap review. No app repairs, commit or Azure deployment.
