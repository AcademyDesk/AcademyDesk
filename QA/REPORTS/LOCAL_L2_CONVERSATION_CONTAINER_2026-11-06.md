# Local L2 cloud conversation/container integration — 6 November 2026

Status: CLAIMED before edits. Local Codex owns this integration/verification
packet on codex/penta-host-integration-20261030. Base9dc2ccd, tracked clean;
raw QA/EVIDENCE stays local. Cloud returned C3.2a4ce05b/implementation34f3b46
and C4 8670605/implementation3cf250e. Preserve their bounded source changes,
cloud reports and all prior local host fixes; no wholesale preservation merge.

Exact implementation-owned source paths (integrate byte-identical cloud blobs):
- `QA/tools/check-web-container.sh`
- `QA/tools/class-media-client.test.cjs`
- `QA/tools/penta-entry-browser.cjs`
- `QA/tools/penta-entry.test.cjs`
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
- `QA/tools/refresh-concurrency.test.cjs`
- `QA/tools/session-cross-tab.test.cjs`
- `QA/tools/session-logout.test.cjs`
- `QA/tools/static-segment-export.test.cjs`
- `QA/tools/static-web-runtime.cjs`
- `apps/web/Dockerfile`
- `apps/web/nginx.static.conf`
- `apps/web/package.json`
- `apps/web/scripts/prepare-static-segments.mjs`
- `apps/web/src/app/login/page.tsx`
- `apps/web/src/app/penta/onboarding/page.tsx`
- `apps/web/src/app/penta/teachers/chat/page.tsx`
- `apps/web/src/app/penta/teachers/page.tsx`
- `apps/web/src/components/penta/onboarding-dialogue-workspace.tsx`
- `apps/web/src/components/penta/onboarding-operation-workspace.tsx`
- `apps/web/src/components/penta/onboarding-review-workspace.tsx`
- `apps/web/src/components/penta/onboarding-review.module.css`
- `apps/web/src/components/penta/onboarding-start-workspace.tsx`
- `apps/web/src/components/penta/role-penta-entry.tsx`
- `apps/web/src/components/penta/teacher-chat-workspace.tsx`
- `apps/web/src/components/penta/teacher-chat.module.css`
- `apps/web/src/components/penta/teacher-review-workspace.tsx`
- `apps/web/src/components/penta/teacher-review.module.css`
- `apps/web/src/components/penta/teacher-start-workspace.tsx`
- `apps/web/src/components/workspace-frame.tsx`
- `apps/web/src/lib/api.ts`
- `apps/web/src/lib/penta-chat-contract.ts`
- `apps/web/src/lib/penta-onboarding-channel.ts`
- `apps/web/src/lib/penta-onboarding-dialogue.ts`
- `apps/web/src/lib/penta-onboarding-operation.ts`
- `apps/web/src/lib/penta-onboarding-review.ts`
- `apps/web/src/lib/penta-onboarding-start.ts`
- `apps/web/src/lib/penta-teacher-dialogue.ts`
- `apps/web/src/lib/penta-teacher-operation.ts`
- `apps/web/src/lib/penta-teacher-start.ts`
- `apps/web/src/vendor/penta-operational/README.md`
- `apps/web/src/vendor/penta-operational/UPSTREAM.json`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/Workspace.tsx`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/index.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/projection.schema.json`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/types.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/validate.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/workspace.css`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/academy-mapping.css`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/contracts.ts`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/foundation.css`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/index.ts`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/primitives.tsx`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/tokens.ts`

Also copy cloud reports QA/HANDOFF/C3_CONVERSATION_2026-11-06.md and
QA/HANDOFF/C4_CONTAINER_READINESS_2026-11-06.md unchanged; shared status pointers
are additive integrator updates. This report records local outcomes only.

Local validation scope:
1. Verify restored/vendor source pins and all unchanged backend/API/SQL source.
2. Rebuild the existing SQL harness without new packages and run the real
   CommunicationRoundtrip module in an owned disposable SQL container, using
   cached SQL image. Preserve originals/services/lease/resource ownership.
3. Use its genuine synthetic-data HTTP/SQL receipt for the three previously
   missing communication-evidence frontend cases; no fixture/log invention.
   Run the combined current frontend suite with cached pinned TS/React tools.
4. Run the new static-segment regressions. If the existing cached QA web image
   contains usable Nginx, test the current config against owned synthetic static
   files in an isolated loopback container. This is component runtime proof, not
   a current production image build or actual89-page export acceptance.
5. Record exact-lock dependency/build/browser prerequisites honestly. No new
   packages/base-image download, model experiment, main merge, customer DB or
   Azure action. Source-bound1,984API result is reused only if backend/tests match.

No change to assertions, original evidence, model/role/tenant authority, approval
or audit requirements. Model qualification and candidate loading stop remain.
Next security/integration route Sol High; settled UI/docs route Sol Medium.
