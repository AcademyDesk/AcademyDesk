# Batch 18 — Academic governance, grading and periods

2026-09-28. Source specification; runtime NOT RUN. Completes AcademicGovernanceController (Schemes/AddScheme/Prerequisites/AddPrerequisite), GradingSchemeLifecycleController (Active/Status) and AcademicPeriodsController (List/CreateYear/CreateTerm/CloseYear/CloseTerm), eleven actions. Reuses ACADEMIC-API-001–009,014–015 and prerequisite dependency from batch17. Assessment grading consumers inspected only as dependencies, not complete assessment controller reviews.

Four forms: ACADEMIC-FORM-001 (3 controls), ACADEMIC-FORM-002 (2), ACADEMIC-FORM-003 (4), ACADEMIC-FORM-004 (4). No new standalone inputs. Cumulative54/82 forms,301 lexical plus96 standalone =397/649 controls and37/70 complete controllers. No grading records, prerequisite edges, terms or years were changed.

## G02 — grading and prerequisite forms

| Field | Contract and tests |
| --- | --- |
| Scheme name | Required name input; API trims/rejects blank. GradingScheme has no explicit name length/unique mapping in current DbContext. Test null/empty/whitespace, Unicode/escaped markup, oversized host/storage input and same-name duplicates without assuming a unique rule |
| passingPercent | Required number min0/max100, no step specified (native integer step). API decimal accepts inclusive0–100. Test0/100 and outside, fraction e.g.59.5, null/omitted/invalidtype, SQL decimal precision readback. Decide whether fractional thresholds are supported in UI; do not silently round them |
| bandsJson | Optional textarea default[]; empty -> string[], API null defaults[] and checks JSON syntax only. Test whitespace/invalid JSON, object/scalar/null/array and malformed entries; no schema, ordering, overlap or band-range validation exists. JSON is text inside request JSON, not an embedded array property |
| courseId / requiredCourseId | Two dropdowns from all courses; client checks both nonempty. API rejects self/missing/foreign and existing exact edge; MustBeCompleted defaults true. Test both absent, malformed GUID, inactive/unpublished courses, duplicate/case-independent ID and graph cycles |

Grading records default active; Schemes returns all tenant schemes, Active returns active only sorted by name. No update/delete/band editor endpoint in reviewed controllers. Status toggles only IsActive and returns ID/state. Optional BandsJson is not required to save a scheme. UI renders raw JSON in pre, so arbitrary markup must remain escaped and long content must scroll/wrap safely.

Prerequisite rows map IDs to course names with Course unavailable fallback; same-name courses and stale lookups need correct identity. UI has no edge deletion or MustBeCompleted editing. BUG-DATA-0029: self-edge guard is insufficient for A→B→A or longer cycles; Enrollments.Create requires prior completed courses, leaving no entry for a new learner. Test acyclic chain/diamond, reciprocal/long cycle and concurrent reciprocal inserts. There is no explicit unique prerequisite mapping in DbContext, so duplicate preflight alone does not establish concurrency safety. Treat no-delete as a recovery constraint, not permission to edit production data directly.

## G02 — academic-year and term forms

| Form / field | Contract and boundary tests |
| --- | --- |
| ACADEMIC-FORM-003 name | Required year name, API trimmed nonblank, EF100 and unique AcademyId/Name. Preflight duplicate guard; test100/101, trim/collation/Unicode and concurrent duplicate |
| year-start / year-end | Required date controls, DateOnly JSON strings. API rejects end<start, accepts same day. Test missing/null/empty/default binding, leap date, reverse/equal range and long period; overlapping years not prohibited |
| isCurrent | Checkbox on ->true elsefalse. Creating current year clears existing current flags in the same SaveChanges then inserts new row. Test checked/unchecked, no current, existing current, persistence failure and concurrent requests |
| ACADEMIC-FORM-004 academicYearId | Dropdown excludes closed years; required by handler. API scopes selected year to tenant but omits IsClosed guard (BUG-DATA-0030). Test stale selected year, concurrent closure, foreign/missing and empty GUID |
| name | Required trimmed nonblank term name, EF100 and unique AcademyId/AcademicYearId/Name. No API duplicate preflight; SQL duplicate must produce truthful no-write feedback |
| term-start / term-end | Required; end>=start and both within parent year. Test exact boundaries, same-day, one day outside, reversed, null/default/malformed and leap day. Overlapping terms are not prohibited; obtain policy before requiring nonoverlap |

New year/term default open. List returns tenant years newest start first, terms ascending start. Term has FK to AcademicYear with cascade; no delete endpoint here. UI sends actual bool for current year, not string. No amount, contact or file fields in these forms; do not invent optional requirements.

## G03 — permission and module boundaries

These controllers rely on registered academyId-aware global filter rather than Authorize attributes. Ordinary users require matching active academy. AcademicGovernance/AcademicPeriods require AcademicGovernance module and Owner/AcademyAdmin or academics.manage. Courses lookup additionally needs Core. Platform flag bypass remains intentional. Anonymous, inactive account, suspended tenant, foreign route/entity IDs, missing modules and revoked/expired/custom grants need real HTTP assertions. Status/close queries scope both entity and academy. Preserve authorized platform cross-tenant operations and deny unauthorized tenant crossover.

