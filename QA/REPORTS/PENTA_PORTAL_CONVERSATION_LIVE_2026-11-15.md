# PENTA Student and Guardian conversation

15 November 2026 · Windows desktop · `codex/penta-host-integration-20261030`

The Student Portal now offers guided conversation for the next scheduled
class, upcoming published and unsubmitted work, and outstanding invoice
balances. A Guardian can choose among currently linked learners. Each
prompt refreshes learner links and relevant subpermissions, and the
application rechecks both before reading the result. Unknown or ambiguous
requests give the user a next step. The Manual Student Portal remains
available.

The read endpoints require Testing and explicit PENTA flags. The actor,
academy, current role, security stamp and learner permissions are checked
against linked Identity and academy SQL. Projections omit contact details,
meeting links and submission contents. Fees use the shared deterministic
ledger and carry exact currency amounts. Each successful query records a
scoped audit without recording the prompt.

## Cloud integration

Reused C29 implementation `61abb67e8d16ad88f45f9155b7045709c533caf6`
from the handoff published at `4b9f48af6926175ecfb26af9355c656bf4068207`.
Five source and test files matched the Cloud implementation before adding
the PortalController partial declaration for the new PENTA endpoints.
The existing Manual endpoint now supports explicit revocation and checks
current Guardian activity. Issue statuses and historical raw evidence
remain in their existing registers.

## Validation

| Check | Result |
| --- | --- |
| Next.js production build, TypeScript and static postbuild | Passed with 201 verified frontend inputs and 682 static files. |
| .NET API and SQL harness builds | Passed. |
| Focused guardian API tests | 36 passed. |
| Portal and shared prompt tests | 12 passed. |
| Real Identity, HTTP and disposable SQL | 61 checks passed, comprising 55 retained guardian flag cases and six read-scope denials. |
| Live browser | Three groups passed. A Student at 390px read their own next class and due work. A Guardian at 1440px clarified the learner and received USD 450.00 from the exact ledger. Unpublished, submitted and sibling-private work stayed hidden. |
| Revocation | The Admin revoked one link through the real Manual API. The Guardian's original bearer was denied for that learner. The next prompt removed the earlier result and offered the still-linked sibling. The Student retained own-record access. |
| Cleanup | Removed the run-owned container, database and login. Restored both original PENTA services and verified HTTP 200. |

Validation window `96333ebe50cf46929cabb8b911e06c00`, SQL run
`1ad5282ca3bc47ae8344112b1e9ccbd5`, harness SHA-256
`016a2fbf122f68f81bbb9911ee5d47d3633d0224c4e1de5d7ff66c061a339d94`.

The previews cover bounded reads. Existing Manual forms, downloads and
physical-device acceptance continue separately. Next is the separately
scoped Platform Owner conversation. Qualified private model evaluation,
the 90% coverage measurement and the six critical production gates remain
open.
