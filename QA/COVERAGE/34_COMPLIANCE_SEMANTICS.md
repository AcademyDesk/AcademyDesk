# Batch 34 — Compliance records and consent semantics

2026-09-29. STATIC-SPECIFIED for this slice; application runtime NOT RUN. No app change, HTTP/browser/SQL execution, database write or deployment. Existing application baseline and student UI changes retained.

## Scope and source anchors

COMPLIANCE-FORM-003 and COMPLIANCE-FORM-004, their composed controls and all seven ComplianceController actions. Five native inputs plus four helper declarations = nine lexical declarations already counted in the 649 census. PersonPicker is instantiated twice with independent state, so its two declarations represent four rendered controls, not shared selection state. No new form or declaration count.

- apps/web/src/app/compliance/page.tsx:14–18 expiry display; :27–40 combined people and loading; :43–67 mutations; :73–82 forms, actions and helpers.
- apps/api/Controllers/ComplianceController.cs:12–94: all actions and three request records.
- apps/api/Domain/Entities/PersonDocument.cs, ConsentRecord.cs, AdminWorkItem.cs: defaults; AcademyDeskDbContext.cs:311–313: lengths/indexes; Migrations/AcademyDeskDbContextModelSnapshot.cs:1244, :2325: persisted types. Scalar subject IDs have no mapped person FK here.
- Program.cs:88–106, AcademyAccessFilter.cs:25–123, PermissionCatalog.cs:14–34, SubscriptionPlanCatalog.cs:42: access ordering.
- StandardSelectField/StandardDateField implementations and enterprise-page-state.tsx: hidden input mirrors, local dates and status/alert semantics. Shared controls are re-exported by design-system/controls.ts.

Existing scenarios: COMPLIANCE-FORM-003/004, COMPLIANCE-IDENTITY-001, COMPLIANCE-API-008 through 014 (individually named below), VISUAL-PAGE-021, UI-DROPDOWN-001, UI-TIME-001, FORM-SUCCESS-001, API-RELIABILITY-001/002, AUTH-002. All remain NOT RUN. Findings reuse BUG-DATA-0045, BUG-FUNC-0001/0003 and BUG-API-0002/0003; no new issue ID.

## G02 — Form and payload trace

L(n) means stored nvarchar(n), not a client/server validation guarantee. Test trimmed lengths n−1/n/n+1, whitespace, Unicode including surrogate pairs, omitted/null/empty, and raw API bypass of browser required checks. SQL length failures are not proven user-friendly 400s: neither form has maxLength and neither create action validates these maxima before SaveChanges. Optional omitted/null remains null; supplied blank strings are trimmed to empty, not normalized to null.

| Logical input / source declaration | UI → request | DTO / guard → stored member | Exact boundaries / scenarios |
| --- | --- | --- | --- |
| personType, each PersonPicker | Defaults student; Student/Parent options emit student/guardian; chooses WHICH body member receives personId, not a stored type property | No personType in DTO. Unknown/tampered type sets both IDs null in this UI handler | Both forms independent; switching type clears its own personId. Unsupported type cannot be trusted as a subject contract. COMPLIANCE-FORM-003/004 |
| personId, each PersonPicker | Hidden mirror, initially empty, no required validation; options currently include BOTH students and guardians for either type | Guid? StudentId / GuardianId. UI sets unselected side null, selected side to FormData value (including empty string). API AddDocument allows both null; AddConsent rejects both null; both APIs accept two non-null IDs without subject lookup | Empty string is not equivalent to nullable GUID null: test binder rejection versus explicit null/omitted. Test local/foreign/missing/all-zero/wrong-type/both IDs. Existing BUG-DATA-0045 / COMPLIANCE-IDENTITY-001 |
| documentType | Required text, unchanged FormData string | string DocumentType; reject whitespace/null/empty; Trim; required L(80) | 79/80/81 after trim; no fixed vocabulary; COMPLIANCE-API-010 |
| fileName | Required text, not file upload | string FileName; reject whitespace/null/empty; Trim; required L(260) | 259/260/261; Unicode/path-like strings are metadata, not filesystem operations; COMPLIANCE-API-010 |
| secureReference | Optional text; empty string sent as empty, not null | string? SecureReference; Trim if non-null; nullable L(1000) | 999/1000/1001; blank/null positive controls. No URL validation, encryption, upload or download authorization provided by this property; COMPLIANCE-API-010 |
| expiryDate in ComplianceDocumentControls | Optional StandardDateField; local YYYY-MM-DD; blank converted to null | DateOnly? ExpiryDate; no business min/max/past-date guard; SQL date | Omitted/null, past/today/+30/+31 days, leap dates, 0001-01-01/9999-12-31 raw API bounds and invalid dates. UI year options 1900..currentYear+25 are not server bounds; month navigation not bounded. UI-TIME-001 |
| visibility in ComplianceDocumentControls | Defaults AdminOnly; options AdminOnly/StaffRestricted; clear emits empty | string? Visibility; whitespace/null/omitted becomes AdminOnly, otherwise Trim; required L(30) | 29/30/31, arbitrary value/case, empty option default. No vocabulary check or field-dependent access gate; label alone does not grant restricted staff access. COMPLIANCE-API-010 |
| consentType | Required text | string ConsentType; reject whitespace/null/empty, Trim; required L(100) | 99/100/101; no vocabulary or deduplication; COMPLIANCE-API-013 |
| granted (no visible control) | Always true on this form | bool Granted; API direct false accepted, missing value binding needs explicit test; false initializes WithdrawnAtUtc to server UTC now, true to null | true/false/omitted/null/wrong-type JSON; don't infer evidence/authorization from caller-supplied true. COMPLIANCE-API-013 |
| evidenceReference | Optional text; empty sent verbatim | string? EvidenceReference; Trim; nullable L(1000) | 999/1000/1001; blank/null accepted; no signature validation or mandatory evidence rule. UI shows Not recorded for falsy value. COMPLIANCE-API-013 |

