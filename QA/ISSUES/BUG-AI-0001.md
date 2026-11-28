# BUG-AI-0001 — Teacher requests are answered as verified learner searches

| Field | Value |
| --- | --- |
| Status | OPEN — typed query wiring/SQL/browser checks pass; real-model semantic acceptance pending |
| Priority | P1 / blocker for AI-first product acceptance |
| Source | 29f908a494f2c0919cd1b5a29175c72735eee895 |
| Confirmation | Owner screenshot plus real cached Mini / Identity / SQL |
| Regression | PENTA-ENTITY-CORRECTION-001 |

23 November 2026: on the owned synthetic demo
`fc7ae045f5574f66b143e2ff9291507f`, an authenticated Owner reads four teachers
from the existing authorized academy teachers API. In a separate Mini conversation,
both prompts below return HTTP201, kind RESULT, a learner result with count0 and
the message "No students match these filters.":

1. "Show me the teachers in my academy."
2. "I said teachers"

Private evidence is retained in that demo's
`teacher-intent-reproduction-5e823352-7cd0-4a8b-b139-e13f2757b65c.json`.
No credentials or customer records are published. This did not alter the owner's
browser conversation, ledger or domain records. The already-authorized running
cached model was used; no download, model replacement or Azure action occurred.

## Root causes verified in current source

28November D76: actual evidence proves first empty-context503 was completed
teacher/search/refine, then an open reply included an illegal name filter. Source
9f09e48 passes all4teacher cases plus student/batch/outstanding fees; latest7/8
executed,6UNRUN. Student financial follow-up still lost a compatible reference.
Typed reference/no-broadening repair at45d7a5a passes370Linux model-free checks,
not live acceptance. D76all3attempts consumed; D77approval pending. Keep OPEN;
original mixed-source/host/role/outcome/device release conditions still apply.
See ../REPORTS/D76_4B_REPAIR_WINDOW_2026-11-28.md.

27November verification: D73real4Bteacher correction selected teacher but copied
student name Mira; D74context isolation implemented at Mini76560ee, but its real
retest stopped at first503/16.926s,13UNRUN. Internal cause unknown because old
runner removed Core logs. Current d144e0a diagnostics preserve closed failure
stage/reason and bounded private logs;361Linux model-free checks pass. No live
pass follows this change. Issue remains OPEN;4Bhas not earned default promotion.
See ../REPORTS/D74_D75_4B_RESULTS_2026-11-27.md. Do not discard old engines/evidence.

D73 closes a separate transport gap: structured task/query generation must stop
normally and return one assistant JSON object without duplicate keys and within
response bounds before compilation.322selected Linux model-free checks pass,
including actual transport-to-endpoint rejection; this is not model intelligence
acceptance. Aurora/attendance4147048 ordinary exact CI passes2,799API and39native
query checks with synthetic planner/real SQL. The NEW connected4Bpacket is prepared
and awaits explicit approval. Teacher/live quality and all-domain conversation
remain OPEN. See ../REPORTS/D73_REPLY_BOUNDARY_AND_MODEL_PACKET_2026-11-24.md.

D69 adds connected calendar reads and denies teacher/batch directory substitution
for classes, unsupported student-calendar inference and known scheduling writes.
304affected host,260model-free Mini,163client,31native query and52browser checks
pass. Real-model conversation quality and all-domain/role acceptance remain OPEN;
see ../REPORTS/D69_CONNECTED_CALENDAR_READS_2026-11-24.md. No model was activated.

D68 extends D67's connected query path to permission-bound student balances and
same-student follow-ups. Final280affected host,244Mini model-free,116client,
24native query and52synthetic-API browser checks pass. No real model was called;
teacher intent/answer quality and complete conversational acceptance remain
OPEN. See ../REPORTS/D68_QUERY_STUDENT_BALANCES_2026-11-24.md. This progress does
not close the issue or qualify all domains/roles.

Historical reproduced source is described below. D66 confirmed the wider missing
catalog/result/history/dispatch connections. D67 adds the first connected typed
query slice and legacy/guided routing guards; the all-portal/model requirement
remains OPEN, not repaired merely by a model swap.

- `PentaMiniProtocol.cs`: Available exposes only SearchLearners/GetLearner;
  MiniState stores learner IDs and learner query filters only.
