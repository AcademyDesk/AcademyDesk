# D56 Desktop release readiness — 23 November 2026

## Sources and integration

Fetched current AcademyDesk integration tip
`c3617f28d458287189ca7173c8fe26ddea8fa271` and Cloud C55
`ea7da00781666732ff2e54340fa825ce987f2a47` before changes. C55 already landed
once in `8077a566957a5363804a87b59a410c69d48dd428`; no replay/cherry-pick.
C54, Student 360, SQL harness, Windows browser helper and Mini grounding remain
preserved. This batch changes QA/tools and documentation only; application,
vendor, lock, migration and workflow bytes are unchanged against c3617f2.
The two existing untracked QA paths remain untracked, including QA/EVIDENCE.

Mini serving source remains `4732116bc9a8c4eb9904ee7c4fc09100866816f6`.
The reconciled provenance/Core CI branch remains
`2686d90e3d3fb57e58889e09969a336312e6d9a5`. Existing C1/C5 and 33 frozen
original byte seals are preserved. No model training/promotion, new candidate,
package/weight download, main merge, production DB or Azure access.

## Current exact-SHA CI, not inferred from historical passes

| Source/run | Ordinary verification | Required paired Core | Overall |
| --- | --- | --- | --- |
| 69a5794 / [45112955162](https://github.com/AcademyDesk/AcademyDesk/actions/runs/45112955162) | Cancelled | Failed: private source credential | Cancelled |
| c52613f / [45114171534](https://github.com/AcademyDesk/AcademyDesk/actions/runs/45114171534) | Passed | Failed: private source credential | Failed |
| 8077a56 / [45118735015](https://github.com/AcademyDesk/AcademyDesk/actions/runs/45118735015) | Not a completed accepted result | Not accepted | Cancelled |
| c3617f2 / [45119449295](https://github.com/AcademyDesk/AcademyDesk/actions/runs/45119449295) | Passed, job137509628906 | Failed, job137509629210 | Failed |

Read the actual completed c3617f2 job logs and steps. Results: all 2,600 API tests
passed, zero failures/skips; 93 current frontend files, 2,637 passed, zero
failures/skips/cancellations; 26 guarded SQL modules; private Blob suite75
assertions; TypeScript/lint, web build, API image build and container smoke pass.
Payroll browser uses two viewports/four PDFs and assessment eight grouped checks
with synthetic intercepted transport; not physical-device/native-browser proof.
API smoke verifies startup, unavailable DB503 and anonymous academy read401.

The completed full npm audit reports zero vulnerabilities. NuGet restore audited
all dependencies with NU1900–NU1904 treated as errors and passed. Older picomatch/
other candidate audit failures remain historical, not current c3617f2 findings.
No dependency update, audit exclusion or assertion suppression was introduced.
The current source and latest publication still need their own exact-SHA result;
do not relabel a prior job as a future candidate pass.

The paired Core step still stops at **Require access to pinned private Core
source**. There is no dedicated token in this process environment and the GitHub
connector has no Actions-secret administration capability. Do not reuse a broad
Git credential. Owner must securely configure PENTA_SOURCE_READ_TOKEN in
[AcademyDesk Actions secrets](https://github.com/AcademyDesk/AcademyDesk/settings/secrets/actions),
with read-only Contents access to private AcademyDesk/PentaMini, then rerun the
required failed job on the exact candidate. Never paste its value into chat.

## Additional C55 native boundary acceptance

Rebuilt the actual SQL harness: zero warnings/errors. Fresh owned SQL run
`0477ec18d09e48a9972abdbe806e8545`, wrapper
`d3e58829eae54edfa6dc9b632cf150ec`, passed on the working source hash below.
86 application and 7 Identity migrations applied; scoped runtime login and real
Identity/API/SQL paths were verified. No model needed for this controlled-provider
security/receipt suite.

- [x] Protected POST/GET preserve one-cent and large separate-currency strings.
- [x] Discarded POST reply leaves one turn/one dispatch; recovery is GET only.
- [x] Legacy encrypted original decimals recover during unavailable provider.
- [x] Added actual protected legacy GET for 0.001: numeric original unchanged,
  no rounding and no unsupported exact-money claim. This is an invented legacy
  receipt fixture, not a three-decimal write into a two-decimal SQL ledger.
- [x] Added actual protected near-40,000-character legacy GET: a valid original
  fits, its hypothetical optional projection exceeds the existing limit, so the
  projection is omitted; returned wire JSON stays within the original bound.
- [x] Current actor/tenant/module/role/account/referenced-learner restrictions,
  malformed/stale/expired/onboarding snapshots, required audit failure/rollback
  and no financial-result disclosure remain exercised by the retained preflight.
- [x] One planner dispatch; unchanged invoice/payment counts and totals; mandatory
  replay audits; original encrypted fixture restored after each boundary case.
- [x] Removed only this run's owned DB/login/container; both original services/
  image IDs and complete container/image/volume/network inventories restored;
  API health200. Earlier bridge-ID drift is not erased or silently whitelisted.

Private result: .build-check/penta-host/mini-exact-money-native-d3e58829eae54edfa6dc9b632cf150ec/result.json
SHA256: `81df35ff5931966f50c78895d76144081aa01e765c37e682df4f610dc34365c6`.
Test source PentaMiniExactMoneySql.cs SHA256:
`e0ef2d42e86c905480555f9bc6424ba91c2ce1a0293713dcf31b493b8054180a`.
The earlier 837 PENTA API/1,020 client/214 browser C55 passes remain separately
scoped evidence in PENTA_C55_COMBINED_ACCEPTANCE_2026-11-23.md. The hosted 2,600
and 2,637 counts are full-suite results and must not be added to those subsets.

## Demo and retained failure

The preceding demo00e3a6 stopped itself on its unchanged low-RAM safety guard and
restored originals. Its 13 checks were not represented as a still-running service.
Restored run33a41e91 passed all13 real-model/Identity/SQL checks: six identities,
nine pending learners, two Piano follow-ups, exact INR2250.00, GET recovery and
optional-fields-empty Manual save. First measured query26.895seconds locally.

An additional real Core outage test passed11 application assertions, including
GET-only exact recovery, anonymous/Teacher/Student/tenant denials, Manual save and
unchanged SQL ledger/turns, but **failed restoration** because Docker stop/start
reassigned the dynamic Core loopback port. Its private recovery receipt remains
failed. The supervisor also exposed a stop-file cleanup race after SQL removed
its own root; the private launcher now tolerates that specific already-cleaned
root. Exact run-owned cleanup was repaired and originals restored.

Before stopping that synthetic fixture, retained a COPY_ONLY/CHECKSUM SQL backup
and its original private key ring under .build-check/d56-20261123/preserved-demo-33a41e91a0934b45a5dd5ce3a34a804b.
RESTORE VERIFYONLY passed; this is not a real restore drill. Backup745,472bytes,
SHA256 `03191b4b4e3683f7cef850ef1093ea00009c7d43c84bd381ad746783540a428e`.
No customer data, raw backup or keys are uploaded. The corrected test uses
pause/unpause and verifies stable Core port bindings plus unchanged inference
StartedAt; it does not reload the model or send another model request.

Final run `12994a6ab57b40f2b333f6efc554f2ba` passed all13 real-model/Identity/SQL
checks again (first query26.242seconds), then all11 corrected outage assertions.
Core availability restored, port bindings and all resource inventories unchanged,
inference container StartedAt unchanged, and no extra model request during the
outage test. Two invented Manual saves remain in this fresh fixture (14learners).
This is API/SQL and static-route acceptance; not a fresh visual/physical-device
browser pass. It does not erase the failed stop/start restoration receipt.

Current URL: http://127.0.0.1:3000/login. Generated credentials remain private in
.build-check/interactive-demo-12994a6ab57b40f2b333f6efc554f2ba/LOGIN.txt.
Demo expires24November2026at03:43IST or earlier on shutdown/low-RAM safety stop.
Use the current demo handoff for startup and safe stop, not an older login file.

Final private receipts under that current demo folder:

| Receipt | SHA256 |
| --- | --- |
| verification.json | b384bb917ba39e5f0fa6abb1430470216befbfc97904a54898872db5569a27d5 |
| recovery-f310afb7ae104c91be278ed90d24ed6d.json | 236aeb4b2ebbe4197ee03f78e87a8ef414daf1cde33adc177ca880fa0dbac1b4 |
| .build-check/d56-20261123/ci-observation.json | ea208c7d15c2d1bb5c84d637dc2e52e6b34bd60dd646ccaad2991e5e9539aa9d |

The last receipt is under the repository root rather than the demo folder.
No further heavy local suite runs beside the demo. Final demo shutdown is unrun
while it remains available for the owner; do not predeclare its cleanup a pass.

## Mini full-suite boundary

[Mini45118369025](https://github.com/AcademyDesk/PentaMini/actions/runs/45118369025)
on2686d90 reports1,748passed/13failed/63errors/1skip and5subtests passed. Final
failure/error summaries require genuine private originals absent on that hosted
runner. All seven direct C47 inputs exist locally and the genuine62-record v1
verification remains recorded with14exposed records excluded and no qualification.
The separate Windows full-suite collection failure requires Linux fcntl; neither
result is hidden by selected tests, fake archives, path aliases or changed hashes.
Secure runner-side archive provisioning and the remaining full-suite acceptance
are open. Mini/Plus/Pro stay production-unqualified; Ultra is deferred.

## Remaining blockers — owner and acceptance condition

| Record/gate | Owner | Required final evidence |
| --- | --- | --- |
| BUG-DATA-0001 | Desktop/native + device operator | Current collection/linked reader/recovery critical chain and physical-device acceptance; existing race/current hosted SQL passes do not close it. |
| BUG-DATA-0002 | Desktop/native + device operator | Adjusted balances/approval/collection/Guardian consumers and linked critical/device chain, with unchanged finance/audit evidence. |
| BUG-DATA-0003 | Desktop/native + device operator | Payroll period/net/idempotency/audit/recovery chain plus linked critical/device acceptance. |
| BUG-DATA-0010 | Desktop/native + device operator | Voided/restoration/reconciliation evidence, correct invoice recomputation, linked critical/device acceptance. |
| BUG-SEC-0001 | Desktop/native + actual Android/iOS/laptop operator | Private media upload/record/resume/reload/download, hashes, credentials and revocation across actual devices and actual Azure private Blob staging. |
| BUG-AUTH-REVOCATION-001 | Desktop/native + device operator | Broader SECURITY-SESSION-001: roles, cross-tab/logout/refresh/2FA/lockout/portals and actual phones; targeted repair alone is insufficient. |
| Paired Core CI | Repository owner, then Cloud | Dedicated read-only credential configured securely; required job and complete workflow green for exact candidate. |
| Mini originals/full suite | Mini/Cloud + private artifact custodian | Original bytes/parents available securely on a permitted runner, unchanged expected hashes, full results accepted without fabricated evidence. |
| Production AI transport/coverage | AcademyDesk + Mini security owners | Approved production private/authenticated transport with host enforcement, all intended role/workflow outcomes and measured eligible90%; no Testing-environment bypass. |
| Azure staging/operations | Infrastructure owner + Desktop/native | Actual configuration inventory, private connectivity, TLS, keys, probes, redacted logs/alerts, controlled migrations and isolated SQL/Blob/key restore and rollback; explicit staging authorization/resources required. |

All six issue records retain their existing OPEN acceptance state. Production
guard15tests pass; running the real guard correctly exits1 with all six required
records unaccepted. No physical phone, Azure resource/session or deployment was
available to turn unrun requirements into passes. The 90%eligible-outcome target
remains unmeasured; a read-only learner demo and12/12 narrow result do not qualify
conversational CRUD, every portal or all model tiers.

## Azure preparation and next handoff

[Reviewed staging/runbook](../../docs/production-deployment.md) corrects the old
automatic-main deployment claim and maps existing settings, managed identity/
secrets, private SQL/Blob, TLS, probes, logs/alerts, migration, restore and rollback.
No paid resources provisioned, production data touched or Azure workflow run.
Follow the current D56 handoff and updated Cloud start/work plan. Sol High for
production transport/security/native blockers; Sol Medium for routine pipeline/
documentation work. Preserve existing Cloud ownership and claim before overlap.
