# C28 delegated academic grading lifecycle — 10 November 2026

MODEL: Sol High for authorization/transactions; Medium for settled documentation.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c28-academic-lifecycle-20261110.
ENVIRONMENT: preserved existing Cloud checkout; no model execution.
BASE:56072b0bb7025e27fd9f9d2351f0bad0a817353a (clean verified C27).
DESKTOP:26fb7d2b9e8338fae0ede2ec99c8f0adb9ea752c already included.
TASK: BUG-FUNC-0015 delegated grading lifecycle consistency; complete reads,
current write intent, explicit mutation confirmation and recovery in governance UI.
STATUS: CLAIMED, reproduce original before changes.

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
