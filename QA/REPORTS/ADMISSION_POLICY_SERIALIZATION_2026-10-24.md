# Prerequisite-policy/admission serialization — 24 October 2026

## Final scoped result — locally qualified, 24 October 2026

- [x]The missing actual-image payload inspection now PASS: run
  `b34599ec7bdf4fcdbe501ff377f41276`, window
  `e3eb6f719afa4b0bbc2384e02f463f7e`. Unchanged3GiB guard admitted at3.051GiB;
  all65files/93,735,819bytes match every local-publish path/byte/SHA256 entry.
  Inspection container never started and was removed; no SQL/API/browser rerun.
- [x]Exact original service IDs/images healthy/full inventories preserved,
  exclusive lease released. No active owned run. Independent policy successor
  qualification PASS against the successful payload window and earlier successful
  SQL child/full2927TRX/source delta.137focused QA-tool tests retained PASS;
  seven relevant negative-control/source/ownership tests rerun PASS this turn.
- [x]Earlier SQL parent remains FAIL for its supplemental inspection RAM refusal;
  payload-only refused window remains refused. Neither is relabelled successful.
- [ ]BUG-DATA-0006, remaining transition/device acceptance, standard source-
  Dockerfile release build, HIGH dependency, AI/model and operations gates OPEN.

Actual qualification command completed0:

```powershell
node QA/tools/admission-policy-qualification.cjs f42f065951044f48bf7e55b75bc9727b 7fa4aca70aba4b098f045b5ac3d770a1 e6f42852846249e38b51e47ee6c1fb4b fcdd4ac478f146efbe036ed974d7f6dc e3eb6f719afa4b0bbc2384e02f463f7e
```

SQL receipt SHA256 `da20cea017af96073519c7ea03a913b5aae77092cf4b2d7116a2e74e609341aa`;
original SQL-window receipt `ff2cba8a443768129f8f29b44c32e3a2d22d62d17f2409c0d818926552001992`.
Qualifier explicitly reports `sqlWindowPassed:false`, retained guard refusal,
separate successful payload window, `browserRerun:false`,
`standardSourceDockerfileValidated:false`, `productionApproved:false`.

