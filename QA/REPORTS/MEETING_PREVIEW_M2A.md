# M2a — internal typed meeting draft validator

2026-10-15. `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`.
Continues [meeting contract](MEETING_INTEGRATION_CONTRACT.md) after [M1 SQL proof](MEETING_READINESS_SQL.md). This is an internal foundation, NOT completed M2, an enabled preview API or an approval/execution feature.

## Implemented

- New `MeetingPreviewService` and typed request/result records under `apps/api/Infrastructure/Meetings`. No registration/controller route or frontend wiring: capabilities still reports every operation unavailable. No meeting/provider writes.
- Strict request JSON disallows unmapped fields, including model/client academy,role,approval,guest URL or SQL additions. Technical draft limits: existing three providers;nonempty batch/organizer IDs;nonblank title up to200characters without control characters;existing UI duration choices30/45/60/90/120;one to100unique nonempty student IDs. These are internal bounded preparation limits,not approved operational policies or quotas.
- Uses existing readiness service for current server Identity/Owner/Admin/tenant/academy/Engagement authorization,then current academy-scoped batch/teacher/student/enrollment queries. Rechecks authority before producing a draft. Active Online/Hybrid batch,no preexisting link,active assigned organizer andstudents,Active enrollment,date windows andvalid email addresses required. Missing recipients fail explicitly;no silently dropped students. Canonical email deduplication case-insensitive.
- Explicit academy timezone identity andoffset-bearing future minute-precision start validated. Zone mismatch,offset mismatch andDST gap rejected;explicit overlap offsets remain distinct instants. UTC start/end retained without laptop-zone conversion/rounding. Full-minute precision supported internally;UI clock unchanged.
- Only one-time,invitees-only,chat-disabled,recording-off preparation currently accepted. Other combinations explicitly rejected as unqualified,not silently downgraded. No guest/recurrence/provider restriction support claimed;policy/adapter qualification still pending.
- Canonical request fingerprint plus actor/academy/security-stamp/source fingerprint andfive-minute expiry. Source fingerprint covers relevant batch/organizer/student/enrollment/channel versions/values,not raw private notes or credentials. Draft always `CanExecute:false`, `ApprovalStatus:NotIssued`,reason `DraftOnlyIntegrationUnavailable`.

## Critical boundary

Fingerprints are plain integrity/change-detection digests,NOT signed authority orapproval tokens. No API consumes them,stores approval,checks replay ordispatches anything. This service cannot be used as execution authorization. Future preview receipt must be securely actor/tenant bound,stored/signed andreevaluated with current authority/source versions before execution. A source digest alone does not guarantee an atomic database snapshot under concurrent mutation. SQL isolation/concurrency andrequired audit/persistence remain M2 follow-ups. No durable outbox/operation/history storage orschema migration created yet.

## Verification

-45new focused tests PASS:strict limits/unknown fields,role denials,tenant-resource scope,inactive/misassigned/linked/offline sources,completed enrollment,missing email,date windows,full-minute Hybrid,changed input/source/channel invalidating digests,role revocation,DST gap andoverlap,non-executable/not-approved output andno domain/audit/notification writes.
- Final132/132combined PASS:45preview+30existing readiness+57communication permission/channel cases;zero skips/failures. Existing readiness Testing-host authentication cases retained. New preview cases use real Identity/EF InMemory fixture,NOT authenticated preview HTTP orSQL acceptance. Shared fixture visibility widened only;existing assertions unchanged.
- Final receipt `QA/EVIDENCE/meeting-preview-20261015/preview-final.trx`. Earlier intermediate130-test receipt retained,not counted asadditional scenarios. `dotnet build apps/api/AcademyDesk.Api.csproj --no-restore --verbosity quiet` PASS,zero warnings/errors in cached normal assets. Final test run recompiles the last fingerprint/date-window changes;no application-source edits afterward.
- This cached build does NOT resolve/override fresh isolated harness NU1902/NU1903 Pkcs9.0.11 advisories recorded inM1SQL report. Security dependency review/remediation remains a pre-Azure blocker. No dependency edits ornew restore here.
- No newSQL/browser/provider/device test this turn. PriorM1SQL21cases andmeeting-draft18/30visual evidence retained at their source/evidence level,not rerun for new internal service. No broader enterprise issue closed.

## Next

Successor2026-10-15:[M2bHTTP/SQL checkpoint](MEETING_DRAFT_HTTP_M2B.md) enables only audited non-executable `/draft-preview`;137selectedtests and41realHTTP/SQL cases PASS. Secure receipt/approval persistence remains pending. Earlier no-route descriptions are historical toM2a.

Sol High:M2b qualified protected draft-preview HTTP boundary/current authority/strict binding tests anddisposableSQL validation,while capability/create/provider dispatch remains unavailable. Then secure version-bound preview receipt/approval persistence anddurable operation/outbox design/migration/failure tests. M3secure provider qualification,M4Manual/PENTA wiring andremaining portal visuals/engine/device/E9–E10 gates pending. No genuine Mini contract change/project switch currently required.

No main/stage/commit/push/Azure/credentials/provider/container/production-data change;previous dirty work/evidence retained. Owner full PENTA+Manual app→authorized go-live→Hayansh→Academy Desk website order unchanged;90%AI remains a target,not measured completion.
