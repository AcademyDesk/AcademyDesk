# Batch 33 — Teacher compensation semantic review

2026-09-29. STATIC-SPECIFIED, runtime NOT RUN. Pinned commit/source fingerprint and existing student UI edits unchanged. This closes the named compensation specification gap, not Phase 1 or any application defect. No application code, HTTP/browser/SQL execution, database writes or deployment.

## Scope and evidence

Seven form declarations under TEACHER-FORM-002, the payment-teacher selector and TeacherCompensationController.Get/Save. No increase to the 649 declaration census. Existing scenarios: TEACHER-PAY-001, TEACHER-API-001 (GET), TEACHER-API-002 (PUT), VISUAL-PAGE-075, UI-DROPDOWN-001, UI-TIME-001, API-JSON-001, AUTH-002, API-RELIABILITY-001, GUARDIAN-PROFILE-001 and ACADEMY-RECOVERY-001. New proposed regression TEACHER-COMPENSATION-ZERO-001 links BUG-DATA-0050; it is not executed.

Source anchors (relative to repository root):

- apps/web/src/app/teacher-payments/page.tsx:9–29: state, initial lookups, selection, payload and response handling; :36–51: selector and conditional form.
- apps/api/Controllers/TeacherCompensationController.cs:12–34: Get/Save; :37–65: stored JSON reader, summary and request records.
- apps/api/Domain/Entities/Teacher.cs:18; apps/api/Data/AcademyDeskDbContext.cs:198; apps/api/Migrations/AcademyDeskDbContextModelSnapshot.cs:3069; migration 20260918160825_AddTeacherEmploymentPlanning.cs:25: nullable CompensationJson, nvarchar(max), no separately mapped rate/date columns.
- apps/api/Program.cs:18, :88–106; apps/api/Security/AcademyAccessFilter.cs:25–123; PermissionCatalog.cs:14–34 and SubscriptionPlanCatalog.cs:25–43: ordered access decisions.
- apps/web/src/lib/api.ts:65 onward: request/retry wrapper. The explicit apiHeaders(true) passed by Save can overwrite a refreshed token (existing BUG-API-0003 / AUTH-002).

## G02 — Exact field trace

All rate rows share decimal? DTO members. Blank client values and off-model values become null. The API first resolves the scoped teacher, checks exact model, checks its primary rate for null, then rejects ANY supplied negative rate, including an irrelevant off-model rate sent directly. Nonnegative off-model rates are accepted then discarded. All saved members are serialized in one replacement TeacherCompensationSummary JSON document; unknown previous JSON properties are not preserved.

| Declaration / UI rules | Outgoing property and conversion | DTO, server guard and persistence | Exact cases / scenario |
| --- | --- | --- | --- |
| payment-teacher, outside form; includes inactive rows; selected first active or first row | teacherId route UUID; academyId comes from first returned academy; no body ID | Both actions query matching AcademyId AND Id; absent/foreign teacher returns 404 after access gate; teacher.IsActive not checked | None/clear, first active/all inactive, local/foreign/missing ID, rapid A/B selection; TEACHER-API-001/002, GUARDIAN-PROFILE-001 |
| payment-model; Monthly or Hourly select; initial Monthly | model from state, no trim/case normalization | string Model; exact Monthly/Hourly only, otherwise 400; saved Model and selected model's rates | Each valid spelling; empty/null/omitted/unknown/lowercase/padded/invalid JSON type; malformed binding must not write. TEACHER-FORM-002, TEACHER-API-002 |
| monthlySalary; only Monthly, required number min0 step.01, no max | monthlySalary = Number(value) only if not Hourly and value truthy, otherwise null | decimal? MonthlySalary; required for Monthly, zero allowed; retained only for Monthly | Blank primary rejects; typed zero succeeds by source path; loaded zero becomes null and fails. Off-model positive discarded, negative rejected. TEACHER-COMPENSATION-ZERO-001 |
| standardHourlyRate; only Hourly, required number min0 step.01, no max | standardHourlyRate = Number(value) only if Hourly and truthy | decimal? StandardHourlyRate; required for Hourly, zero allowed; retained only for Hourly | Same primary-rate round-trip and boundaries as salary; Monthly ignores nonnegative direct values. TEACHER-COMPENSATION-ZERO-001 |
| beginnerHourlyRate; only Hourly, optional number min0 step.01, no max | beginnerHourlyRate = Number(value) only if Hourly and truthy | decimal? BeginnerHourlyRate; null/zero/nonnegative accepted; saved only for Hourly | Omitted/null/blank => null; loaded zero => null on resave, unlike typed zero; independent band values, no ordering guard. TEACHER-COMPENSATION-ZERO-001 |
| intermediateHourlyRate; same optional UI | intermediateHourlyRate uses same Hourly/truthy conversion | decimal? IntermediateHourlyRate; same independent nullable guard/storage | Same cases, including zero with other rates positive and standard rate required. TEACHER-COMPENSATION-ZERO-001 |
| advancedHourlyRate; same optional UI | advancedHourlyRate uses same Hourly/truthy conversion | decimal? AdvancedHourlyRate; same independent nullable guard/storage | Same cases; no beginner ≤ intermediate ≤ advanced rule in source. Do not invent one. TEACHER-COMPENSATION-ZERO-001 |
| effective-from; optional StandardDateField, no min/max prop | effectiveFrom string or null; selected local YYYY-MM-DD; GET strings sliced to ten characters | DateOnly? EffectiveFrom; no controller past/future/joining-date constraint; JSON only, no date column | Blank/null/omitted; leap/non-leap Feb29; past/today/future; DateOnly boundary strings 0001-01-01/9999-12-31 and invalid dates/year0/year10000 via raw HTTP; assert binding denial/no write for invalids. UI-TIME-001, TEACHER-API-002 |

