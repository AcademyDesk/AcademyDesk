"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { createEnrollmentCreation, initialEnrollmentCreationState, type EnrollmentCandidate, type EnrollmentTransport } from "@/lib/penta-enrollment-creation";
import styles from "./enrollment-assistant.module.css";

type Driver = ReturnType<typeof createEnrollmentCreation>;
export default function EnrollmentCreation({ transport, candidate, onActivity }: {
  transport: EnrollmentTransport; candidate: EnrollmentCandidate | null; onActivity(active: boolean): void;
}) {
  const driver = useRef<Driver | null>(null);
  const [view, setView] = useState(initialEnrollmentCreationState);
  useEffect(() => {
    let mounted = true;
    const operation = createEnrollmentCreation(transport, () => window.sessionStorage, value => {
      if (!mounted) return;
      onActivity(value.key !== null);
      setView(value);
    });
    driver.current = operation;
    void Promise.resolve().then(() => operation.initialise());
    return () => { mounted = false; operation.dispose(); driver.current = null; };
  }, [transport, onActivity]);
  useEffect(() => { driver.current?.setCandidate(candidate); }, [candidate]);

  const draft = view.draft;
  const key = view.key;
  const ready = view.availability === "ready";
  return <section className={styles.result} aria-labelledby="enrollment-creation-heading" aria-busy={view.pending}>
    <h2 id="enrollment-creation-heading">Protected enrollment creation</h2>
    {view.notice && <p role="status">{view.notice}</p>}
    {view.pending && <p role="status">Checking this enrollment operation…</p>}
    {view.availability === "unavailable" && <button type="button" disabled={view.pending} onClick={() => void driver.current?.initialise()}>Check creation availability</button>}
    {!key && ready && <>
      <p>Review an eligible student and batch before approving anything. Creation is a separate explicit step.</p>
      <button type="button" disabled={view.pending || !candidate} onClick={() => { if (candidate) void driver.current?.prepare(candidate); }}>Review exact enrollment</button>
    </>}
    {draft && <dl className={styles.details}>
      <div><dt>Student</dt><dd>{view.labels?.student ?? "Verified student"}<code>{draft.admission.studentId}</code></dd></div>
      <div><dt>Batch</dt><dd>{view.labels?.batch ?? "Verified batch"}<code>{draft.admission.batchId}</code></dd></div>
      <div><dt>Start date</dt><dd>{draft.admission.startDate}</dd></div>
      <div><dt>Requested status</dt><dd>{draft.admission.status}</dd></div>
    </dl>}
    {key && <div className={styles.actions}>
      {ready && view.phase === "review" && draft?.status === "Prepared" && <button type="button" disabled={view.pending} onClick={() => void driver.current?.approve(draft)}>Approve exact enrollment</button>}
      {ready && view.phase === "approved" && draft?.status === "Authorized" && <>
        <p>Approved; creation is not yet confirmed. This action submits only the same approved enrollment.</p>
        <button type="button" disabled={view.pending} onClick={() => void driver.current?.execute(draft)}>Create approved enrollment</button>
      </>}
      {ready && view.phase !== "expired" && <button type="button" disabled={view.pending} onClick={() => void driver.current?.recover(key)}>Check enrollment status</button>}
      <button type="button" disabled={view.pending} onClick={() => driver.current?.close(key)}>{view.phase === "succeeded" ? "Done" : view.phase === "review" ? "Cancel review" : "Close after checking Manual Enrollments"}</button>
    </div>}
    {view.phase === "succeeded" && draft?.status === "Succeeded" && <div role="status">
      <h3>Enrollment created</h3><p>The host verified the committed enrollment and its audit.</p>
      <p>Receipt ID <code>{draft.receipt.receiptId}</code></p><p>Enrollment ID <code>{draft.receipt.enrollment.id}</code></p>
      <Link href={`/student-management?studentId=${draft.admission.studentId}`}>Open Student 360</Link>
    </div>}
    <p><Link href="/enrollments">Open Manual Enrollments</Link></p>
  </section>;
}
