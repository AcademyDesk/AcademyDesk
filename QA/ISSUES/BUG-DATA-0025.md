# BUG-DATA-0025 — Branch edits and status toggles erase saved street address and postcode

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / local repair |
| Final verification | Two fresh HTTP/SQL runs x13, API231/231 and controlled frontend94/94 PASS; live browser/device/all-linked/critical pending |
| Severity | Major unintended data loss |
| Priority | P1 |
| Category | DATA |
| Module | ACADEMY |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /branches |
| API | PUT /api/academies/{academyId}/branches/{branchId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMY-BRANCH-001 |
| Evidence classification | Accepted static trace plus controlled TSX and real Identity/HTTP/SQL baseline/repair evidence |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/branches/page.tsx:15 |
| Class/function | saveBranch / toggleActive / Branches.Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures create a branch with AddressLine1 and PostalCode through the API. Edit only its name in the register, then independently test Deactivate and Reactivate on fresh populated fixtures. Read the full persisted row after each action. |
| API response | Baseline list lacks street and PUT erases fields; final list/create/update preserve them in 13 bounded HTTP/SQL cases per run |
| Database before/after | Fresh complete SQL rows compared for populated/null edit/deactivate/reactivate; six rejected requests no-write; see local repair report |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted repair; HEAD unchanged, no push/deployment |
| Retest result | PARTIAL PASS — local original-case/nullable/API/SQL/controlled frontend; not browser acceptance |
| Regression result | API231/231, controlled frontend94/94, two SQL runs x13, TypeScript and targeted lint PASS; full critical pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures create a branch with AddressLine1 and PostalCode through the API. Edit only its name in the register, then independently test Deactivate and Reactivate on fresh populated fixtures. Read the full persisted row after each action.

## Expected

Editing visible fields or active state preserves unrelated stored address and postcode. Clearing either must be an explicit supported user action.

## Actual / evidence

Historical accepted finding: BranchSummary omitted AddressLine1 and UI Branch type omitted PostalCode. Both PUT payloads omitted both optional properties; Update assigned their bound null values, silently clearing existing data. Now reproduced before repair with three independent real HTTP/SQL fixtures and six controlled TSX checks. [Local repair/evidence](../REPORTS/PHASE_2B_BRANCH_ADDRESS_REPAIR.md) returns/sends the preservation fields. Two fresh final SQL runs x13 PASS; issue remains OPEN for browser/device/all-linked/critical. Existing raw API omission/null replacement behavior and concurrency policy remain unchanged; previously lost data is not recovered.

Source snapshot:

```text
14:   function beginEdit(branch: Branch) { setEditingId(branch.id); setEditName(branch.name); setEditCity(branch.city ?? ""); setEditState(branch.state ?? ""); }
15:   async function saveBranch(branch: Branch) { if (!editName.trim()) return setMessage("Branch name is required."); setSavingId(branch.id); const response = await academyApi(`/api/academies/${academyId}/branches/${branch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: editName, city: editCity || null, state: editState || null, isActive: branch.isActive }) }); setSavingId(null); if (!response.ok) return setMessage("The branch could not be updated."); setEditingId(null); setMessage("Branch updated."); await loadBranches(academyId); }
16:   async function toggleActive(branch: Branch) { setSavingId(branch.id); const response = await academyApi(`/api/academies/${academyId}/branches/${branch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: branch.name, city: branch.city, state: branch.state, isActive: !branch.isActive }) }); setSavingId(null); if (!response.ok) return setMessage("The branch status could not be updated."); setMessage(branch.isActive ? "Branch marked inactive." : "Branch reactivated."); await loadBranches(academyId); }
17:   const active = branches.filter(branch => branch.isActive).length;
```

## Suspected root cause

Partial UI representation is sent to a replacement endpoint without preserving fields absent from the editor.

## Business impact and blast radius

API-populated branch addresses/postcodes lost on ordinary register edits or active-state changes; list summary alone cannot verify address preservation.

## Related / required regression

ACADEMY-BRANCH-001: Browser and real HTTP/SQL edit/deactivate/reactivate with populated/null address controls; fresh row asserts unchanged unrelated fields, failed request no-write and explicit API clear behavior.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
