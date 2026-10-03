# BUG-DATA-0040 — Channel settings saves erase configuration fields not exposed by the editor

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | LOCAL REPAIR VERIFIED — bounded TSX/Identity/HTTP/SQL |
| Final verification | BOUNDED PASS; live browser/device/all-linked/critical NOT RUN |
| Severity | Major hidden configuration loss |
| Priority | P1 |
| Category | DATA |
| Module | COMMUNICATION |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /communication-settings |
| API | PUT communication-settings/{channel} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMMUNICATION-ROUNDTRIP-001 |
| Evidence classification | Accepted static diagnosis reused; repaired actual TSX payloads verified through real local HTTP/SQL |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/communication-settings/page.tsx:18 |
| Class/function | load / save / saveMeeting |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | [Repair report](../REPORTS/PHASE_2B_COMMUNICATION_ROUNDTRIP_REPAIR.md), logs and source/evidence snapshot; baseline excerpt retained below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures save an Email channel with valid replyToAddress and externalAccountReference through the supported API. Load page, change only display name, save, and compare a fresh read. Repeat optional fields for other channels. |
| API response | 24 actual editor payloads; complete PUT/GET summaries compared with fresh synthetic SQL |
| Database before/after | 48 real HTTP/SQL cases: target/hidden/secure/unrelated fields, intentional normalization, explicit clears and denied no-writes |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local frontend repair, uncommitted; no deployment |
| Retest result | 39 actual controlled TSX checks PASS; not live browser |
| Regression result | 48 real Identity/HTTP/fresh SQL PASS; TypeScript/build PASS; lint0 errors/1 inherited warning |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures save an Email channel with valid replyToAddress and externalAccountReference through the supported API. Load page, change only display name, save, and compare a fresh read. Repeat optional fields for other channels.

## Expected

Editing a visible field preserves supported existing settings not offered for editing, unless clearing is explicit.

## Accepted baseline finding / historical evidence

Every save hardcodes replyToAddress=null. Email load builds a draft with externalAccountReference empty regardless of stored value, then save overwrites it. Meeting save also forces phoneNumber=null and messagesEnabled=false. API replaces all these values rather than preserving omitted settings. Runtime NOT RUN.

Source snapshot:

```text
17:   useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies", { cache: "no-store" }); const academies: Academy[] = await response.json(); if (!response.ok || !academies[0]) throw new Error(); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Settings could not be loaded. Confirm the API is running on port 5092."); } })(); }, []);
18:   async function save(channel: ChannelName, draft: Draft) { if (!academy) return; const response = await academyApi(`/api/academies/${academy.id}/communication-settings/${channel}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ ...draft, replyToAddress: null }) }); const result = await response.json().catch(() => null); if (!response.ok) return setMessage(result?.message ?? "Settings could not be saved."); setMessage(`${channel} settings saved.`); await load(); }
19:   async function saveMeeting() { if (!academy) return; const response = await academyApi(`/api/academies/${academy.id}/communication-settings/Meeting`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ provider: meeting.provider, status: meeting.status, senderName: meeting.organizerName, senderAddress: meeting.organizerEmail, replyToAddress: null, phoneNumber: null, externalAccountReference: meeting.reference, messagesEnabled: false }) }); const result = await response.json().catch(() => null); if (!response.ok) return setMessage(result?.message ?? "Meeting provider settings could not be saved."); setMessage("Meeting provider settings saved. Secure OAuth connection is the next step."); await load(); }
20:   const fields = "w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2";
```

## Suspected root cause

Partial UI projection is submitted as full replacement including fabricated blank/null fields.

## Business impact and blast radius

Reply-to and external reference settings can disappear during routine sender edits; no claim that these currently drive a working outbound integration.

## Related / required regression

COMMUNICATION-ROUNDTRIP-001: HTTP/browser/SQL roundtrip with each optional field populated, edit one visible field, compare all untouched properties. Test explicit clear separately and keep intentional per-channel normalization documented.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local bounded repair — 2026-10-01

[Verification report](../REPORTS/PHASE_2B_COMMUNICATION_ROUNDTRIP_REPAIR.md): complete sender drafts and saved Meeting hidden settings replace fabricated blanks/nulls; incomplete loads disable/guard saves.39 controlled actual TSX/48 real HTTP-SQL checks PASS, with 24 actual editor payloads traced through fresh SQL/full summaries. Explicit clearing and NotConfigured/Disabled messages normalization remain intentional. API/schema/security/secure-connection state/prior product repairs/normal dev binaries unchanged; no Azure/customer data/commit/deployment. Historical baseline was static, not a runtime-before failure. Cross-panel drafts/async feedback remain separate BUG-DATA-0041; browser/device/critical and release closure pending, Status OPEN.
