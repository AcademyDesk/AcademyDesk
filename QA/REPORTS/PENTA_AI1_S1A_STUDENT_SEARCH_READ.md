# PENTA AI1-S1a — minimum-field active-student search

Date: 2026-10-04. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. This is a local read-only authority boundary, not a live-model feature or completed AI1 milestone.

## Scope

- Added `GET /api/academies/{academyId}/penta/student-search?q=...` (`student.search.v1`, Executor). It uses the existing default-off Development/Testing PENTA gate and current Identity Owner/Academy Admin, account-active and own-active-academy policy. No client-selected actor, tool, model, source or row limit is accepted.
- Search is normalized to 2–80 characters and runs against the academy's **active** Students in SQL. Ordering is deterministic; only 11 minimal rows are fetched to return at most 10 and an honest `hasMore` flag. The response includes student source ID, display name, active flag, relative Student 360 link, academy timezone, UTC as-of and source. It contains no email, phone, birth date, guardian, medical/accessibility/admin notes or financial fields. Names are untrusted recorded data, not instructions.
- Each authorized search writes one audit event with academy, actor and fixed tool metadata, including a zero-result search. Query and names are not stored in audit metadata. Audit failure denies the read. No PENTA execution, usage reservation, domain write, provider or external call occurs.

## Validation

| Check | Result |
| --- | --- |
| API and SQL-harness builds | PASS, zero warnings/errors. |
| Targeted PENTA API | PASS 16/16. Minimum JSON field shape, ten-row cap, private-field exclusion, tenant/role denial, invalid query, account revocation and disabled flag covered. |
| Full API suite | PASS 1,083/1,083, zero skipped, after final revocation/disabled and zero-result assertions. |
| Disposable real Identity/HTTP/SQL `PentaFoundation` | PASS, final run `f0d21a0912b846a0a683e0300dfb7244`. Anonymous 401; teacher/platform/foreign-academy 403; ten own active rows whose IDs and names match the source records, with source paths; no private, inactive or foreign data; no provider call or PENTA execution; one durable audit; audit-write fault denied return. Existing budget/probe/recovery/draft checks remained green. 86 application and 7 Identity migrations; owned SQL database, login and container removed. |

## Open gates

No user-facing PENTA result card, browser/mobile source-link acceptance, golden-question answer evaluation, prompt-injection evaluation or batch lookup has passed. The read endpoint remains unavailable in Production and default-off locally. External customer-data processing still needs explicit provider/model/region/retention/price approval and additional authority/cost controls. Enterprise P0, security, physical-device and pre-Azure gates stay open. No push, merge or Azure deployment.

Next: AI1-S1b bounded batch lookup and result UI, or complete provider/privacy decision separately; keep external model calls off. Sol High/Codex for authority-sensitive backend work, Sol Medium/Cursor for bounded UI, Astra only if a consequential design question remains unresolved.
