# Batch 2 — communications Manual presentation

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. Bounded E3 presentation continuation, not full communications or enterprise acceptance.

## Delivered

- Messages, message templates and contact preferences use a scoped PENTA stylesheet: semantic light/dark surfaces, responsive spacing, wrapped content, persistent field labels and minimum 44px action targets.
- Shared selectors now have their own visible labels rather than interactive controls nested inside native labels. Recipient, channel, template, audience and contact choices retain their existing semantics and collapse after selection.
- Fifteen pre-edit function hashes preserve existing domain handlers. No sending, consent, authorization, entitlement, template eligibility, scheduling, payload or backend policy changes.

## Verification

- Production webpack build, TypeScript and 86-page static export PASS. No application-source changes after that build.
- 141 focused node:test entries PASS, including 15 declaration-preservation checks and existing channel/template compatibility, consent, pending submission and outcome cases. CSS module mocks updated; the older template-state harness now supplies the existing stable pending ref. No behavioral assertions weakened.
- 118 synthetic browser cases PASS: 54 new layout/state cases across 320/768/1440 widths, light/dark and normal/empty/denied states; 32 existing communications outcome cases; 32 existing preference outcome cases. The new normal template path verifies an exact creation payload and saved register. Consent and template/channel switching are exercised without contacting a delivery provider.
- Local receipts: `QA/EVIDENCE/engagement-visual-1792055079772/result.json` (54), `QA/EVIDENCE/communications-feedback-browser-1792055006763/result.json` (32), `QA/EVIDENCE/preferences-feedback-browser-1792055017084/result.json` (32). These used the same application sources as the final repeated build. New matrix records source hashes. Phone screenshots inspected for messages, templates and consent; no document overflow or page errors, visible action targets checked.
- Scoped ESLint: zero errors, one template useEffect dependency warning also reproduced from HEAD. No suppression or unrelated effect rewrite. Pinned vendor contract: six files PASS.

## Limits and next work

Synthetic fixtures do not prove real delivery, SQL, authentication, tenant isolation or physical-device acceptance. Queued is not delivered. Existing mutation/readiness/context/unknown-outcome gaps are not closed by styling. In particular template creation and starter import still need synchronous pending guards, truthful confirmed-write/readback outcomes and uncertain-outcome recovery; messages/preferences retain their previously documented recovery limits. Communication settings is not included in this three-screen slice.

Next: Sol High for bounded template creation/starter-import recovery, then Sol Medium for remaining communication settings and portal presentation. Keep existing real SQL/API/RBAC/tenant/device/PENTA UI2/Mini/domain conversation/E9–E10 gates OPEN. No new engine contract or project switch required.

No backend/schema/Mini/vendor/main/staging/commit/push/Azure/production-data/container changes. Existing dirty work and QA evidence preserved. Owner sequence remains full agreed PENTA+Manual app → authorized go-live → Hayansh onboarding → Academy Desk product website after the first customer is live. The 90% AI objective remains a target, not measured completion.
