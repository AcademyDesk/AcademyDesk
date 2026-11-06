# Local L2 cloud conversation/container integration — 6 November 2026

Status: SOURCE INTEGRATED / local SQL and component checks passed; full frontend
retains one Windows capability failure. The following scope was claimed before edits. Local Codex owns this integration/verification
packet on codex/penta-host-integration-20261030. Base9dc2ccd, tracked clean;
raw QA/EVIDENCE stays local. Cloud returned C3.2a4ce05b/implementation34f3b46
and C4 8670605/implementation3cf250e. Preserve their bounded source changes,
cloud reports and all prior local host fixes; no wholesale preservation merge.

Exact implementation-owned source paths (integrate byte-identical cloud blobs):
- `QA/tools/check-web-container.sh`
- `QA/tools/class-media-client.test.cjs`
- `QA/tools/penta-entry-browser.cjs`
- `QA/tools/penta-entry.test.cjs`
- `QA/tools/penta-onboarding-channel.test.cjs`
- `QA/tools/penta-onboarding-dialogue.test.cjs`
- `QA/tools/penta-onboarding-expiry.test.cjs`
- `QA/tools/penta-onboarding-idle-expiry.test.cjs`
- `QA/tools/penta-onboarding-operation.test.cjs`
- `QA/tools/penta-onboarding-recovery.test.cjs`
- `QA/tools/penta-onboarding-review.test.cjs`
- `QA/tools/penta-onboarding-session.test.cjs`
- `QA/tools/penta-onboarding-start.test.cjs`
- `QA/tools/penta-onboarding-terminal.test.cjs`
- `QA/tools/penta-teacher-dialogue.test.cjs`
- `QA/tools/penta-teacher-operation.test.cjs`
- `QA/tools/penta-teacher-start.test.cjs`
- `QA/tools/refresh-concurrency.test.cjs`
- `QA/tools/session-cross-tab.test.cjs`
- `QA/tools/session-logout.test.cjs`
- `QA/tools/static-segment-export.test.cjs`
- `QA/tools/static-web-runtime.cjs`
- `apps/web/Dockerfile`
- `apps/web/nginx.static.conf`
- `apps/web/package.json`
- `apps/web/scripts/prepare-static-segments.mjs`
- `apps/web/src/app/login/page.tsx`
- `apps/web/src/app/penta/onboarding/page.tsx`
- `apps/web/src/app/penta/teachers/chat/page.tsx`
- `apps/web/src/app/penta/teachers/page.tsx`
- `apps/web/src/components/penta/onboarding-dialogue-workspace.tsx`
- `apps/web/src/components/penta/onboarding-operation-workspace.tsx`
- `apps/web/src/components/penta/onboarding-review-workspace.tsx`
- `apps/web/src/components/penta/onboarding-review.module.css`
- `apps/web/src/components/penta/onboarding-start-workspace.tsx`
- `apps/web/src/components/penta/role-penta-entry.tsx`
- `apps/web/src/components/penta/teacher-chat-workspace.tsx`
- `apps/web/src/components/penta/teacher-chat.module.css`
- `apps/web/src/components/penta/teacher-review-workspace.tsx`
- `apps/web/src/components/penta/teacher-review.module.css`
- `apps/web/src/components/penta/teacher-start-workspace.tsx`
- `apps/web/src/components/workspace-frame.tsx`
- `apps/web/src/lib/api.ts`
- `apps/web/src/lib/penta-chat-contract.ts`
- `apps/web/src/lib/penta-onboarding-channel.ts`
- `apps/web/src/lib/penta-onboarding-dialogue.ts`
- `apps/web/src/lib/penta-onboarding-operation.ts`
- `apps/web/src/lib/penta-onboarding-review.ts`
- `apps/web/src/lib/penta-onboarding-start.ts`
- `apps/web/src/lib/penta-teacher-dialogue.ts`
- `apps/web/src/lib/penta-teacher-operation.ts`
- `apps/web/src/lib/penta-teacher-start.ts`
- `apps/web/src/vendor/penta-operational/README.md`
- `apps/web/src/vendor/penta-operational/UPSTREAM.json`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/Workspace.tsx`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/index.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/projection.schema.json`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/types.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/validate.ts`
- `apps/web/src/vendor/penta-operational/frontend/operational-ui/src/workspace.css`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/academy-mapping.css`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/contracts.ts`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/foundation.css`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/index.ts`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/primitives.tsx`
- `apps/web/src/vendor/penta-operational/frontend/shared-foundation/src/tokens.ts`

