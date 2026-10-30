# Production follow-up — 30 October 2026

Branch: `codex/penta-shared-readiness-20261030`. This is feature-branch
qualification, with no main merge, production deployment or environment publication.

## Previously verified source

Exact public commit `45b6bd30761ba9e5f7c00c42dc8e3ef66b62210a`,
[CI run 41039615424](https://github.com/AcademyDesk/AcademyDesk/actions/runs/41039615424),
completed successfully: 1,645 API and 1,152 frontend tests, 23 real owned SQL
modules, private Blob checks, payroll PDF/assessment browser checks, builds and
API container smoke. Recovery exercised an actual owned restore and exact
70-table/schema comparison. These results qualify that source only. They do not
certify production recovery, physical devices or the complete AI product.

The previous failed Access run remains preserved. Its fixture-only period label
collision was corrected without relaxing the duplicate payroll guard or201
expectation; the successful run above includes the corrected case.

## New source changes

- Payments, payroll profiles/payouts and reconciliation use synchronous submission
  admission. Pending controls lock; confirmed validation failures preserve drafts.
  Lost/5xx/408 results and successful writes with failed readback require checking
  the saved register before another submission. No automatic financial write retry.
- Token-free session notifications retire financial data/forms on account change
  or logout; late completions cannot overwrite retired views. Internal token
  refresh does not retire the current session. Existing request/refresh semantics
  and full payroll payloads/printing remain covered.
- Finance summary returns exact currency groups. Single-currency and empty-book
  legacy numeric fields remain compatible; mixed/invalid currency scalars are null.
  Actual finance and summary consumers show currency groups, with no implied FX or
  default INR. Payment/reconciliation/payroll register totals retain their units.
  Adjustment approval copy uses the stored adjustment currency. Platform summary
  currency aggregation remains an outstanding part of BUG-DATA-0016.
- Meeting creation controls remain unavailable even when metadata records a secure
  connection; fake history/edit promises removed. Genuine communication metadata
  PUTs remain functional, explicitly separate from unavailable OAuth/delivery.
- Adds a mandatory real finance browser module: normal UI login, actual Kestrel
  forwarding to Program/Identity on fresh owned SQL, independent Admin/FinanceUser
  invoices, partial payments with reconciled/voided rows, same-tick duplicate
  submission, canonical readback and independent persisted/audit assertions.
  This new module is not accepted until its exact-source CI run succeeds.
- Adds13 existing SQL modules to a separate required CI job: guardian flags,
  certificate family/enrollment, announcement audience, resource scope, marketing
  consent, template state, preference access, inbox lifecycle, finance lookups,
  governance, collections and invoice settings. Failures remain failures; all
  modules are attempted and evidence is retained. Existing23 modules retained.

## Local evidence for this source increment

- Full API:1,655 passed; zero failed/skipped.
- Exact-lock frontend:1,226 tests;1,223 passed,3 failed, zero skipped/cancelled.
  The3 historical communication cases require a fresh actual SQL receipt. None
  was copied, fabricated or skipped to turn this into a passing full result.
- Relevant groups: finance currency17, submission guards26, payment guards8,
  session notifications9, provider availability12; all passed. Existing payment
  balance7, lookup6, governance11, payroll print11 passed.
- Before-fix controlled tests reproduced7 payment failures,18 payroll/reconciliation
  failures and12 provider availability failures. Currency original API
  reproduction failed before repair. These are controlled code tests, not live
  browser or production data evidence.
- Locked Next16.4.3 webpack export and TypeScript passed. ESLint:0errors,61warnings.
  SQL harness compiled with0warnings/errors. Workflow actionlint and diff checks
  passed. Locked npm audit:0vulnerabilities.
- An inherited dependency symlink initially resolved Next16.3.5 despite the16.4.3
  lock. An isolated exact-lock install replaced only the symlink target for this
  worktree; preserved original source/dependency directories were not overwritten.
  Full frontend/build/type/lint results above use the exact lock.

Fresh CI, extended SQL and new real finance browser outcomes are pending when
this record is committed. Never infer their pass from compilation.

## Acceptance still required

All five P0 records stay open. Current backend/SQL/race/owned recovery proof is
stronger than their historical pending notes, but the closure chain still needs
complete real finance journeys, private-media recovery/expiry/revocation/recording
and transfer coverage, real Android/iOS evidence, supported spreadsheet consumer
acceptance, production/staging recovery and rollback evidence. No90%AI claim.

The laptop filesystem/remote session is not attached here. Existing unpublished
source and genuine local evidence must be reconciled once a real connection is
available. Permission alone does not establish connectivity. Continue with the
existing cross-profile ownership and model stop; no loading/activation inferred.

Next route: Codex/Sol High, inspect exact feature-branch CI, repair any failures,
continue the remaining acceptance, and preserve local laptop changes.
