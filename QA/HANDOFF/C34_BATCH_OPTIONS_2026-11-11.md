# C34 least-privilege batch setup lookups — 11 November 2026

MODEL: Sol High for authority/read isolation;Medium for settled UI/docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH:codex/cloud-c34-batch-options-20261111.
BASE:6d508ee6180e39ad3e3c4e1d2f9a9db00426c42a,clean published C33.
DESKTOP:d5f91d344eb5f9706733907d79cbbe2a52685ff1 and
525f5ad365140a47eff61b40e511df9a66e3ec87 imported with provenance;Desktop owns
protected payment collection next. No overlap with batch setup lookup paths.
STATUS:CLAIMED;capture original actual HTTP/page failures before implementation.
TASK:batch/setup subset BUG-FUNC-0018;preserve existing batch mutation contracts.

Exact MAY MODIFY paths(10):

- apps/api/Controllers/BatchesController.cs — GET options/minimal DTOs only
- apps/web/src/app/batches/page.tsx — options consumer,valid read readiness/scope
- tests/AcademyDesk.Api.Tests/BatchOptionsAccessTests.cs
- QA/tools/batch-options.test.cjs
- QA/tools/batch-options-browser.cjs
- QA/tools/batch-preservation.test.cjs — valid loaded/scoped fixture only;all payload assertions retained
- QA/ISSUES/BUG-FUNC-0018.md
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md
- QA/HANDOFF/C34_BATCH_OPTIONS_2026-11-11.md

MUST NOT MODIFY:permission/filter/module/Identity/session policy,existing batch
mutation/domain/scheduling/teacher-inheritance/admission/SQL/migrations,finance/
PENTA host/new Desktop chat,other workflow pages,globals/vendor/Mini or prior
raw evidence. No main/Azure/environment publication/public access/model loading/
download/inference/training/activation. Mini/Plus/Pro unqualified;Ultra deferred.

Policy:GET batches/options inherits existing batches.manage and current global
academy/tenant/actor gates. Return only courses(id,name,academyType),teachers
(id,firstName,lastName),branches(id,name),scoped/sorted/no-tracking/no-store.
Historical inactive labels remain;lookup availability never authorizes targets
or adds full management access. Core batch workflow needs these minimal labels
without unrelated academics/workforce/MultiBranch management calls;existing
server membership/eligibility and module policy are unchanged. Client consumes
options+batch register without fallback,rejects malformed/partial/duplicate ids,
invalidates read readiness while loading/failed/changing academy,and ignores
obsolete/unmounted read results. Failed reads do not render authoritative zero
metrics/empty directory or enable direct writes. Preserve payloads/hidden fields,
selectors,drafts and existing mutation outcomes. Broader uncertain-save recovery
and remaining0018 schedule/attendance/promotions/makeup/events remain separate.

VALIDATE:original/final actual HTTP/Identity/InMemory Operations/Manager/Owner/
Admin/custom/direct-grant/revoked/expired/foreign/inactive/maintenance/empty/minimal/
Core-only/no-write and full management denial. Actual page read/race/draft tests,
retained16 batch preservation controls,native sandboxed Chromium at320/1440 for
/batches and /batch-setup with synthetic delegated API,exact-lock types/build/
lint/full frontend/API/release guard. Preserve genuine SQL-receipt failures.
STOP WHEN:source-bound handoff and feature publication verified;no issue closure
or production/native SQL/device qualification inferred.
NEXT ROUTE:Desktop Sol High for native SQL/Identity/browser/device acceptance;
Cloud next independently claimed non-AI scope. Model-loading stop remains.

Claim extension before fixture edits: add QA/tools/batch-feedback.test.cjs and
QA/tools/batch-times.test.cjs (12 paths total). The first full run found their
preloaded fixtures omit the new successful-read scope and old refresh routes.
Seed valid loaded state and scoped options,retain all existing payload/feedback/
pending/no-write assertions;update exact refresh request expectations to the two
new endpoints. No-academy tests must use the real context-change handler and a
retained callback so the new unavailable-directory guard is exercised. Original
failures remain private. No implementation weakening to satisfy old fixtures.
