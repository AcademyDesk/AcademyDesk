"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Review = { id: string; reviewedAtUtc: string; notes?: string };
async function fetchReviews(academyId: string) {
  const response = await academyApi(`/api/academies/${academyId}/access-reviews`);
  if (!response.ok) throw new Error();
  const reviews: Review[] = await response.json();
  if (!Array.isArray(reviews)) throw new Error();
  return reviews;
}
export default function SignOff() {
  const [academy, setAcademy] = useState<Academy>();
  const [reviews, setReviews] = useState<Review[]>([]);
  const [message, setMessage] = useState("Loading access-review history…");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) throw new Error();
        const history = await fetchReviews(academies[0].id);
        if (!active) return;
        setAcademy(academies[0]); setReviews(history); setMessage("");
      } catch {
        if (active) setMessage("Access-review history could not be loaded.");
      }
    })();
    return () => { active = false; };
  }, []);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || pending.current) return;
    const element = event.currentTarget;
    const form = new FormData(element);
    pending.current = true; setSaving(true); setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/access-reviews`, {
        method: "POST", headers: apiHeaders(true),
        body: JSON.stringify({ notes: form.get("notes"), createFollowUp: form.get("followUp") === "on" }),
      });
      if (!response.ok) return setMessage(response.status >= 500
        ? "Sign-off could not be confirmed. Check review history before retrying."
        : "Sign-off could not be saved. Your entries have been retained.");
      element.reset();
      setMessage("Access review signed off.");
      try {
        setReviews(await fetchReviews(academy.id));
      } catch {
        setMessage("Access review signed off. Review history could not be refreshed; do not repeat the sign-off. Refresh the page to check the saved review.");
      }
    } catch {
      setMessage("Sign-off could not be confirmed. Check review history before retrying.");
    } finally { pending.current = false; setSaving(false); }
  }
  return <main className="enterprise-settings">
    <header className="enterprise-page-header"><p>Administration / access governance</p><h2>Access review sign-off</h2><span>Record the review outcome and create an accountable remediation task where required.</span></header>
    {message && <p className="enterprise-page-state mt-5" role="status" aria-live="polite">{message}</p>}
    <section className="mt-5 grid gap-5 xl:grid-cols-2">
      <form onSubmit={save} className="surface-panel rounded-xl p-5">
        <h3 className="font-semibold">Sign off access review</h3>
        <fieldset disabled={saving || !academy} aria-busy={saving} className="min-w-0 border-0 p-0">
          <textarea required name="notes" aria-label="Review notes" className="field mt-4 min-h-28" placeholder="Scope reviewed, exceptions, owner and next action" />
          <label className="mt-3 flex gap-2 text-sm"><input name="followUp" type="checkbox" /> Create high-priority remediation task</label>
          <button className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950">{saving ? "Recording…" : "Record sign-off"}</button>
        </fieldset>
      </form>
      <section className="surface-panel rounded-xl p-5"><h3 className="font-semibold">Review history</h3>
        {reviews.map(review => <div key={review.id} className="mt-3 rounded border border-slate-700 p-3"><b>{new Date(review.reviewedAtUtc).toLocaleString()}</b><p className="mt-1 text-sm text-slate-400">{review.notes || "No notes recorded."}</p></div>)}
        {academy && !reviews.length && <p className="mt-4 text-slate-400">No access reviews have been signed off.</p>}
      </section>
    </section>
  </main>;
}
