# Phase 2B — Family certificate download

2026-10-02. Bounded repair for accepted BUG-DATA-0014 / FAMILY-DOCUMENT-001 and the certificate portion of BUG-SEC-0005 / SECURITY-GUARDIAN-002. Previously agreed Sol High allocation. Issue/Phase 2B/release remain OPEN; no repeated Astra source audit or Azure/commit/push/deploy.

## Reproduction and repair

The next linked certificate row, FAMILY-API-011, had no full-pipeline coverage. Two real Identity/HTTP/disposable-SQL baseline requests reproduced the accepted findings: a permitted guardian's HTML certificate named the requesting guardian, and a guardian with CanViewDocuments=false still received it. Both reads left captured SQL/Identity/audit state unchanged. [Baseline log](../EVIDENCE/logs/phase-2b-certificate-family-baseline-resumed.log). These are reproduced defects, not two passing repair checks.

Only PortalController.DownloadCertificate changed. It now reads the certificate subject's same-academy student name, retaining HTML encoding, and enforces the linked guardian's live document permission. Existing active-child/portal/link/tenant and Issued-only guards, HTML content type/filename, title/number/date contract remain. It still uses the current learner name, not an immutable original issuance-name snapshot. Invoice download, class-history resources, global authorization policy, schema and frontend are untouched.

## Final verification

[25/25 new real HTTP/SQL cases](../EVIDENCE/logs/phase-2b-certificate-family-final.log), run c5b75d29bd3e4931884089b3628194a8 on loopback SQL port 50989. Seven successful HTML responses; one401, thirteen403, four404. Parameterized responses are not25 distinct defects. Shared health/login/tenant controls are not added to that count.

- Learner account alias and two differently named guardians produce the learner's encoded name, never the requester's account name. Name/title markup is encoded; exact title, number, leap-day date, content type and attachment filename checked.
- Null batch/notes are retained in the synthetic issued records; downloads do not require them. Mixed-case Issued variants continue to match under the actual SQL collation. Revoked/Replaced credentials cannot be downloaded.
- Anonymous, unlinked admin/teacher, unrelated learner, foreign identity/child/number, missing child/number and document-restricted guardian return no certificate HTML.
- Revoking document permission or the guardian link after login immediately denies later requests. A second permitted guardian is unaffected. Portal-off and inactive-child controls deny; documents-only access works with academic/finance flags off.
- Every GET compares fresh complete snapshots of certificates, students, guardians, links, academies, Identity users and audits before/after, proving no changes within that captured scope. Fixture flag/activation edits between requests are deliberate setup, not endpoint writes. This is not a comparison of every table, nor a concurrent revocation/status-race guarantee.

[Existing backend suite](../EVIDENCE/logs/phase-2b-certificate-family-backend.log):1019/1019 PASS,0 failed/skipped; [TRX](../EVIDENCE/certificate-family-test-results/certificate-family.trx). Those existing tests were rerun because Portal source changed; no new unit cases or combined inflated pass total. Isolated baseline/final SQL builds succeed with zero warnings/errors; retained font/UI evidence was not rerun.

## Preservation, cleanup and next

An initial runner launch preceded build completion and found no harness DLL. It performed no endpoint tests; its [failed launch log](../EVIDENCE/logs/phase-2b-certificate-family-baseline.log) and [owned cleanup](../EVIDENCE/certificate-family-initial-launch-cleanup.json) remain, excluded from product failures/passes. The resumed baseline and final run apply82 application/7 Identity migrations only to fresh guarded databases, verify runtime targets, reject mismatched cleanup markers and remove owned databases/logins/containers. [Post-check](../EVIDENCE/certificate-family-cleanup.json):all three exact containers/roots absent, owned ports no listeners. Removed data was disposable synthetic QA data; logs/source/binaries are retained.

[Before receipt](../EVIDENCE/certificate-family-before.json) preserves495 accepted source/evidence/binary entries. Only declared method/QA dispatch/checkpoint changes differ; original Portal/harness/runner sources and separate baseline/final binaries are retained. No ordinary development service/database/migration, customer/provider data or frontend/shared-control changes.

The first consistency validator compared uppercase .NET checksum text with lowercase JS hex. That [QA-only failure](../EVIDENCE/certificate-family-validator-initial-failure.json) and original validator are retained; normalizing the computed hex fixes the comparison without changing any response, test log or product source. No extra runtime passes are counted from the validator.

The second validator expected the early terminating PowerShell error in the partial Tee log, although it appeared only in terminal tool output. Its [failure/source](../EVIDENCE/certificate-family-validator-second-failure.json) is retained. The corrected check uses actual partial progress plus the launch/cleanup receipt; the log was not rewritten to manufacture an error trace.

HTML response evidence is not browser download/PDF rendering, physical-device, full portal E2E or complete critical-suite acceptance. Both issues stay OPEN; BUG-SEC-0005's invoice and nested-resource portions remain unresolved. Certificate native PDF/fonts/glyph/long-content/remaining-theme/device/status-race and other linked/release gates stay open. Next: remaining guardian invoice/document and class-history permission paths from the same accepted issue, then outstanding critical gaps. Continue Sol High; no routine user test handoff.
