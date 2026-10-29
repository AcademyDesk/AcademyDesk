# Custom-role save/audit atomicity — 25 October 2026

Existing BUG-API-0002/E1/E9 missing-path continuation after qualified grants and
regular payroll. No new PENTA security tool, role authority, tenant policy or
system-role rule. Use current ordinary Owner domain roles/Create and Assign,
same synthetic Teacher bearer and current server-resolved permissions.

## Acceptance and limits

Temporary exact owned SQL AuditLogs INSERT denial surrounds actual role Create
and existing staff role assignment. HTTP500must leave all11domain/Identity/audit
table snapshots unchanged. In particular, failed reassignment must keep the old
students.manage scope and deny academics.manage; successful retry must change the
effective rights with one success audit. Two normal custom-role creates plus
initial/recovery assignments yield four success audits. No unrelated domain or
role writes, self-role creation or unauthorized finance access; no429.

Baseline mode separately records persisted unsafe changes if reproduced. It
never qualifies rollback. Reuse existing action-level shared Identity/domain
save-and-audit mechanism if needed; no app change before completed reproduction.
Do not change Staff role-replacement safeguards or invent policy for protected
Owner/Admin assignment. Whole role-management authorization, global role-name
uniqueness, wider Identity failures/concurrent reassignment, platform
bypass, frontend/devices and broader issue/release remain separate.

## Current evidence

- Syntax and proof-negative controls PASS. Exact extra QA PUT route is academy/
  roles/staff/GUID/assignment only, with malformed/foreign write-shape refusal.
- Baseline SQL `d0ce1a6259504cbaa72eb0af960011d8` PASS49ordinary HTTP checks/two
  reproduced500unaudited mutations. Failed Create persists a custom role; failed
  Assign changes membership and same original Teacher bearer from enrollment200/
  academic403to enrollment403/academic200. Unrelated tables unchanged, four normal
  success audits; finance/self-role denials retained. SQL source/owned cleanup/
  full inventory PASS. Parent `6d5268074f8546149b828989cdd15a79` PASS/exact
  original services healthy/full Docker inventory/lease release.
  Successful reproduction is explicitly NOT protection acceptance.
- Minimal repair adds existing `AtomicAcademyMutation(IncludeIdentity = true)`
  only to Create/Assign plus namespace import. List/role policies/catalogs and
  action bodies stay byte-equivalent. Six action-boundary/Identity tests prepared;
  full regression/fresh development build window
  `bd3c3fb6263445e4a2eb00e95f47f8ac`: full2956/2956API tests PASS/zero failed/
  skipped. Offline development image `b9d447d5eba74a19a8ef22a17de66628`,
  `sha256:2b4aba271aecfe31b987d41aceebad2806ff8c88455298f1a224acef527a1516`
  built with65published files/93,741,059bytes, no downloads. Build window PASS/
  exact original services healthy/full inventory with only intended image added.
  Repaired native window `505fb9cd09a949e2801814987f31793e` PASS. This is
  development runtime evidence, not standard source-image/release acceptance.
- Additional repaired-run control denies INSERT only on owned AspNetUserRoles
  after the ordinary assignment action starts. If old-role removal saves before
  the new membership insert fails, the shared transaction must restore it, all11
  snapshots and same-bearer enrollment200/academic403. Exact fault revoked in
  finally; normal recovery then requires one success audit. This supplements,
  rather than relabels, the two earlier baseline audit-failure reproductions.
- Approved two-service temporary stop, unchanged3GiB guard, no downloads/restart/
  pruning/main/Mini/Azure/customer data; exact original healthy restoration/full
  inventories/owned cleanup/lease required. No issue/production closure inferred.

## Qualified repaired result

SQL `8da09467be40454697ff326e61065c72` PASS51ordinary HTTP checks, with no429
or unexpected5xx. The two deliberate audit INSERT failures and one membership
INSERT failure each returned500and left all11domain/Identity/audit snapshots
unchanged. Failed assignment and failed replacement preserve enrollment200/
academic403for the original Teacher bearer; recovery changes that same bearer
to enrollment403/academic200. Two role creates and two assignments produce exactly
four success audits. Unauthorized finance and self-role creation stay denied.

The parent window passed exact original service/image healthy restoration,
complete Docker inventory preservation, owned SQL/API cleanup and lease release.
`role-audit-qualification.cjs` PASS binds this baseline, repair,2956test TRX,
current484source pins, new development image and both native receipts. The only
application delta from the preceding payroll image is the two transaction markers
and namespace import; removing them reproduces the previous file hash.

Guard SQL receipt SHA256:
`393ac21289b588bba5579eba7066e0b8da8229a2c68d94ec405bc52c5a610c5f`.
Parent receipt SHA256:
`cb42b33699c97837e01195edf9585efacaeda1faf9d085a6ef9405f297b6e9cc`.
Later actual-image inspection `48e15528e1d44af08a65abffcb3aa9a1` independently
passed all65file/93,741,059byte hashes against the role image's published manifest,
source unchanged/unstarted inspection container removed. Receipt SHA256:
`3e40c5ff8d1dbb3b72a48ce2a06f83aa2acaebcc8205c68cffb4099410af8df7`.
Its parent Access Review browser fixture failed before any grant request and
remains FAIL; no broader browser acceptance is inferred from this payload pass.
No standard source-Dockerfile release, physical-device, whole authorization,
concurrency or production acceptance is claimed. BUG-API-0002 remains OPEN.
