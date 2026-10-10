# Academy Desk government compliance / host security checkpoint

Date: 2026-10-10. Route: Sol High / Codex. Scope: Phase0 mapping and focused Phase1
verification of already-implemented critical host controls, not full directive completion.
Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`.
Starting HEAD `df184a85e7ffa823589255147f13cdf8f5b512ce`.
Publication commit is the commit containing this report; no self-referential SHA.

**ACADEMY DESK GOVERNMENT COMPLIANCE CHECKPOINT: READY FOR REVIEW**

This means the assessment and narrow engineering evidence are ready for review.
It does not mean legal compliance, certification, engine qualification, issue closure
or production approval. No application/domain permission change was necessary for
the tested existing host boundary. New code is regression-fixture code only.

## Twenty-two requested checkpoint items

| # | Requested area | Evidence / honest current outcome |
| --- | --- | --- |
| 1 | Repository | `codex/penta-search`, starting SHA above; feature-only scoped publication. Main observed `f7af512f884ca5fe8ac3ddb287c852492592e709` and not edited. Unrelated dirty Mini/private-spec work preserved and excluded. |
| 2 | Enterprise QA | Continue existing roadmap/issues. Prior1042 baseline and later1130 historical checkpoints are not rerun here. P0, critical P1, runtime/browser/device/integration/release remain OPEN. |
| 3 | India laws / dates | [Implementation map](../../COMPLIANCE/ACADEMY_IMPLEMENTATION_MAP.md) pins shared21-ID register, official commencement/corrigendum/CERT/IT amendments and source-access gaps. Main DPDP processing phase13 May2027; consent-manager phase13 November2026. No assumption all provisions operate today. |
| 4 | Applicability | Separate current/future, conditional telecom/payment/media/geography/SDF, contracts, voluntary frameworks and drafts. Roles and corrected rule-level scope require humans; no blanket global compliance. |
| 5 | Existing controls | Current Identity/security-stamp/role/tenant/module/resource checks, two-read strict tool validation, protected context/receipt, quotas, deterministic fees, atomic completion/audit, private media code and manual fallback. Source scope below, not blanket runtime acceptance. |
| 6 | New work | Canonical Academy implementation/evidence mapping, twenty invariant/policy-level mapping, release-gate extension and Mini correction handoff. Extended hostile-planner fixture from32 to40 checks; no new production policy engine, DTO, model tier or tool. |
| 7 | Unimplemented | Verified age/parent consent by purpose, rights/erasure lifecycle/holds, live incident/log retention, operational key management/MFA, qualified real Mini, durable learning, future domain approvals and standalone assurance remain gaps. |
| 8 | Critical blockers | Real Mini86/94 NOT ACCEPTED; current P0/critical release evidence; customer/minor-purpose/processor proof; operational keys/ingress/log/restore and legal review before customer production. |
| 9 | Children | DOBnullable/guardian relationship flags do not prove age/parental authority/consent. No approved child profiling, targeting, biometrics/emotion or fully automated consequential decision tool. |
| 10 | Privacy/consent | Generic consent/withdrawal and marketing preferences exist. Purpose/versioned notice/evidence/propagated withdrawal/legal-reviewed roles and commercial documents not established. |
| 11 | Tenant isolation | Forty focused controls include actor/tenant denial, hostile fields/UUID/state and current tenant change after planning. Not universal testing of every ERP or standalone connector. |
| 12 | Financial security | SQL fixture: invoice1000, Completed300+Reconciled300, Voided200 excluded → authoritative outstanding400; payments/source unchanged. Existing payment/adjustment race/lifecycle reports retained, not freshly rerun or closed. |
| 13 | Model/tool security | Only SearchLearners/GetLearner, no executable code/SQL/role/delete/send/payment tools; model facts/state/approval/tier branding untrusted. No actual Mini/Plus/Pro/Ultra inference qualification in fake-planner tests. |
| 14 | Learning | No activated durable learning/training or global customer-data training. Metadata does not authorize memory or override policy. Reviewed tenant provenance/purpose/ACL/retention and paired Core contract needed. |
| 15 | Incidents | Design and owner requirements recorded. Separate current CERT six-hour reportable-incident process from future applicable DPDP notice process. No appointed/submitted PoC or rehearsed reporting claimed. |
| 16 | Logs/retention | Completion audit failure tested fail-closed. Operational India ICT logs/180-day retention/NTP/tamper resistance unverified; expiry24h is not physical erasure. No universal personal-data retention invented. |
| 17 | Azure | Source config inspected, no live Azure/credentials/production DB access. Managed-identity/private-Blob code is not proof of deployed least privilege/private endpoints/regions/encryption/backup. No deployment. |
| 18 | Mini dependencies | Remote commit2f880696 pinned; shared policy/profile reference modules acknowledged, not wired into live chat. Corrected legal register and reviewed customer-data integration requested in existing handoff; existing language qualification still blocked. |
| 19 | Human work | Assign legal/privacy/security reviewers; confirm roles/geography/purpose, children, terms/DPA/subprocessors, telecom/payment/content/licensing/GST/retention/holds, incident contacts and operational evidence. No approval invented from reference strings. |
| 20 | Tests | Final40/40 hostile fake-planner Identity/HTTP/SQL controls, retained foundation including5/5 conversation races,78/78 targeted backend; build0 warnings/errors. Exact evidence/exclusions below. |
| 21 | Release | Assessment READY FOR REVIEW; production NOT APPROVED. No issue closed, no certifications, no Azure gate bypass. Full suite, real-provider/browser/device, infrastructure and human approvals still required. |
| 22 | Next | Sol High: bounded customer-data read-purpose/minor/prompt boundary and key/session deployment hardening assessment/tests while Mini qualifies the existing read engine. Keep premium UI/manual ERP and existing enterprise remediation moving; no new testing plan. |

## Inspection scope

Governance and latest handoffs read; source/configuration reviewed:

- `apps/api/Program.cs`: Identity opaque bearer (not JWT), current user/security
  stamp, refresh/expiry, proxy/rate limiter, SQL/configuration, private old-media guard.
- `apps/api/Security/AcademyAccessFilter.cs`: tenant/module/role/grants and selected
  mutation transaction/audit boundaries, not blanket row-level SQL security.
- `apps/api/Intelligence/Penta/PentaFoundation.cs` (`PentaPilotPolicy`),
  `AcademyDeskConnector.cs` (`PentaFinancePolicy`), `PentaMiniOrchestrator.cs`,
  `PentaMiniTools` validation and chat controller: current read boundary.
- Consent/student/guardian/privacy entities and `ComplianceController`, media storage
  registration/Azure private-access guard, deployment workflow and both Dockerfiles.
- Existing release/security matrices and OPEN DATA0001/2/3/10, SEC0001 and relevant
  role/guardian/marketing issue bodies. Their local repairs remain distinct from closure.
- Mini `COMPLIANCE` register/handoff/control plan and non-negotiable policy handoff
  at remote-verified2f880696. No Mini file/container changed.

Existing unrelated dirty Mini provenance/provider changes were part of the local
compiled snapshot, not accepted/published by this task. Their separate evidence stays
local. This run must not be represented as qualification of a clean full application
release or as publication of those changes. New tests use existing public two-argument
MiniProviderOutcome shape, compatible with the unchanged committed read protocol.

## Expanded real SQL / HTTP regression

All original32 assertion bodies retained; runner and production quotas unchanged.
Eight new assertions cover:

| Added boundary | Expected / observed |
| --- | --- |
| Finance module removed during blocked planning |403, no protected result |
| Academy suspended during blocked planning |403, no protected result |
| Actor moved to another tenant during blocked planning |403 despite retained token |
| Displayed student deactivated during blocked planning |201 clarification receipt, no data result |
| Completion-only AuditLogs insert fault |503 safe/manual guidance, no record/SQL/internal exception leakage |
| Completion transaction rollback |Version0 and initial encrypted state unchanged; pending request/turn retained, no receipt |
| Audit persistence |Claimed audit remains, no RESULT audit survives |
| Retry of exact pending input |409 and planner call count unchanged; no blind redispatch |

Fault is armed only after durable claim and planner entry, not during session creation.
Final assertion explicitly verifies one injected completion-audit fault. Denial
fixtures restore authority/source state; all data is synthetic and run-owned SQL.

First expanded attempt `379e08774ced4e8a929aa873cc5944c8` stopped after32 checks because
the fixture exceeded the unchanged20-session per-user cap. This was a test-fixture
setup defect, not a reproduced application failure. Use of second current admin
for added cases fixes it without deleting prior sessions or raising limits. Failure
log retained; exact labeled loopback container inspected/stopped/removed, no prune.

Corrected run `796e91f760284499ba3b75813b9e258c`, loopback58380, passed40/40 plus
foundation,88 application/7 Identity migrations, runtime-target/login/negative-cleanup
preflight and database/login teardown; exit0,45.6s. Exact owned container removed.
Final run after explicit fault-counter assertion: `26cb62c222d94a7299635640de6b251d`,
loopback64376,40/40 host checks plus unchanged foundation and5/5 conversation races
PASS;88 application/7 Identity migrations and scoped database/login teardown PASS;
exit0,41s. Runner removed its exact labeled SQL container. Subsequent inventory
contains only the two pre-existing Mini containers. Both corrected and final runs
remove only their own SQL resources; the failed-run test database was disposable
and discarded with its owned container, while failure evidence remains local.

Commands:

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/penta-mini-sql
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/penta-security-tests --filter 'FullyQualifiedName~Penta'
$env:QA_PENTA_HOST_SECURITY='1'
# QA_PENTA_MINI and QA_PENTA_BROWSER unset only in this child process.
powershell -NoProfile -ExecutionPolicy Bypass -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module PentaFoundation
```

