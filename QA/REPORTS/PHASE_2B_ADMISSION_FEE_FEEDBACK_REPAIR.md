# Admission-fee loading and save feedback

2026-10-10; `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `e02c57daad47ba2913ab5687fc27656f7b4a666d`.

## Scope

Student Fee Details now mounts a keyed `AdmissionFeeEditor` for the selected academy/student. It retains existing form styling, optional values, zero/min0/step0.01 and the exact admission-fee PUT (`amount: Number(value) or null`, `dueDate: value or null`). No backend, database, approval, finance, authority or Mini changes.

- Checked uncached initial details, loading/failure notice and disabled save until loaded. Null optional fields become empty inputs; malformed non-object/array/field-type responses are not treated as a loaded fee.
- Durable polite success on the same screen, rejected/uncertain draft retention and generic safe explanations. HTTP5xx/network means unconfirmed: check the student's fee before retrying. No automatic retry or additional save/readback request; successful save deliberately retains entered values, matching prior behavior. This is not fresh SQL verification.
- Synchronous submit guard and disabled amount/date/button during save; native fieldset disables the existing date control. Pending-key remount closes a previously open date picker; callback also checks the pending ref. No shared date-control API change.
- Keyed scope prevents old detail-load or save responses from overwriting/showing status in another student's editor. Changing student intentionally starts a fresh editor. Parent academy/student list loading checks status/array and ignores unmounted results before exposing the workspace.

## Verification

- 21 new actual-TSX handler checks PASS: checked mount, exact PUT and retained values, optional nulls/zero/fraction/date-only request preservation,400/403/500/503/network, failed/malformed initial details, synchronous duplicate/date guard, keyed scope, unmount during mount/write and stable rerender.
- 17 existing subject-fee handler checks retained/rerun PASS; combined38/38.
- 52 exported-browser synthetic cases PASS:320/1440px × light/dark ×13 scenarios (success,blank,zero,400,403,500,network,initial-failure,malformed,scope-load,scope-write,academy-failure,student-malformed). Invalid workspace prevents editor; bad details prevent PUT; held saves plus duplicate submit send exactly one scoped PUT; switched-student draft survives late load/save. No page exceptions; expected HTTP/network faults are injected, not clean-HTTP claims.
- 88 unchanged decimal browser checks PASS on both consuming routes.
- Target page/component eslint0 errors/0 warnings, TypeScript and production webpack export83/83 PASS.
- 12 screenshots; mobile light success and dark scope-write screenshots visually inspected.

Local final evidence: `QA/EVIDENCE/admission-fee-feedback-browser-1791649537485/result.json`; decimal rerun `QA/EVIDENCE/subject-fee-amount-browser-1791649499727/result.json`. Initial browser run `admission-fee-feedback-browser-1791649487979` passed9 cases then timed out on a test's native-select lookup; corrected to the existing custom-dropdown option interaction, all assertions retained. Initial TypeScript check rejected an unsupported StandardDateField disabled prop; removed it in favor of native fieldset and pending callback guard. Final lint/types/build/browser pass, no suppression. Failure evidence retained locally.

## Open gates / next

Synthetic browser transport and controlled handlers are not real SQL/HTTP/Identity/RBAC/tenant/physical-device/critical-suite acceptance. Earlier subject-fee72-case browser receipt and enterprise results retained, not rerun/replaced here. BUG-FUNC-0003 remains OPEN and original28-route checkpoint count unchanged; additional shared editors are explicitly separate. No SQL container, customer data, production credentials or Azure touched.

NEXT Sol Medium: source-confirmed Student360 administrative-profile save feedback/pending/late-response gap, retaining fields/nulls/payload and server safeguards. Sol High for any privacy, authority, business rule, transaction or release decision. Shared FW1/Mini qualification and enterprise gates remain OPEN; no new testing plan or Mini project switch. Scoped feature publication only, unrelated Mini/32+28 continuity/evidence retained; no main edits/merge/push/deploy.
