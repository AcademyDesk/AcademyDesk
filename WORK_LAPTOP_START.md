# Academy Desk — continue from the work laptop or another profile

Updated30October2026. GitHub source is available; the original laptop's running
services, ignored evidence, dependencies and model caches are not transferred.

## Verified published checkpoints

| Repository | Branch | Commit |
| --- | --- | --- |
| AcademyDesk/AcademyDesk | codex/penta-teacher-service-integration-20261029 | c248912041ac55fbc84ea640fa5b355a54a4e45a |
| AcademyDesk/AcademyDesk | codex/penta-enrollment-integration-20261029 | 4e707629f23a9ef14a92186890fcf85b65c7d85b |
| AcademyDesk/AcademyDesk | codex/penta-contract-integration-20261029 | d44aea500dc14f050e50c78923b0435119b892aa |
| AcademyDesk/AcademyDesk | codex/finance-cloud-reconcile-20261029 | a74a9e44385441a885056ad485229f30b77118d6 |
| AcademyDesk/AcademyDesk | codex/cloud-profile-sync | 00d0d00f0264bb913065aa040a9f0ef0967771b6 |
| AcademyDesk/AcademyDesk | codex/penta-local-source-20261029 | 6887e6eb26e88f8fc99eee7a00c267a14c12883f |
| AcademyDesk/PentaMini | codex/mini-local-source-20261029 | c3c61b9ef3f239035c8b8f46649c66997e98f2b6 |

The first five integration commits form a chain. The original application/Mini
preservation branches hold the remaining source; they must not be merged wholesale.
Accepted main is unchanged atf7af512. Do not mistake a preservation branch for
the integrated application or a production release.

Teacher publication has been independently fetched and verified atc248912.
The next student-service checkpoint is
`codex/penta-student-service-integration-20261029` fromc248912, with1606API and
50SQL/HTTP/service checks including5mixed duplicate races. Owner authorized its
publication on30October. Fetch and verify the actual remote branch/SHA before
using it; do not infer publication from this document alone.

The teacher-service continuation is on
`codex/penta-teacher-service-integration-20261029`, based on4e70762. Fetch and
verify whether this branch has been published and read its actual handoff and
validation report. Do not assume that this document itself grants publication
or that an intermediate running validation passed. If not published,4e70762 is
the last verified integration checkpoint listed here.

## Exact continuation prompt

```text
Continue Academy Desk's existing enterprise/PENTA implementation. Do not restart
the backend, repeat accepted audits or use another profile's chat as evidence.

Fetch GitHub metadata for AcademyDesk/AcademyDesk. Prefer the verified published
codex/penta-student-service-integration-20261029 checkpoint if present; otherwise
use codex/penta-teacher-service-integration-20261029 atc248912. Record exact branch/SHA
and status. Read AGENTS.md, WORK_LAPTOP_START.md, AI_HANDOFF.md,
AI_CROSS_PROFILE_HANDOFF.md, AI_MODEL_ROUTING.md, AI_PRODUCT_VISION.md,
PENTA_HOST_SECURITY_POLICY.md and ENTERPRISE_TESTING_ROADMAP.md.

Start a separate bounded codex/<task> branch from the verified integration tip.
Do not have other profiles edit the same files. The preserved remaining Academy
source is on codex/penta-local-source-20261029 at6887e6e. Mini/Core/frontend source
is in AcademyDesk/PentaMini on codex/mini-local-source-20261029 atc3c61b9. Inspect
each LOCAL_SOURCE_TRANSFER.md and manifest. Bring forward coherent source/tests,
not a wholesale merge. Do not replace the larger retained local test set with
the smaller integration subset or add overlapping counts.

Retain owner-selected financial rules: reject new zero-net payroll, allow only
validated Voided restoration, preserve invoice reductions/refund restrictions,
current tenant/role/resource authority, exact finance, transactions and audit.
Enrollment Manual/service operations and typed adapters are integrated; protected
enrollment conversation approval/execution/receipt/recovery is still pending.
Student/teacher domain services are integrated in the student checkpoint; their
protected conversational approvals/receipts/recovery are still pending. Preserve
and join the8new cloud commits througha63e392 fromcodex/release-export-security,
including role/session/recovery/export, payroll print, assessment/attendance and
work-item validation; do not replace those fixes with the preserved older source.
Continue protected student/teacher host persistence, approvals and recovery,
the explicit cloud/local migration path, then connected PENTA/Manual visuals and
remaining conversational workflows using the existing source and tests.

Mini is NOT ACCEPTED. The owner explicitly stopped before candidate weights
loading: no model loading/training/inference experiment/activation/downloads,
paid compute or Azure. Source access does not override that stop. Current Mini
preservation CI40804103447 failed with26failed/1651passed/1skipped/60errors;
logs include missing laptop-only evidence prerequisites. Do not fabricate those
receipts, erase failing tests or hide failures with skips. Review portability
without changing historical provenance or promoting training/model acceptance.

Preserve independent Academy host authorization, typed allowed tools, required
scoped approval, deterministic services, real results and mandatory audit. Keep
Manual usable if AI is unavailable. No arbitrary model SQL/code/roles/tenants.

Run relevant existing tests against exact source and disposable owned SQL only.
Never access production/customer DBs or copy laptop credentials. Existing local
Docker/RAM permissions refer to the original machine, not unrelated work-laptop
processes. Obtain/setup a suitable approved runtime instead of stopping unknown
services. No main merge, Azure deployment or repository-settings change. New
commits/pushes require current task authorization.

Update handoff with actual source/results/failures, remaining gates and next
model. Sol High for security/transactions/host integration; Sol Medium for settled
UI/docs.90%conversational AI/10%Manual remains a target, not verified coverage.
```

## Laptop-specific prerequisites still local

The D: model checkpoints/training environment and historical QA review/runtime
receipts remain on the original laptop. They are not needed merely to review or
edit shared source. They are required where an existing native/provenance test
explicitly depends on them; that test cannot be declared passed without its
actual prerequisites. No model qualification was performed by the source transfer.

Use the existing configured cloud environment if one is available to the current
profile. This chat has not selected/started a cloud task automatically. The new
machine must have repository access to both AcademyDesk and private PentaMini;
do not paste credentials into chat or project files. The public product website
remains deferred until the app launches and the first academy is onboarded.
