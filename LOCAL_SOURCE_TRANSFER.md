# Local PENTA source transfer — 29 October 2026

This is an isolated preservation checkpoint, NOT the accepted integration branch
and NOT a production release. Original working folders remain untouched.

## Sources and integration order

- Academy application source: `AcademyDesk/AcademyDesk`, branch
  `codex/penta-local-source-20261029`, based on local `b1f8cb6`.
- Mini/Core/frontend source: `AcademyDesk/PentaMini`, branch
  `codex/mini-local-source-20261029`, based on `2f88069`.
- Shared development integration base: `AcademyDesk/AcademyDesk`, branch
  `codex/cloud-profile-sync`, verified published commit
  `00d0d00f0264bb913065aa040a9f0ef0967771b6`.

Fetch and record actual branch SHAs/status before starting. Use a separate task
branch from the shared integration base. Read AI_CROSS_PROFILE_HANDOFF.md there.
Do NOT merge this preservation branch wholesale or replace either repository.
Its older zero-net/Voided policies are superseded by the owner's cloud choices:
reject new zero-net payroll; permit validated Voided restoration under current
invoice/tenant/amount/currency/transaction/audit safeguards. The shared base
already contains the qualified payroll reconciliation; retain it.

Reconcile payment/invoice/adjustment guards first, then coherent PENTA services,
contracts, authorization, domain controllers, migrations, frontend and tests.
Preserve both cloud fixes and stronger local protections. Overlapping migration
histories require an explicit tested upgrade path. Never apply both histories
blindly. Do not substitute the cloud API suite for the larger local suite.

## What this transfers

Source, tests, contracts, synthetic fixtures, QA reports, shared visual sources,
and engineering handoffs. LOCAL_SOURCE_TRANSFER_MANIFEST.json binds each copied
change to the original local bytes and to portable LF-normalized text hashes.
The base commit supplies unchanged tracked files. Read historical reports as
historical evidence; this transfer does not claim fresh runtime acceptance.

Excluded: QA/EVIDENCE, operations/evidence, historical frontend validation runs,
distribution archives/generated output, ignored dependencies/environments,
model weights/cache, local secrets and runtime data. These remain on the original
laptop. A reference to an excluded report artifact is not a transferred test pass.
Missing evidence must not be invented. Regenerate safe artifacts through existing
runners only within approved scope and available resources.

## Current accepted checkpoints and remaining gates

- Local Academy historical full API: 3,183 passing, zero skipped.
- Teacher chat UI qualified locally using a synthetic QA interpreter only.
- Shared enrollment operations qualified locally; protected conversational
  approval/execution/receipt/recovery remains open.
- Shared integration payroll: 1,234 API passing, 23 additional SQL checks,
  five deterministic races and two audit-failure rollbacks passing.
- The two API totals overlap; never add them or claim combined acceptance.
- Mini full-plan fresh 6/12, exposed 7/12: NOT ACCEPTED.
- Owner explicitly stopped before candidate model-weight loading. No loading,
  training, inference experiments, activation, downloads or paid compute follows
  from this transfer. The D: setup remains preserved but is not transferred.
- 90% conversational AI/10% Manual is the product target, not verified coverage.
- Financial/PENTA reconciliation, real interpreter qualification, remaining
  workflow cohorts, all-role/device acceptance, combined CI/security, database
  restore/rollback/staging and explicit release approval remain open.

No main merge, production database use, Azure deployment or repository-setting
change. Retain independent host authorization, required approval and audit.
Manual ERP must function when AI is unavailable.

## Work-laptop/cloud continuation prompt

Continue Academy Desk from shared branch codex/cloud-profile-sync at verified
00d0d00, reading its governance and cross-profile handoff first. The two source
preservation branches above contain the previously laptop-only application and
Mini/Core/frontend work. Fetch them for review; do not merge wholesale. Start a
bounded finance reconciliation branch, then integrate PENTA in coherent slices
with relevant tests. Follow owner cloud financial policies and the stop before
model loading. Keep prior evidence/history intact, record exact source/results,
and update the shared handoff. Do not restart the existing testing program or
claim production readiness until its actual gates pass. Sol High for finance,
security and PENTA integration; Sol Medium for settled UI/documentation.

## Dependency setup on a different machine

The source transfers, not this laptop's installed dependencies or model services.
Use the existing repository dependency/lock files in an approved development
setup. For Mini frontend tooling, set PENTA_CONSUMER_NODE_MODULES to the local
Academy Desk apps/web/node_modules directory after its normal dependency setup;
the old D: default is merely this laptop's cache path. PENTA_BUNDLED_NODE_MODULES
is an optional tooling override, not a model endpoint. Do not copy credentials,
absolute-path caches or model weights into Git. Run only deterministic contract
checks until model/runtime prerequisites and owner scope explicitly permit more.
