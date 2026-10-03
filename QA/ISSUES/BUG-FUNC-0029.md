# BUG-FUNC-0029 — Disabled templates remain selectable and accepted for notification creation

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED; bounded local repair PASS |
| Final verification | NOT RUN |
| Severity | Major template lifecycle inconsistency |
| Priority | P1 |
| Category | FUNC |
| Module | COMMUNICATION |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /communications; /message-templates |
| API | POST notifications with templateId |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMMUNICATION-TEMPLATE-STATE-001 |
| Evidence classification | Real Identity/HTTP/SQL baseline; controller/controlled TSX/HTTP-SQL retest; browser pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/NotificationsController.cs:30 |
| Class/function | Create template lookup / Communications template options |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create a synthetic template with Status Disabled and IsActive true using the current form, whose availability checkbox defaults true. Select it in Messages and queue for a controlled recipient; compare IsActive false and Approved controls. |
| API response | Baseline201 for Disabled/active; final400 unavailable, no notification/audit write; scoped full summaries checked |
| Database before/after | Fresh disposable SQL full data/audit snapshots; unrelated/foreign templates and notifications preserved |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted repair; no push/deployment |
| Retest result | BOUNDED PASS:633 backend (24 new),26 controlled TSX,42 real HTTP-SQL |
| Regression result | 50 existing channel checks PASS;22 prior compose payloads unchanged; browser/race/critical pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## 2026-10-01 bounded repair checkpoint

[Repair report](../REPORTS/PHASE_2B_TEMPLATE_STATE_REPAIR.md):2 real HTTP/SQL baseline defects,8 controller baseline failures and20 controlled TSX baseline failures (8 Disabled eligibility,12 unavailable-feedback). Disabled overrides availability in selector/selection/submit and fresh API lookup, including case/whitespace legacy contradictions. Draft/Approved usability and Draft starters are preserved; local Approved is not provider approval. Template editing/listing retains disabled rows. Committed disable before submit verified; simultaneous disable/queue and already-queued/dispatch/retry remain pending.633 backend/26 new controlled TSX/50 existing channel/42 real HTTP-SQL PASS; TypeScript and isolated builds PASS; page lint0 errors/1 inherited warning. Exact owned resources cleaned; dev DB/services/Azure/commit/deploy unchanged. Issue remains OPEN for browser/device/all-linked/critical/release acceptance. Original static evidence below is retained as history.

## Original reproduction

Create a synthetic template with Status Disabled and IsActive true using the current form, whose availability checkbox defaults true. Select it in Messages and queue for a controlled recipient; compare IsActive false and Approved controls.

## Expected

A disabled template is unavailable for ordinary message creation, or the product disallows contradictory Disabled/Available settings with an explicit validation error.

## Actual / evidence

Template Create/Update accept Disabled plus IsActive true independently. Message selector filters only isActive; notification lookup likewise checks only IsActive, not Status. Disabled and Draft templates are accepted, including active Draft starter templates. Runtime NOT RUN; actual provider approval/delivery is not established.

Source snapshot:

```text
29:         {
30:             var template = await dbContext.CommunicationTemplates.SingleOrDefaultAsync(x => x.Id == request.TemplateId && x.AcademyId == academyId && x.IsActive, cancellationToken);
31:             if (template is null) return BadRequest(new { message = "The selected template is unavailable." });
32:             channel = template.Channel; title ??= template.Name; body ??= template.Body;
```

## Suspected root cause

Two lifecycle fields have inconsistent meaning and only one is enforced by consumers.

## Business impact and blast radius

Disabled-template usage and draft review expectations; no external delivery claim.

## Related / required regression

COMMUNICATION-TEMPLATE-STATE-001: Browser/HTTP template Status × IsActive matrix including starter templates, concurrent disable before submit and legacy contradictory rows. Require deterministic documented Draft/Approved/Disabled policy; do not conflate local Approved with provider approval.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
