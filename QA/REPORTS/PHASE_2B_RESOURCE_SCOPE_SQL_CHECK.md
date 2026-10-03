# Phase 2B — learning-resource scope: real HTTP/SQL and family audiences

2026-10-01. BUG-DATA-0044 / LEARNING-RESOURCE-SCOPE-001. Supersedes the "HTTP/SQL/family NOT RUN" handoff in the [controller repair](PHASE_2B_RESOURCE_SCOPE_REPAIR.md), without rewriting that historical evidence. One fresh isolated run passes73 checks. Issue, Phase2B, browser and release gates remain OPEN. Continue Sol High; next accepted gap BUG-DATA-0045 compliance person-reference validation.

## Scope and changes

QA-only: ResourceScopeRegression plus an explicit dispatcher/wrapper mode. Product controller, frontend, family query, permission/module/auth filters, schema, private-file rules, prior source/tests/evidence and normal dev assemblies unchanged from accepted controller repair. The previous912/912 backend result (37 new resource cases) and4 expected baseline controller failures are retained, not rerun or counted as new cases. No second real-HTTP baseline is claimed.

JSON link Create and multipart administrator Upload retain intentional empty HTTP200 bodies. Real Identity actors, production authorization/audit/model-binding pipeline, actual SQL migrations and fresh-context readback; no mock authorization or filter bypass. Tiny five-byte synthetic uploads test stored bytes only, not valid playable files, Blob delivery/download or previews.

## Executed evidence

- [Final build](../EVIDENCE/logs/phase-2b-resource-scope-sql-build-final.log):0 warnings/errors, isolated .build-check/resource-scope-sql. [Initial build](../EVIDENCE/logs/phase-2b-resource-scope-sql-build.log) failed on a QA-only C# await/ReadOnlySpan expression; corrected by awaiting bytes before comparison. Failed compiler evidence retained/excluded; no HTTP host/database was provisioned by that failed build.
- [HTTP/SQL log](../EVIDENCE/logs/phase-2b-resource-scope-sql.log):73/73 bounded checks PASS, one fresh run.30 matrix cases (15 scopes × link/upload): matching/mismatching, batch-only, subject-only, academy-wide, missing/foreign/empty IDs in each dimension, inactive match/only/mismatch controls. Existing inactive behavior preserved. Two further API creations establish subject-B-only and matching batch-B audiences independently.
- Six create authorization controls: anonymous401, foreign academy actor403, same-tenant Teacher403, both endpoints. Three JSON binding cases: absent content415, explicit null400, invalid Guid400. Two multipart Guid binding cases400; missing/empty/disallowed file guards400. Two subscription module403 controls with fixture-only module toggles.
- Full17-row ordered same-tenant List equals fresh SQL projection (16 API creations plus unpublished local sentinel), foreign published sentinel excluded. Three denied List actors no-write. Two exact selected Publish changes (unpublish/re-publish), five foreign/missing/auth denied Publish controls; all other row fields and every file unchanged.
- Four Student audiences independently enumerated from fixture assignments: A-only, B-only, dual enrollment and no enrollment (global only). Guardian-A matches enrolled student audience. Anonymous, foreign student and another local student's direct profile calls denied. Unpublish hides selected row from student and guardian; republish restores student visibility. Guardian documents=false yields empty resources; revoked guardian access403; completed enrollment leaves only academy-wide resources.
- Every mutation denial and every read compares resources, source courses/batches/students/guardians/links/enrollments, all audit rows and existing upload hashes. Accepted creates preserve all prior owned/foreign resource rows, exactly one added resource and exact actor/route POST audit, timestamps/defaults/nulls/scope/full stored fields, exactly one new matching file for multipart or no file for link. Publish preserves complete unrelated fields/resources/files and records one exact actor/route PATCH audit. Intentional empty responses are not parsed as required JSON.

## Isolation and cleanup

Run6f6c33547f0a4992a47dd195973b6394, loopback SQL port60118,73.4 seconds. Run-owned database/login removed after negative mismatched-marker cleanup refusal; exact labelled container stopped/removed, owned host root absent and port not listening. Existing unrelated QA containers left untouched.

Both migration histories82 domain/7 Identity and runtime313 routes/302 controller method-routes/10 framework Identity entries verified. Runtime inventory SHA256562F95AD3C4514C384CCC11317E91174D0BAB7203E6AA7F162FCD74E8BE81A99 unchanged. Normal local DB/services/binaries, Azure/customer data, outbound providers, commit/push/deploy untouched.

[Source snapshot](PHASE_2B_RESOURCE_SCOPE_SQL_SOURCE_SNAPSHOT.json)/[validator](../tools/validate-resource-scope-sql.cjs) preserve accepted source/evidence/binary chain, including unchanged controller/frontend/schema and prior912 TRX, with declared QA dispatcher/wrapper/checkpoint successors only.

## Remaining gates

This is not browser/device or full-portal release acceptance. OPEN: UI success/error/selector behavior and physical Android/iOS, all-linked/critical acceptance, legacy contradictory records, batch-subject reassignment races, archive/inactive policy decisions, audit/upload persistence-fault rollback, large/multi-format media, preview/native download/private Blob execution, non-admin/custom-role breadth and portal30-row limit behavior. No deployment or issue closure. Use Sol High for the next bounded accepted compliance identity gap; Astra only for a genuinely unresolved policy/architecture decision.
