# Access Review expiry display — 25 October 2026

Existing BUG-UI-0003/E2/E3 continuation after the qualified Staff workflow and
mobile-containment packet. No backend/permission/finance/model change. Keep the
accepted same-bearer/stored-expiry native grant evidence bound to its own image;
this packet tests frontend display timing and must not claim new SQL authority.

## Reproduction and repair

Actual-source controlled-clock baseline `QA/EVIDENCE/access-review-expiry-baseline-20261025.tap`
has seven failures/zero passes: expired unrevoked rows counted active, boundary
status not distinguished, invalid dates can throw during formatting, no long-open
clock refresh/focus or visibility recovery, and time-of-day absent from IST expiry.

Only `apps/web/src/app/access-review/page.tsx` changes relative to accepted Staff
containment export. Revoked takes precedence; permanent unrevoked stays Active;
temporary expiry must parse and be strictly greater than current display time.
Invalid/missing temporary expiry is unavailable/inactive and formatted safely.
Expired/unavailable rows do not show a usable active-grant revoke button. The
already-existing backend routes/authorization are unchanged; display status does
not grant or revoke permission.

A bounded timer schedules the nearest expiry or60seconds, with focus/visible
resume refresh and proper timer/listener cleanup. Initial empty server render
does not depend on a server/client clock comparison; the clock is published with
the API-loaded register. Exact equality is expired. Explicit IST date and time
are shown for valid temporary grants. Device clock skew may affect the display;
the backend clock/permission checks remain authoritative. No UI state or memory
can override current server authorization.

## Current evidence and acceptance

- [x]Seven actual TSX/controlled-clock cases now PASS, including mixed states,
  exact boundary, invalid data, long-open/focus/visible recovery, IST and cleanup.
- [x]All16existing grant/revoke feedback/pending/uncertainty handlers still PASS.
  That harness now supplies inert timer/event surfaces; the separate new clock
  tests assert actual scheduling/cleanup behavior. No existing assertion removed.
- [x]Fresh UI `11505c82b2d4463e86d925bf4069e17e`, web build
  `60343a6972bf4d4e92c57a313514fb4d` PASS23handlers/typecheck/lint/627files.
  Window `0e2dade20e8c40979df785641e4945db` PASS/exact originals/full inventory.
  First window `47e802fbb6d14582b2f21cc8d6226e2a` remains FAIL at child
  preflight; no UI build created. Generic preflight error does not prove its cause.
- [x]Real exported presentation in390/1440light-dark with seven synthetic rows,
  2active→1active at the exact boundary, no new read/write to expire the display,
  invalid-date safety/IST/status/no page errors/screenshot checks.
- [x]Browser `66af30d7073c40a3928c2e0b5319e45c` PASS4/4; all four PNGs
  visually inspected. No new read/write needed for display expiry.
- [x]Exact original PENTA IDs/images restored, inference healthy, full inventory/
  lease release and `node QA/tools/access-expiry-qualification.cjs` PASS.
  Browser receipt SHA256
  `931bb8e91e558276dd8b75b67bb9b25cc078c002d7ac2d5d85ee53cce1617406`;
  window receipt SHA256
  `1602e69dda6c498d2d0ed63f842ced7ff602ba582d97807bf3f627a5ab9b9298`.

Browser reads/tokens and clock are deliberately synthetic in this display test;
no SQL/Identity/server-expiry/Android/iOS/clock-skew authority acceptance. Earlier
actual grant native/revocation/expiry cases are retained, not repeated or relabelled.
No new backend build/full API suite/download/main/Mini/GitHub/Azure/customer data.
BUG-UI-0003 and release gates stay OPEN until their applicable broader acceptance.
Medium is appropriate for this settled display change; use High for any future
server authority/expiry policy alteration. No running-model switch is implied.