GradingSchemeLifecycle defaults to Core module and has no permission mapping: ordinary Owner/AcademyAdmin pass but custom academics.manage is denied. BUG-FUNC-0015 documents visible create-versus-toggle mismatch. Active lookup has same problem and may affect assessment setup. Approve whether lifecycle should share AcademicGovernance module; the current fallback also means an administrator with Core but not AcademicGovernance can reach lifecycle routes if IDs known. Test module/role combinations independently; do not expand staff roles or remove scope checks as a workaround.

Generic audit is a second save after domain action, reuse BUG-API-0002. Authoritative tests inspect response, domain rows and audit separately, including rejected actions and lost response after commit. All read lists are tenant scoped; empty results, malformed route IDs and unauthorized responses must remain distinguishable in UI.

## Closure, current-year and graph invariants

CloseYear rejects if any term for that year remains open, then sets IsClosed=true and IsCurrent=false. CloseTerm simply sets IsClosed=true. Repeated close is accepted; no reopen endpoint. UI has no confirmation or busy guard for closing; specify deliberate confirmation/undo policy because this is one-way through current UI. Test no-terms year, mixed closed/open terms, repeated calls, foreign entities and no mutation on409.

CreateTerm does not check parent IsClosed; it can introduce open terms after CloseYear established all terms closed (BUG-DATA-0030). Fresh dropdown excludes closed years, but stale form/direct calls bypass that presentation guard. Test closure-versus-create concurrency and ensure rejection is atomic. Two concurrent CreateYear(IsCurrent=true) calls can each observe old current rows and leave more than one current; no filtered singleton-current index exists. Add barrier-based SQL test and approved uniqueness expectation; no concurrency execution claimed.

Year/term closure is a record flag in these handlers, not proof every assessment/batch operation is locked. Downstream consumers must be audited before claiming closure prevents all academic edits. Prerequisite text on courses is not the graph; completed enrollment satisfies required-course edges, not module publication or grading-scheme threshold. Reuse enrollment transition finding BUG-DATA-0006; full enrollment review remains pending.

## Grading consumers and policy decisions

Assessments.Create permits a GradingSchemeId only if same-tenant and active. AssessmentResults.Upsert with a scheme uses explicit request Grade when nonnull, otherwise score/max*100 compared with PassingPercent -> Pass/Fail. It does not read BandsJson. Empty/whitespace Grade trims to empty but does not trigger fallback. Existing linked assessments continue resolving a scheme after deactivation because result lookup does not check IsActive. Specify null/empty/manual override, below/at/above threshold, decimal score/max precision and deactivated-linked versus new-assessment behavior. Missing linked scheme makes SingleAsync throw; no delete route exists here, so do not assert normal deletion causes this today.

Grade bands are currently syntax-checked stored text, not implemented automatic letter-grade ranges. Clarify whether they are descriptive or executable policy; if executable, define schema, bound inclusivity, overlaps/gaps and override audit before implementation. No inferred grading semantics or silent conversion from arbitrary JSON. Teacher assessment result path uses its own grade handling; parity belongs in upcoming full assessments audit. Administrative result permission mapping also requires later full-pipeline comparison, not an assumed completed review.

## G01 — feedback, dates and visuals

Both governance submit forms and both period submit forms call event.currentTarget.reset after await; reuse BUG-FUNC-0001. Governance catches that exception and may display raw error despite persistence; periods handler lacks catch and may leave stale page after saved data. Both pages set success then load clears it, including scheme toggle and period close; extend BUG-FUNC-0003. Governance finally releases saving, periods create/close has no busy guard. Test duplicate taps, rejected requests, transport failure, save success plus readback failure, retained fields and durable announced success before returning focus to correct register. Failed reset prevents subsequent controlled input reset and reload, not evidence that database failed.

Governance load internally catches failures and updates notice, so callers do not receive an explicit readback success result. Three lookups must all succeed; test missing lookup permission and stale data. Generic restart advice must not substitute for403/validation diagnostics. Initial counts/empty registers are not reliable zeros while loading. Governed courses tile currently counts all courses regardless of prerequisite/scheme configuration; label semantics need explicit acceptance.

BUG-UI-0006: period formatDate parses date-only at local browser noon then displays in Asia/Kolkata. For Los Angeles September noon, India time is next day; saved dates display incorrectly. Test all stored start/end dates in India/UTC/US/Pacific zones, DST/year/leap boundaries. Use exact calendar-date equality, not just valid formatting. This is display only; DB dates are unchanged.

Visual cases320/360/390/768/1280,200% zoom,both themes,mobile keyboard/landscape: clear labels and field fill, percentage bounds/errors, long raw JSON confined to panel, long course/year names wrap, status chips distinguish closed/current/active. Table-local horizontal scrolling must not trap page scrolling. Dropdown/date pickers anchored/unclipped with direct year selection and close-on-choice. Header links Periods/Curriculum/Promotions and main navigation must work directly with correct active label. Keyboard focus, accessible names, role=status duration and focus return after save/close require browser verification. No screenshots/device baselines approved.

## Outcome / next scope

Four new OPEN static findings: BUG-DATA-0029 P1, BUG-DATA-0030 P1, BUG-FUNC-0015 P1 and BUG-UI-0006 P2. Feedback/reset findings reused. AcademicGovernance partial mapping now complete. Next: batches, enrollment and promotions, followed by assessments/scheduling and remaining gaps. Phase1 G01/G02/G03 remain open; continue agreed Astra High audit stage, Phase2 not started. No application repairs, runtime writes or deployment.
