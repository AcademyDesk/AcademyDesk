# Protected PENTA class rescheduling — 10 November 2026

Base `5927974bd6c2dd0646fa8287a70ccda711ecfdc4`. Cloud C30 maintenance
enforcement at `796e8c4aaa2cd56ad3b0f19316e966aab1ff5641` is a separate
acceptance packet.

The Testing-only class scheduling route accepts
`AcademyDesk.ClassSession.Reschedule` revision 1 when
`Penta:ClassSessionReschedulingEnabled` is enabled with the existing PENTA
and class scheduling flags. The current tenant Owner or AcademyAdmin reviews
the exact source and proposed slot, including delivery mode and room or
HTTPS link, and approves it with `ApproveClassSessionReschedule`. Only a
future Scheduled class can move. The operation preserves the class ID,
batch, teacher, branch, status and linked attendance.

Preparation and approval leave the class unchanged. Execution checks the
original slot and update timestamp, current assignment and destination
clashes under the Manual scheduling lock. A Manual schedule save stamps the
existing update field, so moving away and back also invalidates the draft.
The class move, audit and protected receipt commit together. Competing
approved moves can update the original source only once. An authorized retry
recovers the same receipt and creates no further audit or class write. This
operation changes the schedule without queuing communications.

## Verified locally

| Check | Result |
| --- | --- |
| API and SQL harness | Built with zero warnings or errors |
| ScheduleDefaults | 26 new reschedule checks and 61 retained scheduling checks passed, run `2b02d32f7d5549d19bbd3e4ed8bb8336` |
| AttendanceNotes | 50 retained Manual and assigned-teacher checks passed, run `669921938b924b5383a6377a3e39dbdb` |
| Production guard | 15 guard tests passed. Current guard blocks on five P0 records and the session security gate |

Native HTTP used real bearer authentication and disposable SQL. Tests
covered feature and role denial, foreign classes, past or non-scheduled
sources, unsafe links, injected status or assignment, wrong approval
intent and ID, late slot conflicts, a Manual move away and back, preserved
attendance, rollback on audit failure and concurrent execution. The
retained SQL modules reused the existing schedule and attendance form
payloads from the unchanged frontend source.

Window `20839769ab0f4c0c9e19fe71df0a1c85` started with 3.35 GiB free
against the 3 GiB guard. The window removed its disposable SQL resources,
restored both original PENTA services and verified the restored API
at HTTP 200 with Docker inventory preserved.

Batch teacher propagation, browser and device acceptance, conversation UI,
Mini qualification and production gates remain open.
