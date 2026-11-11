# PENTA host integration status — 5 November 2026

## Backend integration sequence — 10 November 2026

The Academy Desk host and Manual release work can advance in parallel. Host
integration uses deterministic test requests and the same domain services as
Manual. The Mini loading and activation stop still applies. Model qualification
starts when the intended pilot tools have a verified host boundary and the
owner authorizes the specific private evaluation packet. Production release
then requires both AI-inclusive acceptance and the existing ERP release gates.

| Order | Backend packet | Current boundary |
| --- | --- | --- |
| 1 | Student and batch lookup, academy context, student and teacher intake | Bounded read pilots and protected credential-free intake paths exist. Verify the intended end-to-end operations against the approved host/model contract before enabling them. |
| 2 | Enrollment | Testing-only create, status and transfer share Manual rules and have protected approval, execution and recovery. The final run passed 23 create, 16 lifecycle, 13 preview and 57 shared native checks. Model and browser acceptance remain open. |
| 3 | Scheduling, make-up classes and attendance | Testing-only make-up, regular class creation and rescheduling use shared Manual rules and approval. Assigned-teacher attendance shares assignment, enrollment and InApp rules. Manual batch reassignment now preserves substitutes and shares the clash lock. Native checks passed. Browser, device and conversation acceptance remain open. |
| 4 | Billing, collection, reconciliation, expenses and payroll | Testing-only academy totals, invoices, collections, reconciliation, expenses and payroll reuse Manual ledger sources and current authority. Bounded previews preserve currency and redact sensitive ledger fields. The Academy Admin chat runs bounded Testing-only reads with browser verification. The Testing-only collection API shares the Manual transaction, checks exact approval and returns a recoverable receipt. Admin browser approval and recovery come next. |
| 5 | Leads, communications, resources, private media and remaining role journeys | Bind current CRM and portal permissions, provider readiness, consent and private storage rules. Distinguish a queued message from confirmed delivery. |
| 6 | Private model qualification and UI acceptance | Use the agreed PENTA frontend and versioned Mini handoff. Verify real dialogue, clarification, corrections, refusal, completion and fallback per role. Keep credential and security administration in secure Manual flows. |

