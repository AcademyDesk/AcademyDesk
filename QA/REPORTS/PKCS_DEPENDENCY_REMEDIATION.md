# PKCS dependency security remediation

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Bounded successor to [M2c](MEETING_RECEIPT_M2C.md); not enterprise or release closure.

## Finding and implementation

Fresh NuGet audit identified `System.Security.Cryptography.Pkcs/9.0.11`, transitively requested by `Microsoft.Data.SqlClient/6.1.6`, in the API, tests and isolated SQL harness. Microsoft lists two denial-of-service advisories: [GHSA-846f-f764-76mx](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx) (high, SignedCms signature processing) and [GHSA-2fmv-rv2c-59x7](https://github.com/dotnet/runtime/security/advisories/GHSA-2fmv-rv2c-59x7) (moderate, EnvelopedCms decoding). Both list 10.0.13 as patched in the .NET 10 line. No exploit payloads or customer data used.

Added one explicit API PackageReference to `System.Security.Cryptography.Pkcs/10.0.13`. Kept SqlClient, EF, application behaviour and all existing test assertions unchanged. No advisory suppression or blanket package upgrade.

An intermediate 9.0.21 pin cleared package auditing but was rejected as the final solution: API runtime output omitted that older-major package in favour of the shared framework. The final 10.0.13 pin is present in all three `project.assets.json` and all three output `.deps.json` files, with platform runtime DLLs copied. The new `PkcsDependencyTests` checks the **actually loaded** SignedCms/EnvelopedCms assembly informational version is a patched .NET 10 servicing release, rather than trusting restore metadata alone.

## Final verification

- API build: PASS, zero warnings/errors.
- Isolated SqlHarness build with `--artifacts-path .build-check/meeting-readiness-sql`: PASS, zero warnings/errors. Restore auditing explicitly enabled for all dependencies, lowest advisory severity, warnings treated as errors; no NU190x suppression. The package-list CLI does not accept `--artifacts-path`; harness verification therefore uses that audited isolated restore/build plus resolved assets/deps inspection, not a purported successful package-list invocation.
- API and tests `dotnet list ... package --vulnerable --include-transitive --no-restore`: both report no vulnerable packages against current NuGet sources. This is known-advisory dependency evidence, not an all-security guarantee.
- All three assets/deps resolve PKCS10.0.13, parent SqlClient remains6.1.6. Windows runtime DLL ProductVersion is `10.0.13+4099f83977f4d29604a9aeb971fb0a7bc1d5f699`; identical DLL SHA256 across API/test/harness outputs: `7BCE9317B8ECCD6B261B1AF3321AC4A5FF4E770A2C6C666F133F0108C5D066F4`.
- **180 selected tests PASS**, zero failed/skipped: loaded-PKCS regression, protected key-ring, meeting readiness/preview/receipt and communication preference/channel tests. Final TRX: `QA/EVIDENCE/pkcs-remediation-20261015/dependency-final.trx`. Earlier179-test/9.x exploratory proof is not the accepted final verification.
- Final `Run-ReconciledPayment.ps1 -Module MeetingReadiness`: **58/58 real HTTP/Identity/SQL cases PASS** (21 readiness,20 preview,17 receipt), no429, exit0,69.9seconds. Run `feb4cd02cb3e4bbf94c81fd47d97accc`, loopback49699. Concurrent review200/409 with exactly one review audit; injected required-audit failure500 rolls back; actor/tenant/replay/expiry/tamper/stale-source protections retained. Migration prior-upgrade/populated-down refusal/empty-down-up PASS;91application/7Identity migrations and runtime least-privilege/owned-target controls retained.
- Final SQL receipts: `QA/EVIDENCE/meeting-readiness-sql-feb4cd02cb3e4bbf94c81fd47d97accc/result.json`, `QA/EVIDENCE/meeting-preview-sql-feb4cd02cb3e4bbf94c81fd47d97accc/result.json`, `QA/EVIDENCE/meeting-receipt-sql-feb4cd02cb3e4bbf94c81fd47d97accc/result.json`. Prior exploratory run `b180a7f2fb084254b03de7ae055a0e80` also passed58cases and owned teardown, but predates the accepted10.x package pin; preserved, not substituted for final proof.
- Only run-owned SQL databases/logins/containers and guarded harness scratch cleaned by existing ownership checks. Both pre-existing PENTA containers preserved. QA evidence retained locally; ignored build output remains ignored. Main HEAD unchanged at `f7af512f884ca5fe8ac3ddb287c852492592e709`.

Source SHA256: API csproj `A3CE62FB4EE04B12F1ECED3DCCA7CC74DF91F7E4D5BDE4756AFA2BB9A13B62EA`; new regression `602DAD0240A29B23FFFA446903892ECEF85203DEB81A76A8D78CEFF1330DCB86`.

## Remaining release gates and next route

Package remediation is complete locally; installed machine runtimes were **not upgraded**. SDK10.0.401, shared ASP.NET/Core10.0.12 and older side-by-side runtimes remain installed. Microsoft guidance also requires applicable runtime/SDK servicing and restart/redeployment. The Dockerfile uses floating SDK/ASP.NET10.0 tags; no fresh Linux image build, image digest/runtime inventory, Linux loaded-assembly test or Azure redeployment was performed here. Keep runtime/SDK/container patch qualification as a separate pre-Azure gate; this fix does not certify deployed workloads.

Existing decimal-mapping warnings, protected-key persistence/rotation, full security/runtime/SQL/device regression, UI2/Mini qualification and E9–E10 remain open. Frontend unchanged; prior visual proof retained, not rerun. M2 overall is not complete. Provider dispatch and all execution capabilities remain disabled; ReviewedDraft grants no execution approval.

Next **Sol High**: M2 durable operation/outbox/recovery foundation with independent current authorization, consumed-once/version-bound execution authority, atomic audit and failure/concurrency proof. Continue existing M3 qualified-provider and M4 Manual/PENTA integration gates thereafter. No Mini contract/project switch required for this host dependency fix. No stage/commit/push/merge/Azure/production database or credential changes. Preserve full agreed app completion → authorized go-live → Hayansh onboarding → Academy Desk public website;90%AI remains a target, not delivered coverage.
