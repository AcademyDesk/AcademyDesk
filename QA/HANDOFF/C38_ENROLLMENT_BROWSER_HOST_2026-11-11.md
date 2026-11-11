# C38 protected enrollment browser host — 11 November 2026

OWNER:Cloud Codex,Sol High for authority/session/transaction boundaries.
STATUS:CLAIMED. BRANCH:codex/cloud-c38-enrollment-browser-host-20261111.
BASE:4c4382ff1cd5fc08e7ccd1c53af7146f29291fb2,clean published C37.
Desktop def038296da5c6b7d54f9d551e090c57daf535b0 imported with provenance at
63aa2b580332bd90a06e0f736e01b3761a0171e5. Desktop owns adjustment review/recovery.

Exact MAY MODIFY paths(7):
- apps/api/Controllers/PentaOnboardingController.EnrollmentBrowser.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentCreationWorkflow.Browser.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentBrowserSession.cs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentCreationWorkflow.cs(strict browser checks only)
- tests/AcademyDesk.Api.Tests/PentaEnrollmentBrowserSessionTests.cs(new)
- tests/AcademyDesk.Api.Tests/PentaEnrollmentBrowserAccessTests.cs(new)
- QA/HANDOFF/C38_ENROLLMENT_BROWSER_HOST_2026-11-11.md

DELIVER:Testing-only/default-disabled browser-readiness,explicit purpose-bound
conversation start,GET start recovery and strictly bound create draft/read/approve/
execute routes. Browser routes require both existing enrollment enablement and
Penta:EnrollmentBrowserContractsEnabled. Reuse existing enrollment host/domain/
audit/transaction services;new sessions bind scope/version/expiry/security stamp.
Quota/idempotency/start audit remain atomic;readiness never creates or reserves.
Use separate browser routes and a conditional strict path so existing legacy
routes/drafts/receipts retain their behavior. No cross-purpose legacy session
accepted by the browser contract. Create only;status/transfer UI follows later.

MUST NOT MODIFY:Desktop finance/adjustment/payment/invoice/PENTA shared workflow,
legacy controller actions/enrollment fields/domain services,SQL harness or native
fixtures/migrations/permissions/Identity/session policy/frontend/vendor/Mini,
root shared handoffs/tracker,original evidence/assertions. No main/Azure/
environment publication/replacement/deletion/public access,packages/paid compute,
model download/loading/inference/training/activation or workflow dispatch.

VALIDATE:real Identity/HTTP availability/role/scope/malformed/non-SQL refusals,
cryptographic purpose/tenant/actor/stamp/version/expiry/pending tamper controls,
legacy source preservation,combined API and SQL harness compile,release guard,
Desktop/native task matrix/source hashes. InMemory cannot prove SQL start/write/
quota/audit rollback/races;Desktop owns that positive native acceptance before
browser integration/production activation. Model-loading stop remains.
STOP:publish bounded source/results and exact remote SHA;no production qualification.
NEXT:Desktop native host SQL validation;Cloud next bounded enrollment review/
approval/execute/receipt browser using this contract. No frontend write is enabled
by this host packet;C37 remains read/preview only until its separate integration.
