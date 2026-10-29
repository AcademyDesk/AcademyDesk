# Batch1 — native recording / explicit upload / authorized readback

Date:2026-10-13. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `2e376b0ec47268c9b11d0dda2387620932ce2dd0`. QA-only linked composition, existing enterprise program retained. Batch1/BUG-SEC-0001/BUG-FUNC-0007 remain OPEN.

## Accepted run

`a2efcad24ca74385b65db07918cc0269`,125.3s,exit0; exact loopback SQL port61710. Original75 Blob assertions unchanged and PASS. Four actual exported Teacher journeys320/1440px light/dark passed with native Edge fake-device getUserMedia/MediaRecorder and real Identity/API/disposable SQL/private Azurite.

Each journey records native audio, pauses/resumes/stops, verifies all stream tracks ended and no pre-confirmation write, then presses **Confirm upload**. Real statuses: create201 → chunk0 PUT200 → complete200. Existing native permission configured via isolated CDP; no human microphone/private conversation captured. No injected recorder error/data signals in this run.

Teacher UI shows success, clears file/title/comment and disables empty upload. Authoritative refreshed Class history contains exactly one new item with original title/comment. Its existing PrivateMaterialActions Preview performs real protected HEAD/GET, plays the saved WebM with advancing clock/readyState4/no media error, and matches recorded preview SHA256/length exactly.

| Journey | Recorded and saved bytes | Saved playback clock | Exact SHA256 equality |
| --- | ---: | ---: | --- |
|320 light|31869|0.120078s|PASS|
|320 dark|28971|0.198057s|PASS|
|1440 light|28971|0.180127s|PASS|
|1440 dark|29937|0.177982s|PASS|

Per journey, real enrolled Student API GET200 returns the same full bytes/hash; anonymous401, foreign-tenant404 and same-tenant unassigned Teacher404. Four direct repeat completion POSTs return200/same resource ID without changing SQL/Blob state. These direct API probes are not Student browser UI or native browser Download tests.

## Integrity oracle

101 real HTTP requests,93 unchanged per-request snapshots; eight permitted SQL transitions =four creates and four completions. Only those narrowly allowed upload routes can write through the isolated relay; all other domain writes fail the gate. Previous read-only recorder/recovery relay and oracles remain independent.

Every creation preserves all protected data/previous upload rows/committed Blob inventory and adds exactly one current-Teacher-owned immutable upload. Owner/teacher/academy, current batch/class session, whole-class student scope, client request ID, length/filename/title/comment/type are checked against the actual request and SQL authority. Session belongs to the selected batch/Teacher. Existing client type is **Attachment**, not the API's omitted-type default Class material.

Every completion may only set its existing session completion timestamp, add one matching published resource, add one correctly scoped InApp Queued notification per **distinct Active enrolled recipient**, and commit one new Blob. All prior rows/Blob ETags preserved. Original chunk SHA256 equals full committed private Blob hash. No financial/audit/role/tenant/user-projection/local-file changes. Four completion retries are exact no-write snapshots. Captured bytes remain in memory/run-owned disposable storage, not evidence. Identity snapshot is its existing safe projection/role assignments, not all authentication metadata. The protected15-field projection does not include every application table; scope checks include the selected ClassSession independently.

No HTTP429. Existing limiter unchanged/650ms request pacing. Root HTTP/non-admin runtime/two-tenant controls,90 application +7 Identity migrations, mismatched-marker cleanup refusal, exact owned private Blob/SQL/login cleanup PASS. Runner removed only its two name+runlabel+single loopback-bound containers. All nine pre-existing containers preserved; normal dev/production DB/Azure credentials untouched.

## Changes and validation

New `RecorderUploadRegression.cs` and `recorder-upload-cases.cjs`, guarded `-RecorderUpload` runner flag, separate opt-in dispatch in existing recorder gate, and browser route mode. No application code/permission/limit/schema/Mini/Core changes. Original native/recovery cases untouched when upload flag absent. Five mixed-mode refusals and orphan-upload pre-provision refusal PASS; JS syntax/PowerShell parser/diff/scoped high-confidence secret-pattern scan PASS. Fresh builds25.18s then11.92s,zero warnings/errors. Existing56 focused client checks/1484 API tests/83-page export retained, not redundantly rerun for QA-only tools. Export/uploader SHA256 exactly match preceding native/recovery receipts.

Failed new run `9b1236bab91e466d81273d7bd02e4dbd`,75.9s retained: strict new creation oracle expected default Class material while actual existing client requests Attachment. Disposable SQL/source diagnostic confirmed correct saved title/comment/whole-class/session scope and existing Attachment contract; corrected QA oracle requires equality to request **and** Attachment, without changing app or weakening authority/byte/single-publication checks. Failed run's exact owned two containers removed only after ownership/loopback checks; evidence retained.

Receipts: `QA/EVIDENCE/batch1-20261012/sql-recorder-upload.txt`, `sql-recorder-upload-final.txt`; JSON/four saved screenshots at `QA/EVIDENCE/recorder-upload/a2efcad24ca74385b65db07918cc0269`. Final320-dark saved screenshot inspected: success/reset/history/playable preview present. Existing fixed mobile navigation overlays history and narrow headers remain Batch2 review, not all-screen visual acceptance. Desktop Classroom selection precedes resizing; this gate does not accept mobile section navigation.

## Remaining / next

Recording-to-explicit-upload/readback composition is now bounded PASS. Human microphone permission prompt, genuine hardware interruption/long capture, Android/iOS/physical devices, wider codecs, Student UI/native Download of these recordings, crash/real-network and already-admitted authorization timing remain separate. Security and operational/release gates are not closed by these four journeys. Next Sol High: remaining scoped security/operational candidate gates, with after-admission authority timing kept explicit; reuse accepted receipts, no fresh whole-program audit. Later Medium: settled shared Manual Orbit+Aura dark/Prism light four-portal visuals after actual versioned PENTA design handoff. PENTA frontend stays in its project. No new Core contract/project switch/handoff, stage/commit/push/main/Azure.
