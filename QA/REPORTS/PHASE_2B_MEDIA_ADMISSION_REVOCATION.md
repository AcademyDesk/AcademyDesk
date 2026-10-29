# Batch1 — already-admitted private-media revocation: FAILED

Date:2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. QA-only coordinated diagnostic; no application repair implemented. Existing accepted recorder/maximum-transfer/lifecycle/finance gates remain independent. Batch1 and BUG-SEC-0001/BUG-FUNC-0007 remain OPEN.

## Evidence

Run `eca7d5c6d7be4c699573b9b907216e80`,90.4s,nonzero exit `-532462766`. Real disposable SQL port54638/private Azurite/native ASP.NET Identity/TestServer API. Original75 Blob assertions PASS before this new gate. New admission gate **FAILED:0/6 guarded**,22 additive HTTP requests/no429. This is a reproduced gap, not another successful security checkpoint.

| Current authority deactivated | In-flight operation | Fresh request | In-flight response | Observed result after deactivation |
| --- | --- | ---: | ---: | --- |
|Teacher|stage|404|200|One new uncommitted block, no resource/notification|
|Teacher|complete|404|200|Committed Blob, one resource and one notification|
|Academy|stage|404|200|One new uncommitted block, no resource/notification|
|Academy|complete|404|200|Committed Blob, one resource and one notification|
|Batch|stage|404|200|One new uncommitted block, no resource/notification|
|Batch|complete|404|200|Committed Blob, one resource and one notification|

Each synthetic file is3 bytes. Separate uploads/request IDs prevent one case from hiding another. The request is paused immediately before the native provider StageAsync/CommitAsync operation **after** existing CurrentTeacher/AuthorizedSession/domain policy has admitted it. A separate scoped SQL UPDATE deactivates exactly one authority row; an independently denied new progress GET404 proves current persisted authority has changed. Only then is the original request released to real storage. No HTTP response/authentication/policy/provider result is fabricated. This controls scheduling, not an OS/network pause or production-load simulation.

Authority controls compare full protected-state/upload/committed-Blob snapshots with only the selected IsActive bit changed. Fresh denied requests leave the snapshot unchanged. Staging leaves SQL/committed inventory unchanged but actual provider progress changes from no block to block0. Completions set only the target session timestamp, add one resource/notification and committed Blob, preserving previous rows/ETags and unrelated protected-state fields. Exact byte hashes of the new staged/committed payload were **not** separately measured by this diagnostic; existing byte-integrity gates remain separate. No cross-tenant or anonymous leakage is established by these timings; the finding is stale authority at the storage/publication boundary. Independent read authorization remains enforced for subsequent requests.

Teacher/academy/batch activity is restored after every observation and independently checked. Identity role/security-stamp/account changes, changes after the Blob commit but within SQL publication, already-started downloads, browser/physical devices and production cancellation guarantees are not exercised. Existing snapshot covers15 named protected fields plus uploads/committed Blob inventory, not every application table/authentication column.

## Cause and repair boundary

`ClassMediaUploadsController.Chunk` authorizes before buffering/staging. `Complete` authorizes before Blob CommitAsync, then begins its SQL publication transaction and inserts the resource/notifications using the earlier session authority. There is no second authoritative domain check inside the publication transaction, and no transaction/lock ordering with revocation established by these paths. Checks for a new request work; previously admitted requests can continue.

This fails the deliberately strict immediate-revocation oracle. Do not relabel an admission-time permission model as immediate cancellation or suppress the failing assertion. The safe proposed repair boundary is: **do not publish a new resource or notification when authority has been revoked before publication; retain nonpublic uploaded bytes for authorized retry instead of deleting evidence/data.** Byte staging and already-streaming downloads need explicit semantics: private staging after admission is not itself proof of public access, and previously transmitted bytes cannot be recalled.

Before implementation, settle whether private already-admitted staging may finish while publication is blocked, or whether every provider write must also be canceled/serialized. Then implement current-user/role/stamp and domain checks with transaction/lock ordering at the durable publication boundary. A simple late IsActive check alone must not be advertised as a race-free guarantee. Preserve idempotent completion and Blob/SQL recovery, old role/tenant/lifecycle oracles, and the six failing observations. No Model/Core changes or cross-project handoff are required: Academy Desk owns this boundary.

## QA changes / validation / cleanup

New opt-in `-AdmissionReview`, dedicated ignored `.build-check/media-admission-sql/SqlHarness.dll`, `MediaAdmissionReview.cs` with test-only provider timing wrapper, and narrow Program dispatch. Original recorder/recovery/upload/lifecycle/large modes unchanged. Mixed/inherited modes rejected before provisioning; six mixed-mode refusals and wrong-module pre-provision refusal PASS. Fresh harness32.81s/zero warnings-errors, PowerShell parse/diff/scoped high-confidence secret-pattern scan PASS. Scan initially flagged `sk-` inside the two exact known run-owned container names; reviewed as metadata false positive, rescanned with only those names substituted, no probable real credential found/values not printed. Original75 Blob checks/root HTTP/tenant controls passed; broader1484 API tests/56 focused client checks/83-page export retained, not rerun for QA-only tools.

Runner honestly returned failure and retained only its two disposable containers. After preserving log/JSON, removed exactly `academydesk-qa-eca7d5c6d7be4c699573b9b907216e80` and `academydesk-blob-qa-eca7d5c6d7be4c699573b9b907216e80`, verifying exact name/run label/single loopback binding and known SQL port. Their disposable databases/media are discarded; evidence remains. All nine pre-existing containers preserved. Harness-owned local folders use existing guarded cleanup; no successful final SQL/login cleanup receipt is claimed for this failed run. No production/dev database or Azure credentials accessed, no app change/stage/commit/push/main merge/Azure.

Receipts: `QA/EVIDENCE/batch1-20261012/sql-media-admission.txt`; six-case JSON at `QA/EVIDENCE/media-admission/eca7d5c6d7be4c699573b9b907216e80/receipt.json`. Feature/main SHAs unchanged; evidence untracked/artifact ignored/no staged files.

## Next route

Sol High: settle and implement the private-staging versus publication revocation boundary, then rerun retained failing regression with real SQL/Blob, idempotency and authorization controls. Do not start broad Manual visuals or Azure release as if Batch1 had passed. Physical/operational/qualified-review gates remain open. Medium later for settled shared Manual Orbit+Aura dark/Prism light four-portal visuals after versioned PENTA design handoff.
