# C12 compensation metadata read safety — 7 November 2026

Owner: Cloud Codex. Status: IMPLEMENTED / CLOUD VALIDATED, local acceptance pending.
Branch: `codex/cloud-c12-compensation-metadata-20261107`.
Base: `3ffaa9edb409fee3984cd0e3525ee6a18390a3cd`, clean checkout verified.
Latest inspected Desktop claim: `8963869`, private media/payment/restore retest;
latest integration tip `3ffaa9e` includes the C11 return.

Exact claimed paths:

- `apps/api/Controllers/TeacherCompensationController.cs`
- `tests/AcademyDesk.Api.Tests/TeacherCompensationMetadataTests.cs`
- `QA/ISSUES/BUG-API-0004.md`
- `QA/HANDOFF/C12_COMPENSATION_METADATA_2026-11-07.md`

Reproduce valid non-object compensation JSON through the public controller GET,
then make its metadata lookup safe without changing legacy object aliases,
zero/null preservation, save/audit behavior, payroll rules or authorization.
Validate original root shapes, valid objects, missing/foreign teacher routing,
read-only preservation and relevant API regressions using existing dependencies.
Attempt the installed full API suite without downloading packages or contacting
the configured SQL host. InMemory/controller results are not SQL/HTTP authority
acceptance. Where practical use the existing test-host patterns for HTTP checks.

Portal announcement readers are part of the same issue but NOT claimed: Desktop
owns current portal/private-media retesting. Return an exact change request and
malformed-metadata cases instead of modifying its active controller. The issue
remains OPEN until that reader and all linked release checks are accepted.

No shared handoff overwrite: this task report is the bounded handoff for the
integrator to summarize in `AI_HANDOFF.md`. No main merge, Azure deployment,
environment changes, customer database or model loading/activation.

## Actual result

Implementation: `375418c22be01315a93e48355482657c8a1ae27a`.
Published path claim: `af6ff2b`. Only the four claimed paths changed.

The original `Property` reader called `EnumerateObject` on every parsed root.
For valid JSON null, arrays, strings, numbers and booleans this throws
`InvalidOperationException`, outside the existing `JsonException` catch.
The repair checks `JsonValueKind.Object` before enumeration. Other shapes use
the existing empty compensation summary, without rewriting the original row.

Canonical and legacy case-insensitive object aliases, zero/null values,
decimal amounts, effective dates and invalid optional-field fallback retain
their behavior. Save, audit, authorization, payroll and schema are unchanged.

## Executed evidence

Installed SDK 10.0.403 and existing package cache; no package restore/download.
Each command targets `tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj`:

```sh
/workspace/.tools/dotnet/dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --filter FullyQualifiedName~AuditOutcomeTests --logger 'trx;LogFileName=runner-preflight.trx' --results-directory /workspace/c12-compensation-metadata-20261107
/workspace/.tools/dotnet/dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --filter FullyQualifiedName~TeacherCompensationMetadataTests --logger 'trx;LogFileName=metadata-baseline.trx' --results-directory /workspace/c12-compensation-metadata-20261107
/workspace/.tools/dotnet/dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --filter FullyQualifiedName~TeacherCompensationMetadataTests --logger 'trx;LogFileName=metadata-final.trx' --results-directory /workspace/c12-compensation-metadata-20261107
/workspace/.tools/dotnet/dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --no-build --logger 'trx;LogFileName=api-full-final.trx' --results-directory /workspace/c12-compensation-metadata-20261107
node QA/tools/check-production-gates.cjs
git diff --check
```

The baseline command preceded the repair and used the unchanged controller.
The final targeted command rebuilt both assemblies; the full regression reused
those exact assemblies without intervening source changes. Output and TRX were
retained. TRX counters independently checked:

| Run | Pass | Fail | Skip / unexecuted |
| --- | ---: | ---: | ---: |
| Audit runner preflight | 48 | 0 | 0 |
| Original metadata baseline | 22 | 14 | 0 |
| Repaired metadata target | 36 | 0 | 0 |
| Full API suite | 2,027 | 0 | 0 |

