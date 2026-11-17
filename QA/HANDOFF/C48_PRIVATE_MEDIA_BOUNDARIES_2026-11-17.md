# C48 — private media response and cancellation boundaries

Cloud / Sol High, 17 November 2026, Asia/Calcutta.
Branch: `codex/cloud-c48-private-media-boundaries-20261117`.
Clean base: current Desktop
`56ee768a6ef40ecf543e12e30cdeb93af2c84fc3`, verified by fetch.
Preserve its integrated C46, Next16.4.5 lock/security update and independent CI
jobs. C47 remains a preserved earlier combined checkpoint, not a replacement
for this newer source.

## Ownership before implementation

Cloud claims only:

- `apps/web/src/lib/private-material.ts` — bounded private-file response,
  argument and cancellation validation; preserve current authenticated paths,
  safe preview MIME policy and native download ticket handoff.
- `QA/tools/private-material-boundaries.test.cjs` — new runtime boundary tests.
- this task-specific handoff.

First reproduce meaningful failure cases, then change only the claimed client
helper if needed. Run retained media tests, types/lint, locked frontend build
and relevant browser checks. Do not change the domain/storage API, SQL harness,
database, signed/download credential protocol, shared session API, components,
CI workflow, model runtime, dependencies or root trackers in this packet.
This advances the client portion of BUG-SEC-0001; no whole-issue closure follows.

## Desktop coordination / immediately required prerequisites

The latest shared handoff reports a failed private paired checkout in hosted CI.
Configure `PENTA_SOURCE_READ_TOKEN` in AcademyDesk/AcademyDesk Actions secrets
using a secure credential with read-only Contents access to AcademyDesk/PentaMini.
The Cloud repository connection and a workflow's cross-repository credential
are separate. Never paste the credential into chat or commit it. Desktop owns
the current verification workflow and its required ordinary/paired jobs.

Desktop retains the protected API/SQL/migration, native browser/device and
staging acceptance. Recover original byte-pinned Mini archives listed in
`QA/HANDOFF/C47_INTEGRATED_ACCEPTANCE_2026-11-17.md` on branch
`codex/cloud-c47-integrated-acceptance-20261117` at91132efe; the raw inputs stay
private. Do not recreate evidence or alter archived identifiers/hashes.
Prepare the first real Mini qualification scope, retaining the explicit stop
before model loading/inference until the owner separately authorizes that run.

Desktop should avoid the two claimed Cloud source/test paths while this packet
is active and publish its own exact file claim before further edits. Return a
feature branch/commit, exact test results and remaining failures/prerequisites.
Use the newest shared source; preserve dirty work and all prior evidence.

No main merge, Azure deployment, production database access, public-access change,
environment publication or model download/loading/training/inference/activation.
Mini/Plus/Pro remain unqualified; Ultra deferred;90% coverage is unmeasured.
All six critical acceptance records remain open.

## Completed client packet — 17 November 2026

The ownership claim was published at
`592939df105df894a87dd5710458ad716e311b3a` before implementation.
Only the three claimed paths changed. Desktop's latest observed tip is
`f1f62158eb692564d50aeb60a36dad3c109b9c45`; its newer workflow, finance fixture,
Mini preparation and shared-report changes do not overlap the C48 source/test.
No older Cloud checkout was applied over Desktop's security/dependency work.

### Behavior

- Validate finite positive integer file sizes and buffer budgets before HTTP.
  Preserve the 32MiB preview boundary, 64MiB maximum JavaScript buffer budget
  and 2,000,000,000-byte private-file metadata cap. Larger permitted downloads
  retain the existing native browser handoff.
- Require complete HTTP200 responses for HEAD, preview and ticket reads. Reject
  unsolicited206, Content-Range, followed redirects and an unexpected response
  URL when transport supplies one. Normalize approved absolute file URLs to
  their existing canonical private path before authenticated transport.
- Validate canonical Content-Length and compare complete GET size with checked
  metadata. Keep chunked responses supported. Compressed JSON is capped by its
  decoded bytes; its compressed Content-Length is not treated as decoded size.
- Check cancellation before requesting, after response validation, after every
  awaited stream read, at EOF and after cleanup. Abort cancels a pending reader;
  all readers release their locks. Rejected/error responses discard their bodies.
- Bound ticket responses to8KiB before JSON parsing, require the existing exact
  two-field envelope and60-second expiry, and retain valid tickets up to3,000
  characters. Malformed JSON reports a fixed message without response contents.

