# C13 portal metadata and C14 fee reminders — 7 November 2026

Desktop integrated C13 `508c9bb` and C14 `19f235c` at `8f0a8c7`. All 11
declared paths match the Cloud return except for a trailing blank line
removed from its browser test script. Application and API test trees
match Cloud's 2,058-test run. Its six controlled C14 Chromium cases were
reused. Cloud's full frontend result remains 2,016 passes and three
missing communication SQL receipt failures.
[Portal handoff](../HANDOFF/C13_PORTAL_METADATA_2026-11-07.md)
[Reminder handoff](../HANDOFF/C14_FEE_REMINDERS_2026-11-07.md)

Owned SQL run `744641b7f2cc4af78196797bce8b1f1b` passed all 16 native
HTTP and SQL checks. The build had zero warnings or errors.

| Area | Observed result |
| --- | --- |
| Invoice and reminders | The actual projection returned INR 200 after adjustments and reconciled/Completed payments, and USD 50 separately. Bulk queued only those two rows for their distinct recipients. Canonical and mixed-case cancellations, a foreign invoice ID and ineligible bulk rows produced no reminders. An explicitly targeted future invoice retained its existing behavior. |
| Authority and audit | Anonymous, Teacher, Student and foreign-tenant writes were denied without changes. Accepted zero-queue calls retained one actor audit. Injecting a failure in that mandatory audit rolled back the queued notification and summary audit. Ledger rows remained unchanged. |
| Portal metadata | Four authenticated role/tenant reads preserved valid audience restrictions and excluded malformed, invalid-start, future and expired records. Stored notifications and read receipts remained unchanged. |
| Compensation metadata | The non-object legacy root returned the empty summary. A valid legacy monthly zero and its effective date survived real HTTP readback. Both stored JSON values remained byte-for-byte unchanged. |

The first run assumed that an accepted request with zero queued reminders
also had zero audits. It stopped on the actor audit. The final test asserts
that audit and checks every other captured row unchanged. Valid announcement
fixtures were also corrected to include their required expiry. No additional
application change was needed. Both test attempts remain in local evidence.

The validation used cached images and synthetic data in one owned SQL database
per attempt. The completed run verified 85 application and seven Identity
migrations, removed its owned database, login and container, and restored the
original Docker inventory and both PENTA services. No provider delivery was
performed. Cloud C15 owns teacher availability. The five P0 records, remaining
critical and device checks, current CI/images, and staging recovery stay open.
[Release gates](../09_RELEASE_GATES.md)
