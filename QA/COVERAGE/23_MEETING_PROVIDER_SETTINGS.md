# Batch 23 — Meeting links and communication-provider settings

2026-09-29. Source-backed specification only; runtime NOT RUN. Completes CommunicationSettingsController List/Save: COMMUNICATION-API-003 and COMMUNICATION-API-004. NotificationsController remains partial: connection/consent predicates are comparison dependencies, not completed notification lifecycle coverage.

SCHEDULE-FORM-005 has14 lexical controls. COMMUNICATION-FORM-002 has4 lexical declarations: one senderForm definition renders Email and WhatsApp variants; count once, test both. Five standalone/composed controls in MeetingProviderSettings plus three history filters add8. No separate native meeting-provider form exists.

## G02 — Meeting form and actual execution boundary

BUG-FUNC-0024: submit checks connected then only sets an informational message. It makes no request, creates no class/link, changes no batch, sends no invite, and stores no history. meetings state is [] without a setter or load path. Edit copies title/provider/date/time but has no selected record ID or save path. All provider-behavior assertions below are required future contract tests, not claims of existing implementation.

| Control | Current behavior and cases |
| --- | --- |
| provider | GoogleWorkspace/Zoom/Microsoft365. connected requires exact Meeting channel, matching provider and hasSecureConnection. It ignores status and messagesEnabled. Test false/true fixture flags, Disabled/NotConfigured state, unknown/case-variant provider and switching provider; a stale flag must not count as verification |
| title | Required native input, no trim/length/semantic check in submit. Test empty/spaces/Unicode/long title and escaped rendering |
| batch | Optional selector filters exact Online/Hybrid with no http(s)-prefixed meetingLink. Any http(s) prefix is treated as a link, not validated. Existing URL removes batch from this workflow; define edit/replace path. On change attendee IDs clear, organizer remains |
| teacher | Optional organizer selector, not limited by selected batch; selected teacher is not included in attendees and is not sent anywhere. Future contract must distinguish organizer account, assigned teacher and attendee |
| invite-student | Requires Active enrollment into eligible batch by source predicate; does not inspect Student.IsActive or enrollment dates. With no batch selected, options still include students from all eligible batches despite placeholder telling user to select batch first. Selecting adds unique ID then resets selector; removable chips use button type=button |
| guestEmail | Comma-split, trimmed, nonempty strings appended without format validation or deduplication. Missing student email silently drops that recipient; different learners sharing email count twice. Test duplicates/case/spaces/invalid/missing and intended guardian invite policy before enabling real delivery |
| date / start-time | Required date and default17:00 time; no UTC conversion/request exists. Academy.timeZone is typed but unused. Define timezone and DST ambiguity rules, past-date policy and midnight/month/year boundaries; do not infer an IST contract from other pages |
| duration |30/45/60/90/120 strings, default60; no end calculation or persistence. Test provider range and visible summary parity after implementation |
| recurrence | Once/Weekly/Custom. Non-Once exposes an uncontrolled input with no name/state/ref; its text is never consumed. Test recurrence day/end/count/timezone and change-to-Once clearing semantics |
| access | InviteesOnly/Organisation/Open; only summary labels change. No enforcement by any provider. Future tests must prove actual access policy, not merely posted labels |
| chat / recording | Disabled/HostsOnly/Enabled and Off/Auto/Host. Summary only; no provider enforcement or consent handling. Unsupported provider capabilities must not be advertised as applied |

Attendees summary counts computed email strings, not validated/deduplicated recipients or organizer. Title/date native validation can stop button submit, but cannot supply missing integration behavior. Loading failure message always suggests port5092 regardless of401/403/module denial/JSON/network reason; preserve diagnostic distinction without leaking sensitive responses.

Standalone created-from and created-to filter createdAt date substring inclusively, not meeting date. emailFilter performs case-insensitive substring but does not trim query. Reversed range, empty range and timezone crossing need cases. These filter/edit/open states are currently unreachable with real rows because history never loads; future synthetic component fixtures must be explicitly labeled. Open meeting link has no scheme guard at render; validate returned URLs before enabling history.

## G02 — Sender and meeting-provider fields

COMMUNICATION-FORM-002 has draft.provider, draft.senderName, conditional draft.senderAddress and draft.phoneNumber. Email providers GoogleWorkspace/Microsoft365/SMTP; WhatsApp MetaCloudApi/BSP. Display name optional; Email input type=email is not required; phone is plain optional text. Neither sender form exposes status, messagesEnabled, reference or reply-to. Fresh drafts remain NotConfigured/false after saving because no activation control exists. Existing status and message flag are preserved by load; do not confuse saved sender metadata with enabled delivery.

MeetingProviderSettings standalone fields are meeting-provider, meeting-status, organizerName, organizerEmail and reference. Status options NotConfigured/Configured(label Ready to connect)/Disabled. Provider-specific reference hints are ID/reference text, not credentials. Provider change keeps old organizer/reference/status, so retest stale incompatible reference. Unknown provider falls back to Google explanatory text; it is not server validation.

Meeting-provider component is a section, with a type=button save. Required/email input attributes do not automatically run form constraint validation here. API only checks organizer/reference nonblank for Configured, not email format. Test blank/malformed/whitespace organizer and reference under all statuses; optional fields must remain optional in NotConfigured/Disabled. No real secrets should be entered in references: summary API returns them to authorized admins.

BUG-FUNC-0025: only metadata persistence and HasSecureConnection readers/declaration exist in audited source; no secure connection callback, test, disconnect or token lifecycle was found. New entities default false and Save never sets that flag. UI promises a next step but offers no connect action. This is a feature-completeness finding, not a recommendation to flip the flag or store credentials in ordinary fields. Actual external integrations or deployed services outside this source were not inspected.

