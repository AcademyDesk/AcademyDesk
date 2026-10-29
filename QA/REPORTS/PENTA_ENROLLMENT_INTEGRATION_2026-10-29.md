# PENTA shared-enrollment integration — 29 October 2026

Status: SHARED DOMAIN FOUNDATION VALIDATED LOCALLY on the cloud/finance/contract
integration chain. Conversational enrollment approval/execution remains pending.

Worktree `D:\AcademyDesk-penta-enrollment`, branch
`codex/penta-enrollment-integration-20261029`, base
`d44aea500dc14f050e50c78923b0435119b892aa`. The original implementation/tests
were imported byte-for-byte from published preservation commit6887e6e.

## Resulting behavior

Manual enrollment creation, lifecycle change and transfer now use the same
deterministic operations as the independently authorized Owner/Admin service
facade intended for later conversational integration. The service requires a
shared SQL/Identity transaction before relational access and current stored user,
role, academy and module authority; model claims cannot substitute for those.

Retained admission rules serialize capacity writers with the existing academy
admission lock. Duplicate Active/Waitlisted entries are refused; the existing
waitlist row is activated. Course prerequisites remain same-tenant and promotion
may count its verified source completion within its own transaction. Transfer
preserves source history and saves both sides together. Batch updates/promotions
participate in the same capacity boundary. Manual granular access remains with
the existing filter; the PENTA facade deliberately does not grant FrontDesk or
PlatformOwner tool access.

Only the existing enrollment facade is registered as a domain service. No model
tool/interpreter, conversation endpoint, automatic approval, receipt or UI is
registered. Inactive typed enrollment adapters carry forward strict IDs, fields,
date/status and lifecycle-reason validation. Domain service success is not a
durable approved conversation receipt.

The existing attribute/filter opt-in for platform-owner audit transactions was
carried forward as a small prerequisite. Default bypass is unchanged; enrollment,
batch and promotion actions do not opt platform owners into this policy. No PENTA
controller audit exemptions were imported. Current cloud finance and all other
controller/access-filter behavior are preserved. QaApiFactory gains only its
existing trusted domain-interceptor injection after owned SQL preflight.

No DbContext/entity/migration or frontend/model/configuration changes. No customer
database use, main merge, model loading/training/inference experiment or Azure.

## Validation actually completed

- Full unfiltered API suite: **1,515passed, zero failed/skipped**. The143 retained
  cases extend integrated1,372; these are not new examples or a replacement for
  the separate larger local3,183 historical suite.
- Rebuilt SQL harness: zero warnings/errors.
- Retained **28lifecycle native SQL/HTTP cases** and **57shared service/HTTP/SQL
  checks**, including15mixed Manual/service races:5last-seat,5duplicate and5
  transfer/create. Exact success/conflict outcomes and fresh row/source-history
  counts are required;429 cannot count as a guarded result.
- Current-user/role/academy denials, revoked membership despite stale Owner
  claims, inactive/missing authority, relational shared-transaction requirement,
  FrontDesk Manual permission and teacher/anonymous refusal pass.
- Required-audit fault controls roll back service/native creation, lifecycle
  and both transfer halves. Refusals are committed by the fixture too, proving
  no hidden staged mutation. No production audit record or domain action.
- Final window `20426cd275d842a6904dd9db822451c9`, actual SQL run
  `445147690a884b37bca71fa7751f4576`. Cloud84application/7Identity migrations
  applied only to run-owned SQL. Exact original PENTA services/images/health,
  API200, starting Docker inventory, run-owned cleanup and shared lease PASS.
- Source-bound receipts remain local in `.build-check/penta-enrollment/` and
  `QA/EVIDENCE/enrollment-shared-sql-445147690a884b37bca71fa7751f4576/`.
  No raw evidence/build output is committed. CI now includes EnrollmentShared;
  no fresh GitHub run or combined release claim.

First windowa62bddfa9fa24cd9bd135896aa1a0726 failed compilation because the
preserved tests referenced the existing platform-owner audit opt-in helper that
was not in the cloud baseline. Added only that attribute/helper/conditional
transaction branch, preserving default policy and every test assertion. No SQL
started in the failed window; exact services/health/inventory restored. Original
failure receipt retained; the successful successor does not rewrite it.

## Next work and release limits

Protected enrollment draft/preparation/scoped approval/execute/receipt/recovery
remains open, followed by the shared Core conversation and UI. Revalidate current
source/target/capacity/prerequisites/authority at execution. Existing protected
student/teacher persistence/services/UI and its migration integration still need
coherent transfer from the preservation branch. Do not copy overlapping local
payroll migrations over the reconciled cloud history.

Real Mini semantic acceptance, other workflow cohorts, all-role/customer/device,
privacy/security, source-bound CI, restore/rollback/staging and release gates
stay open. Owner stop before candidate loading remains. Source portability is
not a qualified runtime: Mini preservation-branch CI has a recorded failure due
to missing laptop-only historical prerequisites; see the portability report.
Sol High next for protected host integration; Sol Medium for settled UI/docs.
