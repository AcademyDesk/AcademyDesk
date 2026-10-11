# Phase 2B — Teacher compensation zero-rate round-trip

Date:2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `0e2a2150569dcf1fad666cd8d073bf1262439aa4`. Existing BUG-DATA-0050 continuation, Sol High. **OPEN / RETEST**, not issue closure or enterprise/release acceptance.

## Reproduction and narrow repair

Actual TSX handler tests against unchanged source:39 cases,22 PASS/17 FAIL. Loaded numeric0 is falsy, so the previous save payload converts it toNULL. A typed string"0" initially saves0 but is hydrated back as numeric0 and loses that value on the next save. This affects monthly salary, standard hourly rate and all three optional hourly bands. Baseline failures are retained, not weakened or retrospectively marked passed.

Only application change: `teacher-payments/page.tsx` uses a single explicit null/undefined/empty-string converter for each applicable rate. Numeric0 and string"0" convert to0; absent/empty optional rates remainNULL. Monthly still excludes all hourly fields; Hourly still excludes monthly salary. Date conversion, native required/min0/step0.01, request fields/route/PUT, existing success/error messages, model rules and tenant/role authority remain unchanged. The type now describes actual API numbers/nulls and native input strings; no new helper package or duplicated business logic.

Backend already accepts zero and rejects missing primary/negative rates. No API/domain service/schema, stored-data rewrite, payroll calculation, salary policy, zero-net payout policy, accounting/tax rule, PENTA capability/Core or model change. Compensation JSON and PayrollProfiles are distinct stores; this repair does not establish compensation-to-payroll propagation or payment semantics.

## Verification

| Layer | Result and exact scope |
| --- | --- |
| Actual TSX |39/39 PASS after repair. Five fields × six numeric/string/positive/null/empty inputs with second unchanged save=30; five real input-event zero/hydrated resaves; two model/date exclusion cases; two rejected negative draft controls. Baseline22/39,17 FAIL. |
| API regression |35/35 unchanged MetadataShapeTests PASS; previous invalid JSON/alias/zero/date guards retained. Not full enterprise suite. |
| Native SQL/Identity/HTTP |39/39 PASS, no429. Run `5d7d3225ff7b4539930fd0a2cad708fe`, port65039, migrations88 application/7 Identity, scoped runtime login. |
| SQL positive matrix |Nine contracts × initialPUT/exact response+fresh SQL, GET/exact rates+date, unchangedPUT/exact response+fresh SQL=27. Monthly0/positive/date; standard hourly0/optionalNULL; all hourly0; each optional zero independently; all optionalNULL; positive fractional bands/leap date. Foreign teacher sentinel unchanged. |
| SQL negative matrix |Missing primary2, each of five negative rates, unsupported model=8 exact400/no teacher or audit changes; anonymous401, Teacher403, foreign Admin403, foreign route403=4. No permission relaxation, quota override or retry-until-green. |
| Exported browser |40/40 synthetic cases:320/1440px × light/dark × six valid flows plus four native-invalid flows.24 valid cases each save/reload/resave,48 exactPUTs;16 negative/required native-invalid cases noPUT. Exact five-field/date JSON, displayed values, reset-to-blank optionalNULL, two-save hydration, no horizontal overflow and zero console/page errors checked. |
| Visual review |320px light monthly0 and dark optional hourly0 screenshots inspected; zero inputs, success text, labels and stacked panel remain readable. No claim of final all-portal redesign/accessibility/physical-device acceptance. |
| Web |Installed TypeScript PASS; production webpack export83/83 pages PASS. Target lint exit0,0 errors/1 inherited `loadCompensation` effect dependency warning. Same warning independently reproduced on starting HEAD; **not clean lint** and not suppressed. |
| Harness/cleanup |Build0 errors/0 warnings. Exact owned database/login/container removed; mismatched ownership-marker refusal retained. Other containers untouched. |

Native SQL exercises existing real API contracts, not browser forwarding. Browser transport is synthetic; these two proofs do **not** claim directly linked browser-to-SQL acceptance. A native frontend baseline was not run; actual-handler baseline reproduces the client defect and native SQL proves the existing server preservation contract. Tests ran against the preserved dirty snapshot, not acceptance/publication of unrelated Mini work.

Evidence intentionally local/untracked:

- `QA/EVIDENCE/teacher-compensation-roundtrip-20261011/baseline.txt`, `final.txt`, `metadata-retained.trx`, `sql-final.txt`.
- `QA/EVIDENCE/teacher-compensation-browser-1791699768135/result.json` and screenshots.

First final handler command used the web directory with root-relative evidence paths; it did not run those handler tests. Corrected root-directory invocation passed39; the installed TypeScript/lint commands in that web invocation completed separately. No assertion or existing suite was weakened. Unrelated evidence was preserved.

## Remaining gates and next route

- BUG-DATA-0050 remains OPEN for directly linked browser/real SQL, physical Android/iOS/tablets, complete model-transition/role/fault/critical acceptance.
- This narrow converter does not fix the wider editor's loading readiness, stale teacher selection, duplicate/pending submit, network/5xx recovery, malformed successful response, accessible status or audit-fault/transaction behaviour. Existing safeguards/release queue remain controlling; do not claim the whole compensation editor is finished.
- Large-decimal precision/resource limits, currencies/compensation-to-payroll policies and concurrent updates were not newly qualified. Zero permitted by the existing endpoint is not an approval of zero-net payroll payout policy.
- Original28 feedback checkpoints, accepted Astra audits, previous expense/work-item/import proofs retained, not restarted. Manual Orbit + Aura dark/Prism light rollout and PENTA-owned FW1 remain pending; no new AI tool or90% target acceptance.

Scoped feature commit/push only to `codex/penta-search`, after stage/secret checks. No main merge/Azure deployment/production DB or credentials. `.build-check` ignored, `QA/EVIDENCE` local, unrelated Mini/Platform/private files and older32/28 handoff/README additions excluded/preserved. This report's scoped Git history identifies the fix commit. No Mini handoff required for the host form conversion.

NEXT existing BUG-API-0005 publication boolean-binding reproducer, **Sol Medium** for bounded frontend serialization. Use **Sol High** if consequential domain/authority/SQL contract work is needed; do not automatically change this chat's model. Broader compensation recovery remains a separate High host-integrity/Medium settled-UI packet, not silently closed here.
