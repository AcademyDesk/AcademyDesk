# Phase 2A — SECURITY-FILE-001 / BUG-SEC-0001 runtime reproduction

Date: 2026-09-30. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. This is a local synthetic-data reproduction, not a product fix or production claim.

## Fixture and route

Two fresh, labelled Docker SQL Server 2022 containers and run-owned databases were used: `479ea34864a145288133a57d93d659f2` and `29889442285d426682df53d92b88a72e`. Each run applied 79 application and 7 Identity migrations under an admin connection and used a database-scoped non-admin login for the real ASP.NET TestServer. The real Identity and middleware pipeline were retained. The fixture created academies A/B, academy admins, Teacher A with an assigned synthetic batch/course, and no customer data.

Teacher A authenticated and uploaded a synthetic PDF through `POST /api/teacher/resources/upload` (200). The response supplied its `/uploads/teacher-materials/{generated-name}.pdf` URL. A fresh SQL context verified the matching learning-resource row and academy/batch ownership. The run-owned webroot contained the exact uploaded bytes (SHA-256 `8206251DDFF9FAC3E0A98331D341D11EC0FE62A418A72071A3794F81D08D4CD2`). Thus subsequent 200s are not a missing-fixture or public-link false positive.

| Request to returned URL | Expected | Observed in both fresh runs |
| --- | --- | --- |
| Teacher A authenticated GET | Exact content | 200, exact content |
| Anonymous GET | No private content | **200, exact content** |
| Authenticated academy B GET | No academy A content | **200, exact content** |
| Anonymous `Range: bytes=0-7` | No private content | **206, exact first eight bytes** |
| Academy B `Range: bytes=0-7` | No academy A content | **206, exact first eight bytes** |
| Anonymous GET after academy A admin unpublished resource and a fresh SQL context confirmed `IsPublished=false` | No private content | **200, exact content** |

The first exploratory run used the admin learning-resource upload and also showed anonymous/cross-tenant content delivery. Its initial retry was discarded because the synthetic academy lacked the Certificates subscription module; that 403 was a fixture error, not product evidence. The two runs above used the exact Teacher route from the issue and completed successfully.

## Result and boundary

**SECURITY-FILE-001 FAIL; BUG-SEC-0001 RUNTIME-REPRODUCED, OPEN P0.** `UseStaticFiles()` serves the webroot URL before authorization, so neither the teacher upload's authorization nor the resource's persisted publication state guards direct retrieval. This evidence covers this Teacher material route, ordinary GET, Range, foreign tenant, and unpublish. It does not establish production exposure of any particular file, exhaustive other upload folders, cache behavior, or a fix.

Both runs retained the negative ownership-marker cleanup refusal check and then removed their run-owned database, login, host folders and exact labelled container. No dev/Azure database or external delivery was touched. No application business rule was edited, committed, pushed or deployed. Next Phase 2A case: FINANCE-RULE-001; repair and regression follow in Phase 2B after reproductions.
