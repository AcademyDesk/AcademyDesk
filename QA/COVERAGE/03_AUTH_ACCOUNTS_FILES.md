# Coverage batch 03 — authentication, portal accounts and private files

2026-09-28. **STATIC REVIEW / TEST DESIGN; runtime NOT RUN.** Pinned application baseline unchanged; see [acceptance](../REPORTS/PHASE_1_ACCEPTANCE.md). Five native forms, 15 controls: login (2), registration (3), portal accounts (5), teacher note (2), teacher attachment (3). Full action/permission mapping for AuthSessionController, PortalAccountsController and LearningResourcesController; only resource actions in TeacherPortalController and file-related paths in PortalController. This does not mark either large portal complete.

## Sources and stable IDs

Paths are repository-relative. UI: `apps/web/src/app/login/page.tsx`, `register/page.tsx`, `portal-accounts/page.tsx`, `teacher/page.tsx:406–467`; shared session handling `apps/web/src/lib/api.ts`. API: `apps/api/Program.cs`, `Controllers/AuthSessionController.cs`, `PortalAccountsController.cs`, `LearningResourcesController.cs`, `TeacherPortalController.cs:420–530`, `PortalController.cs:135–260,344–359`. Models: `Domain/Identity/ApplicationUser.cs`, `Data/IdentityDbContext.cs` and Identity migration snapshot; `Domain/Entities/LearningResource.cs`, `Data/AcademyDeskDbContext.cs:485`.

| Reviewed form / interaction | Test IDs |
| --- | --- |
| Login form-1 | AUTH-FORM-003; AUTH-001, AUTH-002, SECURITY-SESSION-001 |
| Registration form-1 | AUTH-FORM-005; AUTH-FRAMEWORK-001 |
| Portal accounts form-1 | AUTH-FORM-004; AUTH-API-011; AUTH-PROVISION-001 |
| Teacher note form-1 | TEACHERPORTAL-FORM-001; TEACHERPORTAL-API-025 |
| Teacher upload form-2 | TEACHERPORTAL-FORM-002; TEACHERPORTAL-API-026; TEACHERPORTAL-UPLOAD-001 |
| Session current/profile/password/image APIs | AUTH-API-007 through AUTH-API-010 |
| Admin resource list/create/upload/publish APIs | LEARNING-API-007 through LEARNING-API-010 |
| Resource security and lifecycle | SECURITY-FILE-001; SECURITY-LIFECYCLE-001; SECURITY-GUARDIAN-001 |

## G02 — all 15 form controls

All length limits below are storage limits unless explicitly identified as server guards. Test N−1/N/N+1, blank/null/omitted/whitespace, Unicode and invalid JSON types. Never log passwords/tokens; fixtures must be synthetic. GUID tests include malformed, missing, nonexistent, other tenant and wrong relationship. Identity SQL constraints require real SQL, not InMemory.