## API and database contract

Both List and Save enforce action IsOwner: same academy plus Owner or AcademyAdmin role. List orders tenant channels and projects metadata plus flags, not credentials. Save canonicalizes channel Email/WhatsApp/Meeting and status NotConfigured/Configured/Disabled case-insensitively, without trimming first; padded values reject. Provider must be nonblank, is trimmed but is not allowlisted per channel. Test unknown provider/cross-channel combinations and define supported providers explicitly.

Configured Email requires sender address; WhatsApp requires phone; Meeting requires organizer address and external reference. There is no API email/phone/URI format check. All optional strings normalize whitespace to null. MessagesEnabled becomes requested flag AND Configured; HasSecureConnection remains unchanged, including after provider/account changes or disabling. Future connection lifecycle must bind verification to exact provider/account and revoke/revalidate on changes rather than preserve a stale boolean.

EF limits: Channel30, Provider60, Status30, SenderName200, SenderAddress320, ReplyToAddress320, PhoneNumber30, ExternalAccountReference300. Unique academy/channel. Test missing/null/blank/whitespace, exact limits/+1, malformed JSON/boolean, wrong channel, same channel in another tenant, case canonicalization, concurrent first insert and concurrent replacement. API returns200 summary after upsert; no explicit version/concurrency token. UpdatedAtUtc changes on every save.

BUG-DATA-0040: save always sends replyToAddress:null; Email draft reconstruction hardcodes reference empty, discarding a supported API value on unchanged save. Meeting save similarly forces phone null/messagesEnabled false. Roundtrip all supported optional properties and distinguish deliberate per-channel normalization from silent hidden data loss. Do not solve by making optional fields mandatory.

BUG-DATA-0041: successful save reloads all channels and replaces all existing drafts, losing unsaved changes in other panels. Test each panel dirty/clean, no existing row, simultaneous saves/reversed reads and page navigation. Shared message state can attribute a result to the wrong panel.

Save adds CommunicationChannelSaved audit metadata (channel/provider/status/messagesEnabled) in the same context/save as the channel. Global filter then performs its own audit save (BUG-API-0002). Test both audit rows/actor semantics, first-save rollback and post-save audit failure separately. Never log tokens or actual credentials.

## G03 — Access, readiness and failure matrix

Global filter requires Engagement for CommunicationSettings and denies delegated permissions because this controller has no permission catalog mapping. Action-level same-tenant Owner/AcademyAdmin gate also applies; PlatformOwner global bypass does not bypass this action check. Treat cross-tenant platform-owner denial as observed contract pending policy, not automatically an exploit.

Meeting page needs students, teachers, batches, enrollments and communication-settings all successful. Scheduling/make-up grants alone do not provide these permissions; even AcademyAdmin without Engagement cannot use it. Define a narrow readiness read contract separately from privileged provider configuration. Test academy no rows, unauthorized, foreign tenant, suspended/module-disabled tenant, expired/revoked grant, malformed/non-array/null JSON and each lookup failure independently.

Notifications Create checks exact Email/WhatsApp recipient consent then channel MessagesEnabled+HasSecureConnection. Metadata save alone does not make external delivery available. No provider test message was sent; queue state is not delivery evidence. Existing BUG-SEC-0008 fee-reminder inconsistency is reused, not closed or duplicated. Remaining template/preference/announcement/status workflow is next batch.

Both sender save and meeting save lack busy/network catch. Buttons remain enabled once academy exists, including following dependency-load failure. Duplicate submit/concurrent writes/readback failure must preserve input and report saved/unknown outcomes honestly. BUG-FUNC-0003 extends: all positive channel messages are immediately cleared by load. Success must persist after refresh and must not falsely imply provider connection.

## G01 — Visual/accessibility states and future integration tests

Specify loading, empty, denied, partial dependency, unsaved, saving, failed save, saved metadata, unavailable connection, connected fixture, disabled/revoked and unsupported provider. Test320/375/390/430 mobile,768 tablet,1280/1440 desktop and200% zoom; long labels/emails/references must wrap without horizontal scroll or overlapping Save controls. Verify shared dropdown placement/layers, date year selection, time popup, page scroll, focus/Escape/outside close and keyboard/chip removal.

Sender controls rely on placeholders; confirm accessible names and field errors. Required meeting inputs outside a form need explicit validation feedback. Use per-panel durable live success/error messages, explain why Create is disabled and preserve a usable link to settings. Do not approve a Connected badge from arbitrary stored flag or a UI-only access/recording setting.

Future provider suite must use stub transport and synthetic accounts: tenant-bound callback/state, canceled/denied consent, secret storage/redaction, refresh expiry, revoked credentials, provider outage/rate limit, idempotent retry and partial remote-success/local-save failure. These are prerequisite specifications, not external protocol implementations or executed tests; consult current official provider docs when implementing. No real meeting, invite, connection test, OAuth grant or customer modification performed.

## Outcome

Four OPEN source findings: P1 BUG-FUNC-0024, BUG-FUNC-0025, BUG-DATA-0040; P2 BUG-DATA-0041. Existing save-feedback issue extended. Adds2 native forms/18 lexical controls,8 standalone controls and1 controller/2 actions. Cumulative65/82 forms,381 lexical+121 standalone=502/649 controls,50/70 complete controllers. Runtime NOT RUN; counts are not percentage completion.

Next: communication messages, templates and consent preferences, completing NotificationsController with its remaining status/announcement behavior. G01/G02/G03 remain open; Phase1 NOT CLOSED, Phase2 not started. No application repairs, commit or Azure deployment.
