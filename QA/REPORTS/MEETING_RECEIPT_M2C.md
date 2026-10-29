# M2c — durable protected meeting-draft receipts and review

2026-10-15. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD `b1f8cb6`.
Continues [M2b](MEETING_DRAFT_HTTP_M2B.md). Records draft review,NOT authorization to invite,create a provider meeting orexecute PENTA work.

## Implemented

- `MeetingPreviewReceipt` table stores initiator/academy,key,input/source fingerprints,expiry,status,optimistic concurrency version andData-Protection-encrypted authenticated envelope. Envelope purpose binds academy/actor/receipt ID;immutable andmutable stored fields are integrity checked against protected content. No plaintext title,attendee emails orcredential material persisted in receipt columns. Input references/title inside the envelope remain private academy data requiring existing protected key-ring/retention release controls.
- Three authenticated no-store routes:POST `/meetings/preview-receipts` withbounded `Idempotency-Key`;GET `/meetings/preview-receipts/{id}`;POST `/{id}/review` withstrict expected version/request/source hashes. Creation/review use existing opt-in atomic domain+audit filter,including the platform-owner path,while independently retaining Owner/Admin/tenant/module checks. Other admins in the same academy receive404 for an initiator's receipt;foreign routes403. Existing synthetic PENTA approval system unchanged.
- Prepare validates current draft andsource,then saves receipt andrequired actor audit. Same key/same input/source returns the same receipt;changed input/source conflicts;SQLunique actor/academy/key constraint protects concurrent insertion. A SQLduplicate-insert conflict returns409,no automatic retry. Parallel prepare races are constrained in schema butnot separately exercised this batch.
- GET/review decrypt andverify integrity,expiry andcurrent authority/source eligibility. Review requires exact displayed version/hash,changes AwaitingReview→ReviewedDraft,versions0→1,andrecords audit. Replays/stale/changed/tampered data fail closed. Optimistic concurrency allows only one successful review;no second review audit. Expired receipts remain stored as evidence;they are not renewed ordeleted by a GET.
- Every public state remains `CanExecute:false`. ReviewedDraft means this initiator reviewed the draft only,NOT a policy approval for external sends,guests,recording,bulk automation orprovider execution. No dispatcher,outbox,operation store,provider credentials orfrontend controls enabled. Qualified provider capabilities remain unavailable.

## Migration and existing work

Added `20261015111822_MeetingPreviewReceipts` migration/designer andupdated existing domain model snapshot. Scaffold adds only receipt table/indexes;the earlier uncommitted payroll nullable PaidAtUtc snapshot change is retained. No Identity schema change. EFscaffolding is source generation,not database deployment;SQL migration runs only target owned disposable QA database.

Down migration explicitly blocks if ANY receipt exists,preventing deletion of review evidence. TheSQLtest adds/removes only its exact synthetic migration-probe row in its run-owned database;no customer/history cleanup. Empty down/up is tested. Production migration/backup/restore/retention/keyring qualification remains a release gate.

## Verification

-155selected tests PASS:18new receipt tests plus80meeting/57communication tests. Cover protected storage,actor scope,expiry,request/version/source mismatch,idempotent replay,changed source/revoked authority,field tampering andnon-executable review. Unitreceipt tests use existing Identity/InMemory fixture andephemeral protection provider;notSQLconcurrency proof. TRX:`QA/EVIDENCE/meeting-receipt-20261015/receipt.trx`.
- Isolated harness build PASS,zeroerrors/four unresolved NU1902/NU1903 Pkcs9.0.11 warnings. EFmodel consistency command reports no pending changes. Scaffold/model validation also warns about existing unconfigured decimal mappings PassingPercent/AdjustedAmount outside this newtable;no unrelated finance mapping rewritten. Neither warning category is waived by these tests.
- Final realHTTP/Identity/SQL run`2a1d9499165d4d42a09b34c5ec4bc1d7`,loopbackport50263:58/58cases PASS(21readiness+20draft+17receipt),no429,runnerexit0 in109.2seconds. Existing91application/7Identity migration inventory,runtimeleast-privilege andtwo-academy controls passed.
- ReceiptSQLcases verify prepare/same-key replay/changed-body conflict,ownedGET/other-admin/foreign denial,wrong version,review/replay,race prepare,simultaneous review exactly200/409 withone review audit,audit-fault preparation plusinjected required audit failure500 withreview/state rollback,clock-advanced expiry410,SQLstatus tampering409,andstale-source review409. No notification/provider action.
- Migrationchecks PASS:previous→new upgrade,populated downgrade rejected while row survives,exact synthetic probe removal,empty downgrade/re-upgrade. No actual production downgrade performed.
- Receipts:`QA/EVIDENCE/meeting-readiness-sql-2a1d9499165d4d42a09b34c5ec4bc1d7/result.json`,`QA/EVIDENCE/meeting-preview-sql-2a1d9499165d4d42a09b34c5ec4bc1d7/result.json`,`QA/EVIDENCE/meeting-receipt-sql-2a1d9499165d4d42a09b34c5ec4bc1d7/result.json`. Finalsource hashes verified. Runtimeinventory331routes/320controller method-routes/10Identity method-routes;hash`4CB0A30AEEE83AF0D443D650A2E40166E2B54B524C982AE3955AEFF219862B81`.
- Only exact ownedSQLdatabase/login/container andguard-owned testhost scratch removed. Both PENTAcontainers retained;allQAevidence preserved. Frontendunchanged,priorvisual proof retained notrerun.

## Limits and next route

Durable receipt/review foundation is delivered;M2overall isNOT complete. No consumed-once execution permit,operation/outbox/dispatch,reconciliation/cancel/history,production keyrotation/restart/retention/load/rate-limit qualification ormid-read source-concurrency authorization guarantee. Fingerprints+review never replace current revalidation at future execution. Final approval/actor separation rules for actual external actions remain policy-gated;this content review grants none.

Next Sol High:resolve the already-observed moderate/high transitive cryptography dependency advisories withcompatible pinned resolution andaffected build/tests,then M2durable-operation/outbox/recovery foundation. Keep provider dispatch disabled until M3connection/adapter/consent/approval qualification andM4Manual/PENTA integration/enterprise gates. No new Mini contract/project switch required for current host-domain work. Existing broader visuals/UI2/Mini/privacy/device/E9–E10 gates remain open;no enterpriseissue globally closed.

No main/stage/commit/push/Azure/provider/credentials/productiondata changes. Earlier dirty work,including payroll migration/snapshot edits,retained. Owner fullPENTA+Manual app→authorized go-live→Hayansh→Academy Desk website order unchanged;90%AI remains a target,not measured delivery.
