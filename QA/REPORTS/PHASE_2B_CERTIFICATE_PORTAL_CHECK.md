# Linked certificate portal — BUG-FUNC-0032

Date: 2026-10-03. Branch: `cursor/certificate-validation`. Model: Grok 4.7 in Cursor. No product source change. No Azure deploy. No merge to `main`.

## Result

The signed-in certificates page issued one certificate against disposable SQL, and the official PDF text matches that saved row. BUG-FUNC-0032 stays **OPEN**. The official PDF still repeats on three letter pages. The issue audit does not store the signed-in actor. A physical phone was not available.

## What ran

Disposable SQL Server 2022, run `b7510d43503d457789ce63cbf02979c6`, database `AcademyDesk_QA_b7510d43503d457789ce63cbf02979c6`. The existing QA API host served `http://127.0.0.1:49202`. The real certificates page, not the synthetic probe, ran at `http://127.0.0.1:49201`. Sign-in was the normal login form. Tenant controls passed before the browser session: each admin read only its own student, and cross-tenant GET/POST returned 403.

One active enrollment was seeded for Portal Learner in Portal programme. The browser selected that learner and class, set the title to Portal saved achievement and the note to Portal recognition, and clicked Issue certificate.

`POST /api/academies/5129dcf9-2c52-45d6-91b5-e3e4ab85352f/certificates` returned 201.

## Saved row

Fresh SQL after the host stopped, one certificate:

| Field | Value |
| --- | --- |
| Id | `10be5e05-b987-4d6a-bf09-ad3ff5d5cb97` |
| Number | `CERT-20261003105343-561` |
| Verification | `F1C3B80D1A6F44F4` |
| Title | Portal saved achievement |
| Status | Issued |
| Student | Portal Learner, `c82a2298-1bf7-4f2b-b2d9-0af8f4a76a37` |
| Batch | Portal programme, `e655f05d-b3de-44b0-894f-f9398e54cf27` |
| Issued date | 2026-10-03 |
| Notes | Portal recognition |
| Theme | music-recital |

## PDF

Before issue, the draft PDF was watermarked `PREVIEW — NOT AN ISSUED PRINT` and had no certificate number.

After issue, the draft title was changed to `UNSAVED DRAFT TITLE`. The official PDF does not contain that title. Its text matches the saved row: number, verification code, Portal Learner, Portal programme, Portal saved achievement, Portal recognition, Issued, 2026-10-03, Portal Signatory. The PDF is three letter pages that each repeat the full certificate. Pagination is not accepted.

## Audit

One `CertificateIssued` audit row points at certificate `10be5e05-b987-4d6a-bf09-ad3ff5d5cb97`. `ActorUserId` is null. The issue method writes the audit without the signed-in user.

## Cleanup

The run-owned database and login were removed. The owned SQL container was removed. Application migrations observed: 82. Identity migrations observed: 7.

## Not run

Physical phone. Simulated layout is not that check.

## Next

Keep BUG-FUNC-0032 open until pagination is accepted and a physical device is checked. Do not treat the null audit actor as closed. After this certificate slice, the open P0 queue remains BUG-DATA-0001, BUG-DATA-0002, BUG-DATA-0003, BUG-DATA-0010, and BUG-SEC-0001. Stay in Cursor for the next P0 reproduction. Move to Codex only if a P0 fix becomes a genuine financial, security, or transaction design block. Do not open v0, Lovable, or Framer for this queue.
