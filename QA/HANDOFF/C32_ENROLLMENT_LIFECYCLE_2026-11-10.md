# C32 enrollment lifecycle reason and safe form recovery — 10 November 2026

MODEL: Sol High for intent/recovery/authority checks; Medium for settled UI/docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c32-enrollment-lifecycle-20261110.
BASE: cc1bae0970f9197e08f8bbd5aa46390e120b5282 (clean published C31).
DESKTOP:4f458f9011fdfa32426a733fd1b4055bcfabe2e8 imported with provenance;
Desktop retains protected finance collection/reconciliation reads. No overlap.
STATUS: CLAIMED; reproduce original actual TSX/HTTP/browser behavior before repair.
TASK: BUG-FUNC-0016 missing lifecycle reason; enrollment subset of0012 busy/error
recovery. Complete the existing Manual page; do not change admission/domain rules.

Exact MAY MODIFY paths (9):

- `apps/web/src/app/enrollments/page.tsx`
- `QA/tools/enrollment-lifecycle.test.cjs`
- `QA/tools/enrollment-lifecycle-browser.cjs`
- `tests/AcademyDesk.Api.Tests/EnrollmentLifecycleAccessTests.cs`
- `QA/ISSUES/BUG-FUNC-0016.md`
- `QA/ISSUES/BUG-FUNC-0012.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C32_ENROLLMENT_LIFECYCLE_2026-11-10.md`

MUST NOT MODIFY: API/business/host/SQL/migrations/permission/session logic,
finance/scheduling/attendance, globals/vendor/Mini, existing tests/evidence or
unrelated work. No broad lookup permissions, new backend concurrency contract,
whole-file replacement of newer Desktop source, main/Azure/environment publication,
public access, model download/loading/inference/training/activation. Mini/Plus/Pro
unqualified; Ultra deferred. Production/90% conversational outcome acceptance open.

Policy: choosing a lifecycle status opens an explicit review; send no mutation
until confirmation. Collect a trimmed required reason for every non-Active target,
show exact student/batch/current/requested status and preserve the existing endDate
contract (Active clears it). Cancel/blank/same-state/invalid/stale UI sends nothing.
Creation retains Active/Waitlisted and server admission authority. Use immediate
pending/intent guards, finally cleanup, mounted/read-generation checks. Keep
existing data/drafts on failures; show useful server denial text. Distinguish
confirmed save with failed readback from unconfirmed transport/5xx/malformed
receipt; recovery GET never replays writes. After uncertain result require fresh
records and explicit review/new intent before another attempt. Empty/loading/failed
lookups must not look like an authoritative empty enrollment register. Minimal
scoped lookups for delegated operational roles remain a separate0018 packet.
No unsupported server optimistic-lock promise: a pre-submit read can refresh the
review but only the existing server rules arbitrate concurrent business writes.

VALIDATE: original/final actual TSX, full HTTP/Identity/InMemory contract with all
six offered states and required/no-write/authority guards; native sandboxed Chromium
with synthetic API at320/1440, real reason confirmation/cancel/fault/readback and
retained drafts; exact-lock types/build/lint/full frontend/API and release guard.
Capture failures outside Git; no fabricated SQL receipts or weakened assertions.
STOP WHEN: bounded source, checks, exact-source handoff and feature publication
verified. Issue closure/native SQL/device/release acceptance remains OPEN.
NEXT ROUTE: Desktop Sol High for actual SQL/Identity/browser/device acceptance;
Cloud next separately claimed non-AI gap. Model-loading stop remains.