Also copy cloud reports QA/HANDOFF/C3_CONVERSATION_2026-11-06.md and
QA/HANDOFF/C4_CONTAINER_READINESS_2026-11-06.md unchanged; shared status pointers
are additive integrator updates. This report records local outcomes only.

Local validation scope:
1. Verify restored/vendor source pins and all unchanged backend/API/SQL source.
2. Rebuild the existing SQL harness without new packages and run the real
   CommunicationRoundtrip module in an owned disposable SQL container, using
   cached SQL image. Preserve originals/services/lease/resource ownership.
3. Use its genuine synthetic-data HTTP/SQL receipt for the three previously
   missing communication-evidence frontend cases; no fixture/log invention.
   Run the combined current frontend suite with cached pinned TS/React tools.
4. Run the new static-segment regressions. If the existing cached QA web image
   contains usable Nginx, test the current config against owned synthetic static
   files in an isolated loopback container. This is component runtime proof, not
   a current production image build or actual89-page export acceptance.
5. Record exact-lock dependency/build/browser prerequisites honestly. No new
   packages/base-image download, model experiment, main merge, customer DB or
   Azure action. Source-bound1,984API result is reused only if backend/tests match.

No change to assertions, original evidence, model/role/tenant authority, approval
or audit requirements. Model qualification and candidate loading stop remain.
Next security/integration route Sol High; settled UI/docs route Sol Medium.


## Returned integration and local results

Integrated66implementation paths byte-exact to cloud C3.2 `34f3b46` and C4
`3cf250e` (enclosing reportsa4ce05b/8670605). Both cloud reports are copied
unchanged. All12vendor normalized hashes match. Backend/API tests/SQL harness
and dependency lockfile are unchanged from9dc2ccd; only C4's existing package
postbuild script is added. No domain, model, protocol, feature flag or permission
is activated by these source copies. No older source branch was imported wholesale.

C3.2 restores the protected student and teacher workflow pages and their real
host-adapter dependency closure. Explicit login epochs, same/peer-tab logout and
bounded cross-tab renewal controls are included; normal renewal preserves its
workspace identity. Protected draft/approval channels still require a fresh host
check when their bearer context changes. These controls do not grant authority.
Other roles retain honest conversation-unavailable and usable Manual navigation.
This is not accepted90%AI coverage, all-role conversation or complete Manual styling.

| Actual local validation | Result / limit |
| --- | --- |
| Genuine TSX input generation | 24actual controlled TSX payload records generated by existing test; no invented receipt |
| Harness build | PASS,0warnings/errors; same compiled source reused after shell correction |
| Fresh SQL CommunicationRoundtrip | **48cases PASS**, run2e5e6d6d3ab74cbbb431a104df67f0ce; actual Identity/HTTP/owned SQL,85application/7Identity migrations, tenant/auth/module checks and audit invariants |
| Previously missing communication evidence | Existing communication-drafts group **48/48PASS**, including all three genuine receipt cases; local only, raw evidence is not published |
| Corrected full frontend | **1,894tests:1,893PASS/1FAIL/0skip/cancel**,68files; runner exit1 remains correct |
| Sole remaining local frontend failure | static-segment-export symbolic-link case: Windows EPERMcreating a file symlink;13/14other file cases passed; no assertion skipped/weakened or privilege/policy changed |
| Actual Nginx config component | Seven byte/MIME GET checks + three404checks + query-route check PASS; same current config and actual segment-preparation script on cached historical Nginx image |
| API baseline reuse | Existing1,984PASS is retained only because backend/test source hashes match; not a rerun or new combined browser/API acceptance |
| Resource restoration | Exact original PENTA services/images healthy,API200; complete Docker inventories preserved; owned resources removed and leases released |

