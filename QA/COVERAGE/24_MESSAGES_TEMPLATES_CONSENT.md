# Batch 24 — Messages, templates and communication consent

2026-09-29. Source specification; runtime NOT RUN. Completes NotificationsController (List/Create/UpdateStatus), CommunicationTemplatesController (List/Create/StarterTemplateCatalogue/AddStarterTemplates/Update), and CommunicationPreferencesController (List/Save):10 actions. Reuse COMMUNICATION-API-001/002 and005–012. Prior partial Notifications List/Create comparison in batch08 is incorporated, not counted as another controller. PortalController and TeacherPortal comparisons reuse completed reviews.

COMMUNICATION-FORM-001 preferences has6 lexical controls; COMMUNICATION-FORM-003 messages13; COMMUNICATION-FORM-004 custom templates11. One standalone starterTemplateSelected checkbox repeats per catalogue row but counts once. Three forms/30 lexical+1 standalone added.

## G02 — Message composer

| Field | Source behavior and cases |
| --- | --- |
| recipientType | Guardian(label Parent), Student, Teacher, Academy banner. Change resets recipientId only, retaining template, variables and content. API does not allowlist type or require matching same-tenant recipient existence; test unknown/padded/case/null type, mismatched/foreign/missing/empty ID and Academy with ID. No unauthorized read is proven merely by storing a foreign ID |
| recipientId | Required by handler/button for non-banner; selector itself lacks required. Separate student/guardian/teacher lookups. No selector filtering by active/contact/consent state. Test inactive and absent address, duplicate names and selection changes during save |
| templateId | Lists all IsActive templates, including Disabled/Draft. Choosing sets channel, title to template.name, body, and blank variables. Clearing template leaves previous content/channel/variables intact. Server overrides request channel with template channel; missing/foreign/inactive template rejects |
| variables[name] | Required repeated controls derived only from original selected template.body regex alphanumeric/underscore double braces. Editing body to add another variable does not update controls. Title/subject variables are not expanded by API. Test duplicate/missing/extra/empty/null variable values, special characters, unknown placeholders and escaped display |
| title / body | Required native fields; API trims and rejects whitespace before substitution. With template, only null title/body falls back, not empty string. Fallback title uses template Name, not Email Subject. Substitution occurs only on body; missing variables remain raw placeholders, empty substitutions can erase substantive text after validation |
| channel | InApp/Email/WhatsApp. Server defaults blank to InApp and trims but does not allowlist/canonicalize. Consent branch uses exact Email/WhatsApp; lowercase/unknown channel skips it. Test canonical/case/space/unknown separately, and define normalized rejection instead of assuming Queued means deliverable |
| scheduledDate / scheduledTime | Optional pair; both needed for conversion, so date-only/time-only silently sends null. Conversion uses browser-local Date, no timezone label. Test partial pair, invalid/DST/midnight, past date and explicit offset policy; no dispatch implementation inferred |
| announcementAudience | Student/Teacher/Student,Teacher; sent as variables.audiences. API has no allowlist, accepts arbitrary variable metadata. Portal recognizes audience tokens and Both; missing audiences is broad. Test blank/unknown/mixed case and intentional guardian policy (existing BUG-SEC-0009) |
| displayHours | Required number1–168, default4. API bounds only when explicit RecipientType equals Academy ignoring case and IsImportant true; null defaults1. Test0/1/168/169/fraction/invalid JSON. Blank recipient type defaults to Academy later but skips important metadata branch |
| announcementStartDate / announcementStartTime | Date optional, time09:00. No date means immediate regardless of chosen time. API computes startsAtUtc/expiry variables for important Academy requests. Test future, exact start/expiry boundary, past start and timezone conversion |

BUG-FUNC-0026: selected external template survives switch to banner. UI posts InApp but hidden template overrides it server-side to Email/WhatsApp, typically BlockedConsent because banner has no recipient. UI still claims banner will run; portal banner query does not filter channel. Direct channel edits with a retained template are similarly ineffective. Verify mode switching, actual request, returned channel/status and downstream display.

Template Subject, provider template name and language are not used by notification materialization. Treat them as unsupported downstream capabilities until a defined contract exists; do not claim email subject/approved provider template rendering from stored metadata alone. Keep submitted variable data out of unsanitized logs.

## Notification API, persistence and lifecycle

List filters academy and optional recipientId, not recipientType; newest CreatedAtUtc first, unpaginated. Empty/foreign filter yields empty rows rather than parent existence validation. Create requires academy existence plus nonblank title/body, but recipient IDs/types are not validated against entity records. Test correct recipient controls and no orphan/misaddressed rows in rejected cases once contract is defined.

Template lookup scopes tenant and IsActive. BUG-FUNC-0029: neither selector nor API checks Disabled Status; contradictory Disabled+IsActive=true is accepted by template save and can be used. Starter templates are Draft and default active. Define Draft review availability explicitly; local Approved status is not external-provider approval evidence.

