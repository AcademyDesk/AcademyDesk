# C45 — scoped Student/Guardian conversation

Claimed 15 November 2026 (Asia/Calcutta), Cloud / Sol High.
Base: `e481a7705618b6135f0fd06f4929dce06f0422bf`, Desktop's current
`codex/penta-host-integration-20261030` tip verified by fetch. Both checkouts
were clean. C43 remains preserved on its own branch. Private C44 staging/archive
is retained; Desktop already implemented scheduling, so it is not reapplied.

## Ownership before implementation

Branch: `codex/cloud-c45-learner-conversation-20261115`.
Only the following paths are claimed:

- `apps/api/Controllers/PentaPortalController.cs` (new)
- `apps/api/Intelligence/Penta/PentaPortalReadService.cs` (new)
- `tests/AcademyDesk.Api.Tests/PentaPortalReadTests.cs` (new)
- `apps/web/src/lib/penta-portal-read.ts` (new)
- `apps/web/src/components/penta/learner-conversation.tsx` (new)
- `apps/web/src/components/penta/role-penta-entry.tsx` (Portal mount only)
- `QA/tools/penta-portal-read.test.cjs` (new)
- `QA/tools/penta-portal-read-browser.cjs` (new)
- this handoff (task-specific replacement for a shared AI_HANDOFF edit).

Scope: Testing-only, default-off guided reads for current own/linked active
learners, upcoming class, due work and permitted fee balances. Resolve current
Identity role, account, stamp, academy and guardian flags on the host; never
trust prompt IDs or cached linkage. Bound minimal rows, save a read audit before
returning data, keep currency separate and retain Manual. No domain mutations.
Frontend validates response scope, clears superseded results and invalidates on
login/session changes. Unsupported or ambiguous input asks a specific question.

Validation: actual HTTP/Identity authorization and projections (in-memory is
not SQL qualification), client response/transport/session tests, native Chromium
synthetic-host checks, exact-lock type/lint/build and affected regression suite.
Desktop retains protected SQL/migration/device validation; this packet must
explicitly hand off live browser/SQL acceptance. No root tracker, native SQL
harness, vendor, Mini, existing domain service or migration edits.

Mini/Plus/Pro remain unqualified; Ultra deferred. Model loading, inference,
activation, main merge, Azure deployment and environment publication stay
stopped. Keep private environment access and restricted network policy.
The 90% target remains unmeasured. Six critical records, exact release CI/images,
staging recovery and physical-device acceptance remain open.

Implementation and validation results will be appended here after execution.
