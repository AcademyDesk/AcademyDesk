# Batch2 — enrolment, attendance and leave

2026-10-14. `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Existing E3 continuation, not a new audit or enterprise issue closure. **Bounded visual/control checkpoint PASS; not full page/portal/production acceptance.**

## Delivered scope

- Enrolments: labelled shared student/batch/status dropdowns, optional year-capable date, responsive intake/register,44px review/save/cancel actions. Non-Active status still requires a trimmed lifecycle reason and explicit save. Cancel is read-only; defaults, null optional date, existing end-date and request shapes retained.
- Attendance: persistent session/status/notes labels, wrapping identities, responsive roster and44px Save. Existing500-character/session-scoped notes, active-enrolment roster, same-tick write guard, saved/readback/uncertain feedback retained. Placeholder selection cannot send an empty status.
- Leave: shared required person/date controls, persistent reason label, responsive cards and44px Approve/Reject. Blank person or either date blocks native submit; pending disables every editable control. Student/Teacher switch clears the identity, and requests still send only the matching ID. Existing create/decision handlers, date-only payloads, Requested-only decisions and uncertainty safeguards unchanged. Load failures now give refresh/access guidance, not an unverified migration diagnosis.
- Shared select required validation is opt-in; existing optional selectors remain optional. Reuses the existing required-value proxy/focus/warning lifecycle without exposing OS dropdown/date widgets or changing backend validation.
- Added Enrolments and Leave requests to existing navigation groups. Mobile current labels also resolve resource-context queries such as `?studentId=...`; exact configured menu views retain precedence. No module/role/tenant authorization expansion. Three-route selection collapses without writes.

All styling is a scoped host CSS module consuming the accepted PENTA tokens, not vendor edits or a chat-layout replacement. Prior dirty backend/payroll/media/Mini work remains preserved.

## Verification

- Final webpack production build, TypeScript and86 exports PASS.86 outputs do not mean86 accepted screens.
-171 focused checks PASS:138 domain-handler checks (136 retained plus2 placeholder guards),13 required-control checks,20 existing workspace/route-census checks. Fixture imports/callbacks/selectors updated for actual shared components; payload/rejection/stale-handler/duplicate-write assertions retained.
- Scoped changed-page/select lint0 errors/0 warnings; expanded shell lint retains one inherited `no-img-element` warning. Whole-app lint remains an open gate; this is not a global clean result.
- Six immutable vendor hashes unchanged; scoped diff/parser checks PASS.
- Final54-case responsive suite PASS:320/768/1440 × both themes × enrolments/attendance/leave × populated/empty/403. Synthetic long names/reasons, all settled buttons44px, no page overflow, persistent native labels, required-field no-write, pending disable, successful reset without stale alerts, Teacher-only leave ID and optional enrolment date null. Phone tests include direct three-route navigation/collapse and selected-student query label. Receipt: `QA/EVIDENCE/operations-visual-1791997640126/result.json`; source hashes rechecked against current files.
- All seven final browser suites PASS,270 cases total (separate from171 focused checks), on the unchanged final production export:

| Suite | Cases | Local evidence under `QA/EVIDENCE/` |
| --- | --- | --- |
| Operations responsive/state/control/navigation |54| `operations-visual-1791997640126/result.json` |
| Enrolment/promotion feedback and exact payloads |80| `enrollment-promotion-feedback-browser-1791997640139/result.json` |
| Enrolment lifecycle reason/review/cancel/denial/uncertainty |44| `enrollment-lifecycle-browser-1791997640155/result.json` |
| Shared dropdown/time anchoring/layering/keyboard/scroll |36| `manual-controls-1791997640331/result.json` |
| Leave save/decision,400/500 committed uncertainty/readback |16| `leave-feedback-browser-1791997785564/result.json` |
| Attendance notes/status feedback/readback/uncertainty |16| `attendance-feedback-browser-1791997785578/result.json` |
| Admin shell/Core-full module navigation/fallback |24| `admin-manual-1791997826503/result.json` |

Final source shell SHA256: `506c5652af45058a1036e691e593c546dd83935dab097708228a15a7cf3c0a6f`. The54-case receipt pins the three pages, operations CSS and shared select; every available receipt source hash rechecked. Final phone enrolment/leave light and attendance dark images inspected after navigation repair; long values wrap and confirmation remains visible. No application-source changes followed the final export. The inherited singular roster caption (`1 students`) is a remaining minor copy-polish item, not a blocked save or silent full-page signoff.

An initial new pending-control assertion accidentally included non-editable hidden payload inputs. Corrected it to check editable inputs and every visible trigger/action; held-response/no-write assertions remain. Earlier browser passes before the navigation/query-label repair are retained but are not counted as final-export proof. No failing receipt was deleted or relabelled.

## Limits / next

This is synthetic exported-browser evidence, not real Identity/API/SQL, screen-reader, physical Android/iOS or enterprise acceptance. Existing issues remain OPEN where broader gates remain. No new API, schema, Mini, financial calculation, domain permission, SQL/container or credential work; no stage/commit/push/main/Azure action. Index empty; main remains `f7af512f884ca5fe8ac3ddb287c852492592e709`.

Next coherent E3 operations slice: make-up classes/holidays/events, then remaining finance/academic/communications and full Platform/Teacher/Student states. Retain accepted onboarding/directory/scheduling work. Actual AI UI2 host composition, Mini qualification and governed domain conversations remain separate E4–E8 deliverables;90% AI is still a target. Continue existing E9/E10 exact-candidate runtime/SQL/security/critical-workflow/device/rollback gates before authorized Azure deployment. Production urgency does not waive them or authorize deployment.

Routing: Sol Medium for settled visual rollout; Sol High for consequential domain/context/finance/security/release work. No new PENTA contract dependency or project switch is required here; existing engine handoff remains valid.
