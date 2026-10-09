# Attendance feedback gap check — 2026-10-09

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD
`fdcaeb25e7ee66b2b3dbfda94da034d1d204d016`. Existing enterprise continuation.

## Reuse, not reimplementation

The earlier Attendance-note repair already supplies durable success, failed-refresh
guidance, session-scoped draft preservation and synchronous pending protection.
All original 24 controlled checks passed before this slice; that work was NOT redone.
`QA/EVIDENCE/attendance-feedback-baseline-20261009.log`.

One narrow gap remained: 5xx writes could imply definite failure, even when the
synthetic transport had committed. Three production lines now append unconfirmed
outcome/check-before-retry guidance to 5xx responses, preserving server guidance,
drafts and unchanged 4xx messages. No automatic retry, new endpoint, payload,
note/default/status/enrollment/permission/timezone or successful-save change.

## Validation

- Added eight controlled cases: 500/502/503, committed and uncommitted outcomes,
  malformed 5xx body and pending readback protection. Original 24 cases retained;
  four old 5xx expectations strengthened for the intentional outcome guidance.
- Final frozen 32-case baseline against HEAD: 21 PASS / 11 FAIL, zero skipped or
  cancelled. `QA/EVIDENCE/attendance-feedback-final-baseline-20261009.log`.
  Earlier expanded baseline retained; the new readback case was strengthened from
  an inadequate preliminary fixture to a real held-after-write gate.
- Final actual TSX: 32/32 PASS, including note limits/null/clear/Unicode, other-session
  isolation, server guidance, duplicate guards, confirmed success and refresh failure.
  `QA/EVIDENCE/attendance-feedback-final-20261009.log`.
- Exported UI: 16/16 PASS, 320/1440 light/dark, success/failed refresh/400/committed
  500. One exact POST with original student/status/notes, saved/rejected draft display,
  restored controls, no horizontal overflow and no unexpected JS/hydration errors.
  Expected 400/500/503 resource errors counted separately. Reviewed 320 light screenshot.
  `QA/EVIDENCE/attendance-feedback-browser-1791565928099`.
  Synthetic API transport, NOT live SQL/Identity or physical-device acceptance.
- Production webpack 83-page export and TypeScript PASS.
- Target lint FAIL: one inherited set-state-in-effect error and two exhaustive-deps
  warnings. Independently linted HEAD source has the exact same rules/counts. No
  suppressions or broad hook cleanup. This remains an explicit next task/release gate.

Previous 16-case AttendanceNotes SQL evidence is retained, not rerun. No backend
suite/database/container/model rerun or mutation. BUG-FUNC-0003/BUG-DATA-0035,
physical/linked/critical and enterprise/release gates remain OPEN. Main/Mini/Azure
and unrelated local work/evidence untouched; publish only this scope.

NEXT: bounded Attendance effect-lint cleanup with existing session/draft/failure and
browser guards, Sol Medium; Sol High if authority or persistence needs modification.
Mini remains saved 86/94 CONTRACT READY / ENGINE BLOCKED under its existing handoff.
