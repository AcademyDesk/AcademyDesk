# Private microphone recording and download — 8 November 2026

Edge used its built-in fake microphone, the exact-lock Academy Desk export,
real Identity and the existing loopback-only disposable SQL/private Azurite
bridge. The browser verified the 658 exported files. Teacher, student and
guardian views ran at 390px. The unassigned Teacher used a separate context.

| Check | Verified outcome |
| --- | --- |
| Microphone denied | A browser-level permission denial left the selected synthetic file and title intact, kept upload available, and sent no upload request. |
| Recording and review | The Teacher started, paused, resumed and stopped a MediaRecorder recording. The browser showed the local review before the user-confirmed upload. |
| Private upload | The real API created one session, staged one chunk and completed once. SQL showed one completed session with the exact 17,677-byte recording length. |
| Student and guardian | The student preview decoded the recording. Both authorized native downloads preserved the filename, exact length and SHA-256 of the Teacher's local recording. |
| Denied content | Anonymous GET returned 401. The same-academy Teacher assigned to another batch received 404 and no recording. |

The first run passed the recording, student download and denial controls.
The expanded run `0cadca51d9b24100b2a6955301f9214f` also covered denied microphone
permission and linked-guardian download. Edge recorded 55 API responses
without a rate limit or page error. The owner-checked cleanup removed the
disposable SQL database, login and the two owned containers. Both original
PENTA services and Docker inventory were restored.

Keep BUG-SEC-0001 OPEN for the physical Android/iOS and other-engine checks,
real hardware permission behavior, large-transfer and wider critical coverage,
and staging recovery. This scope uses synthetic desktop capture and private
local storage.
