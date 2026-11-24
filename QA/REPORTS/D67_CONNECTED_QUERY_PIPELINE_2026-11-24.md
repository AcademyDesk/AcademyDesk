# D67 — connected typed query pipeline and routing correction

24 November 2026. Continues D66 and the existing enterprise program.
Academy base `3540c978cca3bfe7331c2b3446d7421831e1f7e0`; Mini base
`d9c3d7550194685d4e3572befbc092d25541ff04`. Current feature branches retained;
main/Azure/production data/model artifacts and unrelated untracked drafts untouched.

Published Mini query source: `30b88db356d3e22653dca706ffb4212c7bf37cb6`
on `codex/academy-conversation-core-20261123`. Academy paired CI now pins that
reviewed source; legacy bridge originals are unchanged. Ordinary CI enables and
requires the actual query SQL packet (at least16checks); the required private
Core job is retained, never skipped to work around missing credentials.

## Completed scope

- [x] Replaced the guided parser's unknown-request -> Finance fallback with
  explicit unsupported clarification. Known supported finance commands still
  use the separately authorized existing finance handler.
- [x] Added denial-only legacy guard: known unsupported domains/actions cannot
  return a successful learner result even if a model proposes a learner tool.
  It is not a general classifier and does not implement those missing workflows.
- [x] Added one versioned `academy-query.v1` catalog for six reviewed read tools:
  teacher/student/batch search and opening a current displayed result. Each
  descriptor has a typed domain-table reader and source-controller module policy.
- [x] Current stored Identity/stamp/role/active-tenant checks independently govern
  Owner/Admin reads; no Finance permission required for a teacher directory.
  Teacher/Student/Guardian/PlatformOwner cannot acquire this admin catalog by
  supplying an Owner claim, conversation or model output.
- [x] Added query provider/endpoint composition, durable encrypted context and
  chronological history, serialized SQL claims, planner outside transactions,
  current reauthorization, mandatory atomic audit and GET-only receipt recovery.
  Existing sessions/turn tables are reused with a distinct marker/protection
  purpose. Legacy receipts and task drafts are not reinterpreted or migrated.
- [x] New main composer negotiates actual host query availability, renders typed
  results and protected history. A missing query route retains an explicitly
  labelled legacy student-fee pilot; uncertain/down/denied query routes do not
  silently downgrade into another domain. Guided workflows/Manual remain.
- [x] Query results distinguish teacher specialty from student data. No contact,
  credentials, compensation or finance fields are projected. Record IDs come
  from current scoped SQL; model positions bind only to saved host results.
  Teacher/Student links target their supported profile/360 routes. Batch records
  link explicitly to the Manual batch directory: no false selected-batch deep
  link is claimed because that Manual page does not consume batchId today.
- [x] Screenshot review found/fixed transcript/composer overlap. Query history
  now scrolls independently; browser geometry asserts the composer cannot cover
  it. Existing premium shared shell/Prism light/Orbit dark styling is retained.

## Actual request path

```text
RolePentaEntry -> AcademyConversation protocol negotiation
  -> createQueryConversation -> PentaQueriesController
  -> PentaQueryConversations current stored authority + durable claim
  -> PentaQueryPlanner /v1/academy-query/plan
  -> Mini academy_workspace_api -> academy_query -> bounded TaskInferenceProvider
  -> closed plan -> host semantic denial/current authorization
  -> PentaQueryReader -> real academy-scoped teacher/student/batch projections
  -> encrypted result/context/receipt + mandatory SQL audit
  -> typed result/history validation -> conversation + current Manual destination
```

Query source/prompt hashes and runtime identity are validated and stored in audit.
They are response-reported source provenance, not independent model attestation.
No SQL/role/tenant/URL proposed by a model is executed or treated as authority.
An upgrade of model weights cannot expand this catalog or enable mutations.

## Validation — source-bound, not summed into product completion

| Check | Actual result | Boundary |
| --- | --- | --- |
| Affected .NET suite | 264 PASS, 0 failed/skipped | Includes 44 new query access/reader/contract/transport/semantic-guard cases; retained Mini/task tests |
| Mini Linux suite | 237 PASS, 0 failed/skipped; one inherited Starlette/httpx warning | 30 new query cases plus retained 207-case task/native/API/capacity/operational selection; model-free |
| Actual C# -> Python request | PASS | C# request vector parsed by actual Python Request schema; six current descriptors and minimized context |
| Client/parser assertions | 106 PASS | 32 query/workspace/platform/portal plus 74 retained legacy Mini/exact-money/recovery cases; synthetic channels/storage |
| Query native SQL/Identity/HTTP | 16 PASS on final source | Explicit synthetic planner, real owned database reads and authentication/persistence; no model qualification |
| Existing PentaFoundation native packet | PASS | Existing protected source search, budgets, recovery/approval, current actor/tenant scopes, 5/5 stale-tab races retained |
| Migrations/owned cleanup | 86 application + 7 Identity migrations PASS | Disposable SQL and runtime login guard; owned database/login/container removed |
| Query browser | 8 PASS | Owner/Admin x 320/1440 x light/dark; actual exported components, synthetic API interception |
| Existing portal/shell browser | 36 PASS | Six roles x three widths x two themes, native dialogs; synthetic APIs |
| Next 16.4.5 compatibility build/typecheck/export | PASS, 92 pages | Isolated D: copy, cached dependencies, evaluation flags only in that copy; lockfile caveat below |
| Changed frontend ESLint | PASS, zero warnings | Same isolated compatibility dependency cache; exact locked CI remains required |
| Diff validation | PASS | Source and documentation; no history rewrite/main merge/deployment |

