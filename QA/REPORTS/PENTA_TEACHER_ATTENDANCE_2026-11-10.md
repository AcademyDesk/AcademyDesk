# PENTA assigned-teacher attendance — 10 November 2026

Base `29c0a940102e1436304e67c16cd339c1d81204b1`. Cloud C29 guardian access
and C30 maintenance do not overlap these implementation paths.

Teacher Manual reads and writes resolve the authenticated user's current
Teacher role, active academy and teacher, and the specific class assignment.
The shared writer verifies active enrollment and academy ownership, rejects
duplicate learner rows in a bulk save, and preserves the existing Present
and Absent statuses and trimmed optional notes. The attendance changes,
queued Student InApp notices and actor audit commit together. The academy
attendance write also takes the same SQL lock, with its existing statuses
and response preserved.

The Testing-only `AcademyDesk.TeacherAttendance.Mark` revision 1 host route
requires `Penta:Enabled` and `Penta:TeacherAttendanceEnabled`. It prepares one
learner's attendance mark and exact InApp notice without writing attendance
or a notice. The protected draft binds the actor, assigned class, payload
and current attendance. `ConfirmTeacherAttendanceAndQueueInApp` must carry
the reviewed digest. It rechecks assignment, enrollment, class and prior
attendance, then saves and audits in one transaction. Recovery returns the
existing receipt while the teacher remains authorized. The notice state is
queued, with no claim of delivery. The first draft expires after 10 minutes
and a successful receipt is recoverable for up to 24 hours.

## Verified locally

| Check | Result |
| --- | --- |
| API and SQL harness | Built with zero warnings or errors |
| Controlled attendance form | 24 passed, including 10 actual TSX payloads consumed by the SQL run |
| AttendanceNotes | 16 retained Manual and 34 new assigned-teacher HTTP and SQL checks passed, run `fdb1ec113f1b4b5da143677bfdd4186f` |
| ScheduleDefaults | 61 retained Manual and protected scheduling checks passed, run `e84c52a3163f4e8cbb05f5ac8b07204c` |

Disposable SQL used current Identity bearer accounts. The suite verified
unassigned, foreign and inactive-teacher refusal, duplicate bulk and
non-enrolled learner refusal, exact marks, notes, notices and attributed
audits, and no early write. Assignment change, enrollment withdrawal and
a newer Manual mark stopped a previously reviewed draft. Manual and PENTA
audit failures rolled back attendance and notices. Two concurrent
confirmations returned HTTP 201 and 200 and the same receipt, with one
attendance write and one queued notice. A fresh receipt read made no write.

The initial window `352de5db3d114a169b04882a2efd2b50` built the API and
stopped before SQL when the new test source was missing a namespace import.
Window `04050c9777eb404c8eca5a022375f027` corrected the test import and
reran both modules with 3.11 GiB available at admission against the 3 GiB
guard. Each window restored the original PENTA services. The final run
removed its disposable SQL resources, preserved Docker inventory and
verified the restored API at HTTP 200.

PENTA attendance is limited to the Testing host and one learner per review.
Manual bulk remains available. Conversation UI, model qualification,
browser and physical-device verification, and production gates remain open.
