# Batch2 — Admin student/teacher onboarding PENTA visuals

2026-10-14. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD unchanged `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Continues E3 in the existing four groups. **Bounded local UI checkpoint PASS, not enterprise/release or whole-portal acceptance.** No commit/push/Azure.

## Delivered

- Two onboarding pages: persistent native labels, explicit required/optional captions, PENTA-mapped controls, consistent section/card/field spacing and responsive stacking. Removed decorative title copy; restored the operational minor/adult parent-requirement hint previously hidden by a generic helper rule.
- Reusable `OnboardingField` and scoped `onboarding.module.css`, not global/vendor overrides. Existing field names, required flags, submission routes, error retention and success/reset/student navigation retained. Shared select supports an optional persistent accessible label; existing callers retain selected-value naming.
- Teacher availability now uses the existing anchored time control, not the browser clock. Full-minute precision (`intervalMinutes=1`), honest blank display, clear and Done/Escape focus return; same names/HH:mm payload. Only checked days enter existing availability JSON; success resets controlled times, errors retain them. No scheduling-policy change.
- Visual review found the minor parent-access checkbox visibly unchecked after DOB changed. It now reflects the existing server rule (`StudentOnboardingController`: `isMinor || request.AllowParentPortalAccess`). Disabled checkbox remains non-authoritative. Explicit adult consent is separate and survives DOB correction. No backend/permission rule changed.

## Final verification

Application source unchanged after final successful export.

| Check | Result / local receipt |
| --- | --- |
| `npm run build -- --webpack` | PASS: TypeScript +86 exported outputs, not86 accepted Manual screens |
| Scoped ESLint: two pages/new field/shared select/time | PASS:0 errors/0 warnings |
| `node --test QA/tools/onboarding-dates.test.cjs QA/tools/workspace-entry.test.cjs` | PASS29:9 existing handler contracts +20 entry/census checks |
| `node QA/tools/onboarding-dates-browser.cjs` | PASS96; `QA/EVIDENCE/onboarding-dates-browser-1791969168245/result.json` |
| `node QA/tools/onboarding-states-browser.cjs` | PASS44; `QA/EVIDENCE/onboarding-states-1791969178726/result.json` |
| `node QA/tools/manual-controls-browser.cjs` | PASS36; `QA/EVIDENCE/manual-controls-1791969189085/result.json` |
| Immutable design source | PASS6 normalized FW1-R1 vendor hashes |
| Script syntax/scoped Git whitespace | PASS; repository LF→CRLF warnings only |
| Local frontend GET | `/student-onboarding` on localhost3000 returned200; not an authenticated save |

**176 final synthetic browser cases**, separate from29 unit checks. Onboarding96:320/390/768/1440 × light/dark × two forms × blank/valid optional dates × success/400/500. Assertions cover exact payload/null dates, response-held pending/disabled fields, one POST, retained details on error, successful reset/student return, persistent native labels, no native time input/no horizontal overflow, standard heading, subject add/remove, exact09:37/10:37 availability, popup keyboard/focus and PENTA token mapping. Three isolated case workers accelerate tests without dropping assertions.

State44:320/1440 × both themes × two forms × empty/malformed academy response/403/503, plus320/768/1440 × both themes for minor-parent required fields/automatic-grant display/adult consent transitions and time clearing/top-layer bounds/focus. Disabled forms are not proof of backend authorization. Shared-control36 separately retains Fees/Teacher Payments/Make-up Classes menu/date/time/layer/scroll checks, including short landscape.

Reviewed final dark teacher desktop availability and light student phone screenshots; earlier dark teacher phone inspected too. Synthetic identities only. Full-page screenshot capture does not expand internally scrolling desktop containers; interaction assertions provide the recorded coverage. Physical iOS/Android/screen-reader, real authentication/save/SQL and full-page/portal acceptance remain OPEN.

## Failures and limits retained

- Handler fixture lacked CSS-module support: added explicit style mock, existing handler assertions retained.
- Initial time-test relative locator was doubly scoped: corrected locator, not positioning behavior.
- Initial state test used Playwright `isDisabled` on fieldset: now asserts actual DOM property plus disabled native input/submit.
- Earlier96/44 receipts precede minor-display repair. Final build and all affected browser suites rerun after repair; old evidence/failures retained.
- No live SQL/API/model/finance tests rerun for this UI slice. Existing enterprise/backend/payroll/media/Mini work and unresolved gates preserved.

## Next

Admin student/teacher directories/management/profiles, then classes/batches/calendar/schedule and remaining screens. Real UI2 host composition, qualified Mini and governed conversational tools remain E4–E8 work, not delivered by this UI checkpoint. Full route/theme/state acceptance remains open. Sol Medium for settled visuals; Sol High for authority/context/finance/real AI execution. No new Mini dependency or project switch; existing dependency handoff retained. No API/schema/credentials/production DB/Mini/vendor/main edits; no staging/commit/push/deployment. Other dirty work preserved.
