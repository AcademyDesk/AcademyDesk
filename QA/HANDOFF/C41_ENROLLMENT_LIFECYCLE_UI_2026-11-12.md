# C41 enrollment lifecycle UI — 12 November 2026

OWNER:Cloud Codex. MODEL:Sol High for session/approval contracts. TOOL:Codex.
REPOSITORY:AcademyDesk/AcademyDesk. ENVIRONMENT:managed Linux cloud.
BRANCH:codex/cloud-c41-enrollment-lifecycle-ui-20261112.
BASE:27291e211fb2753a321c8715c2aefce946d63858 (published C40).
STATUS:CLAIMED. Latest Desktop read-only tip83452a129c1a7315922dbeeaf1b50de3d62cbdac.
Desktop owns live C39/C40 SQL/browser qualification. Shared trackers stay untouched.

TASK:separate /penta/enrollments/manage screen for bounded verified source lookup,
status/transfer exact canonical review,tool-specific approval,explicit execution,
verified receipt and GET-only same-request recovery. Separate lifecycle purpose
and five-key scoped recovery pointer; no C39 create-session reuse or model calls.
Target batches come only from current protected batch-search results.

MAY MODIFY (exact7 paths):
- apps/web/src/lib/penta-enrollment-lifecycle.ts (new)
- apps/web/src/components/penta/enrollment-lifecycle.tsx (new)
- apps/web/src/app/penta/enrollments/manage/page.tsx (new)
- apps/web/src/components/penta/enrollment-assistant.tsx (navigation link only)
- QA/tools/penta-enrollment-lifecycle.test.cjs (new)
- QA/tools/penta-enrollment-lifecycle-browser.cjs (new)
- QA/HANDOFF/C41_ENROLLMENT_LIFECYCLE_UI_2026-11-12.md (this handoff)

MUST NOT MODIFY:API/SQL/migrations/domain/security/permissions,Desktop finance or
native work,root shared trackers/handoffs,C39 creation/lib/component/UTC fix,
existing assertions/raw evidence,Mini/vendor/lockfiles/dependencies/environment.
No main merge,Azure deployment,environment publication/replacement/deletion,
public internet enablement,workflow/PR dispatch,model loading/download/inference/
training/activation. Mini/Plus/Pro unqualified;Ultra deferred;Manual retained.

VALIDATE:strict runtime parsing/source and target binding/per-tool intent,
receipt consistency,read-only interrupted recovery,stale callback/session/storage
protection;actual native Chromium desktop/mobile synthetic protocol checks,
TypeScript/lint/exact-lock web build/frontend regression;retain genuine missing
SQL receipt failures and all release blockers. Source-bound backend C40 reused.
STOP WHEN:bounded source and sanitized handoff feature-pushed;remote SHA verified.
NEXT ROUTE:Desktop Sol High for native SQL/live browser acceptance;Cloud follow-up
from remaining outcome backlog,without activating models. Production NOT QUALIFIED.
