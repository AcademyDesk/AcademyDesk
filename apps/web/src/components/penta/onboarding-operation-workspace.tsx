"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { OperationalWorkspace, type Projection, type Intent } from "@/vendor/penta-operational/frontend/operational-ui/src";
import { Button, Notice } from "@/vendor/penta-operational/frontend/shared-foundation/src";
import { createOnboardingOperationChannel, OnboardingOperationUnavailable, type OnboardingOperationState } from "@/lib/penta-onboarding-operation";

type Props = { projection: Projection; theme: "orbit" | "aura" | "prism"; onManual: () => void;
  onNew: () => void; onTheme: (theme: "orbit" | "aura" | "prism") => void;
  onSend?: (intent: Extract<Intent, { kind: "send" }>, signal: AbortSignal) => Promise<void> };
export default function OnboardingOperationWorkspace({ projection, theme, onManual, onNew, onTheme, onSend }: Props) {
  const channel = useRef<ReturnType<typeof createOnboardingOperationChannel> | null>(null);
  const alive = useRef(true), lock = useRef(false);
  const [flow, setFlow] = useState<OnboardingOperationState | null>(null);
  const [busy, setBusy] = useState(false), [accessDenied, setAccessDenied] = useState(false);
  const [selectionEpoch, setSelectionEpoch] = useState(0);
  useEffect(() => { alive.current = true; return () => { alive.current = false; channel.current?.dispose(); }; }, []);
  const terminal = projection.host.draft?.status === "succeeded";
  const candidate = !!projection.host.draft && ["awaiting_approval", "approved", "succeeded"].includes(projection.host.draft.status);
  async function act(kind: "review" | "execute" | "recover" | "confirm", intent?: Extract<Intent, { kind: "confirm" }>, signal?: AbortSignal) {
    if (!candidate || lock.current || !alive.current || accessDenied) throw new Error("This action is unavailable.");
    lock.current = true; setBusy(true);
    try {
      // Construct only after an explicit gesture, not render/mount/prefetch.
      channel.current ??= createOnboardingOperationChannel(projection);
      const value = kind === "confirm" ? await channel.current.confirm(intent!, signal) : await channel.current[kind]();
      if (alive.current) setFlow(value);
    } catch (error) {
      if (alive.current) {
        if (error instanceof OnboardingOperationUnavailable && error.access) { setAccessDenied(true); channel.current?.dispose(); }
        setFlow({ phase: "unknown", projection, studentId: null });
      }
      throw error;
    } finally { lock.current = false; if (alive.current) setBusy(false); }
  }
  const gesture = (kind: "review" | "execute" | "recover") => { void act(kind).catch(() => {}); };
  return <>
    {candidate && <section aria-label="Host creation controls" style={{ padding: "12px 16px", flexShrink: 0 }} aria-busy={busy}>
      {accessDenied ? <Notice title="Current academy access could not be verified" tone="warning">Private review details are hidden. Sign in to your academy or use Manual.</Notice> :
        flow?.phase === "unknown" ? <><Notice title="Creation outcome is unconfirmed" tone="warning">Do not submit creation again. Checking the saved result only reads this same host operation.</Notice>
          <Button disabled={busy} onClick={() => gesture("recover")}>Check saved result</Button></> :
          flow?.phase === "succeeded" ? <Notice title="Student created" tone="success">The host verified the saved receipt and audit. No portal accounts were created.
            {flow.studentId && <p><Link href={`/student-management?studentId=${flow.studentId}`}>Open Student 360</Link></p>}</Notice> :
            flow?.phase === "approved" ? <><Notice title="Approved, not yet confirmed as created" tone="info">Create only the exact approved student and optional guardian shown in the work canvas.</Notice>
              <Button disabled={busy} onClick={() => gesture("execute")}>Create approved student</Button></> :
              flow?.phase === "review" ? <Notice title="Review before approval" tone="info">Open the work canvas, check every canonical field and select Confirm exact operation. This approves the draft; creation is a separate explicit step.</Notice> :
                terminal ? <><Notice title="Verify the previously completed result" tone="info">This draft is marked completed. Check its saved operation, receipt and audit before opening the student. Nothing will be approved or created again.</Notice>
                  <Button disabled={busy} onClick={() => gesture("review")}>Verify saved result</Button></> :
                  <><Notice title="Host-prepared operation" tone="info">This screen does not invoke AI. Check the protected host operation before approving or creating anything.</Notice>
                    <Button disabled={busy} onClick={() => gesture("review")}>Review prepared operation</Button></>}
    </section>}
    {!accessDenied && flow?.phase !== "unknown" && (!terminal || flow?.phase === "succeeded") && <OperationalWorkspace key={selectionEpoch} projection={flow?.projection ?? projection} theme={theme} adapter={{
      onManual, onTheme, onIntent: async (intent, signal) => {
        if (intent.kind === "new_conversation") { onNew(); return; }
        if (intent.kind === "send" && onSend) { await onSend(intent, signal); return; }
        if (intent.kind === "select_capability" && intent.capability === "Executor" && onSend) { setSelectionEpoch(value => value + 1); return; }
        if (intent.kind === "confirm") { await act("confirm", intent, signal); return; }
        throw new Error("This operation does not dispatch conversational tools.");
      },
    }} />}
  </>;
}
