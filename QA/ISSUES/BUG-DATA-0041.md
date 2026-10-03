# BUG-DATA-0041 — Saving one communication panel silently discards unsaved changes in another

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED — controlled actual TSX; bounded local repair PASS |
| Final verification | BOUNDED PASS; live browser/device/critical NOT RUN |
| Severity | Moderate draft data loss |
| Priority | P2 |
| Category | DATA |
| Module | COMMUNICATION |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /communication-settings |
| API | PUT one channel then GET all communication-settings |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | COMMUNICATION-DRAFT-001 |
| Evidence classification | 6 controlled product draft-reset failures reproduced; repaired48/new87 combined TSX PASS, not live browser |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/communication-settings/page.tsx:16 |
| Class/function | load after save / saveMeeting |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | [Repair report](../REPORTS/PHASE_2B_COMMUNICATION_DRAFTS_REPAIR.md), baseline/final logs and pinned snapshot; original excerpt retained below |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | With persisted Email, WhatsApp and Meeting rows, edit WhatsApp number and organizer draft without saving. Change Email display name and save Email; inspect unsaved panels after successful reload. |
| API response | Controlled complete PUT summaries, faults and all six cross-panel reply orders;24 payloads identical to accepted previous HTTP/SQL fixtures |
| Database before/after | No new database run; unchanged API and previous48-case HTTP/SQL evidence reused, not recounted |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Frontend-only local repair, uncommitted; no deployment |
| Retest result | 48 new actual controlled TSX cases PASS;6 baseline product failures now pass |
| Regression result | 87 combined actual TSX PASS (48 new/39 reused); TypeScript PASS, lint0 errors/1 existing warning |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

With persisted Email, WhatsApp and Meeting rows, edit WhatsApp number and organizer draft without saving. Change Email display name and save Email; inspect unsaved panels after successful reload.

## Expected

Independent Save buttons preserve other panels dirty drafts or request explicit confirmation before replacing them.

## Accepted baseline / historical evidence

Every panel save calls load, which replaces all three drafts with saved server rows if present. Unsaved changes in other existing panels vanish without warning. Runtime NOT RUN.

Source snapshot:

```text
15:   const [academy, setAcademy] = useState<Academy>(); const [items, setItems] = useState<Channel[]>([]); const [email, setEmail] = useState(fresh("GoogleWorkspace")); const [whatsApp, setWhatsApp] = useState(fresh("MetaCloudApi")); const [meeting, setMeeting] = useState(meetingBlank); const [message, setMessage] = useState("Loading channel settings…");
16:   async function load(id?: string) { const academyId = id ?? academy?.id; if (!academyId) return; const response = await academyApi(`/api/academies/${academyId}/communication-settings`, { cache: "no-store" }); if (!response.ok) throw new Error(); const rows: Channel[] = await response.json(); setItems(rows); const emailRow = rows.find((x) => x.channel === "Email"); const whatsAppRow = rows.find((x) => x.channel === "WhatsApp"); const meetingRow = rows.find((x) => x.channel === "Meeting"); if (emailRow) setEmail({ provider: emailRow.provider, status: emailRow.status, senderName: emailRow.senderName ?? "", senderAddress: emailRow.senderAddress ?? "", phoneNumber: "", externalAccountReference: "", messagesEnabled: emailRow.messagesEnabled }); if (whatsAppRow) setWhatsApp({ provider: whatsAppRow.provider, status: whatsAppRow.status, senderName: whatsAppRow.senderName ?? "", senderAddress: "", phoneNumber: whatsAppRow.phoneNumber ?? "", externalAccountReference: whatsAppRow.externalAccountReference ?? "", messagesEnabled: whatsAppRow.messagesEnabled }); if (meetingRow) setMeeting({ provider: meetingRow.provider, status: meetingRow.status, organizerName: meetingRow.senderName ?? "", organizerEmail: meetingRow.senderAddress ?? "", reference: meetingRow.externalAccountReference ?? "" }); setMessage(""); }
17:   useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies", { cache: "no-store" }); const academies: Academy[] = await response.json(); if (!response.ok || !academies[0]) throw new Error(); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Settings could not be loaded. Confirm the API is running on port 5092."); } })(); }, []);
18:   async function save(channel: ChannelName, draft: Draft) { if (!academy) return; const response = await academyApi(`/api/academies/${academy.id}/communication-settings/${channel}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ ...draft, replyToAddress: null }) }); const result = await response.json().catch(() => null); if (!response.ok) return setMessage(result?.message ?? "Settings could not be saved."); setMessage(`${channel} settings saved.`); await load(); }
```

## Suspected root cause

Whole-page refresh unconditionally hydrates independently edited forms.

## Business impact and blast radius

Cross-panel unsaved sender/organizer changes and concurrent panel save ordering.

## Related / required regression

COMMUNICATION-DRAFT-001: Browser dirty/clean/missing-row panel matrix, save each panel, reversed response order and failed readback. Assert untouched drafts preserved and each success attributed to the right panel.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local bounded repair — 2026-10-01

[Verification report](../REPORTS/PHASE_2B_COMMUNICATION_DRAFTS_REPAIR.md): confirmed PUT updates only its channel; untouched drafts survive and normalized submitted fields reconcile without clearing newer pending edits. Panel-specific confirmations and same-turn duplicate guards, failed/uncertain-save retention and malformed-response blocking added. Baseline3 pass/6 product failures; final87 pass (48 new/39 prior).24 prior actual payload fixtures preserved exactly, API/schema/security/prior repairs/normal binaries unchanged. No new SQL/backend run, dev DB/Azure/commit/deployment. Live browser/device/navigation persistence/critical/release acceptance pending; Status OPEN.
