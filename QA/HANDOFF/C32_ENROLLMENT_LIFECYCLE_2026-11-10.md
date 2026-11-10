# C32 enrollment lifecycle reason and safe form recovery — 10 November 2026

MODEL: Sol High for intent/recovery/authority checks; Medium for settled UI/docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c32-enrollment-lifecycle-20261110.
BASE: cc1bae0970f9197e08f8bbd5aa46390e120b5282 (clean published C31).
DESKTOP:4f458f9011fdfa32426a733fd1b4055bcfabe2e8 imported with provenance;
Desktop retains protected finance collection/reconciliation reads. No overlap.
STATUS: IMPLEMENTED / CLOUD VALIDATED; native acceptance and release remain OPEN.
TASK: BUG-FUNC-0016 missing lifecycle reason; enrollment subset of0012 busy/error
recovery. Complete the existing Manual page; do not change admission/domain rules.

Exact MAY MODIFY paths (9):

- `apps/web/src/app/enrollments/page.tsx`
- `QA/tools/enrollment-lifecycle.test.cjs`
- `QA/tools/enrollment-lifecycle-browser.cjs`
- `tests/AcademyDesk.Api.Tests/EnrollmentLifecycleAccessTests.cs`
- `QA/ISSUES/BUG-FUNC-0016.md`
- `QA/ISSUES/BUG-FUNC-0012.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C32_ENROLLMENT_LIFECYCLE_2026-11-10.md`

MUST NOT MODIFY: API/business/host/SQL/migrations/permission/session logic,
finance/scheduling/attendance, globals/vendor/Mini, existing tests/evidence or
unrelated work. No broad lookup permissions, new backend concurrency contract,
whole-file replacement of newer Desktop source, main/Azure/environment publication,
public access, model download/loading/inference/training/activation. Mini/Plus/Pro
unqualified; Ultra deferred. Production/90% conversational outcome acceptance open.

Policy: choosing a lifecycle status opens an explicit review; send no mutation
until confirmation. Collect a trimmed required reason for every non-Active target,
show exact student/batch/current/requested status and preserve the existing endDate
contract (Active clears it). Cancel/blank/same-state/invalid/stale UI sends nothing.
Creation retains Active/Waitlisted and server admission authority. Use immediate
pending/intent guards, finally cleanup, mounted/read-generation checks. Keep
existing data/drafts on failures; show useful server denial text. Distinguish
confirmed save with failed readback from unconfirmed transport/5xx/malformed
receipt; recovery GET never replays writes. After uncertain result require fresh
records and explicit review/new intent before another attempt. Empty/loading/failed
lookups must not look like an authoritative empty enrollment register. Minimal
scoped lookups for delegated operational roles remain a separate0018 packet.
No unsupported server optimistic-lock promise: a pre-submit read can refresh the
review but only the existing server rules arbitrate concurrent business writes.

VALIDATE: original/final actual TSX, full HTTP/Identity/InMemory contract with all
six offered states and required/no-write/authority guards; native sandboxed Chromium
with synthetic API at320/1440, real reason confirmation/cancel/fault/readback and
retained drafts; exact-lock types/build/lint/full frontend/API and release guard.
Capture failures outside Git; no fabricated SQL receipts or weakened assertions.
STOP WHEN: bounded source, checks, exact-source handoff and feature publication
verified. Issue closure/native SQL/device/release acceptance remains OPEN.
NEXT ROUTE: Desktop Sol High for actual SQL/Identity/browser/device acceptance;
Cloud next separately claimed non-AI gap. Model-loading stop remains.

## Source and publication

Claim was published and remote-verified before edits at
`727af922e2b546fe836e668f3a6262874f3f7590`.
Implementation: `56555fa5946dedc0b28f166d13070ef141cf4407` (four source/test paths).
Desktop4f458f9 finance reads were imported with provenance at
`5d361b9d5e89dec4075dc523adba98872990eea4`.
During C32, Desktop published protected collection/reconciliation reads at
`241810e488fcd7a1951dd0b341ae1c63a75b77fa`; imported with provenance at
`2786623e0b8cd2a412cb32111802fcaa1083b07c`. All18 distinct paths in the two
Desktop returns were byte-compared to that published Desktop source. No host,
Manual finance/domain, migration or SQL harness edits were authored by Cloud.
Desktop now owns protected expense/payroll read details; no overlap with C32.

