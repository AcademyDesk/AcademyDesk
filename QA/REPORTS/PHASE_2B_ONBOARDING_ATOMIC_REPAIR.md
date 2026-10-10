# Student onboarding — domain/Identity/audit atomicity

2026-10-10; `D:\AcademyDesk-codex-p0`, `codex/penta-search`, starting HEAD `517a943060f6430a1f0f7423b9f37180445164c8`. Existing BUG-DATA-0004 / STUDENT-DB-001 continuation, not a new testing plan or repeated accepted audit.

## Reproduction and repair

Baseline native run `066c3660351545adbd4f5024c72875d3`, SQL loopback port52895: real authenticated minor onboarding with two requested portal accounts and invalid parent password returned400, then failed the strict fresh snapshot equality assertion. The academy-only transaction did not encompass UserManager writes. This receipt proves changed captured state after failure; baseline did not emit individual row deltas and is not represented as a fully enumerated orphan count. Zero strict cases passed before failure. Original domain rollback alone was insufficient.

The endpoint now uses the existing `AtomicAcademyMutation` same-store domain/Identity transaction and success-audit boundary. Its inner transaction/early commit are removed. Both role creation and membership assignment results are checked; failures retain the existing400 error paths. The response is successful only after the action/filter boundary commits. Audit-store failure returns500 and rolls back rather than publishing an unaudited success.

An explicit, default-false `IncludePlatformOwner` marker makes this action use the same boundary for a platform-flagged, same-academy administrator. Otherwise that actor would bypass the shared transaction. It changes transaction/audit protection, not authorization: the controller still demands its existing same-academy Owner/AcademyAdmin rights; platform-flagged foreign requests are403. Other endpoints retain their existing bypass behavior. No new PENTA permission/tool or global policy is introduced.

SQL configuration must match for the existing Identity enlistment; no cross-database transaction, new schema/migration, Azure credential, production data, model or frontend change. Date/default/optional guardian/minor/access-consent and payload semantics are unchanged. Incomplete credential created flags remain BUG-DATA-0007, not silently claimed fixed.

## Strict native evidence

Final run `97dde5dfa27849bab8dfbdb2597582d0`, port56583,21/21 cases PASS:

- Invalid second-account password and duplicate parent username:400; no captured persisted changes.
- SQL faults on guardian link, first user, first membership, second user, second membership:400; each fault reached exactly once, complete captured rollback.
- SQL success-audit fault:500, exactly once, complete rollback.
- Parent membership IdentityResult failure and Guardian role-creation IdentityResult failure:400 and complete rollback. The membership-result validator verifies the parent account already exists inside the active store before rejecting the pending membership.
- Minor with both accounts:200; exactly one student, guardian and relation, two users/memberships and one audit; fresh SQL account IDs/academy/roles link to the created records. Optional accounts omitted:200 with no accounts/memberships and false flags; domain guardian access default retained.
- Anonymous401, Teacher403 and foreign academy403: complete no-write snapshots.
- Owner and same-tenant platform-flagged Admin: successful200 exact deltas and invalid-parent400 complete rollback each. Platform-flagged audit fault500 rolls back; foreign route403 remains denied.

Snapshot comparisons use fresh SQL contexts and capture student/guardian/relation/audit rows, safe Identity user projections, roles and memberships. No credentials/password hashes or returned tokens are emitted. Native rate limiter is retained; exact expected statuses exclude429. Intentional500 audit faults are not accidental success or hidden failures.

Health/auth/real two-tenant controls and runtime inventory PASS:327 routes,316 controller method/routes,10 Identity routes; route SHA `0B0B59901F3F2AA521F16B0A139D2B91A816A300D661F7E4DD9CEB156FB877FC` unchanged.88 application/7 Identity migrations applied; both resolved contexts/runtime login match the exact disposable store. Negative ownership guard and terminal database/login teardown PASS.

## Retained regression / validation

- OnboardingDates23/23 native SQL/Identity/HTTP PASS after the transaction change: run `c6af1fb5355a457f98c3d38363f1fdb7`, port60204; existing optional/required date binding, successful persistence/defaults and denied/invalid no-write assertions unchanged.88/7 migrations and owned teardown PASS.
- Targeted unit56/56 PASS: AuditOutcomeTests36, retained PeopleBranchValidationTests6, StaffRoleBoundaryTests and AcademyIdentityTransactionTests14 combined. Earlier initial filtered run36 and intermediate50 also PASS; only final56 is used as the consolidated count, not totals added together.
- Both isolated SQL harness builds0 warnings/0 errors. PowerShell runner parsing and scoped diff checks verified before publication. New harness module is additive; existing modules/assertions are not weakened.
- Frontend handler/browser/typecheck/export evidence from the previous optional-date batch retained, not rerun; no frontend file changed. Full API/critical, broader native modules, direct browser-to-SQL, physical devices, concurrency/cancellation and store-mismatch runtime tests NOT RUN in this slice.
- Existing global QA consistency validator remains blocked by missing local `QA/EVIDENCE/logs/observed-checks.json`; not rerun or claimed PASS. Do not fabricate the missing historical evidence.

Local receipt: `QA/EVIDENCE/onboarding-atomic-97dde5dfa27849bab8dfbdb2597582d0/result.json`. The failed baseline container was removed only after exact name/run-label/loopback checks; final runs verified DB/login teardown and their own container cleanup. No other container pruned or removed; Mini containers retained. No successful final database/login teardown is retrospectively claimed for the failed baseline.

The isolated API builds tested the preserved pre-existing dirty Mini snapshot, not acceptance/publication of unrelated Mini changes.11 unrelated tracked edits,32/28 lower continuity additions, intentionally untracked evidence and ignored .build-check remain preserved.

## Disposition and next route

BUG-DATA-0004 remains OPEN for broader acceptance. Existing28 feedback-route checkpoints and enterprise/PENTA/FW1/release gates remain open at their recorded levels. NEXT existing BUG-DATA-0007 created-account response truthfulness with incomplete optional credentials, Sol High for Identity/domain integration; Sol Medium for settled Manual/frontend feedback and visuals. No new Mini contract is needed here, so no project switch or speculative handoff. Continue existing E1–E10, including versioned FW1 frontend delivery, private Mini qualification and manual/AI end-to-end workflows; target90% eligible AI/10% manual is not a measured completion claim.

Scoped feature commit/push under the standing delivery directive only; no main edit/merge/push, Azure deployment or production action. Publication verification is reported in the final handoff, not inferred from these local test passes.
