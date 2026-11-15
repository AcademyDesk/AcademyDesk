# PENTA class scheduling live browser acceptance

15 November 2026 · Windows desktop · `codex/penta-host-integration-20261030`

Academy Owners and Academy Admins can open the class scheduling preview from
the PENTA workspace. The guided Executor composer recognizes a schedule or
reschedule request by batch name. The user selects a verified academy record,
enters the UTC slot and reviews the exact backend plan before separate
approval and execution. Offline uses an optional room. Online and Hybrid
require an HTTPS meeting link. Manual Sessions is available from the preview.

Availability comes from the existing C43 Testing-only host. Lookups and
receipts require private responses. The browser saves only scoped opaque
recovery identifiers. It reads back the same operation after reload and
keeps unconfirmed outcomes visible. The server continues to enforce current
actor, tenant, schedule clashes, exact approval and required audit.

## Results

| Check | Result |
| --- | --- |
| Next.js 16.4.3 production build, TypeScript and static postbuild | Passed. 196 input files bound to the worktree and 681 static files produced. |
| Scheduling client contract | 9 tests passed, including wrong-tenant data, cached responses, altered approvals and read-only recovery. |
| Live Admin and Owner roles | 4 browser groups passed against disposable SQL. Academy Admin scheduled one Offline class at 390px. Owner moved one existing class to a Hybrid slot at 1440px. |
| Lost response | The Owner's save committed, then its response was dropped. Reload recovered the exact receipt through GET with one execution POST total. |
| `ScheduleDefaults` | 95 native HTTP/SQL groups passed. Fresh SQL confirmed both exact slots, preserved assignment, one added row, the original moved row and one execution audit per role. |
| Cleanup | Both original PENTA containers restored with identical images and Docker inventory. PENTA API health returned 200. The run-owned SQL container, database and runtime login were removed. |

Validation window `93ed21c99110482b9a99743ddc5dd716`, SQL run
`b496d853610f4f89baa1b08dfca4a07a`, harness SHA-256
`0eb2e58667093c614bd3b3f91a4f6cd0272dec4205a04b6528165b2069257baa`.
Host source remains pinned to C43 `38cef8ffdf1f088a63df48161c4641cde704c9ea`.

## Remaining acceptance

The composer is a guided preview with two supported prompt patterns. This
run used desktop Chromium at mobile and desktop widths. Physical-device
validation, live make-up and Teacher attendance workflows, private-model
qualification and the measured 90% conversational workflow target remain
open. The production gate checker still reports six critical records.
