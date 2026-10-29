# Batch1 — transaction-ordered media publication and private recovery

Date:2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. Owner's “next” continues the recommended boundary stated at turn start: **block resource/notification publication after revocation, retain nonpublic received bytes for authorized recovery**. Not cancellation of every already-admitted provider write. Batch1/BUG-SEC-0001/BUG-FUNC-0007 remain OPEN.

## Repair

`ClassMediaUploadsController.Complete` validates same-store configuration before Blob commit. After provider IO, it begins a Serializable SQL transaction, enlists the existing scoped Identity context using `AcademyIdentityTransaction`, locks the academy for publication ordering, and rechecks the persisted teaching scope followed by fresh account/stamp/role/owner association. Domain-before-Identity ordering matches linked domain updates. The transaction retains authority read locks until publication commits; Blob transfer itself holds no SQL publication locks.

Denied publication returns404 without setting CompletedAtUtc or inserting a resource/notification. Any already-committed Blob stays private and bound to its original pending upload; normal reads require a published LearningResource, and restored authority can retry completion without reuploading. Original atomic/idempotent claim and Blob/SQL failure-recovery behaviour remain in place. No schema/limits/UI/Mini/Core changes.

`CurrentTeacher` now reads the current account with AsNoTracking, compares the authenticated stamp, and queries current role membership rather than using a membership tracked before provider IO. A first implementation run caught the stale-membership case; the final direct association query fixes it. The [.NET10.0.12 Identity UserStore source](https://github.com/dotnet/aspnetcore/blob/v10.0.12/src/Identity/EntityFrameworkCore/src/UserStore.cs) confirms IsInRoleAsync uses FindUserRoleAsync/FindAsync, which can reuse the tracked membership. No model-supplied authority is introduced. Extracted teaching-scope helper is explicitly domain-only, not an actor permission grant; new unit proof keeps inactive accounts denied.

## Native SQL/API/private Blob acceptance

Run `7340a7d158994fc6a503cd8bf723646a`,98.9s/exit0,SQL loopback53503. Original75 Blob assertions PASS, including concurrent completion, private/native credential controls and recoverable SQL failure after Blob commit.

New nine coordinated cases/74 additive HTTP/no429:

| Revocation | Admitted operation | Outcome while revoked | Recovery |
| --- | --- | --- | --- |
|Teacher/academy/batch (3 cases)|stage|200, nonpublic staged bytes only; no resource/notification|same upload completion200|
|Teacher/academy/batch (3 cases)|complete|404, private committed bytes retained; no resource/notification|same upload completion200|
|Account/Teacher role/security stamp (3 cases)|complete|404, private committed bytes retained; no resource/notification|same original token/session completion200 after exact restoration|

All nine cases verify unpublished Student content GET404/no-write while revoked, new completion denial (account403/stamp401/other404), unchanged SQL/Identity authority/session metadata, prior resource/notification/committed-Blob preservation, and actual private provider progress. Restoring authority publishes exactly one original-scope resource and one intended Student notification. Enrolled Student reads exact three original bytes; repeat completion200 leaves exact state unchanged. Previously committed recovery Blob ETag remains unchanged. Safe authority projection adds stamp hashes internally; no raw stamp or credentials printed. This is not a snapshot of every application table/Identity column.

At a separate publication SaveChanges boundary after authority reads, six real SQL revocation UPDATE/DELETE attempts (academy,Teacher,batch,account,role,stamp) each fail with LOCK_TIMEOUT1222 while publication is held. After release the publication returns200. This establishes actual database lock ordering for those named rows, not a “still pending after250ms” inference. Timeouts are intentional controls, not hidden failures. These are low-contention disposable SQL tests, not whole-application deadlock/high-load certification or cancellation of already-transmitted download bytes. Student/enrollment/class-session predicates remain enforced by the shared domain policy but their concurrent editor timing is not newly tested here.

90 application +7 Identity migrations/root HTTP/two-tenant/non-admin runtime/mismatched-marker cleanup refusal/exact owned private Blob/database/login/two-container cleanup PASS. All nine pre-existing containers retained; no production/dev DB/Azure credentials used.

## Validation and retained failure

API tests **1485/1485,zero skipped**, final run19s; includes one new domain-helper/actor-denial check. First API run also1485/1485 (40s), but did not detect the subsequent native role-removal failure—unit counts alone are not revocation proof. Fresh publication harness builds1m51.51s then39.76s,zero warnings/errors. Fresh recorder harness13.74s/zero warnings-errors. PowerShell parser/five mixed-mode guards/orphan-publication pre-provision refusal/diff/scoped high-confidence secret-pattern scan PASS. Original83-page export/56 client checks retained, frontend unchanged; no redundant whole enterprise audit.

Failed first publication run `a8b97bb715694f4abc1a998213a38c3d`,128.3s/nonzero,passed original75 and seven new cases,then role-removal status oracle failed. Source review found the tracked-membership lookup and replaced it with a real current association query. The failing assertion was not loosened; final role/stamp and lock controls passed. Evidence kept; only its exact name+label+single-loopback SQL/Blob containers removed. Previous six-case strict cancel-all-writes source/failed receipt remain untouched, **not reclassified as passing**; private stage completion is an explicit boundary difference, with publication/no-notification/hidden-byte/recovery assertions added.

Receipts: `QA/EVIDENCE/batch1-20261012/sql-media-publication.txt`, `sql-media-publication-final.txt`, `tests-media-publication.txt`, `tests-media-publication-final.txt`; nine-case receipt `QA/EVIDENCE/media-publication/7340a7d158994fc6a503cd8bf723646a/receipt.json`. No staged files; ignored builds/local evidence preserved; feature/main SHAs unchanged.

## Linked browser retest

The unchanged exported Teacher recorder/upload/history/authorized-preview driver passed against a freshly built copy of the **final changed backend**, not the preceding cached harness. Run `30645035ab98431faef298abf8bb89f2`,147.4s/exit0: four320/1440px light-dark native fake-device journeys/explicit201-chunk200-complete200/success-reset/history/protected preview-WebM playback/exact recorded-saved hash/Student API200/anonymous401/foreign404/unassignedTeacher404/four no-write completion retries PASS. Recorded bytes34767/28971/28971/28971.101 HTTP/93 unchanged snapshots/eight exact allowed create-complete deltas/correct notifications/committed native-byte hashes/no429 plus original75 Blob assertions/90+7/owned cleanup PASS. All nine original containers remain. Desktop Classroom setup precedes target-width recorder steps; not mobile navigation acceptance. Final320-dark saved screenshot inspected: correct success/reset/history/preview; fixed navigation/history overlap remains Batch2. Physical devices/Student UI/native Download/real hardware/network NOT RUN. No personal microphone access.

Receipt: `QA/EVIDENCE/batch1-20261012/sql-recorder-upload-publication-retest.txt`, `QA/EVIDENCE/recorder-upload/30645035ab98431faef298abf8bb89f2/receipt.json` and four screenshots. Earlier accepted browser reports remain historical. Teacher export/uploader-source hashes match preceding receipts; no frontend change or export rebuild.

## Remaining / next

Bounded publication/recovery gate PASS, not issue/Batch1/release closure. Remaining scoped candidate review includes optional teaching-scope concurrent edits, wider lock/deadlock/cancellation/load behaviour, physical Android/iOS/native network/device capture and operational/release gates. Next Sol High these consequential integration checks; later Medium settled Manual Orbit+Aura dark/Prism light four-portal visuals after actual versioned PENTA design handoff. PENTA AI frontend remains its own project. No new Core contract/handoff/project switch, commit/push/main merge/Azure.
