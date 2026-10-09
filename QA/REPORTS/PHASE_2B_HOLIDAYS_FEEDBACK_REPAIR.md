# Holidays success feedback — 2026-10-09

Existing enterprise continuation, `D:\AcademyDesk-codex-p0`, `codex/penta-search`,
starting `d2267150089b99b91bfbd9310eb736acd7e69d80`.

Fixed erased default-add confirmation and missing create/remove confirmation;
transport/readback faults now caught. Polite status notices survive refresh.
Saved but failed refresh says saved/do-not-repeat;4xx server guidance or fallback
and5xx/network uncertainty/check-before-retry avoid misleading success/failure.
Only confirmed create clears form; defaults/removal preserve it. Shared ref guard
blocks duplicate/opposing create/default/remove until readback finishes; input,
date fieldset and actions disable pending. Pure fetch/scoped initial effect clears
dependency warning without suppression. Non-OK initial academy response is rejected.

Existing form payload name/date/notes/isClosed:true/scope:Custom, exact India2026
defaults endpoint/body omission, DELETE endpoint, successful empty bodies, date
display, authority and backend unchanged. No new removal approval policy, holiday
calendar/tax/legal verification, make-up behaviour or visual redesign inferred.

Validation:

- Frozen22 actual-TSX checks:1 PASS/21 FAIL; final22/22 PASS, no skips/cancels.
  Exact payload/method, create/default/remove success/error/readback, network and
  malformed error fallback, draft preservation, held write/readback and opposing
  guard/lifecycle cases. `QA/EVIDENCE/holidays-feedback-baseline-20261009.log` and
  `QA/EVIDENCE/holidays-feedback-final-20261009.log`.
- Final targeted lint0 errors/0 warnings, TypeScript and83-page webpack export PASS.
- Exported browser16/16 PASS:320/1440 light/dark × normal/failed-refresh/rejected400/
  committed500. Each creates, adds defaults, removes existing holiday; exact form
  payload, no default body, one request per action, fixture row outcomes, form
  reset/retention, durable notices and no horizontal overflow/unexpected JS or
  hydration errors. Expected400/500/503 resource errors counted separately.
  `QA/EVIDENCE/holidays-feedback-browser-1791570401960`.
  Synthetic transport/emulated viewport, not live SQL/Identity/device proof.
- First two browser runs failed before editing because fixture enabled only Core.
  Existing shell maps Holidays to Certificates and aria-disables excluded module.
  Diagnostic proved native input itself was enabled but ancestor access gate was
  locked. Fixture now includes Core/Certificates; app authorization unchanged.
  Failed evidence retained; no forced clicks or weakened assertions.

No new backend/SQL/engine/container or normal service changes; prior enterprise
receipts retained, not rerun. BUG-FUNC-0003 and linked/device/critical/release gates
OPEN. Main/Mini/Azure/unrelated local work/evidence preserved. Feature publication
only; Mini saved86/94 CONTRACT READY / ENGINE BLOCKED and qualification handoff
unchanged. NEXT Events feedback gaps, Sol Medium; Sol High for domain/access changes.
