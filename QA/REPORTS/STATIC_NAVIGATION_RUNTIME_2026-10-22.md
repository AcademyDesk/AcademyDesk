# Static navigation runtime repair — 22 October 2026

## Scope and result

Current worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. This is a bounded continuation of
the existing runtime/navigation release gate, not a new test plan or Azure approval.

The current Next 16.3.5 export requests flattened static segment names such as
`/dashboard/__next.dashboard.__PAGE__.txt`, while the export contains
`/dashboard/__next.dashboard/__PAGE__.txt`. The QA Node servers already translated
these paths; the actual cached Nginx runtime did not. Initial runs retained all
25 chat flows but failed on unexpected static prefetch 404s. They are NOT passes.

`apps/web/nginx.static.conf` now translates only the known one-, two- and
three-segment `__PAGE__.txt` grammar and returns 404 for absent payloads. Existing
HTML routing remains unchanged. This covers all 84 current segment payloads, not
arbitrary future export layouts. No backend, authorization or model behavior changed.

## Final local runtime evidence

Run `a1a8e7bff72c405a855a087fb6fefd3a`:

- 563 current exported files match HTTP response bytes by SHA256, including
  86 HTML files and 340 TXT files.
- All 84 flattened segment requests match the corresponding original payload.
- Seven selected direct/query navigation URLs return HTML; one absent route and
  three absent segment paths correctly return 404.
- All 25 existing exported-chat browser cases pass through the container-served
  static origin. No unexpected browser errors; the same three expected synthetic
  expired/stale/network diagnostics remain. These overlap earlier chat runs and
  are not 25 additional distinct application scenarios.
- Non-root `101:101`, read-only root and bind mounts, loopback-only ephemeral
  port, 128 MiB memory, one CPU, dropped capabilities and exact owned cleanup.
- Exact cached image `sha256:add1449de3e17847da8baba6eddbb71c4d1289b7ec7ae7bec25fb37ce830fc2a`;
  no image pull/build/push. Current export/config supplied by read-only binds.
- Owned container removed; 29 containers/41 volumes remain, only the two original
  PENTA services running. No prune/restart or other container removal.

| Local receipt | SHA256 |
| --- | --- |
| `QA/EVIDENCE/static-web-runtime-a1a8e7bff72c405a855a087fb6fefd3a/result.json` | `AE8E087E645B595B2A8438478354440E8E93FEF7156BCD17C9AF2B7570A96BE4` |
| Same directory, `runtime-postconditions.json` | `3DB898C4517365CB190F4E1EFA00A446D1670D4E4DCE0659DD61F85A2F487230` |
| `QA/EVIDENCE/penta-chat-contract-1792608130326/result.json` | `5D2010D7C4035D6A0A693FCF19EE215D45D5CDFB2A6549C941FC4B5F30E9FF59` |
| `apps/web/nginx.static.conf` | `B0CC70A6978651424D962F3A849B6C6696BD72A03F458999526DA3EA909737EA` |

Log: `.build-check/session-resume/static-web-runtime-final.log`. Initial failures
`ef3b7f380f1f4b109b5dbe39586ee73e` and `c7c08cca3f504086bcbf10cd53f9d31e`
and their diagnostics remain local. The copied pre-edit configuration normalizes
line endings and is not represented as a byte-identical original snapshot.

## Portable export successor

The existing GitHub workflow and Dockerfile both invoke `npm run build`. A new
`postbuild` step calls `apps/web/scripts/prepare-static-segments.mjs` to create
byte-identical flattened aliases beside the original nested static payloads.
This changes generated output only; originals remain intact. No application record,
API authority or model contract changes. The step checks the entire tree first,
rejects links/unsupported layouts/conflicting files, writes new files exclusively,
and is idempotent. Future export-format changes fail the build for review rather
than silently overwriting artifacts. It is not a generic URL rewrite engine.

- Nine focused Node tests PASS, including nested/deeper paths, exact binary bytes,
  repeat preparation, conflict preflight, changed/missing layout and a Windows
  junction escape attempt. Targeted script ESLint PASS with no diagnostics.
- Explicit `npm run postbuild` produced 84 aliases. Subsequent
  `npm run build -- --webpack` exited **0**: compilation27.5s, TypeScript8.6s,
  static generation87/87 and automatic postbuild84/84. A second preparation on
  the freshly built output reports created0/unchanged84. The normal lifecycle
  hook is proven locally with webpack; the workflow's default bundler and actual
  image build remain separate, unclaimed evidence.
