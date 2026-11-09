# C27 Sales trial access and lead lifecycle integrity — 9 November 2026

Cloud owner. SOURCE FIXED / source pin pending; SQL/device/release acceptance remains open.
Branch `codex/cloud-c27-sales-trial-integrity-20261109`.
Clean base `abb57f0063398dc71715a116f569b1656d88b563` (complete C26).
Desktop e460dbc86d5e02c4cdf7ede6c330ab74f089c65d is inherited exactly.
Desktop 7341fbba0c656805b2ce7b765643f9e721c67465 and
26fb7d2b9e8338fae0ede2ec99c8f0adb9ea752c were subsequently imported
with cherry-pick provenance; their14 combined changed paths remain byte-exact.
No competing Leads/SalesMarketing/trial-page claim found in current notes;
Desktop retains protected enrollment/host/SQL/migration ownership.

MODEL: Sol High for domain/state/transaction/authority; Medium for settled docs.
TASK: resolve DATA-0026/0027 and FUNC-0013 source defects: direct stage selection
must not manufacture conversion; trials must not regress converted leads;
Sales-authorized booking must not need teacher-management access. Preserve
legitimate unconverted/Won/Lost trials and existing optional teacher/batch policy.

Exact mutation paths:

- `apps/api/Controllers/LeadsController.cs` — stage/conversion lead boundary only
- `apps/api/Controllers/SalesMarketingController.cs` — minimal teacher options, trial creation/status audit boundary
- `apps/api/Infrastructure/LeadMutationBoundary.cs` — scoped SQL lead lock under existing atomic/audit transaction
- `tests/AcademyDesk.Api.Tests/SalesTrialIntegrityTests.cs`
- `apps/web/src/app/trial-bookings/page.tsx`
- `apps/web/src/app/leads/page.tsx` — remove invalid Converted stage choice; preserve actual Convert action
- `apps/web/src/lib/trial-booking-clock.ts` — explicit IST wall time consistent with existing displayed IST
- `QA/tools/sales-trial.test.cjs`
- `QA/tools/sales-trial-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — execute actual clock helper for controlled tests
- `QA/ISSUES/BUG-DATA-0026.md`
- `QA/ISSUES/BUG-DATA-0027.md`
- `QA/ISSUES/BUG-FUNC-0013.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C27_SALES_TRIAL_INTEGRITY_2026-11-09.md`

Reuse the existing AtomicAcademyMutation boundary for stage/convert/trial writes,
including Platform Owner. SQL Server writers lock the same owned lead before
reading conversion state; no new global auth/filter or migration. Actual SQL
race/audit rollback proof remains Desktop-owned. Converted student links are
terminal for trial/stage changes. Legacy Converted-without-link can still use
the existing explicit Convert action to create its actual student; do not invent
or rewrite historical links. New teacher options expose only owned id/name fields,
without granting teacher management or changing existing write authorization.

The trial UI must require complete current reads, preserve history names while
excluding converted leads from new booking, and guard overlapping/stale or
unconfirmed create/status submissions. Read-only recovery must never re-POST;
unknown create needs successful review plus explicit new intent. Retain existing
nullable teacher/notes and status values. Serialize the visible IST date/time
independently of browser zone, validate before toISOString, and label that zone.
Keep Lead Convert distinct from a stage update and preserve unrelated lead UI.

Validate original/final actual HTTP/Identity/InMemory, TSX and real sandboxed
Chromium with synthetic persistence/faults, multiple browser zones, exact-lock
build/types/lint/full suites and unchanged production guard. No assertion skips,
fake SQL receipts, new packages/provider sends or customer writes. Preserve all
older accepted evidence. Publish source-bound feature commits and exact Desktop
SQL/race/device tasks. All five P0s/broad-session/release gates remain open;
Mini/Plus/Pro unqualified, Ultra deferred, stop before model loading/inference/
training/activation. No main/Azure/environment/public-access changes.


## Result and source boundary

C27 implementation: SOURCE_PIN_PENDING. Branch above is the review/integration
return; no main merge, Azure deploy or environment publication. Path claim
0446f9585db9a1cc1c113af72fd5ca34f67263f5 was pushed before implementation.
The existing cloud environment was observed current with no reported failure
and restricted networking enforced. No environment, privacy, VPN or TCP policy
was changed, and no laptop drive/data or weight access is asserted.

Stage-only Converted now returns400 with the explicit Convert instruction.
Stage and trial mutations reject both linked and normalized legacy Converted
leads. Actual Convert still repairs an unlinked legacy Converted record by
creating its real student; repeat conversion returns409. Ordinary unconverted
New/Contacted/FollowUp/TrialBooked/Won/Lost booking remains supported. Existing
history can change trial status without modifying its converted lead.

SQL Server stage/convert/trial-create writers acquire the same owned-lead
UPDLOCK/HOLDLOCK under the existing AtomicAcademyMutation transaction. This
transaction includes the required actor audit, including opted-in Platform
Owner writes. Cached unchanged leads refresh after the lock; modified tracked
leads or a missing SQL mutation transaction are refused. There is no separate
premature business commit. This is a source repair; Cloud has not executed the
SQL lock/race/audit-failure cases. Trial status also opts into required audit
atomicity. No global access filter, protected host, schema or migration edits.

Sales-authorized teacher-options returns only own id/firstName/lastName; it
exposes no email/private teacher payload and grants no teacher management. Trial
page reads that endpoint and requires the complete validated lead/options/
history snapshot before editing. Failed reads show unavailable history with
read-only retry. Converted leads leave the booking picker but retain history
names. Dates and times are explicitly labelled IST and serialize with+05:30,
consistent with the existing Asia/Kolkata display, independently of browser zone.
Invalid dates/times reject before conversion to ISO or API submission.

Pending/overlapping and stale callbacks cannot replay a write. A receipt is
validated before confirmation or draft reset. Network/408/5xx or malformed
confirmation locks further mutations; reload reads history only. An unknown
booking requires successful review and explicit Start a new booking. Unknown
status similarly requires Continue after review. Acknowledged saves with failed
readback stay confirmed and can recover with a read only. Drafts survive refused
or unconfirmed writes; successful confirmation stays visible after readback.
Mounted read versions and cleanup reject late responses. Nullable teacher/batch
and existing status policy are retained; no new delivery or model capability.

## Verification

| Check | Observed result and limits |
| --- | --- |
| Original HTTP/Identity/InMemory |36 cases:19 PASS/17 FAIL; original controller snapshots and TRX retained |
| Fixed HTTP/Identity/InMemory |Same36 PASS; expanded53/53 PASS covering minimal field shape, roles/custom sales permission, Platform Owner audit, normal/terminal stages, explicit legacy conversion, refs, trial statuses and role/tenant/subscription refusal. These are not SQL races/rollback acceptance |
| Full API before final Desktop lifecycle import |2,325/2,325 PASS; zero fail/skip |
| Full API with Desktop26fb7d2 |2,325/2,325 PASS; zero fail/skip on final combined Desktop26fb7d2 source |
| Actual TSX/helper with controlled hooks/transport |Original page21 FAIL;8 new clock checks already PASS. Final29/29 PASS; original run29 total8 PASS/21 FAIL. No SQL or real browser inferred from this harness |
| Full current frontend |86 files,2,317 cases:2,314 PASS/3 FAIL/zero skips/cancelled. The only failure file is communication-drafts:45/48; missing genuine phase-2b communication SQL log. No assertions changed or receipts fabricated |
| Real Chromium |40/40 PASS;320/1440 widths,UTC andAmerica/Los_Angeles,light/dark. Actual dropdown/date controls, optional/assigned teacher, denied/retry reads, duplicate submit, lost-before/lost-after create, confirmed readback failure, status/lost status and stage picker.24 synthetic POST attempts/20 synthetic trial commits/8 synthetic PATCHes; no customer/host/SQL writes. No page overflow in checked trial scenarios, zero captured exceptions/interception errors; normal Chromium sandbox and exact owned-profile cleanup |
| Web build/types/lint |Exact existing lock:build/types PASS;87 static segment aliases,570 export files;lint zero errors/61 pre-existing warnings |
| Release guard |15/15 guard tests PASS; actual guard exit1 for five P0 records plus required broader session record. No acceptance records auto-closed |
| Hosted CI |UNKNOWN. verify.yml pushes only main and codex/release-export-security; C27 feature push is not a hosted CI result. No workflow dispatch/PR/main merge |

Raw local evidence is outside Git at
`/workspace/c27-sales-trial-integrity-20261109`: original source/log/TRX,
final HTTP logs/TRX, all frontend logs/summary, export build/types/lint,
native requests/screenshots/results, publication and hash manifest. These local
artifacts are not uploaded production acceptance. First native runner16 PASS/
24 FAIL because it omitted the real Select lead placeholder in its expected
option list; corrected the harness and retained that complete failure run.
A wrong-working-directory UI command and initial restricted child-process
attempt are not counted as passing checks. A tuple-type error was fixed before
the final successful types/build checks. Screenshot review restored existing
responsive form padding; the final export and native run cover that source.
No test skip or assertion weakened.

## Desktop handoff, in priority order

1. Integrate the exact C27 feature return after preserving current local work.
   Copy/cherry-pick C27's16 claimed paths, retain newer protected host work,
   and do not replace whole Desktop-owned files. Previous C24/C25/C26 returns
   and their live acceptance remain separately queued.
2. On disposable real Identity/HTTP/SQL, run Sales/Marketing/FrontDesk/custom
   sales.manage, Owner/Admin and Platform Owner two-tenant/module cases. Verify
   teacher-options exact3-field owned shape and generic teacher-management
   refusals. Missing/foreign lead/teacher/batch must leave no trial, stage,
   student/link or actor-audit mutation.
3. Exercise exact admission races against the same owned lead in independent
   contexts: Convert/Convert (one linked student), Convert/Trial (conversion
   first=>409/no trial; booking first=>committed trial then terminal Converted),
   stage/Convert and stage/Trial. Verify the source's scoped SQL lock really
   serializes all three writers, including previously tracked stale leads.
4. Inject required actor-audit failure for stage, actual Convert, trial create
   and status PATCH, including Platform Owner. Fresh SQL must show unchanged
   student/link/stage/trial/status and no orphan conversion/domain audit. Verify
   successful writes have exactly the expected domain and attributed actor audit.
5. Run actual browser/SQL booking, terminal filtering, optional/assigned teacher,
   IST date/time in differing browser zones, lost-before/lost-after response and
   acknowledged-save/readback recovery. Refresh/review must never re-POST or
   replay PATCH. Preserve original all-stage/dashboard/legacy-row evidence;
   do not silently rewrite legacy fake conversions or infer historical repair.
6. Complete actual role/device and linked critical/regression/documented
   acceptance before closing DATA-0026/DATA-0027/FUNC-0013. Cloud source/test
   success does not close them or the five P0/broader-session gates.

## What still blocks production and AI qualification

All120 inventory records remain indexed (5 P0/102 P1/13 P2); this is not120
untouched defects or a completion percentage. C27's3 P1 fixes have scoped Cloud
verification, with SQL/race/audit/device acceptance pending. Five P0 records,
broader-session/all-role/physical-device acceptance, C24/C25/C26 live acceptance,
actual provider setup/meetings/delivery, exact candidate CI/current security
reviews/images and staging backup/restore/rollback stay open. Do not rerun the
previously rejected external npm audit disclosure under Continue.

Desktop's latest deterministic Testing-only enrollment status/transfer workflow
is preserved exactly; its report records16 new native checks plus23 creation,
13 preview and57 shared cases including15 mixed admission races. Those Desktop
results are retained claims with source/report provenance, not newly repeated
SQL results here. Desktop owns make-up scheduling next, protected API/SQL,
migrations and remaining local data/device acceptance. Cloud can take the next
bounded non-AI source defect after publishing this return; claim its paths first.

It is still too early to say everything except AI is complete. No model was
loaded, downloaded, inferred, trained or activated. Mini/Plus/Pro remain
unqualified; Ultra is deferred and90% conversational-outcome coverage remains
unaccepted. Notify the owner when the documented non-AI entry gate is met;
keep the explicit stop on real model loading.
