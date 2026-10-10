# Student onboarding — truthful account-created responses

2026-10-10; `D:\AcademyDesk-codex-p0`, `codex/penta-search`, starting HEAD `70f1637019554e53ada6961f634e1e9124fe0b6b`. Existing BUG-DATA-0007 / STUDENT-API-001 continuation. No audit restart or new testing strategy.

## Bounded change

Account creation already requires both nonblank username and temporary password; parent creation additionally requires a guardian record. The response previously tested only usernames. Both returned flags now start false and are set true only after the matching CreateAccount call returns successfully, including its checked membership assignment. The prior domain/Identity/audit commit boundary remains authoritative; a later audit failure still returns failure rather than successful flags.

Incomplete optional credentials keep the existing successful no-account behavior. No new mandatory fields, validation rule, role, permission, PENTA tool, model contract, frontend or schema/migration change. Adult/minor/guardian consent defaults, temporary-password handling and original response property names remain unchanged. These flags describe this operation's confirmed creation, not a promise about future account availability.

## Native reproduction and proof

Baseline `11a905dd8a874305908f6324ce0bdaa4`, port64351: omitted-credentials control PASS, then parent username-only HTTP200 fails expected-false flag assertion. Source shows the identical student username-only defect. Baseline stopped at the flag assertion before the failing case's fresh account query, so it is not represented as an enumerated account-count proof. No customer data or credential values emitted.

Baseline build invocations overlapped during test-only review; the older compiled ordering reached parent username-only first. This is recorded, not claimed as student-first/source-parity evidence. Final builds completed before their runs; final ordering, fresh persistence checks and source hashes are pinned in the local receipt. Test review also accommodates legitimate empty401/403 bodies, preserves strict statuses, moves flag acceptance after fresh account/role verification, and prioritizes the student username-only case. No acceptance cases removed or weakened.

Final OnboardingAccounts run `5f36b5def8d6444095ced20cf91819c6`, port51150:33/33 PASS:

- All16 combinations of student and parent credentials: neither/username-only/password-only/both. Only completed credentials with a corresponding domain person yield an account and true flag.
- Six explicit null/empty/whitespace controls, independently for each account while the other is created.
- Four no-guardian controls with parent credentials absent/partial/complete; no guardian account falsely reported or saved.
- Two minor cases: no accounts and both accounts; guardian access/minor defaults retained. Adult guardian access default also asserted across the matrix.
- Anonymous401, Teacher403 and foreign-tenant403: unchanged captured state and no successful flags.
- Invalid student password400 and invalid second-account password400: complete captured rollback and no successful flags.

Every successful case asserts exact fresh SQL student/guardian/relation/account/membership/audit row deltas, returned IDs/minor state, correct academy/person links and real Student/Guardian membership before flags are accepted. Rejected cases compare complete captured safe snapshots. No credentials/hashes/tokens captured in snapshots or printed. Rate limiter unchanged; exact expected statuses exclude429.

## Retained checks and cleanup

- Unmodified OnboardingAtomic source,21/21 native cases PASS after this change: run `9377d527f7f041478684753bf447a00e`, port53377. Existing SQL-stage/role-result/audit failures, optional-account success, Owner/platform opt-in and denied no-write assertions retained.
-56 targeted unit tests PASS: AuditOutcomeTests, PeopleBranchValidationTests, StaffRoleBoundaryTests, AcademyIdentityTransactionTests. Not the full API/critical suite.
- SQL harness/API build0 warnings/0 errors; runner PowerShell parsing and scoped diff checks PASS. Module/flag additions are additive; existing modules untouched except shared argument allowlist/runner routing.
- Both final native hosts verify base health/auth/two-tenant controls,327 routes/316 controller method/routes/10 Identity routes, unchanged route SHA `0B0B59901F3F2AA521F16B0A139D2B91A816A300D661F7E4DD9CEB156FB877FC`, exact scoped SQL target/runtime login,88 application/7 Identity migrations, negative cleanup guard and terminal DB/login teardown.
- Failed baseline container removed after exact name/run label/loopback checks; no terminal DB/login teardown is retrospectively claimed. Final runs remove only their own verified containers. Other QA/Mini containers untouched; no prune.
- Receipt `QA/EVIDENCE/onboarding-account-flags-5f36b5def8d6444095ced20cf91819c6/result.json` remains local/untracked. Pre-existing dirty Mini snapshot tested but not accepted/published;11 unrelated tracked files and32/28 shared continuity additions preserved. .build-check remains ignored.

Previous23 optional-date SQL and frontend handler/browser/typecheck/export results retained, not rerun; no date/frontend changes. Direct browser-to-SQL, physical Android/iOS, concurrent onboarding/cancellation/store-mismatch runtime and full critical/release tests NOT RUN. Global QA consistency validation remains blocked by the existing missing local observed-checks.json; not fabricated or claimed PASS.

## Continuation

BUG-DATA-0007 stays OPEN for broader acceptance; BUG-DATA-0004 remains OPEN at its previously recorded bounded repair. Original28 feedback checkpoints and E1–E10 remain intact, not whole-application completion. NEXT BUG-DATA-0008 blank optional student-number unique-index collision: reproduce only missing disposable-SQL cases, Sol High for constraint/transaction proof. Sol Medium for queued onboarding feedback and settled Manual/shared visual work. FW1/Mini qualification and conversation/host integration remain pending at their recorded gates; no new Mini handoff/project switch required for this host-only fix.

Standing delivery directive permits a scoped feature commit/push only. No main edit/merge/push, Azure deployment, production credentials/data, new model/training or permission expansion. Target90% eligible AI/10% manual remains a product goal, not measured readiness.
