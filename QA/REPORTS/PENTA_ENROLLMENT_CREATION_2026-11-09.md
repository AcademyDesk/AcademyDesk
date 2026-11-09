# PENTA enrollment creation with explicit approval and receipt

Base `e460dbc86d5e02c4cdf7ede6c330ab74f089c65d`. The Testing-only workflow
requires `Penta:Enabled` and `Penta:EnrollmentCreationEnabled`. It binds a
typed enrollment request to the authenticated academy, actor, conversation
version and retry key. The host reuses the existing encrypted draft store
with a separate protection purpose. No schema migration was added.

1. Prepare checks current enrollment rules and fixes the precise learner,
   batch, start date and status in the protected draft.
2. Approve requires the exact revision, payload digest and explicit creation
   intent. It issues a host approval ID and records the decision without
   writing an enrollment.
3. Execute requires that approval ID. The host rechecks the authenticated
   actor, session and admission rules. It commits the enrollment, protected
   receipt and required audit in one SQL transaction.
4. Recovery reads the committed receipt for the same authorized actor and
   conversation. Replaying the same execution cannot create another
   enrollment. A completed receipt can be read after the conversation moves
   to a newer version while the session is still valid.

The routes are under
`/api/academies/{academyId}/penta/enrollments/conversations/{conversationId}/drafts/{requestId}`.
They accept only the expected fields and are available to the current Academy
Owner or Academy Admin. Student, Teacher and foreign scopes remain outside
this operation. The existing Manual path continues to use the same rules.

## Verified on disposable SQL

Window `8bca3245a62245f29db7d344d7e9df9a`, SQL run
`f0c1b15a75d842ec93c51ff234c45666`. The API and SQL harness built without
errors or warnings. The test used real Identity bearer logins and owned
conversation fixtures. All 23 new HTTP and SQL checks passed.

| Scenario | Observed result |
| --- | --- |
| Authority and input | Default-disabled, anonymous, Teacher, other actor, and foreign academy requests were rejected. A changed payload, wrong digest, invented approval and conversational `yes` could not authorize creation. |
| Draft and confirmation | Preparing and approving wrote their own audits without writing an enrollment. Identical requests returned the same draft or approval. |
| Commit and retry | Two concurrent execute requests returned 201 and 200 with the same receipt. SQL contained one enrollment and one execution audit. An injected audit failure rolled back the enrollment and left the authorized draft recoverable. |
| Current state and recovery | Changed capacity, changed conversation version and expiry blocked new execution. The completed receipt remained readable after the conversation advanced. |

The 13 existing preview checks and 57 shared enrollment checks passed,
including 15 mixed Manual/service admission races. The owned database, login
and container were removed. The original PENTA services were restored with
health 200 and unchanged Docker inventory. Raw receipts remain local.

Enrollment status changes and transfers still need their own protected
workflows. The private Mini qualification packet, conversational UI, full
release CI and production acceptance remain open. This run used no model
inference or browser.