Local evidence (intentionally not committed):

- `QA/EVIDENCE/penta-compliance-host-security-20261010.log` — failed fixture.
- `QA/EVIDENCE/penta-compliance-host-security-retest-20261010.log` — corrected40.
- `QA/EVIDENCE/penta-compliance-host-security-final-20261010.log` — final counter assertion.

No model/network inference, new domain write tools or customer-data test. Existing
16-case synthetic recovery-browser pass from preceding checkpoint retained, not rerun
because no frontend changed. No fresh TypeScript/export, full enterprise suite,
actual Mini semantics, physical device, live Azure, incident drill, VAPT or legal review.

Validation of12 local links in the scoped map/policy/handoff/gate/report documents
PASS; scoped staged diff checks and added-line credential-pattern scan PASS (heuristic,
not comprehensive secret/security certification). Staging guard excludes product
source/tests, local evidence and ignored outputs. Existing Phase1
`node QA/tools/validate.cjs` is **BLOCKED**: missing local
`QA/EVIDENCE/logs/observed-checks.json` (ENOENT). Do not fabricate that earlier audit
artifact or call its24-check historical result a current pass. The file exists in
the primary checkout but is not copied: it is bound to its earlier commit/manifest.
This does not turn
the independently passing focused SQL/backend checks into a full QA acceptance.

## Publication and review boundary

Only mapping/policy/handoff/release/report/fixture and this task's continuity hunks
may commit/push on `codex/penta-search`. Exclude existing uncommitted Mini work,
private conversational specification, all QA/EVIDENCE and ignored .build-check.
No main/Mini edits or merges, production data, Azure deployment, settings changes,
history rewrites or blanket secret values in reports. Normal feature publication
does not execute the dispatch-only Azure workflow or satisfy release gates.

Next route: **Sol High**, not a requirement to repeat Astra audits. Sol Medium can
handle settled presentation/doc updates; authority/privacy/key/transaction decisions
stay with Sol High. Model routing is a recommendation, not an automatic model change.