| Form: control | Wire → actual guard/storage | Boundary and state obligations |
| --- | --- | --- |
| Login: username | Required text, trimmed → email string used as login identifier; not an HTML email control | Leading/trailing whitespace, case, missing/unknown account, malformed input; normal failed login, lockout, 429, unavailable API are distinct outcomes even though UI shows one message |
| Login: password | Required controlled password; no trimming; show/hide button changes input type only | Empty/wrong/correct; preserve exact spaces; toggle accessible name/pressed state; no token persistence until valid session is established |
| Register: email | Required HTML email → unchanged email; framework validates email and creates Identity username/email | Whitespace differences versus login; duplicate/case-variant username, long email against Identity max 256; no tenant/role fields accepted through registration DTO |
| Register: password | Required minLength 8 → password unchanged | UI minimum differs from framework minimum 6; composition requirements still apply; test 5/6/7/8 and missing character classes separately |
| Register: confirm | Required minLength 8; client equality check; not sent | Mismatch means zero request; matching invalid password still rejected by server; success clears both password inputs, leaves email and notice |
| Portal: portal-role | Select Student/Guardian/Teacher → role string, exact case allowlist | Parent label maps to Guardian; changing role clears person ID; invalid/Owner/PlatformOwner/custom role rejected |
| Portal: portal-person | Select → only selected role's StudentId, GuardianId or TeacherId; other IDs null | Required by handler and API; same-academy existence check. Inactive person is not explicitly rejected. Multiple accounts for one link have nonunique indexes; define policy before changing |
| Portal: displayName | Required UI text → nullable DisplayName Trim; null falls back to trimmed email; empty string does not | Whitespace yields empty; max 200 is EF limit, no controller length check; long fallback email can exceed it; optional API versus required UI is explicit |
| Portal: email | Required HTML email → trimmed username/email; nonblank server guard then Identity validation | Username uniqueness is global (normalized username index), not per academy; same email in another tenant must be handled clearly; Identity email/username max 256 |
| Portal: password | Required minLength 6 → nonblank server guard then Identity password validators | UI hint is not full policy; errors retained; valid creation must include durable role assignment (BUG-DATA-0011) |
| Teacher note: title | Required → Title Trim, nonblank server guard, max 250 | Whitespace, padded long title, rejected length must not produce unexplained 500 |
| Teacher note: notes | Required textarea → Description Trim, nonblank guard, max 2000 | 1999/2000/2001; multiline/Unicode preserved; note stored as published with generated note:// URL |
| Teacher file: title | Optional → title or filename (UI fallback retains extension); server blank fallback uses filename without extension; max 250 | Empty/whitespace, long filename, client vs API fallback; no required-title invention |
| Teacher file: description | Optional → string Trim, max 2000; UI sends empty string | Missing/null/empty distinctions, long comments, unsafe markup rendered as text |
| Teacher file: file | File selector or recorded File state → multipart File; nonempty, up to 50,000,000 bytes; extension allowlist | 0/1/limit−1/limit/limit+1 plus multipart overhead: request limit also 50,000,000 so a file exactly at limit can exceed total request limit. Reject unsupported or disguised content safely |

Teacher attachment extensions: jpg/jpeg/png/webp/pdf/mp3/m4a/wav/mp4/mov/webm/doc/docx (case normalized). Browser wildcard accept is broader; test gif, svg and unsupported audio/video. Extension validation is not content-signature validation. Generated GUID filenames prevent trusting client paths for storage; titles still require encoding and length handling.

Additional teacher payload fields are state, not new form controls: BatchId required owned batch; StudentId optional but must have active enrollment in batch; ClassSessionId optional with no lookup in the two create actions; Type UI sends Note/Attachment, defaults Class note/Class material on API, max 40. Test cross-tenant/mismatched session ID without claiming a confirmed disclosure. Url required max 2000. Notification and resource rows save together after file creation; failed DB save can leave an orphaned file. All file failure injection belongs in isolated storage.

## G01 — screen/interaction states

Login: busy label/disabled submit, error message, role-based redirect. Session GET failure currently becomes null and falls back to AcademyAdmin; tokens are still saved. Test 401/403/500/malformed session and do not mistake navigation for successful authorization. Workspace priority: platform flag, Teacher role, Student/Guardian role, otherwise AcademyAdmin. Platform returnTo accepts a single-leading-slash path, rejects //; test encoded/backslash paths and wrong-workspace routes. There is no MFA/recovery-code field despite framework support; enabled-2FA fixture must expose this unsupported UI path under AUTH-001, not be called a passing sign-in.

Registration: mismatch stays on form with no request; busy guarded submit; server failure generic; success clears secrets and stays with notice, no automatic sign-in/return. Registration creates no platform flag or academy link. AcademiesController.Create separately accepts an unassigned authenticated user and establishes an Owner workspace; this is not Platform Owner escalation. Whether public signup remains intended is a policy question. That controller's full onboarding form is outside this batch.

Portal account: load academy then all three directories; empty academy message; role-dependent choices. Directory responses are parsed without checking each status/schema, so non-array JSON can reach choices.map. Test forbidden/null/problem responses and loading after role switch. No submitting guard or network catch in create; success retains role, clears credentials/person, shows message and remains on screen. Desired success→return behavior needs a defined destination; do not silently change it during audit.

