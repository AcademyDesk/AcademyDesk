# C39 protected enrollment review,approval and recovery UI — 12 November 2026

OWNER:Cloud Codex,Sol High for binding/recovery;Medium for settled UI/docs.
STATUS:CLAIMED. BRANCH:codex/cloud-c39-enrollment-approval-ui-20261112.
BASE:e8385f58b23de757928f99481c33ae3dd582b851,clean published C38.
Desktop af4bc7637806de8c5b2834f818beb7fdd10c7245 adjustment browser return
imported with provenance. Desktop owns Manual exact adjustment amount/currency
and C38 native SQL acceptance. These paths remain separate.

Exact MAY MODIFY paths(11):
- apps/web/src/components/penta/enrollment-creation.tsx(new)
- apps/web/src/lib/penta-enrollment-creation.ts(new)
- apps/web/src/components/penta/enrollment-assistant.tsx(bound transport and mount)
- apps/web/src/components/penta/enrollment-assistant.module.css(scoped controls)
- QA/tools/penta-enrollment-assistant.test.cjs(fixtures/hooks only;retain assertions)
- QA/tools/penta-enrollment-assistant-browser.cjs(readiness404 fixture only)
- QA/tools/penta-enrollment-creation.test.cjs(new)
- QA/tools/penta-enrollment-creation-browser.cjs(new)
- apps/api/Intelligence/Penta/PentaEnrollmentCreationWorkflow.Browser.cs(browser DTO UTC serialization only)
- tests/AcademyDesk.Api.Tests/PentaEnrollmentBrowserViewTests.cs(new)
- QA/HANDOFF/C39_ENROLLMENT_APPROVAL_UI_2026-11-12.md

DELIVER:verified host readiness;exact create review→separate explicit approval→
explicit execution→verified receipt;pre-write scoped recovery pointer;GET-only
reload/recovery;no automatic replay/new request after uncertainty;frozen bindings,
blocking overlaps and session-loss/unmount cleanup. Only C38 browser routes.
PreviewOnly remains advisory. Feature remains default-disabled/Testing-only.
Native SQL acceptance is still Desktop-owned/pending;no production activation.

Browser DTO boundary correction:SQL DateTime reloads are Unspecified. C38 draft
views currently pass expiry through without marking UTC(unlike Finance views).
Normalize only new browser DTO expiry/receipt time before JSON;retain UTC ticks,
status/hash/approval/domain fields and all legacy responses. Add explicit tests.

MUST NOT MODIFY:Desktop finance/Manual adjustment/shared draft or SQL harness,
C38 start/session/authority/domain/transaction rules,legacy enrollment controller/
workflow/receipts,permissions/Identity/session policy,root shared handoffs/tracker,
other pages/vendor/Mini,original assertions/evidence. No main/Azure/environment
publish/replace/delete/public access,model download/loading/inference/training/
activation,new dependencies/paid compute or hosted workflow dispatch.

CHECKS:actual protocol/machine/component faults and storage/session binding,
strict schemas and immutable admission/hash/approval/receipt matching,loss before/
after write and GET-only reload,storage failure,malformed/expiry/refusal/pending/
stale/unmount;real Chromium320/1440 synthetic host;retained47 page/93 entry/32
preview checks;UTC JSON regression;full API/frontend/exact-lock web/build/types/
lint/SQL harness compile/release guard;exact Desktop preservation and feature SHA.
STOP:bounded source/results and native handoff pushed/remote-verified.
NEXT:Desktop real SQL/Identity/browser acceptance for C38+C39;Cloud enrollment
lifecycle/status/transfer in a separately claimed packet. Model-loading stop stays.
