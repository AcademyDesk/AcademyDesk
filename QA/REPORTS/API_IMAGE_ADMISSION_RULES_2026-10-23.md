# Admission prerequisite and duplicate rules — local continuation

## Final scoped result — PASS, completed 24 October 2026

- [x]Approved host admission rules implemented;48new regression cases included
  in full2,927/2,927API suite PASS, zero failed/skipped.
- [x]Native SQL385HTTP checks,17rule outcomes,27barrier-released concurrent
  pairs and six real audit fault/rollback/recovery controls PASS; no429.
- [x]Four real390/1440light/dark browser journeys PASS:201save,409duplicate,
  200pause/200resume, exact SQL and three audited mutations per journey,
  optional-null/reason/cancel/pending-submit guards and eleven unchanged domain/
  Identity table hashes. No page errors/horizontal overflow. Four PNGs retained;
  actual390dark inspected/readable. Not physical Android/iOS acceptance.
- [x]Published actual-image payload:65files/93,735,407bytes exactly match every
  local-publish path/byte/SHA256 entry. Inspection container never started and
  was removed. Artifact run `0bd962c597124610b356eb89fc1e2e16` PASS.
- [x]130focused QA-tool tests and independent source-pinned successor
  qualification PASS. All temporary SQL/API/browser/inspection resources removed;
  exact original services/images restored healthy, full inventories preserved,
  exclusive lease released. No active validation window.
- [ ]BUG-DATA-0006, full source-Dockerfile/release, HIGH advisory, broader AI,
  enterprise, physical-device and operational gates remain OPEN.

Browser SQL `07996661494d4b8880c1a2540a9f038e`, window
`5c6c73314d444b09bdb8bae2212349a1` both PASS after exact finally. First read-only
qualification refused while restoration was still incomplete, correctly; no
assertion was waived. Qualification PASS only after final restoration finished:

```powershell
node QA/tools/admission-rules-qualification.cjs bf877dec849945d6883314979b7e63dc d57b18b87a9e4a67a96d724f9f6b087d 07996661494d4b8880c1a2540a9f038e 5c6c73314d444b09bdb8bae2212349a1
```

SQL receipt SHA256 `4f514379b6b96247c989ba932634aaa80b198869a80675697f3fca8d16acaf82`;
SQL window `97c56dca390cd8305095604588afe60913f062924861349cb018ad3243ba04cc`;
browser SQL `448860a769ffa050dbad0070e7ccb45aa80145fd60b387fada101f285a8f600f`;
browser window `c48445998d0cfdf592ece9d71a46f4806f9a067e087800f3ed0bde7bba226010`.

Final safety: branch/HEAD unchanged, staging empty, `git diff --check` PASS,
main HEAD `f7af512f884ca5fe8ac3ddb287c852492592e709` tracked clean, Mini HEAD
`2f8806967709a1a7ec29e5176e7c31968adbf6aa` unchanged, QA/EVIDENCE untracked/local,
.build-check ignored. Issue census120distinct records/zero missing or orphan
index entries; none closed. Both original services are the only running services.
All pre-existing29containers/41volumes/four networks retained;12images now include
the intended new development image. No prune, restart, credentials, push or Azure.

## Current scope and owner decisions

23 October 2026; Sol High; `D:\AcademyDesk-codex-p0`, branch
`codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus
preserved local work. Continues BUG-DATA-0006/E1/E9, not a new audit program.

Owner explicitly approved both policy choices in this conversation:

1. A promotion may count the source course that the same approved transaction
   completes. Every other required course must already be Completed.
2. Existing Active or Waitlisted admission blocks another admission for that
   student/batch. Activate the existing waitlist row; do not create a replacement
   alongside it. Completed/Withdrawn history is retained, not a duplicate blocker.

## Implemented

- Shared `EnrollmentAdmissionRules` enforces required completion against the
  same academy, student and academy-owned completed batch. Optional prerequisites
  do not block. This does not add recursive prerequisite-completion semantics.
- Create, activation, transfer and promotion reuse those deterministic checks
  under the already implemented transaction-owned academy admission lock.
- New admissions/transition into Active or Waitlisted reject another existing
  Active/Waitlisted row. The existing row is excluded when activating it.
- Entering Waitlisted checks enabled/spare waitlist capacity and active/open
  target. Already-Active or already-Waitlisted reaffirmations do not consume an
  extra place or retroactively requalify a historical admission.
- Promotion reads its authoritative source batch, then validates source-course
  completion credit before mutating anything. Source completion, target admission,
  promotion decision and required audit remain one transaction.
- No migration, model tool/permission, frontend, main or Mini changes. No history
  deletion, automatic duplicate repair, backfill or prerequisite policy migration.

## Verification checkpoints

- New48direct-controller cases BEFORE application repair:27failed/21passed,
  zero skipped. Retained baseline window `082a38ec18a74023a03a31c1140bdcf5`.
  Its expected failing suite caused window FAIL; exact originals/inventories and
  lease finally restored. Not a passing application result.
- Full API2927/2927PASS, zero failed/skipped; includes all48new cases and previous
  2879tests. Build window `f6e76747ec124684a62f02b8d1743ab7` PASS with exact
  original service restoration/inventories and only intended new image addition.
- Offline local-publish DEVELOPMENT build `1ee2ef4724e345ac82eb8660c94f7992`,
  image `sha256:166f49c001a1595d3d4c34933b80c3560735027a3bf96a7813e5c3bc079cbfc4`.
  Source-pinned cached runtime, no restore/network/package/model acquisition.
  This is NOT the standard application source-Dockerfile/release build gate.
- Native SQL window `d57b18b87a9e4a67a96d724f9f6b087d`, child
  `bf877dec849945d6883314979b7e63dc` PASS:385ordinary HTTP checks, original
  sequential capacity negative,15mixed-path capacity races/five audit rollback
  controls, plus17rule outcomes/12same-learner duplicate races and one atomic
  source-completion-credit audit rollback/recovery control. All27barrier pairs
  and six audit fault/recovery controls passed, no429. Fresh SQL state and audit
  counts exact. Owned cleanup, exact original services healthy, full inventory
  and lease finally PASS before the browser run started.
- 130 focused QA-tool/oracle tests PASS, zero failed/skipped. This selected
  tooling suite is not additional unique enterprise workflow coverage.
- Independent native-only and full successor qualifications PASS against actual
  immutable image/source, TRX, SQL, child-window logs, artifacts and browser PNGs.
- Affected browser and actual-image payload inspection window
  `5c6c73314d444b09bdb8bae2212349a1` PASS, exact finally as recorded above.

## Limits and next route

BUG-DATA-0006 stays OPEN. Native SQL and affected browser scope above passed;
remaining device/transition coverage,
prerequisite-configuration concurrent edits and broader release qualification
must not be inferred from direct-controller tests. Earlier capacity and19native
receipts remain historical to their exact API binaries; do not waive source pins.

Full enterprise, HIGH braces, Mini qualification/full90%AI, physical devices,
qualified policy/security/operations reviews and staging/release remain OPEN.
No commit, push, Azure, production access, model activation/training or paid use.
Stay Sol High for admission/security/concurrency and genuine runtime integration;
Sol Medium for settled visual/documentation work. No cross-project Mini contract
change is required by this host-only domain repair.
