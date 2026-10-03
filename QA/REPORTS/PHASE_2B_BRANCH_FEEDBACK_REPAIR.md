# Phase 2B — Branches save and refresh feedback

Historical checkpoint. The next bounded [address/postcode preservation repair](PHASE_2B_BRANCH_ADDRESS_REPAIR.md) now supersedes the current-source fingerprint. This report's original results and snapshots are retained, not rerun/closure claims.

2026-10-01. Bounded frontend-only local repair: 88/88 controlled frontend checks, TypeScript and targeted Branches lint PASS. Agreed Sol High; accepted BUG-FUNC-0003 reused, not a repeat Astra audit. No API/permissions/module/DTO/schema/SQL/Azure/Blob change, no commit/push. BUG-FUNC-0003 and Phase 2B remain OPEN for other forms and live browser/device/critical acceptance.

## Change

[Branches page](../../apps/web/src/app/branches/page.tsx) keeps create/edit/deactivate/reactivate confirmations through list readback. A confirmed save followed by HTTP/network/JSON refresh failure retains success and adds an explicit refresh warning. Successful create clears controlled fields only after confirmation; successful edit closes the editor. Non-successful writes keep entered details and the editor; `finally` releases pending controls. Network and 5xx writes report **not confirmed**, advise checking the branch list before retry, and never automatically repeat a potentially committed write.

A shared in-flight ref prevents duplicate/opposing writes even before React renders pending state. All mutation buttons, form inputs and the academy selector are disabled through both save and refresh. It is an in-page interaction guard, not server idempotency or cross-tab/concurrent-edit protection. Existing branch POST/PUT payloads and active-state policy are unchanged; no `event.currentTarget.reset()` change is needed because these inputs are already controlled.

The loading effects now await read-only fetches before state updates and ignore completions after their cleanup. This also passes current hook lint without suppression. Catalogue loading preserves an existing academy selection through a functional setter. Initial/read failures still show explicit errors. The targeted tests cover cleanup, not all overlapping same-academy/background GET races or real React StrictMode.

## Evidence and limits

- [Authoritative controlled baseline](../EVIDENCE/logs/phase-2b-branch-feedback-before-client.log): 15 cases, 8 PASS/7 FAIL. Four successful action confirmations were erased by reload; three pending duplicate checks sent two requests. Definite rejection/blank-input controls pass before repair. Baseline mode excludes uncaught network/refresh-error paths that old void click handlers cannot expose as promises; these are exercised after repair. [Initial exploratory baseline](../EVIDENCE/logs/phase-2b-branch-feedback-before-client-initial.log) is retained but excluded: it confused untouched initial state with undefined setter writes and emitted async rejection warnings. The corrected baseline ran before editing the application.
- [Current controlled frontend](../EVIDENCE/logs/phase-2b-branch-feedback-client.log): 88/88 = 45 Branches + previous 43 Finance controls. Branches covers 16 confirmed-success/readback cases (four actions × successful/500/network/JSON refresh); 12 rejected/unconfirmed cases (create/edit/toggle ×400/403/500/network); three duplicate/opposing/pending-lock cases; two blank-input controls; three pending-refresh cases; three explicit retry-after-definite-rejection cases; six catalogue/list/read-rejection/effect-cleanup cases. Actual TSX handlers run with controlled hooks/API; this is not browser/device/SQL evidence.
- [TypeScript](../EVIDENCE/logs/phase-2b-branch-feedback-typecheck.log) `--noEmit --incremental false` PASS. [Targeted lint](../EVIDENCE/logs/phase-2b-branch-feedback-lint.log) PASS, no suppression. No full frontend production build, browser/mobile screenshots or new API/SQL test run is claimed.
- [Before application hash](PHASE_2B_BRANCH_FEEDBACK_BEFORE_SOURCE_SNAPSHOT.json) pins the one changed application file. [Current checkpoint](PHASE_2B_BRANCH_FEEDBACK_SOURCE_SNAPSHOT.json) has 126 captures: all 123 prior sources and the API/harness assemblies unchanged, three additional captures (Branches page/test/validator). Previous API 222/222 and SQL invoice-settings ×34 are historical retained evidence, not rerun counts for this task. [Validator](../tools/validate-branch-feedback.cjs) and [validation log](../EVIDENCE/logs/phase-2b-branch-feedback-validation.log) check hashes, current evidence and OPEN issue status.

No new QA server/container/database or duplicate development stack was created. Existing normal local development services/data and unrelated dirty work were preserved.

## Remaining and next

BUG-FUNC-0003 remains OPEN: only Governance and this Branches feedback variant have local repairs, not all listed forms. Live React/browser/mobile/assistive-tech, all linked form cases and full critical acceptance are pending. Request loss, server idempotency, cross-tab conflicts and stale/background read races remain broader gates. Do not infer a persisted branch from a controlled mocked success response.

The accepted [BUG-DATA-0025](../ISSUES/BUG-DATA-0025.md) is the next bounded repair: existing Branches edit/status PUT payloads omit stored address/postcode fields. This task intentionally retains those contracts and does not certify full-record preservation. Use Sol High to repair/verify that accepted finding next; no renewed audit and no inactive-branch policy change. Other policy/finance/platform/media/visual/release gates remain separate.
