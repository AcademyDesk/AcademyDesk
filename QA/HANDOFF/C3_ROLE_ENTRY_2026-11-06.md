# C3.1 — role entry and guarded Manual navigation — 6 November 2026 IST

Owner/environment: Cloud Codex, managed cloud. Status: IMPLEMENTED / bounded local frontend checks VALIDATED. Full conversation, actual host/browser/device, model and production qualification remain OPEN. Branch: `codex/cloud-c3-role-entry-20261106`.

## Source and inherited work

Base `30d0fd5cca8c58dff9b2af5262b8a86e9d53b3de` was fetched and independently observed on Desktop's host branch before this packet. Local L1 source/report updates are preserved, including teacher snapshot-helper correction and the recorded host-only SQL/runtime results with their exact reuse/failure boundaries. No backend acceptance is rerun or broadened by this frontend packet. C1/C2 remain published on their own cloud branches; no wholesale preservation merge or repeated audit.

Implementation commit: `d0d7967843a3cf4e8b3eb15e61f854afa3a012bf`. The final enclosing report commit adds documentation only. All exact path claims were committed/pushed before code edits. Mini repository remains clean and unchanged. The original cloud environment recovered without replacement or publication.

## Resulting behavior

- Verified Owner/AcademyAdmin, Teacher, Student/Guardian and Platform sign-ins have role-scoped PENTA entry routes; non-pilot administrative roles retain the Manual dashboard default. Explicit safe return links stay within the server-reported workspace. The UI does not grant a role, tenant, permission or tool.
- Login validates the token pair and successful noncached server profile before saving tokens. Missing, malformed, failed or mismatched profiles no longer silently select AcademyAdmin. External, encoded, backslash and cross-workspace return paths are rejected by the restored route helper.
- Entry pages request authenticated session metadata through the existing workspace-pinned client (standard auth renewal may occur). They show verified/sign-in/denied/unavailable states and Manual links. No chat input, provider/model selector or business-action control is enabled. Entry metadata and capability information are not operational AI acceptance.
- The new mode bar opens the matching PENTA route from existing Manual pages. Form interaction triggers a conservative leave guard. A native modal dialog keeps focus, allows Stay or Escape without discarding fields, restores mode-link focus, and requires an explicit Leave action to navigate. This is a mode-link safeguard, not autosave, host cancellation or universal browser-navigation protection.
- Orbit/Prism presentation follows the existing light/dark provider with scoped candidate foundation tokens. Aura tokens remain available upstream but no separate three-theme selector is claimed. Existing Manual business forms and domain handlers are not restyled/replaced.

The entry is an honest unavailable surface while conversation qualification remains pending. It must not be presented as completed operational AI or the 90% target. Backend readiness/role-specific conversational capabilities are not established by a profile check.

## Exact owned paths

- `QA/HANDOFF/C3_ROLE_ENTRY_2026-11-06.md`
- `QA/tools/penta-entry.test.cjs`
- `QA/tools/penta-entry-browser.cjs`
- `apps/web/src/app/login/page.tsx`
- `apps/web/src/components/workspace-frame.tsx`
- `apps/web/src/lib/workspace-entry.ts`
- `apps/web/src/components/penta/role-penta-entry.tsx`
- `apps/web/src/components/penta/role-penta-entry.module.css`
- `apps/web/src/components/penta-manual-theme.ts`
- `apps/web/src/components/penta-manual-theme.module.css`
- `apps/web/src/vendor/penta-shared-ui/tokens.ts`
- `apps/web/src/vendor/penta-shared-ui/foundation.css`
- `apps/web/src/app/penta/page.tsx`
- `apps/web/src/app/teacher/ai/page.tsx`
- `apps/web/src/app/portal/ai/page.tsx`
- `apps/web/src/app/platform/ai/page.tsx`

No backend controllers/intelligence/services, SQL/migrations/harness, API tests, auth-refresh implementation (`apps/web/src/lib/api.ts`), workflow, dependency/lockfile, shared tracker or model source changes. Local L1 retains its ownership. No new packages, gallery data, synthetic provider, runtime model or private evidence was imported into the app.

## Preserved-source reconciliation

Selected existing frontend source from `6887e6eb26e88f8fc99eee7a00c267a14c12883f` was reused. Ten existing source files were brought forward; seven remain byte-exact. Three were reconciled: role entry/CSS gains AcademyAdmin presentation, late-profile token checks, explicit recheck and native dialog; the theme hook imports its scoped foundation CSS. The new AcademyAdmin page uses that same entry. Current login/frame received bounded integration edits instead of older whole-file replacements.

The vendor copy is only the two reviewed token/CSS source files, not a claimed complete package/gallery import. No baseline source pin or qualification record was rewritten. Exact provenance is retained outside Git in source-provenance.json:

| Preserved file | Current relation |
| --- | --- |
| `apps/web/src/lib/workspace-entry.ts` | Byte-exact original |
| `apps/web/src/components/penta/role-penta-entry.tsx` | Bounded reconciliation described above |
| `apps/web/src/components/penta/role-penta-entry.module.css` | Bounded reconciliation described above |
| `apps/web/src/components/penta-manual-theme.ts` | Bounded reconciliation described above |
| `apps/web/src/components/penta-manual-theme.module.css` | Byte-exact original |
| `apps/web/src/vendor/penta-shared-ui/tokens.ts` | Byte-exact original |
| `apps/web/src/vendor/penta-shared-ui/foundation.css` | Byte-exact original |
| `apps/web/src/app/teacher/ai/page.tsx` | Byte-exact original |
| `apps/web/src/app/portal/ai/page.tsx` | Byte-exact original |
| `apps/web/src/app/platform/ai/page.tsx` | Byte-exact original |

