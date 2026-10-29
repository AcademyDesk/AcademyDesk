# Regular payroll statement per period — 25 October 2026

Existing BUG-DATA-0003/E1/E7/E9 continuation. Owner explicitly approved:
**one regular payout per worker/profile and pay period; additional amounts need
a separately approved adjustment.** This does not authorize an unrestricted
generic adjustment endpoint or automatic bank disbursement. Existing FP2
NoCashDue, precision, worker identity/history, finance authorization and audit
rules remain mandatory. No policy inferred from accounting/tax standards.

## Bounded execution

- [x]Prepared independent QA pair invariants: one201/one409, one statement/one
  success audit; no429/5xxor false NoCashDue bank evidence can qualify a pair.
- [x]Baseline SQL `3d417b89f1ec44a6bdae6266cd2a11ff` PASS51ordinary HTTP checks,
  all three sequential duplicates and5/5true pairs returned201/201and persisted
  two statements/two audits. Alternate same-worker profiles and NoCashDue also
  reproduced. Required-audit500rollback/201recovery retained. This is defect
  reproduction, NOT protection acceptance. Source unchanged/owned cleanup/full
  SQL inventory PASS. Parent `21ab283d9753482da14081399456c9aa` PASS/exact original
  services healthy/full inventory/lease release.
- [x]Owner-approved guard implemented in PayrollController.Pay: transaction-owned
  academy payroll lock before profile-row lock, bounded worker-snapshot period
  lookup, explicit409duplicate outcome. Different worker/distinct period remain
  payable; original identity/history and audit/NoCashDue/precision remain intact.
  17meaningful unit cases prepared. First full suite2950total/2946PASS/four new
  Teacher-case failures from duplicate synthetic Teacher tracking in a shared-
  profile fixture, before controller invocation; no app failure inferred. New
  fixture now seeds its shared Teacher once before using the retained helper.
  All assertions and earlier suites remain unchanged. Failed build window
  `66bfb4e781984d9fb912e538b95c1881` retained; no candidate image created.
  Exact originals healthy/full inventory/lease PASS. Corrected full-suite/offline
  build `f87fdf029b6143e5b7cc50ae8b054a49`: full2950/2950PASS/zero skips and
  offline publish/image `a930092d4f7a4beca243036d76629be2`,
  `sha256:1caa0b4f0d1f817e79b196f278fda4d4122976e0c2c48aaed28cee3a070e25ba`
  built. Exact originals healthy/full inventory/lease PASS before next window.
  Native guard now also retains original FP1/FP2/FP3 assertions in the same owned
  SQL run, avoiding duplicate setup; no original policy assertions are weakened.
- [x]74focused handler/oracle/qualification checks PASS, including actual Payroll
  TSX409guidance/no reset/no automatic repeat. No frontend application source or
  export was changed; this is handler evidence, not a new linked browser/device pass.
- [x]Payload `96f3e7e7c44f491a980d63a03eb5b92d` PASS65files/93,741,055bytes,
  every manifest entry/hash/byte matches; unstarted inspection removed. Window
  `920d1042c7224c35b8991a697f5f820f` stays FAIL because the subsequent SQL process
  refused unchanged3GiB preflight; no SQL resources/requests started. Exact
  original restoration/full inventory/lease PASS. Retain the successful payload
  proof separately, never rebrand this whole window as a passed integration run.
- [x]SQL-only retry `8789bc45d614493e82f25df29ca8b02e` PASS84ordinary HTTP checks,
  three sequential guards,5/5pairs(one201/one409), month-alias duplicate rejection/
  next-month and other-worker positive controls, three role/tenant403s, required-
  audit500rollback/201recovery. Guard stage has12statements/16Payroll audits
  including four profile creates; later retained policy stage adds its own two
  statements/profiles. No429/unexpected5xx. Original FP1/FP2/FP3 all passed,
  including15no-write refusals/two NoCashDue models/terminal Voided behavior.
  SQL source/owned cleanup/inventory PASS. Parent
  `50692eba69374652844018a3a89b0f28` PASS/exact originals healthy/full inventory/
  owned cleanup/lease after3.089GiB admission. Original PENTA API health200.
  No repeated
  image copy/build/unit suite; same3GiB guard and existing approved service window.
  Independent qualification requires both this complete SQL/policy run and the
  separate actual payload/exact restored refused window, with exact refusal text.

## Final scoped qualification

Independent paired qualifier PASS: source manifest differs from grant-repair API
only in PayrollController; current2950API tests/fresh development image/payload
65files/84native HTTP checks/three sequential duplicate guards/five201-409pairs/
12guard-stage statements/16Payroll audits and retained FP1/FP2/FP3 assertions.
SQL receipt SHA256 `0cb79edd4dd6763c684b4ceff7a62b5784dc4de95fccc37ae21f9ac158b47f38`.
Failed first fixture build and payload-window SQL-startup refusal remain FAIL;
qualification requires the successful separate payload and fully restored windows.
All broader payroll/calendar/adjustment/device/security/release limits below remain.
- [ ]Minimal serialized period guard, meaningful API regressions, fresh offline
  image, native duplicate/concurrency/audit rollback/recovery and existing policy
  verification. Do not call source inspection alone a financial pass.

Coverage: sequential Monthly, SessionBlock and NoCashDue repeated periods;
five true barrier pairs including two alternate same-worker profile pairs and
one NoCashDue pair; different worker/same period positive control; audit failure
cannot leave a period reservation or prevent a real retry. Record exact SQL row
count, net and state plus per-operation success audits, not response status alone.

## Period and history boundaries

The current API records a free-text PeriodLabel. New guard must normalize known
labels (case/whitespace, with standard year-month/date-label equivalents covered)
consistently without silently rewriting old statement identity or amounts.
Current normalizer recognizes YYYY-MM, YYYY/MM, invariant English MMM yyyy and
MMMM yyyy; other labels use whitespace-collapse and ordinal uppercase. The API
now bounds new labels at120characters and guarded lookup at10,000worker-history
labels, failing closed for larger histories. New statements retain entered display
labels. Known recorded identities match across profiles; unknown/partial historical
identity only blocks its original profile, without guessing another worker.
No AI guesses which arbitrary labels mean the same period. A fully structured
payroll calendar, overlapping date ranges, legacy unknown-worker attribution,
recovery from an unknown committed result and the separate approved adjustment
workflow remain explicit remaining work, not hidden duplicate/idempotency closure.
Prior regular/NoCashDue/legacy statements are preserved; no cleanup/migration of
customer payroll data or main/Mini/Git/Azure/model change is authorized here.

High remains appropriate for this financial transaction boundary. This is one
bounded existing P0 slice, not completion of all payroll/enterprise/release gates.
