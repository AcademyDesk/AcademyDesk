# PENTA AI0-S3b — academy-context R0 read

Date: 2026-10-04. Local worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. This is a disabled-by-default Development/Testing-only read boundary, not a customer-facing AI answer.

## Scope

- Added the fixed `GET /api/academies/{academyId}/penta/academy-context` tool (`academy.context.v1`, Twin). It reads only the current active academy's ID, name, country code and timezone; the response includes an as-of UTC timestamp and source path. No student, guardian, finance, subscription, legal-name, media or free-text fields are returned.
- The existing PENTA policy derives the caller from Identity and requires an active Academy Admin or Owner of that exact active academy. Anonymous, teacher, platform-owner, foreign-academy and revoked-actor requests are denied. The database projection additionally filters the active academy ID.
- An authorized read writes one `PentaAcademyContextRead` audit entry containing actor, academy and fixed tool name. An audit-store failure prevents the service from returning the projection. The read does not create a PENTA execution, attempt, usage reservation, work item or other academy action and never invokes a model or synthetic provider.
- The fixed source path points to the existing authenticated academy list. It is provenance for a future UI, not a promise that the current web app contains a PENTA view. Academy names remain untrusted data; no external model receives them in this slice.

## Validation

| Check | Result |
| --- | --- |
| Full API suite | PASS 1,059/1,059, 0 skipped. Added read-role/tenant/minimal-field/audit/model-free, inactive-academy, revoked-actor and disabled-flag checks. |
| SQL harness build | PASS, 0 warnings/errors. |
| Real Identity/HTTP/disposable SQL | Final `PentaFoundation` run `cd4e59545f1c4af4b8b96abb1a0b00b7` PASS: anonymous 401; teacher, platform and foreign-academy 403; authorized projection and one persisted audit; zero PENTA/domain action rows; injected audit write fault denied an unaudited result. Existing priced-budget, recovery, quota and synthetic approval checks stayed green. |
| Cleanup | Run-owned database, login and SQL container removed by the guarded runner; no other container pruned. `QA/EVIDENCE` remains local. |

## Open gates

This is only a narrow academy-identity context, not student search, batch/schedule search, a Pulse briefing, an AI provider, a customer UI or a real Executor action. No external data-processing/provider decision, protected-data field matrix, browser/mobile acceptance, enterprise issue closure or Azure release gate is satisfied. The route remains unavailable in Production and default-off locally. No merge, push or Azure deployment.

Next: AI0-S4 provider abstraction behind an independent disabled flag with fake/fault evaluations and a separate privacy/retention decision before any external academy data; in parallel, prepare the first useful source-verified, minimum-field student/batch read for AI1-S1. Route authority-sensitive code through Codex/Sol High; bounded UI/docs through Sol Medium or Cursor. Astra only for a consequential unresolved architecture or security decision.
