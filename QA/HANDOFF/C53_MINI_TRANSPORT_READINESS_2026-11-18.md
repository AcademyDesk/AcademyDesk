# C53 — Mini application transport and integration readiness

MODEL: Sol High
TOOL: Cloud Codex
REPOSITORY: AcademyDesk/AcademyDesk
BRANCH: codex/cloud-c53-mini-transport-readiness-20261118
BASE: 4cbe56a76119ad257924d975c1bb2ecb97196022
STATUS: CLAIMED — 18 November 2026 UTC

The owner assigned cloud the remaining application-side model readiness while
Desktop owns downloaded artifacts. Cloud claims exactly:

- `apps/api/Intelligence/Penta/PentaMiniProvider.cs` — local provider transport only.
- `tests/AcademyDesk.Api.Tests/PentaMiniProviderTests.cs` — correct HTTP fixture media type.
- `tests/AcademyDesk.Api.Tests/PentaMiniTransportBoundaryTests.cs` — new regressions.
- `QA/HANDOFF/C53_MINI_TRANSPORT_READINESS_2026-11-18.md` — this packet and readiness handoff.

Fresh Desktop tip4cbe56a7 and existing cloud claims were checked; checkout clean.
No other current edit overlaps these Mini transport paths. Desktop retains
protected-host controllers/domain services/SQL/migrations, physical runtime,
artifact checks, model execution and shared root trackers. This task does not
activate a model, alter DI registration, expand tools or remove environment gates.

## Concrete gap and intended behavior

`ReadinessAsync` calls `TryGetProperty` on arbitrary parsed JSON. A valid JSON
array/null/scalar throws InvalidOperationException instead of returning safe
Unavailable. The transport also accepts non-JSON media types and non-final2xx
responses as completed data. Reproduce with regression tests before editing.
Require completed HTTP200 JSON without unsupported content encoding. Keep the
current three-second readiness deadline, bounded16KiB parser/duplicate-key rule,
per-response runtime provenance for plans, no retries/fallback and loopback-only
Development/Testing transport. Preserve `{}` ->Degraded readiness behavior.

Test malformed status/header/body shapes, enabled/disabled gates, valid replies,
no retries, cancellation and per-response recovery. Run relevant existing PENTA
API/host tests and the existing no-model cross-language shared-contract check.
Document the exact current model/capability boundary and Desktop return evidence.
Preserve failures and source/lock/vendor/frozen-Mini files.

No main merge, Azure deployment, environment publication, public access change,
new package/model download, model loading, inference, training or activation.
Mini/Plus/Pro remain unqualified; Ultra deferred;90% target unmeasured. All six
critical acceptance records and the separate private Core credential gate remain.
