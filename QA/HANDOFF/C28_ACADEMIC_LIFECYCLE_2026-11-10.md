# C28 delegated academic grading lifecycle — 10 November 2026

MODEL: Sol High for authorization/transactions; Medium for settled documentation.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c28-academic-lifecycle-20261110.
ENVIRONMENT: preserved existing Cloud checkout; no model execution.
BASE:56072b0bb7025e27fd9f9d2351f0bad0a817353a (clean verified C27).
DESKTOP:26fb7d2b9e8338fae0ede2ec99c8f0adb9ea752c already included.
TASK: BUG-FUNC-0015 delegated grading lifecycle consistency; complete reads,
current write intent, explicit mutation confirmation and recovery in governance UI.
STATUS: SOURCE FIXED / Cloud verification complete; native SQL/audit/device/release acceptance open.

Exact MAY MODIFY paths:

- `apps/api/Security/PermissionCatalog.cs` — add lifecycle academics.manage mapping only
- `apps/api/Security/SubscriptionPlanCatalog.cs` — align grading lifecycle with existing AcademicGovernance subscription policy
- `apps/api/Controllers/GradingSchemeLifecycleController.cs` — explicit boolean validation and existing atomic actor-audit opt-in
- `apps/api/Controllers/AcademicGovernanceController.cs` — grading creation atomic audit opt-in only; preserve prerequisite graph implementation
- `tests/AcademyDesk.Api.Tests/GradingLifecycleAccessTests.cs`
- `tests/AcademyDesk.Api.Tests/CoursePrerequisiteTests.cs` — replace obsolete creation-no-boundary expectation with explicit domain/actor-audit protection; retain prerequisite boundary assertions
- `apps/web/src/app/academic-governance/page.tsx`
- `QA/tools/academic-lifecycle.test.cjs`
- `QA/tools/academic-lifecycle-browser.cjs`
- `QA/ISSUES/BUG-FUNC-0015.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C28_ACADEMIC_LIFECYCLE_2026-11-10.md`

MUST NOT MODIFY: protected PENTA/make-up/enrollment/SQL/migrations, shared access
filter/auth, other role grants or permission keys, grade-band/grading/prerequisite
rules, vendor pins, Mini frozen paths, existing evidence or unrelated work.
No main/Azure/environment publication/public-access changes. Existing model
loading/download/inference/training/activation stop remains. Mini/Plus/Pro
unqualified; Ultra deferred. Five P0s and broader session release gate stay open.

Creation, active lookup and lifecycle share the existing academics.manage and
AcademicGovernance policy. This grants no broad system-role escalation. Verify
active/custom/permanent/timed grants, expired/revoked/wrong permission, tenants,
subscription/inactive academy, admins and Platform Owner. Platform Owner retains
existing global semantics but opted-in writes require atomic actor audit. Reject
missing/null lifecycle state; false is an explicit legitimate state. SQL required-
audit rollback remains Desktop acceptance; Cloud HTTP uses Identity/InMemory.

UI must require a complete current courses/schemes/prerequisites read before any
mutation. Refuse stale/overlapping callbacks, preserve drafts on refusal/unknown
outcome, distinguish confirmed change plus failed readback, and recover with GET
only. Unconfirmed creation needs successful history review plus deliberate new
intent. Keep existing forms, prerequisite rules and nullable bands policy intact.

VALIDATE: original/final actual HTTP tests; actual TSX controlled faults; native
sandboxed Chromium/synthetic persistence at320/1440; locked build/types/lint,
full affected suites and unchanged release guard. Preserve raw originals/failures
outside Git. Publish source/result pins to feature branch and explicit Desktop
SQL/role/browser/device tasks. Do not infer hosted CI from a feature push.
STOP WHEN: bounded fixes verified and feature return/handoff published; no formal
issue or production qualification without remaining acceptance evidence.
NEXT ROUTE: Desktop Sol High for native SQL/audit/role/device acceptance; Desktop
retains make-up scheduling. Cloud next nonoverlapping Manual source packet.


## Claim refinement after full regression

