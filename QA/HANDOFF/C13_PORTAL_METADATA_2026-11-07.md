# C13 portal announcement metadata — 7 November 2026

Cloud owner. Status: IMPLEMENTED / CLOUD VALIDATED; local acceptance pending.
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

## Verified return

Implementation `9edbf60a749c798560a1b1c0405a60af26b6d51a`; claim `e8d5424`.
Latest inspected Desktop tip `b474042` has imported C12. Its compensation source
and tests match this branch byte-exact. Import only this packet's four paths;
preserve Desktop's newer P0 evidence/shared handoffs. No private-media handler
changed: the Portal diff is its announcement predicate and two private helpers.

Original metadata caused HTTP500 for non-object roots and wrong-kind fields.
Invalid present start dates were ignored and could expose the announcement.
The repair validates root and string kinds before property reads, rejects
invalid present start dates and parses each candidate once. Canonical writers,
legacy missing audience/start fields and case-insensitive audience lists retain
their existing behavior. Invalid records are hidden, not rewritten.

| Executed run | Pass | Fail | Skip |
| --- | ---: | ---: | ---: |
| Unchanged Portal baseline | 9 | 10 | 0 |
| Combined C12/C13 target | 55 | 0 | 0 |
| Full API regression | 2,046 | 0 | 0 |

The 19 new test groups execute 160 authenticated announcement GET scenarios:
30 absent/empty/malformed/non-object roots;56 wrong-kind field cases;70 valid
audience/window cases across Student,Teacher,Admin,Owner,Guardian;three actual
notification-writer compatibility reads;one account-without-academy denial.
Each fixture also verifies anonymous401 and performs a real Identity login.
The writer tests call the actual controller directly against owned InMemory
persistence; reads use the full authenticated HTTP pipeline. Matrix reads
compare all notification bytes, audits and read receipts before/after, and
exclude foreign, cancelled and personally addressed rows while retaining an
unrelated valid announcement. No production/provider send or SQL connection.

Commands used installed SDK10.0.403/cache, with no restore/download:

```sh
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~PortalAnnouncementMetadataTests
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~PortalAnnouncementMetadataTests|FullyQualifiedName~TeacherCompensationMetadataTests'
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --no-build
git diff --check
node QA/tools/check-production-gates.cjs
```

Actual executable `/workspace/.tools/dotnet/dotnet`; each test command also used
`--logger 'trx;LogFileName=<run>.trx' --results-directory /workspace/c13-portal-metadata-20261107`.
Original source, logs, TRX and source/assembly hashes remain in that excluded
artifact directory. Baseline9/10 failures were retained; no assertion weakened.
Final target rebuilt the tested assemblies; full regression reused them without
source changes. Diff check passes. Production guard still exits1 on five P0s.

## Remaining acceptance and next owner

Both named `BUG-API-0004` readers now have bounded repairs. Keep the issue OPEN:
Desktop must import/review this return, perform owned SQL metadata byte/readback
and authority checks for both readers, and complete linked browser/critical/
device acceptance. Its completed P0 media/restore proofs stay source-bound;
they do not qualify this new Portal binary automatically. No frontend change
or rebuild. Exact CI/images, staging and production recovery, other critical
workflows and Mini/Plus/Pro qualification remain open; Ultra deferred and model
loading stopped. No workflow dispatch; feature push alone does not trigger
`verify.yml`. Sol High next for protected local acceptance, Medium for import.