The final handoff and both issue/inventory updates complete the9-path claim.
Final local/remote SHA and clean status are verified after feature push and sealed
in private publication.json plus the final response. No self-referential SHA is
inserted into its own documentation commit. Shared root handoffs retain Desktop's
changes; this separately published packet is the Cloud integration return.

No main merge, Azure deployment, environment publication/replacement, public
access, repository settings, PR/workflow dispatch, model or paid-compute change.
Environment was observed current/running/connected with restricted network
enforced. No live laptop/D: access is claimed. Mini remains clean at
`14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8`; all33 frozen paths unchanged.
The16 web vendor paths, including12 upstream pins, match C31 exactly.

## Behavior and material boundaries

The existing Manual enrollment page now has an explicit review and reason for
status changes. It preserves the API's six offered states and exact payload:
status, endDate and lifecycleReason. Active sends null end date/reason; every
non-Active target sends a trimmed required reason and the unchanged prior end
date. It does not invent new lifecycle, date, permission or capacity rules.
Creation retains Active/Waitlisted with selected learner/batch and optional
DateOnly. Date parsing rejects invalid calendar dates before submission.

Choosing a status sends no write. Cancel, whitespace reason, unsupported/same
status and obsolete callbacks do not submit. Review identifies the learner/batch
and reviewed/requested status. Immediately before PUT, no-store GET compares
identity, learner, batch, start/end dates and status with the reviewed snapshot.
Changed/deleted records require refreshed records and a new review. This is a
client safeguard; the existing server has no new ETag/row-version condition and
another actor may change data between GET and PUT. Existing admission locking,
validation, authority and audit remain the server boundary. Atomic protection
against every intervening lifecycle edit is not established by C32.

Pending/intent refs prevent overlapping create/status operations, duplicate
submits and delayed callbacks. New reads, edits, cancel and recovery invalidate
obsolete submit handlers. Generation/mounted checks discard late results.
Failed/malformed initial reads disable writes and show unavailable rather than
empty. Creation drafts remain mounted and selections are not silently replaced
by reload. Current endpoint lookup permissions are unchanged: the delegated-role
lookup problem in0018 remains separate; no broad people/batch grants are added.

A status preflight failure says no write was submitted. Definite HTTP refusals
show server detail;401/403/409 require refresh/review. Network loss after sending,
408/5xx and mismatching/malformed success receipts are unconfirmed. Draft entries
remain and writes stop until a GET refresh plus an explicit new-action choice.
That choice never resubmits previous entries. A verified exact receipt followed
by failed readback stays confirmed; GET-only recovery cannot duplicate the write.
Success notices survive readback. Finally releases busy state on every exit.
No transport/session helper, token lifecycle, host model or business code changed.

## Actual validation

Raw evidence is private outside Git at
`/workspace/c32-enrollment-lifecycle-20261110`. Original failures, intermediate
harness/type errors, final logs/TRX, screenshots and source/artifact hashes remain.
No weakened assertion, hidden skip or fabricated SQL receipt.

| Check | Observed result |
| --- | --- |
| Original actual TSX |0/34 PASS;first check reproduces an immediate write before reason/review;remaining failures include the absent new controls and are not34 independent defects |
| Final actual TSX / controlled transport |38/38 PASS, including six statuses, blank/cancel, stale source/callback, duplicate create/status, precise receipts, preflight/denial/unknown/readback and unmount cases |
| Focused HTTP/Identity/InMemory |125/125 PASS:17 initial new contract cases plus108 retained enrollment admission/mutation controls |
| Final new HTTP scope |20 cases in full API:all6 statuses, missing/blank reason with no writes/audit, unrelated role/foreign/anonymous/inactive/missing-record refusal, capacity conflict and both creation states |
| Full API before Desktop241810e |2,451/2,451 PASS;zero skips |
| Combined API after Desktop241810e |2,451/2,451 PASS;zero skips;fresh compile/full run;not native SQL acceptance |
| Native sandboxed Chromium |40/40 PASS:20 variants ×320/1440;synthetic API state and fault injection only;owned profile removed |
| Native scenarios |All6 statuses;cancel/blank reason;stale source;lost-before/lost-after;wrong receipt;failed readback;server capacity refusal;Active/Waitlisted create;lost create/readback;failed initial/preflight read;one attempted write maximum per intent |
| Exact-lock optimized build |Cached locked Next16.4.3/React19.2.8 webpack export PASS;87 static segment aliases,570 export files;no installation |
| TypeScript / lint |TypeScript PASS after fixing a new Promise.all tuple annotation;lint0 errors/61 retained warnings |
| Full frontend |89 files;2,416 PASS/3 FAIL/2,419 tests;zero skips/cancellations |
| Exact frontend failures |communication-drafts45/48 only;missing Cloud `QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`;historical Desktop receipts are not fabricated here |
| Release guard |15/15 controls PASS;actual guard exit1 retains five P0 records and the broader session gate |
| Issue inventory |120 records,5P0/102P1/13P2;0 formally Closed+Pass;not120 untouched defects or a completion percentage |
| Hosted CI |C32 unverified. Latest GitHub Actions query in C31 returned Forbidden;not retried/bypassed. Feature branch is outside verify.yml push filters;no dispatch/PR |

