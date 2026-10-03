# Concurrent collection repair — BUG-DATA-0001

Date: 2026-10-03. Branch: `cursor/certificate-validation`. Local SQL and HTTP only; no merge, push, Azure deployment, or production data. BUG-DATA-0001 remains **OPEN** for live Payments browser and physical-device checks.

## Change

`PaymentsController.Create` now locks the academy-scoped invoice row with SQL Server `UPDLOCK, HOLDLOCK` before it reads the Completed/Reconciled collected total. The balance decision, payment insert, and invoice status update share one transaction. Normal academy requests reuse the finance access filter's transaction and audit boundary; callers without that boundary open and commit their own transaction. InMemory controller tests retain the existing query path. No schema, permission, or response-message change.

`CollectionRaceRegression` keeps all five attempts and the original status/ledger/row assertions, and additionally requires the rejected HTTP 400 response to say `Payment exceeds the invoice balance.`

## Validation

| Check | Result |
| --- | --- |
| SQL harness build | PASS; 0 warnings/errors |
| `CollectionRace` run `7f5f02d9f7674c6aa7fb31aa3c6dfdb4` | PASS, 5/5 guarded |
| Each concurrent pair | Exactly one 201 and one 400 with the required message; no 429 |
| Each committed ledger | Reconciled 600 + Completed 400 = 1000; exactly two rows |
| `Reconciliation` run `d107470396c54192acbd73def3e6d59e` | PASS; reconciled 600 leaves balance 400, excess 500 rejected without SQL change; mixed-status settlement controls pass |
| API tests | 1,042/1,042 PASS |
| Owned SQL containers | Both runner-owned containers stopped and removed after passing; no prune |

The first script attempt was blocked by local PowerShell execution policy before a container was created. Both actual runs used a process-only execution-policy override; the machine policy was not changed.

## Boundaries

This is a targeted concurrent-POST repair, not a complete financial concurrency certification. Live Payments browser and physical-device checks remain pending. Other open P0 items and the existing pre-Azure gates are unchanged. The original failing race evidence remains in [PHASE_2B_COLLECTION_RACE.md](PHASE_2B_COLLECTION_RACE.md).
