# E5-R25 — completed onboarding result after a full reload

19 October2026. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`,
HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Continue the existing program;
no new testing plan, backend rewrite, runtime activation or deployment.

## Scope and delivered behaviour

- [x] Reproduced the reload gap with the new synthetic terminal suite:31/31
  initial cases failed at the passive loader's rejection of a consistent
  `Succeeded` draft. The red receipt is retained, not counted as acceptance.
- [x] The account/current/draft/digest/current loader now recognizes a consistent
  completed draft. Every existing ownership, authority, catalogue, field, UTC
  expiry, digest and session fence remains. A status label alone is not a receipt.
- [x] Re-entry shows **Verify saved result**, with private work canvas and
  Student360 result withheld until an explicit check. Visiting/reloading still
  performs only the existing four GETs; no proof, approval, creation or inference.
- [x] Explicit verification reuses the exact existing host `core/operation` and
  `core/status` read commands, then fresh account/current verification. Those two
  existing read commands use POST transport and required read audits; they do
  not challenge, confirm, execute or change a draft/domain record.
- [x] A completed channel sets confirmation/execution latches before any request
  and rejects any regression to a non-completed operation. Receipt/scope/revision/
  hash/audit/source and full projection checks are unchanged. A failed or lost
  read can only be explicitly reconciled through the same bound read commands.
- [x] Verified success restores the same Student360 link and read-only shared
  work canvas. No portal accounts are created. Manual remains independent.
- [x] Existing prepared → exact approval → separate creation, lost committed
  response recovery, manipulated-proof denial and nonterminal review retained.

This is active-session re-entry only. No TTL extension, expired-session history,
durable recent-conversation list, conversational composer or qualified model is
delivered. The old loader's mismatched `Succeeded`/collecting negative case still
fails as intended; only a consistent completed snapshot is newly accepted.

## Validation

- [x]33 new terminal transport checks PASS, including no mount action, explicit
  same-operation result, no reapproval/re-execution, expired access, missing or
  manipulated receipt/audit/source/scope/hash/revision, status regression,
  malformed response, exact HTTP denials, token/logout/navigation fences,
  concurrent checks and lost saved-result read recovery. Synthetic, not SQL/model.
- [x] Retained44 operation /42 recovery /72 channel /48 review /38 start checks
  PASS. The48 include all12 immutable vendored source pins. These244 retained
  checks plus33 new checks total277; no additional unit/API count is inferred.
- [x]22 retained synthetic review-browser,22 read-chat-browser and56 role-entry
  browser cases PASS against the actual export. These use synthetic API bodies
  and are separate from real SQL/browser acceptance.
- [x] TypeScript, focused ESLint, webpack87-page export, isolated harness build
  (zero warnings/errors), and `git diff --check` PASS. Normal API DLL unchanged.
- [x] Run `e08bbf237d224c8caa2284bac498cebe`, exit0,224.1s:
  **451 retained SQL checks**,24 actual browser scenarios plus3 additive full
  document reload/re-entry journeys PASS.102app/7Identity migrations applied
  only to the owned disposable target; runtime inventory358 unchanged.
- [x] Operational browser now makes95 real host requests:27 light320,27 dark1440,
 27 lost-response390,14 manipulated-proof768. Three re-entries add24 read-only
  requests. Every successful reload uses4 GETs before the explicit gesture, then
  operation/status/account/current; same student link, no second challenge,
  confirmation or execution. No429/unexpected console errors. Mobile and desktop
  screenshots inspected, no horizontal overflow; not physical-device proof.
- [x] Independent parent SQL assertions: exactly3 added synthetic students,
 3 execution audits,3 Succeeded drafts, fourth manipulated-proof draft Prepared;
 73 exact actor/tenant audits. Required read-audit counts are28 conversation,
 7 draft,14 operation,14 status; challenge4/confirmation3/execution3 remain exact.
  Unrelated domain/Identity/academy/branch/session/turn state is unchanged.
- [x] Retained15 passive review and5 Start browser scenarios and their independent
  SQL postconditions PASS. No interpreter is configured for operational result
  verification; Start uses a scripted interpreter with zero interpretation calls.
  No actual model inference/qualification or physical-device testing this slice.
- [x] Mismatched cleanup marker refused; exact successful run database/login and
  container removed. No pruning, unrelated-container change or evidence deletion.

Reporting correction: R24's narrative said449 while listing422 retained+29 Start.
That arithmetic is **451**, not449. No new direct SQL cases are claimed in R25.
The run's named groups are21 atomic,7 domain-service,32 draft,26 execution,
32 HTTP,21 context,31 preparation,48 lifecycle,39 approval,38 partial,
43 conversation,46 turn,23 gateway,10 abandonment,5 private-bridge,29 Start.
Historical reports/evidence are preserved; this corrects the summary count only.
R24's186 focused .NET tests remain prior evidence, **not rerun**: no API source
changed here. Default Turbopack junction issue remains open; webpack is the
verified build path, not a claim that Turbopack was repaired.

Commands used: `node QA/tools/penta-onboarding-terminal.test.cjs` and the retained
named client/browser scripts; `npx tsc --noEmit`; focused `npx eslint` on the four
touched frontend files; `npm run build -- --webpack`; isolated
`dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql -v quiet`;
`QA_ONBOARDING_REVIEW_BROWSER=1` with
`pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.
Temporary QA environment switches were restored/removed; no config file changed.

