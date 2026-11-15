# PENTA shared conversation checkpoint

15 November 2026 · Windows desktop · `codex/penta-host-integration-20261030`

Academy Owners and Admins can use the main `/penta` composer for student and
batch search, approved finance reads, and class scheduling. For example,
“Look up student Mira” returns bounded academy records, “Open the second
result” resolves the displayed match, and “Try Mira Alpha” runs a refined
search. An ambiguous reference asks which record to use. Lookup context
stays in the tab and expires after five minutes. Session changes clear it.

A class request opens the existing scheduling flow directly in the chat.
The user chooses the verified source and slot, reviews the exact plan, and
separately approves and saves. Reloading the chat restores the scoped draft
or receipt through GET. API readiness remains independent for each operation
family. The existing host decides role, tenant, finance module, lookup
scope, scheduling rules, approval, execution and audit.

## Validation

| Check | Result |
| --- | --- |
| Next.js 16.4.3 build, TypeScript and static postbuild | Passed. The build used 197 frontend input files and produced 682 static files. |
| API and SQL harness build | Passed. |
| Client prompt and scheduling verification | 16 tests passed. |
| Disposable SQL and browser | 7 browser groups and 95 native scheduling groups passed. SQL recorded the expected scoped lookup audits, one new class, one moved original and one execution audit per role. |
| Receipt recovery | Admin reloaded the shared composer and recovered the exact saved class. The dedicated Owner flow recovered a committed reschedule after its response was dropped. Each sent one execution POST. |
| Cleanup | The run-owned SQL database, login and container were removed. The two original PENTA containers recovered their health and the local API returned 200. |

The first browser rehearsal confirmed the lookup flow and refused finance
for a Core-only academy. The final disposable fixture enabled its Finance
module and retained the application authorization checks. The broader
finance browser suites have retained assertions for the renamed composer.
This run exercised its finance read alongside academic and scheduling work.

Validation window `e136b619ed0749faa716fe2f9b826bfb`, SQL run
`1e3e0e6cc4334b348ae33ad29e160586`, harness SHA-256
`f81075aeac723bae6c3a0c4db3387f8678164ec8015771ea06588c01db4a82eb`.

## Remaining acceptance

Prompt interpretation is a guided preview. The actual private-model run
and the 90% target remain unverified. The first Mini evaluation should
qualify learner lookup and follow-up on the existing bridge. Teacher
attendance, Student/Guardian and Platform conversations need their own
role-specific work. The six critical production records remain open.

[Mini evaluation handoff](../HANDOFF/PENTA_MINI_CONVERSATION_EVALUATION_2026-11-15.md)
