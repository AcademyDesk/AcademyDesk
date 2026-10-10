# BUG-API-0009 — Guardian child list fails SQL query translation

| Field | Value |
| --- | --- |
| Status | OPEN |
| Priority | P1 |
| Severity | Major guardian child-list workflow unavailable |
| Module | FAMILY |
| Confirmation status | RUNTIME-CONFIRMED; bounded query repair verified |
| Final verification | PARTIAL: native SQL/HTTP PASS; browser/device/broader release pending |
| API | GET /api/portal/guardians/{guardianId}/children |
| Source | apps/api/Controllers/PortalController.cs, GuardianChildren |
| Discovery/retest | GUARDIAN-CHILDREN-SQL-001; GuardianRevocation native module |
| Environment | Fresh labelled loopback SQL/Testing host, synthetic tenants only |
| Evidence | [Guardian revocation report](../REPORTS/PHASE_2B_GUARDIAN_REVOCATION_REPAIR.md) |

## Reproduction / actual

Authenticate an active linked guardian with accessible synthetic children against real SQL. GET the guardian's own children. Starting source `450249938aa61a9aa6e68df13bf55ca401d63d76` returns500. Initial run `5393c389e5764e5a9a5b0d66cb70764e` failed its positive200 assertion. Explicit diagnostic run `bce44b1494ef485f83b7b620faf06dba` captured three500 responses and verified the exception was an EF `InvalidOperationException` with query translation failure, not a denied-access response.

## Cause / bounded fix

The query orders by `Name` after constructing the `PortalChildSummary` positional record in the EF projection. Move ordering to the equivalent SQL-translatable `FirstName + " " + LastName` expression before record projection. Current guardian/academy/grant/revocation predicate, selected fields, active enrollment count and alphabetical name ordering intent stay unchanged; no client-side unscoped materialization or relaxed authorization.

Native final run `f717a944a28847deaa1703f522c895c8` contains21 successful child-list200 checks across minor/adult/18th-birthday/unknown-DOB active/revoked/regranted states and unlink. Returned IDs exactly match fresh eligible own-guardian grants; foreign/unlinked children excluded. Read snapshots unchanged; no500/429. Full90-case gate PASS. This issue is not closed by a query fix: browser/device, broader portal role/lifecycle and full critical/release acceptance remain pending.