## Source and evidence pins

SHA256 of the final tested files; all evidence paths below are local/untracked.

| Path | SHA256 |
| --- | --- |
| `apps/web/src/lib/penta-onboarding-review.ts` | `2544052C7B9882E612F8EA6C7C145594DB69E4E4128BE53FD7BAAAB54D3A5BE0` |
| `apps/web/src/lib/penta-onboarding-operation.ts` | `1306FBECC7EF5FED8C9B560FF2F529054C8F74C97ED576BA6933B3879A869587` |
| `apps/web/src/components/penta/onboarding-operation-workspace.tsx` | `C712285A12BD84CBC6A59070BF9FC82331F9662A4AAA4EC1F8B73DE6B047CB8B` |
| `apps/web/src/components/penta/onboarding-review-workspace.tsx` | `98C0FB63031C80974EAD6966682E127BBB97EC1849C978A1737DB3F35C7B3C30` |
| `QA/tools/penta-onboarding-terminal.test.cjs` | `7DF5B3E85B31560A2B94E1875D43D64D1AE738A698B80F2FCBA4600D980AA5B2` |
| `QA/tools/penta-onboarding-operation-live.cjs` | `4237E31916C3B6F6621472BB4EE377B688A27DE3B6E4EAC9F97B0AD6BAB181A3` |
| `QA/tools/SqlHarness/PentaOnboardingOperationBrowserRegression.cs` | `CFE680AB53BBF3824731727DBEC8F700C642D01C645191FA18754E2936C9E230` |
| `QA/EVIDENCE/penta-onboarding-terminal-e5r25/final-contracts.json` | `7FC1EB5017A5471E365035B059915B204295FD53D90312EEF1D7F8FBF8153A13` |
| `QA/EVIDENCE/penta-onboarding-operation-live-e08bbf237d224c8caa2284bac498cebe/result.json` | `1B79BF8F67F401ECC5EBBFADCB65C0EC8AD02A7E595FA8CE7D0CFA76B9734E03` |
| same operational directory, `sql-postconditions.json` | `4E2DA19A56099A413FA2FF1026D242F665424B289125F7EC6C152125E2E34E35` |
| `QA/EVIDENCE/penta-onboarding-review-live-e08bbf237d224c8caa2284bac498cebe/result.json` | `596123016ED062FF949400E17810BE1A00DD9C2899D7A03D3FF4AB17A7B8D67F` |
| same passive directory, `sql-postconditions.json` | `1C4D31A5E57DD5A5C9F28650141A87B693A339899C4BEFF0803DA2EAEBB02946` |
| `QA/EVIDENCE/penta-onboarding-start-live-e08bbf237d224c8caa2284bac498cebe/result.json` | `6AA1C284DD2D7BC62F37D560F16B07045D109895EB7E181E2635858DE49F1BA2` |
| same Start directory, `sql-postconditions.json` | `8790486F28C7F34616BEC04A0C3BC49D8DCB6A56520547B0561EEC1C46D28E44` |

Other retained browser receipts: `penta-onboarding-review-browser-1792375737856`
(22), `penta-chat-contract-1792375783346`(22), `penta-entry-1792375772971`(56),
all under `QA/EVIDENCE`, with complete result JSON. Initial red and31-case green
receipts are retained beside the final33-case terminal receipt, not extra counts.

Isolated harness DLL SHA256
`38EEBA1F39397832F1B7D0C611F88D11C62B2C08F9D231D1E846D8CF3F9E3AEE`;
its unchanged API DLL
`AC396B3FCF4B97D26344FD3EAA6AD65BDA2AD1ED921E7D9C6EAE3BB7C4EC9CE7`.
Normal developer API DLL remains
`049BBA230504354EE8245F32A1FFAA01DC0218429B1F30D7F53BC2F21DEAA504`.

## Boundaries and next route

No API/domain/schema/default flag/interpreter/engine/vendor/credential/developer
database change. Normal web3000 onboarding200/API5092 health200/anonymous401;
actual developer Admin sign-in remains unverified. Existing unrelated dirty work
preserved, staging empty, HEAD/main/PENTA HEAD unchanged, evidence local and
`.build-check` ignored. No commit, push, merge, Azure or production-resource use.

- [ ] **Sol High:** mounted conversational send/clarification/current Projection
  with genuine qualified-runtime readiness; current Start readiness is only
  interpreter-connection availability, not permission to enable the composer.
- [ ] Real completed-pending-turn/browser pairing, durable/expired-session receipt
  re-entry and current-state reload/history remain distinct unfinished work.
- [ ] Existing independent Mini CORE-01 semantic/latency qualification is BLOCKED;
 4B CPU latency exceeds the unchanged30s host deadline. No new inference,
  training, resource approval, tier escalation or qualification claim.
- [ ] Other operation families/roles, complete Manual parity, physical devices,
  privacy/security/provider/enterprise/exact-release gates remain OPEN.
  BUG-DATA-0001 stays OPEN; full application is **NOT production-ready**.

Continue in Academy Desk for host adapters; use the established Mini handoff if
a genuine Core contract or model change is needed. No new shared contract or
project switch is required by this terminal-result fix. Sol Medium is suitable
only for settled bounded visual work, not runtime/authorization/approval decisions.
