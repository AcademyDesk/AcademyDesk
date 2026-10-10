# Optional onboarding dates — bounded repair

2026-10-10; `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `403d495a7513602be0582161e0e98ed37eed2ea4`. Existing BUG-API-0001 / FORM-OPTIONAL-001, not a new testing plan.

## Source and scope

Both forms forwarded hidden blank date strings unchanged. Teacher DOB/joining and student admission date now become null when blank; valid/malformed nonempty inputs are unchanged. Required student DOB is not normalized or made optional. Employment/subjects/certifications/availability, guardian/consent booleans and all other original payload fields remain unchanged. No API, Identity, database, scheduling or permission rule changed.

Actual-handler tests also reproduced the existing post-await event.currentTarget.reset fault for valid submissions. Capturing the native form before the await fixes both resets without redesigning feedback: teacher shows existing success; student follows existing Student Management redirect/notice. Failed responses do not reset or navigate. Broader intake synchronous pending guards, readiness, unknown-result and malformed-success handling remain separate/open.

## Evidence

- 9 controlled actual-TSX tests: baseline5 PASS/4 FAIL, fixed9 PASS. Empty-date payload and valid-response reset failures reproduced; valid strings/malformed rejection, subject requirement, no-academy guard, original consent/required-DOB/subject payload and student redirect asserted.
- 32 exported-application synthetic browser cases PASS:320/1440px × light/dark × teacher/student × blank/valid optional dates × success400-rejection. Uses real year/month/day calendar interactions, not hidden-value injection; actual required student DOB selected. Optional blanks arrive as null, valid dates preserved; teacher resets/shows success, student redirects; rejected drafts remain. No page exceptions.8 teacher screenshots captured; mobile light/dark success screenshots inspected.
- 23 real SQL/Identity/HTTP cases PASS in run `63504277b61d4143b7bb4c169bc44ad1`: each teacher DOB/joining and student admission field omitted/null/empty/valid/malformed; student required DOB omitted/null/empty/malformed; both intake routes anonymous/foreign. Omitted/null succeed, empty/malformed400 with field-specific binding errors; fresh SQL dates match, student admission default remains UTC today. Invalid/denied requests leave teacher/student/guardian rows and Identity-user count unchanged. Contract fixtures use no account-creation credentials and omit unrelated optional fields, not a complete browser-payload/Identity-provisioning acceptance run.
- Native base health/auth/two-tenant controls PASS;88 application/7 Identity migrations, runtime target/owned cleanup controls PASS. Only this run's exact SQL container removed; Mini containers retained. Additive OnboardingDates harness module preserves existing modules/assertions.
- Target eslint0 errors/0 warnings, TypeScript and83-page webpack export PASS; SQL harness build0 warnings/0 errors. Existing unrelated Mini dirty snapshot preserved/tested but not accepted or included in publication. Full API/critical suite and prior availability module retained, not rerun.

Local receipts: `QA/EVIDENCE/onboarding-dates-browser-1791650867263/result.json` and `QA/EVIDENCE/onboarding-dates-63504277b61d4143b7bb4c169bc44ad1/result.json`. Previous historical QA validator remains blocked by missing local observed-checks.json; not rerun or claimed PASS. No production/Azure credentials/data, migration or deployment touched.

## Remaining / routing

BUG-API-0001 and BUG-FUNC-0003 remain OPEN; original28 feedback-route count unchanged. Synthetic browser and native SQL are separate evidence, not direct browser-to-SQL, physical Android/iOS, all optional fields, full onboarding Identity/fault or critical/release acceptance.

NEXT existing BUG-DATA-0004 domain/Identity rollback gap: read current evidence and reproduce only missing fault cases, Sol High. Sol Medium for bounded intake feedback/UI. Continue shared FW1/Mini/Manual/frontend and enterprise E1–E10; no new plan, model engine or cross-project handoff required here. Scoped feature publication only; no main edits/merge/push/Azure, unrelated32/28 continuity and local evidence retained.
