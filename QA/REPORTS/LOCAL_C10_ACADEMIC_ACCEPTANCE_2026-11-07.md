# C10 academic and release validation — 7 November 2026

Desktop started from `c153714` and published claim `e63e226`. Cloud C11
`28ad7b9` owns access display, platform status and sales navigation. This
packet reuses the 58 controlled C10 checks and 30 Chromium cases from the
integrated Cloud return. [Cloud verification](../HANDOFF/C10_ACADEMIC_BATCH_2026-11-07.md)

Baseline SQL run `4e0441afbc10408f88df93e7a5a646fa` returned 200 for zero
and negative course-module sequences, and fresh SQL confirmed both writes.
The harness then stopped because it expected JSON from the valid create
action's empty 200. The final harness checks that status and reads back from
the existing list. The application change rejects sequences below one at
create, preserving the valid response, publication behavior and stored history.

Final SQL run `29e7e183e0b04113bdc0e80d74610690` passed 19 checks.

| Area | Real HTTP and SQL proof |
| --- | --- |
| Calendar | A session at the UTC/India year boundary was returned as Scheduled, then Cancelled after update. The stored instant, teacher and location matched the projection. Cloud's controlled calendar already verifies that a Cancelled row hides its join action. |
| Civil dates | Year, term and holiday dates spanning December 31 and January 1 matched fresh SQL and API lists exactly. |
| Curriculum | Zero, negative, missing and fractional sequences returned 400 without writes. Sequence two survived create, readback and publication. |
| Subject fees | INR 1,250.50 and INR 0.01 persisted exactly and matched API readback. Zero was rejected. |
| Authority | Anonymous, Teacher and cross-tenant denials preserved the captured rows and audits. |

The harness compiled cleanly, applied 85 application and seven Identity
migrations, and removed the run-owned SQL database, login and container.
The exact application candidate passed 1,991 API tests, with zero failures or
skips. The original five C10 issue records retain their device and critical
closure requirements.

The local Windows frontend run executed 1,983 tests and passed 1,982, with
zero skips. All three communication checks that failed in Cloud for absent
evidence passed. This run reused the genuine 48-case SQL receipt whose
SHA256 is `00998850f2b43458324020bdedb72e0ac01e5b438b3d0e31f3de18f72379e181`.
Its communication controller, schema, authorization, UI, fixture and test
sources were unchanged. The receipt remains local. The only Windows failure
was EPERM when creating a dangling file symlink before the export assertion.
The corresponding test and export script are unchanged from Cloud C10, whose
Linux run passed that check. Preserve the Windows failure and the separate
Linux pass as scoped evidence. Both original PENTA services, Docker inventory
and API health were restored, and the validation lease was released.

The production gate check remains blocked by five open P0 records
(`BUG-DATA-0001`, `BUG-DATA-0002`, `BUG-DATA-0003`, `BUG-DATA-0010` and
`BUG-SEC-0001`). GitHub's previous integration tip `c153714` returned a
successful Vercel status. The connector reported no PR-triggered Actions
run for that tip. Exact-release workflow and images, critical/device acceptance
and staged restore/rollback remain pending. Review the five P0s against their
existing closure criteria before starting another lower-priority batch.
[Production gates](../09_RELEASE_GATES.md)
