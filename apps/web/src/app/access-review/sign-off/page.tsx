"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Review = { id: string; reviewedAtUtc: string; notes?: string };
type SignOffReceipt = { id: string; reviewedAtUtc: string; reviewerUserId: string; followUpId: string | null };
const validId = (value: unknown): value is string => typeof value === "string"
  && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
  && value !== "00000000-0000-0000-0000-000000000000";
const validDate = (value: unknown): value is string => typeof value === "string"
  && /T.*(?:Z|[+-]\d{2}:\d{2})$/i.test(value) && Number.isFinite(Date.parse(value));
function validReceipt(value: unknown, followUp: boolean): value is SignOffReceipt {
  if (!value || typeof value !== "object") return false;
  const receipt = value as Partial<SignOffReceipt>;
  return validId(receipt.id) && validDate(receipt.reviewedAtUtc) && validId(receipt.reviewerUserId)
    && (followUp ? validId(receipt.followUpId) : receipt.followUpId === null);
}
async function fetchReviews(academyId: string) {
  const response = await academyApi(`/api/academies/${academyId}/access-reviews`);
  if (!response.ok) throw new Error();
  const reviews: Review[] = await response.json();
  if (!Array.isArray(reviews)) throw new Error();
  return reviews.map(review => {
    if (!review || !validId(review.id) || (review.notes != null && typeof review.notes !== "string")) throw new Error();
    // SQL DateTime materialization can omit the suffix on this explicitly UTC field.
    const reviewedAtUtc = typeof review.reviewedAtUtc === "string"
      && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?$/.test(review.reviewedAtUtc)
      ? review.reviewedAtUtc + "Z" : review.reviewedAtUtc;
    if (!validDate(reviewedAtUtc)) throw new Error();
    return { ...review, reviewedAtUtc };
  });
}
export default function SignOff() {
  const [academy, setAcademy] = useState<Academy>();
  const [reviews, setReviews] = useState<Review[]>([]);
  const [message, setMessage] = useState("Loading access-review history…");
  const [saving, setSaving] = useState(false);
  const [refreshing, setRefreshing] = useState(true);
  const [ready, setReady] = useState(false);
  const [locked, setLocked] = useState(false);
  const pending = useRef(true);
  const uncertain = useRef(false);
  const scope = useRef<Academy | undefined>(undefined);
  const generation = useRef(0);
  const mounted = useRef(false);
  async function loadHistory(current: () => boolean) {
    let selected = scope.current;
    if (!selected) {
      const response = await academyApi("/api/academies");
      if (!response.ok) throw new Error();
      const academies: Academy[] = await response.json();
      if (!Array.isArray(academies) || !validId(academies[0]?.id)) throw new Error();
      selected = { id: academies[0].id };
      if (!current()) return;
      scope.current = selected;
    }
    const history = await fetchReviews(selected.id);
    if (!current()) return;
    setAcademy(selected); setReviews(history); setReady(true);
  }
  useEffect(() => {
    mounted.current = true;
    const token = ++generation.current;
    const current = () => mounted.current && generation.current === token;
    pending.current = true;
    void (async () => {
      try {
        await loadHistory(current);
        if (current()) setMessage("");
      } catch {
        if (current()) setMessage("Access-review history could not be loaded. Refresh review history to try again.");
      } finally {
        if (current()) { pending.current = false; setRefreshing(false); }
      }
    })();
    return () => { mounted.current = false; generation.current = token + 1; };
    // Loading runs once per mount; later reads use the same resolved academy.
  }, []);
  async function refreshHistory() {
    if (pending.current || !mounted.current) return;
    pending.current = true; setRefreshing(true);
    const token = generation.current;
    const current = () => mounted.current && generation.current === token;
    try {
      await loadHistory(current);
      if (!current()) return;
      uncertain.current = false; setLocked(false);
      setMessage("Review history refreshed. Check the register for your previous sign-off before recording another review.");
    } catch {
      if (current()) setMessage("Review history could not be refreshed. Your entries have been retained. Check the register before retrying.");
    } finally {
      if (current()) { pending.current = false; setRefreshing(false); }
    }
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !ready || uncertain.current || pending.current || !mounted.current) return;
    const element = event.currentTarget;
    const form = new FormData(element);
    const followUp = form.get("followUp") === "on";
    const token = generation.current;
    const current = () => mounted.current && generation.current === token;
    const unconfirmed = () => {
      uncertain.current = true; setLocked(true);
      setMessage("Sign-off could not be confirmed. Your entries have been retained. Refresh review history and check the register before retrying.");
    };
    pending.current = true; setSaving(true); setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/access-reviews`, {
        method: "POST", headers: apiHeaders(true),
        body: JSON.stringify({ notes: form.get("notes"), createFollowUp: followUp }),
      });
      if (!current()) return;
      if (!response.ok) {
        if ([400, 401, 403, 404, 409, 422].includes(response.status))
          setMessage("Sign-off could not be saved. Your entries have been retained.");
        else unconfirmed();
        return;
      }
      const receipt: unknown = await response.json();
      if (!current()) return;
      if (response.status !== 200 || !validReceipt(receipt, followUp)) return unconfirmed();
      element.reset();
      setMessage("Access review signed off.");
      try {
        const history = await fetchReviews(academy.id);
        if (current()) setReviews(history);
      } catch {
        if (current()) {
          uncertain.current = true; setLocked(true);
          setMessage("Access review signed off. Review history could not be refreshed; do not repeat the sign-off. Refresh review history to check the saved review.");
        }
      }
    } catch {
      if (current()) unconfirmed();
    } finally {
      if (current()) { pending.current = false; setSaving(false); }
    }
  }
  return <main className="enterprise-settings">
    <header className="enterprise-page-header"><p>Administration / access governance</p><h2>Access review sign-off</h2><span>Record the review outcome and create an accountable remediation task where required.</span></header>
    {message && <p className="enterprise-page-state mt-5" role="status" aria-live="polite">{message}</p>}
    <section className="mt-5 grid gap-5 xl:grid-cols-2">
      <form onSubmit={save} className="surface-panel rounded-xl p-5">
        <h3 className="font-semibold">Sign off access review</h3>
        <fieldset disabled={saving || refreshing || !academy || !ready || locked} aria-busy={saving} className="min-w-0 border-0 p-0">
          <textarea required name="notes" aria-label="Review notes" className="field mt-4 min-h-28" placeholder="Scope reviewed, exceptions, owner and next action" />
          <label className="mt-3 flex gap-2 text-sm"><input name="followUp" type="checkbox" /> Create high-priority remediation task</label>
          <button style={{ minHeight: 44 }} className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950">{saving ? "Recording…" : "Record sign-off"}</button>
        </fieldset>
      </form>
      <section className="surface-panel rounded-xl p-5"><h3 className="font-semibold">Review history</h3>
        <button type="button" onClick={refreshHistory} disabled={saving || refreshing} aria-busy={refreshing} style={{ minHeight: 44 }} className="mt-3 rounded-lg border border-current px-4 py-2 text-sm font-semibold disabled:opacity-50">Refresh review history</button>
        {reviews.map(review => <div key={review.id} className="mt-3 rounded border border-slate-700 p-3"><b>{new Date(review.reviewedAtUtc).toLocaleString()}</b><p className="mt-1 text-sm text-slate-400">{review.notes || "No notes recorded."}</p></div>)}
        {academy && !reviews.length && <p className="mt-4 text-slate-400">No access reviews have been signed off.</p>}
      </section>
    </section>
  </main>;
}
