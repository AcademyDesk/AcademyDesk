# Cloud and Desktop production-readiness work — 5 November 2026 IST

Owner authorized parallel work and feature-branch commits/pushes. This document
allocates work; it does not close a defect or authorize main merge, deployment,
environment publication or a model experiment. The 90% AI / 10% Manual target
remains unmeasured acceptance work. Both modes retain host authorization and audit.

## Verified shared source

| Repository | Launch branch | Independently verified base |
| --- | --- | --- |
| AcademyDesk/AcademyDesk | codex/ai-launch-readiness-20261104 | 9ea34e9e5d8600545aeac76999e16b85854a2387 |
| AcademyDesk/PentaMini | codex/mini-ai-launch-readiness-20261104 | 933a7bf4c5134a0f97f0bf8c76795db47ba64638 |

Cloud continuation branches are `codex/cloud-production-readiness-20261105` and
`codex/mini-cloud-production-readiness-20261105`. This first increment is a
documentation-only coordination and observed-validation checkpoint. It changes
no backend functionality. Do not confuse the documentation commit with the
tested source commits above. Later source changes require their own verification.

Desktop must inspect and back up the actual D: roots, linked Git metadata,
uncommitted/untracked/ignored work and local-only commits before pulling or
switching anything. Historical paths in handoffs are discovery hints, not proof
of the current directory layout. Fetch first, compare exact source and preserve
newer local-only work on separate desktop task branches. Never replace a dirty
local tree or merge preservation snapshots wholesale.

## Work ownership and acceptance

| Packet | Owner | Work and completion evidence |
| --- | --- | --- |
| C1: current audit and coordination | Cloud, completed by this checkpoint | Source-bound local results below; explicit blockers; shared work split and private Desktop instructions |
| C2: verification tooling | Cloud, queued | CI workflows, guarded SQL harness and evidence consumers; exact-source run/job/artifact proof once access works; three complete protected-host SQL cohorts, no omitted bridge |
| C3: backend and host repairs | Cloud, queued | Reproduce and repair accepted API/domain/security/transaction defects in bounded slices; preserve finance policy, host approvals, receipts and audit; relevant API plus real SQL/HTTP/race acceptance |
| C4: migration/release safeguards | Cloud, queued | Controlled migration/rollback tooling, private-storage/key configuration guards and recovery checks; implemented and tested source is separate from deployed infrastructure proof |
| C5: evidence portability and integration | Cloud, waiting for genuine Desktop inputs | Validate original hashes/provenance and review safe portable input handling; integrate reviewed source increments, never manufacture historical evidence |
| D1: D: source preservation | Desktop, first priority | Discover actual roots; backups and recovery verification; full status/HEAD/diffs and local-only history; identify changes absent from GitHub before any integration |
| D2: private evidence recovery | Desktop | Original required receipts, metadata and runtime-source artifacts; private manifest of paths/size/SHA256/provenance; no secrets, databases or model weights in GitHub |
| D3: local SQL/runtime acceptance | Desktop | Read-only inventory of intended development services; run existing guarded harness against fresh owned disposable SQL; source-bound logs, migrations/rollback, cohorts, cleanup and restoration evidence |
| D4: Manual and AI frontend/device acceptance | Desktop | Existing full local program and real browser/mobile/tablet/keyboard journeys against approved synthetic data; Manual remains usable during AI outage; no source edit overlap |
| D5: external connection validation | Desktop | Intended nonproduction storage/email/WhatsApp/calendar/meeting connection and consent checks using provider sandboxes/non-delivery checks; actual delivery requires an explicitly approved test destination |

Queued packets are not completed work. Desktop executes existing tooling but
initially does not edit cloud-owned API/domain/migration/CI/harness or private
Core files. Record findings and proposed affected paths in
`QA/REPORTS/DESKTOP_RETURN_2026-11-05.md` and the private return document. Before
either side changes a file owned by the other, record a bounded ownership
transfer. Separate branches alone do not prevent semantic conflicts. UI changes
also start with an explicit file list so cloud does not edit them concurrently.
This allocation is a handoff, not an observed lock or proof Desktop has started.