Stored creation fields not supplied by the UI: new entity Id/CreatedAtUtc, AcademyId from route, document Status=PendingReview, ReviewedDate=null; consent RecordedAtUtc=server UTC now and WithdrawnAtUtc from Granted. Neither request accepts caller-selected record IDs, timestamps or initial document status. Check extra JSON fields do not alter server-owned values. Both create actions return 200 with saved entity; UI checks status only and does not parse these responses.

### Typed identity finding extension

The two PersonPickers correctly own separate state, but both receive the same combined people array and do not filter it by personType. Choosing a guardian under Student therefore posts a guardian GUID in StudentId; choosing a student under Parent does the reverse. API does not check existence/type/academy. Display lookup also searches the combined array using studentId OR guardianId, so it can show the expected name despite the wrong typed relationship. Extend BUG-DATA-0045, not a second issue: verify request members and fresh rows, not just labels. Duplicate names, IDs absent from lookup, inactive subjects, both IDs and foreign subjects require separate controls. No cross-tenant disclosure is asserted merely from accepting an invalid reference.

### Persistence and optional-field boundaries

PersonDocuments and ConsentRecords indexes are not unique. There is no exactly-one-subject database check, deduplication, row version or mapped subject FK in the inspected model. Do not confuse StudentGuardian FKs elsewhere with these tables. Repeat submissions can create distinct rows. No actual file is stored or fetched by ComplianceController, and secureReference is not a proof of protected file storage.

Consent withdrawal mutates one row; repeated calls overwrite WithdrawnAtUtc. No per-withdrawal history is stored in ConsentRecords. Generic administrator audit may create events, but lacks a full old/new consent evidence snapshot and is bypassed for platform-flag callers. Whether repeat withdrawal must be timestamp-idempotent or append-only is an explicit policy question, not assumed legislation or an implemented guarantee.

## Action-level contract and states

All action success/denial expectations below assume the access gate in G03 and a valid bound request. Invalid GUID route values/bodies require full-pipeline binding tests; do not infer their status from direct-controller tests.

