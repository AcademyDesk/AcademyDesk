# Operational availability, imports and expense integrity — 2026-10-28

MODEL: Session Codex model; no override/delegation.
TOOL: Managed-cloud terminal; existing synthetic and guarded SQL infrastructure.
REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/release-export-security; basec25b6b83381b28bf759a47406cefc707baa7ff1c.
TASK: Continue BUG-API-0008, BUG-FUNC-0031, BUG-DATA-0047/0049 in one operational
record-integrity batch while critical invoice CI runs.
MAY MODIFY: Related controllers/tests, guarded module/CI and QA records.
MUST NOT MODIFY: Customer data, unrelated permissions/schema/finance policies.
VALIDATE: Real MVC routing plus controller serialization/no-write cases; full
API regression; compile explicit SQL/HTTP/audit cases; inspect actual CI later.
STOP WHEN: Tested batch pushed; keep moving on remaining independent work.
NEXT ROUTE: Codex for candidate CI outcomes and remaining high-priority gates.

## Repairs and evidence

| Issue | Resulting behavior | Local focused proof |
| --- | --- | --- |
| BUG-API-0008 | Removed duplicate HolidayDeleteController; retained canonical Holidays DELETE, same URL and module/role gates. Endpoint inventory has no equivalent controller method/route collisions. | Five tests: actual Program endpoint matching/anonymous HTTP401 and own/foreign/missing/repeated controller deletion. |
| BUG-FUNC-0031 | Unavailable/foreign teacher reference uses a neutral label; own/inactive teacher names and workload totals remain. No foreign name is disclosed. | Five serialization checks force deferred workload enumeration; missing/foreign/own/inactive/null/cancelled controls. |
| BUG-DATA-0049 | Both validation and import reject within-file trimmed/case-insensitive duplicate emails with row feedback, and null rows fail safely. Existing identity lookup trims stored values. Optional blank identities and cross-tenant independence retained. | Nine import cases; exact accepted counts and no partial writes on invalid input. |
| BUG-DATA-0047 | Update checks branch belongs to academy before mutation. Recorded is the only implemented expense lifecycle state that can be assigned; unsupported Paid/Approved/arbitrary values reject. Omitted status preserves stored history; currency is retained. | Nine expense cases; foreign/missing/null/local/inactive branch and status/currency/no-write controls. |

Initial routing/dashboard ten cases: four FAIL/six PASS. Initial import/expense
18 cases:11 FAIL/seven PASS. After repair all28 pass, plus two atomic-boundary
checks. Full API **1,171 PASS**, zero failed/skipped. These tests do not establish
SQL concurrency, browser/device or full role matrices.

Imports and canonical holiday DELETE now opt into the existing normal-actor
domain/audit transaction. Accepted domain changes roll back if its audit fails.
This does not redesign the existing platform-owner filter bypass or invent a
new expense approval/payment workflow. Existing historic expense states are not
bulk rewritten, and no customer/import/bank operation was performed.

## Guarded SQL continuation

New RecordIntegrityRegression module compiles with zero warnings/errors and is
wired into the existing owned SQL runner/CI.22 prepared real HTTP/SQL cases cover
dashboard tenant/name/read-only boundaries; holiday anonymous/wrong-role/foreign/
own/missing/repeated deletion; duplicate/null/accepted/existing imports; expense
branch/status/currency/foreign actor checks; and audit-fault rollback for holiday
deletion, import and expense update. Synthetic fixture feature flags are scoped
to the generated DB; production permissions/modules were not broadened.

**Those22 cases have not executed locally.** Their compiled preparation is not a
runtime pass. Same-file duplicate checks do not guarantee uniqueness across
concurrent imports/manual enrollment, which remains an open identity-policy and
SQL acceptance area. Actual role/SQL/browser/critical closure remains mandatory.

The workflow now writes source SHA and actual GitHub step outcomes to its job
summary without copying credentials or raw logs. API-image/startup, SQL/private
media, full frontend and build outcomes remain distinct. Actionlint, PowerShell
runner parsing and diff checks PASS. Earlier frontend/lint/build results retained;
no frontend source/dependency change in this batch.

Preceding verification run40728363572 is SUCCESS for exact52495e2. It proves that
older source's required CI steps, not the newer invoice/record changes. Detailed
logs/artifact contents still require sign-in; ordinary overview metadata is
readable. No SQL log was fabricated to turn the local three missing-evidence
frontend failures green.

All work remains on the development branch. Workflow/deployment guards only
become the default-main behavior after a reviewed merge; main was not changed.
No environment publication, Azure workflow dispatch/deployment, configured
application DB use or PentaMini modification occurred. Existing production,
critical P0/P1, browser/device, concurrency and restore gates remain OPEN.
