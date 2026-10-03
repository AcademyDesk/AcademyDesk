"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string }; type Branch = { id: string; name: string; city?: string | null; state?: string | null; addressLine1?: string | null; postalCode?: string | null; isActive: boolean };
async function readBranches(id: string): Promise<Branch[]> {
  if (!id) return [];
  const response = await academyApi(`/api/academies/${id}/branches`, { cache: "no-store" });
  if (!response.ok) throw Error();
  return response.json();
}
export default function BranchesPage() {
  const [academies, setAcademies] = useState<Academy[]>([]); const [academyId, setAcademyId] = useState(""); const [branches, setBranches] = useState<Branch[]>([]); const [name, setName] = useState(""); const [city, setCity] = useState(""); const [state, setState] = useState(""); const [message, setMessage] = useState("Loading branches…"); const [editingId, setEditingId] = useState<string | null>(null); const [editName, setEditName] = useState(""); const [editCity, setEditCity] = useState(""); const [editState, setEditState] = useState(""); const [savingId, setSavingId] = useState<string | null>(null);
  const saveInFlight = useRef(false);
  async function loadBranches(id: string, successNotice = "") { setBranches(await readBranches(id)); setMessage(successNotice); }
  async function refreshAfterSave(id: string, notice: string) {
    setMessage(notice);
    try { await loadBranches(id, notice); }
    catch { setMessage(`${notice} The branch list could not be refreshed. Refresh the page to see the latest branches.`); }
  }
  function startSaving(id: string) {
    // A ref also blocks a second click before the pending state has rendered.
    if (saveInFlight.current) return false;
    saveInFlight.current = true; setSavingId(id); return true;
  }
  function finishSaving() { saveInFlight.current = false; setSavingId(null); }
  const unconfirmedSave = "The branch save could not be confirmed. Your details are still here. Check the branch list before trying again.";
  const unconfirmedUpdate = "The branch update could not be confirmed. Your details are still here. Check the branch list before trying again.";
  const unconfirmedStatus = "The branch status update could not be confirmed. Check the branch list before trying again.";
  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw Error();
        const data: Academy[] = await response.json();
        if (!cancelled) { setAcademies(data); setAcademyId(current => current || data[0]?.id || ""); }
      } catch { if (!cancelled) setMessage("The AcademyDesk API is not reachable."); }
    })();
    return () => { cancelled = true; };
  }, []);
  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try { const data = await readBranches(academyId); if (!cancelled) { setBranches(data); setMessage(""); } }
      catch { if (!cancelled) setMessage("Branches could not be loaded."); }
    })();
    return () => { cancelled = true; };
  }, [academyId]);
  async function createBranch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academyId || !name.trim() || !startSaving("create")) return;
    setMessage("Saving branch…");
    try {
      const response = await academyApi(`/api/academies/${academyId}/branches`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ name, city, state }) });
      if (!response.ok) return setMessage(response.status >= 500 ? unconfirmedSave : "The branch could not be saved.");
      setName(""); setCity(""); setState("");
      await refreshAfterSave(academyId, "Branch created.");
    } catch { setMessage(unconfirmedSave); }
    finally { finishSaving(); }
  }
  function beginEdit(branch: Branch) { if (saveInFlight.current) return; setEditingId(branch.id); setEditName(branch.name); setEditCity(branch.city ?? ""); setEditState(branch.state ?? ""); }
  async function saveBranch(branch: Branch) {
    if (!editName.trim()) return setMessage("Branch name is required.");
    if (!academyId || !startSaving(branch.id)) return;
    setMessage("Updating branch…");
    try {
      // PUT replaces the record: retain fields that this compact editor does not expose.
      const response = await academyApi(`/api/academies/${academyId}/branches/${branch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: editName, city: editCity || null, state: editState || null, addressLine1: branch.addressLine1 ?? null, postalCode: branch.postalCode ?? null, isActive: branch.isActive }) });
      if (!response.ok) return setMessage(response.status >= 500 ? unconfirmedUpdate : "The branch could not be updated.");
      setEditingId(null);
      await refreshAfterSave(academyId, "Branch updated.");
    } catch { setMessage(unconfirmedUpdate); }
    finally { finishSaving(); }
  }
  async function toggleActive(branch: Branch) {
    if (!academyId || !startSaving(branch.id)) return;
    setMessage("Updating branch status…");
    try {
      const response = await academyApi(`/api/academies/${academyId}/branches/${branch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: branch.name, city: branch.city, state: branch.state, addressLine1: branch.addressLine1 ?? null, postalCode: branch.postalCode ?? null, isActive: !branch.isActive }) });
      if (!response.ok) return setMessage(response.status >= 500 ? unconfirmedStatus : "The branch status could not be updated.");
      await refreshAfterSave(academyId, branch.isActive ? "Branch marked inactive." : "Branch reactivated.");
    } catch { setMessage(unconfirmedStatus); }
    finally { finishSaving(); }
  }
  const active = branches.filter(branch => branch.isActive).length;
  return <main className="enterprise-settings branches-standard"><header className="branches-heading"><span className="branches-icon" aria-hidden="true">⌂</span><div><p>Administration</p><h1>Branches</h1></div></header>{message && <p className="branches-notice" role="status">{message}</p>}{academies.length === 0 ? <p className="branches-empty">Create an academy first, then add its branches.</p> : <><section className="branches-kpis"><article><span>Total branches</span><b>{branches.length}</b><small>Locations configured for this academy.</small></article><article><span>Active branches</span><b>{active}</b><small>Locations available for new delivery.</small></article><article><span>Inactive branches</span><b>{branches.length - active}</b><small>Locations retained but not operating.</small></article></section><section className="branches-academy"><label><span>Academy</span><StandardSelectField disabled={savingId !== null} name="academy" value={academyId} onChange={setAcademyId} placeholder="Select academy" options={academies.map(academy => ({ value: academy.id, label: academy.name }))} /></label></section><section className="branches-layout"><form onSubmit={createBranch} className="branches-panel"><header><p>New location</p><h2>Add a branch</h2></header><div className="branches-fields"><label className="branches-wide"><span>Branch name</span><input disabled={savingId !== null} value={name} onChange={event => setName(event.target.value)} className="field" placeholder="Branch name" required /></label><label><span>City</span><input disabled={savingId !== null} value={city} onChange={event => setCity(event.target.value)} className="field" placeholder="City" /></label><label><span>State</span><input disabled={savingId !== null} value={state} onChange={event => setState(event.target.value)} className="field" placeholder="State" /></label><button disabled={savingId !== null} className="enterprise-action-button branches-wide">Create branch</button></div></form><section className="branches-panel"><header><p>Location register</p><h2>Branches</h2></header>{branches.length === 0 ? <p className="branches-empty">No branches yet.</p> : <ul className="branches-list">{branches.map(branch => editingId === branch.id ? <li key={branch.id} className="branches-edit"><div className="branches-fields"><label className="branches-wide"><span>Branch name</span><input disabled={savingId !== null} value={editName} onChange={event => setEditName(event.target.value)} className="field" /></label><label><span>City</span><input disabled={savingId !== null} value={editCity} onChange={event => setEditCity(event.target.value)} className="field" /></label><label><span>State</span><input disabled={savingId !== null} value={editState} onChange={event => setEditState(event.target.value)} className="field" /></label></div><div><button disabled={savingId !== null} onClick={() => void saveBranch(branch)} className="enterprise-action-button">Save</button><button disabled={savingId !== null} onClick={() => setEditingId(null)} className="enterprise-action-button enterprise-action-button-secondary">Cancel</button></div></li> : <li key={branch.id}><div><b>{branch.name}</b><small>{[branch.city, branch.state].filter(Boolean).join(", ") || "Location not set"}</small></div><div className="branches-row-actions"><span data-active={branch.isActive}>{branch.isActive ? "Active" : "Inactive"}</span><button disabled={savingId !== null} onClick={() => beginEdit(branch)} className="enterprise-action-button enterprise-action-button-secondary">Edit</button><button disabled={savingId !== null} onClick={() => void toggleActive(branch)} className="enterprise-action-button enterprise-action-button-secondary">{branch.isActive ? "Deactivate" : "Reactivate"}</button></div></li>)}</ul>}</section></section></>}</main>;
}