Teacher classroom: no selected batch hides note/upload forms. One-person batch automatically focuses first roster student; group permits whole-class or individual target. Batch switch resets focus/session selection; staged content surviving a switch must not be sent to the wrong class. Note handler resets event.currentTarget after await (existing BUG-FUNC-0001). Attachment helper returns normally after rejected HTTP status; caller clears staged File/preview regardless (new BUG-FUNC-0007), then performs the same unsafe async reset. Network rejection behaves differently. Test successful persistence, failed reload, duplicate taps and retained input.

Recording states: permission denied/unavailable API, recording, pause, resume, stop, preview, remove, retry; audio/video/image previews and generic document filename/size. Verify tracks stop and object URLs release on replace/unmount, no unnoticed recording after navigation, mobile media format support and memory limits. These browser/device tests are NOT RUN. Classroom filters/history controls are described as dependencies but not counted as fully reviewed standalone controls in this batch.

## Session and framework contract

Program configures 8-hour bearer access / 30-day refresh lifetime. Tokens are separate localStorage keys for four workspaces; admin reads legacy fallback keys. Logout helper removes only current workspace keys; it does not itself revoke a server token. Test multi-tab sign-out, XSS-sensitive storage, password change/reset, deactivation and token/key rollover. A failed refresh retains stored tokens; 403 does not trigger refresh. Concurrent 401s have no shared refresh lock. Existing BUG-API-0003: explicit init.headers from apiHeaders can override refreshed Authorization on retry. Inspect actual request headers in the future isolated browser test, never put their values in evidence.

Version match: API target net10.0 and Identity package 10.0.12; installed ASP.NET runtime observed 10.0.12. [Framework route source](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.12/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs) maps ten routes, listed in [static manifest](identity-endpoints.json). Register/login/reset routes are outside MVC AcademyAccessFilter. Manage routes require authentication. Refresh checks ticket expiration/security stamp. Login supports cookie/session-cookie or bearer modes and MFA. Confirmation/reset bind tokens to account operations. Forgot/resend responses avoid directly revealing account existence. Manage supports email/password and 2FA operations. No logout route is mapped in this source. AUTH-FRAMEWORK-001 must compare real endpoint metadata with this list and test malformed, expired, replayed and wrong-user tokens without sending real email. Runtime enumeration remains NOT RUN.

[Default password options](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.12/src/Identity/Extensions.Core/src/PasswordOptions.cs): minimum six characters with digit, lower/uppercase and nonalphanumeric requirements. No application override was found. Read effective options in the future harness; do not assume UI hints enforce server policy. [Identity service setup](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.12/src/Identity/Core/src/IdentityServiceCollectionExtensions.cs) supports bearer and cookie authentication. App UI chooses bearer; cookie-mode/CSRF and delivery configuration remain explicit integration checks, not certification.

## G03 — action-level access and storage