Native intermediate results are retained: first36/40 passed;four runner errors
tried to submit the removed confirmed-review form or selected the review form in
an initial-failure view. The runner now dispatches on the retained detached review
node (which cannot trigger a new write) or the actual disabled create form.
A subsequent10/40 passed because the new DOM assignment was returned through CDP
by value; changing only its result to Boolean fixed serialization. Final40/40
uses unchanged business/no-replay assertions. No browser sandbox was disabled.

Commands: `node QA/tools/enrollment-lifecycle.test.cjs`; native runner with
`QA_BROWSER_EXECUTABLE=/usr/bin/chromium`; `node QA/tools/run-frontend-tests.cjs`;
`tsc --noEmit`; `npm run lint`;
`NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack`;
dotnet test --no-restore -p:UseSharedCompilation=false with focused filter, then
full and combined full runs, retaining TRX;
`node --test QA/tools/production-gates.test.cjs`;
`node QA/tools/check-production-gates.cjs`;
`node QA/tools/priority-issue-status.cjs --write`. No new dependencies/audit upload.
The build/native web source is unchanged by imported Desktop finance files.

## Prioritized acceptance / next owners

1. Desktop Sol High: selectively integrate the four C32 source/test paths and
   packet report into its newer feature work; keep its finance/host files intact.
   Real Identity/HTTP/SQL should verify all six lifecycle payloads, required reason,
   exact durable summary/reason/audit, Active/Waitlisted admission/capacity and
   prerequisite refusals, tenant/role/revocation/module boundaries and audit
   rollback. Retain the existing protected enrollment SQL/race evidence with
   exact source scope. C32's20 InMemory HTTP cases do not replace it.
2. Desktop: actual SQL-backed browser for reason confirmation/cancel, all offered
   statuses, current/changed/deleted source, same-epoch session renewal and logout,
   before-commit interruption and lost acknowledgement after commit, failed
   readback, retained creation/reason drafts, zero automatic replay and deliberate
   new intent. Extend mobile/tablet/physical devices and other engines.
3. Cloud: continue bounded non-AI authorized-workflow gaps;0018 lookup access and
   remaining branch/course/batch0012 subsets require their own current source
   review and published path claims. Do not reapply historical fixes or expand
   management permissions merely for selectors. Desktop retains finance reads.
4. Release: five P0 acceptance records remain open:DATA0001/0002 finance,
   reminder/family/permission/currency agreement;DATA0003 positive-net payroll
   identity/history/print;DATA0010 audited void/evidence restore;SEC0001 private
   media authorization/recovery and actual recording/devices. AUTH-REVOCATION001
   broader SECURITY-SESSION001 remains an additional mandatory gate across
   remaining roles/devices/critical workflows. No bulk issue closure.
5. Integrator: exact-release hosted CI and fresh security/dependency audits,
   combined images/runtime, SQL migration/backup/restore/rollback and staging
   acceptance. Preserve genuine missing receipt failures until source-bound input
   exists. No main merge/Azure deployment follows from this feature return.

Non-AI/native/release work remains; AI is not the only next task. Mini/Plus/Pro
unqualified, Ultra deferred;90% eligible conversational outcomes unmeasured.
Testing-only deterministic PENTA host checks are not model qualification.
Candidate loading/download/inference/training/activation remains stopped.