Measure the 90% goal using eligible user outcomes that pass the full
conversation-to-result journey. The C2 HTTP inventory gives source coverage
for planning. It includes transport and protocol routes and does not define
the user-outcome denominator.
[C2 operation inventory](https://github.com/AcademyDesk/AcademyDesk/blob/3397887b97fbbff0d994cdc22b229e4eba0d7d3a/QA/HANDOFF/C2_OPERATION_MAP_2026-11-05.md)

Cloud C24 and C25 are separate Manual finance returns, pinned at `2114c8c`
and `a40c487`. Desktop still needs to integrate and verify their live invoice,
family and reminder paths. Preserve the source-bound C23 and session evidence.
Cloud C26 owns provider settings and meeting availability. C27 covers Sales
trial permissions and lead conversion. Desktop owns the scheduling and
attendance host packet.
[Make-up scheduling evidence](QA/REPORTS/PENTA_MAKEUP_SCHEDULING_2026-11-10.md)

## Cloud conversation/container source integrated — 6 November 2026

- [x] C3.2+C4 returned66implementation paths copied byte-exact;12vendor pins pass.
  Protected student/teacher UI adapters and login-epoch/session controls restored.
- [x] Fresh48case Identity/HTTP/SQL communication roundtrip PASS. All three missing
  receipt cases now pass locally; existing communication-drafts48/48PASS.
- [x] Full current frontend1,893PASS/one Windows symlink EPERMfailure/zero skips;
  no assertion weakened. Nginx synthetic component10HTTP checks plus query PASS.
- [x] Owned SQL/web resources removed; original services/images healthy/API200,
  complete inventories and lease release PASS. Backend/API1,984source unchanged.
- [ ] Exact-lock full image/current CI/audits, actual protected host/browser/device,
  remaining all-role/workflow/security/release and Mini qualification stay OPEN.

Read QA/REPORTS/LOCAL_L2_CONVERSATION_CONTAINER_2026-11-06.md for exact sources,
run/failure/receipt boundaries. Cloud lacks local raw receipts; do not claim its
three evidence cases passed or publish QA/EVIDENCE. No main/Azure/model activation.

## Current continuation — 6 November 2026 (supersedes old next-task instructions)

Fetch `codex/penta-host-integration-20261030`; integrated code checkpoint
`be3093a92ba9db1d822c33ecb55374665fcced73` contains the protected host fixes and
byte-exact cloud C3.1 role-entry source. Fetch the actual current branch tip;
this documentation-only continuation may be newer. Preserve main and dirty trees.
Mini return: `codex/local-c1-evidence-return-20261105` at
`e55e5b9622283f7be8c34f7a717369a8efd73a7d` (tested source216550e).

Completed since the historical 5November start:
- [x] L1 real migration/foundation/student/teacher SQL results with their recorded
  source boundaries; do not redo the enterprise audit.
- [x] Default-host lease singleton and eight actual-composition controls; current
  API1,984PASS/zero fail/skip. No interpreter/model activation.
- [x] C1 genuine local-input return: Linux1,738PASS/two strict provenance failures/
  one inherited skip; separate Windows39metadata/24native/six actual-kernel checks.
  Original input/packet hashes and33frozen checkout paths preserved.
- [x] C2 static359declarations/81controllers match current source; static only.
- [x] C3.1 copied exactly from d0d7967/cloud reportcc5c8de. Local routing/login93
  PASS and separate session93PASS. Entry honestly says conversations unavailable.

Next ownership, without overlapping source edits:
1. Cloud C3.2: claim protected conversation/context/session paths before restoring
   real student/teacher draft/approval/execute/receipt UI. Preserve host authority;
   do not mount fixtures or present unavailable tools as live conversation.
2. Cloud C4: exact-lock production build/audit/CI. Local cached Next16.4.1 is not
   locked16.4.3; no wrong-version local build or unauthorized download substitute.
   Actual production Dockerfile restore remains a prerequisite despite the cached
   runtime smoke. Three genuine communication-evidence frontend cases stay open.
3. Cloud C5: versioned portable exact-original-parent/source binding or retain the
   original Windows-only review. Two Linux review cases remain failing; do not
   rewrite originals/expected hashes or normalize authority implicitly.
4. Local L2: actual isolated role/session/Manual-fallback browser validation once
   pinned build dependencies are available; preserve source-bound existing SQL.
5. Continue current remaining workflows/all roles, privacy/security/device/P0,
   DB/integration/restore/staging/release program. Main/Azure remain untouched.

Read the three6November reports in QA/REPORTS: LOCAL_C1_EVIDENCE_RETURN,
PENTA_HOST_COMPOSITION and LOCAL_C3_ROLE_ENTRY_INTEGRATION. Original cloud C1/C2
and source-bound C3.1 results are not active model/AI acceptance. Mini/Plus/Pro
remain unqualified; Ultra deferred; candidate loading/training/inference/activation
stop remains. Sol High for security/contracts/integration; Sol Medium for settled
visual/docs work. No new test plan or frontend rewrite from zero.

The older numbered starting tasks below are history where they conflict with
this section. GitHub is the durable exchange; no chat/laptop live sync is claimed.

HOST-ONLY SQL MODULES PASSED / MODEL AND PRODUCTION NOT QUALIFIED.

Read QA/REPORTS/PENTA_HOST_SQL_QUALIFICATION_2026-11-05.md. Three modules passed
in the first window; a private teacher snapshot helper repair passed a teacher-only
successor. No application/model change. This is not a full corrected-binary CI run.

Current task:codex/penta-host-integration-20261030 inD:\AcademyDesk-penta-host.
Base published64b222d joins student13ed932 and the other profile'sa63e392source.
This document does not establish publication or production acceptance.

| Area | Implemented source / acceptance boundary |
| --- | --- |
| Independent authority | Current stored authenticated actor/academy/role, allowed fields/tools, branch/domain validation; memory/model output is not authority |
| Domain services | Shared student/teacher/enrollment operations and deterministic finance retained; credential-free student operations cannot initialize roles |
| Protected host | API1976source-bound reuse; migration/foundation/protected student SQL PASS including actual scripted .NET/Python sockets; corrected teacher-only SQL PASS. Guards preserved and services restored |
| Persistence |12new PENTA tables; additive cloud84-to85 migration. Nonempty downgrade is blocked, empty downgrade and re-upgrade preserve existing rows/history |
| Model calls | No new real Mini/model invocation or activation. Fake planner/interpreter tests establish host enforcement only |
| Available roles | Retained pilot is Owner/AcademyAdmin only. This is not Teacher/Student/Platform-owner conversational acceptance |
| Writes | Student/teacher protected write adapters stay Testing-only behind explicit flags. No new production write/tool is enabled |
| Legacy probe | Retained fake-provider contract tests/types compile; external provider client is not registered or configured |
| Frontend | Existing PENTA/Manual sources preserved; current work adds backend only. Full connected UI/Prism/Orbit/Aura/device acceptance still pending |
| Mini qualification | Full cloud portability previously FAILED; semantic result NOT ACCEPTED. Owner's stop before candidate weights loading remains |
| Production | NOT ACCEPTED. Exact source CI, locked web build, active engine qualification, remaining workflows/all roles/security/privacy/recovery/staging/release gates remain |

Read AI_HANDOFF.md and the current QA report for actual passing source and run IDs.
Do not interpret a disabled adapter, synthetic diagnostic or passing API tests as
90%conversational operation coverage. Pulse/Executor/Navigator/Twin/Autopilot are
the product direction; each real operation still needs its own qualified domain,
permission/approval, receipt/recovery, model, UI and customer-critical acceptance.

Next route remains Sol High for current security/transaction/migration integration.
Sol Medium can handle settled UI/docs afterward. No main merge, configured/customer
DB migration, Azure deployment, package/model download, training or activation.
