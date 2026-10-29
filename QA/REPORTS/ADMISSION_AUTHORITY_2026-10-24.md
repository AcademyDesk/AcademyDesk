# Enrollment host authorization successor — 24 October 2026

Missing-case E1/E9/BUG-DATA-0006 continuation on current development image
`f42f065951044f48bf7e55b75bc9727b`, no API/frontend/model/authority change.
SQL `62f710d08001447492fe535e0ab2ae81`, window
`dc75a736c5974fadb35aa389d2f3d032` PASS.84HTTP checks, all31denials and both
authorized readbacks passed; all12domain/identity/permission/audit hashes unchanged
after each attempted operation. No protected foreign sentinel disclosure/no429.
109migration histories verified. Owned SQL/API removed, exact two original
services restored, full inventory identities preserved, exclusive lease released.
Independent qualifier PASS;148focused tooling tests PASS,0failed/skipped.

```text
node QA/tools/admission-authority-qualification.cjs 62f710d08001447492fe535e0ab2ae81 dc75a736c5974fadb35aa389d2f3d032
```
SQL SHA256 `558ca8e0b9bf72de408fbe782cee3867f9821043ba081059e144c64c498660c9`.
Window SHA256 `5f9d1413f2d99da3cbeec9c8d976159911c1ec627f8012d4571a232491fd6609`.

## Bounded acceptance

Real Identity teacher/student users are linked to synthetic own-academy records.
No roles or grants are elevated. Before the retained owner-deactivation security
test, each real owner seeds its own course/two batches/student/enrollment/pending
promotion through the API. Synthetic bindings are restricted to disposable SQL.
The deactivated owner is never reactivated or used to bypass the existing check.

- Teacher and Student own-tenant: reject general enrollment list/create/update/
  transfer, promotion approval/list, batch update (7each) with403.
- Other active owner using the foreign academy route: same7reject403.
- Active owner using own academy route with foreign record identifiers: reject
  foreign enrollment/transfer source/transfer target/promotion/batch update404;
  foreign student/batch/promotion source400.
- Anonymous enrollment read/create401.
- Authorized owner own enrollment/promotion reads200, own records present and
  foreign private sentinel IDs/names absent.

After every attempted operation, exact hashes of12domain/identity/security/audit
tables must be unchanged. Each denied response must exclude protected foreign
sentinel values; no rejected success audit, role/grant change or domain mutation.
31denials+2authorized readbacks,84ordinary HTTP checks including setup. No429.
Actual disposable SQL/Kestrel/Identity, no response mocks/token injection or model
claims as authority. Original services restore/exact inventory/cleanup/lease and
unchanged3GiB guard remain mandatory, no restart/prune/acquisition/Azure.

## Limits

General enrollment administration negative checks are not student self-service
or complete guardian/UI/custom-grant coverage. This does not prove prompt-
injection resistance or all PENTA actions; existing host-security and tenant tests
remain required. Full enterprise/device/release gates and the issue remain OPEN.
