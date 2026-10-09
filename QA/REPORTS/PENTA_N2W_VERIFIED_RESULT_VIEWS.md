# PENTA N2w — verified Cards/Table presentation

Date: 2026-10-09. Repository: `D:\AcademyDesk-codex-p0`.
Branch: `codex/penta-search`; starting HEAD `de7c8a3faf7c57ecd59cda4086c2f024d17ba98c`.
Scope: existing first-read UI only, plus acknowledgement of Mini's return.

## Delivered

- `PentaResultView`: local reusable presentation with typed column labels,
  stable keyed React cells and host-supplied links/actions. No Academy domain,
  route, provider or API dependency; no second planner or published UI package.
- Cards default per answer; a named Cards/Table group reflects selected state.
  Switching preserves server ordering, source keys/links and unsent prompt,
  without another request, sort, calculation or speculative result.
- Semantic caption, column/row headers and a named focusable scroll region;
  horizontal table scrolling stays inside the conversation at phone widths.
  Scoped PENTA tokens, both themes and 44px result controls remain consistent.
- Student codes/references distinguish duplicate display names. Latest
  clarification choices only prepare the ordinal prompt and focus the composer;
  they require Send and existing host/model validation, not automatic selection.
- Host balances retain their existing decimal display units. Every currency
  stays separate; no exchange rate, total, minor-unit conversion or billing.
- Plain text stays escaped. No HTML/markdown execution or model-created URL.
  Empty/error answers have no view switch; capped results show only supplied rows.
- Partial-list wording uses the number of displayed rows rather than an assumed
  number; current source contract still caps at ten and is unchanged.

## Mini return and dependency

Read-only review of Mini's local return packet, frontend synchronization contract,
usage-interface draft and candidate decision: **CONTRACT READY / ENGINE BLOCKED**.
Mini HEAD `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe` is not an artifact manifest
for its dirty/local documents. Active `/v1/chat`/Tool Protocol 0.1 unchanged;
conversation/Core/usage drafts are specifications, not enabled endpoints.

Saved N2r remains 86/94 with eight semantic failures; no engine fix or fresh
inference qualification was returned. Proposed pinned Qwen3.5-4B Q4_K_M trial
needs separate acquisition/runtime/training approval and resource qualification.
Reported free RAM was below its proposed start guard. No download, activation,
Mini edit, serving restart, training, external inference or Azure provision here.

## Local validation

| Check | Result |
| --- | --- |
| `npm exec tsc -- --noEmit` | PASS |
| Targeted ESLint: chat + result-view components | PASS, no warnings/errors |
| `npm run build -- --webpack` | PASS, 83 exported routes |
| Existing response contract runner | 57/57 PASS, unchanged |
| Existing exported-browser recovery runner | 16/16 PASS, unchanged |
| Expanded design browser: 320/768/1440 × light/dark | 6/6 combinations PASS |

Each design combination exercises result, empty, error, three-row clarification
and capped ten-of-fourteen fixtures. Assertions cover exact card/table currency
parity, duplicate codes, stable ordinal/links, five column and three row headers,
escaped markup-like names, missing-code/empty-course/clear-balance display,
choice-only prompt preparation/focus, no view-triggered API call, resetting view
for new answers, source pagination, keyboard horizontal scroll where needed,
44px result controls, manual mode and all five help overlays/Escape return.
Five synthetic turn requests per combination, no automatic retries. Primary Send
contrast remains 5.38 light / 8.38 dark; no unexpected rendering/console errors.
This is not a full WCAG or screen-reader certification.

Final design evidence: `QA/EVIDENCE/penta-design-1791540595305/` (36 screenshots
and result JSON). Initial successful view run retained at
`QA/EVIDENCE/penta-design-1791540482329/`; review caught a narrow column-header
wrap, fixed before final export/run. Final recovery evidence:
`QA/EVIDENCE/penta-chat-contract-1791540614875/` (16/16, result JSON).
Browser/server processes own only disposable
loopback fixture resources and close at runner completion; no SQL container.

Screenshots were inspected at desktop light and mobile dark. Mobile table can
show a subset of columns by design; local scroll and Cards remain available.
No whole-page horizontal overflow. No physical Android/iOS test was run.

## Publication and truth boundary

Checks ran on the preserved local dirty snapshot, including earlier unpublished
N2o/p/provenance repairs. Selective N2w staging must include only the new local
component/styles, result-view integration, expanded design runner and these
documentation changes. It must not silently publish earlier unrelated work,
untracked response/recovery helpers, evidence or Mini files. Passing local
recovery checks are not proof those earlier fixes exist in this feature commit.

API/domain/authorization/tenant/tool/planner/SQL schema and test expectations
remain unchanged. No full API/SQL/live-Mini/15-turn linked/browser-device suite
rerun. Historical evidence and the original enterprise queue remain intact.
No issue, N1/N2, memory/privacy/history, 90% outcome or release gate is closed.
No READY TO VIEW or production-readiness claim; no main merge or Azure deploy.

NEXT: Sol Medium for settled bounded UI refinement. Sol High for consequential
host integration only after Mini returns a qualified changed engine; then the
existing real SQL/security/source/15-turn browser and customer viewing gates.
Do not repeat accepted audits or turn a contract-ready return into engine-ready.
