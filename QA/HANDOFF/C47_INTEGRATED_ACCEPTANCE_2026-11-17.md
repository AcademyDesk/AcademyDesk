# C47 — current four-portal source and C46 integration

Cloud / Sol High, 17 November 2026, Asia/Calcutta.
Branch: `codex/cloud-c47-integrated-acceptance-20261117`.
Clean base: Desktop `a7e0b06e92bb851b7adbc9468353b095fca0c1c4` from the
current published integration branch, verified by fetch. C46 remains published
at `17e50d878aefadc264447f4419ab0d55707cba51` on its own feature branch.

## Ownership before integration

Only these paths may change:

- `apps/web/src/lib/penta-portal-reads.ts`: exact C46 implementation.
- `QA/tools/penta-portal-contract-boundaries.test.cjs`: exact C46 regressions.
- `QA/HANDOFF/C46_PORTAL_CONTRACTS_2026-11-15.md`: original source-bound handoff.
- this C47 task-specific handoff.

Integrate C46's completed implementation commit onto the newer four-portal
Desktop source. Preserve Desktop's API, SQL, Platform/Teacher/Admin/Portal UI,
root trackers, migrations, dependencies and all other files. Do not substitute
the private C45 comparison. Mini investigation is read-only: inspect exact hosted
CI annotations/jobs and source prerequisites without modifying source/evidence,
loading weights or importing model runtimes. Preserve the clean Mini checkout.

Validation: exact integrated-source full frontend and backend tests, relevant
client regressions, types/lint, locked production export and existing controlled
browser checks where available. Keep real SQL/device/model evidence separate.
Record exact candidate CI and all genuine failures; no skipped or invented
communication receipts, provenance or qualification.

Five P0 records plus the broader session gate, model qualification, measured
90% coverage, release CI/images and staging/device acceptance remain open.
No main merge, Azure deployment, environment publication, public-access change
or model download/loading/training/inference/activation is authorized here.

Results will be appended after execution.