## Actual validation on final implementation

| Check | Actual result / limit |
| --- | --- |
| Production webpack/static export | PASS, exit 0; all 86 pages generated including four entry routes. Built with loopback QA API URL for browser verification, not a deployable production-configured artifact |
| Final TypeScript | PASS, exit 0 |
| Frontend lint | PASS, exit 0 |
| New routing/login unit regressions | 93 PASS / zero fail/skip; actual helper and login handler under controlled fetch/storage VM, not Identity or browser proof |
| Existing logout, refresh, concurrent refresh | 18 / 37 / 20 PASS; zero failures/skips. Existing helper source unchanged |
| Final full frontend suite | 1,242 PASS / 3 FAIL / zero skipped/cancelled; 1,245 tests across 53 current files, exit 1. Only communication-drafts.test.cjs fails, requiring genuine generated SQL/HTTP evidence |
| Final Chromium/React | 13 groups PASS, exit 0: four workspace entries at 1280/390 widths, Orbit/Prism, sign-in/mismatch/unavailability, held profile/token replacement and explicit recheck, Manual guard/Stay/native Escape/focus retention |
| Browser traffic/errors | Zero model/tool/business-write requests; zero captured runtime/React console errors. Synthetic session/auth responses and a minimal empty Teacher fixture only; standard refresh attempts included |
| Scope/source checks | PASS: claimed paths only; no backend, API-test, SQL/harness, auth-refresh helper, package/lockfile/workflow changes; Mini unchanged |

Counts overlap and must not be added into a completion percentage. The smaller source-specific frontend suite is not the larger preserved local test program. The browser does not prove real Identity/SQL, physical mobile/Safari/screen-reader, backend permissions, active conversation/model quality or production behavior. Screenshot and result records remain under `.build-check/penta-entry-browser/`; raw logs/manifest remain outside repositories in `/workspace/c3-role-entry-20261106`.

Retained setup/test failures: initial bare TypeScript encountered stale ignored Next route types, then normal typegen/build refreshed them; final check passes. A stricter browser rerun initially reached the Manual guard before a valid hydrated Teacher fixture was established. The fixture now supplies an explicit empty synthetic profile and waits for its real rendered welcome state; no app assertion was removed. Runner artifacts explicitly record RUNNING/PASS/FAIL and preserve previous results, so an old pass cannot mask a failed rerun. These logs remain distinct from the final passing run. No current SQL-dependent test was skipped or fabricated.

## Remaining limits and next packets

1. Actual role-aware Identity/host/browser acceptance is still required. Local/Desktop should fetch this branch, inspect base/paths and test real noncustomer role sessions, normal token expiry, cross-tab logout/replacement, suspended-page recovery and unsaved forms against the reviewed host. The existing refresh implementation is unchanged. Token replacement/rotation during entry verification fails closed to explicit recheck; a later opaque session-epoch integration is needed before presenting sensitive conversation/draft state across all lifecycle changes.
2. This slice deliberately does not restore operational conversation, real provider selection, protected student/teacher chat/draft/approval/execute/receipt UI, full Aura controls or all-role tool coverage. Those existing preserved components require a separate bounded C3.2 contract/context/session claim and versioned source review. Do not simply mount a gallery or real-model client to make the entry functional.
3. Local L1's four SQL module results remain source-scoped as recorded. New connected UI work needs actual combined source acceptance; do not repeat or relabel older API/SQL runs as a pass for new browser behavior. No backend or feature flag was changed here.
4. C1 still needs genuine excluded original files and native proof prerequisites. The three communication receipt cases remain failing until real evidence is generated. C2's catalog ambiguities, missing Meetings source and credential-flow hold remain for the designated integrator.
5. C4 still requires the unmodified production Dockerfile/fresh-audit/container prerequisites; the earlier local cached-runtime smoke is not production build acceptance. Mini/Plus/Pro remain unqualified, Ultra deferred, and model acquisition/loading/inference/training/activation remain stopped. Five P0/critical P1/device/privacy/restore/staging/release gates remain open.

Local return: review/choose compatible feature commits; exercise actual `/api/auth/session` role responses and Manual fallback without customer mutations or model loading; return exact source, commands, outcomes and artifact hashes. One integrator updates shared handoffs/trackers. No main merge, force push, Azure deployment, paid resource, customer-data operation or environment publication follows this packet.

## Evidence fingerprints and publication

- `entry-unit-final.log`: `1766eb1a8edefc50fb614d587133952da37292dd9db808f4b4036181571dc76f`.
- `web-build-source-d0d7967.log`: `65118aab804fd057e7a1228053533e08e95b2f1f0e7cf223f311da0de177a63a`.
- `typecheck-final.log`: `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- `frontend-summary.json`: `8cdb0fa57e58608e4c9113128635f529b19d7a8c29226c3c5d86d76898f89b2c`.
- `browser-result.json`: `f4dad5ce9884963bc9ae6d913d17a4d5db133f75ccf8ebcac102d36949ff3c52`.
- `source-provenance.json`: `2402471aac41fdecf2705cc8d2088bc5ae7cc019c6f3efa73a828d04a3c8f703`.

Publication SHA is the final enclosing report commit on the named feature branch; final reply records its independently verified remote tip. No current CI conclusion is inferred from the local checks. Next route: Codex / Sol High for session/context/authority-sensitive C3.2 decisions; settled visuals follow the existing routing rules.
