# PENTA make-up scheduling and InApp approval — 10 November 2026

Base `26fb7d2b9e8338fae0ede2ec99c8f0adb9ea752c`. The Testing-only host
workflow requires `Penta:Enabled` and `Penta:MakeupSchedulingEnabled`. It
shares scheduling rules with Manual and uses a distinct protection purpose
in the existing draft store.

The preview resolves the learner, batch, teacher, time and delivery mode.
Offline uses the optional venue. Online and Hybrid require a meeting link.
The host also previews the exact InApp message and eligible Student and
Guardian recipients. The pilot checks future UTC times, HTTPS meeting links
and a bounded recipient group. It does not contact a meeting provider.

Approval binds the reviewed schedule and recipients. The host resolves them
again under the same transaction before execution and stops if they changed.
The class, queued notices and execution audit commit with the protected
receipt. Replaying the approved request returns that receipt. Manual creation
also uses one transaction for the schedule, queue and required audit.

## Verified on disposable SQL

The API and corrected harness built with zero warnings or errors. Window
`5ad6daf88f5643c3b719d98eaf31958b`, SQL run
`62187253c0d541f5bcfb68f7ab6daebd`, passed all 50 retained Manual cases
and 31 new authenticated HTTP and SQL checks. The initial window
`fc3dbc2f91bc43ad811d081d89904c74` passed the Manual matrix and the first 11
PENTA checks, then stopped on a duplicate synthetic batch name. The retest
fixed fixture names, verified the unchanged API source and reused its
hash-checked build.

Offline, Online and Hybrid all produced one HTTP 201 and one HTTP 200 from
simultaneous execution requests, with one class, two queued InApp notices
and the same receipt in both responses. Receipt recovery made no extra
writes. The suite verified tenant and role refusal, malformed requests,
duplicate slot refusal, approval tampering and no early scheduling or
notices. Replacing the inherited meeting link or revoking Guardian access
invalidated approval. Required audit failure rolled back Manual and PENTA
scheduling and its queued notices.

The guarded windows admitted work at 4.72 and 3.25 GiB available RAM,
respectively, against the unchanged 3 GiB threshold. Each removed its own
SQL container and restored the original PENTA services. Docker inventory
checks passed and the restored API returned HTTP 200.

These results cover local host HTTP and SQL. Conversation UI, model
qualification, physical devices and production acceptance remain open.
Next is regular scheduling and assigned-Teacher attendance.