The 36 target tests comprise 15 absent/malformed/non-object/object-empty cases,
three monthly and three hourly legacy/zero/value cases, six invalid optional
value cases, two missing/foreign teacher cases and seven authenticated HTTP
cases. The original nine direct non-object cases threw and five HTTP cases
returned 500. After the repair those cases pass unchanged.

HTTP coverage uses actual MVC routing, Identity login/access tickets, current
user validation, academy access filter and response serialization. Each HTTP
case checks anonymous401, same-academy200, foreign-academy403 and missing404.
Fresh scope confirms the exact original metadata and no audit write. Both
domain and Identity providers are explicitly replaced with unique InMemory
databases; migrations/bootstrap/media/PENTA are disabled. No laptop SQL,
configured customer connection, model or external provider is contacted.
These results are not SQL durability, concurrency or deployed-host evidence.

The production gate intentionally exits1 for the five still-open/unverified P0
records. Diff check passes. No frontend changed; C11 browser/build results are
retained source-bound evidence, not rerun or attributed to this packet.

Raw run directory: `/workspace/c12-compensation-metadata-20261107` (excluded
from Git). It retains the original controller, baseline/final logs and TRX,
production-gate output and `source-artifact-manifest.json` with source,
assembly and receipt SHA256 hashes. Source hashes:

| Source | SHA256 |
| --- | --- |
| Original compensation controller | `f02abd5281e752bffb12e8895ff1312e5abe9de9b2b4bc18f8720e0421de23ff` |
| Repaired compensation controller | `fb1f916e056c94556b4efabfda0927b3e7802b0ecd1c7ec2bdb7eb0656debe32` |
| New test file | `2d92cec2a8aec70d0a9428c57e619b438c5d9e9da39ba541220b2a0fa9bd5410` |

## Desktop change request and acceptance

Do not import an entire branch over local work. Inspect and select the four
claimed paths; compensation Save's existing atomic-audit attribute is retained.

1. After current P0 portal/private-media retesting, claim the announcement
   helpers in `PortalController.cs`. Require an object before `TryGetProperty`
   and string kinds before `GetString` for `important`, `startsAtUtc`,
   `expiresAtUtc` and `audiences`. Preserve canonical string metadata emitted by
   Notifications and legacy audience behavior for valid objects. Malformed
   shapes must not expose an announcement to a broader audience.
2. Exercise null/array/scalar roots and wrong-kind important/date/audience
   fields through actual Student and Teacher announcement endpoints. Assert
   no500, correct visibility/time window, tenant/role isolation and no writes.
   Retain canonical valid, expired, future and audience-filtered controls.
3. Against disposable SQL, seed the compensation root/legacy cases, authenticate
   and GET them, compare fresh SQL bytes and unrelated rows/audits, then remove
   only the owned resources. Do not repair production/customer metadata as a test.
4. Run combined API regression after the portal repair and the issue's linked
   critical/browser/device acceptance. `BUG-API-0004` remains OPEN until then.

## Remaining production work

At base `3ffaa9e` there are 120 tracked issue files: original118 plus two newer
P1 findings, grouped as five P0,102P1,13P2. Several have bounded repairs; none
of those counts means untouched work, completed acceptance or percent readiness.
The five P0s remain `BUG-DATA-0001/0002/0003/0010` and `BUG-SEC-0001`.
Desktop owns their current private-media/payment-status/restore retest.

Priorities remain: finish P0 evidence and remaining critical role/tenant/domain
workflows, integrate source and verify exact CI/images/security checks, then
complete physical-device, staging and production rollback/restore gates.
This feature branch push does not trigger `verify.yml`: its push filter is
`main` / `codex/release-export-security`; main PR, manual and reusable entry
points also exist. No workflow was dispatched. Remote CI for this exact source
is UNKNOWN; a successful push is not a green check.

AI90% remains a target, not measured acceptance. Mini/Plus/Pro are not qualified;
Ultra is deferred and model loading remains stopped. No main/Azure/environment
publication or public-network change. Next route: Sol High for Desktop's
protected HTTP/SQL/portal checks; Sol Medium for settled source import/docs.
The final documentation commit is the discoverable branch tip and contains this
implementation SHA; final remote-tip verification is retained in local artifacts.