The initial full API run found CoursePrerequisiteTests' historical assumption
that only AddPrerequisite opts into atomic audit. C28 intentionally protects
AddScheme too. Claim that existing test path before changing it; preserve graph
and read-boundary assertions and positively assert the new grading transaction
and Platform Owner audit protection. This is an updated contract expectation,
not a skipped test or relaxed business invariant. Raw first full failure retained.


## Source and behavior result

Implementation: `db012648e608e1fa5cf7291ce0271af3f4b1407d`. No main merge, Azure deploy, environment
publication or model execution. Original claim09e07d7070dd73c2ed7a3168b2652a4b0dbb3d3d
was pushed before implementation; the existing audit-test path refinement was
also committed/pushed before its update. Existing local work and raw QA records
are preserved. The recovered environment now executes real commands against the
unchanged C27 checkout. Runtime/current observations report running/connected,
restricted network policy enforced. No environment/network/privacy settings were
changed. The policy snapshot declares configured automatic Tailscale access;
empty legacy TCP CIDR grants do not prove failure under that mode. No current
request to the laptop100.98.98.13 or private API/SQL was made, so those services
remain unverified; no D: drive or model-cache access is inferred.

The lifecycle controller now uses the existing academics.manage permission and
AcademicGovernance subscription, matching scheme creation and the existing page
module. No system role gained a broad permission and no global filter changed.
Custom role and active permanent/timed academy grants can perform the existing
workflow; unrelated, absent, expired, revoked and foreign grants remain denied.
Missing/null lifecycle isActive now rejects400; explicit false/true is required.
Active lookup and status mutation stay owned-tenant scoped. Platform Owner's
existing global access semantics remain, with grading creation/status explicitly
opting into the existing domain/actor-audit transaction for that actor too.
Schema, Identity writes, grading/bands evaluation, existing prerequisite graph
and admission rules, and protected PENTA paths are unchanged by C28.

The governance page requires a complete validated current courses/schemes/
prerequisites snapshot before edits. It shows unavailable registers instead of
false empty/zero results on failed reads and offers GET-only retry. Synchronous
pending refs and versioned intent reject overlapping or stale callbacks. Numeric
inputs are validated before blank-to-zero coercion; JSON retains the server's
existing parse-only policy. Captured forms reset only after an acknowledged POST;
unknown outcomes retain fields. Lifecycle PATCH requires the exact id/boolean
receipt. Confirmed saves with failed readback remain confirmed. Unknown outcomes
lock writes until successful read-only review and deliberate new action; neither
reload nor review automatically POSTs/PATCHes. Unmount/read versions reject late
state writes and detached form resets. Existing form layout/padding is preserved.

## Verification boundary

| Check | Actual result / limits |
| --- | --- |
| Original actual HTTP/Identity/InMemory |28 cases:20 PASS/8 FAIL. Failure includes delegated lifecycle/lookup, module mismatch, Platform Owner audit and missing-state mutation; original source/log/TRX retained |
| Targeted fixed API |40/40 PASS:28 new plus12 retained academic workflow/access controls; zero skips |
| Initial full API |2,352 PASS/1 FAIL out of2,353; historical AddScheme-no-atomic-boundary expectation contradicted C28's intended stronger audit contract. Retained failure; replaced that one expectation with positive grading atomic/Platform Owner/domain-only assertions and retained prerequisite/read boundaries; no skip or business assertion weakened |
| Final combined API |2,353/2,353 PASS with Desktop7d5c013,zero failures/skips |
| Actual TSX controlled transport |Original3 PASS/26 FAIL; final29/29 PASS, including invalid/incomplete reads, success, drafts, stale overlap, unknown/confirmed readback, explicit new intent and late unmount. This is not SQL/browser evidence |
| Full frontend |87 files,2,346 cases:2,343 PASS/3 FAIL/zero skips/cancelled. Only communication-drafts fails45/48 because genuine phase-2b communication SQL receipt log is absent locally. No receipt fabricated or check suppressed |
| Native Chromium |24/24 PASS at320/1440,light/dark,synthetic delegated session. Real form/select controls, create/status/prerequisite readback, unavailable/retry, duplicate submit, lost-before/lost-after, malformed status receipt and recovery.16 synthetic POST attempts/14 commits/8 PATCHes. Zero captured runtime/interception errors, no checked page overflow, normal sandbox and owned profile cleanup; screenshot review PASS. No live SQL, actual identity service, physical-device or production proof |
| Exact-lock web |Build/types PASS;87 static aliases/570 files;lint zero errors/61 existing warnings; no package installation or audit disclosure |
| Release guard |15/15 control tests PASS; actual guard exit1 retains five open P0 records and the broader session acceptance record |
| Current hosted CI |UNKNOWN; feature pushes do not trigger verify.yml's main/codex-release filter. No dispatch, PR/main merge or release claim |

