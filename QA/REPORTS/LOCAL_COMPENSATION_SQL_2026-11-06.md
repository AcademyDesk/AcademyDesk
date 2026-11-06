# Current compensation SQL roundtrip and audit boundary

Status: scoped repair and real SQL/API regression PASS; claimed before implementation, 6 November 2026, host checkpoint `c92490f`.
Continue BUG-DATA-0050 / C7's explicit live acceptance gap. Do not repeat accepted
browser coverage or start another enterprise testing plan.

Add one bounded module to the existing SQL harness and runner. Reuse the genuine
local C7 controlled-browser receipt for distinct saved JSON payloads. Exercise
real Identity login, controller binding, five-rate numeric/null preservation,
fresh SQL reads and unchanged resaves, invalid and foreign/unauthorized requests,
exact attributed audits and injected audit-save failure rollback. Preserve payroll
and unrelated teachers/Identity state. This is not physical-device, concurrent
payroll, financial-policy or model acceptance.

First run against unchanged application code. If a real integrity defect appears,
record the failing baseline before a minimal scoped application repair. Keep all
existing tests/assertions, issue closure requirements and 3 GiB admission guard.
Rebuild the harness and run meaningful API regression for any application change.

Owned disposable SQL only; exact ownership cleanup and original PENTA service
restoration even on failures. No customer data, downloads, candidate model loading,
training, activation, main or Azure changes. Use Sol High for this financial and
authorization boundary. The existing candidate-weight stop remains authoritative.


## Confirmed baseline defect and repair

Baseline window `346f835d79c74832be1b2b956a88dc43`, SQL
`64c397682ee840e4b4f10f447d3fb8d1`, built unchanged application from `c92490f`.
The five genuine captured C7 bodies passed first-save/readback/unchanged-resave;
invalid, anonymous, Teacher and foreign-scope requests were rejected. Those
**24 controls passed**, but two independent required-audit tests failed:

- Admin: audit fault hit once and HTTP 500, yet fresh SQL retained the new salary.
- Platform Owner: HTTP 200, audit fault never hit. This actor bypassed the required
  audit entirely. Its request repeated the candidate value from the first case;
  its unchanged-data flag therefore cannot establish rollback.

The failed receipt remains intact. Its exact owned container was removed after
ID/name/label/loopback/no-mount checks; full inventory and original services were
restored. No failed result was relabeled as passing.

Minimal application repair: only TeacherCompensationController.Save opts into the
existing `AtomicAcademyMutation(IncludePlatformOwner = true)` boundary. The shared
filter now keeps the domain save and attributed audit in one SQL transaction,
including existing authorized Platform Owner operations. No permission, Identity
write, payroll/zero-net policy, monetary calculation, payload or schema changes.
Reads stay outside the mutation transaction. Two existing action-boundary theory
cases additionally protect Save/Get selection; previous assertions are retained.

## Fresh repaired acceptance

Window `5da80b080d8241a3bd2395d56f20cf1b`, SQL
`7f8e7cd7937a477791af866108b4d7f6`:

- Harness build **PASS**, zero warnings/errors; real migrations 85 / 7 and exact
  owned runtime login verified. Existing runner adds only CompensationRoundtrip.
- **28 real HTTP/SQL checks PASS**, five distinct actual C7 captured browser bodies
  (receipt SHA256 `dd8d03dee919e830ec30f8db76cadc1bcba5a5b16a6618a2c31c7c721c2b1a69`).
- All five rate fields preserve numeric zero/null/decimal/date values through
  first save, fresh-context SQL, GET, unchanged resave and another read. Other
  teachers, Identity/membership and invoice/payment/payroll rows remain unchanged.
- Each successful save adds exactly one current actor/tenant/route audit. Two
  additive normal Platform Owner save/readback cases pass alongside the retained
  Admin matrix. Both actors' forced required-audit failures return native 500,
  hit the fault once and roll back the complete captured SQL state.
- Invalid negatives in each rate, missing required primary rate, invalid model,
  anonymous write, Teacher financial read/write, foreign-tenant read/write and
  foreign teacher ID in an own-tenant route are denied with no captured change.
- Full fresh API regression **1,986 PASS / zero failed / zero skipped**. This is
  the previous 1,984 cases plus two boundary cases, not a reset test baseline.
- Actual SQL database/login and exact owned container removed. Full Docker
  container/volume/network/image inventory unchanged; both original PENTA service
  IDs/images restored, API health 200, lease released. Admission stayed 3 GiB;
  SQL used the existing bounded 2 GiB/two-CPU mode. No download or Docker restart.
- Original main HEAD/status hash match the preserved snapshot. Source hashes,
  API TRX, native result and failure/repaired logs stay in excluded local evidence.

The frontend did not change for this backend-only repair. The accepted C7 browser
matrix was not repeated; its captured JSON was consumed by the new real SQL module.
Earlier UI/session results retain their exact source/artifact scopes. No current
production-image or all-device acceptance is inferred from this native run.

## Reproduction and remaining gates

Generate the genuine passing receipt with `QA/tools/compensation-browser.cjs`
against the same current UI source and verified built artifact. Set
`QA_COMPENSATION_BROWSER_RECEIPT` to that local result.json and
`QA_CLOUD_FINANCE_RECONCILIATION=1` for the existing bounded SQL mode. Rebuild
QA/tools/SqlHarness/SqlHarness.csproj with --no-restore, then run the existing
Run-ReconciledPayment.ps1 with -Module CompensationRoundtrip and -HarnessPath
pointing to the newly built SqlHarness.dll. Use the established exclusive local
lease, D: temporary root, RAM guard, original-service restoration and exact-owned
cleanup coordinator. Do not replace the receipt with invented success or expose
local raw evidence. Cloud can regenerate its own synthetic browser receipt.

BUG-DATA-0050 and BUG-DATA-COMPENSATION-AUDIT-001 remain **OPEN** for their remaining
critical/regression/device/release closure protocols. Concurrent server writes,
downstream payroll behavior and accounting review are not newly certified here.
C6 live finance approval/Teacher assigned-batch roundtrips, remaining P0/privacy/
role/workflow/device/restore, current production images/CI/audits/staging, full AI
model/tool qualification and release approval remain open. Ultra is deferred.
No candidate model loading/training/activation, customer DB, main or Azure change.
Next route: Sol High for the next live finance/Teacher authorization/SQL packet;
Sol Medium for settled visuals/docs. Continue the existing program from here.
