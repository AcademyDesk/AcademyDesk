# BUG-FUNC-0023 — Next-scheduled online make-ups require a hidden meeting-link field

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major valid scheduling flow blocked |
| Priority | P1 |
| Category | FUNC |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /makeup |
| API | POST makeup-classes (not reached) |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-MAKEUP-HIDDEN-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/makeup/page.tsx:23 |
| Class/function | create / conditional meetingLink input |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Choose Use next scheduled class, select Online or Hybrid and leave link state empty; use a batch with a valid upcoming online session and link. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Choose Use next scheduled class, select Online or Hybrid and leave link state empty; use a batch with a valid upcoming online session and link.

## Expected

Next-scheduled mode inherits its source session link without requiring an invisible manual input.

## Actual / evidence

Meeting-link control renders only for Manual, but validation requires link for Online/Hybrid in both modes. Submit stops before request even though server next-class mode derives the link. Runtime NOT RUN.

Source snapshot:

```text
22:   useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies", { cache: "no-store" }); const academies: Academy[] = await response.json(); if (!response.ok || !academies[0]) throw new Error(); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Make-up classes could not be loaded."); } })(); }, []);
23:   async function create(event: FormEvent) { event.preventDefault(); if (!academy || !studentId || !batchId) return setMessage("Select a student and class or batch."); if (mode === "Manual" && !start) return setMessage("Select a make-up date and time."); if (["Online", "Hybrid"].includes(deliveryMode) && !meetingLink.trim()) return setMessage("A meeting link is required for online and hybrid make-up classes."); setSaving(true); try { const response = await academyApi(`/api/academies/${academy.id}/makeup-classes`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, batchId, teacherId: teacherId || null, startUtc: mode === "Manual" ? toUtc(start) : null, deliveryMode, venue: venue || null, meetingLink: meetingLink || null, useNextScheduledClass: mode === "NextScheduled", notes: null }) }); const result = await response.json().catch(() => null); if (!response.ok) throw new Error(result?.message ?? "The make-up class could not be scheduled."); setStart(""); setVenue(""); setMeetingLink(""); setMessage("Make-up class scheduled."); await load(); } catch (error) { setMessage(error instanceof Error ? error.message : "The make-up class could not be scheduled."); } finally { setSaving(false); } }
24:   async function updateStatus(item: Makeup, status: string) { if (!academy) return; const response = await academyApi(`/api/academies/${academy.id}/makeup-classes/${item.id}`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ status }) }); if (!response.ok) return setMessage("The make-up status could not be updated."); await load(); }
25:
```

## Suspected root cause

Manual-only field requirement is evaluated outside the Manual mode guard.

## Business impact and blast radius

Next-scheduled make-ups when delivery selector is Online/Hybrid, including mode switches retaining state.

## Related / required regression

SCHEDULE-MAKEUP-HIDDEN-001: Browser matrix manual/next × Offline/Online/Hybrid, source-session link/no link, switching modes and blank/prior link; inspect actual request count and final feedback.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
