# Default PENTA host composition — 6 November 2026

Status: VALIDATED bounded infrastructure change; no operational/model activation.
Base b4dddf40b02d559bae0605535f3647b8a64e8a65 on
codex/penta-host-integration-20261030. Scope claimed/published before code edits
in LOCAL_C1_EVIDENCE_RETURN_2026-11-06.md. Cloud C3 frontend paths untouched.

## Change and observed behavior

Program now registers one singleton PentaPrivateContextLeases. The private context
controller's optional dependency previously resolved null in the ordinary host
and safely returned404. The SQL harness injected a store, masking this missing
composition. This is not a claimed previous500, model connection or domain fix.
The comment distinguishes lease infrastructure from interpreter registration.

Eight new tests use actual WebApplicationFactory/Program registrations, without
QaApiFactory, replacement leases, in-memory DB, interpreter or model. They prove:
- One lease store across scopes and the actual controller HTTP pipeline.
- Exact synthetic private lease/version/body permits two reads, then404; disposal
  revokes it. Responses are no-store and callback runs exactly twice.
- Each of five disabled feature gates returns404 without invoking context.
- Production and Staging return404 with every gate true and a real synthetic
  lease. No private version response header is exposed on denial.
- Onboarding/teacher interpreters and the legacy external model provider remain
  absent from DI. The existing SQL Server provider remains registered, without
  connecting to a database. No customer/production DB was used.

Tests use owned D: content roots and ephemeral Data Protection; migrations,
bootstrap and live media are disabled. Private lease credentials are random and
not printed. No role/tenant authority, approval, financial/domain rule, schema,
endpoint gates, provider/runtime/model registry or frontend contract changed.

## Actual validation and retained failures

| Run | Actual result |
| --- | --- |
| Baseline6eb0982d972d47b79ff8235c41884b8f | Eight tests failed for the exact missing default lease registration; preserved TRX confirms all eight reasons |
| First full d1c07ae5f1824cc28839f751cd95015c | 1,982 passed/two failed/zero skipped; all eight new tests passed. Runner TEMP was mistakenly under repository ancestry, correctly rejected by existing protected-key-ring tests |
| Corrected full744e0944f65049e4815e7e905b7a559c | **1,984 passed/zero failed/zero skipped**,14s test duration; normal API/test build passed |
| Every bounded window | Original exact service IDs/images restored, API200, full Docker inventory preserved, validation lease released |

Only runner TEMP/TMP changed for the corrected full run: an owned D: operations
directory outside Git. Application/test bytes remained unchanged; no security
check was weakened. Original failures and all private artifacts are retained.
A private source manifest binds Program, bridge and new test hashes plus final
TRX SHA256. No additional packages/downloads, Docker restart or pruning.

Existing real SQL/migration/bridge qualification retains its earlier exact source
scope; it was not rerun or silently relabeled after this Program change. This
packet freshly tests actual default composition/HTTP and the full current API
suite. It does not establish new combined-source SQL/device/production acceptance.

Mini C1 still has two Linux exact-original-context failures. Model quality remains
NOT ACCEPTED; loading/inference experiment/training/activation stop persists.
Remaining real conversational workflows/all roles, exact-lock frontend/build/CI,
privacy/security/device/P0/DB/restore/staging/release remain open. No main/Azure.
Next: reviewed C3.1 frontend integration, retaining its unavailable-state honesty,
then bounded actual host role/browser and conversation acceptance. Sol High for
integration/security; Sol Medium for settled visual work.