Canonical Email/WhatsApp requests look for exact recipient preference and channel consent, otherwise BlockedConsent. With consent but no enabled secure channel, AwaitingConnection. Other channels default Queued. Teacher UI supports external channels, but preferences API only supports Student/Guardian, so teacher consent cannot be managed through this workflow. Define teacher/broadcast support or disable unsupported choices.

BUG-SEC-0010: MarketingAllowed is never consulted, even with selected template Category Marketing. Test channel consent versus marketing consent independently. Existing BUG-SEC-0008 fee-reminder path still bypasses eligibility; do not close it because one creation path checks consent. Parent versus Guardian aliases and padded/case variants require normalization tests; SQL collation can affect equality, so distinguish C# exact channel branching from SQL recipient matching.

Status PATCH accepts Queued/Cancelled/RetryRequested case-insensitively and stores original casing; only exact RetryRequested clears FailureReason. It validates neither current state nor consent/connection when moving blocked/cancelled rows back to queue. No retry count increment, due-time processing, delivery receipt, dispatcher or provider send was found in audited source. Queue state and retry request are not delivery proof; test eventual worker revalidation as a prerequisite, never fake successful delivery by changing database flags.

Create returns201 summary and Location without GET-by-ID. Notification and NotificationQueued audit share one SaveChanges; global audit adds separate persistence risk BUG-API-0002. EF limits recipientType40/title250/message4000/channel30/status30/variablesJson8000/failureReason1000; nonunique academy/recipient/date index. Test boundaries after substitution/JSON serialization, Unicode, null/wrong-shape body and concurrent/replayed create; no idempotency key exists.

BUG-FUNC-0027: recipient inbox scopes academy/person/type but not ScheduledAtUtc, channel or status. Future/cancelled/blocked rows appear immediately. Learner bell counts every non-Read row, shows first8 but marks all fetched unread rows (up to100) as Read, and does not check resolved PATCH failures before locally marking read. API unconditionally replaces delivery Status with Read. Test not-due/cancelled/blocked cases, unread counts, records beyond first8, partial read errors and separate delivery/read-state expectations.

Announcement reader excludes exact Cancelled and takes newest10 before testing active dates/audiences. New expired/future/other-audience rows can starve an older eligible banner. It tests VariablesJson start/expiry, not channel/ScheduledAtUtc/status eligibility. Reuse prior JSON shape and guardian audience issues; test cancellation casing, forged reserved variables, invalid timestamps, missing audience, and retained template mode bug. No real announcement was published.

## G02 — Contact preferences

recipientType selector supports Guardian(label Parent)/Student and clears recipientId. recipientId lists all loaded people; selecting a saved preference hydrates emailAllowed, whatsAppAllowed, marketingAllowed and notes via effect. Three checkboxes default false; notes optional. Handler sends all flags and notes.

BUG-DATA-0018 extends with a concrete no-row identity case: select contact A with no preference, check consent/type notes without saving, then select B also without preference. selected is undefined for both, so effect dependency [selected] does not change and A draft remains under B ID. Repeat cross-type switch, clear selection and fast clicks. Expected recipient-bound draft/defaults must not silently create B consent from A input. This is an extension of wrong-record state ownership, not a duplicate issue ID.

Save canonicalizes Student/Guardian ignoring case but not spaces, checks same-tenant entity existence, upserts unique academy/type/recipient. Flags are nonnullable booleans: omitted values bind false, not preserve. Optional notes trim/null and EF max1000; test empty/clear/boundaries. Disabled/missing-contact policy needs explicit contract. List tenant-filtered, optional recipientType filter unnormalized, ordered UpdatedAtUtc or CreatedAtUtc.

Email/WhatsApp opt-in times retain original while allowed, clear on withdrawal, regenerate on later opt-in. OptedOutAtUtc is set whenever both channel flags false, even first save and marketing true; repeated opt-out changes time. No separate marketing timestamp, evidence source or immutable consent history exists; generic audit does not replace explicit proof. Specify desired history and partial-channel opt-out semantics without declaring legal compliance.

BUG-FUNC-0028: action CanManage includes Manager, but global filter rejects unmapped CommunicationPreferencesController for non-admin. Manager also lacks students/guardians lookup permission. Full MVC permission test, not direct controller invocation, is required. Do not broadly grant unrelated privileges as a workaround.

Preference save has no busy/catch, reload clears its success (BUG-FUNC-0003); saves can race with contact switching or typing during reload. Test failed POST versus committed save/failed GET and preserve unsaved drafts. No contact consent was changed during this audit.

## G02 — Custom and starter templates

