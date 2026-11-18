# C54 — gated Mini conversation and read-only receipt recovery

MODEL: Sol High
TOOL: Cloud Codex
BASE: 400d0c6bdca302332df3b046dc93c67bd8410eb8 (C53 over Desktop4cbe56a7)
BRANCH: codex/cloud-c54-mini-conversation-ui-20261118
STATUS: CLAIMED — 18 November 2026 UTC

The owner assigned cloud the application/model integration. Latest Desktop4cbe56a7
and all published C50–53 claims are checked; no concurrent path overlap observed.
Cloud claims only:

- apps/api/Controllers/PentaChatController.cs — GET receipt route.
- apps/api/Intelligence/Penta/PentaMiniOrchestrator.cs — own/current receipt read and mandatory audit.
- apps/api/Intelligence/Penta/PentaMiniProtocol.cs — recovery receipt binding validator.
- tests/AcademyDesk.Api.Tests/PentaMiniRecoveryTests.cs — new recovery boundary regressions.
- apps/web/src/lib/penta-mini-conversation.ts — new scoped client/recovery state.
- apps/web/src/components/penta/mini-conversation.tsx — new private evaluation composer.
- apps/web/src/components/penta/role-penta-entry.tsx — default-off build gate only.
- QA/tools/penta-mini-conversation.test.cjs — new client tests.
- QA/tools/penta-mini-conversation-browser.cjs — actual Chromium fixture runner.
- QA/tools/penta-browser-harness.cjs — exact unmodified C52 helper return, reused.
- QA/HANDOFF/C54_MINI_CONVERSATION_UI_2026-11-18.md — this ownership/result/handoff.

No SQL schema/migration/domain/finance policy, production activation, model routing,
model loading or packages are claimed. Desktop retains protected native SQL/device/
model qualification. The cloud Docker daemon is usable but has no cached SQL image;
no image was downloaded or existing container/resource changed. Native SQL receipt
acceptance remains a stated prerequisite unless existing safe infrastructure appears.

Implement the existing read-only Mini host conversation contract. Recovery must be
GET-only, same authenticated actor/academy/session/request, current version and
unexpired read-only purpose, with mandatory audit and no planner/connector call.
Store only scoped opaque recovery IDs/version/expiry in sessionStorage, never raw
prompts, tokens or receipts. Unknown outcomes block another send until recovery or
an explicit separate conversation. No automatic POST replay or retries.

A NEXT_PUBLIC_PENTA_MINI_EVALUATION=true build enables the evaluation component;
it is absent by default and does not open backend gates. Existing Owner/Admin,
Development/Testing and Mini/pilot flags remain mandatory on the server. Existing
guided/Manual workflows remain available. No new tier selection, unqualified live
model claim or model-generated HTML. Do not represent numeric finance payloads as
new exact financial authority; use authorized Manual sources where exact terms
are not in the retained protocol.

Test client failure/foreign/malformed/session/storage/recovery boundaries, actual
browser start/clarification/selection/unavailability/lost response/reload, types,
lint/build and affected API tests. Build the default-off variant as well. Retain
failed runs and exact source bounds. No main/Azure/environment publication/public
access change, model/package download/loading/training/inference/activation.
Mini/Plus/Pro unqualified, Ultra deferred; six critical records and90% remain open.
