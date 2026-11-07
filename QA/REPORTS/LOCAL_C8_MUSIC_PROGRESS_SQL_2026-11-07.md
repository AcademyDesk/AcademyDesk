# Local C8 music progress SQL acceptance — 7 November 2026

Claim recorded before implementation on `8313c80`. Cloud C8 source is
`44c613d885fca59266d670f5c70a29133568e103` with handoff `3f896f6`.
Cloud C9 claim `0b170d9` owns the finance frontend batch.

Integrate the five returned C8 paths after verifying their common base and
current frontend source. Reuse Cloud's source-bound frontend build, handler
tests and controlled Chromium evidence. Desktop owns the outstanding real
Identity, HTTP, SQL and required-audit checks for music status updates.

Add one bounded existing-harness module. Verify zero, positive and null score
preservation, status, target and notes readback, unrelated learners, role and
tenant denials, and audit failure. Record any reproduced defect before repairing
the update action. Keep separate assignment, cross-client conflict, device and
critical release checks open.

Keep the owned SQL lifecycle, 3 GiB admission guard and original service
restoration. Cloud C9 paths stay with Cloud. Sol High for audit and SQL work.

## Cloud source and ownership

The five C8 return paths were disjoint from Desktop C6. The integrated frontend
tree matched Cloud `3f896f6`, including its locked dependencies. Cloud's 42
controlled Chromium cases, 12 handler checks and build evidence were reused.
The cloud frontend suite reported 1,930 passes and three missing SQL receipt
failures. Those failures remain in its original evidence. The frontend build
and controlled tests were performed in Cloud.
[Cloud C8 handoff](../HANDOFF/C8_MUSIC_PROGRESS_2026-11-06.md)

C9 initially owned its finance frontend paths at claim `0b170d9`. The Desktop
music module and update repair did not edit those paths. Cloud published the
completed C9 handoff at `ff662d0` during this validation window.

## Real SQL reproduction and repair

Baseline window `5f9ad90abf044299965eb3383b01c018` and SQL run
`f22e8b381b8f44d9993f44b6ed5b9581` passed 22 value and authority controls.
An Admin update survived a required audit failure despite HTTP 500. A Platform
Owner update returned 200 and changed the row without reaching the audit
interceptor. Each request used a distinct score. The failed receipt and cleanup
proof are retained.

The existing atomic domain and audit boundary now covers the status PATCH,
including Platform Owner. Two API theory cases cover mutation and read scope.
The repair reuses the existing authorization and persistence pipeline.
[Music audit issue](../ISSUES/BUG-DATA-MUSIC-AUDIT-001.md)

Repaired SQL run `af180925d7b049e5a1bdc1b4fee60c3c` passed **25 native HTTP and
SQL controls** in window `e7c143818b26478498aae0c674b3e82f`. All five statuses
roundtripped zero, positive and null scores, target date and notes. Each Admin
update wrote one attributed audit. The other learner and foreign rows kept their
values. Seven anonymous, Teacher, foreign-scope and invalid-status controls left
SQL state intact. Both authorized actors rolled back on audit failure. One
authorized Platform Owner save recorded the expected audit.

The harness built with zero warnings and errors. SQL applied 85 application and
seven Identity migrations and verified the runtime login. It removed the owned
database, login and container. Both original PENTA services returned healthy and
Docker inventory matched. The coordinator initially looked in the wrong receipt
folder after the SQL runner exited zero. The API-only follow-up verified the
actual receipt and identical source hashes before continuing. This preserves the
accepted SQL run and its original coordinator failure without repeating SQL.

The API-only window `cf9f758ce58a4980a41b4fe6114a7aa0` passed all **1,991 API
tests** with zero failures or skips. Source hashes matched the repaired SQL
run. Service health, inventory restoration and lease release passed. The
original main checkout matched its recorded revision and status.

Live browser recovery, independent-client conflict behavior, assignment and
piece creation, and critical and device closure remain open. Cloud C9 now has
a separate returned source handoff for the finance paths.

## Reproduce

Build `QA/tools/SqlHarness/SqlHarness.csproj` with `--no-restore`. Use the existing
3 GiB admission guard and exclusive lease. Set `QA_CLOUD_FINANCE_RECONCILIATION=1`
for bounded SQL and run `QA/tools/SqlHarness/Run-ReconciledPayment.ps1` with
`-Module MusicProgress` and `-HarnessPath` pointing at the built DLL. Restore the
original services in the coordinator finally block and check the owned inventory.
