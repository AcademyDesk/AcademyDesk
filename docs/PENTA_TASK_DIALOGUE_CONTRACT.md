# Academy task dialogue — development contract v1

This default-off candidate extends the shared task foundation. It is NOT the
production conversation driver, a domain execution API or full portal coverage.
Protocol: `academy-task-dialogue.v1`; dependent plan: `academy-task-plan.v1`.
Acceptance and known failures: QA/REPORTS/D63_PROTECTED_TASK_DIALOGUE_2026-11-23.md.

## Host endpoints

All paths start with `/api/academies/{academyId}/penta/chat/tasks`. Authentication,
the current stored Identity actor/stamp/membership, active tenant and enabled
modules are checked independently of model output. Initial scope is non-platform
Owner/AcademyAdmin. These endpoints only operate in Development/Testing with
`Penta:Enabled` and `Penta:Tasks:Enabled` both true. Defaults remain off.

| Method and suffix | Result |
| --- | --- |
| GET /catalog | Current permitted nine-operation metadata subset; executionEnabled=false. |
| POST /conversations | New actor/tenant-bound conversation ID, version0 and24-hour expiry. |
| POST /conversations/{id}/turns | Closed requestId UUID, expectedVersion integer and text1–2000characters. One durable claim before planning. |
| GET /conversations/{id}/turns/{requestId} | Latest completed receipt only; never invokes planner or domain operation. |
| GET /conversations/{id}?beforeVersion={n} | Latest20completed turns before exclusive version, chronological order, hasMore and pendingRequestId. No planner call. |

Receipts include conversation/request/version, kind, message, typed task view,
original input and executionEnabled=false. DRAFT is not a saved academy record;
ERROR retains prior draft. History and receipts are encrypted using a separate
Data Protection purpose containing academy, actor and conversation. A client
cannot deserialize or reuse them as legacy learner conversation state.

Identical request replay returns the recorded latest receipt when its protected
digest and current state match. Different input under an existing ID, stale
versions and pending/unknown results return409. After a timeout/disconnect or
failed completion audit, use GET recovery, not a new dispatch. No in-flight
cancel/abandon endpoint exists yet; do not display a Stop control claiming that
operation is supported. Draft cancel only discards an unexecuted plan.

Create limits are100academy/20actor conversations per day. Turn limits are
500academy/100actor per day and50per session. Existing Mini rows share these
quotas. Expiry stops access; physical retention deletion is a remaining task.
History is20per page, at most50turns. Audit metadata contains status/provenance,
not plaintext conversation or model response. Audit failure is fail-closed.

## Private model contract

The separate provider requires explicit `Penta:Tasks:BaseUrl`, currently only
loopback HTTP root127.0.0.1in Development/Testing. It sends
`POST /v1/academy-tasks/plan`: contract, request_ref, user text, current minimized
task graph and allowed typed catalog. It does not send role claims, tenant IDs,
security stamps or arbitrary transcripts. Do not treat that minimized context as
a substitute for final host/domain authorization.

Closed model decisions: create, edit, clarify, status, cancel, unsupported.
Creation supplies a D62 plan; corrections supply field edits without overwriting
unaffected tasks. Unknown operations/fields, mismatched IDs/types, dependency
cycles, wrong reference entities and model claims of approval/execution fail.
Required/conditional fields and business validation remain host-owned.

Host request maximum16KiB; response maximum32KiB; provider deadline125seconds,
HttpClient130seconds; no redirect, retry or public fallback. New endpoint
source/prompt/contract headers and legacy Mini runtime header are required.
These are reported identities, not independent attestation. The current private
transport is NOT a production authenticated private receiving gateway.

Mini candidate4a0ad0bc90c60e0882c4374704344bb35ae83c70 is on
`codex/academy-conversation-core-20261123`. New opt-in entrypoint and source pins
are documented in its operations/ACADEMY_TASK_DIALOGUE_2026-11-23.md. The pinned
legacy Core used by ordinary demo/paired CI remains unchanged. Do not activate
this candidate on the strength of synthetic tests; real semantic/latency probes
failed and candidate weight-loading approval remains stopped.

## Reproduce model-free/native validation

Run affected .NET tests, build the existing SQL harness with its API project
dependencies into an isolated output, then use the existing guarded runner.
`QA_PENTA_TASKS_NATIVE=1` enables this synthetic-provider SQL packet within
`-Module PentaFoundation`; combine with existing host security, exact-money and
composer preflight flags as documented in the source-bound report. It must use
fresh owned SQL, actual Identity, unchanged RAM guards and exact resource cleanup.
Never direct the harness at production or substitute a mock database for SQL.

No frontend activation in this slice. Next connect authorized factual reads,
source-backed responses, reference resolution and protected domain operations/
approval/receipts before a single model-backed composer replaces the pilots.