### Numeric and date boundaries

For EACH rate, propose raw JSON -0.01, 0, 0.01, 1.005, decimal maximum 79228162514264337593543950335 and overflow 79228162514264337593543950336. There is no server two-decimal or business upper-limit guard. Decimal representation/binding overflow behavior must be recorded by the later HTTP suite; do not call these values tested. Browser step.01 restricts ordinary submission differently from the API. Compare comma locale input, exponent input, malformed/null/boolean/string JSON, very small fractions and browser Number precision above safe integer range; use raw JSON text for exact server bounds so JavaScript cannot round the fixture first. No rate is mapped as decimal(18,2) here.

Null optional rates are distinct from zero; there is no fallback calculation in this controller. EffectiveFrom does not schedule changes: Save immediately replaces the JSON even for future dates. No history/version token or concurrency guard is present on this path. History, zero-rate policy, rounding and band ordering are business decisions, not silently inferred fixes.

The date control's year OPTIONS are 1900 through currentYear+25 (2051 for 2026); month navigation is not bounded by that list. This is not an API date range. Stored dates outside the list, leap-day navigation, Clear/Today and timezone-neutral YYYY-MM-DD round-trip need separate UI checks.

### GET and persistence/readback

- Missing/blank/whitespace CompensationJson returns a summary with empty Model and all rates/date null; UI defaults empty Model to Monthly. Existing positive rates and date hydrate directly.
- Reader property matching ignores case. Monthly accepts baseMonthlySalary or monthlySalary, hourly accepts baseHourlyRate or standardHourlyRate; when both aliases exist, source JSON property order determines the first match, not alias order. Test legacy/current/both-order variants.
- Number reader accepts JSON numbers or parseable text; culture-dependent text/date parsing needs explicit culture fixtures. Invalid numeric/date members become null; Model is not vocabulary-validated by GET.
- Malformed JSON falls back on JsonException. Valid non-object roots reach EnumerateObject and can throw outside that catch: retain BUG-API-0004, API-JSON-001. Object/array/null/scalar and nested wrong-type members remain separate fixtures.
- PUT summary contains teacherId, model, five rates and effectiveFrom. Verify casing, null members, no unrelated teacher mutation, matching GET, and a fresh SQL read of the JSON. This is a proposed assertion chain, not observed SQL evidence.
- Replacement writes can overwrite concurrent edits; no protected historical rate schedule is implemented. Admin audit save happens after the controller's save in a separate step, so a later failure can leave committed compensation despite an error (BUG-API-0002). The platform-flag bypass skips that filter audit. Do not infer rollback from an error response.

## G01 — Source-specific state and interaction matrix

| State / action | Current source behavior | Required later assertions / existing IDs |
| --- | --- | --- |
| Initial loading | Message shown; academy list first, then teachers, then chosen teacher's compensation. No form until teacherId; teacherId is set before compensation finishes | Slow lookups, independent non-2xx/parse/transport failures; prevent saving default/stale data during load. TEACHER-FORM-002, GUARDIAN-PROFILE-001 |
| No academy / no teachers | No academy gives create-academy message; empty teacher rows clear loading text and leave only empty selector | Truthful distinct empty state; no PUT; all inactive chooses first row. VISUAL-PAGE-075 |
| Change/clear teacher | ID changes immediately, old fields remain; pending/error GET does not lock Save. Clear hides form but retains state; late responses unconditionally setForm | A/B reverse GET order, A→clear, select during save and failed B GET. Never save A values to B; retain BUG-DATA-0018. TEACHER-FORM-002 |
| Switch Monthly/Hourly | set only changes Model; hidden values stay in draft until save. A successful save replaces draft with summary and discards off-model rates | Switch twice before/after save; hidden negatives via direct API; required visible primary; blank optional bands accepted. TEACHER-PAY-001 |
| Submit invalid native field | Required/min/step constraints exist on rendered rates; custom selects/date have hidden mirrors | Blank/negative/step mismatch blocks ordinary browser submit; API bypass still enforces own rules. TEACHER-FORM-002 |
| Save pending | Button disabled/text Saving, but teacher selector, model and all editable inputs remain enabled; message is not cleared first | Rapid click/Enter, edits and selection mid-save, old success notice while new request is pending, response identity. TEACHER-FORM-002 |
| PUT rejection | Parsed non-OK resets saving, preserves draft, displays result.message or generic text | 400/401/403/404/429/500; structured validation bodies lacking message; retry same intended record. TEACHER-API-002, AUTH-002 |
| Transport failure | save has no try/finally; rejected academyApi prevents setSaving(false) | Busy state recovery and no false success; extends existing BUG-FUNC-0012 / ACADEMY-RECOVERY-001 pattern. Committed-but-lost-response must be read back before retry |
| Empty/malformed success response | JSON parse falls back null; result.effectiveFrom dereference then throws after saving is reset | 200 null/empty/malformed/wrong-shaped body must not be treated as confirmed save; retain TEACHER-FORM-002 response variants |
| Valid success | Replaces draft with response, stays on this screen, displays Teacher payment details saved; no load helper clears it immediately | Keep as positive feedback control, not ordinary success-notice-clearing BUG-FUNC-0003. Verify correct teacher, durable readable/announced notice, fresh read; selecting teacher later clears notice. TEACHER-FORM-002 |
| Cancel/modal/delete | No compensation modal, cancel/reset, delete, approval, upload or separate payment execution action exists | N/A here; selector/calendar overlays are shared controls and must still be tested |
| Layout/accessibility | Monthly and Hourly layouts, toolbar select and date overlay; message is a conditional p without explicit live-region role | VISUAL-PAGE-075, UI-DROPDOWN-001, UI-TIME-001: 320/375/390/430,768,1280/1440,200% zoom; both themes, keyboard/focus, accessible names, overlay anchoring at scroll/resize, long names/inactive label, no clipping |

