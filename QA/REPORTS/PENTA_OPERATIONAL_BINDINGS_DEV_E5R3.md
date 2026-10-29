# E5-R3 — bound preparation/receipt parser and existing local runtime

17 October2026. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved work. Continues E5-R2 without resetting/rebuilding the ERP, another source inventory or unchanged Mini trial. PENTA project read-only this batch; source/model/container settings unchanged.

## Host operational integration implemented

Added inactive `PentaOperationalBindings` beside the existing canonical payload adapter. Strict closed preparation/receipt parsing binds incoming records to independently supplied current host expectations: exact tenant, actor, connection, conversation, authority/context versions, operation/request/tool/draft identities/revisions and canonical payload hash. It recomputes the prepared payload digest, rejects protected top-level arguments, incomplete approval pairs, naive expiry timestamps and invalid/null/unknown/duplicate fields. Receipt parsing preserves non-success/unknown outcomes and rejects a success claim without receipt/audit identifiers or with an error.

The parser accepts claims only. It does **not** authenticate a sender, create/verify durable approval, prove that an audit/commit exists, query current permissions, resolve domain references or execute an operation. `HasCurrentApprovalClaim` is explicitly only a status/expiry check; callers must independently check current durable approvals, permission revocation, atomic claims/CAS, shared domain transaction/audit and authoritative receipt state. No route, DI registration, database table, tool activation or model-controlled execution added. Persisted operational workflows/confirmations/status APIs remain pending, not implemented by these pure parsers.

## Actual checks

- **164/164 PENTA-focused .NET tests PASS**, zero failed/skipped:39 new binding cases plus125 prior tests. API/test projects compiled without emitted compile warnings/errors. Receipt `QA/EVIDENCE/penta-operational-bindings-20261017/penta-bindings.trx`.
- Actual PENTA SDK `PreparedOperation`/`Receipt` validated both copied upstream synthetic example fixtures and their shared scope/payload binding using `python -B`; no inference, business mutation or source/cache edit. The .NET parser accepts those same fixtures. Their timestamps are fixed historical test data, not current valid approvals.
- Negative cases include six scope substitutions on both record types, operation/request/tool/draft/revision/hash substitutions, altered canonical values, missing success audit/receipt, success errors, unknown outcomes, naive/invalid/expired approval, malformed/unknown/duplicate envelopes and protected fields with recomputed self-consistent hashes. Tests prove parsing/binding only, not actual SQL authorization or transaction safety.
- Active SearchLearners/GetLearner READ-only boundary and unaccepted Mini qualification remain unchanged. No new browser/model/SQL-domain integration test claim. Existing framework warnings about GradingScheme.PassingPercent/Invoice.AdjustedAmount precision were observed by EF tooling/runtime; not new clean-runtime or enterprise clearance.

Source SHA256: parser `53E9EFA34877C070C0C3335DB8EF374D47618030543E95C4F372D35F7D8813D2`; tests `2B75904EE410B92BBA37B3A46E8E5DB39558E90984025C7CDF944D47D595429F`.

## Local development restoration — actual changes and recovery

Existing local DefaultConnection was validated as a local SQL alias, not Azure. Read-only SQL showed87 combined migration-history rows. An initial query for a separate Identity-history table failed because this local database uses the shared history table; no data change from that query. Current source has21 unapplied application migrations.

Generated EF forward SQL into ignored `.build-check/dev-startup-20261017/pending-upgrade.sql`, SHA256 `C1AE4FF98DD927D5FC566597E8F9EB028184BA7F436043C9018B91A81B1253F2`. Reviewed its operation summary and relevant migrations:22 new tables, added columns/indices/constraints, payroll PaidAtUtc made nullable, meeting-state check-constraint replacements; no table/column drops or DELETEs. Only migration history receives inserts. No production SQL target or downgrade.

Before applying, native SQL **COPY_ONLY/CHECKSUM backup and RESTORE VERIFYONLY passed**:

`C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\AcademyDesk_dev_before_20261017_1178659F-0F1A-4E3B-BC84-FEC67B04E4B8.bak`

This backup contains local application data; keep it private/local, never commit/upload it. No backup or other file was deleted. Restore would overwrite local data and needs a separately reviewed/authorized operation; it was NOT performed.

Applied the exact generated forward script to the existing local database successfully:87→108 combined history rows. Before/after counts remained AspNetUsers10, Students4, Teachers4, Invoices2, Payments1, PayrollPayouts2. Count equality is not an all-column/content checksum or restore-drill certification. No seed/reset run or financial data rewrite performed.

Started the **existing** API on `http://localhost:5092` from the same worktree, with process-only Testing profile to skip the Development seeder's sample-record updates/password resets. The same local connection was explicitly supplied; automatic migrations/bootstrap/media and PENTA/model access are disabled for this runtime. No tracked configuration, secret value or persistent setting changed. Existing frontend uses localhost5092 and runs on `http://localhost:3000` through Next webpack dev. This is local inspection, not a production/staging or fully AI-integrated environment.

- API `/health` returned200 against local SQL.
- Frontend `/login` returned200; Next ready, both listeners verified.
- Existing local Admin sign-in with the locally configured password returned401 for the known login ID and email. No token/secret printed or saved. No account reset, role change, failed-login loop or fabricated login success. A working authenticated test login remains unresolved.
- Runtime logs contain the existing decimal warnings and missing local HTTPS-redirection-port warning. Local loopback HTTP is not a production TLS configuration.

API tool session50617 and frontend session80830 were left running for inspection; these IDs are session-lifetime references, not persistent service installation. No second backend repository or disposable replacement academy environment created.

## Next

- [x] Scoped preparation/receipt payload parsers and SDK compatibility checks.
- [x] Verified backup, existing local forward schema upgrade, API health and login-page availability.
- [ ] Verify an authenticated local login; existing password mismatch requires current credentials or permission for a narrowly scoped local test-account reset. Do not reset unrelated accounts or rerun the broad Development seeder.
- [ ] Implement current-authority/catalog and durable operational draft/approval/status/application-service bindings, then real disposable SQL/HTTP/concurrency/audit tests and authenticated controlled frontend.
- [ ] Accept the private interpreter before operational chat activation; preserve exact failed/partial Mini evidence and capacity deferrals. Existing enterprise/device/provider/privacy/release gates stay open.

Primary main/PENTA/Academy HEADs unchanged, zero staged files; QA evidence local/untracked and build SQL ignored. No commit/push/merge/Azure, real provider call or deployment. Recommended route remains High for consequential host/security/domain integration, Medium only for settled presentation. Manual fallback/security retained; cosmetic work deferred per owner.
