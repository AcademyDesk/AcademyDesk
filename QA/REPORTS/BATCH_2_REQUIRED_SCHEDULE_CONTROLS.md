# Batch2 — required schedule controls and navigation

2026-10-14. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Successor to [class/calendar/schedule checkpoint](BATCH_2_CLASS_SCHEDULE_PENTA.md), not a new testing program. **The two recorded control/navigation tasks are finished at the synthetic browser level. Full page/portal/production acceptance remains OPEN.**

## Delivered

- Batch setup selected-day required time now uses the shared themed, anchored time picker with one-minute precision. Existing day JSON, payloads, optional values and reset/rejection behaviour retained.
- Schedule Start/End (IST) use shared date + time composition, direct month/year selection and exact minutes. Composition retains a local wall-clock string; the existing schedule handler remains responsible for IST→UTC conversion. No API or domain calculation change.
- Required-value validation participates in native form submission without exposing an OS date/time popup. Blank/partial dates cannot submit, pending controls are disabled, and invalid submission announces a message and focuses the visible trigger. Optional time pickers are unaffected. The validation proxy has no payload name.
- Mobile Workspace chooser now recognizes `/schedule` as **Class schedule**, supports direct Calendar↔Schedule movement and collapses after selection. Existing module eligibility/security rules retained.
- Screenshot review found that the first warning implementation resurrected a blank-field warning after successful reset. Completing a value now resets the validation component's warning state; additive unit/browser assertions prevent regression. No suppression or reduced save assertions.

## Final verification

All browser suites used the unchanged final production export after the warning repair. No application-source edits followed the final build.

| Check | Result / local evidence |
| --- | --- |
| `npm run build -- --webpack` | PASS compile, TypeScript and86 exported outputs; not86 accepted pages |
| Eight focused unit suites |216/216 PASS: batch preservation/times/feedback, schedule defaults/feedback, calendar details, workspace entry and8 new required-control checks |
| Schedule browser |12/12 PASS; `QA/EVIDENCE/schedule-feedback-browser-1791993707617/result.json` |
| Batch/calendar browser |60/60 PASS; `QA/EVIDENCE/batch-calendar-visual-1791993707663/result.json` |
| Shared dropdown/time controls |36/36 PASS; `QA/EVIDENCE/manual-controls-1791993707628/result.json` |
| Admin shell/navigation |24/24 PASS; `QA/EVIDENCE/admin-manual-1791993707676/result.json` |
| New required/date-time controls and time ESLint |0 errors/0 warnings |
| Vendor integrity, runner parsing, `git diff --check` |PASS; six normalized vendor hashes unchanged |

**132 final-export synthetic browser cases**, separate from216 unit checks. Schedule covers320/1440, both themes, successful save,400 rejection and confirmed save with failed refresh. Assertions include blank/date-only no-write, pending disable, exact10:37/11:37 IST→05:07/06:07 UTC, optional IDs null, rejected input retention, successful reset without stale alerts, one POST/PUT, current-section navigation and no JS/hydration errors. Batch covers320/768/1440, both themes, Offline/Online/Hybrid, required blank time no-write/focus and09:37 request precision; existing edit/deactivate configuration preservation remains. Shared controls retain layering/scroll/focus/keyboard checks for student fees, teacher payments and make-ups, including short landscape. Admin retains Core/full module locks and navigation/fallback assertions.

Final dark320px schedule and light320px batch screenshots inspected: clear separated controls, current menu label, readable status, no stale required alert or page overflow. Fixtures are synthetic; no real login/SQL/backend/device or enterprise acceptance is claimed. Earlier intermediate failures/receipts retained. Initial mobile Calendar fixture lacked an endpoint; the synthetic fixture was completed, not an application authority change.

## Lint and source provenance

Expanded touched-source comparison to HEAD retains3 inherited errors/6 warnings: date picker2 effect errors/1 dependency warning; batch1 effect error/3 warnings; schedule1 dependency warning; shell1 image warning. Time/new components clean. No new diagnostics, lint suppression or unrelated effect refactor. Whole-app lint is **not** clean and remains a release gate.

Final source SHA256 (raw bytes; application unchanged after export):

| Path under `apps/web/src` | SHA256 |
| --- | --- |
| `components/standard-date-time-field.tsx` | `84fbd97b8bd278a973b104b6c591977535ce37268bc3b53986c7900959d29566` |
| `components/design-system/required-control-value.tsx` | `17704213a8b2422aab27acf0c371968c08a71e570650815825de3c9f9b2dad8a` |
| `components/design-system/required-control-value.module.css` | `07bdc7e27b2477b81b7e87154add073485833f3526b433c62775301adcb25f08` |
| `components/standard-date-field.tsx` | `114a02aed606359e70fb04a0dc4c307c049decf379f4bb156cebf0fe8d98f068` |
| `components/standard-time-field.tsx` | `3d939577e745a647129425cdbfe1691e88647e04e40ffc53014c2bf969a1d754` |
| `components/enterprise-shell.tsx` | `338ad6d052fb8d859ca66e28b6dfa2766dbf043f52c57a9989cae68dfa366b5a` |

Browser-receipt source hashes independently rechecked against current files. Batch/schedule/calendar/CSS hashes are in the60-case receipt.

## Remaining and fastest safe continuation

Continue existing E3 enrolment/attendance/leave and remaining portal/state migration in coherent batches, not one cosmetic change per turn. Prior onboarding/directory/shared-foundation work is retained, not restarted. Full-page ledger stays unchecked where live/conditional/device acceptance is outstanding.

Production urgency does not authorize deployment or waive gates. Continue E9/E10 on an exact scoped candidate: outstanding P0/critical P1, lint, runtime/SQL migrations/rollback, role/tenant/finance and critical linked workflows, changed-device/accessibility/recovery checks, then reviewed GitHub integration and explicitly authorized Azure staging/deployment. Use the existing issue queue/evidence rather than restarting accepted audits. Full PENTA UI2 host composition, Mini qualification and operational AI capabilities remain separate open deliverables; the90% eligible AI target is not current coverage. Do not enable unqualified AI to accelerate release.

Next route: Sol Medium for settled E3 layouts; Sol High for consequential required-input, context, security, finance and release integration. This recommendation does not change the running model. No genuine new Mini contract dependency here; existing handoff remains in force.

No main/API/Mini/schema/SQL/container/credentials/permissions/staging/commit/push/Azure changes this slice. Earlier dirty backend/payroll/media work and local QA/EVIDENCE preserved. Main remains `f7af512f884ca5fe8ac3ddb287c852492592e709`; index empty.
