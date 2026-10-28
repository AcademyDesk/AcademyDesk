"use client";

import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { useRecordEditor } from "@/lib/use-record-editor";

type Academy = { id: string };
type Guardian = { id: string; firstName: string; lastName: string };
type Profile = {
  preferredName?: string;
  addressLine1?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  preferredLanguage?: string;
  students?: unknown[];
  invoices?: unknown[];
  communications?: unknown[];
};
const count = (rows?: unknown[]) => rows?.length ?? 0;
const prepare = (value: Profile): Record<string, string> => ({
  preferredName: value.preferredName ?? "", addressLine1: value.addressLine1 ?? "",
  city: value.city ?? "", state: value.state ?? "", postalCode: value.postalCode ?? "",
  preferredLanguage: value.preferredLanguage ?? "",
});

export default function GuardianProfilePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [guardians, setGuardians] = useState<Guardian[]>([]);
  const [guardianId, setGuardianId] = useState("");
  const [message, setMessage] = useState("Loading family records…");
  const editor = useRecordEditor(academy && guardianId ? `/api/academies/${academy.id}/guardians/${guardianId}/profile` : "", prepare, "The parent record could not be loaded.");
  const profile = editor.data;
  const form = editor.draft ?? {};
  const saving = editor.saving;
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!active) return;
        if (!academies[0])
          return setMessage(
            "Create an academy before managing family records.",
          );
        setAcademy(academies[0]);
        const guardians = await academyApi(`/api/academies/${academies[0].id}/guardians`);
        if (!guardians.ok) throw new Error();
        const rows: Guardian[] = await guardians.json();
        if (!active) return;
        setGuardians(rows);
        const requestedId = new URLSearchParams(window.location.search).get(
          "guardianId",
        );
        const selected =
          rows.find((guardian) => guardian.id === requestedId) ?? rows[0];
        if (selected) {
          setGuardianId(selected.id);
        }
        setMessage("");
      } catch {
        if (active) setMessage("Parent records could not be loaded.");
      }
    })();
    return () => { active = false; };
  }, []);
  function select(id: string) {
    setGuardianId(id);
    setMessage("");
  }
  async function save() {
    await editor.save(form, "Parent administrative profile saved.", "The profile could not be saved. Check the record before retrying.");
  }
  const set = (key: string, value: string) =>
    editor.update((current) => ({ ...current, [key]: value }));
  const notice = message || (editor.loading ? "Loading parent record…" : editor.message);
  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Parents / record</p>
        <h2>Parent record</h2>
        <span>
          A reusable family contact record with controlled relationship,
          communication and billing context.
        </span>
      </header>
      <section className="enterprise-settings-toolbar">
        <select
          value={guardianId}
          onChange={(e) => void select(e.target.value)}
          className="field max-w-md"
        >
          <option value="">Select parent</option>
          {guardians.map((guardian) => (
            <option key={guardian.id} value={guardian.id}>
              {guardian.firstName} {guardian.lastName}
            </option>
          ))}
        </select>
      </section>
      {notice && (
        <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/30 p-3 text-sm text-amber-100">
          {notice}
        </p>
      )}
      {editor.failed && <button onClick={editor.reload} className="enterprise-action-button mt-3">Retry loading profile</button>}
      {profile && (
        <>
          <section className="mt-5 grid gap-4 md:grid-cols-3">
            <div className="surface-panel rounded-xl p-5">
              <p className="text-sm text-slate-400">Linked students</p>
              <strong className="mt-2 block text-3xl">
                {count(profile.students)}
              </strong>
            </div>
            <div className="surface-panel rounded-xl p-5">
              <p className="text-sm text-slate-400">Family invoices</p>
              <strong className="mt-2 block text-3xl">
                {count(profile.invoices)}
              </strong>
            </div>
            <div className="surface-panel rounded-xl p-5">
              <p className="text-sm text-slate-400">Communication history</p>
              <strong className="mt-2 block text-3xl">
                {count(profile.communications)}
              </strong>
            </div>
          </section>
          <section className="surface-panel mt-5 rounded-xl p-5">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="font-semibold">
                  Administrative parent profile
                </h3>
                <p className="mt-1 text-sm text-slate-400">
                  Contact preferences and address details shared safely across
                  linked students.
                </p>
              </div>
              <button
                onClick={() => void save()}
                disabled={saving}
                className="rounded-lg bg-cyan-400 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
              >
                {saving ? "Saving…" : "Save profile"}
              </button>
            </div>
            <fieldset disabled={saving} className="mt-5 grid gap-3 md:grid-cols-2">
              <input
                value={form.preferredName ?? ""}
                onChange={(e) => set("preferredName", e.target.value)}
                placeholder="Preferred name"
                className="field"
              />
              <input
                value={form.preferredLanguage ?? ""}
                onChange={(e) => set("preferredLanguage", e.target.value)}
                placeholder="Preferred language"
                className="field"
              />
              <input
                value={form.addressLine1 ?? ""}
                onChange={(e) => set("addressLine1", e.target.value)}
                placeholder="Address"
                className="field"
              />
              <input
                value={form.city ?? ""}
                onChange={(e) => set("city", e.target.value)}
                placeholder="City"
                className="field"
              />
              <input
                value={form.state ?? ""}
                onChange={(e) => set("state", e.target.value)}
                placeholder="State"
                className="field"
              />
              <input
                value={form.postalCode ?? ""}
                onChange={(e) => set("postalCode", e.target.value)}
                placeholder="Postal / PIN code"
                className="field"
              />
            </fieldset>
          </section>
        </>
      )}
    </main>
  );
}
