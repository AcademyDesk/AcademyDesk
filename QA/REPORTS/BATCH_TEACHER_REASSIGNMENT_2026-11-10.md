# Batch teacher reassignment — 10 November 2026

Base `fed29147cee23c272073a8da9131fc709b722a76`. Cloud C30 at
`51cd043c68c637d05726debd61beb21e100e29f9` covers a separate maintenance
acceptance packet.

A batch rename or status change with the same default teacher leaves all
class rows unchanged. Changing the default updates only future Scheduled
sessions that follow it. Explicit substitutes, past classes and classes
with another status keep their assignments. A null default clears only
following sessions. Their timestamp changes with reassignment so a stale
protected reschedule needs a fresh review.

The additive `20261110112020_TrackClassSessionTeacherInheritance` migration
stores the choice in nullable `ClassSessions.FollowsBatchTeacher`. New
class creation treats the batch's default teacher as following the batch,
including when the current UI sends the same ID explicitly. A different
teacher is a substitute. Legacy records start with unknown provenance.
At reassignment, a legacy class whose teacher matches the previous default
is treated as following it. A different legacy assignment is preserved and
recorded as a substitute when it would otherwise become indistinguishable
from the new default. A downgrade refuses to discard recorded provenance.

Batch edits acquire the academy scheduling lock before the admission lock.
The proposed teacher must have no overlapping non-cancelled regular class,
including a substitute assignment within the same batch or overlap among
the sessions being moved. Batch and affected class rows share the existing
transaction and actor-audit boundary. The pre-existing Platform Owner
audit policy is preserved. Protected class creation binds whether the
teacher follows the batch to its preview and rejects a later change even
when the resolved teacher ID stays the same.

## Verified locally

| Check | Result |
| --- | --- |
| API and SQL harness | Built with zero warnings or errors |
| Focused API tests | 24 batch preservation and enrollment admission checks passed |
| ScheduleDefaults | 87 retained Manual and protected scheduling checks passed, run `57722244de1c47fcb48a48908e105377` |
| BatchPreservation | 27 retained batch checks, 13 new reassignment checks and 2 migration checks passed, run `5cb826242ff44ab89cce29c89e3e9338` |
| PentaHostMigration | Four retained migration checks passed, run `b7e02f3d48d147cfb69098ff9df8d526` |
| Production guard | 15 guard tests passed. Five P0 records and the session security gate still block release |

Native Identity, HTTP and disposable SQL verified default propagation,
substitute preservation through consecutive changes, cancelled and adjacent
slots, tenant isolation and rejection of clashing slots. Two concurrent
reassignments and a mixed Manual class-create race each admitted one
winner. Required audit failure rolled back the batch and class changes.
The migration preserved a populated legacy row and refused to remove
the provenance column after it recorded an assignment. The retained
PENTA migration checks restored the full latest migration history.

Window `38d56e405a7342cd90ec972628b27093` passed the three SQL modules.
The final window `671d3fa42f474f80a41e538fd0bdd96f` repeated them after
retaining the original Platform Owner audit scope and added the focused
API suite. It reclaimed RAM from 2.49 to 3.22 GiB before admission against
the 3 GiB guard. Both windows removed their disposable SQL resources and
restored the original PENTA services, with Docker inventory preserved
and the restored API at HTTP 200.

BUG-DATA-0005 stays OPEN for browser, physical-device and linked critical
acceptance. Conversation UI, model qualification and production gates remain
open. This migration was exercised on disposable SQL only.
