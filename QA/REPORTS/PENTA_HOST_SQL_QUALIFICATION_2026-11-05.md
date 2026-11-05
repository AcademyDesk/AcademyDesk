# L1 protected PENTA host SQL qualification — 5 November 2026

Status: HOST-ONLY MODULES PASSED. Model, frontend and release acceptance remain OPEN.
Base: `8d7a02c87d867d9f1609aa524d3f3a04f0d7a934` on
`codex/penta-host-integration-20261030`. No application/domain/model source change
was needed in this packet; the only code repair is the teacher SQL snapshot helper.

## Actual validation

| Existing module | Actual owned SQL run | Result |
| --- | --- | --- |
| PentaHostMigration | `e10f43c917954d02bbd5f6dff0f1250b` | PASS |
| PentaFoundation | `22ea1c7a42dd4bab953747693c84c9bb` | PASS |
| PentaOnboardingProtected | `6f67e6bfb85e403897769a70c9bc481d` | PASS |
| PentaTeacherProtected | `943a808cc446466f85c45356942a240f` | PASS |

First window `a46a5f2b7b614e8e869acf04620206b6` passed migration, foundation and
protected student modules. Teacher failed before its no-write checks because an
empty nested SQL JSON snapshot was DBNull. Failure and owned SQL fixture
`1776b1c4243a42cfb2ea03e8cbb8fb45` are retained; its exact label/name/loopback/no-mount
ownership was verified before stopping it. No deletion/pruning or evidence loss.

The helper now uses SQL COALESCE to represent an empty present table as `[]`,
rejects non-string snapshots and asserts empty mapped/fixture tables explicitly.
Absent-schema, actual mapped-table, full 12,000-character row and tail-change
assertions are retained. The shared teacher service cases increase from 17 to 18.
Application/API tests and other module sources are unchanged.

Teacher-only successor `e7cb191030fa4fb6b9c32d091d832a52` rebuilt the harness with
zero warnings/errors and passed 18 shared creation, 30 protected execution,
21 Core, 43 conversation and 46 startup/recovery cases. These distinct teacher
subgroups are not an application coverage percentage. The real .NET/Python
socket bridge in the student module passed with the scripted pure Core provider,
including private-context denial, authority/catalog drift and interpreter outage.
Student shared checks include 23 additive cases and five Manual/service duplicate
races; retained atomic/direct checks are unchanged. Foundation includes 16 read
privacy controls. Migration includes four real upgrade/rollback/re-upgrade checks.

The 1,976 API results are reused only after exact application/test-source and
TRX verification from `cc576fb68b674d6bbdcabfae04260abd`.
Earlier three SQL modules retain their own source-bound results. Only the private
teacher snapshot helper changed between windows; its callers are the two teacher
dispatch modes in Program.cs. This is not a claim that the corrected binary ran
all four modules in a single new window. Current full CI remains OPEN.
Ignored local source proof: `.build-check/penta-host/l1-combined-source-proof.json`.
Corrected helper SHA256: `b7c7613ccc08733d0e8ac8a561270a2d300d462f64762cd2c84c586fb42462b6`.

## Resource handling and restoration

The owner authorized automatic safe development-memory reclamation. The first
window passed the unchanged 3 GiB guard after stopping only the two original
PENTA services. No unrelated user application was closed or killed.

Controlled Docker restart `8cd9b5dc7dcb40d39aab5c53c47997e1` completed and restored
original service IDs/images/health/API200. Containers, volumes and images were
unchanged; the default bridge network ID regenerated while the PENTA custom
network and other network IDs stayed unchanged. Its strict full-ID inventory
receipt therefore remains FALSE; it is not rewritten as an exact inventory pass.

For the teacher-only window, stopped-Docker Linux page cache was safely flushed;
the unchanged RAM guard admitted at 4,124,409,856 free bytes. No downloads,
pruning, model candidate loading/training/generation/activation or paid compute.
Final teacher window verifies exact original service IDs/images, healthy state,
API200, full post-restart Docker inventory unchanged and shared lease released.
All successful run-owned SQL databases/logins/containers were cleaned by the
existing ownership guards. Earlier failed fixtures and raw evidence stay local.

## Scope and next work

- Protected writes stay Testing-only; current pilot is Owner/AcademyAdmin.
- This is host enforcement with scripted interpretation, not real model quality.
- Cloud C1 Mini/Core portability and C2 read-only operation mapping remain assigned.
- Local next: combined runtime/release acceptance after cloud's qualified source
  and operation-gap handoff. Actual model experiments retain the owner's stop.
- Exact-lock web/current CI, all-role connected UI/device checks, remaining
  conversational cohorts, legacy migration path, privacy/Blob/restore/staging and
  critical/release gates remain. No main merge, Azure, customer DB or issue closure.

Route: Sol High for remaining integration/authority/runtime work; Sol Medium for
settled UI/docs. Preserve the larger existing enterprise testing program.
