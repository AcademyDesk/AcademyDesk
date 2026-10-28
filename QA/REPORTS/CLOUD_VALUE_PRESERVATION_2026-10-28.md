# Zero values and unrepresented settings preservation — 2026-10-28

MODEL: Session Codex model; no override or delegation.
TOOL: Codex cloud terminal.
REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/release-export-security; base fa0ded212c1289c65d57dccb35ac7ce651c9ca31.
ENVIRONMENT: Managed Linux; controlled TSX/synthetic replies, no SQL/browser.
TASK: Continue BUG-DATA-0050/0053/0054 without changing financial business rules.
MAY MODIFY: Teacher payments, music status payload, finance-policy payload and QA.
MUST NOT MODIFY: Financial arithmetic, API rules/RBAC/schema or customer data.
VALIDATE: Exact zero/null/positive/model/theme payloads and affected frontend checks.
STOP WHEN: Focused repairs validated and pushed with remaining gates recorded.
NEXT ROUTE: Codex for remaining tracked integrity/security and guarded SQL/HTTP.

## Reproduction and final behavior

The initial 41 controlled actual-TSX cases produced 20 failures/21 passes.
Teacher rate hydration converted numeric zero to null during unchanged resaves;
even an initially string-zero fixture failed after one successful hydration.
Music status changes sent zero scores as null. Every Finance Policy payload sent
null for the absent payslip-theme field, including when Standard/Compact/
Professional had been loaded. Teacher payment edits also accepted previous data
under a pending new teacher and accepted a late save into the new teacher's UI.

- **Teacher compensation (BUG-DATA-0050):** normalize editable rates to strings;
  explicitly distinguish empty input from zero when constructing numeric/null
  payloads. Existing Monthly/Hourly applicability and optional-null rules remain.
  Reuse the loaded-record editor guard for pending/failed/reversed selections,
  old save completions, retries, duplicate pending saves and transport recovery.
  This edits compensation metadata; no payroll calculation or payout is performed.
- **Music progress (BUG-DATA-0054):** status-only PATCH uses nullish score fallback,
  preserving zero and positive scores while absent scores remain null. All other
  PATCH fields and server behavior remain unchanged.
- **Finance Policy (BUG-DATA-0053):** preserve the loaded payslip theme because
  that field is not exposed by this editor. Other fields still come from its
  form; a missing loaded theme retains the existing Standard default. This is
  ordinary single-editor snapshot preservation, not a concurrent-update solution.

## Validation

- All 51 expanded focused cases PASS: 37 compensation, eight music-status and
  six finance-theme checks; zero failed/skipped/cancelled. Includes actual typed
  zero edits, loaded zero/null/positive/undefined values, repeated resaves,
  Monthly/Hourly transitions, selection/save races, retry and transport rejection.
- Full frontend: 47 current files, 1,098 tests; 1,095 PASS / three FAIL,
  zero skipped/cancelled. Exit1 retained for the same historical communication
  SQL-evidence assertions; original log remains absent and was not fabricated.
- TypeScript PASS; targeted lint zero errors/one inherited music-effect warning.
  Full lint PASS with zero errors/62 warnings. No suppression.
- Production webpack build and diff checks PASS. API 1,073-pass and package-audit
  results retained, not rerun for frontend-only work. PentaMini unchanged.
- Logs remain ignored under .build-check/: value-preservation-before.log,
  value-preservation-final.log, value-preservation-full-frontend.log,
  value-preservation-lint.log, value-preservation-build.log and frontend-tests/.

The three issues remain OPEN. No real HTTP/SQL persistence, actual browser/device,
denied SQL no-write, concurrent settings preservation, music stale-response,
server idempotency, complete financial critical regression or release acceptance
is established by synthetic request tests. No financial rule/schema changed, no
configured application DB/seeder was used, and no real finance action occurred.
Cloud CDN settings are still unapplied; no environment publication/configuration,
main merge or Azure deployment was performed. Recurring development-branch pushes
remain user-authorized.