Final native run: `115e4931853e4663bc0333c6cc5c6676`, elapsed 35.5 seconds;
earlier complete run `717d0c99092f47aba231b67294a77493` also passed. These overlap,
not 32 different query checks. Mandatory-audit fault retains pending claim with
no committed private receipt/context. Concurrent duplicate/recovery do not call
the planner twice. Actor revocation after planning returns403 with no result.
Other actor/tenant/teacher access is refused. Student/teacher/batch row counts and
payment rows/totals remain unchanged. No HTTP429 occurred in this native packet.

Browser exercises teacher result -> correction to student -> lost response ->
GET recovery -> reload/history without another POST. Layout checks include real
composer/transcript geometry, no horizontal page overflow and cleared owned
browser contexts/profiles, with browser sandbox retained. This does not represent
an actual model conversation, physical device or combined SQL/browser run.

## Retained failures / environment qualifications

1. Initial API compile lacked the existing Security namespace import; corrected.
   Initial harness compile used an invalid multiple `var` declaration; corrected.
2. Initial direct TypeScript invocation lacked this checkout's generated Next
   LayoutProps. Full isolated Next builds generated and validated the types;
   an actual query-client narrowing error found there was corrected.
3. Windows Python could not import Linux fcntl. Model-free endpoint validation
   ran in the existing Linux image with no model mounts/network/downloads instead.
4. First native invocation was blocked by process script-execution policy before
   SQL startup. The existing runner was invoked with process-only ExecutionPolicy
   Bypass; no machine policy changed. Original failure log retained privately.
5. The old interactive-demo lease owner PID20384 was confirmed absent. Its exact
   lease bytes were archived privately before acquiring the new validation lease.
   No active lease was stolen, and no original service/container/volume deleted.
6. First browser attempt hit a test wait's null document.body during reload.
   The wait now requires a ready body; assertions retained. Separate visual review
   then found actual composer overlap, corrected with a scroll-contained transcript
   and a new geometry assertion. Final eight query and36shell cases pass.
7. First cached frontend tree was Next16.3.5, so its successful build is only a
   historical smoke result. Final builds use existing Next16.4.5/React19.2.8 cache.
   A comparison against the current lockfile still found missing optional packages
   and different development lint dependencies. **Exact locked npm-ci/build/audit
   remains a release gate.** No package or lockfile changed or downloaded here.

Private logs/source vectors/screenshots remain `.build-check/d67-query/`;
final query screenshots `browser/2026-11-24T05-26-54.434Z-7564/`.
Final shell screenshots `.build-check/penta-workspace-shell/2026-11-24T05-27-07.166Z-9288/`.
No raw credentials, prompts from customers, backups or model weights are published.

## Configuration / next acceptance

Defaults remain OFF; no live model/production activation occurred. An approved
isolated evaluation needs:

- Mini `penta_mini.academy_workspace_api:app`, loopback binding and
  `PENTA_ACADEMY_QUERIES_ENABLED=1`, with the existing private runtime/gate config.
- Host Dev/Testing, `Penta:Enabled`, `Penta:Queries:Enabled`, explicit loopback
  `Penta:Queries:BaseUrl`; current real Owner/Admin Identity, SQL and protection keys.
- Isolated frontend `NEXT_PUBLIC_PENTA_MINI_EVALUATION=true` and its owned API URL.
  No committed password or secret is provided by these instructions.

Do not execute a new weight-loading/inference packet from a generic continuation;
the prior approved one-off 4B comparison is complete. New model-backed teacher,
correction, specialty/status, reference, student/batch and unsupported-operation
journeys must qualify the connected source before this is enabled for an owner
demo or production. Model source selection alone is not those outcomes.

## Still open — next coherent work

- [ ] Real-model acceptance of this connected pipeline, including natural grounded
  answer quality/latency and adversarial wrong-domain cases. Templates now reflect
  typed authoritative results; no new factual answer-generation pass is claimed.
- [ ] Extend the SAME catalog/context/result architecture to finance/exact balances,
  calendar, attendance and remaining D66 families with current domain permission.
  The legacy/guided finance paths remain separate and preserved.
- [ ] Bind existing dependent task drafts to approvals and deterministic execution;
  complete Batch mutation/reference resolution and student + batch + enrollment
  end-to-end. No create/update/delete/send operation was enabled by D67.
- [ ] Remaining Teacher/Student/Guardian/Platform catalogs and operational parity;
  broader history reasoning, cross-entity joins, outcome ledger and P/E/N/T/A engines.
- [ ] Exact-source locked CI, paired private-Core source credential, missing genuine
  archives, original critical issue/device/staging/security/release gates.

BUG-AI-0001 stays OPEN. Host source/SQL/client wiring has progressed; neither the
90% operational target nor full conversational coverage nor production readiness
is accepted. Stay with Sol High for the next integration/security work.
