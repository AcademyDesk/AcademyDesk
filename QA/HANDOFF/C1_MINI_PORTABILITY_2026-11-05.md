# C1 — Mini/Core portability return — 5 November 2026 IST

Owner/environment: Cloud Codex, managed cloud. Status: IMPLEMENTED / bounded portability VALIDATED; full Mini gate BLOCKED on genuine missing evidence. Not model, host or production qualification.

## Source, ownership and inherited work

Academy checkpoint `8d7a02c87d867d9f1609aa524d3f3a04f0d7a934` was fetched and independently verified on `codex/penta-host-integration-20261030`; the checkout was clean before creating this task branch. Paired Mini checkpoint `c3c61b9ef3f239035c8b8f46649c66997e98f2b6` was also verified remotely and checked out clean. Both task branches are `codex/cloud-c1-mini-portability-20261105`.

Read CLOUD_CODEX_START.md and CLOUD_LOCAL_WORK_PLAN.md before governance and source. Prior completed branches remain intact. Source inventory, existing domain work and pending protected-host checks are inherited; none were restarted or reapplied wholesale. Local L1 retains all protected-host API/SQL/migrations. Academy changes only this packet report; shared handoffs/trackers remain with the designated integrator. C2 mapping remained read-only and is not claimed complete.

Initial path claims were published before portability mutations; the mocked deadline test's added claim was published before its edit. Private exact paths and original-evidence categories are in `operations/CLOUD_C1_PORTABILITY_2026-11-05.md`. Neither runtime/Core implementation nor host source was changed.

## Actual C1 changes

1. Reused existing published fix `f2adfbaaadca43dbdf982458942e3d21c067b674` as `5db6dbf`, rather than reimplementing it or importing later runtime commits. Restores byte-exact line endings for 29 pinned JSON files and protects them with Git attributes. All JSON values are unchanged: 27 files match original transfer-manifest raw/portable hashes; the other two match unchanged tier-lock raw hashes. No expected hash or qualification was rewritten.
2. Existing offline Windows checks use Windows path semantics and explicit mocked Windows ABI/process primitives on Linux. This is unit-level policy/layout proof, not actual Windows kernel job-object or GPU/runtime acceptance. The original safety assertions remain.
3. New commit `307847c` imports the standard-library Windows type helper explicitly. Standalone test collection previously failed due to import-order dependence; the same command now collects all seven original tests.
4. New commit `9934a12b322cf630425a8b8a1b96ca18454b43d0` makes the late-transport regression wait for actual mocked dispatch and then reschedule its real timeout contexts. The first post-restoration full run retained a startup scheduling failure at the former 80 ms test clock. The fix keeps the production timeout/profile unchanged and retains timeout, no-second-completion, no-retry/busy, lease-ownership and cleanup assertions. It also checks the two timeout contexts share the same absolute deadline. No production provider/deadline code changed.

## Actual checks and outcomes

| Check | Source / actual result |
| --- | --- |
| Installed prerequisites | Existing Python 3.12.14/dev dependencies reused; pip check passed; no new package or model downloads |
| Complete baseline | Mini c3c61b9: 26 failed / 1,651 passed / 60 errors / one inherited skip; five subtests passed; exit 1 |
| First post-restoration full run | 307847c: 14 failed / 1,663 passed / 60 errors / one inherited skip; one retained timing-sensitive mocked deadline failure investigated, not hidden |
| Standalone Windows-test collection | Before: collection error; after: seven collected, exit 0 |
| Complete mocked intent-provider file | 66 passed, zero skips, exit 0 |
| Complete final suite | 9934a12: 13 failed / 1,664 passed / 60 errors / one inherited skip; five subtests passed; exit 1 |
| Inventory comparison | Same test IDs; all 13 original portability failures now pass; no previously passing case fails in final run; no new skips |
| Fresh local Git checkout, core.autocrlf=true | Final source: all 29 pinned JSON files byte-identical; 79 relevant regression cases pass; checkout clean; exit 0 |
| Scope checks | Runtime src/, operational tools, original transfer manifest, tier-selection lock and workflows unchanged; git diff --check passed |

These overlapping counts are not additive. Raw logs/XML and SHA256.json remain outside repositories in `/workspace/c1-mini-portability-20261105`; only sanitized results are published. For example, final.xml SHA256 is `e64ef6edcdd33f8132a02f167860e3cbb587218fc1497579ab2428876829fb6d`. Exact current GitHub run/job results are still unobserved because the cloud API policy is unapplied; local verification is not a CI claim. The enclosing final report commit adds documentation only beyond the tested source.

## Remaining blockers and exact local return

All 73 remaining failed/error cases report missing original files, not a newly discovered Core logic failure. They require genuine excluded laptop review/parent/source records and archived runtime-source evidence. One inherited skip requires a pinned offline native proof binary; it was not added or relabelled. Private details stay in the Mini packet. No fake D: tree, fabricated receipt, expectation weakening or skipped failing case was introduced.

Local evening tasks:

1. Fetch both C1 branches, verify their actual remote tips, inspect the diff and review the inherited restoration plus two test corrections. Source integration is a designated-integrator task; no automatic main merge or wholesale preservation merge.
2. Recover original excluded private inputs and verify existing hashes/provenance. Run the exact full Mini suite with those genuine prerequisites and return source SHA, commands, counts and private artifact hashes. New evidence cannot impersonate historical originals. An authorized portability adapter is a later packet if original paths need mapping.
3. Retain the existing paired-source pin until L1 explicitly reviews any update. The production Core files and the bridge's contract fixture are byte-identical to c3c61b9; no host source or CI pin was edited. C1 does not qualify or replace L1's pinned source.
4. Compile the latest owned protected-host bridge and complete the existing `PentaHostMigration`, `PentaFoundation`, `PentaOnboardingProtected` and `PentaTeacherProtected` SQL modules with unchanged resource guards, audit/tenant/approval/race checks, owned cleanup and exact original-service restoration. Earlier foundation/migration evidence remains source-scoped. Cloud did not execute those local-owned checks.
5. Observe exact source-bound GitHub CI outcomes through authorized access. No deployment workflow is dispatched by this packet. The full Mini gate remains failing until its real prerequisites and all tests pass.

Next cloud task: C2 remaining-operation mapping, read-only, after re-reading current remote claims. Do not edit L1 API/SQL/migrations. C3 frontend or C4 runtime work requires an explicit bounded path claim and available authorized prerequisites.

Publication: this report is on the named feature branch; use its enclosing Git commit and independently read remote tip for the final publication SHA. The final reply records verified tips. No shared AI_HANDOFF overwrite, main merge, Azure/customer database access, model acquisition/loading/inference/training/activation, new paid compute or environment publication. Mini/Plus/Pro remain unqualified, Ultra deferred, and all five P0/enterprise/production gates remain open. Next route: Codex / Sol High for L1 acceptance and any evidence-portability decision.
