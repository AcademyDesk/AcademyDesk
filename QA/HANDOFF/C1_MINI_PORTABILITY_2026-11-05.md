# C1 — Mini/Core portability — 5 November 2026 IST

Owner/environment: Cloud Codex, managed cloud. Status: CLAIMED; baseline reproduction in progress. Local L1 retains protected-host API/SQL/migration validation.

Verified Academy base: 8d7a02c87d867d9f1609aa524d3f3a04f0d7a934 on codex/penta-host-integration-20261030; checkout clean at task start. Verified paired Mini base: c3c61b9ef3f239035c8b8f46649c66997e98f2b6 on codex/mini-local-source-20261029. Cloud task branches in both repositories: codex/cloud-c1-mini-portability-20261105.

## Scope and exact ownership

Academy may change only this packet report. No API, SQL, migration, frontend, workflow or shared AI_HANDOFF/tracker edits; the designated integrator retains shared status ownership. C2 is read-only and makes no operation-mapping changes in this packet.

Private C1 claims are listed in operations/CLOUD_C1_PORTABILITY_2026-11-05.md in PentaMini. They cover only the existing byte-preservation and offline Windows-test portability fix plus its packet report. No runtime/provider/qualification changes or activation.

An existing published Mini commit f2adfbaaadca43dbdf982458942e3d21c067b674 contains the relevant portability fix. Review/reuse it selectively rather than reimplementing completed work or importing later runtime commits. Baseline and complete post-fix pytest results, fixture/pin checks, preserved failures and exact evening return tasks will be recorded here after execution. Installed dependencies are reused; no new packages or downloads.

## Restrictions and next owner

No model acquisition/loading/inference/training, main merge, Azure/customer database access or environment publication. Mini semantic, host integration and production gates remain open. Existing local evidence is absent, not fabricated. No CI green is inferred from local tests.

Next owner: Cloud Codex / Sol High completes bounded C1 verification; local Codex retains L1 and receives source-bound return tests. Publication SHA is obtained from this report's enclosing commit; final publication verification will follow.

## C1 claim extension: deterministic cancellation regression

The first post-restoration full run exposed a timing-sensitive pure mocked Core check: an 80 ms test deadline can expire before the transport enters its held phase, making the late-transport ownership assertion test a different scenario. Claim `tests/test_intent_provider_e5r59.py` in the private repository to coordinate transport entry and explicitly trigger its actual timeout contexts. Preserve the exact production timeout/profile, timeout/no-retry/busy/ownership/drain assertions and all runtime source. This report and the existing private report are the only public/private documentation edits; L1 remains untouched. Publish this extension before editing the added test file.