| Field | Validation and mapping |
| --- | --- |
| channel | Email/WhatsApp canonicalized ignoring case, no trim before allowlist |
| templateGroup | UI Finance/Admissions/Classes/Academic/Academy updates/General; arbitrary API text, trimmed default General, EF60 |
| name | Required trimmed nonblank, EF200 |
| templateKey | Required trimmed nonblank then lowercase invariant, EF100; unique academy/channel/key, not language. Test duplicates/case/concurrent creates, same key other channel/tenant and desired multilingual identity |
| category | Utility/Marketing/Authentication/Transactional canonicalized; Marketing downstream consent gap above |
| status | Draft/Approved/Disabled; independent IsActive creates contradictory states, see BUG-FUNC-0029 |
| language | Optional, blank defaults en, trimmed EF20; no locale allowlist |
| providerTemplateName | Optional trim/null EF200; saving does not register/approve provider template |
| subject | Optional trim/null EF250 even Email, not consumed by current notification composer |
| body | Required trim nonblank EF4000; placeholder syntax not validated by save; test multiline/Unicode/markup/unresolved variable and expansion boundaries |
| isActive | Checkbox default true; direct omitted boolean false; distinguish availability from lifecycle status |

Create adds explicit audit and returns201 summary; Update is full replacement, tenant-scoped, validates before lookup, returns200 and sets UpdatedAtUtc. No dedicated UI edit/status/deactivate action on saved library; only POST create form and display. Confirm an intended maintenance path rather than assume selecting a saved item edits it. No API DELETE or GET-by-ID exists.

Starter catalogue has16 definitions (8 Email/8 WhatsApp). standalone starterTemplateSelected checkbox and group toggle maintain selected IDs across channel tabs; global Add selected count can include hidden tab choices. Test add/clear one/all groups, switching tabs, keyboard, mixed tab selection and duplicate existing entries. AddStarterTemplates rejects null/empty/all-invalid list; deduplicates case-insensitively, ignores unknown IDs if at least one valid ID remains. Existing channel/key is skipped, including disabled/custom records, not overwritten. Adds Draft/en/default-active rows and one count audit; returns200/0 for all-existing. Unique index protects duplicates but concurrent prechecks can still cause batch failure.

Template create and starter-add notices survive load, unlike message/consent notices; preserve as positive controls. Handlers have no saving/catch. Starter-add dereferences result.message after parse fallback null even on malformed successful response; message create similarly uses result.status. Test malformed/null/HTML200 without claiming ordinary valid API responses are null. Readback failure after save must not imply safe retry. Browser support for Object.groupBy remains a compatibility test against supported real devices, not a verified baseline.

## G03 — Security, failure and performance prerequisites

All three controller families use Engagement; none has named PermissionCatalog entry. Global filter allows admin/owner tenant paths, denies delegated communications.manage despite catalog name existing. Template action IsOwner also requires same-tenant Owner/AcademyAdmin, so PlatformOwner global bypass is not sufficient there; Notifications has no equivalent action-level check. Validate intended owner boundaries rather than weaken guards.

For each API run anonymous/foreign/inactive/suspended/module-off/custom-expired/revoked cases and a valid allowed fixture. Verify academy/person/template ownership and no unauthorized reads/writes/audits. Stored messages render text in current React views; future HTML email must have its own safe rendering boundary. Use synthetic recipients, no real email/WhatsApp/provider traffic.

UI load depends on all lookups succeeding; message compose also loads teachers and templates for manual InApp messages. Verify each403/500/transport/malformed/null/empty separately. Save controls can enable after academy loads even if remaining data load fails. Test duplicate taps, request timeouts, expired-token retry BUG-API-0003, audit failure, navigation during save and late response state replacement. Retain user input, durable success and distinction among rejected/saved/unknown. No app runtime performance threshold invented; list pagination/history volume remains baseline-required.

## G01 — Visual and interaction states

Test all three pages at320/375/390/430,768,1280/1440 and200% zoom. Include long names/keys/emails, many dynamic variables, multiline template previews, grouped starter catalogue and large log. Verify page scrolling, sticky/header overlays, dropdown anchoring, date year selection, time popup, keyboard/focus order and touch-target separation.

Compose mode changes must not preserve invisible routing choices, start dates must state timezone, template selection must surface availability/approval clearly, and blocked/pending statuses must not look like sent success. Template tabs/group checkboxes need accessible selection/count including hidden selections; saved library needs clear status versus active state. Consent labels must tie to the selected person, reset correctly, and announce success/errors in role=status. Confirm keyboard toggle and no accidental consent from stale drafts. No screenshots approved or physical Android/iOS checks performed.

## Outcome

Five new OPEN P1 source findings: BUG-SEC-0010, BUG-FUNC-0026, BUG-FUNC-0027, BUG-FUNC-0028 and BUG-FUNC-0029. Existing BUG-DATA-0018 and BUG-FUNC-0003 extended. Three controllers/10 actions,3 forms/30 lexical controls and1 standalone declaration added. Cumulative68/82 forms,411 lexical+122 standalone=533/649 controls,53/70 complete controllers. Notifications is no longer partial. Runtime NOT RUN; these are source counters, not completion percentage.

Next: assignments, submissions and lesson planning; then remaining learning resources/music/practice, documents and operations/residual forms/shared controls. G01/G02/G03 remain open, Phase1 NOT CLOSED, Phase2 not started. No repairs, customer messages/consent writes, commit or Azure deployment.