The server still independently resolves current account, role, tenant, resource
and credential scope. No domain/storage API, SQL transaction, native-ticket
protocol, component, shared session transport, source manifest or lockfile changed.
The native POST body still contains only the scoped short-lived credential;
no account token is placed into a URL. Safe preview MIME detection is unchanged.

### Verification

Private raw logs, scripts and receipts:
`/workspace/c48-private-media-boundaries-20261117`.

| Check | Actual result and scope |
| --- | --- |
| Failure reproduction against original helper |60 tests:23 PASS /37 FAIL, zero cancellation/skip |
| Final boundary regressions |62 PASS, zero fail/skip; includes two additional cancellation/maximum-ticket controls |
| Retained class-media client suite |28 PASS, zero fail/skip; unchanged assertions |
| Full current frontend |92 files /2,562 tests:2,559 PASS,3 FAIL, zero skip/cancel |
| Three retained frontend failures |Email, WhatsApp and Meeting historical HTTP receipts require missing `QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`; no receipt, expected value or skip was fabricated |
| TypeScript / lint |PASS; zero lint errors,61 inherited warnings |
| Exact-lock production webpack export |PASS with Next16.4.5/React19.2.8,90 static segment aliases |
| Dependency advisory check |npm audit reports0 findings across all severity levels on the committed graph |
| Native Chromium helper probe |4 groups PASS: actual68MiB native disk download with exact size/SHA256, unsolicited partial refusal, pending-read cancellation/lock release and oversized-ticket refusal |
| Browser scope/cleanup |Sandbox enabled, run-owned browser profile and fixture processes cleaned; synthetic file retained privately. Actual helper/DOM/native download with loopback synthetic API, not real Identity/SQL/device/storage-provider qualification |
| Backend |C47's2,493 PASS retained, not rerun: complete `apps/api` and API-test Git trees are byte-identical to C47. No new backend/SQL qualification claimed |
| Production guard |15 tests PASS; actual guard still exits1 for all six critical acceptance records |

The initial test harness had a syntax error and a stream fixture that did not
close; these were corrected before the complete60-case baseline was recorded.
Those initial logs remain preserved. The final62-case runner and retained tests
complete without unresolved readers or cancelled cases.

The previously installed16.4.3 node_modules was preserved at
`/workspace/.dependency-cache/web-node_modules-next16.4.3-c48-20261117`.
Restored the existing committed lock with `npm ci --ignore-scripts --no-audit
--no-fund`, then ran the explicit npm audit. This installed the already-approved
16.4.5 graph; dependency source files and the lock were not modified.

### Current Desktop handoff and remaining acceptance

Desktop has acknowledged the C48 ownership. Its newer handoff reports recovery
of the seven direct Mini archive inputs, verification of11 retained original
hashes,12 proposed Mini scenarios and no weight loading. Those are Desktop's
source-bound results; the earlier missing-input list is coordination history,
not a new request to reconstruct or resend recovered inputs.

Integrate only C48's three paths into the current Desktop feature checkpoint.
Run the62 boundary and28 retained media tests using the established isolated
compiler override if needed. Then verify real authenticated HEAD/GET and native
download on disposable SQL/private storage: full and chunked responses,
401/403/revocation, cancellation and lost network, legacy and Blob resources,
large native downloads, current role/recipient scope and actual iOS/Android
devices. Retain current server authorization, range support, mandatory audit and
ticket binding. C48's preview reads request full files; server range delivery
for authorized native clients is not changed or disabled.

Desktop owns current hosted CI and protected native tests. At the last handoff,
the paired Core job still required the secure `PENTA_SOURCE_READ_TOKEN`; both
required verification jobs must pass on the exact integrated source. A preview
status or successful local frontend build is not release CI acceptance.

`BUG-SEC-0001` remains OPEN for the wider private-media closure protocol.
Collection, adjustment consumers, payroll, void/restoration and broader session
revocation also remain open. Exact candidate images/security, physical devices,
staging migration/restore/rollback and actual private-model qualification remain
required. Do not promote guided or synthetic outcomes into the90% numerator.

SOURCE CONTINUATION / NOT PRODUCTION QUALIFIED. Environment privacy/network/VPN
settings were not changed. No main merge, Azure deployment, model download,
loading, training, inference or activation occurred. Mini/Plus/Pro remain
unqualified; Ultra deferred; the owner model-load stop remains in force.
