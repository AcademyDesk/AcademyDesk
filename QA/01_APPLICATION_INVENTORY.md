# Application inventory

Baseline and count definitions are in [BASELINE.md](REPORTS/BASELINE.md). Source was scanned across the actual web client, API, entities, mappings, migrations and existing tests. Semantic traces focus on authorization, write/response chains, financial calculations and shared UI controls; dynamic runtime behavior remains unverified.

## Architecture traced from code

`apps/web/src/app` contains 79 Next.js route files. `apps/web/next.config.ts` sets `output: "export"`; Azure hosts static output. React client pages call `academyApi` from `src/lib/api.ts`; there are no Next server API routes in this source tree. Components include EnterpriseShell, WorkspaceNav/Frame, standard date/time/select controls, interactive tiles and detail modals. Large `/teacher`, `/portal` and `/platform/control` files render multiple conditional/tab screens; 79 is a route-file count, not the number of rendered states.

`apps/api/Program.cs` registers MVC controllers, a global AcademyAccessFilter, SQL-backed AcademyDeskDbContext and IdentityDbContext, ASP.NET Identity API endpoints, CORS, rate limiting, forwarded headers, static file hosting and authentication. Controllers use DbContexts directly; no separate repository/service application layer or registered hosted/background worker was found. Infrastructure classes bootstrap identities. Notification delivery states exist, but queued Email/WhatsApp does not establish an actual external delivery service.

Authentication uses ASP.NET Identity bearer tokens, with configured 8-hour access and 30-day refresh lifetimes. Tokens are stored in browser localStorage under workspace-specific keys. `/api/auth/session` maps platform-owner flag and role names to Platform/Teacher/Portal/AcademyAdmin. Teacher/Student/Guardian links on ApplicationUser constrain self-service actions. The global academy filter enforces tenant identity, academy/module availability and controller permission mappings; many actions add stricter guards. Source absence of `[Authorize]` alone is not evidence of an unprotected academy endpoint.

Database: SQL Server, EF Core migrations in two context trees. There are 57 application DbSets/mapped tables, plus 8 Identity tables (`AccessGrants`, `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`): **65 mapped tables**, excluding migration history and unverified live-schema objects. EntityBase adds Id/CreatedAtUtc and AcademyEntity adds AcademyId; see source manifest and entity definitions. Identity domain classes are ApplicationUser, ApplicationRole and AccessGrant; base Identity join/claim entities are framework types. There is no separately implemented Flutter/Dart client.

Uploads in TeacherPortalController, PortalController and AuthSessionController write to local webroot folders. Export/download routes return CSV or generated HTML documents. `UseStaticFiles()` precedes authorization: privacy and storage durability require explicit review. Azure Container Apps deployment has min/max replicas, but source workflow does not provision shared durable upload storage. Do not infer that files survive replica replacement.

## Navigable census

| Inventory | Contents / limits |
| --- | --- |
| [MODULES.md](INVENTORY/MODULES.md) | 23 functional groups; all controller classes and route files mapped |
| [PAGES.md](INVENTORY/PAGES.md) | Every page route, headings, imports and file-local API calls; aliases are visible through imports |
| [FORMS_AND_FIELDS.md](INVENTORY/FORMS_AND_FIELDS.md) | 82 native form declarations, 649 JSX field/control occurrences, handlers, bindings and declared constraints; controlled editors outside forms retained |
| [UI_COMPONENTS.md](INVENTORY/UI_COMPONENTS.md) | 843 source UI occurrences: buttons/actions, navigation links, lists/tables/cards, selects/date/time controls and dialogs; dynamic repeats count once |
| [API_ENDPOINTS.md](INVENTORY/API_ENDPOINTS.md) | 282 HTTP actions across 70 controller classes in 69 files, routes/methods/request signatures, guards, data sets and return expressions |
| [CONTRACTS.md](INVENTORY/CONTRACTS.md) | 271 positional record contracts, class-based multipart contracts and frontend request expressions |
| [DATABASE.md](INVENTORY/DATABASE.md) | Application fields, nullable types, max lengths, precision, indexes and explicit relationships from mappings |
| [ASYNC_FORM_RISKS.md](INVENTORY/ASYNC_FORM_RISKS.md) | 21 distinct post-await event-reset source sites across 13 files |
| [SOURCE_MANIFEST.md](INVENTORY/SOURCE_MANIFEST.md) | 411 source/config-definition files with hashes and line counts; no secret configuration values |

Counts are extracted source occurrences, not estimated runtime coverage. Labels/options generated from arrays, component composition, CSS pseudo-elements and conditional forms need browser enumeration. The generated API parser handles HTTP attributes including named arguments; lexical branch summaries must be checked against the linked source before implementing tests. Appsettings, env values, uploaded files and database contents were not copied into reports.

## Non-controller endpoints

`GET /health` is a source-declared anonymous DB connectivity probe, giving **283 directly declared actions/routes** including controller methods. Development maps OpenAPI. `MapIdentityApi<ApplicationUser>()` adds framework routes under `/api/auth`: register, login, refresh, confirmEmail, resendConfirmationEmail, forgotPassword, resetPassword, manage/2fa and GET/POST manage/info are the expected framework surface; exact runtime enumeration remains BLOCKED until an isolated HTTP host is available. Do not conflate 282 controller actions with the full runtime endpoint count. The sample anonymous WeatherForecast controller is included in 282.

## State and business-rule hotspots

- Forms generally use local message/saving state; success handling is not centralized. Several successful saves reset an expired React event target or clear success while reloading. Optional string/date/JSON handling varies across pages and endpoints.
- Student intake conditionally creates guardian and linked Identity accounts; DB/Identity atomicity and created-account flags need tests. Teacher creation stores names plus nullable details and JSON subjects/availability; record creation is separate from portal-account creation.
- Enrollment validates prerequisites and capacity on create but transition paths differ. Sessions validate date ranges and some clashes; teacher fallback/rescheduling need parity checks. Attendance is an upsert per session/student, with bulk and teacher-self attendance paths.
- Invoices, payments, adjustments and family summaries use inconsistent paid/balance definitions. Payroll and teacher compensation are separate models. Test exact SQL state, serialized amounts and all displaying portals.
- UI uses body-portaled fixed selects and inline date popovers; CSS has many route-specific overflow/stacking rules. The reported top-of-page and clipping failures need the explicit scroll protocol.
- Public static uploads, role-policy disagreements, cross-context writes, CSV exports and session refresh are high-risk boundaries. See individual issues rather than assuming source review proves exploitability or runtime correctness.
