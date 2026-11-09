# PENTA enrollment status changes and transfers — 9 November 2026

Base `7341fbba0c656805b2ce7b765643f9e721c67465`. The existing Testing-only
draft, approval, execution and receipt routes accept two additional typed
operations under `Penta:EnrollmentLifecycleEnabled`. The existing PENTA and
enrollment creation flags are also required. The protected store and schema
are unchanged.

| Operation | Checked preview and effect |
| --- | --- |
| `AcademyDesk.Enrollment.Update` | Shows the current enrollment, target status, end date and lifecycle reason. `ApproveEnrollmentUpdate` authorizes precisely that change. |
| `AcademyDesk.Enrollment.Transfer` | Shows the source and destination batch and exact date. `ApproveEnrollmentTransfer` authorizes a transfer that closes the source and creates its replacement in one transaction. |

The public draft digest binds the operation and reviewed fields. The host
compares the separately protected fingerprint with the current source record
under the existing admission lock, then reuses the same domain checks as
Manual. An intervening edit, including a change to the stored reason, makes the old
approval stale. A different operation cannot reuse the same request key or
approval. Transfer replay returns the original receipt. Required audit failure
rolls back both transfer rows and preserves the authorized draft.

## Verified on disposable SQL

Final window `3fc803cebf9d42eda72c373c5f8099fc`, SQL run
`d4da2bbf20b5412bb13282fe6f16255e`. The API and SQL harness built with zero
warnings or errors. All 16 lifecycle checks passed through authenticated HTTP
with real Identity and SQL. The suite verified separate flag gating, exact
status and transfer fields, cross-operation approval refusal, transfer audit
rollback, both committed transfer rows and replay without another write. A
Manual edit that changed only the stored reason invalidated an old transfer
approval even though the displayed status stayed the same.

The first lifecycle run passed before the digest privacy change. The final
run retained that behavior while keeping the private source fingerprint out
of the public digest. The 23 creation, 13 preview and 57 shared enrollment
checks also passed, including all 15 mixed admission races. Both original
PENTA services returned healthy after owned SQL cleanup, and Docker inventory
was preserved.

This is a Testing-only host result. Browser conversation, model qualification
and production acceptance remain open. Next is make-up scheduling, where the
approval needs to cover the resolved location and queued InApp notifications.
