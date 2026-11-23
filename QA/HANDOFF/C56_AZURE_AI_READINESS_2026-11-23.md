# C56 — Azure preparation and conversational launch gaps

MODEL: Sol High
TOOL: Cloud Codex
REPOSITORY: AcademyDesk/AcademyDesk
BRANCH: codex/cloud-c56-azure-ai-readiness-20261123
ENVIRONMENT: managed cloud, local/read-only checks
BASE: c3617f28d458287189ca7173c8fe26ddea8fa271
STATUS: CLAIMED — 23 November 2026, Asia/Calcutta

Cloud owns exactly these new paths:

- `QA/tools/azure-readiness-preflight.cjs`
- `QA/tools/azure-readiness-preflight.test.cjs`
- `QA/HANDOFF/C56_AZURE_AI_READINESS_2026-11-23.md`

TASK: Prepare a dependency-free local preflight that reports required deployment
inputs without exposing their values, retains the existing critical acceptance
guard and never calls Azure. Review current deployment/runtime wiring and map
current conversational limitations against the owner's 90% launch goal.

MUST NOT MODIFY: Desktop-owned native SQL/model/demo paths, shared root handoffs,
runtime/provider gates, workflows, application behavior, packages/locks, secrets,
cloud environment settings, original archives or frozen inputs. No main merge,
deployment/provisioning, public-access change or model loading/activation.

VALIDATE: Pure validation and subprocess tests for invalid/missing configuration,
credential redaction and blocked release behavior; existing production-guard
tests; current real preflight must retain open critical failures. Compare all
unclaimed base paths byte-for-byte. Verify remote publication SHA and exact CI.

STOP WHEN: bounded preparation is tested and published with explicit remaining
native, model, device, staging and release requirements. This packet cannot
qualify all AI workflows or authorize a deployment.

NEXT ROUTE: Desktop Sol High for native/model and combined acceptance; Cloud
continues only independent claimed source. Preserve the current synthetic demo;
do not run heavy local suites while Desktop's demo is active.