| Actions | Implemented boundary | Targeted obligations |
| --- | --- | --- |
| AuthSession.Current | Authorize + user lookup; missing user 401; active-user middleware rejects IsActive false | Every role may inspect own session, including unassigned user; never return password/token; no academy gate expected for own-account recovery |
| AuthSession.UpdateProfile | Same own-user gate; DisplayName required trimmed ≤200; PhoneNumber whitespace→null, no app cap | 199/200/201 name; null phone; SQL snapshot phone nvarchar(max); failures preserve old values |
| AuthSession.ChangePassword | Same own user; new password nonblank length≥6 then Identity.ChangePasswordAsync(current or empty) | Wrong current/no-write; effective composition rules; security stamp and existing bearer/refresh behavior; success JSON message |
| AuthSession.UploadProfileImage | Same own user; multipart image nonempty ≤2,000,000; declared ContentType jpg/png/webp only | Signature mismatch, missing file, total multipart limit. GUID/user filename; previous file deleted BEFORE Identity update; inject failed update and concurrent uploads to check retained URL/file consistency. Public avatar policy is separate from private material policy |
| PortalAccounts.Create | Global active academy/Core gate then explicit same-academy Owner or AcademyAdmin | workforce.manage alone cannot pass action. Platform flag bypasses global gate but NOT action's tenant/admin check. Role-link type and tenant validated; user creation result checked, role-add result unchecked. Failure-inject all provisioning steps; no claim of atomicity |
| LearningResources.List/Create/Upload/Publish | AcademyAccessFilter enforces user, tenant, active academy, Certificates module; admin or platform bypass permission check | No catalog permission mapping for LearningResources: ordinary roles/custom grants fail closed. Lack of Authorize attribute is NOT anonymous access because filter still runs |
| Teacher resources/history/note/upload | Authorize; user AcademyId/TeacherId plus own batch; selected student active enrollment where used | No Teacher role check and no academy/teacher activity gate in these four actions. Compare Me's active teacher check; direct action does not require Me first. New BUG-SEC-0003. IsActive Identity false still denied by middleware |
| Portal.Student resource projection (partial) | Own active student or unrevoked CanAccessPortal guardian link; CanViewDocuments hides resource list; published, enrolled batch/course and individual targeting filters | Toggle each guardian flag, unlink enrollment, unpublished resource; assert no resource in list AND direct download denied (currently separate public-file defect). Full student detail DTO/workflow not marked reviewed |
| Portal.SubmitAssignment file path (partial) | Own active student/authorized guardian academic flag; published assigned work and active enrollment | Files stored under student-submissions; test private access separately from upload gate; missing/zero file plus blank text boundary; full submission UI not counted here |

Admin resource contracts: Title nonblank and absolute Url on Create; no allowed-scheme restriction beyond Uri.TryCreate, so test javascript/data/file/https against explicit product policy. Optional Description max 2000, Type default Link max 40, Url max 2000. Batch/Course IDs individually same tenant but no mutual-course relation check. Create returns empty 200, defaults IsPublished from required Boolean value. Upload: title fallback filename, type Document, omitted IsPublished defaults true, multipart limit 50,000,000; narrower extensions than teacher (no m4a/mov/webm), no separate file-length ceiling guard. Publish scopes ID to academy, 404 if missing; toggles only metadata, empty 200. No UI fields for /resources are marked reviewed yet.

## Private-file trace and negative controls

Existing BUG-SEC-0001 / SECURITY-FILE-001 remains OPEN P0; no duplicate issue created. Program places UseStaticFiles before authentication/authorization. Teacher materials, admin learning resources, and student submissions are placed beneath wwwroot/uploads; ordinary links open URLs without bearer headers. Upload/list authorization does not protect a known file URL. Include all three folders, unpublished metadata, revoked guardian permissions/enrollment, suspended academy, anonymous/foreign-tenant access, HEAD/Range and cached copies in the private-file test variants. Guessability is not authorization.

Each future reproduction creates one synthetic supported file, verifies its existence/content hash through an allowed control, then attempts denied reads using the same known URL. A 404 from a missing fixture is not a pass. Restrict generated paths/cleanup to run-owned storage. Storage persistence across restart/replicas and orphan files need separate operational tests; current production filesystem behavior was not inspected.

## New findings and coverage boundary

New: BUG-SEC-0003 (P1 lifecycle gates), BUG-DATA-0011 (P1 unchecked account role assignment), BUG-FUNC-0007 (P1 rejected upload loses staged file). All STATIC-FINDING / OPEN / final verification NOT RUN. Existing refresh, private-file and async-reset findings are extended by this specification, not counted twice.

Cumulative reviewed scope: 13/82 native forms, 87 form controls +3 standalone =90/649; 9/70 complete controller mappings. Ten framework routes have a separate static contract, not executable coverage. Remaining Teacher/Family actions, profile UI variants, staff roles/grants and other module forms remain unfinished. Next bounded batch: staff lifecycle, role assignment, temporary grants and access-review permissions; continue with Astra for security judgment, then Sol for implementation after Phase 1 acceptance closes.