Each increment must record its base SHA, exact changed paths, result/exit codes,
remaining failures, output hashes and requested integration. Push only owned
source/tests and sanitized summaries to feature branches; no force push. Compare
remote tips before integration, inspect diffs and rerun affected checks. A later
push is not acceptance, a CI pass is not deployment approval, and overlapping
suite totals must not be added.

## Fresh cloud observations at the launch bases

| Check | Observed result |
| --- | --- |
| Full Academy API suite | 1,991 passed; zero failed/skipped |
| Current API dependency restore/audit | Passed with all NuGet vulnerability warnings treated as errors |
| Full frontend suite | 2,125 passed; 3 evidence-dependent communication failures; zero skips |
| Production webpack build and TypeScript | Passed |
| Frontend lint | Zero errors, 61 warnings |
| Release/preflight regression checks | 249 passed; no Azure requests |
| Actual release guard | Correctly blocks all five open P0 issues |
| Private engine | Focused model-free contracts pass; full suite still has failures/errors requiring genuine excluded inputs; details stay private |
| npm dependency audit | Timed out without a report; security result unverified |

Cloud replaced stale installed dependencies using existing declarations/locks;
no application, manifest, lockfile or assertion was changed in that audit. Raw
logs and full inventories remain outside Git in the cloud audit directory.
The smaller cloud suite cannot replace the larger preserved local testing
program. Historical source and results must remain distinguishable.

## Blocking gates remain

The register has 115 open records (5 P0, 97 P1, 13 P2), including partial repairs
and static findings; these are not 115 newly reproduced defects. Five P0 issues
remain `BUG-DATA-0001`, `0002`, `0003`, `0010` and `BUG-SEC-0001`. Respect each
issue's actual closure requirements and critical P1/security/tenant gates.

Student/teacher protected conversational writes still require Testing. Private
host acceptance needs complete PentaFoundation, PentaOnboarding and PentaTeacher
cohorts against the immutable host pin with actual Core scripted fixtures.
Synthetic qualification is not real model qualification or production authority.
Mini/Plus/Pro remain unqualified; Ultra deferred; stop before model acquisition,
loading, inference, training or activation remains controlling.

The preserved catalog has 317 declarations, 302 eligible actions and 15
exclusions, not accepted operational coverage. Reconcile current source and
define a workflow/role/language/device denominator. Acceptance needs real
conversation, current authority, clarification/correction, canonical preview,
required approval, domain transaction, audit, durable receipt and recovery,
plus correct Manual state/fallback. None follows from an endpoint count.

Production additionally needs private Blob/managed identity/scoped access,
shared encrypted Data Protection keys/certificates, controlled migrations and
compatible rollback/restore, provider delivery, full browser/device/staging and
operational acceptance. No main merge or Azure deployment is authorized.

## Access and preservation prerequisites

The runtime is running with restricted HTTP egress. `api.github.com` is saved
in the draft but not applied; exact current CI remains unobserved. Tailscale is
configured but TCP grants are empty, so laptop `100.98.98.13/32` is not granted.
These settings require the supported environment editor/operator; source work
can continue without exposing the laptop or enabling unrestricted internet.
Keep access Only me. Do not publish, delete or replace the environment until its
verified backups have an independently recoverable copy outside that environment.
The prior startup failure and disabled Publish cause are still undiagnosed.

## Desktop Codex starting instruction

Read this document and the private repository's
`operations/DESKTOP_CONTINUATION_2026-11-05.md` from their cloud continuation
branches before acting on older October handoffs. Fetch without overwriting the
D: originals. Complete D1, then D2/D3/D4/D5 using existing tests and safe local
data; return exact source-bound results and proposed changes on separate desktop
branches. Preserve all model and release stops. Use Codex / Sol High for source
reconciliation, security, SQL and transaction decisions.
