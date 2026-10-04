# PENTA AI1-S1b — bounded active-batch search

Date: 2026-10-04. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. This is a local read-only authority slice, not an AI workspace or release acceptance.

## Scope and boundary

- `GET /api/academies/{academyId}/penta/batch-search?q=...` (`batch.search.v1`, Executor) uses the existing default-off Development/Testing PENTA gate and current active Identity Owner/Academy Admin, own active academy policy. No caller-selected actor, tool, model or row limit.
- A normalized 2–80 character name-or-code search filters active batches within the academy in SQL. Stable name/ID ordering and 11-row lookahead return at most ten records with `hasMore`. Each row contains only batch source ID, name, code and `/batch-setup`. The existing page is a list, **not a batch-detail deep link**; the ID is provenance, not a claim that the page will select it.
- No schedule, teacher, room, meeting link, capacity, notes, contact or finance projection. Batch names/codes are source data, not instructions. Each authorized read, including zero results, requires fixed-metadata actor/academy audit; audit failure denies return. No PENTA execution, usage reservation, academy action or model/provider call.

## Validation

| Check | Result |
| --- | --- |
| SQL-harness build | PASS, zero warnings/errors. |
| Focused PENTA API | PASS 17/17. Shape, cap, active/tenant/private-field exclusion, name/code search, invalid query, 401/403, revocation and disabled flag. |
| Full API | PASS 1,084/1,084, zero skipped. |
| Disposable real Identity/HTTP/SQL `PentaFoundation` | PASS run `937e950a62d041919a8b09ddf2043206`. Own active source ID/name/code comparison, 10-row cap, 401/403, invalid query, no private/foreign/inactive fields, no provider/execution, three required audits and audit-write-fault denial. Existing student, budget, provider-probe, recovery and draft checks remained green. 86 application and 7 Identity migrations; run-owned database, login and container removed. |

## Open gates and next slice

No PENTA result UI, browser/mobile source-link acceptance, golden-answer or prompt-injection evaluation has passed. The list destination must not be represented as a batch-detail link. External customer-data processing still needs provider/model/region/retention/price approval. Enterprise P0, security, physical-device and pre-Azure release gates remain open. No push, merge or Azure deployment.

Next: AI1-S1c bounded source-linked PENTA result UI with explicit list-vs-detail wording and browser/mobile evidence; external model calls remain off. Sol Medium/Cursor is suitable for that UI slice, with Sol High for any changed authority or data projection. Astra only for an unresolved consequential design decision.