## G03 — Effective GET/PUT access

This table specifies implemented behavior for BOTH TEACHER-API-001 and TEACHER-API-002; valid route/body and no infrastructure failure assumed. It does not approve the product policy or demonstrate runtime authorization.

| Caller/context | Get | Save | Controlling evidence |
| --- | --- | --- | --- |
| Anonymous or no resolvable user | 401 | 401, no compensation write | AcademyAccessFilter resolves user |
| Authenticated IsActive=false, including platform flag | 403 | 403 before action | Program middleware precedes filter |
| Active IsPlatformOwner=true | Bypass tenant/module/academy-active gate, then scoped teacher lookup | Same bypass, then teacher lookup and request validation | Flag, not role name; no filter mutation audit on bypass |
| Active Owner or AcademyAdmin, matching active academy, Finance enabled | 200 summary or 404 teacher absent | 200 valid save, 400 controller rejection, 404 teacher absent | Finance module checked before admin role; missing academy also fails filter |
| Same admin with Finance absent/malformed modules, or academy inactive/missing | 403 | 403, no compensation write | SubscriptionPlanCatalog maps TeacherCompensationController to Finance |
| Non-platform user with mismatched AcademyId, including admin | 403 | 403 | Tenant check before module/role path |
| Same-tenant FinanceUser, Manager, Operations, Sales, Marketing, FrontDesk, Teacher, Student or Guardian; no admin role/flag | 403 | 403 | RequiredFor(TeacherCompensationController) is null; linked teacher identity gives no exception |
| PlatformOwner role NAME only, flag=false and no admin role | 403 in own eligible tenant; tenant/module may deny earlier | Same | Role name alone not used by filter bypass |
| Custom role or temporary/permanent grant, even finance.manage/workforce.manage/all permissions | 403 absent admin/flag | 403 absent admin/flag | Null catalog entry denies before permissions/grants evaluated; grant expiry/revocation cannot change this denial |

Multi-role users with Owner/AcademyAdmin take the admin path after tenant/module checks. Flagged platform users still need an existing teacher in the ROUTE academy; changing teacherId to a different academy returns 404. Teacher.IsActive=false does not itself deny either action. No action-local owner/linked-user check exists, unlike some other controllers.

Lookup dependencies: /api/academies returns the logged-in user's assigned academy only, or an empty list. A tenantless platform owner therefore cannot load this page's choices despite direct API flag access. TeachersController.List is Core, also unmapped for non-admin roles, and returns inactive teachers. FinanceUser cannot load the teacher choices nor use compensation merely by possessing finance.manage. Disabled Finance can leave teacher choices loaded while compensation GET fails; the page must not confuse lookup success with permission to save.

For each denied request assert unchanged compensation and no action execution. Audit assertions must distinguish filter-generated records: early denial never enters ExecuteAndAudit; action-level 400/404 auditing depends on the filter's pre-result HTTP-status inspection and must be checked under API-RELIABILITY-001, not assumed correct from the returned ActionResult.

## Findings and bounded completion

New OPEN P1 static finding BUG-DATA-0050: loaded zero rates fail lossless resave. Existing stale-selection, stuck-busy, token-retry, invalid-JSON and post-save-audit risks are linked above without duplicating issue IDs. FinanceUser denial is documented implemented policy; this review does not authorize widening access.

G01/G02/G03 are STATIC-SPECIFIED for this slice: seven form fields + one selector + two actions. Runtime and visual outcomes remain NOT RUN; no application issue closed. The finite acceptance queue is in SEMANTIC_CHECKLIST.md. Next is compliance record/consent semantics, not another compensation pass.
