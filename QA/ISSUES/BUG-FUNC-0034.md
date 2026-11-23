# BUG-FUNC-0034 — Guardian child list returns500 for a valid linked learner

| Field | Value |
| --- | --- |
| Status | OPEN / FIXED LOCALLY — bounded native acceptance passed |
| Confirmation status | REAL-IDENTITY-HTTP-SQL-REPRODUCED |
| Final verification | API36/SQL58/current six-role native69 PASS; exact candidate CI and broader release acceptance pending |
| Priority | P1 |
| Severity | Major family portal availability |
| API | GET /api/portal/guardians/{guardianId}/children |
| Source | apps/api/Controllers/PortalController.cs:GuardianChildren |
| Regression | GUARDIAN-CHILD-LIST-SQL-001 |

On exact application source2e8e12c, the authorized synthetic Guardian logs in,
GET /api/portal/me returns200 with the active linked child and its own parentId,
and the child's details/invoice download succeed. GET of that own guardianId's
children endpoint returns500. Direct follow-up confirmed login200/me200/list500.
No foreign record access is asserted. A61-assertion six-role native probe retains
the failure before its remaining checks; historical full API/CI passes do not
replace this real SQL reproduction.

Private evidence: current synthetic demo12994a6a,
permissions-782dbfb64d64439a9a809c847396615b.json and
.build-check/d57-20261123/guardian-probe.json. Credentials and raw bodies stay
private. Original synthetic DB/key ring were backed up before the native window.

The fresh native baseline also reproduced200expected/500actual. Moving the same
name ordering before PortalChildSummary construction fixes SQL translation;
all original guardian/tenant/link/revocation checks, DTO and read-only semantics
remain. Native58checks (retained55plus3child-list controls), focused API36 and
current-source six-role native69 all passed. The existing GuardianFlags module
is added once to ordinary CI; all26older modules and paired Core guard remain.
No empty-result exception masking or client-side authorization substitute.
Current exact-source result, retained failures and evidence seals:
[D57 acceptance](../REPORTS/D57_PORTAL_AND_PREFLIGHT_ACCEPTANCE_2026-11-23.md).
This remains a bounded API/SQL repair, not a new physical-device/visual release
pass or closure of the broader enterprise program.
The six existing critical release records remain unchanged and OPEN.
