# Batch2 — courses, curriculum and lesson plans

2026-10-15. `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. Bounded E3 Manual presentation continuation,not full academic/portal/enterprise acceptance.

## Implemented

- Three screens consume a scoped academic PENTA stylesheet:semantic light/dark surfaces,18px panels,readable spacing,wrapping text/action rows,44px actions and responsive field sizing. No shared vendor/global CSS change.
- Course academy/type/edit-type and curriculum course selectors have persistent labels. Lesson-plan native select is replaced with the shared labelled anchored Batch selector;state,selected ID,options,request construction and nullable objectives/module/session values retained.
- Lesson plans have a proper teaching header,create/register panels,permanent title/optional-objective labels,readable cards,empty register state and disabled fields before successful load. Curriculum unavailable prerequisites show unavailable counts/register rather than zero/false empty state. No new server authority or publication policy.
- Syllabus table is a named focusable horizontal-scroll region;wide data stays inside it. Course list mobile flex-basis gap and duplicate Academy label were caught in screenshot review and repaired with additive browser assertions.
-16 pre-edit working-source function hashes preserve all named course create/edit/status/load helpers,curriculum fetch/mutate/add/publish and lesson fetch/initialise/add functions. Existing pending/save/publication/domain behavior is retained,not newly claimed complete.

## Verification

- Final webpack build/TypeScript/static export86 PASS. Scoped three-page ESLint retains1 error/1 dependency warning in Courses;both reproduce from HEAD through stdin lint. Curriculum/lesson pages have no new diagnostics. No lint suppression or unrelated effect rewrite;whole-app lint gate remains OPEN.
- 156 focused tests PASS:16 new function-preservation checks plus existing course field-preservation/feedback,lesson-plan feedback and trial/curriculum feedback tests. CSS mocks updated without weakening assertions. Course hidden fields/PUT preservation remains enforced.
- New54-case synthetic browser matrix:3screens×320/768/1440×light/dark×normal/empty/denied. Normal paths exercise create POST/readback with exact payloads,course edit/type/cancel,shared selectors collapse,syllabus keyboard scroll,44px actions and no document/main overflow/page errors. Normal APIs remain synthetic,not actual authorization.
- Existing lesson-plan16 and trial/curriculum64 synthetic browser scenarios rerun on the final export:success,rejection,uncertain committed write and confirmed-write/readback failure;the64 includes32 trial regression cases for unchanged trial source. Batch selection assertions now drive the actual visible shared selector and verify its hidden submitted ID,not a native-select-only test. Total final browser cases134;exact receipts/source hashes recorded below after completion.
- Final134/134 synthetic browser cases PASS: `QA/EVIDENCE/academic-visual-1792046960334/result.json` (54), `QA/EVIDENCE/lesson-plans-feedback-browser-1792046970997/result.json` (16), `QA/EVIDENCE/trial-curriculum-feedback-browser-1792046981289/result.json` (64). New matrix source hashes match final app files;final phone Courses light/Curriculum dark/Lesson dark screenshots inspected. No application-source edit after final build. Vendor6 unchanged;script parsing/scoped whitespace PASS. These are not live SQL/auth/device/release receipts.

## Test corrections and limits

The first layout fixture lacked the existing AcademicGovernance entitlement,so native actions correctly remained disabled under the host preview lock. Test fixture corrected to include that module;host permissions/entitlements untouched,no force clicks. An attempted escaped-string replacement did not change the fixture;subsequent correction verified actual fixture content before rerun. Lesson-plan test initially retained one removed native-select variable;updated to assert the shared control's submitted ID. Intermediate failures remain local.

This visual checkpoint does not improve the preserved action uncertainty/retry policies,course academy-switch/context/readiness logic,lesson malformed array parsing or draft-reset concurrency. These remain consequential High workflow follow-ups before full academic acceptance,not waived by CSS or synthetic transport. Courses initial empty/error semantics remain inherited;only curriculum/lesson explicit unavailable presentation is improved. Lesson route's broader chooser/navigation coverage also remains in the portal ledger,not claimed finished. No liveSQL/Identity/physical Android/iOS/full domain/device/release acceptance;no issue closure.

Next Medium:assessment/assignment/resource academic presentation then communications and remaining portal states. High for current-context/action recovery,authority,AI/runtime or other consequential findings. Existing PENTA UI2/Mini/domain tools/E9–E10 quality gates stay OPEN. Owner order unchanged:complete agreed PENTA+Manual app → authorized go-live → Hayansh → Academy Desk product website after first customer live.

No backend/schema/Mini/main/staging/commit/push/Azure/production data/container changes. Prior unrelated dirty application work and QA/EVIDENCE retained. No project switch/new engine handoff needed.
