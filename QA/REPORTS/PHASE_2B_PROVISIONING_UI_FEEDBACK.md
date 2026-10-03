# Phase 2B — Provisioning feedback safety

2026-10-02; Sol High. Bounded successor to the accepted provisioning conflict repair; no repeated Astra audit or previous SQL matrix. Local uncommitted changes only.

## Fixed

Portal Access now checks all three person lookups before enabling creation. Denied/malformed lookups show a connection/access message, rather than enabling a form on invalid data or advising an unsupported migration restart. A synchronous submit guard plus disabled fieldset prevents overlapping submissions and field changes while pending. Success announces through role=status and clears the draft without requiring a JSON success body. Rejections retain entries, expose the API conflict message, and distinguish401/403 fallbacks through role=alert. Network/response loss shows an uncertain-outcome message, retains entries, and does not automatically resubmit or falsely claim rollback.

Platform Overview separates confirmed academy creation from the later portfolio refresh. A refresh error after successful creation closes/clears the creation dialog and reports the academy was created, with an explicit instruction not to submit again. Existing request contracts, optional fields, role/tenant policies, visual design and API/auth helper unchanged. This is not a redesign or a repair of every platform-control form.

## Evidence and limits

16/16 actual TSX handler checks PASS with controlled hooks/transport: success/reset, synchronous duplicate-submit/pending guard,409/400/401/403/transport errors and draft retention, denied/malformed person lookups, three role-selector resets, and four platform create/refresh outcomes. These are component-handler tests, not browser or SQL acceptance. Initial12/16 run excluded: four platform fixture selectors incorrectly required exact Onboard academy text, missing its leading plus glyph; fixture corrected, original test source/log retained. No application defect inferred from that fixture failure.

TypeScript noEmit/incremental=false exit0, empty diagnostic log. Isolated SQL harness build passed, zero warnings/errors. Existing1032 backend acceptance is preserved, not rerun or recounted.

The owned real SQL/Identity/API bridge started with unchanged313 routes and82/7 migration sets. Existing fixture preflight checks established real tenant boundaries. A copied QA-only frontend on49141 served login200 with the real API on49142. Browser control could not inspect/interact: initial navigation timed out during22.9s compilation, then focus emulation timed out for snapshot/reload/documented own-tab replacement. Read-only tab listing worked. No login or creation submission occurred; no screenshots or browser successes counted. Browser/mobile, visible notices, new-account routing and role/session-refresh gates remain UNVERIFIED/OPEN. No fake responses, localStorage/session injection, alternate browser or invented visual evidence.

The aborted host correctly failed its required browser-created-account assertions after an owned stop marker; this is an incomplete test, not a product provisioning failure. Fresh real SQL queries confirmed zero intended browser-created identities and zero new test academy. Run1b29405dad3b453cb6212233cfd4631a/SQL58091 was cleaned only after folder/SQL/name/hostname/image/run-label/loopback/mounts0 verification. Guarded storage removal completed; mount-free SQL database/login removed with the exact container; verified owned frontend processes stopped. No remaining58091/49141/49142 listener. Copied frontend/build artifacts and all evidence retained; unrelated older fixtures/customer data/dev services/Azure untouched.

[Final handlers](../EVIDENCE/logs/phase-2b-provisioning-ui-handlers-final.log), [initial excluded fixture run](../EVIDENCE/logs/phase-2b-provisioning-ui-handlers.log), [types](../EVIDENCE/logs/phase-2b-provisioning-ui-types.log), [build](../EVIDENCE/logs/phase-2b-provisioning-ui-build.log), [aborted SQL/browser host](../EVIDENCE/logs/phase-2b-provisioning-ui-sql.log), [copied frontend](../EVIDENCE/logs/phase-2b-provisioning-ui-web.log), [browser outcome](../EVIDENCE/provisioning-ui-browser-outcome.json), [cleanup](../EVIDENCE/provisioning-ui-cleanup.json), [before manifest](../EVIDENCE/provisioning-ui-before.json), [receipt](../EVIDENCE/provisioning-ui-source-receipt.json), [consistency validator](../tools/validate-provisioning-ui.cjs).

788 predecessor/source entries pinned. Only two product pages, owned-host dispatch/runner additions and three tracker successors permitted. Existing controllers/transactions/classifier/schema/auth helper/login/accepted artifacts/HEAD preserved. No commit/push/deploy/Azure change. BUG-DATA-0011/Phase2B/release OPEN.

Next: bounded session-refresh/request-header behavior checks using the existing auth contract, then retry the pending visible provisioning flow when browser control is available. Continue the agreed Sol High allocation; no fresh source audit or assumed browser pass.