| Action / existing ID | Implemented contract | Required cases and no-write/readback assertions |
| --- | --- | --- |
| Documents — COMPLIANCE-API-008 | GET scoped by route AcademyId, order ExpiryDate then DocumentType, returns entire rows; no pagination or visibility/person/status filter | Empty/many, null/tied dates, long refs, only tenant rows; nullable sort semantics checked in SQL; equal keys need no invented stable tie-order |
| AddDocument — COMPLIANCE-API-010 | POST validates type/name only, saves entity, returns 200 | Required strings, all optional/subject/visibility variants above, duplicate/concurrent creates; invalid request must not create row; fresh ID/tenant/status/readback |
| CreateReviewTask — COMPLIANCE-API-009 | POST scoped document lookup, 404 absent/foreign; no body. Creates new AdminWorkItem, returns empty 200 | API-only here: no button in this page. Type Compliance, title Review + document type, description from filename, EntityType PersonDocument, EntityId document.Id, Status Open, no assignee. Repeat creates another task; no dedup/status eligibility guard |
| ReviewDocument — COMPLIANCE-API-011 | PATCH scoped document, 404 absent/foreign; Status case-insensitive PendingReview/Approved/Rejected/Expired, canonicalizes case but does NOT trim; invalid =>400. ReviewedDate supplied or UTC date today; returns entity | All 4×4 old→new status pairs allowed by source; blank/null/unknown/padded status; omitted/null/date supplied; repeat updates, future/past dates, expiry/status contradictions and concurrent last-writer behavior |
| Consents — COMPLIANCE-API-012 | GET scoped rows, RecordedAtUtc descending, no granted-only/person filter | Granted/withdrawn, empty/many, equal timestamp, missing lookup subject; readback preserves original evidence and recorded time |
| AddConsent — COMPLIANCE-API-013 | POST requires type and at least one non-null subject, saves with caller Granted, returns entity | Typed IDs plus required/optional boundaries; missing evidence is allowed. False creates a withdrawn row immediately. No duplicate/history policy is enforced |
| WithdrawConsent — COMPLIANCE-API-014 | PATCH scoped consent, 404 absent/foreign; no body; sets Granted false and WithdrawnAtUtc now even if already false; returns entity | First/repeated/concurrent withdrawal, unchanged recorded time/evidence/subject, route isolation, full response and fresh row; UI cannot regrant/edit/delete |

Review-task date boundary: priority High if ExpiryDate ≤ UTC date now+30; Normal when expiry is null or later. DueAtUtc is produced by DateOnly.ToDateTime(midnight) without an explicit UTC conversion even though named Utc; specify timezone/readback checks rather than assuming local-to-UTC policy. Derived maximum title (Review plus 80 chars) fits L(240); description built from L(260) filename fits L(4000). Repeated requests and document status are not checked for task eligibility.

For all writes include cancellation, offline/timeout, 401-refresh, 403, 429, 500, malformed success body and committed-write/lost-response cases under API-RELIABILITY-002. Do not blindly retry a create after an ambiguous response. Global post-action audit can fail after committed data (BUG-API-0002 / API-RELIABILITY-001). Explicit apiHeaders(true) also retains the known refresh-header risk (BUG-API-0003 / AUTH-002).

## G01 — Page state / interaction checklist

| State | Source behavior and required assertions |
| --- | --- |
| Initial/empty/no academy | Forms and empty queues render while loading; academy set before lookups finish; no academy gives generic load error. Submit before academy simply returns. Verify no false empty-success state or early write during load |
| Lookup success/failure | Four lookups run in parallel, then JSON assigned sequentially without response.ok or array-shape checks. A rejected/invalid response can leave partial prior state; an error object assigned to documents/consents can break map rendering. Test each failed dependency, 401/403 JSON/text, null/wrong-shaped data, retry and old/new data mixtures |
| Form independence | Separate PersonPicker instances; changing one type resets only its ID. Document expiry/visibility also have component state. Filling consent must not change document draft and vice versa |
| Browser validation | Required only on type/name text fields; whitespace passes browser required but fails API. Custom person selector isn't required; date/ref/evidence optional. Test blank person payload separately from explicit null IDs |
| Pending/create success | No busy flag or disabled submit; repeated taps permitted. Both create handlers await network then use event.currentTarget.reset: existing async-reset risk BUG-FUNC-0001 can prevent feedback/reload after a committed write. Test the failure and the intended path separately; do not mark a write failed solely because reset throws |
| Reset/refresh feedback | Even once that reset defect is isolated, native reset does not explicitly reset React person/date/visibility state. Check visible values AND hidden FormData after reset/next render. Success notice is then cleared by load (BUG-FUNC-0003); forms are not closed/navigated away |
| Mutation failure | Non-OK gives generic text, not field-specific server string. Network rejection is uncaught by mutation handlers; draft remains, no coordinated busy/retry state. Both forms share one message slot; overlapping operations can overwrite each other's feedback |
| Review controls | Approve/Reject always render, no busy/confirmation/status eligibility check. UI sends only status; server uses today's UTC reviewedDate. Other review statuses and supplied date are API-only, not missing UI fields to invent |
| Withdrawal | Button only when granted; no confirmation/pending lock; non-granted displays Withdrawn even if directly created false. Success load clears notice. Repeated direct API calls remain possible even after button disappears |
| Expiry display | Local midnight minus Date.now, ceil(days); expired/≤30-days text overrides persisted status (including Rejected). Test yesterday/today/+30/+31 at day boundaries, timezone/DST and invalid stored date, keep review status distinguished from computed expiry in expected UI design |
| Accessibility/mobile | EnterprisePageState has status/alert and polite live region. Check durable announcements, placeholder-only field accessible names, repeated Approve/Reject row context, both independent selectors' labels, keyboard/focus, date/dropdown geometry and horizontal tables at 320/375/390/430,768,1280/1440 and 200% zoom, both themes (VISUAL-PAGE-021, UI-DROPDOWN-001, UI-TIME-001) |
| Explicit N/A | No document upload/download, create modal, cancel/editor, delete, consent regrant, or review-task button on this page. API-only actions are specified above; no fictional UI path |

