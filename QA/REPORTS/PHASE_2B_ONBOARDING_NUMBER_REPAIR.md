# Student onboarding — optional student-number SQL normalization

2026-10-10; `D:\AcademyDesk-codex-p0`, `codex/penta-search`, starting HEAD `d33ee60bac9b011810e77349737ad58b1020b390`. Existing BUG-DATA-0008 / STUDENT-RULE-002 continuation, not a new audit or testing plan.

## Bounded change

The real form sends its optional studentNumber as an empty string. Previously the controller trimmed it but retained empty, so it participated in the filtered unique (AcademyId, StudentNumber) SQL index. The controller now maps null/blank/whitespace to null and still trims a supplied nonblank number. The unique index,50-character constraint, required name/DOB, optional credentials, guardian defaults, original rejection message, response flags, host authorization and shared domain/Identity/audit transaction are unchanged.

No schema migration, historic data rewrite, additional required field, frontend change, PENTA permission/tool or Mini contract change. Existing blank rows are not rewritten; new optional-null intakes coexist with them. This does not infer that all historical Azure onboarding failures have the same cause.

## Baseline reproduction

Baseline run `bdc8940bc4b14c88a18642e976756190`, port64320, uses the newly added strict regression with the unchanged d33ee60 controller. The first empty-number intake returned200; the second returned400 where200 was required. The harness fails immediately, before its pair's final canonical-null/delta checks. A separate read-only query against that exact owned SQL database confirmed one synthetic intake and one non-null empty student number. No baseline partial-account counts or complete rollback claim inferred from that query.

Baseline and fixed regression source SHA256 are identical: `0C03C29F416CD681ACE17F9ED655226C1339A7F79CF865EEA2BF82358D0B08E7`. Builds completed before execution; no overlapping builds on the same artifacts. Only the production normalization expression changed between those runs.

## Fixed native matrix

OnboardingNumbers run `44d44e74c20d4e97aa9e40e9380b2610`, port55720:29/29 real SQL/Identity/HTTP cases PASS:

- Twelve repeated adult intakes: omitted,null,empty,spaces,tabs/newlines and Unicode whitespace, two each. Every saved optional number is SQL NULL.
- Four blank/space minor intakes with required guardian details and both accounts. Correct full student/guardian/relation, account/role/tenant and truthful created flags verified.
- Five explicit-number controls: trimming, same-academy trimmed duplicate400, distinct number, identical number in another academy, that academy's own duplicate400. Index protections and isolation retained.
- Three denied controls: anonymous401, Teacher403, foreign-tenant route403; complete captured safe state unchanged.
- Existing51-character constraint400 with full no-write state, preserving the50-character SQL limit.
- Two new blank intakes alongside a synthetic legacy empty row; legacy value preserved, new values NULL.
- One simultaneous pair, blank and whitespace, both minor/full accounts:200/200, two complete chains, exact deltas and legacy row preserved. Two cases, not a stress campaign or explicit-number race claim.

Successful cases assert fresh exact student/guardian/link/audit/account/membership deltas, returned IDs, persisted number, original minor/guardian/optional-account semantics and actual Identity roles. Rejected cases compare full captured safe domain/Identity/audit snapshots, including roles, and exact existing400 message. Tokens/passwords/hashes never captured or printed. Live rate limiter unchanged; exact statuses exclude429.

## Retained regressions and validation

- OnboardingAccounts unchanged33/33 PASS, run `c36bfc051b3946f18e6cfe80a9a24ecd`, port62349: partial/blank/absent/complete credentials, exact flags/roles/deltas, no-guardian/minor defaults and rejected rollback.
- OnboardingAtomic unchanged21/21 PASS, run `f9e2c51ea6db4c12bbad5191707b2701`, port62365: SQL stage/audit/Identity-result failures, successful and optional-account intake, Owner/platform transaction opt-in and denied no-write.
-56 targeted unit tests PASS: AuditOutcomeTests, PeopleBranchValidationTests, StaffRoleBoundaryTests and AcademyIdentityTransactionTests. Not the full API or enterprise critical suite.
- Isolated SQL harness/API build0 warnings/0 errors; runner PowerShell parsing, scoped diff review/check and staged secret-pattern check PASS. Existing harness modules/assertions unchanged; new flag/module routing is additive.
- Three successful SQL runs verify health/auth/two-tenant controls,327 routes/316 controller method/routes/10 Identity routes, route SHA `0B0B59901F3F2AA521F16B0A139D2B91A816A300D661F7E4DD9CEB156FB877FC`, exact owned runtime target/login,88 application and7 Identity migrations, negative cleanup guard and terminal database/login teardown.
- Baseline container removed only after read-only SQL proof and exact name/run label/loopback checks. The failed harness did not reach terminal database/login cleanup; no retrospective success claimed. Each successful runner removes only its own verified container. Other QA/Mini containers untouched, no prune.
- Local receipt `QA/EVIDENCE/onboarding-numbers-44d44e74c20d4e97aa9e40e9380b2610/result.json` remains untracked. .build-check ignored;11 unrelated tracked files and32/28 shared continuity additions preserved. Existing dirty Mini snapshot participates in the host build but is not accepted or published by this batch.

Prior23 optional-date SQL and frontend handler/browser/typecheck/export results retained, not rerun; no dates/frontend changed. Linked browser-to-SQL, physical Android/iOS, broader explicit-number/concurrent/stale/cancellation/store-mismatch and full critical/release suites NOT RUN. Global QA consistency validator remains blocked by the inherited missing local observed-checks.json; not fabricated or represented as passing.

## Continuation / delivery boundary

BUG-DATA-0008 remains OPEN at RETEST disposition for wider acceptance; account/transaction issues remain OPEN with their retained bounded proofs. Original28 feedback-route checkpoints and E1–E10 preserved. Next: queued Student/Teacher onboarding pending/unknown-result/success feedback, Sol Medium for settled UI; then independent Manual/shared visual foundation and its outstanding FW1 return. Sol High remains appropriate for host security/finance/transaction/Mini integration or release decisions.

No Mini-owned dependency introduced; no project switch or new handoff request needed.90% eligible conversational AI/10% manual remains the product target, not measured readiness. Standing directive permits scoped feature commit/push only; no main edit/merge/push, Azure, production data/credentials or history rewrite.
