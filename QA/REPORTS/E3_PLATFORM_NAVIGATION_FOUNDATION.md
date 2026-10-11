# E3 — Platform Owner navigation/header checkpoint

Date:2026-10-11. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, starting `c0eaf173418ea3b8ce19b7bcaffabcd66191a427`. Bounded Manual presentation/navigation repair; not full portal/E3/enterprise acceptance.

## Demonstrated gaps and changes

Overview's duplicated menu omitted **Announcements**; control pages included it. Baseline exported UI fails the11-destination assertion with10 actual links. Both pages now import one presentation-only `platform-navigation.ts` registry, including the existing ten control tabs. This grants no authority or tenant access.

Desktop active destinations now expose `aria-current="page"`; decorative icons are excluded from accessible names. Scoped host-token CSS ensures44px navigation targets and3px visible focus. Existing desktop sidebar scrolling, mobile all-destinations grid, button-based control switching, `router.replace`/query synchronization and content/document scrolling remain intact. No URL/router/domain policy redesign.

Screenshot review revealed320px role text overlapping utility buttons. The first element-box test missed text extending beyond its parent; strengthened **text Range geometry** independently reproduced the overlap. Phone-only header CSS now places identity and utilities on separate rows. Tablet/desktop layout unchanged; no global theme rewrite.

## Validation

`QA/tools/platform-navigation-browser.cjs` exercises the real export with synthetic Platform Owner read fixtures,15 academy records and empty supplementary lists; external requests blocked. No write action invoked, zero mutation requests and page errors required. Not real authentication/authorization/SQL/model acceptance.

Final **16/16 journeys PASS**:320/390/768/1440px × light/dark × Overview/direct Admins entry. Each checks11 available destinations, then keyboard-activates11 consecutive sections, exact URL/tab, correct title and one named active destination,44px targets/visible focus, non-overlapping role text/utilities and no horizontal page overflow. Back/Forward restores Tenant Management/Overview and active state. Document/mobile or bounded desktop content scroll verified. These are176 destination transitions plus history checks, not176 independent domain tests.

Final receipt: `QA/EVIDENCE/platform-navigation-1791688301372/result.json`, with four source SHA256 identities. Light Overview/dark control-entry320px screenshots inspected: readable identity, separate utilities, complete menu and visible focus. Screenshot filenames describe the initial route; screenshots are taken after returning to Overview.

Retained receipts:

- `platform-navigation-1791687992798`: original export fails10-versus11 destination check immediately; not a complete baseline matrix.
- `platform-navigation-1791688109356`:16 initial navigation journeys pass before URL/history strengthening.
- `platform-navigation-1791688166270`:16 URL/history-strengthened journeys pass before header checks.
- `platform-navigation-1791688228934`:16 element-box header checks pass but do not detect overflowing role text; **not header acceptance**.
- `platform-navigation-1791688254302`: strengthened Range-based header assertion reproduces overlap. Final assertion retained and passes after repair.

Previous shared popup runner rerun unchanged during this batch:32/32 PASS, `QA/EVIDENCE/manual-detail-modal-1791688176509/result.json`. This ran against the first navigation build before the final Platform-only header CSS adjustment; shared popup source unchanged. Not fresh SQL/device acceptance.

- Installed TypeScript `--noEmit`:PASS.
- Final Next16.3.5 webpack/export:83/83 PASS.
- New registry ESLint:0 errors/0 warnings PASS.
- **Platform page lint NOT PASS**: Overview1 error/2 warnings; control7 errors/8 warnings. Same before/after counts independently checked against HEAD via ESLint stdin. Control rule/severity multiset identical. Existing state-in-effect/purity/dependency/image/unused diagnostics not suppressed or expanded into business-state refactoring.
- Runner syntax/scoped diff checks:PASS.
- Earlier SQL/Identity/RBAC/tenant/finance evidence retained NOT rerun; application APIs, save payloads, permissions, calculations, Mini/protocol and existing enterprise strategy unchanged.

## Remaining / handoff

Platform Owner is not fully accepted. Its separate legacy detail overlays, utility availability (existing notification/search placeholders), full form feedback/loading, inherited lint, long-source data, physical Android/iOS/Safari/screen-reader and real host/role journeys still require their own bounded checks. Mobile full menu remains the accepted visible-destinations layout, not a new collapsed-navigation design. No issue closed, no full accessibility or production-readiness claim.

NEXT **Sol Medium**: Platform Owner detail overlays/utility states and bounded lint ownership, preserving accepted navigation fixes; then Admin -> Teacher -> Student. **Sol High** for consequential identity/tenant/domain/finance/shared-package/release integration. FW1/Mini86/94/live/device/enterprise/release remain OPEN, original28 feedback checkpoints unchanged. No Mini project switch or external message, no main/Azure. Preserve unrelated11 tracked files/32+28 continuity lines, untracked work and local evidence. Scoped feature publication only.
