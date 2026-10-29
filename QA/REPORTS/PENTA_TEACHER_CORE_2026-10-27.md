# Teacher shared Core adapter — 27 October 2026

## Completed backend and contract checkpoint

The protected teacher host workflow now implements the existing PENTA operational
contract's current context, partial save, preparation, operation, execution and
receipt/status shapes. It reuses the previously validated teacher service and
transaction/draft/approval/audit boundary. It remains Testing-only and independently
gated; no inference, public model-tool activation or production approval.

- [x] Unfiltered API build/test **3,065/3,065**, zero failed/skipped. Sixteen new
  cases cover teacher Core input, independent gates, normalization and descriptor.
- [x] Final SQL run `598c1da99b22477f802e4d5a151fe2d5`, window
  `216226a354534407bf3df39749a03c51`: **21 new Core checks**, retaining all
  **30 teacher workflow**, **15 teacher service** and **45 availability** cases.
- [x] Actual PENTA Python schema and ConnectorSDK accept the saved synthetic
  host responses in **11 compatibility checks**, with exact shared source hashes.
  No model import, loading, inference, live connector mutation or Mini source edit.
- [x] **33 CI artifact/workflow guard tests**, current full-suite floor **3,065**.
  Older 2,995/3,040/3,049 test receipts remain history, not a new candidate's proof.
- [x] Linked source/receipt/TRX/schema/live service/main verification:
  `QA/EVIDENCE/penta-teacher-core-qualified-c8ed13746d9c4cfbbcdfa2b4632f9cc2/result.json`.
  Original services/images/health and entire Docker inventory restored; only owned
  SQL resources removed, validation lease released. Main checkout unchanged.

## What changed

- The current database administrator/academy/session selects a single AI-05
  `AcademyDesk.Teacher.Create` revision1 descriptor with14 bounded non-credential
  fields. The response never adopts model-supplied actor, tenant, role, catalog or
  branch identity. Optional branch references come only from this academy; large
  catalogs fail closed instead of silently truncating. Existing saved inactive
  branches retain the Manual domain's current behavior and are explicitly labeled.
- Current context emits the existing `operational-draft-0.3.0` scope/catalog/draft
  contract. Each draft is an authority epoch; permission/context changes invalidate
  it rather than silently broadening scope. Native authorized drafts with no Core
  preparation identity are refused by the Core context adapter, preserving Manual
  behavior without manufacturing an approved Core operation.
- Strict WorkflowDecision parsing accepts exactly one teacher replacement edit,
  bound request/scope/base/catalog revision, preparation or bounded known partial
  questions, and explicit review invalidation. Unknown authority/credential fields,
  wrong tool, foreign scope, malformed input and implicit approval fail closed.
- Partial saves and preparation run in the same academy-first serializable linked
  SQL/Identity transaction. Preparation revalidates fields/branch/possible duplicates,
  normalizes the existing trimmed Manual values, increments revision and records
  a durable encrypted request digest/history and current preparation identity.
  Ordinary edits preserve request history while invalidating approval/preparation.
- Identical request replay returns the stored current result without a new write,
  revision, audit or expiry extension. Changed payload/scope or replay after later
  edits refuses. Preparation returns the SDK's list containing one bounded canonical
  operation; partial save returns the shared current snapshot.
- Preparation may issue a host review challenge but never authorizes creation.
  Explicit native host approval remains a separate user action. The shared operation
  reflects that committed approval; the Core execution port uses the same teacher
  transaction, existing duplicate controls and required audit/receipt. Completed
  replays and status recover the exact committed receipt, never create another row.
- Separate `Penta:TeacherCoreContractsEnabled` is required in Testing alongside
  the existing teacher/draft/HTTP gates. No production flag/config was enabled.
  Shared versions are unchanged; no alternative Core, model or business-rule engine.

## Actual SQL and compatibility evidence

The21cases verify anonymous/Teacher/foreign-tenant/other-admin denials, independent
Core gate, current authoritative descriptor, partial field persistence/replay,
foreign actor and role-field manipulation, shared preparation binding, unchanged
request replay, changed-payload refusal, unapproved execution refusal, explicit
host approval, simultaneous Core execute201/200 with exactly one teacher/audit,
receipt/status recovery, refusal of a native-only approval without Core identity,
foreign receipt scope, preserved request history after native edit, injected
preparation audit failure rollback, and inactive actor refusal. Scoped snapshots
compare domain teachers, drafts, sessions, audits, safe Identity data/memberships,
students and payments on refusals. Core adds exactly one synthetic teacher and
one execution audit; no portal accounts or inference.

Actual native receipts:

- `QA/EVIDENCE/teacher-creation-window-216226a354534407bf3df39749a03c51/`
- `QA/EVIDENCE/penta-teacher-core-sql-598c1da99b22477f802e4d5a151fe2d5/`
- `QA/EVIDENCE/penta-teacher-core-qualified-c8ed13746d9c4cfbbcdfa2b4632f9cc2/`

`contract-samples.json` contains synthetic actual HTTP outputs. The read-only
`check-teacher-core-contract.py` validates them with PENTA's actual frozen schema
classes and replays them through ConnectorSDK: preparation accepted, unapproved
execution denied, approved receipt accepted, status reconciles without execution,
foreign scope denied. It calls a receipt replay object rather than a live API;
that is compatibility proof, not model quality or end-to-end conversational use.
Both projects' source boundaries are hash-recorded; no Python cache/model writes.

The earlier20-case run `8cf26842738e47559e0f7a465e4ba21d` was passing for its
source. Final review added the native-approval/Core-identity refusal; final21case
run qualifies that change. Previous teacher workflow evidence is retained, not
relabeled as current release-image or new browser proof.

## UI continuation and remaining release work

- [x] New protected `/penta/teachers` review page and host-only state/approval UI
  implemented with shared Orbit/Prism foundation, separate approval/create,
  receipt/profile link, cancellation, unavailable/denied/unknown states and
  same-draft GET recovery. No visible conversational inference is implied.
- [ ] Fresh UI build and actual disposable-SQL browser qualification in progress;
  confirm screenshots, modal/scroll/mobile/light/dark and exact postconditions.
  The first UI lint found ref access during rendering; fixed without weakening
  lint. Its failed evidence/restoration is retained. No new UI acceptance yet.
- [ ] Qualified private teacher interpretation/dialogue and current conversation
  routing through shared Core; full AI/Manual workflow cohorts, all-role/device
  customer acceptance and existing exact enterprise/release gates remain open.

Owner stopped before model loading; no loading/train/inference/activation/download,
paid compute, commit/push/merge/main/production DB/Azure action. Existing model
fresh6/12/exposed7/12 remains NOT ACCEPTED. Production remains NOT READY; no
90%coverage, completed AI frontend or broad issue closure is claimed.

Next: continue the visible review and genuine shared conversation integration here.
Stay on Sol High for the host/Core authority boundary; Sol Medium once contracts
settle for bounded visual refinements. Use established Mini file handoff only for
a genuine shared contract/engine dependency; do not repeat accepted audits.
