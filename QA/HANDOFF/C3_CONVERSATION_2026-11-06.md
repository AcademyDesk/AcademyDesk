# Cloud C3.2 protected conversation/session packet — 6 November 2026

Status: CLAIMED / implementation and validation pending. Not production-qualified.

Base: `9dc2ccda5c6aca18d1cc33ec16976df827574533`, verified against live host feature branch. Cloud C3.1 is already integrated; do not reapply it. Source preservation reference: `6887e6eb26e88f8fc99eee7a00c267a14c12883f`; selectively restore only this protected UI dependency closure.

Cloud owns the following exact paths for this packet (Desktop retains all backend/API/SQL/migration work):

- `apps/web/src/app/penta/onboarding/page.tsx`
- `apps/web/src/app/penta/teachers/chat/page.tsx`
- `apps/web/src/app/penta/teachers/page.tsx`
- `apps/web/src/components/penta/onboarding-dialogue-workspace.tsx`
- `apps/web/src/components/penta/onboarding-operation-workspace.tsx`
- `apps/web/src/components/penta/onboarding-review-workspace.tsx`
- `apps/web/src/components/penta/onboarding-review.module.css`
- `apps/web/src/components/penta/onboarding-start-workspace.tsx`
- `apps/web/src/components/penta/teacher-chat-workspace.tsx`
- `apps/web/src/components/penta/teacher-chat.module.css`
- `apps/web/src/components/penta/teacher-review-workspace.tsx`
- `apps/web/src/components/penta/teacher-review.module.css`
- `apps/web/src/components/penta/teacher-start-workspace.tsx`
- `apps/web/src/lib/penta-chat-contract.ts`
- `apps/web/src/lib/penta-onboarding-channel.ts`
- `apps/web/src/lib/penta-onboarding-dialogue.ts`
- `apps/web/src/lib/penta-onboarding-operation.ts`
- `apps/web/src/lib/penta-onboarding-review.ts`
- `apps/web/src/lib/penta-onboarding-start.ts`
- `apps/web/src/lib/penta-teacher-dialogue.ts`
- `apps/web/src/lib/penta-teacher-operation.ts`
- `apps/web/src/lib/penta-teacher-start.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/Workspace.tsx`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/index.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/projection.schema.json`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/types.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/validate.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/workspace.css`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/contracts.ts`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/index.ts`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/primitives.tsx`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/tokens.ts`
- `QA/tools/penta-onboarding-channel.test.cjs`
- `QA/tools/penta-onboarding-dialogue.test.cjs`
- `QA/tools/penta-onboarding-expiry.test.cjs`
- `QA/tools/penta-onboarding-idle-expiry.test.cjs`
- `QA/tools/penta-onboarding-operation.test.cjs`
- `QA/tools/penta-onboarding-recovery.test.cjs`
- `QA/tools/penta-onboarding-review.test.cjs`
- `QA/tools/penta-onboarding-session.test.cjs`
- `QA/tools/penta-onboarding-start.test.cjs`
- `QA/tools/penta-onboarding-terminal.test.cjs`
- `QA/tools/penta-teacher-dialogue.test.cjs`
- `QA/tools/penta-teacher-operation.test.cjs`
- `QA/tools/penta-teacher-start.test.cjs`
- `apps/web/src/lib/api.ts`
- `apps/web/src/app/login/page.tsx`
- `apps/web/src/components/workspace-frame.tsx`
- `apps/web/src/components/penta/role-penta-entry.tsx`
- `QA/tools/penta-entry.test.cjs`
- `QA/tools/penta-entry-browser.cjs`
- `QA/tools/session-logout.test.cjs`
- `QA/tools/session-refresh.test.cjs`
- `QA/tools/session-refresh-concurrency.test.cjs`
- `QA/tools/session-epoch.test.cjs`
- `AI_HANDOFF.md`

This report is also owned by this packet. Shared AI_HANDOFF update will be a short additive task pointer, for the integrator to reconcile; no other shared governance edits.

Scope: explicit-login identity and private UI invalidation, authenticated protected student/teacher draft/review/approval/execute/receipt UI; preserve real server authority, readiness/qualification gates and Manual fallback. No model acquisition/loading/inference/training/activation, no backend changes, no packages, main merge or Azure deployment.

Validation: existing and preserved session/operation/review unit tests, exact-installed-lock TypeScript/lint/build, controlled real Chromium browser checks and full frontend regression with genuine evidence prerequisites kept explicit. Synthetic transport tests do not qualify live Identity/SQL/model/production operation.
