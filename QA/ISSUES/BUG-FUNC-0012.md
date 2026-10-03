# BUG-FUNC-0012 — Branch, course, batch and enrollment status controls remain busy after rejected network requests

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Moderate recovery failure |
| Priority | P2 |
| Category | FUNC |
| Module | ACADEMY |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /branches; /courses; /batch-setup; /enrollments; /schedule |
| API | PUT branches/{branchId}; PUT courses/{courseId}; PUT batches/{batchId}; PUT enrollments/{id} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMY-RECOVERY-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/branches/page.tsx:15 |
| Class/function | saveBranch / saveCourse / saveBatch / toggleActive / updateStatus |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Begin a branch edit or status toggle and make academyApi reject with a transport error before returning a response. Inspect the targeted button, status notice, retained inputs and retry behavior. Compare a resolved HTTP500 response. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Begin a branch edit or status toggle and make academyApi reject with a transport error before returning a response. Inspect the targeted button, status notice, retained inputs and retry behavior. Compare a resolved HTTP500 response.

## Expected

Every failure path releases busy state, explains the failure or unknown save outcome and supports safe retry without losing inputs.

## Actual / evidence

Both handlers set savingId before awaiting the request and clear it only afterward. No catch/finally handles rejected promises, so Save or status action remains disabled for that row without a new failure notice. Courses.saveCourse/toggleActive repeat the same missing catch/finally pattern. Batch save/toggle and enrollment updateStatus repeat the same pattern. Schedule updateStatus also lacks catch/finally and retains savingId after transport rejection. HTTP error responses that resolve do clear busy; distinguish these paths. Runtime NOT RUN.

Source snapshot:

```text
14:   function beginEdit(branch: Branch) { setEditingId(branch.id); setEditName(branch.name); setEditCity(branch.city ?? ""); setEditState(branch.state ?? ""); }
15:   async function saveBranch(branch: Branch) { if (!editName.trim()) return setMessage("Branch name is required."); setSavingId(branch.id); const response = await academyApi(`/api/academies/${academyId}/branches/${branch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: editName, city: editCity || null, state: editState || null, isActive: branch.isActive }) }); setSavingId(null); if (!response.ok) return setMessage("The branch could not be updated."); setEditingId(null); setMessage("Branch updated."); await loadBranches(academyId); }
16:   async function toggleActive(branch: Branch) { setSavingId(branch.id); const response = await academyApi(`/api/academies/${academyId}/branches/${branch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: branch.name, city: branch.city, state: branch.state, isActive: !branch.isActive }) }); setSavingId(null); if (!response.ok) return setMessage("The branch status could not be updated."); setMessage(branch.isActive ? "Branch marked inactive." : "Branch reactivated."); await loadBranches(academyId); }
17:   const active = branches.filter(branch => branch.isActive).length;
```

## Suspected root cause

Busy-state cleanup and error reporting cover HTTP status failures but not transport exceptions.

## Business impact and blast radius

Branch/course/batch/enrollment management after network interruption; page reload or another interaction may recover but is not a reliable retry flow.

## Related / required regression

ACADEMY-RECOVERY-001: Browser mocked transport rejection, HTTP403/500, timeout after possible persistence, refresh failure and success; assert busy cleanup, honest durable notice, preserved values and authoritative readback before retry.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
