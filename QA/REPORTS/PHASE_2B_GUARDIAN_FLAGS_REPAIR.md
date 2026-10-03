# Phase 2B — Guardian invoice and nested-document permissions

2026-10-02. Local, uncommitted repair of the remaining two accepted BUG-SEC-0005 branches. Continue Sol High; no repeated Astra inventory/audit.

## Reproduction and bounded repair

The real Identity/HTTP/SQL baseline reproduced both disclosures with finance=false, documents=false, academic=true: the invoice download returned 200 although the details projection hid invoices, and classHistory returned private/common attachment URLs although top-level resources were empty. Both GETs left captured database/Identity/audit snapshots unchanged. These are baseline defects, not passing repair tests.

Only two product expressions changed in PortalController:

- Invoice download enforces live CanViewFinance on the existing active, scoped guardian portal link. CanViewDocuments is deliberately not an additional invoice gate: the existing invoice projection is finance-gated. Finance-only access remains valid.
- Nested classHistory resources require CanViewDocuments. Permitted academic history metadata remains available; academic=false still hides history entirely. No change to existing resource audience/published/enrollment filters.

The previously repaired certificate method, invoice arithmetic/HTML, access helpers, other methods, frontend, schema/migrations and normal development assemblies are retained. Nullable contact details and a resource without description remain valid fixtures; no new required fields.

## Verification

55/55 real Identity/global-filter/HTTP-SQL GET cases passed (19x200, 2x401, 31x403, 3x404). Counts are separate from 1,019/1,019 existing backend tests rerun; no new unit tests or inflated combined count.

- 24 checks: all eight finance/document/academic combinations across student details, invoice download and retained certificate download.
- 22 details checks total: permitted history ID, batch, timestamps, delivery mode, completion and attendance retained; private/common attachments present only when allowed. Top-level documents/certificates and invoices independently gated. Unpublished, other-child, unenrolled and foreign-resource sentinels excluded.
- 25 invoice checks total: finance-only positive, documents-only denied, learner access independent of guardian restrictions; scoped missing/other-child/foreign invoice 404; anonymous, unlinked administrative/teacher/other/foreign learner, wrong/missing child, revoked link, portal-off and inactive learner denied. Completed + Reconciled payments produce 300 paid and 500 balance on a 1,000 invoice with 200 adjustment; Voided payment excluded. HTML type and filename retained.
- Eight certificate checks are included in the 55, not an extra suite; prior learner recipient and document gate retained.
- Live flag and link changes are tested with the same previously issued token. Deliberate fixture edits between requests are not endpoint writes.
- Every baseline/final GET compares fresh before/after snapshots of students, guardians, links, invoices, payments, resources, sessions, attendance, enrollments, batches, certificates, academies, Identity users and audits. This is not a claim about every database table.
- Baseline and final isolated builds passed, zero warnings/errors. Runtime routes313 unchanged; 82 application/7 Identity migrations applied only to disposable storage. Strict ownership-mismatch cleanup refusal passed. Both owned containers/databases/logins/temp roots/ports cleaned; normal dev database/services and Azure untouched.

## Evidence and preservation

- [Baseline HTTP evidence](../EVIDENCE/logs/phase-2b-guardian-flags-baseline.log)
- [Final HTTP evidence](../EVIDENCE/logs/phase-2b-guardian-flags-final.log)
- [Backend rerun](../EVIDENCE/logs/phase-2b-guardian-flags-backend.log), [TRX](../EVIDENCE/guardian-flags-test-results/guardian-flags.trx)
- [Baseline build](../EVIDENCE/logs/phase-2b-guardian-flags-baseline-build.log), [final build](../EVIDENCE/logs/phase-2b-guardian-flags-final-build.log)
- [Owned cleanup](../EVIDENCE/guardian-flags-cleanup.json)
- [Accepted predecessor snapshot](../EVIDENCE/guardian-flags-before.json), [current source receipt](../EVIDENCE/guardian-flags-source-receipt.json), [consistency validator](../tools/validate-guardian-flags.cjs)

Before manifest captures524 accepted source/evidence/binary entries. Only six declared application/harness/tracker files may differ; exact normalized comparison limits product edits to the two expressions and harness dispatch to the new mode. Previous certificate reports, receipts, failed-attempt evidence and baseline/final assemblies are immutable. Baseline SQL build, final SQL build and backend rerun each use new isolated artifact paths. No dev assembly replacement, browser interaction, external media fetch, customer-data mutation, commit/push/deployment or restart.

## Remaining gates and next task

BUG-SEC-0005 and Phase2B remain OPEN for linked full-portal/browser/device/critical regression, complete role and dual-linked identity semantics, permission-change races and release acceptance. This completes the bounded local repair of its three known endpoint/projection branches, not whole-issue closure. Invoice download remains HTML, not inspected PDF acceptance; private media delivery beyond this projection is not certified.

Next: accepted BUG-SEC-0009 — guardian identity incorrectly treated as Admin for owner announcements. Reproduce the audience exposure, narrowly repair and run the real scoped audience matrix. Same Sol High allocation; Astra findings remain trusted.
