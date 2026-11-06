# Cloud C3.2 protected conversation/session packet — 6 November 2026

Status: SOURCE IMPLEMENTATION COMPLETE / controlled validation passed except three existing evidence prerequisites. Not production-qualified.

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
- `QA/tools/session-cross-tab.test.cjs`
- `AI_HANDOFF.md`

- `apps/web/src/vendor/penta-operational/README.md`

- `apps/web/src/vendor/penta-operational/UPSTREAM.json`

- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/academy-mapping.css`

- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/foundation.css`

- `QA/tools/refresh-concurrency.test.cjs`
- `QA/tools/class-media-client.test.cjs`

This report is also owned by this packet. Shared AI_HANDOFF update will be a short additive task pointer, for the integrator to reconcile; no other shared governance edits.

Scope: explicit-login identity and private UI invalidation, authenticated protected student/teacher draft/review/approval/execute/receipt UI; preserve real server authority, readiness/qualification gates and Manual fallback. No model acquisition/loading/inference/training/activation, no backend changes, no packages, main merge or Azure deployment.

Validation: existing and preserved session/operation/review unit tests, exact-installed-lock TypeScript/lint/build, controlled real Chromium browser checks and full frontend regression with genuine evidence prerequisites kept explicit. Synthetic transport tests do not qualify live Identity/SQL/model/production operation.

## Returned source and behavior

Implementation: `34f3b46352e65a9f6624a07e1a7e56bc1ea2e9fa` on
`codex/cloud-c3-conversation-20261106`, based on verified shared host `9dc2ccd`.
The implementation changes 59 files. Desktop can integrate that bounded commit
and this report; do not wholesale import the older preservation branch.

- Restore three real protected routes: `/penta/onboarding`, `/penta/teachers/chat`
  and `/penta/teachers`. All 32 restored code dependency paths are byte-exact to
  preservation `6887e6e`. Twelve immutable vendor files retain their pinned
  normalized hashes; UPSTREAM metadata remains unchanged. No gallery/fixture
  bootstrap, package, backend/controller/DI/SQL/migration/workflow edit.
- Verified AcademyAdmin entry offers links to these protected workflows. Other
  role entries retain honest conversation-unavailable and usable Manual links.
  Navigating a workflow only performs its authenticated reads/readiness checks;
  unavailable, denied or invalid host responses cannot enable a draft or action.
- Existing student adapters verify account, current cursor, payload SHA-256,
  revision, actor/tenant/connection/tool and expiry before rendering private
  fields. Explicit synthetic-test conversation availability is separate from
  passive review. Approval and execution are separate gestures. An unknown write
  can only reconcile the same operation; no automatic POST replay. Saved success
  requires the bound host receipt and audit.
- Existing teacher adapters separately review, approve and create a teacher,
  check payload/revision/approval and require saved receipt/audit/source IDs.
  Prompt text never enters URL/localStorage; opaque request lookup keys support
  explicit GET recovery. Defaults and model/host gates are unchanged.
- Explicit verified login now begins an opaque per-workspace login epoch before
  storing tokens; renewal retains it. Logout, same-document replacement, peer
  logout and resumed pages invalidate old UI. Requests check the epoch even if
  identical token strings return and no event was delivered. The marker is never
  server authorization. Supported browsers serialize same-workspace renewal
  through native Web Locks with bounded acquisition; another role is independent.
- Normal renewal preserves the workspace boundary and role entry. Protected
  draft channels intentionally retain their stricter bearer/epoch fences: a token
  change requires a fresh host check rather than silently transferring an old
  approval or uncertain operation to renewed credentials.

## Actual validation on this implementation

