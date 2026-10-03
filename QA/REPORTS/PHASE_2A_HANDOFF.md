# Phase 2A acceptance and repair handoff

Date: 2026-09-30. Checkout: `D:/AcademyDesk`. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c` plus the scoped dirty-tree evidence below.

## Decision

**Phase 2A CLOSED WITH RECORDED LIMITATIONS — accepted for targeted Phase 2B repairs, not release.** The six bounded reproduction-phase criteria in [the starting plan](PHASE_2_START_PLAN.md) have evidence. All five P0 issues remain **OPEN / RUNTIME-REPRODUCED** and their business assertions remain **FAIL**. Phase 2B has **NOT STARTED**. This report does not approve the application, Azure configuration, visual baselines or untested workflows.

The accepted Astra Phase 1 audit was not repeated or regenerated. Of its 411 pinned source records, 408 still match; three changed records are the API project, API Program and test project. These are attributable to the harness/testability work and the separately approved private Blob foundation, not finance repairs. The pre-existing student onboarding/profile edits still match the accepted working-tree baseline and were preserved. [Source snapshot](PHASE_2A_SOURCE_SNAPSHOT.json) records the comparison and 23 scoped raw-byte hashes; its compact records-array SHA256 is `cdb900c39e26ee7eb23757f618ff0660c39b308fb67fb46f71b56eacc9384512`. This is a scoped fingerprint, not a hash of every dirty/untracked file.

## Acceptance criteria and evidence

| Criterion | Result and limits |
| --- | --- |
| Fail-closed isolation and ownership | PASS for the implemented guard tests and recorded fresh local runs. Generated exact database/login, labelled Docker host, loopback-only port and ownership markers were checked. Invalid cleanup marker was refused; resolved application and Identity connections were checked against the exact owned target and generated runtime credential. No development/Azure database was used. |
| Real host, schema and runtime routes | PASS for the contained Testing host using real Program/TestServer, Identity, authorization and audit filters. Both migration sets applied: 79 application + 7 Identity, 86 recorded migration IDs. Runtime inventory recorded 293 method/route entries. This is metadata capture, not execution coverage of all endpoints. |
| Five P0 reproductions with fixture controls | COMPLETE, all five business results FAIL. Health, real login, anonymous protected-request denial and bounded tenant A/B read/write controls PASS. Fresh SQL and read API observations are recorded; an accepted HTTP response is not treated as business success. |
| Original failing evidence and stable issue state | PASS for evidence retention. Five original per-case reports and the consolidated sanitized log are linked below. No issue was closed or downgraded. |
| Repeat on fresh owned resources; existing checks | PASS for repeated baseline reproduction and identical schema/route inventory. API suite 45/45 passes with no failures/skips. Current frontend lint FAILS with 15 errors and 8 warnings; retained honestly, not suppressed. |
| Consolidated handoff | COMPLETE: commit/dirty hashes, run timing, schema/package versions, synthetic fixture summaries, HTTP/SQL outcomes, cleanup and policy/coverage limits retained here and in linked artifacts. No commit/push/deployment performed. |

The five individual cases had already completed twice. These additional two combined captures fill the new runtime-inventory/schema evidence gap and check the handoff instrumentation; they do not repeat static audit work.

## Fresh combined captures

| Run ID | UTC start | Loopback SQL port | Duration including SQL readiness | Outcome |
| --- | --- | --- | --- | --- |
| `fd522b5268184938b15b99df92684968` | 2026-09-30T12:10:24.6386487Z | 65364 | 47.8 seconds | Controls PASS; five P0 cases FAIL; infrastructure exit 0 |
| `fd4c6ce456d84109bdf15ae8b458f68b` | 2026-09-30T12:14:08.0616598Z | 54812 | 43.0 seconds | Same control/case outcomes and inventory/schema; infrastructure exit 0 |

SQL Server version `16.0.4295.3`, Developer Edition (64-bit), cached SQL Server 2022 image digest `sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090`. .NET SDK `10.0.401`; resolved packages: EF SQL Server and ASP.NET MVC Testing `10.0.12`, Microsoft.Data.SqlClient `6.1.6`, Azure.Identity `1.21.0`, Azure.Storage.Blobs `12.29.2`.

[Schema evidence](PHASE_2A_SCHEMA_EVIDENCE.json) retains both migration lists and the actual applied history. [Runtime endpoint evidence](PHASE_2A_RUNTIME_ENDPOINTS.json) contains 282 controller method/routes, 10 framework Identity method/routes and one health route. Four custom AuthSession controller routes are included in the 282 controller entries, not the 10 framework entries. Both captured endpoint arrays have exact compact UTF-8 SHA256 `01DA5334907B569F29A6130AB75C76B2CC89D0A0B1F59F87E954BA1FC9DA6CEC`.

The first console summary labelled all 14 `/api/auth/` routes as Identity; the repeat fixes that display to 10 framework routes. The endpoint arrays were identical. [Exact captured endpoint line](../EVIDENCE/logs/phase-2a-runtime-endpoints.log) preserves the original serialization for digest verification. Authorization metadata is not effective access proof: custom filters and framework token checks require action-level scenarios, and absent AllowAnonymous metadata does not by itself mean a route is protected. Development-only OpenAPI is not mapped in this Testing host.

## Confirmed failures and repair queue

Synthetic fixtures use isolated tenant A/B identities and records, real Identity tokens kept out of reports, actual API mutations and fresh-context SQL reads. No customer media or financial record is used. Detailed variants, fixture controls and original evidence remain in the existing reports.

| Test / P0 issue | Confirmed baseline failure | Evidence |
| --- | --- | --- |
| SECURITY-FILE-001 / BUG-SEC-0001 | Teacher upload is anonymously and cross-tenant readable (200); Range also leaks (206); unpublishing does not revoke the static URL. Authorized read matches the synthetic payload. | [Private file reproduction](PHASE_2A_PRIVATE_FILE_REPRO.md), [issue](../ISSUES/BUG-SEC-0001.md) |
| FINANCE-RULE-001 / BUG-DATA-0001 | Reconciliation changes student balance to 1000 while admin balance is 400 after payment 600; extra payment 500 is accepted, collected ledger 1100 on invoice 1000. | [Reconciled payment reproduction](PHASE_2A_RECONCILED_PAYMENT_REPRO.md), [issue](../ISSUES/BUG-DATA-0001.md) |
| FINANCE-RULE-002 / BUG-DATA-0002 | Approved adjustment 200 + payment 600 leaves 200, but 300 is accepted; exact settlement remains PartiallyPaid; fully adjusted invoice accepts another payment. | [Adjustment reproduction](PHASE_2A_INVOICE_ADJUSTMENT_REPRO.md), [issue](../ISSUES/BUG-DATA-0002.md) |
| PAYROLL-RULE-001 / BUG-DATA-0003 | Gross 1000, deductions 1000.01/1001 persist negative Paid payouts. Negative deduction controls reject without payout writes. | [Payroll reproduction](PHASE_2A_PAYROLL_BOUNDARY_REPRO.md), [issue](../ISSUES/BUG-DATA-0003.md) |
| FINANCE-RULE-003 / BUG-DATA-0010 | Voiding full/adjusted payments leaves Paid invoices with collectible balances 1000/800 excluded from collections; partial invoice status also remains stale. Generic Reconciled transition accepts missing reference/timestamp. | [Transition reproduction](PHASE_2A_PAYMENT_TRANSITION_REPRO.md), [issue](../ISSUES/BUG-DATA-0010.md) |

[Combined sanitized run log](../EVIDENCE/logs/phase-2a-handoff-runs.log) retains actual HTTP outcomes, fresh SQL amounts/statuses and synthetic record identifiers. Infrastructure exit 0 means the diagnostic run completed and cleaned up; **it is not a green business-regression or release result**. Before wiring repairs into a critical CI gate, make failed security/finance expectations fail the regression process rather than relying on reproduction-mode exit status.

## Checks, cleanup and contained source changes

- Harness build: PASS, zero warnings/errors. [API unit output](../EVIDENCE/logs/phase-2a-handoff-unit.log): 45 passed, zero failed/skipped. Unit PASS does not override integration FAIL.
- [Current lint evidence](PHASE_2A_LINT_EVIDENCE.json): exit 1, 15 errors / 8 warnings. Frontend files were not repaired in this batch. This remains a release blocker, not a reproduction-harness failure.
- `git diff --check`: PASS; Git emitted line-ending notices but no whitespace errors. Historical Phase 1 source validation was not weakened or claimed to pass against the expanded Phase 2 tree.
- Targeted handoff integrity assertions: PASS for all 23 scoped hashes and their manifest digest, exact captured endpoint digest/JSON/counts, applied migration union, two retained FAIL outputs for each P0 case, five OPEN / RUNTIME-REPRODUCED issue records, lint totals and all 19 report links. This is artifact consistency, not additional business coverage.
- Both fresh runs refused a false ownership marker before cleanup, then removed the exact marked database and generated runtime login and checked their absence. Owned host folders were removed through the guard boundary; exact labelled containers were stopped/removed. A final read-only `docker ps -a --filter label=academydesk.qa.run` returned no containers. Cached SQL images remain. Removed synthetic container/database data cannot be recovered from those containers.
- The Program hook exposes partial Program for TestServer and avoids pre-validation webroot directory creation in Testing. Non-Testing webroot behavior, middleware order and existing auth/audit filters are retained. Media DI registration, Azure SDK packages and disabled non-secret Production media settings belong to the separately user-approved [Blob foundation](TEACHER_MEDIA_BLOB_FOUNDATION.md); they do not secure the existing public upload route. No finance controller, schema migration, student UI, token lifetime or Azure resource was changed in this handoff.

## Pending policies, limits and next bounded task

**No policies were invented from current runtime acceptance.** Zero-net payout handling and restoration of Voided payments remain POLICY-PENDING. The latter needs authority, evidence and audit expectations. A focused review of these decisions is needed before the affected repair slices; unrelated confirmed defects do not require another full audit.

Next: Phase 2B, one confirmed P0 at a time, starting with private-material access. Use the accepted user direction (private Azure Blob, resumable upload, explicit 2 GB maximum, download fallback) and the existing foundation. First bound the authenticated upload/read/revocation contract and disable public exposure; then implement and rerun the original case, access/Range regressions, module suite and critical checks. Do not silently migrate or delete live files. Real Azure IAM/private-container verification, production legacy-file treatment and device-format preview remain separate prerequisites/coverage, not passed here.

Following the existing model allocation: **Sol High** for the targeted repair and executable regressions; **Astra High** only for focused security/financial policy ambiguity or release decisions; **Terra** only for repetitive extensions after a passing pattern exists. This does not repeat accepted Astra audits or switch models automatically.

Broader guardian/link/role/revocation matrices, concurrent finance and duplicate writes, all payment-state pairs, optional/null/save/session behavior, media endpoint/browser integration, desktop/mobile visuals and Android/iOS recording/preview remain later verification. TestServer is not a browser/device certification. No release or visual-baseline approval, commit, GitHub push or Azure deployment is authorized by this Phase 2A handoff.
