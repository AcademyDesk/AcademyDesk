# Phase 2B — Student import identity integrity

Date:2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `ee7efc6bf6f6e8f0774524c469d9cee4c9d543e4`. Existing BUG-DATA-0049 continuation, not a restarted audit or new enterprise plan. Sol High for domain/tenant/transaction work. Issue **OPEN / RETEST**, not closed.

## Reproduction and repair

Starting controller suite:17 cases,4 PASS/13 FAIL. Duplicate files accepted, blank optional values stored empty rather than NULL, null rows threw, length violations were not rejected, and persisted email whitespace did not match normalized input. Baseline disposable SQL run `a0c795be9b3040ada73dd7b8074a193c` confirmed validate200/no errors and import200 storing two normalized duplicate emails. This run used a harness built before application edits; the baseline observation text did not match the runner output filter, but strict assertions and exit0 confirm the observation. The subsequent output-prefix adjustment was not a rerun of that baseline.

`StudentImportsController` now reports every occurrence of an in-file duplicate email, using trimmed case-insensitive comparison, and rejects import400 before domain writes. Optional blank email/student number/phone persists NULL. Existing academy-local email checks trim/case-match and reject409 before inserting any batch. Foreign academy matches do not block permitted local imports. Required names, field lengths and null rows are checked before writing; original basic email syntax, optional admission date/default and success200 response are retained.

The import opts into the existing host domain/audit transaction, including the previously permitted Platform Owner path without granting new access. SQL takes a parameterized `UPDLOCK,HOLDLOCK` on the owning Academy before the existing-student read; its lifetime covers batch insert and success audit. A direct SQL caller without a host transaction owns/disposes its own transaction. SQL unique identifier violations2601/2627 return a safe409, and host rollback prevents partial batches. No index weakening, migration, existing-data rewrite, financial calculation, Identity account creation or PENTA/Core permission change.

Validation200 is a successful POST under the existing audit filter even when its errors array is nonempty. Validation changes no student/domain data; this report does **not** claim that validation writes no audit record. Rejected imports and denied requests leave the scoped fresh domain/audit snapshots unchanged.

## Manual workflow

The current file must validate successfully before import is enabled; a replacement file invalidates that state. Validation errors are rendered as structured React text with row numbers, not JSON/HTML. Successful validation does not promise the later database checks will pass. Confirmed imported count must match the submitted count before rows/file state clear.400/403/409 and malformed responses retain the file; network/5xx/unexpected success count explain that the outcome is unconfirmed and require checking the student register before manual retry. No automatic write retry.

A synchronous shared pending guard blocks duplicate/opposing validate/import calls and file replacement during a request. Versioned reads prevent a slow old file from replacing a newer selection; a read failure cannot reuse an older validated file. Existing CSV parser, exports, payload fields/date-null semantics and role/tenant authority remain unchanged. A polite persistent status region carries feedback. No broad visual redesign or full export workflow repair in this batch.

## Final verification

