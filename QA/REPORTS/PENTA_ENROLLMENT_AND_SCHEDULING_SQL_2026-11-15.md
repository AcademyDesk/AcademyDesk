# PENTA enrollment lifecycle and class scheduling on disposable SQL

15 November 2026 · Windows desktop · `codex/penta-host-integration-20261030`

Imported the seven C43 source, test and handoff files from
`38cef8ffdf1f088a63df48161c4641cde704c9ea`. Every imported blob matched that
commit, and the existing scheduling workflow matched the Cloud parent before
the import. The scheduling browser host remains in Testing behind its flags.
The enrollment and class mutations reuse their existing domain services.

## Validation

| Check | Result |
| --- | --- |
| Academy Desk API and SQL harness builds | Both passed |
| Focused enrollment, scheduling and browser contract API tests | 317 passed |
| `EnrollmentShared` | 132 native groups passed, including 8 new lifecycle browser groups and the retained 15 mixed admission races |
| `ScheduleDefaults` | 95 native groups passed, including 8 new class scheduling browser groups |
| Production gate tests | 15 passed. The real release checker reported six open critical records. |

### Enrollment

The lifecycle endpoint returned a private readiness response and refused
teacher and foreign-academy access. Admission and lifecycle requests with the
same start key created separate purpose-bound conversations. Active,
Waitlisted, Completed, Withdrawn, Cancelled and Paused each exercised real
Identity, typed review, rejection before approval, approval, execution and
receipt replay. SQL held the original enrollment ID, expected status, end date
and reason. The receipt identified the required actor and academy audit.
Live UI evidence remains scoped to the previously tested Paused and Transfer
journeys.

### Class scheduling

The feature flag, teacher and foreign-academy checks refused access. Private
batch and upcoming-session lookups filtered identically named foreign
fixtures and wrote scoped audits. Concurrent start returned one conversation
and one start audit. A schedule request needed an exact approval before its
one SQL write. The committed class appeared in the source lookup.

The rescheduling draft identified the original session and destination.
Injecting a failure in the required audit rolled back the move and preserved
its approved draft. Retrying concurrently returned one committed move and
one replay against the original row, with the same receipt and one audit.

### Run ownership

- Validation run `113a1167bc3d4b0ba2daba03d29a406c`
- Enrollment SQL run `ba43a44f294a4c3591a4a37a2d621a35`
- Scheduling SQL run `8b57c4eb744f49058d531bfc1123409a`
- Built harness SHA-256 `3bee3e46db9b2d8c8636948b86f8577a1c986e6058eb30be248cacfd42d3abc1`

Both modules used fresh run-owned databases, scoped runtime logins and the
cached local SQL image. The owned containers, databases and logins were
removed. Both existing PENTA containers were restored with their original
images, the Docker inventory matched, and the local PENTA API returned 200.

## Remaining acceptance

Connect the scheduling browser host to the Academy Admin and Owner
conversation interface. Verify the live review, approval and receipt recovery
on disposable SQL before device acceptance. Teacher attendance and the
Student portal remain separate role journeys. Private model qualification,
the measured 90% conversation-to-result target and production acceptance
remain open. This checkpoint used deterministic typed requests without model
activation or Azure.
