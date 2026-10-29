# Effective individual grants — 24 October 2026

QA-only continuation of host authorization/E1/E9 on existing immutable development
API build `f42f065951044f48bf7e55b75bc9727b`. No role/catalog/application/frontend/
model/schema changes or rebuild. No production or real user grants.

## Bounded acceptance

Same actual Teacher bearer throughout; an active synthetic Owner uses existing
AccessGrants POST/PATCH endpoints in the disposable academy. No token injection,
role edits or model-supplied identity. General enrollment access initially denied;
explicit temporary students.manage grant allows read and one real enrollment.
Finance, foreign academy, self grant/revoke remain denied. Foreign target and
unknown/past-expiry commands denied without writes/audits. Admin revoke then
same-bearer read/create/update403immediately. Another valid temporal grant reads200;
the exact run-owned persisted expiry is changed to a fixed past timestamp only
as a synthetic fixture, then read/update403. This is not a wall-clock boundary
race or a production expiry edit.

Two independent permanent students.manage/academics.manage grants then allow
their respective reads; revoke one, student scope denies while academic scope
continues; revoke second, both scopes deny. Role/user/role-membership hashes stay
unchanged throughout. No rejected mutation/read changes any of13snapshot tables.
Exactly four grant rows (three revoked/one expired), one active enrollment and
eight success audits for four grants/three revocations/one enrollment. No429.
Expected70HTTP checks including baseline/setup/30permission cases plus the
absent-AccessGovernance subscription403control. The run-labelled container/image/
database/teacher-user/academy/module-list are checked before enabling only that
module in the exact disposable synthetic fixture, not a host permission bypass.
The retained
owner-deactivation and admission-capacity negative are still required afterward.

## Limits and safety

No grants exposed as PENTA tools or permission expansion; these are existing
authorized manual-domain controls. This packet does not qualify AccessGrant/
audit transaction rollback, concurrently admitted requests, natural expiry edge,
guardian rights, custom-role replacement, UI labels or full security/release.
Existing issue records/testing remain OPEN; no new issue count or inference test.
Only exact owned SQL/API resources; approved two-service temporary stop, unchanged
3GiB guard, exact healthy original restoration/full inventory/lease finally.
No restart/prune/download/Azure/main/Mini or credentials/customer data access.

First SQL `2cf38aa2847f4eaab31f6e3ebf340b0f` stopped at grant creation403:
AccessGovernance was absent from the disposable academy. Window
`b21a169e91a14be6a2867101d8187839` remains FAIL, exact originals healthy and full
inventory preserved. No application permission was weakened. The corrected
fixture explicitly first checks the absent-module403, then enables the module
only after exact run/container/image/database/user/academy/module guards.

Corrected SQL `9e4ebe3e0cf34612aa20b32178c29d98`, window
`47cbc97b4c97455885b1d89ec7595002` PASS. Independently qualified25October:
70HTTP checks/30permission cases, four grants (three revoked/one stored expired),
one active enrollment/eight success audits. Same Teacher bearer and unchanged
Identity role/user/member hashes. Denied/read operations leave13tables unchanged,
no429. Retained owner deactivation/admission refusal PASS. Exact original services
healthy/full Docker inventory/owned cleanup/exclusive lease release PASS.

SQL receipt SHA256 `0759bca62434683f5da4836228a1e689018a3835d03151581bd49ed6626fc6f2`.
Window SHA256 `d63f32ee8b8be72d2768fb11805584a1721b03853ba33ee0ad14ca250e4cb8f6`.
This qualifies the bounded existing grant behavior, not audit-fault atomicity.
