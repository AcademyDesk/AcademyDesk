# C22 expense lookup and recovery acceptance — 8 November 2026

Integrated the seven-path Cloud return `3bcbacb` at `730e09a`. The import
preserved local protected-write, payroll, private-media and SQL work. The
issue record carries both Cloud and Desktop evidence. The 46 targeted
expense frontend tests passed locally. [Cloud C22 evidence](../HANDOFF/C22_EXPENSES_RECOVERY_2026-11-08.md)

Disposable SQL run `8355c7760ceb4509a4d0f645add491f0` passed **17/17** real HTTP
and SQL checks. FinanceUser, Academy Owner and Academy Admin could read
the register and the two-field branch options with Finance enabled and
MultiBranch unavailable. The options were ordered, tenant-scoped and excluded
private addresses. Empty results, foreign and missing branch IDs, anonymous
and Teacher access, and disabled Finance were exercised. A legacy USD entry
and its historical status survived the reads.

FinanceUser created INR 19.37 with a local branch and optional fields blank,
read it back, and edited the amount to INR 20.08 with no branch. Each accepted
write had one actor audit. Injecting a required-audit failure rolled both
actions back. Other captured data remained unchanged. The combined API suite
passed **2,196/2,196** with zero skips.

The exact-lock Next.js 16.4.3 export and postbuild passed. Edge verified all
658 artifact files and used the actual app at 390px and 1440px. The browser
and SQL run `4daaee1582f348a2a7cfbfe16f88bec9` passed six phases.

| Transport case | Verified recovery |
| --- | --- |
| Response lost after commit | A double submit produced one real POST and one INR 25.55 expense. The page retained the draft until a fresh read and explicit start of a new entry. The expense appeared exactly once. |
| Save acknowledged, refresh interrupted | The real 201 committed one INR 27.05 expense with its local branch. The page told the operator the expense was saved, then recovered through a read-only reload. |
| Request interrupted before forwarding | The unsent draft remained available for review. A fresh read found no expense, and the explicit new-entry action cleared the draft without replay. |

Each committed expense wrote one attributed audit. Captured SQL preserved the
existing USD record and unrelated rows. Edge saw 19 API responses with zero
rate limits or page errors. The guarded windows removed only their owned SQL
resources and restored both original PENTA services.

The remaining C21 real-host optional-feed cases, other roles and physical
devices, linked critical workflows, exact-release CI and images, and staging
restore and rollback remain open. Keep BUG-DATA-0016 and the five P0s open.
