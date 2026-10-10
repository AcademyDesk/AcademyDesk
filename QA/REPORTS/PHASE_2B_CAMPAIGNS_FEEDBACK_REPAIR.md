# Sales Campaigns feedback repair — 2026-10-10

Starting HEAD `9b0a492d9572a6baeb0ee7dda75bbd033699ec25`, `codex/penta-search`, `D:\AcademyDesk-codex-p0`. Bounded original [BUG-FUNC-0003](../ISSUES/BUG-FUNC-0003.md) continuation, not a new audit/testing plan. Issue stays OPEN.

## Scope and preserved contracts

- [Page](../../apps/web/src/app/sales-campaigns/page.tsx): create/status durable positive notices; separate confirmed-write/failed-readback warning; network/5xx explicitly unconfirmed with register-check-before-retry guidance. No automatic retry. Failed drafts and unrelated create drafts survive status changes. Only confirmed create clears name/budget, retaining channel/start date/status as before.
- Synchronous shared pending ref guards stale same-tick duplicate/opposing handlers through write/readback; visible controls disabled and saving state released on all outcomes. Native fieldset and explicit custom-select disabling, polite status notice. Checked initial academy/list reads and effect cleanup; unusable initial workspace remains disabled. Generic load guidance does not speculate that restart is required.
- Original POST name whitespace/channel/startDate/status/`Number(budget || 0)` retained; PATCH status/original budget/`endDate || null` retained. No API/DTO/database/authorization/lifecycle/budget/currency/date-control/PENTA/security changes. Browser layout retained, no new permission or campaign execution claim.

## Verification

- [Actual-handler test](../tools/campaigns-feedback.test.cjs):26/26 PASS. Durable notice, exact payload, blank budget0, end-date null/non-null, matching reset, readback failure, HTTP400/403/500/503/network, same-tick duplicate/opposing guards at write/readback, initial unavailable/malformed workspace and no effect-repeat reads. Frozen starting HEAD under same assertions:3 PASS/23 FAIL, assertion counts not23 unique defects.
- [Exported browser runner](../tools/campaigns-feedback-browser.cjs):32/32 PASS, 320/1440px × light/dark × create/status × normal/readback503/rejected400/uncertain-but-fixture-committed500. Exact synthetic payload/write counts, retained/reset fields, date/channel/status preservation, polite notice, zero unexpected console/hydration errors or horizontal overflow. Full screenshots; mobile normal-create screenshot visually reviewed. Receipt local `QA/EVIDENCE/campaigns-feedback-browser-1791631262650/result.json`.
- Target ESLint0 errors/0 warnings, installed web TypeScript `--noEmit` PASS, webpack production export83/83 PASS. Initial handler fixture21/26 failed because unavailable workspace legitimately has no row; corrected test checks absence/no write rather than invoking a nonexistent row. First root `npx tsc` resolved non-TypeScript command and failed; installed `apps/web/node_modules/typescript/bin/tsc` rerun PASS. No dependency/application change for these tooling corrections.
- Not run: fresh live SQL/campaign lifecycle/auth/tenant tests, physical iOS/Android, screen-reader acceptance, concurrency across tabs/users, audit-failure or full enterprise suite. Synthetic transport is not server authorization/integrity proof. Existing accepted domain/security tests retained, not weakened/rerun. This packet does not establish production readiness.

## Continuity

[Original queue](PHASE_2B_FEEDBACK_ROUTE_RECONCILIATION.md):21 feedback checkpoints/7 route-level gaps, not total app tasks or percentage. Next `/trial-bookings`, **Sol Medium**, Academy Desk; preserve accepted duration repair and separate Sales lookup-authority gate. Escalate to High only for genuine domain/security/transaction/privacy decisions. Existing Mini86/94 and enterprise/release gates OPEN. Preserve unrelated32/28 continuity additions/Mini dirty work/local evidence/ignored `.build-check`/main, no Azure or Mini project changes. Publish only this8-file UI/QA/continuity packet on feature branch.
