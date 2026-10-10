# Academy Desk regulatory implementation and evidence map

Assessment: **2026-10-10, Phase 0 + focused Phase 1 security verification**.
Engineering owner: Academy Desk / Codex, Sol High. Legal reviewer: **NOT ASSIGNED**.
This is implementation evidence and a prioritized gap assessment, not a legal opinion,
certification, exhaustive legal-change search or approval for customer production.

## One canonical register

PENTA Mini/Core owns [regulatory register v0.1](https://github.com/AcademyDesk/PentaMini/blob/2f8806967709a1a7ec29e5176e7c31968adbf6aa/COMPLIANCE/REGULATORY_CONTROL_REGISTER.md).
Pinned Mini commit `2f8806967709a1a7ec29e5176e7c31968adbf6aa` was verified against
its remote feature branch; register Git blob `956e5b37b5fdfe982a23862e5a696f142e7205c0`.
Do not create a second legal register here. The IDs below inherit that register's
provisions, jurisdiction, official sources and classifications. Local evidence is
additional, not legal sign-off. Canonical next source review: 2027-01-10; recheck
immediately on a new notification, geography, data role, feature or subprocess change.

Status vocabulary: **DESIGNED / IMPLEMENTED / TESTED / SECURITY REVIEWED /
LEGAL REVIEWED / PRODUCTION APPROVED**. A source inspection is not a runtime test;
a reference policy is not an enforced integration. All entries below lack legal
review and production approval. Engineering review here is not an independent audit.

## Official-source verification and limitations

| Instrument | Status as assessed on 2026-10-10 | Evidence / limitation |
| --- | --- | --- |
| DPDP Act 2023, phased commencement G.S.R.843(E) | Machinery provisions already commenced; s6(9)/27(1)(d) in the one-year phase; main processing duties in the eighteen-month phase | [Official notification](https://www.meity.gov.in/static/uploads/2025/11/c56ceae6c383460ca69577428d36828b.pdf) inspected. Canonical dates 2025-11-13, 2026-11-13 and 2027-05-13; [MeitY/PIB](https://www.pib.gov.in/PressReleasePage.aspx?PRID=2261823&lang=1&reg=3) explicitly confirms 13 May 2027. Do not describe all provisions as operative today. |
| DPDP Rules G.S.R.846(E) | Canonical: rules1,2,17–21 immediate; rule4 consent-manager phase 2026-11-13; rules3,5–16,22,23 2027-05-13 | [MeitY index](https://www.meity.gov.in/documents/act-and-policies/digital-personal-data-protection-rules-2025-gDOxUjMtQWa) and canonical source reviewed; [complete rule PDF](https://www.meity.gov.in/static/uploads/2025/11/53450e6e5dc0bfa85ebd78686cadad39.pdf) could not be independently fetched in this task. Corrected rule-by-rule legal verification remains OPEN. Rule4 is not a universal November deadline for every academy consent form. |
| Rules corrigendum G.S.R.892(E) | Notification dated 10 December 2025; Gazette 11 December, not the website's 16 December listing date | [Official corrigendum](https://www.meity.gov.in/static/uploads/2025/12/3c7ebbae0e5456f493f486e6845df86b.pdf) fully inspected. Corrects publication phrasing, Departments, order wording, body/Companies Act reference and schedule lettering. It does not replace the phase lengths. Request canonical textual mapping update; no claim of legal approval. |
| Current IT Act / SPDI transition | Applicable current obligations must not be treated as already repealed merely because DPDP was enacted | [Act text](https://www.meity.gov.in/static/uploads/2024/06/2bf1f0e9f04e6fb4f8fef35e82c42aa5.pdf) and commencement place s44(2), including omission of IT Act43A, in the eighteen-month phase. Canonical REG009; counsel determines current SPDI scope, including medical/health data. |
| CERT-In 2022 directions | Operative requirements for covered entities; separate from future DPDP breach notification | [Directions](https://www.cert-in.org.in/PDF/CERT-In_Directions_70B_28.04.2022.pdf) fully inspected: specified reportable incidents within six hours of noticing/being brought to notice; PoC, synchronized ICT clocks, securely held rolling180-day ICT logs in India. [FAQ](https://www.cert-in.org.in/PDF/FAQs_on_CyberSecurityDirections_May2022.pdf) fetch failed here; covered-entity/FAQ review still required. This is not blanket180-day retention of every student record. |
| IT Rules2021 / G.S.R.120(E), synthetic information | February2026 amendment operative 20 February2026 for entities/use cases within scope | [Notification](https://www.meity.gov.in/static/uploads/2026/02/f55fe52418b03f58b0669f6a8bc03b6d.pdf), [official consolidated rules](https://www.meity.gov.in/static/uploads/2026/03/0b576f2071694b52e4cd6bb1b6dfab1e.pdf) inspected. Applicability depends on intermediary role and realistic synthetic audio/visual media; internal text chat is not automatically such media. April proposal must not be called enacted; final October change completeness remains a legal-review item. |
| Telecom commercial messaging / DLT | Conditional on SMS/voice commercial communications and provider/principal-entity roles | [TRAI official amendment index](https://trai.gov.in/release-publication/regulations/amendments-page/7617) now lists Third Amendment2026, 18 September2026. [Gazette PDF](https://trai.gov.in/sites/default/files/2026-09/Regulation_18092026.pdf) exceeds browser fetch limit. Exact commencement/exceptions and provider implementation need review before enabling sends; do not use the March draft or February2025 amendment as the latest final text. Canonical entry requested. |
| RBI payment-data rules / PCI | Conditional on actual payment-system role and card-processing integration, not every academy invoice ledger | [RBI notification](https://rbi.org.in/Scripts/NotificationUser.aspx?Id=11244) and official role guidance are registered for review. Payment-system-provider obligations must not be assumed for all SaaS; gateway, storage, subcontract and card-data scope need review. No card-data collection feature is authorized by this assessment. |
| Other jurisdictions / certifications | GDPR, EU AI Act, COPPA, FERPA and UK law only after geography/use-case assessment; NIST/OWASP/ISO/SOC are assurance frameworks or contractual requirements, not automatic universal laws | Canonical REG015–020 retained. EU AI Act amendment/timeline assertions in the register need independent consolidated-source review before reliance; UK entry requested. No global launch, certification or automatic high-impact AI is approved. |

This checkpoint does not assert that no other amendments exist through October2026.
Unfetched/full-text and applicability gaps are explicit release-review dependencies.

## Academy mapping to canonical IDs

Owner A = Academy engineering; M = Mini/Core; H = human legal/privacy/security owner.
Production criticality means launch risk, not an invented statutory defect severity.
Evidence IDs S1/S2/H1, K1 and H2 link to source/tests rather than an unexplained PASS.

| Canonical ID / classification | Academy applicability / control location | Evidence and present status | Gap / owner / criticality |
| --- | --- | --- | --- |
| REG001–002 now + future / review | India purposes; this map, release gates, pinned shared register | DESIGNED date/applicability process; primary checks above | Corrected rules, entity roles, scheduled source review / H+M / HIGH |
| REG003 future + current SPDI/contract review | `Controllers/ComplianceController.cs`, `Domain/Entities/ConsentRecord.cs`, marketing preferences | S1: IMPLEMENTED generic consent/withdrawal, not purpose/versioned notice approval | Reviewed notices, basis and consent evidence by purpose; withdrawal propagation / A+H / HIGH before personal-data pilot |
| REG004 future + current safeguards | Program.cs Identity; AcademyAccessFilter; PENTA policy/connector/orchestrator; private media | S1+H1: IMPLEMENTED/TARGETED TESTED current read boundary; opt-in encrypted key-ring foundation [K1](../QA/REPORTS/PENTA_PROTECTED_KEY_RING.md) TESTED, disabled | Actual durable key provisioning/migration/rotation/restore, deployed encryption/network proof, mandatory privileged MFA / A+H / CRITICAL pre-Azure |
| REG005 future DPDP | No verified personal-data-breach workflow | DESIGNED response requirements below, NOT IMPLEMENTED | Separate applicable DPDP notices/board deadlines; legal-approved templates and rehearsal / A+H / HIGH |
| REG006 future children + current child safety | `Student.DateOfBirth`, `StudentGuardian` access flags, consent records | S1 relationships/flags; [H2](../QA/REPORTS/PENTA_READ_PRIVACY_BOUNDARY.md) 16 synthetic HTTP/SQL minimization/claim-denial tests PASS, not parental-consent/age assurance | Unknown DOB is not adult; reviewed authority/consent/purpose, age transition and actual inference minimization / A+H / CRITICAL before minor-data AI |
| REG007 future + current contract/retention | Compliance create/withdraw; platform retention setting; PENTA24h expiry | S1: PARTIAL; expiry is not erasure | Rights case management, reviewed schedule/holds and deletion across copies/backups / A+M+H / HIGH |
| REG008 future + conditional current transfer rules | Local Mini HTTP provider, private media managed identity | S1: configured destination boundary, no location certification | Map actual SQL/Blob/inference/log/backup/support regions/subprocessors and contracts / A+M+H / CRITICAL before customer data |
| REG009 now + transition | Medical/accessibility notes and personnel/financial records may enter current SPDI scope | S1: authentication/least privilege present; legal classification UNREVIEWED | Privacy policy/consent/security/transfer contractual assessment during transition / H+A / HIGH |
| REG010 now | Logs/audit infrastructure and incident operations | H1 only proves mandatory read audit failure behavior, not CERT operations | India180-day ICT logging evidence, restricted access, clock sync, PoC and six-hour drill / A+H / CRITICAL operational launch |
| REG011 conditional / drafts separate | Student/teacher uploads and future generated media; text PENTA | Current read returns source facts; no arbitrary generate/publish tool | Determine intermediary/content roles, review synthetic-media scope, takedown/complaint controls; no default media label claim / H+A / HIGH if applicable |
| REG012 voluntary guidance | PENTA host policy + human oversight gates | DESIGNED/partial IMPLEMENTED controlled tools | Risk review and transparency are engineering safeguards, not certification / A+M / MEDIUM |
| REG013 conditional consumer / contract | SaaS offers/subscriptions, AI tier availability/action status | S1 existing UI; current read receipt not a performed write | Reviewed terms/pricing/cancellation, truthful feature/tier claims, no deceptive nudges / A+H / HIGH commercial launch |
| REG014 copyright / child safety | Teacher media; model/software/vendor licenses | S1 private storage does not prove legal content handling | Approved upload rights, reporting/escalation, preservation duties, licenses/SBOM/model terms / H+A+M / HIGH |
| REG015 GDPR conditional | EU customers/data subjects/use cases not approved in this slice | NOT ACTIVATED; canonical mapping retained | Controller/processor/DPA/transfer/rights assessment before geographic expansion / H+A+M / CONDITIONAL |
| REG016 EU AI Act conditional | Admissions/evaluation/profiling/employment capabilities unavailable | No decision tools enabled; NOT LEGAL REVIEWED | Confirm current legislation/timeline and intended-purpose risk; human oversight before high-impact activation / H+M+A / CONDITIONAL CRITICAL |
| REG017 COPPA conditional | US child-directed/known child processing not approved | NOT ACTIVATED | Age/parent verification and notice review before relevant US launch / H+A+M / CONDITIONAL |
| REG018 FERPA conditional | US education-record/customer relationship | NOT ACTIVATED | School-official/contract/access scope review; not universal SaaS applicability / H+A / CONDITIONAL |
| REG019 voluntary / contractual | Existing QA gates, PENTA security tests | H1 targeted integration TESTED; no independent certification | Pentest, assurance scope and contract requirements; no ISO/SOC claims / H+A+M / MEDIUM or contract-critical |
| REG020 proposed / review | Shared versioned register + paired handoff | DESIGNED change review; not automatic legal update | Assigned reviewer, source digest/date/delta, approval, due date, recurring monitoring only if requested / H+M+A / HIGH |
| REG021 conditional future SDF | Designation-dependent enhanced duties | No designation assumption; NOT ACTIVATED | Monitor designation; then required governance/audit/DPIA and relevant controls / H / CONDITIONAL |

Implementation paths above are under `apps/api/`. Exact source checkpoints are
listed in the [checkpoint report](../QA/REPORTS/PENTA_COMPLIANCE_PHASE0_SECURITY_CHECKPOINT.md).
Do not turn this map into a second statutory schedule; submit legal corrections to M.

## Privacy/data-flow and legal role assessment

The following are **provisional role hypotheses**, not legal determinations. Each
purpose needs an approved record: role, purpose/basis, subjects/categories, recipient,
location/transfer, versioned notice/consent, retention/hold and accountable owner.

| Purpose / subjects / data | Provisional roles and flow | Present safeguard / required evidence |
| --- | --- | --- |
| Academy ERP: student/guardian/teacher records, attendance, finance, optional health/accessibility notes | Academy likely decides purposes; Academy Desk processes on its behalf. Browser → authenticated API → tenant SQL/private Blob | Current access checks; need academy instructions/DPA, minimum fields, sensitive-data classification and actual storage/backup regions |
| PENTA fees conversation: admin prompt, bounded displayed identities/filter context and fees result | Academy purpose; Desk host and Mini/Core processor chain subject to contracts; model gets bounded planner context, not unrestricted database access | Actor/tenant-bound protected host state, exact tool allowlist, audited deterministic projection; prompt may contain private material even when host does not persist it. Need engine log/redaction/no-training/retention/egress proof before customer data |
| Desk subscription/account/security | Desk may be fiduciary for its own customers/staff/service-security purposes | Identity and billing separate from academy instruction; purpose/notice/retention/legal review needed, no blanket processor claim |
| Learning/memory/analytics | Separate purpose from operational chat, not implied by using the product | Current metadata proposal is not learning activation. Tenant ACL/origin/purpose/validation/retention, opt-in basis where required, quarantine/review/deletion before durable memory or global training |
| Standalone PENTA + customer connectors | Customer owns authoritative business access; PENTA roles depend on offering | Same Core/Mini policy plus independent customer-system authorization; new connector attestation and geographic/privacy contract review. Not automatically qualified by Academy tests |

## Child-data / privacy / retention work required

- DOB is nullable; unknown age cannot authorize an adult-only AI purpose. Do not
  impose a new blanket ERP denial without approved domain migration and tests.
- Verify guardian identity and authority separately from a relationship row or
  access checkbox. Bind evidence to child, tenant, purpose, notice version and
  grant/withdrawal time; support disputes/revocation and age transition.
- No child targeting, covert surveillance, speculative psychological/medical labels,
  unapproved biometrics/emotion inference, or fully automated consequential outcomes.
  Future academic recommendations require proportionate sources and human review.
- Implement privacy case workflow with authenticated requester/subject/guardian
  scope, jurisdiction, reviewed due date, action/decision evidence and appeal/grievance.
  The model cannot decide to deny a legal right.
- Retention schedule must distinguish student/teacher/finance/media/communications/
  conversations/learning/embeddings/analytics/backups/security logs/audit. Do not use
  the current global2555-day setting as blanket legal justification.
- PENTA24h session expiry denies use; it does **not** physically delete encrypted
  state/receipts/audits. Build tested deletion/hold lifecycle before claiming erasure.
- Delete or justify lawful retention across SQL, Blob, indexes, caches, vectors,
  learning datasets, queued jobs, connector copies and backup expiry/recovery. Preserve
  immutable required finance/audit evidence under reviewed holds; do not destroy it
  because a conversation asks to delete. No universal duration assigned here.

## Threat/risk register and priority

| Risk | Existing mitigation / verification | Next action / dependency | Gate |
| --- | --- | --- | --- |
| Forged role/tenant/context, hostile tools, cross-tenant finance | Independent host checks, strict two-read allowlist; H1 | Qualified real Mini semantic/security and linked browser qualification | First real-read acceptance |
| Authority/resource revoked during inference | Current DB checks after planning; H1 extends module/academy/actor/resource cases | Preserve regression for every future tool | Critical host invariant |
| Audit failure or uncertain/replayed outcome | SQL atomic result/state/audit; H1 completion fault and replay checks | Production audit durability, alerting and tamper-resistant retention proof | Pre-Azure |
| Current mini meaning/planning mismatch | Existing 86/94 qualification NOT ACCEPTED | Mini returns qualified pinned candidate, then host retest; don't repeat unchanged failed run | First real feature |
| Prompt/training/learning leakage | No activated memory/training tools; endpoint redirects/proxy disabled | Core runtime egress/log/retention and redacted validated feedback evidence | Customer-data pilot |
| Unverified minors/consent/purpose | Existing guardian flags/generic consent; H2 tests minimal fee projection/planner context, rejected caller claims and unchanged unknown DOB/guardian rights | Reviewed child-data purpose/verification workflow and actual inference/retention proof; synthetic tests are not approval | Minor-data pilot |
| Host key/session infrastructure weakness | Identity + protected receipts; existing revocation tests historical | Persist/rotate protected key ring safely; privileged MFA, trusted forwarding and effective rate-limit tests | Pre-Azure |
| Exposed media / unsafe formats/uploads | Private Blob guard/managed identity and retained media QA | Close SEC0001 with actual private-network, malware/quarantine, recovery/browser/physical-device evidence | Existing release gates |
| Finance races / lifecycle / policy | Deterministic fee source; historical collection/adjustment race repairs | Keep DATA0001/2/3/10 OPEN through required browser/device/policy acceptance | Existing P0 gate |
| Deployment/CI/restore/supply chain | Workflow dispatch, secret refs, existing Docker evidence | Exact-release CI/test/SBOM/dependency/image/licensing scans, rollback/migration/restore and least privilege | Pre-Azure |
| Incident/logging unproven | Required application audit only | PoC, legal incident triage, India ICT-log retention, alert routing, NTP and rehearsal | Commercial launch |
| Conditional law misclassification | Canonical register + explicit gaps | H confirms telecom/payment/content/international/SDF role; M updates register | Feature/geography activation |

## Incident-response and log design (not operational acceptance)

1. Appoint accountable incident lead and CERT-In PoC; establish restricted contact
   and escalation channels. A document does not submit registration.
2. Preserve detection/notice UTC time, source, trace IDs, affected tenants/data,
   evidence hashes, scope confidence, containment decisions and chain of custody.
3. Triage the CERT-In AnnexI categories and covered-entity applicability promptly;
   retain a six-hour reporting clock independent of future DPDP breach notification.
   Do not wait for complete investigation. Only authorized humans submit reports.
4. Separately assess affected-person/board/contract notices under operative law;
   correct DPDP Rule7 clocks/templates require corrected-text review, not an invented
   universal deadline. Capture decisions, approvals, actual dispatch times and receipts.
5. Contain via authorized rollback/access revocation; audit gaps cause failed/unknown
   status, never model-reported success. Restore only tested runbooks and rehearse.
6. Implement India-located, access-controlled ICT logs with reviewed retention,
   protected integrity, query/export controls, secret/prompt redaction and synchronized
   timestamps. CERT180 days and future DPDP safeguards are distinct policy entries;
   keep minimum evidence, not unbounded personal conversation archives.

## Azure/configuration assessment — source only

`Program.cs` uses ASP.NET Identity opaque bearer tokens (not JWT), current active-user
and security-stamp checks, SQL and authorized routes. The initial assessment found
no custom durable DataProtection configuration. [K1](../QA/REPORTS/PENTA_PROTECTED_KEY_RING.md)
now implements/tests opt-in certificate-encrypted persistence, disabled by default;
actual provisioning, migration, rotation/restore and required privileged MFA remain
unverified. Forwarded-header trust and rate-limiter ordering need a focused
deployment-boundary assessment; their presence alone is not proof of effective limits.

Private Blob code requires PublicAccess.None and production ManagedIdentityCredential.
This is code evidence, **not** verification of deployed grants, firewall/private
endpoints, public access, malware scanning, regions or encryption/key rotation.

`.github/workflows/deploy-azure.yml` is dispatch-only, uses Azure credential/SQL/bootstrap
secret references and API external ingress, and enables startup migrations. It does
not contain the full xUnit/SQL/security/restore gate or establish OIDC. Docker images
are tag-based; least privilege, SBOM/license/scans and exact immutable release proof
remain required. No live Azure resources/credentials or production databases were
accessed; no Azure changes or deployment occurred in this assessment.

## Delivery sequence / human and Mini dependencies

1. **This checkpoint:** canonical mapping, permanent invariants and critical existing
   host-control verification. Do not rewrite accepted domain/UI or restart enterprise QA.
2. **Next Sol High slice:** customer-data read-purpose/minor/prompt boundary and key/
   session deployment hardening design with targeted tests; keep pilot local/default off.
   Select a finite engineering slice from actual gaps, not all documentation at once.
3. **First real read:** Mini must return a qualified language candidate and reviewed
   Core integration contract; actual host/SQL/browser acceptance and source facts.
   Premium UI/manual ERP work can proceed independently using explicitly synthetic data.
4. **Privacy/commercial:** implement approved notice/guardian/rights/retention/deletion,
   incident and contract controls alongside existing enterprise remediation.
5. **Actions:** exact payload approvals, idempotency/transaction/audit/concurrency and
   current authority for each write/send/delete; UI must reflect backend capabilities.
6. **Standalone:** shared Core/Mini contract plus customer connector attestation and
   per-system independent enforcement; never duplicate model intelligence in Academy.

H must review operator identity, purpose/roles, DPA/subprocessors, notices/consents,
children, geographic scope, telecom/payment/content obligations, accounting/GST policy,
retention/holds, incident processes, licenses and contractual tier claims. Assign real
reviewers and evidence references; a configured string cannot manufacture approval.

M dependencies and legal-register correction requests live in the existing
[Mini handoff](../PENTA_HANDOFF_TO_MINI.md). No unversioned changes to active0.1 DTOs,
no learning activation or duplicate policy engine. Current shared reference modules
are not wired into live `/v1/chat`; fake policy-ledger tests are not production proof.

## Evidence keys

- **S1:** source inspection at Academy starting `df184a85e7ffa823589255147f13cdf8f5b512ce`
  with explicitly preserved unrelated dirty Mini work; details in checkpoint report.
- **S2:** historical QA reports/OPEN issue bodies, not a fresh full-suite pass.
- **H1:** real Identity/HTTP/disposable SQL hostile fake-planner fixture in
  `QA/tools/SqlHarness/Program.PentaHostSecurity.cs`, linked checkpoint report. It tests
  host controls, not actual Mini reasoning, legal compliance or other customer connectors.

Production approval remains **NOT ESTABLISHED** under
[existing extended release gates](../QA/09_RELEASE_GATES.md).