SQL success removed its exact run-owned database/login/container after marker,
label and loopback ownership checks. The genuine output was copied byte-identical
into excluded local QA/EVIDENCE for the existing frontend test. Its SHA256 is
00998850f2b43458324020bdedb72e0ac01e5b438b3d0e31f3de18f72379e181.
No customer data, secure provider credentials or outbound communication was used.
Cloud still lacks those private local receipts; its three missing-input cases are
not automatically closed by this local result. Do not publish raw QA/EVIDENCE or
replace them with made-up fixtures to green the cloud runner.

### Nginx evidence boundary

Run9dc1415ca4e7478f9124f568f910baf2 used cached image
sha256:add1449de3e17847da8baba6eddbb71c4d1289b7ec7ae7bec25fb37ce830fc2a.
Only current server config and owned synthetic static files were mounted read-only;
owned top-level config limits workers and uses private temporary paths. One
non-root,128MiB,read-only,capability-dropped container had network=none and no
published ports. Its own existing HTTP client fetched its own Nginx loopback.
The alias script produced one legacy alias, retaining two flat files. This is
real component routing/byte proof, NOT a new production image, current89-page
artifact, host-published port, authenticated browser, API, model or device proof.

The first owned internal-network probe failed before HTTP assertions because
its immediate inspection found no port binding. Its result/cleanup remain retained;
this does not establish an application or production-image defect. The network-none
successor provides isolated component HTTP evidence and does not pretend to close
the separate host-published/full-image gate. Both complete inventories matched.

### Retained setup failures and corrections

- First windowd484f7d0stopped before tests at unchanged3GiBRAM guard; restored.
  Safe Linux cache release/compaction recovered enough RAM for the successor;
  no unrelated user application was closed, no guard lowered or Docker restart.
- Window94cc931c built the harness, but legacy Windows PowerShell refused script
  execution before SQL creation. The already-installed PowerShell7host's existing
  RemoteSigned policy executes the local script normally; no execution-policy
  override, privilege change or system setting was used.
- Windowee6cee54 passed the SQL roundtrip. Initial frontend1884PASS/two failures
  plus one collection failure exposed missing cached react-dom/eslint resolution,
  alongside the symlink prerequisite. Full first-run logs were archived unchanged.
- Only the ignored cache resolver was extended for existing ReactDOM19.2.8 and
  ESLint9.39.5, both matching direct locked versions; TS5.9.3/React19.2.8 also match.
  Corrected windowdc3d7674reused the exact genuine SQL log and reached1893/1894.
  Source/tests/expected results did not change. No package download/install.
- Ordinary whitespace checks identify exactly six unchanged pinned vendor EOF
  blank lines. Preserve those original bytes; all other whitespace checks pass
  with only blank-at-eof excluded per command. No repository config change.

Raw logs/manifests/native receipts stay under ignored/local evidence. No full
pinned Next16.4.3 install/build was run locally; cached16.4.1 is not substituted.
Cloud's own pinned build/browser results retain their source-specific boundaries.
Full image build/current CI/audits/actual host-browser/physical-device/P0/privacy/
remaining workflows/restore/staging/release stay open. Mini still has two Linux
provenance cases and unaccepted quality; candidate loading/training/inference/
activation remains stopped. No main merge, Azure or customer database action.

Next local L2: actual protected host/browser and role/session roundtrips with a
reviewed exact-lock artifact, followed by the established remaining gates. Cloud
continues its separately claimed build/audit/CI and portable-provenance tasks.
Sol High for integration/security; Sol Medium for settled visual/docs work.
