# Default production build — 22 October 2026

## Boundary

Continuation of the existing release/runtime gate in `D:\AcademyDesk-codex-p0`,
`codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.
Previous webpack/build/portable-export proof is retained, not relabeled as the
default bundler or a new image build. Initial work was QA-only; the reproduced
desktop failure below required one scoped AI-shell CSS repair. No config/package/
dependency change in this packet. No Azure, staging, commit, push or model action.

## Reproduction and isolation

The exact workflow command, `npm run build`, exits1 in this worktree before
compilation: Turbopack rejects `apps/web/node_modules` because its junction points
to `D:\AcademyDesk\apps\web\node_modules`, outside the detected project root.
This is local fixture topology, NOT a demonstrated Azure compilation failure.
Installed Next16.3.5 documentation confirms that files outside Turbopack's root
cannot resolve. Widening production `turbopack.root` to the D: drive is not a fix.
The original failure log remains `.build-check/session-resume/default-production-build.log`.

`QA/tools/default-web-build.cjs` creates an ignored run-specific physical copy
under `.build-check/default-web-build/<run>/app` on D:. Only allowlisted app inputs
and already installed dependencies are copied; no `.env`, package-registry config
or private-key/certificate inputs. Dependencies are read from main but NEVER
modified there. No dev server or second development environment is started.

The build uses a whitelisted child environment, explicit synthetic loopback API
origin, telemetry disabled and npm offline. It neither installs nor downloads
packages and does not inherit credentials, `NODE_OPTIONS` or model configuration.
This is not `npm ci`/fresh-install proof or an exact Linux image build.

The first copy staged21358dependency files/424007644bytes and then stopped at the
unchanged3GiB pre-build guard. No compiler started. After natural RAM recovery,
the same run `686f9a1434aa4a438b0f809408dc4e60` is resumed without recopy/overwrite:
physical-root checks, exact app hashes and every copied dependency's byte hash
must match before the guard and build. No Docker restart or guard reduction.
The sequential dependency verifier was stopped by exact owned PID14084 while it
had no children/compiler; exit-1 is not a build or verification pass. Replaced its
large-buffer sequential reads with four bounded workers/64KiB streams and the
same all-file SHA256 comparisons. A subsequent below3GiB start was refused, not
retried with a lower guard. The final helper queries OS free bytes directly rather
than allocating a separate PowerShell probe; admission is still exactly3GiB,
including rejection of invalid measurements.

The default compilation has a10-minute bound on its own spawned process tree.
Generated staging data remains ignored/local; no cleanup/reset/prune/delete of
application files or QA evidence. Existing PENTA containers/volumes are not touched.

## Additive tooling verification

Twelve focused tests PASS: exact3GiB/invalid memory boundaries, streamed all-file
verification and tamper rejection, secret/config input exclusion, child environment
allowlist, exact byte copy/source preservation, no overwrite, junction rejection,
path escape rejection, and static-byte/browser export-root guards. No original
browser assertion or diagnostic expectation is removed or relaxed.

Runtime runners accept the original export OR one exact successful isolated run.
The latter requires its successful default-build receipt and current source hashes;
arbitrary filesystem paths are rejected before browser launch or HTTP access.
Any runtime proof still uses a cached non-root/read-only/loopback Nginx container,
not an image rebuild, real model or API/SQL acceptance.

## Outcome

Isolated default build run`686f9a1434aa4a438b0f809408dc4e60` exits **0**, with
Turbopack explicitly observed and no webpack flag: compilation27.2s, TypeScript
35.3s, static generation87/87 in2.2s and automatic postbuild84/84 aliases.
The original217application input hashes still match after the build. All21358
copied dependency files/424007644bytes were verified before build admission;
no install/download, original worktree edit or main modification. The build's
10-minute timeout was not reached; timeout recovery is not claimed as exercised.

| Build/tool identity | SHA256 |
| --- | --- |
| `QA/EVIDENCE/default-web-build-686f9a1434aa4a438b0f809408dc4e60/result.json` | `991758C02A485B24BF973E8F5001EF0B7EC728ABE2FF18D0BE7288F37595B535` |
| Same directory, `build.log` | `6FE7E9B522ADAA95F8445057D3C41F4B20440C333A957F5D6B722EDD91D8DB1F` |
| `QA/tools/default-web-build.cjs` | `AE406E2F43C36DC04B0D31AB8FE86CA9ED1C571DCE090390B6E1161E2E98D42C` |
| `QA/tools/default-web-build.test.cjs` | `76C35359F295455AF01083903440F01A8F86FE9FE938AB6CEBCEBC50B68C83FC` |

Logs remain in`.build-check/session-resume/default-web-*`. Final12tool tests PASS,
plus all9retained static-segment preparation tests PASS. No assertions weakened.
Default-export runtime`434b97996b4647b9b6503ff2787f3309` passed628exact static
file/86HTML/84segment/seven navigation/negative404 checks, but its browser run
**FAILED** after two mobile cases: desktop1440px Send could not be clicked because
the conversation width collapsed. The run exits1 and its exact owned container
was removed. This is not a passing default-export runtime.
Turbopack and webpack emit different asset-file counts:628vs647does not imply
19missing routes; all current exported files,86HTML and84segments are tested.

## Desktop CSS ordering defect and bounded repair

Retained failed browser receipt: `QA/EVIDENCE/penta-chat-contract-1792661226231/failed.json`.
Its screenshot was visually inspected. Read-only geometry diagnostic
`QA/EVIDENCE/penta-layout-1792661543480/geometry.json` confirms the shell grid
had302.391px and1119.8px tracks despite the hidden Manual sidebar. The workspace
occupied302.391px and the conversation only2px; its internal grid read0px/270px.
No browser-console error was present. Thus compilation/static200 alone missed a
real desktop core-action failure, not a timeout to suppress.

Cause: `.aiShell` and global `.enterprise-app-shell` had equal specificity; the
default bundler's final CSS order selected the Manual two-track rule. The narrowly
scoped production repair in `penta-shell.module.css` uses
`.aiShell:global(.enterprise-app-shell)`, which outranks the global rule regardless
of source order. It does not change Manual layout, sidebar authority or permissions.
No bundler/root/global CSS setting is changed.

An explicitly injected diagnostic of that selector restores workspace1422.19px/
conversation1076.19px; it is NOT compiled repair acceptance. Separately,12 actual-CSS
source-order browser contexts PASS across320/768/1440px,both themes,both CSS orders,
including clickable synthetic control and restored Manual two-track/sidebar checks:
`QA/EVIDENCE/penta-shell-order-1792661634544/result.json`.
The original25compiled-portal checks remain; an additive desktop minimum600px
conversation assertion catches this regression before attempting Send. No force
click, timeout increase or removed assertion.

Repaired default build`bad444b6c61e45f680ed62f76d35dcf4` exits0: Turbopack
compilation52s/TypeScript16.4s/static87/87/postbuild84aliases. Source217pins remain
unchanged during the runner; only the scoped shell CSS differs from the preceding
build. Receipt field`applicationChanged:false` means the runner did not mutate its
application inputs; it does NOT assert that no application repair happened between
the two builds. Both original failing evidence and original successful compiler
receipt remain intact. No dependency/lockfile/root/bundler configuration changes.

Repaired receipt SHA256`4CCE0B2B7AD3F018CF823648CEB2E81261712E2CBE8362CE8194FAD30601FFF5`,
build.log`696BE663417B18282ECFCBC7DC9756F816E5CF61EFCFAB1D037607B11B413A4F`,
shell CSS`905950D28522B600AF35B71F4B4807330AD2B869CA9C6C00D56A3226C56252A5`.
Twelve CSS-order context receipt SHA256
`EC9CFBE3A232BF297A60C72F7A264147B01AD670D1BCBC0767849D4F5BC983CA`.
Retained57chat contract+13actual component-session assertions rerun PASS.

## Repaired compiled runtime — PASS

Run`6ee3936711bd4217b57f52b0747b5942` exits **0**, serving the exact repaired
default-build export above via read-only bind/plain-file Nginx (no segment rewrite):

- 628exact static-file responses/86HTML/84segment payloads/seven selected navigation
 URLs/one absent route+three absent segment404controls PASS.
- All25compiled chat contexts PASS across320/390/1440px and both themes, including
 the additive usable desktop-width assertion and real non-forced Send clicks.
 Zero unexpected browser errors; exactly3retained synthetic410/409/network logs.
- Non-root101:101/read-only root+binds/loopback128MiBcached-image restrictions retained.
 Exact owned container removed,29containers/41volumes and original2PENTA services
 remain. No image pull/build/push/restart/prune or existing container removal.

| Repaired runtime receipt | SHA256 |
| --- | --- |
| `QA/EVIDENCE/static-web-runtime-6ee3936711bd4217b57f52b0747b5942/result.json` | `4C780777E21F9568F9BF29A627F6929286765DB139BD44D9E4B578241B08CCE7` |
| Same directory, `runtime-postconditions.json` | `72B1C3B9BE0D55A3E651F8C84686CEEF2E417FD88162EE86389363D546CE2649` |
| `QA/EVIDENCE/penta-chat-contract-1792661749747/result.json` | `AD3A72B2B958FC651F5C71FE2F16443323EF1314224A8CFDE2B8CBFE29F8930F` |

Actual repaired1440px null-balance screenshot visually inspected: conversation and
composer use the wide column,context/Manual links stay in their right rail and the
expected malformed-receipt denial is visible. This is synthetic API transport, not
qualified Mini inference, native SQL/role/device or all-screen visual acceptance.
The12CSS-order contexts and25compiled scenarios are separate fixture scopes; repeated
25-case reruns are not counted as additional unique scenarios.

Final full configured frontend `npm run lint` exits0 with **0errors/0warnings**,
log`.build-check/session-resume/default-web-final-lint.log`. Syntax/PowerShell parser
and tracked/new-file whitespace checks PASS. No tracked main modifications, staging,
commit/push/deployment or Mini changes. Local evidence remains untracked and the
two isolated build folders remain ignored on D:; neither is a running dev server.

Prior647file/84segment/25chat, webpack87static-page and lint0/0 evidence remains
valid for its prior artifacts. Model independent review/qualification and existing
11broad delivery packages remain open at their outstanding gates.

Route: High for runtime/integration; Medium for settled documentation. No project
switch/new Core contract is needed; Mini's pending review gate is unchanged.
