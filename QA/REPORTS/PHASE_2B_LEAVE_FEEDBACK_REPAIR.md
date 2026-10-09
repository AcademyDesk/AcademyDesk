# Leave-request success feedback — 2026-10-09

Existing enterprise continuation in `D:\AcademyDesk-codex-p0`, branch
`codex/penta-search`, starting `716fca9d7884c465b959c2db2a98e71746449f5a`.

Accepted Leave identity repair reused, not redone. Backend unchanged: canonical
Student/Teacher with only matching person ID, same-day date support, unchanged
reason and decision notes:null payloads. Create and Decide return empty200; UI
does not parse successful bodies. Existing API permissions and status options
unchanged; no inference of teacher self-service or expanded approval authority.

Remaining frontend gaps reproduced: no create/decision success, unchecked rejected
decision, uncaught transport/readback faults, erased notices and duplicate/opposing
writes. Durable polite status announcements now survive reload. Confirmed saves
with failed list refresh are described as saved/do-not-resubmit;4xx use server
guidance/fallback and5xx/network describe unconfirmed/check-before-retry outcomes.
Rejected/uncertain create retains person/dates/reason; confirmed create clears only
the same fields as before. Shared ref guard covers create and decisions through
readback; editable controls/action buttons disable until settled. Initial loader
uses pure shared fetch and scoped async effect, clearing inherited dependency
warning without suppression or reload loops. No visual redesign/shared-control edit.

Validation:

- Final frozen23 controlled actual-TSX cases at HEAD:2 PASS/21 FAIL, no skips or
  cancels; final23/23 PASS. Positive Student/Teacher payload checks pass at HEAD;
  faults/success/pending/lifecycle expose remaining gaps. Empty-success-body mock
  deliberately throws if parsed. Initial narrower baselines retained.
  `QA/EVIDENCE/leave-feedback-frozen-baseline-20261009.log` and
  `QA/EVIDENCE/leave-feedback-final-20261009.log`.
- TypeScript, final targeted lint0 errors/0 warnings and83-page webpack export PASS.
  Initial target lint showed one inherited load dependency warning; fixed here.
- Final exported browser16/16 PASS:320/1440 light/dark × success/failed-refresh/
  rejected400/committed500. Each submits then approves existing request; exact
  identity/date/reason POST and status/notes:null PATCH, retained/reset inputs,
  synthetic row states, durable feedback, no horizontal overflow/unexpected
  JS/hydration errors. Expected400/500/503 resource errors counted separately.
  `QA/EVIDENCE/leave-feedback-browser-1791570075455`.
  Earlier browser passes retained; adapted fixture initially used201/204 success,
  corrected to controller-exact empty200 and rerun before acceptance.
  Synthetic transport/emulated viewports, NOT live Identity/SQL/device proof.

Prior51 direct-controller identity and63 HTTP/SQL receipts retained, not rerun;
historical source-pin validator is not current acceptance after authorized changes.
No backend/engine/database/container or normal development service change.
BUG-FUNC-0003/BUG-DATA-0038 and linked/device/critical/enterprise/release gates
remain OPEN. Main/Mini/Azure/unrelated dirty work/local evidence preserved; scoped
feature publication only. Mini saved86/94 CONTRACT READY / ENGINE BLOCKED and
existing qualification handoff unchanged.

NEXT Sol Medium: existing Holidays feedback gaps; Sol High if domain/access/
persistence changes. No new testing plan, enterprise completion or Azure release.