| Proof | Result and scope |
| --- | --- |
| Controller/audit regression |89 PASS:17 student-import +36 retained work-item +36 retained AuditOutcome API tests. This is not the full enterprise suite. |
| Actual TSX/helper handler checks |27 PASS: exact optional-null payload, duplicate/malformed/denied validation, safe success/reset, retained denied/uncertain file, no auto-retry, synchronous pending and file-version recovery. |
| Native SQL/Identity/HTTP |57 PASS, no429. Run `134f21301c5f4d9d9d15403cb6efa0ce`, SQL58381, migrations88 application/7 Identity, scoped runtime login. |
| Concurrency |Five duplicate-email import pairs: exactly one200 and one409, one new student/one new audit per pair; sentinels/unrelated domain unchanged. Distinct pair:two200/two additional students/audits. |
| Native failure/authority |Duplicate variants and existing matches; null/empty/invalid/oversized rows; optional blank/date/foreign-match positive controls; anonymous/teacher/student/finance/foreign admin denials; foreign route/suspended academy; unique-number batch rollback; audit insert fault500/no-write and restored success. |
| Browser |36 synthetic exported-browser cases:320/1440px × light/dark × normal, duplicate,400,409,403,500, malformed result, pending, replacement. Exact calls/payload/nulls/status/reset/draft retention/no horizontal overflow/expected console errors checked. Not linked SQL or physical devices. |
| Visual inspection |320px light duplicate and dark uncertain screenshots reviewed: readable wrapped notices, retained filename/rows, stacked controls, no horizontal page overflow. This is not final Orbit/Aura/Prism rollout acceptance. |
| Web validation |Target page/helper lint0 errors/0 warnings; TypeScript PASS; production webpack export83/83 pages PASS. Helper comment-only edit afterward changes no emitted behaviour. |
| SQL harness build |0 errors/0 warnings. Both native runs removed their exact owned database/login/container; mismatched-marker negative cleanup guard retained. Other containers untouched. |

Native57 breakdown:8 duplicate validate/import,3 blank optional batches,1 valid date/trim batch,2 existing local conflicts,1 foreign-match allowed batch,20 invalid-row validate/import,10 actor denials,2 foreign-route denials,1 identifier-constraint rollback,2 audit-fault/recovery,5 duplicate pairs,1 distinct pair,1 suspended academy. Case counts are checks, not57 separate defects.

Local evidence, intentionally uncommitted:

- `QA/EVIDENCE/student-import-integrity-20261011/baseline.trx`, `final.trx`, `sql-baseline.txt`, `sql-final.txt`, `ui-final.txt`.
- `QA/EVIDENCE/student-import-feedback-browser-1791699133587/result.json` and screenshots. Failed fixture runs are retained separately.

The first browser invocation matched both shell avatar and CSV inputs; narrowed selector. Following fixture runs lacked AccessGovernance and the shell remounted the route, leaving validation disabled. The synthetic academy now enables the existing required module and waits for readiness; application permissions and assertions were not weakened. Starting API/SQL defect evidence is retained. Native tests ran against the preserved dirty local snapshot, not acceptance/publication of unrelated Mini changes.

## Remaining acceptance and publication boundary

- Academy-row serialization coordinates **this import endpoint only**. Other student create/update/onboarding writers do not necessarily take this lock; no global concurrent identity uniqueness guarantee. No new database email unique constraint or repair of legacy duplicates.
- No server idempotency key/cross-tab retry protocol; blank-email duplicate intent still requires operational review. Unknown outcomes retain drafts and explain review, not guaranteed deduplication of every retry.
- Existing simple comma/line splitting is not a full quoted/multiline CSV parser. No general RFC mailbox/Unicode normalization or bulk-resource limit qualification added.
- Linked real browser-to-SQL, physical Android/iOS/tablets, Platform Owner positive control, alternate cultures/collations, other-writer races, cancellation/stale authorization and full critical/release gates remain pending.
- Original28 feedback checkpoints and existing enterprise program retained. E3 all-four-portal Manual Orbit + Aura dark/Prism light rollout, PENTA-owned FW1 UI, Mini qualification and approved financial-policy work remain separate.

Only this scoped feature batch may be committed/pushed to `codex/penta-search`; no main merge, Azure deployment, production database/credentials or model training changes. `QA/EVIDENCE` local, `.build-check` ignored; unrelated Mini/Platform/private work and32/28 older handoff/README additions preserved/excluded. Commit identifiable by this report's scoped Git history. No PENTA Mini contract handoff required for this host import repair.

NEXT: existing BUG-DATA-0050 teacher compensation zero-rate load/resave reproduction, preserving legitimate0 vs optionalNULL. **Sol High** for finance semantics and real SQL validation; Sol Medium for subsequent settled visual/feedback implementation. Do not repeat accepted audits or mark enterprise complete.
