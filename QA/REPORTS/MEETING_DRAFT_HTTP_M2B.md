# M2b — protected draft-preparation HTTP boundary

2026-10-15. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD `b1f8cb6`.
Continues [M2a](MEETING_PREVIEW_M2A.md), not full meeting execution or completed M2.

## Implemented

- Authenticated `POST /api/academies/{academyId:guid}/meetings/draft-preview`, no-store/no-cache,32KB declared request limit. Registered existing draft validator in DI. This is preparation only,not `/preview` with an approved executable receipt. No create/update/cancel/history route added;readiness still reports qualified provider operations unavailable. No frontend wiring or provider connection.
- Existing global academy/module/current-identity filter plus independent domain Owner/Admin/tenant checks protect source reads. Draft DTO rejects unmapped client/model authority,approval,guest andexecution fields. Replies contain source-authorized recipient details only on successful validation;denial/validation replies do not include foreign records. Request size attribute is a server transport bound,not a substitute for DTO limits.
- Added `MeetingStartConverter`:rejects missing/non-ISO offset before the default DateTimeOffset parser could infer the host timezone. Explicit ISOoffset/Z parsing tested;current academy-zone offset match remains mandatory. This closes the laptop-vs-Azure implicit timezone risk without changing existing schedules.
- Successful draft POST passes through the existing actor-bound action audit;source records/notifications remain unchanged. Failed binding/validation/authorization does not add a success audit. Audit contains bounded route/action metadata,not request body,recipient emails orprivate draft content. No returned fingerprint is an approval token.

## Verification

-137selected tests PASS:80meeting checks (including five new explicit-offset JSON cases) plus57existing communication permission/channel tests. Zero failures/skips. Local TRX `QA/EVIDENCE/meeting-preview-20261015/preview-http.trx`.
- Isolated SQL harness build PASS,zero errors/four preexisting NU1902/NU1903 warnings for Pkcs9.0.11;warnings remain open,not suppressed. Updated existing MeetingReadiness module to retain all21prior assertions then run20additional draft HTTP/SQL cases. Existing controller metadata test now explicitly permits exactly capabilities GET anddraft-preview POST,not arbitrary writes.
- Final SQL run `ae7f982cdbde4d0d85bae2c1f2b88ab9`,owned database `AcademyDesk_QA_ae7f982cdbde4d0d85bae2c1f2b88ab9`,loopback port53165. Receipts:`QA/EVIDENCE/meeting-readiness-sql-ae7f982cdbde4d0d85bae2c1f2b88ab9/result.json` and `QA/EVIDENCE/meeting-preview-sql-ae7f982cdbde4d0d85bae2c1f2b88ab9/result.json`.
- Final41/41SQL/HTTP cases PASS,no429,runner exit0 in80seconds. Database/login/ownership-checked container teardown confirmed;otherPENTA containers remain running. Final receipt source hashes checked against current files.
-20new cases cover valid full-minute draft,repeated preparation without provider writes,teacher/foreign/anonymous denials,unknownacademy/role/approval/guest/SQL fields,missing offset,null body,foreign/duplicate recipients,unqualified recording/recurrence,offset mismatch,completed enrollment,Hybrid source fingerprint change androle revocation with old ticket.
- Success checks canonical title/UTC/recipients,executionfalse/NotIssued,digest shape,no-store andexactly one actor audit. Rejections check no attendee leakage andno success audit. All cases assert unchanged batch/student/enrollment/channel/notification source snapshots. Existing21readiness cases keep their stronger no-audit GET assertion. No429 accepted.
- Runtime inventory328routes/317controller method-routes/10Identity method-routes,hash`7E6EFCD3F50335F8CE96BDC911F75593A277DAA645F2455ACB59435E0917E9AA`. Existing health/login/two-academy/runtimeSQL guards and90application/7Identity migration sets retained. No new schema migration.

## Limits and next

This endpoint prepares a non-executable draft;it does not persist a preview receipt,issue approval,enforce replay/consume-once,create an operation/outbox ordispatch a provider. Source digests detect differences butare not atomic SQL snapshot guarantees orauthorization. Future execution must reload/revalidate current identity/role/tenant/source/approval andcredential generation,not trust this response or its expiry. No adversarial mid-read concurrency/audit-insertion-fault/durable idempotency/provider/device/full enterprise acceptance claimed.

Next Sol High:M2c secure actor/tenant/version-bound preview receipt andapproval persistence design/implementation with disposableSQL migration/rollback/audit/replay tests,keeping execution unavailable. Do not infer product approval for external guests,bulk invitations,recording orother unsupported controls. M3qualified secure provider,M4Manual/PENTA integration,remaining visuals/UI2/Mini/device/E9–E10 pending. Transitive cryptography advisories remain an explicit pre-Azure security remediation item.

No credentials/provider/network invitation,frontend/main/staging/commit/push/Azure/production-data change. Only exact run-ownedSQL resources/host scratch can be removed;otherPENTA containers/evidence preserved. No new Mini contract/project switch. Owner fullPENTA+Manual app→authorized go-live→Hayansh→Academy Desk website sequence retained;90%AI remains a target,not measured completion.