Raw originals, logs/TRX, full frontend output, native request/result/screenshots,
Desktop preservation hashes and final publication/source/export/assembly manifest
are retained outside Git at`/workspace/c28-academic-lifecycle-20261110`.
No QA/EVIDENCE or generated artifacts are published. Initial full failure remains
separate from final regression. Tests were updated only for the deliberately
stronger atomic audit contract; original prerequisite graph checks are retained.

## Concurrent Desktop source preserved

Desktop subsequently published7d5c013c7312a91a6bb472e86d3a7d8a4d34c891.
It was imported with cherry-pick provenance; its13 changed paths are byte-exact,
including shared Manual make-up service, Testing-only approved make-up workflow,
SQL harness/report and shared handoff/status files. Desktop's report records50
retained Manual and31 authenticated HTTP/SQL cases including approval, recipients,
replay and rollback. Those are retained Desktop results, not rerun SQL here.
Its next owned work is regular scheduling and assigned-Teacher attendance.
No overlap with C28's declared academic paths was found. Do not overwrite newer
Desktop files when integrating this return; cherry-pick C28 implementation/pins
or copy its exact13 claimed paths. Prior C24–C27 live acceptance remains queued.

## Desktop acceptance, in priority order

1. Integrate the C28 source/result pins while preserving newer protected-host,
   scheduling, SQL and local work. Use Sol High for authorization/transactions.
2. Run actual Identity/HTTP/disposable SQL: delegated academic role and permanent/
   timed grants create/list/active/deactivate/reactivate; real next-request expiry
   and revocation; unrelated staff/Teacher/Student/Guardian, foreign grants/tenant,
   missing module and suspended academy. Preserve existing Platform Owner global
   policy and normal role/module refusals. Do not grant teacher/staff management.
3. Require explicit boolean lifecycle400/no-write for missing/null/malformed state;
   verify owned sorting/filtering, foreign/missing record404, metadata unchanged
   and explicit false/true persisted. Check existing assessment active-scheme
   selection and grading results while scheme lifecycle changes.
4. Inject required actor-audit failure on scheme POST and lifecycle PATCH, including
   Platform Owner. The fresh SQL scheme/status and actor audit must roll back
   together. Confirm successful operations commit exactly once with attributed
   audit. Cloud InMemory cannot demonstrate transaction durability/rollback.
5. Run live browser/SQL create/status/prerequisite workflows and interrupted
   responses. GET-only recovery must never replay the write; unknown POST needs
   deliberate new intent, confirmed plus failed readback stays confirmed. Verify
   real role/module UI, overlays and physical devices alongside retained graph,
   grading and critical regression before closing BUG-FUNC-0015.

## Remaining production and AI entry gates

The issue stays OPEN for native SQL/audit/actual-device/linked critical/documented
acceptance. All120 records remain inventoried (5 P0/102 P1/13 P2); this is not120
untouched defects or a completion percentage. Five P0 records and broader session,
all-role/physical-device, prior Cloud returns' live acceptance, real provider
setup/meetings/delivery, exact-candidate hosted CI/current security/image checks,
and staging backup/restore/rollback remain open. Do not retry the previously
rejected external npm audit disclosure under Continue.

The owner's non-AI entry gate is not yet complete. Deterministic Desktop PENTA
backend integration can continue in parallel. Real model loading/downloads/
inference/training/activation remain stopped; Mini/Plus/Pro are not qualified,
Ultra is deferred and90% conversational outcome coverage is unaccepted. Notify
the owner when the non-AI baseline is accepted, before the concrete model packet.
