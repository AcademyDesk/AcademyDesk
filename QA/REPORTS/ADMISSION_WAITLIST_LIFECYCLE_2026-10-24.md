# Admission waitlist lifecycle — 24 October 2026

## Final scoped outcome — PASS

- [x]SQL `a423cd348ab14082a79e7d00331770b9` PASS78ordinary HTTP checks,
  10lifecycle outcomes/three barrier-released final-waitlist-place races/two real
  audit failure500/exact rollback/recovery200 controls. No429.
- [x]Window `fb9b3552625541d8aabc587d96bfbbf8` PASS; owned SQL/API/network
  cleanup, exact original services healthy/full inventories/lease release PASS.
- [x]Independent source-pinned lifecycle qualification PASS.140focused QA-tool
  tests PASS, zero failed/skipped; application source unchanged484pins.
- [ ]Issue/physical-device/wider transitions/release remain OPEN; no app repair
  was needed for these additional cases. See separate UI feedback finding below.

```powershell
node QA/tools/admission-lifecycle-qualification.cjs a423cd348ab14082a79e7d00331770b9 fb9b3552625541d8aabc587d96bfbbf8
```

SQL receipt SHA256 `7aaa6f6956e5d478888effca180f55389c49702c33e0f70e87db7f2bda628007`;
window receipt `50b5b8772848dfc117ff32f0602a49065c7d7f8429e6779bc1a1f98ab245f2b1`.
An early read-only qualification correctly refused before restoration finished;
no guard or assertion was waived, acceptance came only after exact finally.

Separate bounded UI finding: enrollment409 always claimed duplicate Active,
masking actual full-capacity, missing-prerequisite and waitlist/closed denials.
That repair and its new-source validation are tracked independently in
[feedback report](ENROLLMENT_CONFLICT_FEEDBACK_2026-10-24.md). The SQL waitlist
batch itself changed no application code and does not prove the later UI binary.

## Scope

Sol High, existing E1/E9/BUG-DATA-0006 continuation. Local worktree
`D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved work.

QA-only: no application/UI/model/permission/schema/business-policy changes,
commit, push, Azure, production data or external model work. Existing immutable
development API build `f42f065951044f48bf7e55b75bc9727b` remains source-pinned.
Its2,927API tests and accepted policy/payload proof are retained, not rerun or
relabelled. No frontend image rebuild or physical-device claim.

## Missing cases selected

- Existing Active row enters Waitlisted with canonical status and trimmed reason.
- Existing Waitlisted row can be reaffirmed at full waitlist capacity without a
  replacement row; activation retains its ID/student/batch/start date.
- Paused→Waitlisted and Waitlisted→Paused retain the same row and exact fields.
- Disabled/full-policy/closed-batch/prerequisite/blank-reason/unknown-status
  denial paths retain captured domain/Identity/audit snapshots.
- Three real barrier-released pairs compete for the final waitlist place:
  POST Waitlisted for another learner versus PUT existing Active→Waitlisted.
  Accept either valid serial winner, exactly one successful mutation/audit,
  one Waitlisted row and exact original learner status/total rows.
- Required audit failure500 must roll back Active→Waitlisted and
  Waitlisted→Active separately; recovery200 mutates the same original row with
  exactly one audit.

This validates existing behavior only. It does not decide new terminal-status,
reactivation/date, retroactive prerequisite or capacity-reduction policies.
It does not repair corrupt historical duplicate rows or add database uniqueness.

## Current run

Window `fb9b3552625541d8aabc587d96bfbbf8`, SQL
`a423cd348ab14082a79e7d00331770b9` PASS as recorded above. Unchanged3GiB guard admitted at
3.316GiB. Existing role/tenant baseline and original sequential activation-capacity
negative retained before the new waitlist cases. Own disposable academy explicitly
enables AcademicGovernance; no production authorization waiver.

Acceptance requires10lifecycle outcomes, three last-place pairs, two audit fault/
recovery controls, all actual HTTP/SQL assertions, source manifest unchanged,
run-owned cleanup and exact original services healthy/full inventories/lease
release. No passing result was claimed before final cleanup/restoration.

Two new oracle tests PASS: exact row/status/date/nullable fields and negative
controls for double winners, excess rows, incorrect source status or extra audit.
GUIDs compare as case-insensitive identities; status/reason/dates remain exact.

## Boundaries

BUG-DATA-0006, all11broader packages, standard source-Dockerfile release build,
HIGH dependency, Mini qualification/full90%AI, all-screen/device/qualified-review/
operations/staging/release gates remain OPEN. No duplicate enterprise audit or
claim that successful API/SQL cases complete production testing. Next route High
for consequential domain/native integration; Medium for settled visual work.
