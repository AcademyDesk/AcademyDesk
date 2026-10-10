# Read-purpose / minor-data / prompt boundary — 2026-10-10

Repository `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting commit
`8d0acc0cea6449d3d8b2012f3da28ec3d862038f`. Route: Sol High / Codex.
**Current bounded High packet COMPLETE; next route Sol Medium.**
This does not complete all security, compliance, PENTA or enterprise testing.

## Scope and implementation decision

Inspect and verify the existing first-read boundary rather than introduce duplicate
authorization, regex intent policing, a pretend consent checkbox or a new model DTO.
Only QA fixture/evidence mapping and continuity documentation changed. Production
application/DTO/provider/finance/guardian behavior and configuration are unchanged.

Existing controls retained:

- `PentaPilotPolicy` permits only configured Development/Testing Owner/AcademyAdmin,
  active identity/academy and current authority; Finance permission is independently
  checked. This remains an operational fee read, not general student profiling.
- `OutstandingFeesService` projects explicit operational fields: source ID, display
  name, subjects, balances, source path and record code. It does not project DOB,
  medical/admin notes, contact/address/emergency fields or guardian records/rights.
- Planner DTO includes only conversation ID, current user message, scoped IDs/filters
  and the two-tool allowlist. Source result rows and profiles are not sent back to
  the planner. Identifiers and user-supplied names/filters are still personal data,
  not anonymized information.
- Unmapped processing/age/consent claims cannot be added to the strict request DTO.
  Unknown medical/profiling/consent tools cannot execute. Model prose/state/approval
  is not host fact, authority or an executed operation.
- Existing SQL state and receipts are protected; raw user prompts/model prose are
  not persisted in those session/turn/audit records. This is not inference-service
  logging or training-retention proof.

## New executable evidence — H2

`QA/tools/SqlHarness/Program.PentaReadPrivacy.cs` adds an isolated fake planner and
fresh synthetic same-academy known-minor/unknown-DOB records, foreign record,
private medical/contact/guardian markers, revoked guardian rights and reconciled
fee fixtures. Capture actual host-to-planner requests without printing their values.
The existing host-security runner invokes these cases **after** its original 40
assertions; no original assertion, quota, expectation or runner is weakened.

| New assertions | Verified result |
| --- | --- |
| 1–3 | Both synthetic own-academy records retain exact400 balances; intentional name ambiguity retained; explicit six-field projection excludes private/guardian/foreign details and model prose; no age/profile inference fields |
| 4–6 | Exact existing four-field planner envelope/two-tool allowlist, minimal displayed-student detail; follow-up receives scoped IDs/filters, not financial result rows or source profiles |
| 7–10 | Client `purpose`, `parentalConsent`, `adultVerified`, `privacyApproved` overposting returns400 before planner dispatch; claims confer no authority |
| 11–13 | Medical-note, child-profiling and parental-consent tool proposals return safe ERROR without results/execution |
| 14–16 | Untrusted prompt/model prose is not echoed as host output or plaintext persisted state/turn/audit; guardian grants, unknown DOB, consent count and source ledger remain unchanged |

Fresh build: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path
.build-check/penta-mini-sql --verbosity minimal`: **0 warnings /0 errors**.

Fresh execution: existing `Run-ReconciledPayment.ps1 -Module PentaFoundation` with
`QA_PENTA_HOST_SECURITY=1`; real Mini/browser flags unset in this process only.
**16/16 H2 + unchanged40/40 H1 + existing foundation/5-of-5 conversation races PASS**.
Race statuses201/409, no429. Run `9581fa7b05e34a8bbd68be93655567bc`, loopback SQL
port55522, elapsed33.4s,88 application+7 Identity migrations. Mismatched cleanup
marker refused; exact owned database/login/container removed. Two unrelated Mini
containers retained. Receipt `QA/EVIDENCE/penta-read-privacy-boundary.log` stays local.

Prior116 targeted backend tests, including24 key-ring tests, are retained from the
preceding checkpoint, **not rerun**: no application source changed in this slice.
No fresh frontend/export/device/full-suite/real-inference/Azure test. Actual tests
use the preserved working tree; unrelated Mini dirty work is not accepted/published
by this commit. Legacy Phase1 consistency receipt remains absent here, not fabricated.

## Important limits / deferred High gates

This proves host minimization and independent enforcement on synthetic data, not
legal consent or genuine model understanding. Unknown DOB is **not** treated as an
approved adult: it stays unknown and unchanged. These tests do not implement age
verification, parental/guardian approval, withdrawal propagation or a reviewed
processing-purpose registry. Do not infer child-data/customer-pilot authorization.

The actual user prompt is still sent to the configured private planner. Users may
paste personal information in it. Inference egress/logging/no-training/retention,
lawful reviewed processing facts and consent/guardian UX remain customer-data gates.
A keyword filter would not prove those controls and was not introduced here.
Fake hostile proposals are not actual-model prompt-injection/semantic acceptance.
Saved Mini86/94 remains NOT ACCEPTED; no unchanged benchmark rerun or engine edit.

Key-ring setup remains disabled; provisioning/migration/ACL/expiry/restore/replica
acceptance is pending. Privileged MFA, trusted ingress/effective limits, remaining
P0/critical P1/device/runtime/restore/legal/operations requirements remain open.
No issue, customer-data approval, production gate, main merge or Azure deployment.

## Clear next handoff — Sol Medium in Academy Desk

Resume the queue from [Submission Review](PHASE_2B_SUBMISSION_REVIEW_FEEDBACK_CHECK.md)
and `BUG-FUNC-0003`, not a new audit or replay of completed repairs:

1. Reconcile the original success-feedback route list with existing accepted reports.
2. Classify each route as repaired, genuinely uncovered, or awaiting linked/device
   acceptance. Do not label synthetic browser passes as live/physical acceptance.
3. Select the next genuinely uncovered bounded form; preserve domain payload,
   permissions, nullable optional fields and already-accepted behavior.
4. Implement only its feedback/draft/pending/accessibility/UI gap and run affected
   controlled/browser/typecheck/lint validation. Update the existing issue/handoff.
5. Return to Sol High only if a new task needs authority, persistence/transactions,
   privacy policy, engine qualification or consequential deployment decisions.

Stay in Academy Desk. A future genuine shared-engine dependency uses the existing
Mini handoff and explicit SWITCH PROJECT CHECKPOINT; do not switch silently, edit
Mini here or acquire a new model. Remaining High release gates do **not** block
independent manual/UI work and must not be forgotten before activation/deployment.