- `AcademyDeskConnector.cs`: both executable paths read OutstandingFeesService.
  There is no teacher retrieval path in this connection.
- Mini's current orchestrator prompt/schema plans into that narrow vocabulary;
  it incorrectly converts these unsupported requests into learner searches.
- `PentaMiniOrchestrator.cs`: non-tool model prose is replaced by fixed pilot
  messages. This is not a general grounded conversation/answer composition stage.
- `mini-conversation.tsx`: every RESULT receives the heading Verified learner
  read. Successful schema/HTTP checks do not verify that the requested entity
  and answer source agree.

## Required repair and acceptance

Use the existing domain services and host authority. Implement a versioned
entity-aware conversation/tool contract, permitted teacher list/detail reads,
domain-scoped contextual references and source-backed answer composition. Keep
legacy receipts and exact-money/recovery safeguards; do not relabel teachers as
learners, pass arbitrary SQL to the model or accept invented facts.

First acceptance journey: teacher list from real authorized teacher records;
correction from students to teachers; "only piano" stays in teacher context;
selection/disambiguation opens the correct teacher; status/subject/branch facts
match the source; switching back to students clears incompatible context.
Ambiguous relationships ask a useful question. Unsupported/denied/outage requests
must not become a successful empty learner search. Validate role/tenant and
revocation limits, audit, duplicate dispatch/recovery and real paraphrases.
Evaluate fresh held-out wording separately from exposed regression examples.

The existing 1020-client/256-browser/13-native pilot checks remain valid at their
stated scope. They do not cover or pass this outcome. Do not close this issue
with a wording change, a keyword-only demo, a mock teacher response or a larger
model selection. Teacher conversational acceptance and broader AI-first launch
remain OPEN until the real model-to-source-to-answer path passes.

## D62 foundation progress — 23 November 2026

Owner clarified that teachers are an example, not the product scope. Whole-product
Manual capability indexing and the first typed multi-task draft reducer now exist;
896affected checks pass, including56new cases. They are inactive internal host
foundation code, not a repaired live conversation. The current demo's two-tool
learner path is unchanged. Keep this issue OPEN; teacher/entity correction and
compound cross-domain model/SQL/browser acceptance remain mandatory. See
`QA/REPORTS/D62_CONVERSATIONAL_TASK_FOUNDATION_2026-11-23.md`.

## D63 runtime progress — 23 November 2026

Default-off protected draft conversations/current-authority catalog now exist,
with934affected host tests,167model-free Mini checks and19actual SQL/API checks
passing. Existing security/privacy/exact-money regressions also pass. These use
a synthetic planner and do not close this issue. The new catalog is write-draft
only; teacher factual reads/context/answer composition still need implementation.
Real cached2B goal/mapping/compound/latency probes failed; the live legacy composer
is unchanged. Keep OPEN. See QA/REPORTS/D63_PROTECTED_TASK_DIALOGUE_2026-11-23.md.

## D64 native-routing investigation — 24 November 2026

Native-template variants still fail semantics on the cached2Bprofile: parallel
calls repeat/truncate, a bounded workflow list converts a teacher read into
Teacher.Create, and a serial loop adds unrequested operations. Actual execution
is absent and the new adapter stays inactive.207model-free checks do not close
this issue.4Bartifact verified only; comparison requires owner approval under
the existing loading stop. Missing authorized read/context/grounded-answer and
full operational integration remain separate host gaps. Keep OPEN; see
QA/REPORTS/D64_NATIVE_ROUTING_AND_DEMO_RECOVERY_2026-11-24.md.

## D67 integration correction — 24 November 2026

Typed teacher/student/batch search/open now have a current-authority host catalog,
real scoped readers, protected context/history/recovery and a negotiated main
composer. Unknown guided prompts no longer go to Finance; legacy known unsupported
domains cannot become successful learner reads.264host tests,237model-free Mini,
106client/parser assertions,16real SQL/Identity/API query checks and8query/36shell
synthetic browser cases pass. No real model was invoked in this batch; queries
remain default-off. The actual connected held-out teacher/correction/follow-up
journey plus remaining operational coverage are still required to close this
issue. See QA/REPORTS/D67_CONNECTED_QUERY_PIPELINE_2026-11-24.md. Earlier4Bapproval-
pending text above is historical; the single approved comparison completed and
failed before this integration correction, without activation.