## G03 — Effective permissions for every action

The following shared caller table applies separately to Documents, CreateReviewTask, AddDocument, ReviewDocument, Consents, AddConsent and WithdrawConsent (COMPLIANCE-API-008/009/010/011/012/013/014). Implemented behavior, not approved legal/business policy. Assume valid route/body and no infrastructure failure.

| Caller/context | Effective result across all seven actions |
| --- | --- |
| Anonymous/unresolvable user | 401 at academy filter; no action |
| Authenticated user IsActive=false, including platform flag | 403 at Program middleware before filter |
| Active IsPlatformOwner=true | Bypasses tenant/academy-active/module/catalog gates; action still scopes document/consent lookup by route academy. Add actions do not independently validate person/academy existence. Global filter mutation audit bypassed |
| Active same-tenant Owner/AcademyAdmin, academy active, AccessGovernance enabled | May reach all actions, then action validation/404 rules above |
| Same admin, missing/inactive academy, AccessGovernance absent/invalid JSON | 403; module checked before admin bypass. Finance or Core alone is not sufficient |
| Any ordinary user with foreign or unassigned AcademyId | 403 before module/role checks |
| Same-tenant Manager, Operations, Sales, Marketing, FrontDesk, FinanceUser, Teacher, Student, Guardian without admin role/flag | 403: ComplianceController has no catalog mapping; linked person does not override |
| PlatformOwner role name only, no flag/admin role | No bypass; ordinary tenant/module checks then 403 on missing catalog |
| Custom role or grant, even all catalog permissions | 403 without admin/flag; no mapping means denied BEFORE permission/grant evaluation. Permanent/unexpired/expired/revoked grants do not change it |

Multiple roles including Owner/AcademyAdmin take the administrator path after module/tenant checks. Visibility=StaffRestricted has no independent authorization effect; no staff disclosure claim is inferred because ordinary staff are currently denied. No action-local CanManage check adds another restriction.

Lookup dependency: /api/academies returns assigned academy only; tenantless platform owner cannot populate this page despite direct flag access. Students/Guardians list APIs are Core + students.manage for non-admins, so FrontDesk/delegated readers can pass those lookups yet still fail Compliance. Their lists include inactive rows, and this page drops the active flag. Test partial lookup authorization explicitly; don't weaken Compliance access to make UI load.

Denied calls must preserve document/consent/task rows. Early filter denial does not enter generic audit; action-returned 400/404 auditing needs full-pipeline verification because audit inspects response status before result execution. Allowed writes need separate business and audit persistence assertions; platform flag callers skip filter audit.

## Completion boundary

Final QA validation completed 2026-09-30: 8/8 coverage-helper tests and 25/25 artifact-consistency checks pass after expanding the abbreviated form reference to its full permanent ID. These are documentation/tooling checks, not executed application tests.

This slice now specifies both forms, all four helper declarations (including both PersonPicker instances), seven action contracts, conditional/N/A states, boundaries and effective permissions. BUG-DATA-0045 gains the UI wrong-type selection trace; no duplicate issue or runtime confirmation. Issue totals stay 107 OPEN, with 597 registered scenarios.

Next: teacher inline editing and batch assignment. Certificates, audit/export, expense/work-item permissions and final evidence reconciliation remain outside this completed slice. Phase 1 remains NOT CLOSED.
