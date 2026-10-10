# Enrollments + Batch Promotions feedback — 2026-10-10

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting commit `c0d348a1592a2946b398639e8fcb17c2e2d5d359`. Existing E2 queue/BUG-FUNC-0003 continues; not a new audit/test plan or enterprise closure.

## Bounded implementation

- Enrollment create/status and promotion create/approve/reject retain success after refreshing their registers.
- A confirmed write followed by failed readback shows success with a refresh warning and asks users not to repeat the write. Network/5xx is an unconfirmed result, preserves drafts and asks users to check the register before retrying. No automatic write retries.
- One synchronous ref per page guards all write/readback handlers; pending forms and row actions are disabled. Malformed/non-OK/empty initial workspace loads do not enable writes. Reads are uncached and refresh the captured academy without reselection.
- Enrollment successful creation clears only the start-date draft; student, batch and initial status are preserved. Optional start date remains null, duplicate409 remains understandable, status/endDate body remains unchanged. Query-selected student now resolves during the initial effect, preserving the known-ID/fallback behavior without render-time window access.
- Promotion creation preserves learner/source/notes and clears target/effective date only after confirmed creation, matching prior behavior. Empty create notes remain null; optional approval note remains empty string; rejection requires a nonblank reason. Active source and different-target option filters remain unchanged.
- Promotion prompt cancellation now returns without a decision request or notice change. Prior `prompt(...) ?? ""` could turn approval cancellation into a real approval; this narrow UI safeguard does not alter backend decision authority, terminal state or SQL transaction policy.
- Polite status regions announce authoritative feedback. Manual forms remain available independently of PENTA.

## Known separate workflow gap — do not claim acceptance

[BUG-FUNC-0016](../ISSUES/BUG-FUNC-0016.md) remains OPEN: enrollment UI sends status/endDate only, while the real API requires `LifecycleReason` for non-Active updates. This batch preserves that request rather than silently relaxing server validation or bundling a lifecycle-contract change. The UI now surfaces the backend reason instead of a generic error. Synthetic successful non-Active updates test feedback only, **not real lifecycle success**. Next domain-linked reason input/confirmation and SQL/browser tests are a separate bounded task. Retain [promotion terminal integrity](PHASE_2B_PROMOTION_REPAIR.md) and existing lifecycle safeguards; no backend/API/schema changes or fresh SQL retest here.

## Evidence

- Actual-handler runner `QA/tools/enrollment-promotion-feedback.test.cjs`:59/59 PASS; frozen starting source under same assertions5 PASS/54 FAIL. Assertions are not54 distinct defects. Includes the actual existing API lifecycle-reason400 wording, with unchanged status/endDate payload and no false success.
- Five actions each test exact payload/reset/success, confirmed-save/readback failure,400/403/500/503, network uncertainty and duplicate/opposing stale handlers during both pending stages. Initial workspace failure, prompt cancellation/required rejection/optional approval,409 duplicate and null optional date controls included.
- Initial harness run failed12 checks because it treated native fieldset-disabled children as explicitly disabled and tried to invoke nonexistent register actions in empty-workspace fixtures. Corrected ancestor-aware assertions and create-only unavailable-workspace action, without weakening domain or existing runners.
- Target ESLint:0 errors/0 warnings, installed TypeScript `--noEmit` PASS, production83-page export PASS. An ESLint invocation from repository root failed config discovery; correct app-directory command is authoritative. Whitespace-only diff issues corrected.
- Responsive synthetic browser runner:80/80 PASS (five actions ×320/1440px ×light/dark ×normal/confirmed-save-readback503/rejected400/uncertain-but-committed500). Exact request and draft semantics, polite notice, no document overflow and no unexpected console/page errors asserted; two mobile screenshots visually inspected. Source selection may become unavailable after approved promotion completes its enrollment; hidden draft value remains retained, backend remains authoritative.
- Local/untracked evidence: `QA/EVIDENCE/enrollment-promotion-feedback-browser-1791637784275/result.json` and80 screenshots. Runner closed browser contexts/server; no real backend or SQL container created.

## Limits / continuation

No SQL containers, live academy data, Identity/RBAC/tenant integration, real model inference, physical devices, complete enrollment/promotion workflows or enterprise/release acceptance. Static exported app runs on a disposable loopback server with intercepted synthetic APIs. Local evidence and unrelated Mini/continuity work preserved. No main merge or Azure deployment.

After bounded browser verification: feedback queue27/28 checkpointed, Assessments creation remaining. BUG-FUNC-0003 and BUG-FUNC-0016 remain OPEN. Next Sol Medium Assessments creation; then schedule the known enrollment-reason integration separately, escalating real authority/transaction choices to High. Financial FP1/FP2/FP3 unapproved; wider E0–E10/AI/shared frontend/enterprise gates retained in [progress tracker](../../ACADEMY_PROGRESS_CHECKLIST.md). No Mini project switch required for this packet.
