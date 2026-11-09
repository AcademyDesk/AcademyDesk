# C26 truthful provider setup and meeting availability — 9 November 2026

Cloud owner. CLAIMED / reproduce next.
Branch `codex/cloud-c26-provider-readiness-20261109`.
Clean base `a40c487e16135124ab7349b15255125bc3b47af6` (complete C25).
Latest Desktop6f77a94c528b6e6df0301b98c5ed75dcc76c6581 is already integrated.
No new competing provider-settings/meeting-page claim found in current notes.

MODEL: Sol High for capability/privacy boundaries; Medium for settled UX/docs.
TASK: fix FUNC-0024/0025's misleading provider capability claims without pretending
OAuth/meeting execution exists. Preserve metadata writes, stored flags, domain
permissions and future draft layout. External integration remains unfinished.

Exact mutation paths:

- `apps/web/src/app/communication-settings/page.tsx` — distinguish loaded metadata/recorded flags from verified provider setup
- `apps/web/src/components/meeting-provider-settings.tsx` — truthful metadata labels/copy
- `apps/web/src/app/meeting-links/page.tsx` — unavailable creation/history, guarded required reads; no provider execution
- `QA/tools/provider-readiness.test.cjs`
- `QA/tools/provider-readiness-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — execute actual meeting settings component for controlled tests
- `QA/ISSUES/BUG-FUNC-0024.md`
- `QA/ISSUES/BUG-FUNC-0025.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/AI_QUALIFICATION_ENTRY_GATE_2026-11-09.md` — add the real provider prerequisite
- `QA/HANDOFF/C26_PROVIDER_READINESS_2026-11-09.md`

Saving metadata must not promise a nonexistent next-step connection flow. A true
stored hasSecureConnection is a recorded flag, not a fresh provider verification.
Show channel-specific status only after valid complete reads. Meeting creation
is unavailable even with that flag: no real meeting/history endpoint or provider
transport exists here. Disable creation and guard programmatic submission; never
claim invitations sent or offer fake saved-history editing. Retain planned draft
layout and existing manual/calendar navigation. Hide ready zero meeting summaries
until required lookups finish; remove developer port advice and ignore late reads.

Validate original/final actual TSX (three providers, false/true/disabled/missing
records, blocked direct submission and metadata save payloads), real sandboxed
Chromium with synthetic APIs, exact-lock build/types/lint/full frontend and guard.
Reuse C25 API2,272 only after relevant source/test/assembly hash comparison.
No new API/provider/OAuth/secret/dependency/real external calls, permissions,
financial writers, SQL/migrations or shared CSS/vendor changes. Desktop owns
combined SQL/live provider/device/release acceptance. Keep all formal gates OPEN.
Publish feature source/results; current hosted CI is UNKNOWN. This is truthful
unavailable UX, not completed meeting/provider integration. Keep the explicit
stop before model loading/training/inference/activation;Mini/Plus/Pro unqualified,
Ultra deferred. No main merge, Azure or environment/public-access changes.
