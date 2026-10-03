# BUG-SEC-0010 — Marketing message eligibility ignores the recorded marketing opt-out

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED; bounded local repair PASS |
| Final verification | NOT RUN |
| Severity | Major consent-policy inconsistency |
| Priority | P1 |
| Category | SEC |
| Module | COMMUNICATION |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /communications; /communication-preferences |
| API | POST notifications |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMMUNICATION-MARKETING-001 |
| Evidence classification | Real Identity/HTTP/SQL baseline and retest; controller matrix; browser pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/NotificationsController.cs:49 |
| Class/function | Create template/category and consent checks |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures create an active Marketing Email template and a Student preference EmailAllowed=true, MarketingAllowed=false. Queue using that template with a synthetic connected/enabled channel; compare MarketingAllowed=true and channel-consent=false controls. |
| API response | Baseline201 Queued and200 Queue/Retry despite opt-out; final201 BlockedConsent at creation,400 no-write on requeue/retry |
| Database before/after | Full fresh SQL row/settings/audit snapshots; exact status/reason, scoped summaries, actor audits and foreign preservation |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted repair; no push/deployment |
| Retest result | BOUNDED PASS:813 backend (180 new),190 real HTTP/SQL cases |
| Regression result |128 creation combinations, committed opt-outs, unavailable-template retry, channel/marketing precedence; browser/races/original-category snapshot/future dispatch/critical pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## 2026-10-01 bounded repair checkpoint

[Repair report](../REPORTS/PHASE_2B_MARKETING_CONSENT_REPAIR.md):6 actual HTTP/SQL creation/queue/retry opt-out bypasses and40 controller baseline failures reproduced. Marketing-template Email/WhatsApp now requires both current scoped channel and marketing consent. Shared Create/Queue/Retry check; committed opt-outs reject retry without data/audit writes. Unavailable templates reject requeue; cancellation/InApp/independent Utility controls preserved.813 backend (180 new)/190 real Identity/global-filter/HTTP-SQL PASS, including128 creation combinations. Frontend/schema/permissions/normal assemblies unchanged; owned resources cleaned; no normal dev/Azure/outbound/commit/deploy. Browser, simultaneous races, immutable original category on template edits, manual-message classification and future dispatch-time revalidation remain OPEN. This is not legal compliance/provider-delivery certification. Original static evidence below is retained as history.

## Original reproduction

In isolated fixtures create an active Marketing Email template and a Student preference EmailAllowed=true, MarketingAllowed=false. Queue using that template with a synthetic connected/enabled channel; compare MarketingAllowed=true and channel-consent=false controls.

## Expected

Marketing-category messages require both the channel consent and marketing permission, or an explicit approved equivalent policy; a visible opt-out must not be silently ignored.

## Actual / evidence

Create loads template but does not use its Category. Eligibility reads only EmailAllowed or WhatsAppAllowed; MarketingAllowed is stored and displayed but never consulted by this path. With a connected fixture row it persists Queued despite marketing opt-out. Runtime NOT RUN; no external delivery is claimed.

Source snapshot:

```text
48:             var preference = request.RecipientId.HasValue ? await dbContext.CommunicationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId && x.RecipientId == request.RecipientId && x.RecipientType == request.RecipientType, cancellationToken) : null;
49:             var consent = channel == "Email" ? preference?.EmailAllowed == true : preference?.WhatsAppAllowed == true;
50:             if (!consent) { status = "BlockedConsent"; failureReason = $"No recorded {channel} consent for this recipient."; }
51:             else if (!await dbContext.CommunicationChannels.AnyAsync(x => x.AcademyId == academyId && x.Channel == channel && x.MessagesEnabled && x.HasSecureConnection, cancellationToken)) { status = "AwaitingConnection"; failureReason = $"{channel} is not securely connected for this academy."; }
```

## Suspected root cause

Category-specific consent is absent from queue eligibility.

## Business impact and blast radius

Marketing Email/WhatsApp template requests and future dispatch safety; distinct from fee-reminder bypass.

## Related / required regression

COMMUNICATION-MARKETING-001: Full HTTP/SQL category × channel consent × marketing consent × connection matrix, with all provider transport stubbed. Recheck consent at dispatch/retry after opt-out; never send real messages.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
