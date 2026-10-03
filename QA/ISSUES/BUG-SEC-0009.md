# BUG-SEC-0009 — Guardian accounts are classified as Admin for owner announcements

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — bounded local repair verified |
| Final verification |47 real HTTP-SQL cases/1019 existing backend rerun PASS; browser/critical OPEN |
| Severity | Major announcement audience isolation failure |
| Priority | P1 |
| Category | SEC |
| Module | FAMILY |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /portal; admin announcement consumer |
| API | GET /api/portal/announcements |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-ANNOUNCEMENT-001 |
| Evidence classification | Real Identity/HTTP/owned SQL; original static trace retained below |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PortalController.cs:81 |
| Class/function | Announcements |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | As platform owner publish a current Academy administrators announcement to synthetic academy A. Read announcements as a Guardian-only identity in A, actual A admin, A teacher/student and academy B control. Use fewer than10 candidate rows and valid expiry metadata. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted announcement audience selection; no deployment |
| Retest result |47 HTTP-SQL cases: one owner publication/46 unchanged GETs;1019 existing backend rerun PASS |
| Regression result | Live administrative roles, preserved dual-link precedence, metadata/tenant controls PASS; wider gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

As platform owner publish a current Academy administrators announcement to synthetic academy A. Read announcements as a Guardian-only identity in A, actual A admin, A teacher/student and academy B control. Use fewer than10 candidate rows and valid expiry metadata.

## Expected

Admin-only announcement is returned only to permitted administrative audience; guardian link alone must not grant it.

## Actual / evidence

Audience is Student when StudentId exists, Teacher when TeacherId exists, otherwise Admin. GuardianId and administrative roles are ignored, so a same-academy guardian with neither other link matches audiences=Admin. Runtime NOT RUN.

Source snapshot:

```text
80:         if (user?.AcademyId is null) return Forbid();
81:         var audience = user.StudentId.HasValue ? "Student" : user.TeacherId.HasValue ? "Teacher" : "Admin";
82:         var candidates = await db.Notifications.AsNoTracking()
83:             .Where(x => x.AcademyId == user.AcademyId && x.RecipientId == null && x.RecipientType == "Academy" && x.Status != "Cancelled")
```

## Suspected root cause

Catch-all audience fallback equates absence of student/teacher links with administrative authority.

## Business impact and blast radius

Guardian and other unlinked same-academy accounts reading owner admin-targeted announcements; does not grant other admin APIs.

## Related / required regression

SECURITY-ANNOUNCEMENT-001: Real HTTP two-tenant all-role/link matrix for Admin/Student/Teacher audiences, guardian/unlinked/dual-linked identities, expiry and revoked links; assert exact payload absence for denied audience and positive admin control.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local successor — 2026-10-02

[Announcement audience repair](../REPORTS/PHASE_2B_ANNOUNCEMENT_AUDIENCE_REPAIR.md) supersedes the original runtime-NOT-RUN statements. Real owner publication reproduced private Admin-message disclosure to guardian-only, unlinked custom and Manager accounts. The fallback now requires current Owner/AcademyAdmin role; existing Student/Teacher precedence retained, unsupported accounts return200 empty.47 real Identity/HTTP-SQL cases PASS including scoped owner publication/platform audit, sixteen role/link combinations, same-token role revocation/restoration, metadata and tenant isolation; all46 GET captured snapshots unchanged.1019 existing backend rerun PASS. Initial harness-only receipt-key compile error retained/corrected before endpoint execution. Owned SQL cleaned, previous guardian/certificate methods and accepted source/evidence/binaries retained; dev DB/services/Azure/commit/deploy untouched. Remains OPEN for full-portal/device/browser/critical/races and wider lifecycle/legacy/candidate-limit gates. Next BUG-DATA-0023 platform audit deletion scope; Sol High.
