# C13 portal announcement metadata — 7 November 2026

Cloud owner. Status: CLAIMED; implementation and validation pending.
Branch: `codex/cloud-c13-portal-metadata-20261107`.
Base: `b7c9f84871b68c92332832278632ff83761ec294`, clean checkout verified.
This builds on the published C12 compensation return, not yet assumed imported
by Desktop. Refresh exact source before integration.

Desktop tip inspected: `ad54028`; its current P0/private-media/payment/restore
packet reports all four modules complete. No active competing portal source
claim is present in that completed handoff. Preserve all of its raw evidence.

Exact claimed paths:

- `apps/api/Controllers/PortalController.cs` — announcement helpers/call only
- `tests/AcademyDesk.Api.Tests/PortalAnnouncementMetadataTests.cs`
- `QA/ISSUES/BUG-API-0004.md`
- `QA/HANDOFF/C13_PORTAL_METADATA_2026-11-07.md`

Reproduce malformed/non-object roots and wrong-kind announcement fields through
the actual authenticated announcement endpoint, then guard metadata reads.
Preserve valid audience/time filtering and tenant isolation; invalid metadata
must not broaden visibility. Keep private media, profile writes and all other
portal handlers unchanged. Reuse the C12 fixture pattern with unique InMemory
domain/Identity providers, disabled migrations/bootstrap/media/PENTA and real
Identity login. Do not contact the configured SQL host or load models.

Validate baseline versus repair, canonical notification-writer compatibility,
Student/Teacher/Admin/Guardian visibility, foreign/cancelled/personal exclusions,
row/audit preservation and the combined installed API suite. No new packages.
These are actual HTTP pipeline tests with synthetic persistence, not SQL or
physical-device acceptance. Existing issue/release closure requirements remain.

Publish bounded source/results on this feature branch. This report substitutes
for editing Desktop's actively maintained shared handoff files. Sol High for
visibility/security and subsequent Desktop SQL acceptance; Medium for import/docs.
No main merge, Azure/environment publication or model activation.