| Check | Result and boundary |
| --- | --- |
| Installed pinned Next16.4.3 production build, webpack | PASS; 89 static pages; QA loopback API URL, local artifact only, no deployment |
| TypeScript `tsc --noEmit` | PASS |
| ESLint | PASS exit0; 0 errors / 63 existing warnings; not a warning-free claim |
| Full current frontend runner | **1,880 tests: 1,877 PASS, 3 FAIL, 0 skipped/cancelled**, 67 files |
| Restored student/teacher contracts | 576 PASS across 13 restored groups; real source/validator, controlled transport |
| Routing/login / refresh / refresh concurrency | 93 / 37 / 20 PASS |
| Logout/epoch and separate-realm Web Locks VM | 46 / 31 PASS |
| Real Chromium/React/DOM | **24 groups PASS**, no runtime/console exceptions; all API responses intercepted |
| Original vendor contract | All 12 normalized pins PASS; 32 restored code files byte-exact to original source |
| Backend source overlap | No backend/API/SQL/DI/migration change; no live backend/SQL acceptance rerun |
| GitHub Actions status query | BLOCKED: actual `gh run list` to api.github.com returned Forbidden; CI conclusion remains UNKNOWN |

The three frontend failures are the existing Notice/Announcement/Meeting receipt
checks in `communication-drafts.test.cjs`, requiring genuine
`QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`. They are not skipped,
rewritten or replaced with synthetic evidence. The baseline was1242PASS/3FAIL;
635 added cases all pass. Legacy restored test collectors now publish native
node:test counts while retaining every original assertion and JSON observation.
Two existing transport mocks were extended for browser logout events. No pass
was manufactured by reducing an assertion or overriding expected vendor hashes.

Chromium covers eight role/viewport/theme groups, auth denial/unavailability,
late-session recheck, Manual form leave/Stay/Escape, explicit epoch renewal/
replacement/other-role isolation, two actual browser module realms sharing one
refresh through **native** Web Locks, four disabled-host workflow landings,
a verified passive student projection followed by private-UI removal, and two
teacher review→approval→execution→receipt flows. The second teacher response is
lost after the simulated commit: the UI checks the saved result with GET and
executes once. There are exactly six intercepted synthetic teacher mutations,
zero live business writes and zero model calls. These tests do not qualify real
Identity/SQL, a live protected host, provider, model or physical device.

Artifacts retained at `/workspace/c3-conversation-20261106`: final build/lint logs,
full frontend summary, browser receipt, preserved source hashes and earlier failed
attempts. Detailed per-file logs remain in `.build-check/frontend-tests`; browser
receipt/screenshots in `.build-check/penta-entry-browser`. Every earlier browser
receipt is preserved before a new RUNNING/PASS/FAIL receipt. No raw laptop evidence,
credentials or downloaded model material is committed.

Normal staged whitespace check reports six blank EOF lines in immutable pinned
shared-foundation files. These original bytes are deliberately preserved; the
check with only `blank-at-eof` excluded passes. No other whitespace issue is waived.

## Prioritized acceptance and handoff

1. **Desktop L2:** integrate only reviewed C3.2 changes into a fresh shared feature
   checkpoint; run actual isolated current-role/session/Manual browser and protected
   student/teacher HTTP/SQL roundtrips with matching source and genuine receipts.
   Keep customer data and original private evidence local. Previous Desktop API
   **1984PASS** at the shared source is reused evidence, not a cloud rerun or proof
   that every backend function currently works.
2. **Cloud C4:** finish dependency/security audit, restore/review the actual
   production Dockerfile and validate its exact-lock container path, then read
   actual GitHub CI once API access is available. This local static production
   build does not close the Docker/runtime/CI/release gate. Do not install packages
   or change network/public-access settings under this packet.
3. **Cloud C5:** resolve the two strict Linux original-parent/source provenance
   failures by an explicit versioned binding, or retain Windows-only original
   review. Never rewrite originals, expected hashes or authority normalization.
4. Continue the existing all-role/workflow, privacy/tenant/security/concurrency,
   device/P0, DB/restore, staging and release gates. C2's359HTTP declarations and
   285Manual candidates remain static mapping, not measured conversational outcome
   coverage. The90%AI/10%Manual target has not been demonstrated.

Mini/Plus/Pro remain **NOT production-qualified**; Ultra is deferred. Preserve the
stop before candidate loading/training/inference/activation. Main/Azure/customer
DB and environment configuration were untouched. Next route: Sol High for
security/contracts/integration; settled docs/visual work may use Sol Medium.
