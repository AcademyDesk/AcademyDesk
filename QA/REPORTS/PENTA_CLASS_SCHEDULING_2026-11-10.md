# PENTA regular class scheduling — 10 November 2026

Base `7d5c013c7312a91a6bb472e86d3a7d8a4d34c891`.
Cloud C28 at `a713a506853733215111d4fb29fc725f14c521d8` covers delegated
grading. Its scheduling files match the imported prior Desktop checkpoint.

Manual creation and rescheduling use the resolved teacher and branch to
check overlaps. Moving or reopening a session excludes its own row from the
comparison. Cancellation releases the slot, adjacent times remain legal,
and the same room name can be used at another branch. A batch can have only
one live session starting at an exact time, including batches with neither
teacher nor room assigned. Manual and protected PENTA writers use the same
academy SQL lock. The class and required actor audit commit together.

The `AcademyDesk.ClassSession.Create` revision 1 host route requires
`Penta:Enabled` and `Penta:ClassSessionSchedulingEnabled` in Testing.
The host verifies the current Owner or AcademyAdmin, tenant, subscription,
security stamp and conversation. Typed requests require a future UTC slot,
an optional physical room or a required HTTPS link, and values within the existing schema.
The preview resolves optional teacher and branch defaults. Explicit
`ApproveClassSessionSchedule` authorizes the exact preview. Execution
rechecks the current assignment and slot, commits the protected receipt with
the class and audit, and returns the same receipt on an authorized replay.
There are no meeting-provider calls.

## Local verification

| Check | Result |
| --- | --- |
| API and SQL harness | Built with zero warnings or errors |
| Schedule form logic | 18 controlled tests passed, including 12 real TSX payloads replayed in SQL |
| ScheduleDefaults native HTTP and SQL | 18 retained Manual and 43 new checks passed, run `5ff0890ed15b49038c1942ef300bd025` |
| MakeupLocation native HTTP and SQL | 50 retained Manual and 31 protected workflow checks passed, run `6479aa98867148ed95726cefc666c21e` |
| Production guard tests | 15 passed. Current release guard still blocks on five P0s and the session security gate |
| Validation window | `c30f2eb1ca6e496a8607046deb4ec2b8`, RAM 1.74 to 3.67 GiB after cache reclamation against the unchanged 3 GiB guard |

Native checks used real Identity bearer authentication and disposable SQL.
They verified inherited and explicit teacher conflicts, inherited branch
conflicts, Manual rescheduling, cancellation and reopening, tenant and
role denial, unsafe location refusal, stale context and default assignment
drift, tampered approval and audit rollback. Simultaneous Manual and PENTA
writes to the unassigned batch slot admitted one class. In each delivery mode,
two PENTA executes returned one HTTP 201 and one HTTP 200 with the same
receipt. The retained make-up workflow passed in the same final window.

The first window `643d937d2ca24aabbcbe8ed3ac52ffb0` built and completed
ScheduleDefaults with exit 0, but its log summary was filtered by the
runner, so the wrapper did not accept it or run the next module. The
final window used the existing schedule log prefix, added the unassigned
duplicate case, and reran both modules. Owned SQL resources were removed,
both original PENTA services were restored and the API returned HTTP 200.
Docker inventory was preserved.

BUG-DATA-0005 remains OPEN for batch teacher propagation, live browser
and device coverage, and linked critical acceptance. PENTA rescheduling,
assigned-Teacher attendance, the conversation UI, Mini qualification and
production gates remain open.