- Initial plain-runtime attempt stopped before container creation because the
  unchanged 3 GiB guard failed. After natural recovery, run
  `57c10867904243caa3c67bf5ac33f3ad` exited **0** with:
  - 647 exact exported file responses (563 originals + 84 aliases), 86 HTML,
    84 flattened segments, seven navigation URLs, one absent route and three
    absent segment paths returning 404;
  - the same 25 chat browser cases PASS, no unexpected errors and the same three
    expected synthetic diagnostics;
  - plain-file fixture `QA/tools/fixtures/nginx-direct-static.conf` with NO Next
    segment rewrites; same read-only/non-root/loopback/cached-image restrictions;
  - exact owned container removed, no volume/image/other container pruning.

| Portable receipt/source | SHA256 |
| --- | --- |
| `QA/EVIDENCE/static-web-runtime-57c10867904243caa3c67bf5ac33f3ad/result.json` | `AD5D4D2DFD362FD1EB2B5CA8F17D4BA99756356EE2D55E9ABCD49E485A1E851C` |
| Same directory, `runtime-postconditions.json` | `CDD76FF938C52C4F124A6257C9A5EEA51EAEC5C967DEF684B08BE4FC42496174` |
| `QA/EVIDENCE/penta-chat-contract-1792659655521/result.json` | `5D2010D7C4035D6A0A693FCF19EE215D45D5CDFB2A6549C941FC4B5F30E9FF59` |
| `apps/web/scripts/prepare-static-segments.mjs` | `7B107CB9654A0F94339C6D04244974366CEFEECB03F49D0364592996A0ED85E1` |
| Plain-file Nginx fixture | `A43A423B2D104DEBFC0782AD03602FC78017381DC0C347F717FBAB5A89618645` |

Logs: `.build-check/session-resume/static-web-portable-final.log` and
  `static-segment-tests.log`; build log `static-portable-build.log`. The equal chat receipt hashes reflect identical
successful scenario results, NOT two independent sets of 25 application scenarios.

### Fresh-build runtime confirmation

After the full npm build above, plain-file runtime run
`3e83fb8a06de4720b46e7cda249ba31d` exited **0**: all647files/86HTML/84segments/
seven navigations/negative404 checks and the same25chat cases PASS. This supersedes
the earlier portable run for the newly generated export; it is not an additional
set of distinct scenarios. The same three expected synthetic diagnostics and no
unexpected browser errors remain. Exact owned container removed; final inventory
29containers/41volumes, only the two original PENTA services running.

| Fresh-build receipt | SHA256 |
| --- | --- |
| `QA/EVIDENCE/static-web-runtime-3e83fb8a06de4720b46e7cda249ba31d/result.json` | `F2761828C959BE0CBBC6629629AF3B18B764384497C6C50B722BFDE596F73D02` |
| Same directory, `runtime-postconditions.json` | `F2B2C5D12F1CDE742F3F6E544B0E136D62A365DBBB3A66758AA7B2E8E0E92AE5` |
| `QA/EVIDENCE/penta-chat-contract-1792659876161/result.json` | `5D2010D7C4035D6A0A693FCF19EE215D45D5CDFB2A6549C941FC4B5F30E9FF59` |
| `.build-check/session-resume/static-portable-build.log` | `25B1D5F02CD600B19BC9C2550236C6AC2BF8FF06217EF3580029238468E49F48` |
| `apps/web/package.json` | `C781D040AED86DA46E4B88D4255DC9307F0B8477F39496D8C27B4D88D103064E` |

Current-batch application changes are the Nginx configuration, package lifecycle
entry and new static-artifact preparation script only. No dependency or lockfile
changes. Prior PENTA/session application source and native SQL receipts retained.
Full configured frontend `npm run lint` exits0 with **0errors/0warnings** on the
final source, including the new preparation script (`static-portable-lint.log`).
Node syntax/PowerShell parser and scoped tracked/new-file whitespace checks PASS.

## Limits and next step

The GitHub workflow deploys `apps/web/out` to Azure Static Web Apps, NOT this Nginx
configuration. Portable files are now locally proven without server-specific
rewrites, but Azure-specific upload/hosting smoke remains NOT RUN; do not claim
the deployed site has changed. Normal build-hook and rebuilt-export plain-file
proof now pass. Continue existing exact-image/native/device/release and model
qualification gates, not another navigation rewrite or a new enterprise plan.

No image rebuild, native API/SQL/model/device acceptance or full release acceptance
is claimed here. Prior session/SQL evidence is retained; full frontend build/lint
was rerun for the portable packaging successor. All 11 broad delivery packages remain open at their
outstanding gates; model qualification/independent review remains blocked.
No staging/commit/push/main/Mini/credential/database/model/Azure change.
Recommended route: High for deployment/runtime integration, Medium for settled docs.
Read-only recheck of Mini's two return handoffs still points to E5-R63,76pending/
zero eligible/full-plan NOT ACCEPTED; no new independent-review designation or
experiment approval returned. No unchanged inference or repeated data/tool audit
was run, and no new cross-project security/tool contract is needed for this fix.
