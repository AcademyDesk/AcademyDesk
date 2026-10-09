# Make-up success-feedback repair — 2026-10-09

Existing enterprise continuation, not a new testing plan. Worktree
`D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD
`22cb2bd1ef2e12cd4fae241aceb18efdf2d9b9e0`.

## Existing work preserved / remaining gaps

Prior make-up location repair is accepted and reused, not reimplemented. All11
existing source-handler cases passed before this change: Offline optional room,
Online/Hybrid required link, hidden-field omission, next-scheduled inheritance and
mode switching. Controller, payload and business/location/timezone rules unchanged.

The current loader erased scheduling success. Status writes lacked confirmation,
server details and fault handling; failed refresh could replace a committed save
with an apparent failure. No synchronous guard prevented duplicate/opposing writes.

Loader no longer clears notices; initial load clears its own loading notice.
Create/status announce success in a polite status region and keep it after refresh.
Failed readback explicitly says the record was saved, refresh and do not resubmit.
4xx retain server guidance/fallback;5xx/network describe uncertainty/check-before-
retry. Inputs remain on rejected/uncertain create; confirmed create clears only the
same date/room/link fields as before. One ref guard locks create/status through
readback; form/status controls disable while pending. Date/time use a disabled
fieldset because their shared props do not support disabled; shared controls untouched.

## Validation

- Frozen20 actual-TSX feedback cases against HEAD:5 PASS/15 FAIL, no skips/cancels.
  `QA/EVIDENCE/makeup-feedback-frozen-baseline-20261009.log`.
  Initial baseline retained too. Status fixture now awaits returned handlers and
  two async ticks for original void callbacks; assertions not weakened. Original
  status network/readback paths genuinely produce unhandled rejections at baseline.
- Final20 new cases plus11 unchanged delivery/location assertions:31/31 PASS.
  `QA/EVIDENCE/makeup-feedback-final-20261009.log`.
  Prior controlled fixture gained stable useRef support only; assertions unchanged.
  Delayed write/readback gates verify duplicate/opposing protection and control
  locking; failure cases verify input retention and exact one request/no retry.
- TypeScript and production webpack83-page export PASS. Initial typecheck failure
  exposed unsupported date/time disabled props; corrected with scoped fieldset.
- ESLint zero errors/one inherited exhaustive-deps warning, independently identical
  on HEAD/final. `QA/EVIDENCE/makeup-feedback-lint-20261009.log`.
  No suppression; bounded dependency cleanup remains next.
- Exported browser16/16 PASS:320/1440 light/dark, normal/failed-refresh/rejected400/
  committed500; each performs create then Cancelled status update. Exact POST/PATCH
  payloads (including IST UTC conversion, optional teacher/link), input reset or
  retention, stored fixture rows/status, durable notices, no horizontal overflow
  and no unexpected JS/hydration errors verified. Expected400/500/503 resource
  errors counted separately. Reviewed320-light-normal screenshot.
  `QA/EVIDENCE/makeup-feedback-browser-1791568427624`.
  Synthetic API and emulated viewport; NOT live Identity/SQL or physical devices.

Prior SQL/engine/enterprise receipts retained, not rerun. No backend, credentials,
database/container/model or production changes. BUG-FUNC-0003 remains OPEN for
remaining forms and linked/device/critical gates. Main/Mini/Azure and unrelated
work/local QA evidence untouched. Mini saved86/94 CONTRACT READY / ENGINE BLOCKED
and existing qualification handoff unchanged. Scoped feature-branch publication only.

NEXT Sol Medium: bounded Make-up mount-effect dependency cleanup with these guards,
then existing Leave feedback gaps. Sol High only if domain/access/persistence needs
changes. No enterprise/release completion or Azure deployment claim.
