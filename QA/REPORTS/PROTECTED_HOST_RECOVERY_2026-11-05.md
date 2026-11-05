# Protected PENTA host recovery — 5 November 2026 UTC

Branch: `codex/ai-launch-readiness-20261104`, base `7db8171`.
Preserved source: `6887e6eb26e88f8fc99eee7a00c267a14c12883f`.

## Scope

Selectively restore the durable host sessions, drafts, approvals, operation
claims, mandatory audit, receipts, recovery and controlled PENTA UI. Preserve
the current domain services and owner-approved finance policies. Do not merge
the preservation branch wholesale or import its superseded migration history.

The additive migration creates exactly 12 PENTA tables plus indexes. It does not
alter or delete existing domain tables. Offline migration/model tests now pass.
Owned SQL verification must prove upgrade, rollback and reapplication against
the current previous migration, including unchanged legacy/Identity data.

Session state now includes a persisted login/logout boundary marker. Explicit
replacement changes it before credentials are persisted; a refresh retains it.
This invalidates stale cross-tab PENTA operations even when token bytes match.
Existing refresh replacement and missing-token safeguards are preserved.

The retained student and teacher conversational writes stay Testing-only.
Production flags cannot qualify a synthetic interpreter. The restored UI keeps
unsupported capability controls unavailable. Default provider/model activation
remains off; this increment is source recovery and host verification.

## Observed verification

| Check | Actual result | Limitation |
| --- | --- | --- |
| Previous published CI `7db8171` / run 41100668488 | Both jobs passed; 1,662 API, 1,239 frontend, 38 owned SQL modules | Applies to base only |
| New full API suite | 1,989 pass, 2 fail, 0 skip | Both positive private-key directory tests encounter sandbox-injected `.git` ancestry |
| Exact rebuilt key-recovery suite, owned offline container | 24 pass, 0 fail/skip | Fresh temporary storage; no customer data/network |
| New full frontend suite | 2,125 pass, 3 fail, 0 skip | Three retained checks require actual communication SQL receipt |
| Azure preflight | 235 pass, 0 fail/skip | Validation and blocking only; no Azure requests |

Raw local logs remain outside Git under `/workspace/work/`:
`ai-launch-api-post-migration.log`, `ai-launch-keyring-nov5.log`,
`ai-launch-frontend-nov5.log`, and `azure-preflight-review-final.log`.
Do not add overlapping suite totals or convert partial evidence into a release pass.

Three complete model-free SQL cohorts are required: foundation/security,
student onboarding and teacher creation. The onboarding cohort must exercise
the actual separately held Core planner/reducer/private factory with scripted
test proposals, including fresh host context, lease/body binding, unauthorized
transport, drift and outage checks. Public CI cannot silently omit that bridge.
Run it in the private integration CI against an immutable public host commit.
This is not inference, model qualification or production activation.

## Azure and release blockers

The new local configuration preflight precedes Azure login and all mutations.
It never prints configured values and cannot be made green by setting approval
flags. The deployment still lacks implemented provisioning/verification of:

- Private Blob storage, API managed identity and scoped storage permissions.
- Shared encrypted Data Protection keys and certificate access across replicas.
- Controlled migrations, compatible rollback and production restore acceptance.

The existing five-critical-issue release guard remains unchanged. All five
critical issues and broader enterprise/browser/device gates remain open.
Mini, Plus and Pro require independent qualification; Ultra remains deferred.
The 90% AI goal is an acceptance target, not measured production coverage.

No main merge, Azure deployment, environment publication, configured database
access, model download/load/inference/training or paid compute occurred.
