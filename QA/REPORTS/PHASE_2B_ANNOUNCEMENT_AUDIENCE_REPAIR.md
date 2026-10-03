# Phase 2B — Administrative announcement audience

2026-10-02. BUG-SEC-0009, accepted Astra finding; local Sol High repair, not a repeated inventory/audit.

## Baseline and change

A real Platform Owner HTTP publication persisted a same-academy Admin-only InApp announcement and its actor-linked platform audit. Guardian-only, unlinked custom account and Manager each received the full private message from GET portal/announcements. The true Admin positive control also received it. All four baseline GETs left captured snapshots unchanged; these baseline disclosures are not passing repair cases.

One product selection block changed in PortalController.Announcements. Existing StudentId, then TeacherId precedence remains. Without either link, the current Identity roles must contain Owner or AcademyAdmin, matching the existing administrative boundary; there is no catch-all Admin authority. Unsupported accounts return200 with an empty array, not a new role grant or an error. Current role lookups, not JWT role claims, govern the administrative fallback. A guardian also holding an actual administrative role remains an administrator; guardian linkage alone does not confer authority.

No announcement writer, audience/expiry helper, query order/limit, learner/teacher link eligibility policy, schema, frontend or shared navigation changes. In particular, guardian links do not inherit Student announcements. Previous guardian finance/document and certificate repairs remain byte-equivalent after newline normalization.

## Results

- 47/47 real Identity/global-filter/HTTP-SQL cases PASS: one owner publication with persisted row and platform audit, plus 46 GETs (44x200, 1x401, 1x403). Counts are independent of 1,019/1,019 existing backend tests rerun PASS; no new unit tests.
- Sixteen role/link combinations: AcademyAdmin/Owner, Manager/Finance/custom delegated communications role, guardian/live or revoked link, linked and unlinked Student/Teacher, and dual Student-Teacher/Guardian-Student/Guardian-Teacher/Guardian-Admin/Admin-Student identities. Student/Teacher precedence and legitimate administrative access retained. Unsupported accounts disclose no announcement bytes, including legacy audience-less and Both rows.
- Same-token Admin role removal/restoration and Owner grant/removal take effect on subsequent reads. Guardian link revocation and portal-off do not create Admin authority.
- Other-academy administrator receives only that academy's own announcement; anonymous401 and academy-less platform account403 retained.
- Twenty-one metadata checks: future, expired, Cancelled, not-important, malformed JSON, targeted recipient and wrong recipient type excluded for Admin/Student/Teacher. Supported exact, comma-separated/case/whitespace, Both and absent-audience metadata retained for valid audiences. Exact returned ID/title/message/creation timestamp and descending order checked.
- Every GET compares fresh captured notification/read-receipt/platform-audit/domain-audit/academy/person/link/Identity-user/role-membership snapshots. No read changes captured rows. Intentional synthetic fixture/role changes between requests are not endpoint writes. Owner publication adds exactly one notification/platform audit and leaves all other captured collections unchanged.
- Baseline/final isolated builds PASS with zero warnings/errors;313 routes unchanged,82 application/7 Identity migrations applied only to owned disposable SQL. Ownership-mismatch cleanup refusal PASS. Both owned containers/databases/logins/temp roots/ports removed. No customer/dev database mutation, dev-service restart, Azure/Blob/provider traffic, commit/push/deployment or ordinary assembly overwrite.

## Evidence and preservation

[Baseline HTTP](../EVIDENCE/logs/phase-2b-announcement-audience-baseline.log), [final HTTP](../EVIDENCE/logs/phase-2b-announcement-audience-final.log), [backend rerun](../EVIDENCE/logs/phase-2b-announcement-audience-backend.log), [TRX](../EVIDENCE/announcement-audience-test-results/announcement-audience.trx), [baseline build](../EVIDENCE/logs/phase-2b-announcement-audience-baseline-build.log), [final build](../EVIDENCE/logs/phase-2b-announcement-audience-final-build.log), [owned cleanup](../EVIDENCE/announcement-audience-cleanup.json).

One initial harness compile failure used a nonexistent read-receipt Id; corrected to the actual NotificationId/UserId composite key. [Failure retained](../EVIDENCE/logs/phase-2b-announcement-audience-initial-build-failure.log); no endpoint tests or container were started on that attempt. It is not a product defect or passing case.

[Before receipt](../EVIDENCE/announcement-audience-before.json) pins545 accepted source/evidence/binary entries; only six declared product/harness/tracker successors may change. [Current receipt](../EVIDENCE/announcement-audience-source-receipt.json) and [consistency validator](../tools/validate-announcement-audience.cjs) verify the method-only change, unchanged prior methods, new harness mode and immutable predecessor evidence/assemblies. Normal development binaries and earlier SQL/font/browser evidence are retained, not reclassified as current browser acceptance.

## Open gates and next

BUG-SEC-0009/Phase2B/release remain OPEN for full-portal/browser/device/linked/critical acceptance, role-change races and complete account/person lifecycle policy. Stale/foreign/inactive StudentId/TeacherId links, legacy typed-metadata faults, delivery-status policy and candidate crowd-out above10 rows were not repaired or certified. No persisted parent audience or new permission policy was invented.

Next accepted independent gap: BUG-DATA-0023, invalid platform activity-deletion scope silently targets both audit stores. Reproduce and narrowly repair using owned synthetic data only; continue Sol High. Astra findings remain trusted.