Final safety readback: unchanged `codex/penta-search` HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`, staging empty, original211tracked
dirty paths preserved, `git diff --check` PASS. Main tracked clean at
`f7af512f884ca5fe8ac3ddb287c852492592e709`; Mini HEAD unchanged at
`2f8806967709a1a7ec29e5176e7c31968adbf6aa`. QA/EVIDENCE local/untracked,
.build-check ignored, no active validation lease. Original29containers/41volumes/
four networks/13images retained exactly for this inspection; only original two
PENTA services running.120issue records/120distinct IDs/zero missing-or-orphan
index references; none closed. No app source changes, commit/push/Azure/model/
production work during the final payload-only continuation.

## Previous bounded result (retained history)

- [x]Prior overlap reproduced on the unchanged rule image, not inferred from code.
- [x]One application path repaired: shared admission lock before graph lock.
- [x]Full2,927/2,927API tests PASS, zero failed/skipped;137focused tooling tests
  PASS, including the final supplemental-refusal negative control.
- [x]Repaired SQL `e6f42852846249e38b51e47ee6c1fb4b` PASS48HTTP setup/security/
  fixtures plus three native policy cases: actual blocked admission, prerequisite
  writer respects admission lock, required-audit500/no-change/recovery200 then
  denied admission409/no-write. Sequential capacity negative retained.
- [ ]Whole window `fcdd4ac478f146efbe036ed974d7f6dc` FAILS supplemental actual
  image-payload guard after successful SQL (runtimeExit0). Failure exactly
  `Unchanged3GiB guard blocked actual-image payload inspection.` No payload
  helper ran. SQL resources removed, originals healthy/full inventory/lease PASS.
  Preserve this failed envelope; it is not a successful complete window.
- [ ]Payload-only recovery window `75c4c7e33267462396ab53532cbc59a8` refused
 2.926GiB BEFORE helper execution against unchanged3GiB guard. Exact original
  services restored healthy/full inventories/lease release PASS; no active run.
  No further service cycling planned while memory is constrained.
  At that checkpoint actual-image payload/combined successor qualification was
  NOT RUN/OPEN. The successful later window above completes only that missing step.

At that checkpoint next safe execution was only the missing payload inspection, not another
SQL/API/browser audit. The policy qualification tool requires a successful
separate payload window and explicitly reports the retained original supplemental
guard failure; it cannot turn arbitrary failed SQL or restoration into acceptance.
No issue closure, production approval or claim that this image has browser proof.

Local Sol High continuation of BUG-DATA-0006/E1/E9; not a new audit program.
Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work.
No main/Mini/model/UI/permission/migration changes, commit/push/Azure or paid use.

## Exact missing boundary

Admissions already acquire transaction-owned
`AcademyDesk:EnrollmentAdmission:{academyId}`. Prerequisite edits independently
acquire `AcademyDesk:CoursePrerequisites:{academyId}`. The latter currently does
not join the common admission lock in the reproduced baseline, so pending policy
work could overlap admission. The local repair below changes this boundary.
This is a configuration/admission serialization gap, not proof that every
overlapping admission violates policy or that policy changes are retroactive.

The bounded intended fix is one authoritative lock order: admission first, graph
second, kept through the existing domain/required-audit commit. Graph cycle and
membership checks, approved source-course completion credit, duplicate/waitlist
policy and existing platform-owner bypass/audit policy must remain unchanged.

## Verification plan and current state

- Before changing application source, native baseline uses exact accepted
  development build `1ee2ef4724e345ac82eb8660c94f7992` with disposable SQL,
  authenticated real HTTP, original role/tenant checks and the repaired sequential
  capacity negative. Window `5659d3071d9e4cae9ad59c83915cc97c`, SQL
  `82286f21e2374d348bd871bc5c9c4610` FAIL before a policy-blocking observation;
 40setup/fixture checks completed. This is an inconclusive harness attempt,
  not product reproduction. Exact owned cleanup/original restoration/full
  inventory/lease PASS. Retained, not relabelled.
- Corrected baseline window `8f4f09749223477a8e1bf77800b305ba`, SQL
  `42a5ba9a78264d7099024f94ec827b1b` FAILS the intended invariant on the
  same unchanged application image. Gate readiness now checks an actual granted
  SQL application lock/session via SQL metadata rather than redirected sqlcmd
  output, which may be buffered. No assertion, timeout or domain rule relaxed.
  Actual writer blocked on graph; admission did NOT block on the writer,
  returned201 and inserted one enrollment while zero prerequisite rows existed.
  This confirms overlapping policy/admission, not retroactive invalidation of
  historical admissions.40HTTP setup checks completed; exact cleanup, original
  services healthy/full inventory/lease release PASS. Negative receipt retained.
- Implemented one bounded application change: AddPrerequisite acquires the
  existing academy admission lock before its graph lock. Existing graph checks,
  atomic audit boundary and platform-owner policy remain unchanged.
- Full2927/2927API tests PASS, zero failed/skipped, in guarded build window
  `7fa4aca70aba4b098f045b5ac3d770a1`.136focused QA-tool tests PASS, including
  negative controls for old201escape/incomplete blocking/wrong source/partial
  rollback and ownership checks. Build window PASS/exact originals healthy/full
  inventory/lease release. Fresh offline DEVELOPMENT image
  `f42f065951044f48bf7e55b75bc9727b`,
  `sha256:31321bbae6f4a8b82ae879a1fa2bd2864121fdf6b63ac4491b83198df0751070`,
  484source pins/65published files/93,735,819bytes. Independent source delta
  verifies ONLY AcademicGovernanceController changed since prior rule image.
  Native repaired retest passed as scoped above; later combined artifact/receipt
  qualification PASS as recorded in final result. Standard source-Dockerfile release
  gate is not substituted with this cached local-publish development build.
- A test-only SQL transaction holds the graph lock. Real AddPrerequisite must
  block on it; a new learner request then must block on the policy writer's shared
  admission lock. Assert actual SQL blocking session IDs, not timing alone. Once
  released, policy saves200 and the learner receives409/no admission row/one audit.
- A separate held admission lock must also block a real prerequisite write.
- Required-audit insertion failure must roll the new edge back; after recovery,
  the edge persists with one audit and admission is denied without another write.
- Gates are run-owned in the disposable database, bounded to20seconds, released
  in finally; ordinary HTTP transport retains its15second bound. No production
  lock, arbitrary model SQL, timeout relaxation or rate-limit bypass is involved.
- Existing normal-flow and27pair rule proof remains historical to its exact
  binary. Do not repeat every accepted audit or waive source pins.

## Release boundary

Completed payload-only step after genuine memory recovery (retained command):

```powershell
node QA/tools/api-runtime-window.cjs --approved-policy-payload-cache-stop-restore-24oct f42f065951044f48bf7e55b75bc9727b
```

Qualification was run only after that new payload window succeeded and restored
originals. Its exact successful invocation is in the final result above; template:

```powershell
node QA/tools/admission-policy-qualification.cjs f42f065951044f48bf7e55b75bc9727b 7fa4aca70aba4b098f045b5ac3d770a1 e6f42852846249e38b51e47ee6c1fb4b fcdd4ac478f146efbe036ed974d7f6dc <new-successful-payload-window-id>
```

The final argument must come from a real new receipt, never the refused payload
window. Both failed envelopes remain failed. The qualifier explicitly requires
runtime0/exact restoration/native child linkage and a separate successful payload
window; the original failure remains in its output. Do not rerun the completed
SQL/API suite just to recreate a combined envelope.

No frontend source changed or browser rerun performed in this narrow lock packet.
Existing browser evidence is historical to its exact prior image. Actual-image
inspection is a development payload heuristic, not full-image security clearance.

BUG-DATA-0006 and all broader release gates remain OPEN. A supplementary offline
development image does not qualify the standard source Dockerfile build. HIGH
braces, Mini semantic qualification/full90%AI, physical devices, qualified reviews
and operations/staging/release are separate. Model route remains Sol High for
this SQL/transaction work; Medium for settled visual/documentation tasks.
