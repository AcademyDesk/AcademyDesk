# Batch2 — frontend Git checkpoint and post-login preview

Date:2026-10-14. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`. Owner explicitly requested a Git commit and a post-login look. Local commit only; no push, main merge or Azure deployment authorized here.

## Scope

Commit the completed four-portal Manual PENTA foundation, mobile navigation and shared top-layer controls/modal presentation, immutable versioned vendor files, scoped browser tools and corresponding visual reports. Preserve all unrelated dirty backend/finance/Mini work and QA/EVIDENCE. Teacher page contains both visual and older payroll changes: stage only the visual imports/layout/navigation hunks. Do not include the payroll imports/components/data contract changes. Shared history documents receive only a new scoped handoff entry in the index, not unrelated uncommitted history.

Prior evidence and limitations: [shared-source integration](BATCH_2_PENTA_FOUNDATION_INTEGRATION.md), [shared controls](BATCH_2_SHARED_CONTROLS_PENTA.md). Existing132 bounded browser cases/9 focused checks are recorded at their actual working-tree source point. This commit excludes unfinished backend/payroll changes; validate the staged tree independently rather than assuming the whole dirty-tree build proves the commit.

## Preview

Normal static-export frontend restored at `http://localhost:3000/login`, using the actual built export without API mocks. `QA_PREVIEW_PORT=3000 node QA/tools/penta-static-preview.cjs` binds loopback only. API restored at5092 using guarded localhost integrated-auth SQL connection, LocalPreview environment, migrations disabled and bootstrap email/password explicitly empty. Startup does not seed users. Health200 and unauthenticated session401 verified after initial cold health timeout. No schema migration, production database, credentials, account creation or automatic login performed. Existing local schema/data workflow limitations remain open.

`QA/tools/manual-review-preview.cjs` renders the actual Admin dashboard in an isolated headless browser context with intercepted synthetic reads and a visible SAMPLE DATA / NOT A LIVE LOGIN watermark. Never inserts synthetic data into the normal app/database, never sends backend writes; validates Manual token mapping, displayed3 students/90% attendance, tile modal/dismissal, no page horizontal overflow or unexpected page errors. Desktop1440/tablet768/mobile390, both themes. Initial fixture used the wrong summary property names; screenshot review caught missing KPI numbers and the fixture was corrected to the real API contract with value assertions. No production dashboard payload/authorization change. Preview is a visual review, not proof of real login, permissions, SQL or production readiness.

## Candidate validation / commit

Final staged-tree build and scoped test receipts are recorded below before committing. A unique ignored `.build-check/frontend-candidate-*` directory contains an index export, not a second development environment or Git worktree. It uses the existing dependency junction read-only; no install, remote update or main checkout edit. Artifacts remain ignored/local. No evidence is staged.

Final candidate `.build-check/frontend-candidate-20261014-01`: webpack production build, TypeScript and83-page export PASS. Its frontend source equals staged blobs after CRLF/LF normalization. Six vendor files and one QA tool had extra terminal blank lines canonicalized to a single final newline; all six pinned normalized content hashes still match, no functional vendor edit/version change. Refreshed and rebuilt the candidate after normalization. Scoped new-component/vendor lint0/0, staged whitespace and high-confidence secret/private-file/scope scans PASS. Existing broader lint/security/enterprise gates remain open.

**190/190 candidate browser cases PASS**, copied without deleting originals to local `QA/EVIDENCE/frontend-git-20261014-01/`:

| Suite | Cases | Receipt subdirectory (`result.json`) |
| --- | ---: | --- |
| Admin foundation |24|`admin-manual-1791965915726`|
| Shared select/date/time controls |36|`manual-controls-1791965915698`|
| Post-login dashboard visual preview |6|`manual-review-1791965960946`|
| Platform navigation |24|`platform-navigation-1791966015073`|
| Student/guardian foundation |48|`student-manual-1791965980294`|
| Teacher foundation, visual-only staged page |36|`teacher-manual-1791965915698`|
| Retained Teacher publication contract |16|`teacher-publication-browser-1791965987679`|

These suites used the independently built staged candidate, not the financially modified Teacher working copy. They are synthetic exported UI tests, not190 live enterprise workflows. The earlier preview fixture contract mismatch was not an application defect; initial incomplete KPI screenshot and correction history remain in local evidence. Final staged-candidate dark desktop/phone and light desktop screenshots visually inspected; preview cards show3 active students/90% attendance as asserted. No saved credentials, auth bypass in the application, real login, full role/tenant/SQL, physical device or Azure acceptance claimed. Normal preview remains working-copy export with existing unrelated WIP; the reviewed dashboard/theme/navigation source is identical to candidate. The temporary candidate server3001 was stopped; normal frontend3000/API5092 remain running.

Git checkpoint contains49 explicitly selected paths. Teacher payroll changes, all backend/Mini/SQL harness changes, unrelated issue/history/checklist edits, credentials and evidence remain unstaged/local. Only the scoped new entry in AI_HANDOFF.md and the frontend-only clarification hunk in AI_PRODUCT_VISION.md are included; unrelated uncommitted history/private-model direction is preserved. Parent is `2e376b0ec47268c9b11d0dda2387620932ce2dd0`; exact new SHA is in Git history and the final response. No push, main edit/merge, remote configuration or Azure deployment.

Next: remaining Admin page/form/table/bespoke-dialog migration and remaining four-portal state parity, then actual AI UI2 composition and existing enterprise/device/release gates. The foundation is not the full frontend redesign or an authorization to deploy. Settled Manual visual route remains Sol Medium; consequential integration/security remains Sol High.
